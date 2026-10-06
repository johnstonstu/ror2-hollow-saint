using System.Collections.Generic;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    /// <summary>
    /// Everything drawn for Gaze of the Hollow, on every machine, from GazeBeam's per-frame data.
    ///
    /// Wind-up: launch burst at the feet, the crown lifts off and its four gaps light one by one
    /// while sparks stream inward and a tether runs from the chest core to the crown.
    /// Beam: four layered lines (wide haze, Titan-laser body, crackling lightning sheath, white
    /// core) that flare at ignition and pulse, two bolts spiralling around the beam, two long
    /// branching arcs along it, short arcs snapping off its surface, a flare/sparks/light at the
    /// impact with expanding rings, and lightning forks racing across the ground from the impact.
    /// All colors follow the skin palette.
    /// </summary>
    public sealed class GazeBeamFx
    {
        private const int HelixCount = 2;
        // Confirmed launches gradually widen the settled beam beneath the 3.85m event sleeve.
        private float shownRamp;
        private const float HelixRadius = 0.25f;
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        private readonly GazeBeam owner;
        private SkinFxPalette palette = SkinFxPalette.ForIndex(0);
        private GameObject root;
        private LineRenderer haze, beamBody, sheath, core;
        private readonly LineRenderer[] helices = new LineRenderer[HelixCount];
        private readonly List<Material> ownedMaterials = new List<Material>();
        private LightningLine tether;
        private GazeTendrils tendrils;
        private GazeEmpowermentFx empowerment;
        private float focus;
        private AnimationCurve beamWidthCurve;
        private float charge;
        private readonly LightningLine[] arcs = new LightningLine[2];
        private Transform impact, muzzle;
        private Light impactLight, muzzleLight;
        private ParticleSystem impactFlash, impactSparks, muzzleFlash;
        private Transform coreSocket;

        private float ignitedAt = -10f, endedAt = -10f;
        private float snapTimer, ringTimer, streamTimer, helixJitterTimer;
        private int gapsLit;
        private bool humming, beamVisible = true;
        private Vector3[] linePoints = new Vector3[0];
        private Vector3[] helixPoints = new Vector3[0];
        private Vector2[] helixJitter = new Vector2[0];

        public GazeBeamFx(GazeBeam owner)
        {
            this.owner = owner;
        }

        private CharacterBody Body { get { return owner ? owner.Body : null; } }

        // ------------------------------------------------------------------ lifecycle

        public void Begin(SkinFxPalette skin)
        {
            GazeAssets.Load();
            palette = skin ?? SkinFxPalette.ForIndex(0);
            Build();
            if (!tendrils) tendrils = owner.GetComponent<GazeTendrils>() ?? owner.gameObject.AddComponent<GazeTendrils>();
            tendrils.Begin(owner, palette);
            gapsLit = 0;
            shownRamp = 0f;
            ignitedAt = endedAt = -10f;
            streamTimer = snapTimer = ringTimer = 0f;
            SetBeamVisible(false);
            var body = Body;
            if (!body) return;
            coreSocket = KitUtil.ResolveSocket(body, "Core");
            Vector3 feet = body.footPosition;
            VfxParticles.Burst(feet, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.2f, Vector2.zero, new Vector2(1.6f, 1.9f), palette.Arc);
            VfxParticles.Burst(feet, Quaternion.LookRotation(Vector3.down), palette.Material(VfxAssets.Spark), 24, 0.45f,
                new Vector2(6f, 14f), new Vector2(0.08f, 0.16f), palette.Core, stretch: 0.09f, spreadAngle: 60f);
            Vector3 ground = Ground(feet, 3f);
            VfxParticles.Ring(ground, Vector3.up, 0.4f, 4f, 0.4f, 0.12f, palette.Material(VfxAssets.Trail), palette);
            VfxParticles.Ring(ground, Vector3.up, 0.2f, 2.2f, 0.3f, 0.06f, palette.Material(VfxAssets.Trail), palette);
            for (int i = 0; i < 4; i++)
                LightningLine.Spawn(feet, ground + Random.insideUnitSphere * 1.8f, 0.2f, 0.7f, 1, 0.2f, palette: palette);
            VfxParticles.FlashLight(feet + Vector3.up, palette.Arc, 3f, 8f, 0.3f);
            Util.PlaySound(GazeSfx.Launch, body.gameObject);
            Util.PlaySound(GazeSfx.Charge, body.gameObject);
        }

        public void Ignite()
        {
            ignitedAt = Time.time;
            SetBeamVisible(true);
            var body = Body;
            if (!body) return;
            Vector3 origin = owner.Origin;
            Vector3 dir = owner.Direction;
            VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.25f, Vector2.zero, new Vector2(3.2f, 3.6f), palette.Core);
            VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.35f, Vector2.zero, new Vector2(2.2f, 2.4f), palette.Arc);
            VfxParticles.Burst(origin, Quaternion.LookRotation(dir), palette.Material(VfxAssets.Spark), 40, 0.5f,
                new Vector2(10f, 24f), new Vector2(0.08f, 0.18f), palette.Core, stretch: 0.09f, spreadAngle: 35f);
            VfxParticles.Ring(origin, dir, 0.5f, 4.5f, 0.35f, 0.14f, palette.Material(VfxAssets.Trail), palette);
            VfxParticles.Ring(origin + dir * 2f, dir, 0.3f, 3f, 0.3f, 0.08f, palette.Material(VfxAssets.Trail), palette);
            VfxParticles.FlashLight(origin, palette.Arc, 7f, 16f, 0.4f);
            if (ImpactFeelSettings.Enabled)
                ShakeEmitter.CreateSimpleShakeEmitter(origin, new Wave { amplitude = 1.4f, frequency = 16f, cycleOffset = 0f }, 0.4f, 30f, true);
            Util.PlaySound(GazeSfx.Ignite, body.gameObject);
            Util.PlaySound(GazeSfx.IgniteBlast, body.gameObject);
            Util.PlaySound(GazeSfx.HumStart, body.gameObject);
            Util.PlaySound(GazeSfx.CrackleStart, body.gameObject);
            humming = true;
        }

        public void End()
        {
            endedAt = Time.time;
            if (tendrils) tendrils.Clear();
            StopHum();
            var body = Body;
            if (!body || ignitedAt < 0f) return;
            Vector3 origin = owner.Origin;
            VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 22, 0.45f,
                new Vector2(3f, 9f), new Vector2(0.08f, 0.14f), palette.Arc, stretch: 0.08f);
            VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.18f, Vector2.zero, new Vector2(1.4f, 1.6f), palette.Arc);
            Util.PlaySound(GazeSfx.End, body.gameObject);
        }

        public void Stop()
        {
            if (tendrils) tendrils.Clear();
            StopHum();
            SetBeamVisible(false);
            SetLoopsVisible(false);
            if (tether) tether.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            if (tendrils) tendrils.Clear();
            StopHum();
            if (root) Object.Destroy(root);
            foreach (var material in ownedMaterials) if (material) Object.Destroy(material);
            ownedMaterials.Clear();
            root = null;
        }

        private void StopHum()
        {
            if (!humming) return;
            humming = false;
            var body = Body;
            if (!body) return;
            Util.PlaySound(GazeSfx.HumStop, body.gameObject);
            Util.PlaySound(GazeSfx.CrackleStop, body.gameObject);
        }

        // ------------------------------------------------------------------ per frame

        public void Render(GazeBeam.Phase phase, float age, Vector3 origin, Vector3 dir, GazeCrownMount mount, float dt)
        {
            if (!root) return;
            var body = Body;
            if (!body) return;

            if (!empowerment) empowerment = owner.GetComponent<GazeEmpowermentFx>();
            focus = empowerment ? empowerment.ReadabilityFocus : 0f;
            if (GazeReleaseTuning.Enabled) focus *= .45f;

            // Tether: chest core to the floating crown, while the crown is away.
            bool away = mount.Weight > 0.05f;
            if (tether)
            {
                tether.gameObject.SetActive(away);
                if (away)
                {
                    tether.start = coreSocket ? coreSocket.position : body.corePosition;
                    tether.end = origin;
                    tether.width = (0.10f + 0.08f * mount.Weight) * Mathf.Lerp(1f, 0.2f, focus);
                    tether.Tick(dt);
                }
            }

            bool firing = phase == GazeBeam.Phase.Beam || (phase == GazeBeam.Phase.Ending && Time.time - endedAt < 0.15f && ignitedAt > 0f);
            if (!firing)
            {
                SetBeamVisible(false);
                SetLoopsVisible(false);
                if (phase == GazeBeam.Phase.Windup) RenderWindup(age, origin, dir, mount, dt);
                return;
            }
            SetBeamVisible(true);
            SetLoopsVisible(focus < 0.15f);

            var hit = GazeServer.Trace(origin, dir);
            if (tendrils) tendrils.Render(hit);
            Vector3 end = hit.Point;
            float length = Vector3.Distance(origin, end);
            float sinceIgnite = Time.time - ignitedAt;
            float collapse = phase == GazeBeam.Phase.Ending ? 1f - Mathf.Clamp01((Time.time - endedAt) / 0.15f) : 1f;
            float pop = 1f + 0.2f * Mathf.Clamp01(1f - sinceIgnite / 0.18f);
            float grow = Mathf.Clamp01(sinceIgnite / 0.08f);
            float pulse = 1f + 0.07f * Mathf.Sin(Time.time * 38f) + 0.05f * (Mathf.PerlinNoise(Time.time * 9f, 0.3f) - 0.5f);
            float w = pop * grow * pulse * collapse;
            if (empowerment && phase == GazeBeam.Phase.Beam)
            {
                // Hold/release readout from behind: +12% width per loaded charge, a snap on
                // each load, and a hard tier-scaled flare on release.
                int loaded = empowerment.PreparedCharges;
                float flare = empowerment.SurgeFlare;
                w *= 1f + .12f * loaded + .22f * empowerment.LoadFlash + .30f * flare;
                charge = Mathf.Clamp01(loaded / 3f * .5f + .35f * empowerment.LoadFlash + .4f * flare);
            }
            else charge = 0f;
            shownRamp = GazeBeamWidthPolicy.Advance(shownRamp, GazeReleaseTuning.Enabled ? 2 : owner.RampSteps, dt);

            // Resource motion owns the accent. Duck the continuous decorative layers while
            // preserving the baseline damage beam and server-confirmed contacts.
            if (beamWidthCurve != null)
            {
                beamWidthCurve.MoveKey(0, new Keyframe(0f, 0.45f * (empowerment ? empowerment.CrownApertureScale : 1f)));
                // Unity copies AnimationCurve values into the renderer on assignment.
                if (haze) haze.widthCurve = beamWidthCurve;
                if (beamBody) beamBody.widthCurve = beamWidthCurve;
                if (sheath) sheath.widthCurve = beamWidthCurve;
                if (core) core.widthCurve = beamWidthCurve;
            }
            FocusTint(haze, palette.Outer, 0.14f, Mathf.Lerp(1f, 0.16f, focus));
            FocusTint(beamBody, palette.Arc, 0.72f, Mathf.Lerp(1f, 0.72f, focus));
            FocusTint(sheath, palette.Arc, Mathf.Lerp(0.5f, 0.85f, charge), Mathf.Lerp(1f, 0.24f, focus));
            FocusTint(core, Color.Lerp(palette.Core, Color.white, charge), 1f, Mathf.Lerp(1f, 0.38f, focus));
            foreach (var helix in helices) FocusTint(helix, palette.Core, 1f, Mathf.Lerp(1f, 0.12f, focus));
            FocusEmitter(impactFlash, palette.Arc, 5f, Mathf.Lerp(0.35f, 0.08f, focus));
            FocusEmitter(impactSparks, palette.Arc, 18f, Mathf.Lerp(0.65f, 0.2f, focus));
            FocusEmitter(muzzleFlash, palette.Arc, 8f, Mathf.Lerp(0.5f, 0.08f, focus));
            Line(haze, origin, end, GazeBeamWidthPolicy.Haze(shownRamp, w * Mathf.Lerp(1f, 0.7f, focus), GazeTuning.Radius), Time.time * -1.5f);
            Line(beamBody, origin, end, GazeBeamWidthPolicy.Body(shownRamp, w, GazeTuning.Radius), Time.time * -6f);
            Line(sheath, origin, end, GazeBeamWidthPolicy.Sheath(shownRamp, w * (0.85f + 0.3f * Mathf.PerlinNoise(Time.time * 25f, 1.7f)), GazeTuning.Radius), Time.time * -14f);
            Line(core, origin, end, GazeBeamWidthPolicy.Core(shownRamp, w, GazeTuning.Radius), 0f);
            Helices(origin, dir, length, w, dt);
            if (focus < 0.15f) Arcs(origin, end, w, dt);
            if (focus < 0.1f) Snaps(origin, dir, length, w, dt);
            Impact(hit, origin, dir, w * Mathf.Lerp(1f, 0.12f, focus), dt);
            Muzzle(origin, dir, w * Mathf.Lerp(1f, 0.12f, focus));
        }

        private static void FocusTint(LineRenderer line, Color color, float alpha, float gain)
        {
            if (!line) return;
            color *= gain; color.a = alpha;
            line.startColor = line.endColor = color;
        }

        private static void FocusEmitter(ParticleSystem particles, Color color, float rate, float gain)
        {
            if (!particles) return;
            var emission = particles.emission; emission.rateOverTimeMultiplier = rate * gain;
            var main = particles.main; color *= gain; color.a = 1f; main.startColor = color;
        }

        private void RenderWindup(float age, Vector3 origin, Vector3 dir, GazeCrownMount mount, float dt)
        {
            float windup = Mathf.Max(0.1f, GazeTuning.WindupSeconds);
            // The four gaps of the crown light one after another.
            int target = Mathf.Clamp(Mathf.FloorToInt((age / windup - 0.35f) / 0.15f) + 1, 0, 4);
            while (gapsLit < target)
            {
                Vector3 a = mount.ArcPoint(gapsLit), b = mount.ArcPoint((gapsLit + 1) % 4);
                VfxParticles.Burst(a, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.2f, Vector2.zero, new Vector2(0.5f, 0.6f), palette.Core);
                VfxParticles.Burst(a, Quaternion.identity, palette.Material(VfxAssets.Spark), 8, 0.25f, new Vector2(2f, 6f), new Vector2(0.05f, 0.1f), palette.Arc, stretch: 0.06f);
                LightningLine.Spawn(a, b, 0.3f, 0.5f, 0, 0.2f, palette: palette);
                VfxParticles.FlashLight(a, palette.Arc, 1.2f + 0.5f * gapsLit, 4f, 0.25f);
                gapsLit++;
            }
            // Power streams inward to the crown centre.
            streamTimer -= dt;
            float charge = Mathf.Clamp01(age / windup);
            while (streamTimer <= 0f)
            {
                streamTimer += Mathf.Lerp(0.045f, 0.016f, charge);
                Vector3 from = origin + Random.onUnitSphere * Random.Range(0.9f, 1.7f);
                LightningLine.Spawn(from, origin, 0.09f, 0.12f + 0.2f * charge, 0, 0.25f, palette: palette);
            }
            if (muzzleLight)
            {
                muzzle.position = origin;
                muzzleLight.enabled = true;
                muzzleLight.intensity = 3f * charge * charge;
                muzzleLight.range = 6f;
            }
        }

        // ------------------------------------------------------------------ layers

        private void Line(LineRenderer line, Vector3 from, Vector3 to, float width, float scroll)
        {
            if (!line) return;
            int count = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(from, to) / 4f) + 1, 2, 16);
            if (linePoints.Length != count) linePoints = new Vector3[count];
            for (int i = 0; i < count; i++) linePoints[i] = Vector3.Lerp(from, to, i / (float)(count - 1));
            line.positionCount = count;
            line.SetPositions(linePoints);
            line.widthMultiplier = width;
            var material = line.sharedMaterial;
            if (scroll != 0f && material && material.HasProperty(MainTex)) material.SetTextureOffset(MainTex, new Vector2(scroll, 0f));
        }

        private void Helices(Vector3 origin, Vector3 dir, float length, float w, float dt)
        {
            int count = Mathf.Clamp(Mathf.CeilToInt(length / 0.6f) + 1, 8, 90);
            if (helixPoints.Length != count) helixPoints = new Vector3[count];
            if (helixJitter.Length != count * HelixCount)
            {
                helixJitter = new Vector2[count * HelixCount];
                helixJitterTimer = 0f;
            }
            helixJitterTimer -= dt;
            if (helixJitterTimer <= 0f)
            {
                helixJitterTimer = 0.04f;
                for (int i = 0; i < helixJitter.Length; i++) helixJitter[i] = Random.insideUnitCircle * 0.14f;
            }
            Vector3 u = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 v = Vector3.Cross(dir, u);
            float t = Time.time;
            for (int h = 0; h < HelixCount; h++)
            {
                var line = helices[h];
                if (!line) continue;
                float phase = h * Mathf.PI + t * 9f;
                for (int i = 0; i < count; i++)
                {
                    float s = length * i / (count - 1);
                    // Tight at the crown, opening to full radius within a few metres.
                    float r = HelixRadius * w * Mathf.Clamp01(0.35f + s / 4f);
                    float a = phase + s * 1.9f;
                    Vector2 j = helixJitter[h * count + i];
                    helixPoints[i] = origin + dir * s + u * (Mathf.Cos(a) * r + j.x) + v * (Mathf.Sin(a) * r + j.y);
                }
                line.positionCount = count;
                line.SetPositions(helixPoints);
                line.widthMultiplier = 0.035f * Mathf.Max(0.2f, w);
            }
        }

        private void Arcs(Vector3 origin, Vector3 end, float w, float dt)
        {
            for (int i = 0; i < arcs.Length; i++)
            {
                var arc = arcs[i];
                if (!arc) continue;
                arc.start = origin;
                arc.end = end;
                arc.width = 0.16f * w;
                arc.Tick(dt);
            }
        }

        private void Snaps(Vector3 origin, Vector3 dir, float length, float w, float dt)
        {
            snapTimer -= dt;
            if (snapTimer > 0f || w < 0.2f) return;
            snapTimer = 0.16f;
            Vector3 u = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 v = Vector3.Cross(dir, u);
            for (int k = 0; k < 1; k++)
            {
                float s = Random.Range(0.08f, 1f) * length;
                float a = Random.value * Mathf.PI * 2f;
                Vector3 radial = u * Mathf.Cos(a) + v * Mathf.Sin(a);
                Vector3 from = origin + dir * s + radial * 0.2f;
                Vector3 to = from + radial * Random.Range(0.2f, 0.5f) + dir * Random.Range(-0.2f, 0.4f);
                LightningLine.Spawn(from, to, 0.08f, 0.1f, 0, 0.12f, palette: palette);
            }
        }

        private void Impact(GazeServer.Impact hit, Vector3 origin, Vector3 dir, float w, float dt)
        {
            if (!impact) return;
            Vector3 normal = hit.HitWorld ? hit.Normal : -dir;
            impact.position = hit.Point + normal * 0.15f;
            impact.rotation = Quaternion.LookRotation(normal);
            if (impactLight)
            {
                impactLight.enabled = true;
                impactLight.intensity = (1f + 0.3f * Mathf.PerlinNoise(Time.time * 20f, 4.1f)) * Mathf.Clamp01(w);
            }
            ringTimer -= dt;
            if (ringTimer <= 0f && w > 0.2f)
            {
                ringTimer = 0.7f;
                VfxParticles.Ring(hit.Point + normal * 0.08f, normal, 0.15f, 0.65f, 0.22f, 0.035f, palette.Material(VfxAssets.Trail), palette);
            }

        }

        private void Muzzle(Vector3 origin, Vector3 dir, float w)
        {
            if (!muzzle) return;
            muzzle.position = origin;
            muzzle.rotation = Quaternion.LookRotation(dir);
            if (muzzleLight)
            {
                muzzleLight.enabled = true;
                muzzleLight.range = 8f;
                muzzleLight.intensity = 1f * Mathf.Clamp01(w);
            }
        }

        // ------------------------------------------------------------------ construction

        private void Build()
        {
            if (root) { ApplyPalette(); return; }
            root = new GameObject("HS_GazeBeam");
            Object.DontDestroyOnLoad(root);
            haze = MakeLine("haze", GazeAssets.BeamHaze, LineTextureMode.Tile);
            beamBody = MakeLine("body", GazeAssets.BeamBody, LineTextureMode.Tile);
            sheath = MakeLine("sheath", VfxAssets.ArcGlow, LineTextureMode.Tile);
            core = MakeLine("core", VfxAssets.ArcCore, LineTextureMode.Stretch);
            for (int i = 0; i < HelixCount; i++) helices[i] = MakeLine("helix" + i, VfxAssets.ArcCore, LineTextureMode.Stretch);
            SetWidthCurves();

            tether = MakeLoop("tether", 1);
            for (int i = 0; i < arcs.Length; i++)
            {
                arcs[i] = MakeLoop("arc" + i, 3);
                arcs[i].jag = 0.035f;
                arcs[i].rejagInterval = 0.05f;
            }

            impact = new GameObject("impact").transform;
            impact.SetParent(root.transform, false);
            impactLight = impact.gameObject.AddComponent<Light>();
            impactLight.type = LightType.Point;
            impactLight.range = 12f;
            impactLight.shadows = LightShadows.None;
            muzzle = new GameObject("muzzle").transform;
            muzzle.SetParent(root.transform, false);
            muzzleLight = muzzle.gameObject.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.shadows = LightShadows.None;
            MakeEmitters();
            ApplyPalette();
        }

        private void MakeEmitters()
        {
            if (impactFlash) Object.Destroy(impactFlash.gameObject);
            if (impactSparks) Object.Destroy(impactSparks.gameObject);
            if (muzzleFlash) Object.Destroy(muzzleFlash.gameObject);
            impactFlash = VfxParticles.Loop(impact, palette.Material(VfxAssets.Flash), 5f, 0.1f, Vector2.zero, new Vector2(0.35f, 0.55f), palette.Arc);
            impactSparks = VfxParticles.Loop(impact, palette.Material(VfxAssets.Spark), 18f, 0.2f, new Vector2(3f, 7f), new Vector2(0.04f, 0.08f), palette.Arc, stretch: 0.05f, spreadAngle: 45f);
            // Small enough that the floating crown still frames the beam's root.
            muzzleFlash = VfxParticles.Loop(muzzle, palette.Material(VfxAssets.Flash), 8f, 0.1f, Vector2.zero, new Vector2(0.2f, 0.35f), palette.Arc);
            SetEmitting(impactFlash, beamVisible);
            SetEmitting(impactSparks, beamVisible);
            SetEmitting(muzzleFlash, beamVisible);
        }

        private LineRenderer MakeLine(string name, Material material, LineTextureMode mode)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.textureMode = mode;
            line.numCapVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = 2;
            line.widthMultiplier = 0f;
            line.enabled = false;
            line.sharedMaterial = OwnedMaterial(material);
            return line;
        }

        /// <summary>Per-beam copy of the skin material, so scrolling it never moves anything else.</summary>
        private Material OwnedMaterial(Material source)
        {
            var themed = palette.Material(source);
            if (!themed) return null;
            var owned = new Material(themed) { name = themed.name + "_Gaze" };
            ownedMaterials.Add(owned);
            return owned;
        }

        private LightningLine MakeLoop(string name, int branches)
        {
            var go = new GameObject("HS_Gaze_" + name);
            go.transform.SetParent(root.transform, false);
            var line = go.AddComponent<LightningLine>();
            line.loop = true;
            line.manualTick = true;
            line.drawTime = 0f;
            line.branches = branches;
            line.jag = 0.12f;
            line.SetPalette(palette);
            go.SetActive(false);
            return line;
        }

        private void SetWidthCurves()
        {
            // Pinched where it leaves the crown's aperture, full almost at once, slightly narrower at the impact.
            beamWidthCurve = new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.04f, 1f), new Keyframe(0.9f, 1f), new Keyframe(1f, 0.85f));
            foreach (var line in new[] { haze, beamBody, sheath, core }) if (line) line.widthCurve = beamWidthCurve;
            var helixCurve = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.1f, 1f), new Keyframe(1f, 0.6f));
            foreach (var line in helices) if (line) line.widthCurve = helixCurve;
        }

        private void ApplyPalette()
        {
            foreach (var material in ownedMaterials) if (material) Object.Destroy(material);
            ownedMaterials.Clear();
            Retheme(haze, GazeAssets.BeamHaze, palette.Outer, 0.55f);
            Retheme(beamBody, GazeAssets.BeamBody, palette.Arc, 0.8f);
            Retheme(sheath, VfxAssets.ArcGlow, palette.Arc, 0.9f);
            Retheme(core, VfxAssets.ArcCore, palette.Core, 1f);
            foreach (var helix in helices) Retheme(helix, VfxAssets.ArcCore, palette.Core, 1f);
            if (tether) tether.SetPalette(palette);
            foreach (var arc in arcs) if (arc) arc.SetPalette(palette);
            if (impactLight) impactLight.color = palette.Arc;
            if (muzzleLight) muzzleLight.color = palette.Arc;
            if (impact) MakeEmitters();
        }

        private void Retheme(LineRenderer line, Material source, Color color, float alpha)
        {
            if (!line) return;
            line.sharedMaterial = OwnedMaterial(source);
            color.a = alpha;
            line.startColor = color;
            line.endColor = color;
        }

        private void SetBeamVisible(bool visible)
        {
            if (visible == beamVisible) return;
            beamVisible = visible;
            foreach (var line in new[] { haze, beamBody, sheath, core }) if (line) line.enabled = visible;
            foreach (var line in helices) if (line) line.enabled = visible;
            if (!visible && muzzleLight && muzzle) muzzleLight.enabled = false;
            if (!visible && impactLight) impactLight.enabled = false;
            SetEmitting(impactFlash, visible);
            SetEmitting(impactSparks, visible);
            SetEmitting(muzzleFlash, visible);
        }

        private void SetLoopsVisible(bool visible)
        {
            foreach (var arc in arcs) if (arc && arc.gameObject.activeSelf != visible) arc.gameObject.SetActive(visible);
        }

        private static void SetEmitting(ParticleSystem system, bool on)
        {
            if (!system) return;
            var emission = system.emission;
            emission.enabled = on;
        }

        private static Vector3 Ground(Vector3 p, float reach)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out hit, reach, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.08f;
            return p;
        }
    }
}
