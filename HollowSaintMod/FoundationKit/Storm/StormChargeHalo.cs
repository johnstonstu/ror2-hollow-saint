using RoR2;
using UnityEngine;
using HollowSaint.FoundationKit.Vfx;

namespace HollowSaint.FoundationKit.Storm
{
    /// <summary>Each earned charge adds an orb orbiting the halo; empty slots stay invisible.
    /// The orbit follows the live ring (HaloRing), so when Open Circuit lays the arcs flat
    /// into a crown the orbs swing with it. Runs after HaloRing (150).</summary>
    [DefaultExecutionOrder(160)]
    [DisallowMultipleComponent]
    public sealed class StormChargeHalo : MonoBehaviour
    {
        private const int MaxOrbs = 20;
        private const float OrbitRadius = 0.78f;
        private const float OrbitDegreesPerSecond = 9f;
        private const float RimRadius = 0.43f;
        /// <summary>Orbit plane follow rate (1/s): the 0.5 s crown unfold is tracked with
        /// about 0.06 s of lag, and bone jitter never reaches the orbs.</summary>
        private const float PlaneFollow = 16f;
        private static readonly int TintColor = Shader.PropertyToID("_TintColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private CharacterBody body;
        private DischargeMeter meter;
        private Transform halo;
        private Transform orbitRoot;
        private MeshRenderer[] renderers;
        private MaterialPropertyBlock properties;
        private int builtForMax;
        private readonly StormChargeSequence sequence = new StormChargeSequence();
        private float nextArc;
        private int arcIndex;
        private bool gatherSound;
        private SkinFxPalette palette = SkinFxPalette.ForIndex(0);
        private HaloRing ring;
        private Vector3 planePosition;
        private Quaternion planeRotation = Quaternion.identity;
        private float radiusScale = 1f;
        private bool hasPlane;
        // 1.3.1 (Stu): a newly banked charge pops (big, white-hot, a short arc from the body), then settles.
        private const float PopSeconds = .35f;
        private float[] popAt = new float[0];
        private int lastLit = -1;

        private void Start()
        {
            body = GetComponent<CharacterBody>();
            meter = GetComponent<DischargeMeter>();
            halo = KitUtil.ResolveSocket(body, "Halo");
            if (!body || !meter || !halo) { enabled = false; return; }
            ring = HaloRing.For(body);
            VfxAssets.Load();
            properties = new MaterialPropertyBlock();
            Resize(Mathf.Clamp(KitTuning.StormChargeMax, 2, MaxOrbs));
        }

        private void OnDestroy()
        {
            StopGatherSound();
            if (orbitRoot) Destroy(orbitRoot.gameObject);
        }

        private void OnDisable() { StopGatherSound(); }

        private void StopGatherSound()
        {
            if (!gatherSound) return;
            KitSfx.StopThunderGather(gameObject);
            gatherSound = false;
        }

        private void LateUpdate()
        {
            if (!body || !meter || !halo) return;
            if (Gaze.GazeFuelController.OwnsPresentation(body) || ChargedStorm.StoredChargeState.IsGathering(body) || Thundercloud.ThundercloudCrownPose.OwnsPresentation(body))
            {
                StopGatherSound();
                if (orbitRoot && orbitRoot.gameObject.activeSelf) orbitRoot.gameObject.SetActive(false);
                return;
            }
            var currentPalette = SkinFxPalette.ForBody(body);
            if (currentPalette != palette)
            {
                palette = currentPalette;
                foreach (var renderer in renderers)
                    renderer.sharedMaterial = palette.Material(VfxAssets.ArcGlow ? VfxAssets.ArcGlow : VfxAssets.ArcCore);
            }
            int max = Mathf.Clamp(KitTuning.StormChargeMax, 2, MaxOrbs);
            if (max != builtForMax) Resize(max);

            FollowRing();
            bool alive = body.healthComponent && body.healthComponent.alive;
            if (!alive) StopGatherSound();
            var model = body.modelLocator && body.modelLocator.modelTransform
                ? body.modelLocator.modelTransform.GetComponent<CharacterModel>()
                : null;
            bool visible = alive && (!model || model.invisibilityCount <= 0);
            if (orbitRoot.gameObject.activeSelf != visible) orbitRoot.gameObject.SetActive(visible);
            int charge = alive ? Mathf.Clamp(meter.Charge, 0, max) : 0;
            bool full = alive && charge >= max;
            if (popAt.Length != renderers.Length) { popAt = new float[renderers.Length]; for (int i = 0; i < popAt.Length; i++) popAt[i] = -10f; }
            if (lastLit >= 0 && charge > lastLit && visible)
            {
                for (int i = lastLit; i < charge && i < popAt.Length; i++) { popAt[i] = Time.time; Pop(i); }
                // An audible gain cue: a chime that rises with the bank (the full bank has its own cue).
                if (charge < max && CustomSoundBank.Ready) Util.PlaySound("Play_HS_GazeLoad" + Mathf.Clamp(charge, 1, 5), gameObject);
            }
            lastLit = charge;
            sequence.Tick(Time.time, Time.deltaTime, alive);
            bool released = sequence.Released(Time.time);

            Color active = full ? palette.Core : palette.Arc;
            // v0.9: the orbs dim with the halo while a Stormspear charges in the hand, flash back on release.
            float pulse = (full ? 0.72f + 0.28f * Mathf.Sin(Time.time * 10f) : 1f) * Stormspear.Fx.StormspearFx.HaloOf(body);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = !released && i < charge;
                float angle = Mathf.PI * 2f * i / max;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                renderers[i].transform.localPosition = direction * (OrbitRadius * radiusScale) * (1f - sequence.GatherAmount);
                Color color = i < charge ? active * pulse : new Color(0.025f, 0.06f, 0.08f, 1f);
                float pop = i < popAt.Length ? 1f - Mathf.Clamp01((Time.time - popAt[i]) / PopSeconds) : 0f;
                if (pop > 0f && i < charge) color = Color.Lerp(color, palette.Core * 2.2f, pop);
                color.a = 1f;
                SetTint(renderers[i], color);
                renderers[i].transform.localScale = Vector3.one * (i < charge ? (full ? 0.105f : 0.085f) : 0.055f) * (1f + 2.2f * pop * pop);
            }
            if (visible && charge > 0 && !released) Crackle(charge);
        }

        private void Pop(int index)
        {
            try
            {
                if (index < 0 || index >= renderers.Length || !renderers[index]) return;
                Vector3 at = renderers[index].transform.position;
                VfxParticles.Burst(at, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, .16f, Vector2.zero, new Vector2(.45f, .6f), palette.Core);
                VfxParticles.Burst(at, Quaternion.identity, palette.Material(VfxAssets.Spark), 8, .25f, new Vector2(2f, 5f), new Vector2(.04f, .08f), palette.Arc, stretch: .06f);
                if (body) LightningLine.Spawn(body.corePosition, at, .14f, .1f, 1, .2f, palette: palette);
            }
            catch (System.Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_CHARGE_POP " + error.Message); }
        }

        /// <summary>Explicit server beats, never inferred from a charge reset or death.</summary>
        internal void ReceiveThunderBeat(Beat beat, Vector3 target, float duration, int revision, SkinFxPalette eventPalette = null)
        {
            if (revision <= sequence.LastRevision) return;
            bool release = sequence.Receive(beat, duration, revision, Time.time);
            StopGatherSound(); // cancellation, re-pick and launch all finish the old charge sound
            if (!body || !body.healthComponent || !body.healthComponent.alive) return;
            if (beat == Beat.ThunderGather)
            {
                BodyCurrentFx.Thunder(body, duration);
                KitSfx.Play(Beat.ThunderTelegraph, gameObject, true);
                gatherSound = true;
            }
            if (release)
            {
                BodyCurrentFx.Thunder(body, 0.35f);
                KitSfx.Play(Beat.ThunderRelease, gameObject, true);
                if (orbitRoot && orbitRoot.gameObject.activeInHierarchy)
                    StormChargeRelease.Play(orbitRoot.position, target, eventPalette ?? SkinFxPalette.ForBody(body),
                        duration > 0.05f ? duration : KitTuning.ThunderboltFlightSeconds, orbitRoot, gameObject);
            }
            if (beat == Beat.ThunderCancel) BodyCurrentFx.Thunder(body, 0f);
        }

        private void Crackle(int charge)
        {
            var current = body ? body.GetComponent<BodyCurrentFx>() : null;
            if (!current || !current.IsActive) return;
            if (Time.time < nextArc) return;
            nextArc = Time.time + Mathf.Lerp(0.28f, 0.09f, charge / (float)builtForMax);
            // Two short arcs per tick at most, following the moving orb and physical ring.
            int count = Mathf.Min(2, charge);
            for (int n = 0; n < count; n++)
            {
                int index = arcIndex++ % charge;
                Vector3 orb = renderers[index].transform.position;
                Vector3 rim;
                if (ring && ring.Valid) rim = ring.Shape.Nearest(orb); // lands on the copper
                else
                {
                    float angle = Mathf.PI * 2f * index / builtForMax;
                    rim = orbitRoot.TransformPoint(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (RimRadius * radiusScale));
                }
                var line = LightningLine.Spawn(renderers[index].transform.position, rim, 0.12f, 0.22f, 0, 0.18f, palette: palette);
                line.startAnchor = renderers[index].transform;
            }
        }

        /// <summary>Places the orbit in the live ring plane: centre on the ring centre, local Y
        /// along the ring normal, local Z toward arc 1, plus the slow spin. Smoothed in the
        /// socket's space so body motion never lags, only the plane change is eased.
        /// Falls back to the socket's own XZ plane (the authored rest ring) without a ring.</summary>
        private void FollowRing()
        {
            Vector3 targetPosition;
            Quaternion targetRotation;
            float targetScale = 1f;
            if (ring && ring.Valid)
            {
                var shape = ring.Shape;
                targetPosition = halo.InverseTransformPoint(shape.Center);
                targetRotation = Quaternion.Inverse(halo.rotation) * Quaternion.LookRotation(shape.Axis, shape.Normal);
                targetScale = ring.RadiusScale;
            }
            else
            {
                // Blender bones point along their local Y. The halo socket points out of the
                // ring, so its XZ plane matches the copper halo (bundle03: socket is -90deg X).
                targetPosition = halo.parent && halo.parent.name == "halo root"
                    ? halo.InverseTransformPoint(halo.parent.position) : Vector3.zero;
                targetRotation = Quaternion.identity;
            }
            if (!hasPlane)
            {
                planePosition = targetPosition;
                planeRotation = targetRotation;
                radiusScale = targetScale;
                hasPlane = true;
            }
            else
            {
                float follow = 1f - Mathf.Exp(-PlaneFollow * Time.deltaTime);
                planePosition = Vector3.Lerp(planePosition, targetPosition, follow);
                planeRotation = Quaternion.Slerp(planeRotation, targetRotation, follow);
                radiusScale = Mathf.Lerp(radiusScale, targetScale, follow);
            }
            orbitRoot.localPosition = planePosition;
            orbitRoot.localRotation = planeRotation * Quaternion.Euler(0f, Time.time * OrbitDegreesPerSecond, 0f);
        }

        private void Resize(int count)
        {
            if (orbitRoot) Destroy(orbitRoot.gameObject);
            builtForMax = count;
            orbitRoot = new GameObject("HS_StormChargeOrbits").transform;
            orbitRoot.SetParent(halo, false);
            if (hasPlane)
                orbitRoot.localPosition = planePosition;
            else if (halo.parent && halo.parent.name == "halo root")
                orbitRoot.localPosition = halo.InverseTransformPoint(halo.parent.position);
            renderers = new MeshRenderer[count];
            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i) / count;
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = "HS_StormChargeOrb" + i;
                orb.transform.SetParent(orbitRoot, false);
                orb.transform.localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * OrbitRadius;
                orb.transform.localScale = Vector3.one * 0.055f;
                var collider = orb.GetComponent<Collider>();
                if (collider) Destroy(collider);
                renderers[i] = orb.GetComponent<MeshRenderer>();
                renderers[i].enabled = false; // no empty placeholders or first-frame flash
                renderers[i].sharedMaterial = palette.Material(VfxAssets.ArcGlow ? VfxAssets.ArcGlow : VfxAssets.ArcCore);
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderers[i].receiveShadows = false;
            }
        }

        private void SetTint(MeshRenderer renderer, Color color)
        {
            renderer.GetPropertyBlock(properties);
            properties.SetColor(TintColor, color);
            properties.SetColor(ColorProperty, color);
            renderer.SetPropertyBlock(properties);
        }
    }
}
