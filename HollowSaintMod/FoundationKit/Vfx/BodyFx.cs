using System.Collections.Generic;
using System.Linq;
using HollowSaint.FoundationKit.ArcStep;
using HollowSaint.FoundationKit.OpenCircuit;
using HollowSaint.FoundationKit.SpearDischarge;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>
    /// Per-body presentation that runs on every machine and reads only replicated state:
    /// chest-core glow and halo crackle driven by the Discharge meter, heel jets and Arc Step
    /// afterimages. The Open Circuit crown lives in CircuitCrownFx.
    /// </summary>
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class BodyFx : MonoBehaviour
    {
        private CharacterBody body;
        private SkinFxPalette palette;
        private FoundationPresentation presentation;
        private DischargeMeter meter;
        private Transform core;
        private Transform halo;
        private Light coreLight;
        private ParticleSystem coreGlow;
        private HaloRing ring;
        private BodyCurrentFx current;
        private float crackleTimer;
        private float afterimageTimer;
        private float dashRemaining;
        private SkinnedMeshRenderer[] afterimageSources;
        private Transform heelL;
        private Transform heelR;
        private ParticleSystem jetL;
        private ParticleSystem jetR;
        private ParticleSystem jetCoreL;
        private ParticleSystem jetCoreR;
        private Vector3 jetDirection;
        private bool hasJetDirection;
        private bool wasGrounded = true;
        private Vector3 lastTrailPoint;
        private float trailTimer;

        private void Start()
        {
            body = GetComponent<CharacterBody>();
            presentation = body && body.modelLocator && body.modelLocator.modelTransform
                ? body.modelLocator.modelTransform.GetComponent<FoundationPresentation>() : null;
            meter = GetComponent<DischargeMeter>();
            core = KitUtil.ResolveSocket(body, "Core");
            heelL = KitUtil.ResolveSocket(body, "HeelL");
            heelR = KitUtil.ResolveSocket(body, "HeelR");
            halo = KitUtil.ResolveSocket(body, "Halo");
            ring = HaloRing.For(body);
            if (!GetComponent<BodyCurrentFx>()) gameObject.AddComponent<BodyCurrentFx>();
            current = GetComponent<BodyCurrentFx>();
            // Open Circuit crown and feed (replaces the old four world-axis crown lines).
            if (!GetComponent<CircuitCrownFx>()) gameObject.AddComponent<CircuitCrownFx>();
            VfxAssets.Load();
            palette = SkinFxPalette.ForBody(body);
            if (core)
            {
                var lightGo = new GameObject("HS_CoreLight");
                lightGo.transform.SetParent(core, false);
                coreLight = lightGo.AddComponent<Light>();
                coreLight.type = LightType.Point;
                coreLight.range = 3f;
                coreLight.shadows = LightShadows.None;
                coreGlow = VfxParticles.Loop(core, palette.Material(VfxAssets.Flash), 30f, 0.1f, Vector2.zero, new Vector2(0.25f, 0.3f), palette.Arc);
                var main = coreGlow.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
        }

        private void RefreshPalette()
        {
            var current = SkinFxPalette.ForBody(body);
            if (ReferenceEquals(current, palette)) return;
            palette = current;
            ClearJets();
            if (coreGlow)
                coreGlow.GetComponent<ParticleSystemRenderer>().sharedMaterial = palette.Material(VfxAssets.Flash);
        }

        private void OnDestroy()
        {
            ClearJets();
        }

        private void OnDisable()
        {
            ClearJets();
            dashRemaining = 0f;
            if (coreLight) coreLight.intensity = 0f;
            if (coreGlow) coreGlow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            if (!body) return;
            RefreshPalette();
            bool alive = body.healthComponent && body.healthComponent.alive;
            UpdateAfterimages();
        }

        private void LateUpdate()
        {
            // The pose modifier runs at order 100. Read its final heel transforms here.
            if (!body || !presentation) { ClearJets(); return; }
            var model = body.modelLocator && body.modelLocator.modelTransform ? body.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
            bool visible = presentation.IsPresentingAlive && (!model || model.invisibilityCount <= 0);
            UpdateCore(meter ? meter.Normalized : 0f, visible);
            UpdateCrackle(visible && current && current.IsActive);
            UpdateJets(visible);
        }

        /// <summary>Heel jets: skin-colored compact plumes from both heels while gliding
        /// (sprint), pointing opposite the travel direction; a burst when leaving the
        /// ground upward.</summary>
        private void UpdateJets(bool alive)
        {
            if (!alive) { ClearJets(); return; }
            if (!heelL || !heelR || !body.characterMotor) { ClearJets(); return; }
            var motor = body.characterMotor;
            Vector3 v = motor.velocity;
            float planar = new Vector2(v.x, v.z).magnitude;
            bool dashing = ArcStepState.IsBodyDashing(body);
            if (dashing) ClearJets(); // no detached exhaust persists into an Arc Step
            float glideWeight = dashing ? 0f : Mathf.Clamp01(presentation.GlideWeight);
            // Retain a faint plume at low speed and build it smoothly toward the
            // authored 8.7 m/s glide speed; no hard 1 m/s on/off edge.
            float speedWeight = Mathf.Lerp(0.15f, 1f,
                Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 8.7f, planar)));
            float weight = glideWeight * speedWeight;

            if (weight > 0.01f && jetL == null)
            {
                jetL = MakeJet(heelL, out jetCoreL);
                jetR = MakeJet(heelR, out jetCoreR);
                jetL.transform.SetParent(null, true);
                jetR.transform.SetParent(null, true);
            }
            if (jetL != null && weight <= 0.01f)
            {
                StopJets();
            }
            if (jetL != null)
            {
                if (!dashing) AimJets(v, planar);
                SetJetStrength(jetL, jetCoreL, weight);
                SetJetStrength(jetR, jetCoreR, weight);
                jetL.transform.position = heelL.position;
                jetR.transform.position = heelR.position;
            }

            bool grounded = motor.isGrounded;
            // Air jump (double jump): a small storm cloud cracks under the feet.
            int jumps = motor.jumpCount;
            if (alive && !grounded && jumps > lastJumpCount && jumps >= 2) StormJump(motor);
            lastJumpCount = grounded ? 0 : jumps;
            if (alive && wasGrounded && !grounded && v.y > 1f)
            {
                foreach (var heel in new[] { heelL, heelR })
                    VfxParticles.Burst(heel.position, ConeAim(Vector3.down), palette.Material(VfxAssets.Spark), 10, 0.25f,
                        new Vector2(4f, 9f), new Vector2(0.04f, 0.08f), palette.Arc, stretch: 0.05f, spreadAngle: 30f);
            }
            wasGrounded = grounded;
        }

        private int lastJumpCount;

        /// <summary>Orient a cone's local forward axis along its desired world direction.</summary>
        private static Quaternion ConeAim(Vector3 direction)
        {
            return Quaternion.FromToRotation(Vector3.forward, direction.normalized);
        }

        private void StormJump(CharacterMotor motor)
        {
            Vector3 feet = body.footPosition;
            Vector3 cloud = feet + Vector3.down * 0.15f;
            VfxParticles.Burst(cloud, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.22f, Vector2.zero, new Vector2(0.9f, 1.1f), palette.Outer);
            VfxParticles.Burst(cloud, ConeAim(Vector3.down), palette.Material(VfxAssets.Flash), 6, 0.45f,
                new Vector2(0.6f, 1.6f), new Vector2(0.35f, 0.6f), palette.Outer, spreadAngle: 30f);
            VfxParticles.Burst(feet, ConeAim(Vector3.down), palette.Material(VfxAssets.Spark), 16, 0.35f,
                new Vector2(4f, 10f), new Vector2(0.08f, 0.16f), palette.Arc, stretch: 0.09f, spreadAngle: 55f);
            VfxParticles.Ring(cloud, Vector3.up, 0.2f, 1.5f, 0.3f, 0.06f, VfxAssets.Trail, palette: palette);
            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 90f + UnityEngine.Random.Range(-25f, 25f), 0f) * Vector3.forward;
                LightningLine.Spawn(cloud, cloud + dir * UnityEngine.Random.Range(0.7f, 1.1f) + Vector3.down * UnityEngine.Random.Range(0.4f, 1.2f), 0.2f, 0.7f, 1, 0.18f, palette: palette);
            }
            VfxParticles.FlashLight(cloud, palette.Arc, 1.5f, 4f, 0.15f);
            Util.PlaySound(KitSfx.AirJump, body.gameObject);
        }

        private void AimJets(Vector3 velocity, float planar)
        {
            Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 back = planarVelocity.sqrMagnitude > 0.0001f ? -planarVelocity.normalized : Vector3.zero;
            // At speed, exhaust points behind and below the heels. As speed drops it angles
            // farther down; world-down keeps the plume below the Saint through flips.
            float down = Mathf.Lerp(1.4f, 0.45f, Mathf.InverseLerp(1f, 8f, planar));
            Vector3 target = (back + Vector3.down * down).normalized;
            float follow = 1f - Mathf.Exp(-14f * Time.deltaTime);
            jetDirection = hasJetDirection
                ? Vector3.Lerp(jetDirection, target, follow).normalized
                : target;
            hasJetDirection = true;
            Quaternion aim = ConeAim(jetDirection);
            jetL.transform.rotation = aim;
            jetR.transform.rotation = aim;
        }

        private ParticleSystem MakeJet(Transform heel, out ParticleSystem core)
        {
            // Compact nozzle exhaust, rather than stretched hitspark sprites that
            // become parallel rails when viewed along the travel axis. Local space
            // bounds the plume length independently of the character's speed.
            var ps = VfxParticles.Loop(heel, palette.Material(VfxAssets.Flash), 0f, 0.12f, new Vector2(3f, 5f),
                new Vector2(0.07f, 0.11f), palette.Arc, spreadAngle: 12f);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            core = VfxParticles.Loop(ps.transform, palette.Material(VfxAssets.Flash), 0f, 0.06f, Vector2.zero,
                new Vector2(0.10f, 0.14f), palette.Core);
            main = core.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            return ps;
        }

        private static void SetJetStrength(ParticleSystem jet, ParticleSystem core, float weight)
        {
            var emission = jet.emission;
            emission.rateOverTime = 80f * weight;
            var main = jet.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f + 2.2f * weight, 1.2f + 3.8f * weight);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f * weight, 0.11f * weight);
            emission = core.emission;
            emission.rateOverTime = 30f * weight;
            main = core.main;
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f * weight, 0.14f * weight);
        }

        private void StopJets()
        {
            StopJet(jetL, false); StopJet(jetR, false);
            jetL = jetR = jetCoreL = jetCoreR = null;
            hasJetDirection = false;
        }

        private void ClearJets()
        {
            StopJet(jetL, true); StopJet(jetR, true);
            jetL = jetR = jetCoreL = jetCoreR = null;
            hasJetDirection = false;
        }

        private static void StopJet(ParticleSystem ps, bool clear)
        {
            if (!ps) return;
            ps.Stop(true, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
            ps.transform.SetParent(null, true);
            if (clear) Destroy(ps.gameObject);
            else Destroy(ps.gameObject, 0.4f);
        }

        private void UpdateCore(float charge, bool visible)
        {
            if (!coreLight) return;
            // Copper ember at empty, arc cyan at half, white-hot at full.
            Color c = charge < 0.5f
                ? Color.Lerp(HsPalette.Copper, palette.Arc, charge * 2f)
                : Color.Lerp(palette.Arc, palette.Core, (charge - 0.5f) * 2f);
            float active = visible && current ? current.Intensity : 0f;
            float pulse = charge >= 1f || active > 0f ? LightningRhythm.Gain(Time.time) : 1f;
            c = Color.Lerp(c, palette.Core, active * LightningRhythm.Pulse(Time.time) * 0.55f);
            coreLight.color = c;
            coreLight.intensity = visible ? (Mathf.Lerp(0.4f, 4f, charge) + active * 1.3f) * pulse : 0f;
            if (coreGlow)
            {
                var main = coreGlow.main;
                main.startColor = c;
                main.startSize = visible ? (Mathf.Lerp(0.12f, 0.55f, charge) + active * 0.1f) * pulse : 0f;
                var emission = coreGlow.emission; emission.enabled = visible;
                if (!visible) coreGlow.Clear(true);
                else if (!coreGlow.isPlaying) coreGlow.Play(true);
            }
        }

        private void UpdateCrackle(bool full)
        {
            if (!full || !halo) return;
            crackleTimer -= Time.deltaTime;
            if (crackleTimer > 0f) return;
            crackleTimer = Mathf.Lerp(0.18f, 0.07f, LightningRhythm.Pulse(Time.time));
            Vector3 a, b;
            if (ring && ring.Valid)
            {
                // Short sparks leaping off the copper itself, wherever the ring is posed.
                float angle = Random.Range(0f, Mathf.PI * 2f);
                a = ring.Shape.PointAt(angle);
                b = ring.Shape.PointAt(angle + Random.Range(0.25f, 0.6f), Random.Range(1.1f, 1.35f));
            }
            else
            {
                a = halo.position + Random.onUnitSphere * 0.3f;
                b = halo.position + Random.onUnitSphere * 0.4f;
            }
            LightningLine.Spawn(a, b, 0.08f, 0.45f, 0, 0.3f, palette: palette);
        }

        /// <summary>Called by ArcStepState on every machine when a dash starts.</summary>
        public void BeginDash(float duration)
        {
            dashRemaining = duration;
            afterimageTimer = 0f;
            lastTrailPoint = Vector3.zero;
            trailTimer = 0f;
        }

        private void UpdateAfterimages()
        {
            if (!body || !body.healthComponent || !body.healthComponent.alive || !ArcStepState.IsBodyDashing(body))
            { dashRemaining = 0f; return; }
            if (dashRemaining <= 0f) return;
            dashRemaining -= Time.deltaTime;
            // Thin angular ground trail along the dash path.
            if (body.characterMotor && body.characterMotor.isGrounded)
            {
                trailTimer -= Time.deltaTime;
                Vector3 foot = body.footPosition + Vector3.up * 0.05f;
                if (trailTimer <= 0f)
                {
                    if (lastTrailPoint != Vector3.zero && (foot - lastTrailPoint).sqrMagnitude > 0.04f)
                        LightningLine.Spawn(lastTrailPoint, foot, 0.5f, 0.5f, 1, 0.06f, palette: palette);
                    lastTrailPoint = foot;
                    trailTimer = 0.08f;
                }
            }
            else lastTrailPoint = Vector3.zero;
            afterimageTimer -= Time.deltaTime;
            if (afterimageTimer > 0f) return;
            afterimageTimer = 0.09f;
            SpawnAfterimage();
        }

        private void SpawnAfterimage()
        {
            if (afterimageSources == null)
            {
                var model = body.modelLocator ? body.modelLocator.modelTransform : null;
                if (!model) return;
                // The largest skinned pieces give the silhouette; baking all ~140 would be wasteful.
                afterimageSources = model.GetComponentsInChildren<SkinnedMeshRenderer>()
                    .Where(r => r.sharedMesh)
                    .OrderByDescending(r => r.sharedMesh.vertexCount)
                    .Take(10).ToArray();
            }
            var root = new GameObject("HS_Afterimage");
            var meshes = new List<Mesh>();
            foreach (var source in afterimageSources)
            {
                if (!source || !source.gameObject.activeInHierarchy) continue;
                var mesh = new Mesh();
                source.BakeMesh(mesh);
                meshes.Add(mesh);
                var part = new GameObject("part");
                part.transform.SetParent(root.transform, false);
                part.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                part.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = part.AddComponent<MeshRenderer>();
                mr.sharedMaterial = palette.Material(VfxAssets.Afterimage);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            root.AddComponent<AfterimageFade>().Init(meshes, 0.28f, palette.Outer);
        }

        private sealed class AfterimageFade : MonoBehaviour
        {
            private List<Mesh> meshes;
            private MeshRenderer[] renderers;
            private MaterialPropertyBlock block;
            private float duration;
            private float age;
            private Color tint;

            public void Init(List<Mesh> baked, float seconds, Color color)
            {
                meshes = baked;
                tint = color;
                duration = seconds;
                renderers = GetComponentsInChildren<MeshRenderer>();
                block = new MaterialPropertyBlock();
            }

            private void Update()
            {
                age += Time.deltaTime;
                float t = age / duration;
                if (t >= 1f) { Destroy(gameObject); return; }
                // 1.2: was 1.2x linear, which read as a solid flat magenta cut-out. Ghostlier now.
                float fade = (1f - t) * (1f - t);
                Color c = tint * (.65f * fade);
                c.a = .7f * fade;
                block.SetColor("_TintColor", c);
                foreach (var r in renderers) if (r) r.SetPropertyBlock(block);
            }

            private void OnDestroy()
            {
                if (meshes != null) foreach (var m in meshes) if (m) Destroy(m);
            }
        }
    }

    /// <summary>
    /// v0.9: the Conductor Mark is gone. Plugin.Update still calls Tick, so the type stays as a no-op
    /// until that call site is removed.
    /// </summary>
    public static class MarkReticles
    {
        public static void Tick(float deltaTime) { }
    }
}
