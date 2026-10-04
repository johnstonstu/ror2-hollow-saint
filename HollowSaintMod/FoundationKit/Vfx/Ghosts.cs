using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>
    /// Projectile ghosts (the visual half of a projectile, spawned on every machine).
    /// Built in code: a writhing arc orb for Arc Bolt, a white-hot lance for Conduit Spear.
    /// </summary>
    public static class Ghosts
    {
        public static GameObject ArcBolt { get; private set; }
        public static GameObject Spear { get; private set; }

        internal static void Build()
        {
            if (ArcBolt != null) return;
            VfxAssets.Load();
            ArcBolt = VfxAssets.NewPrefab("HollowSaintArcBoltGhost");
            ArcBolt.AddComponent<ProjectileGhostController>();
            ArcBolt.AddComponent<ArcOrbGhost>();
            AddTrail(ArcBolt, 0.10f, KitTuning.ArcBoltRadius * 0.6f, HsPalette.ArcCyan);
            AddLight(ArcBolt, HsPalette.ArcCyan, 2.5f, 5f);

            Spear = VfxAssets.NewPrefab("HollowSaintSpearGhost");
            Spear.AddComponent<ProjectileGhostController>();
            Spear.AddComponent<LanceGhost>();
            AddTrail(Spear, 0.4f, 0.3f, HsPalette.ArcCyan); // v0.9.1: longer, wider (the spear flies at 150 m/s)
            AddLight(Spear, HsPalette.WhiteHot, 3f, 6f);
        }

        private static void AddTrail(GameObject root, float time, float width, Color color)
        {
            var trail = root.AddComponent<TrailRenderer>();
            trail.sharedMaterial = VfxAssets.Trail;
            trail.time = time;
            trail.startWidth = width;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.1f;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.alignment = LineAlignment.View;
        }

        private static void AddLight(GameObject root, Color color, float intensity, float range)
        {
            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        /// <summary>Owner body of a projectile ghost (null until the projectile is known).</summary>
        internal static CharacterBody OwnerFor(GameObject ghost)
        {
            var controller = ghost.GetComponent<ProjectileGhostController>();
            var source = controller ? (controller.authorityTransform ? controller.authorityTransform : controller.predictionTransform) : null;
            var projectile = source ? source.GetComponent<ProjectileController>() : null;
            return projectile && projectile.owner ? projectile.owner.GetComponent<CharacterBody>() : null;
        }

        internal static SkinFxPalette PaletteFor(GameObject ghost)
        {
            var controller = ghost.GetComponent<ProjectileGhostController>();
            var source = controller ? (controller.authorityTransform ? controller.authorityTransform : controller.predictionTransform) : null;
            var projectile = source ? source.GetComponent<ProjectileController>() : null;
            return projectile && projectile.owner ? SkinFxPalette.ForBody(projectile.owner.GetComponent<CharacterBody>()) : null;
        }

        internal static void TintRoot(GameObject ghost, SkinFxPalette palette)
        {
            var trail = ghost.GetComponent<TrailRenderer>();
            if (trail)
            {
                trail.sharedMaterial = palette.Material(VfxAssets.Trail);
                trail.startColor = palette.Arc;
                var end = palette.Arc; end.a = 0f; trail.endColor = end;
                trail.Clear(); // no previous owner's pooled trail remains.
            }
            var light = ghost.GetComponent<Light>();
            if (light) light.color = palette.Arc;
        }
    }

    /// <summary>A crackling ball: a bright core and three short arcs that re-jag every
    /// frame around the travel point. Ghosts may be pooled, so setup is in OnEnable.</summary>
    public sealed class ArcOrbGhost : MonoBehaviour
    {
        private LightningLine[] arcs;
        private Vector3[] arcDirs;
        private float[] arcRoll;
        private ParticleSystem core;
        private LineRenderer[] contours;
        private SkinFxPalette palette;

        private void OnEnable()
        {
            palette = null;
            if (arcs == null)
            {
                arcs = new LightningLine[3];
                arcDirs = new Vector3[3];
                arcRoll = new float[3];
                for (int i = 0; i < arcs.Length; i++)
                {
                    var line = new GameObject("arc" + i).AddComponent<LightningLine>();
                    line.transform.SetParent(transform, false);
                    line.loop = true;
                    line.width = KitTuning.ArcBoltRadius * 0.55f;
                    line.branches = 0;
                    line.jag = 0.05f;
                    line.rejagInterval = 0.03f;
                    line.drawTime = 0f;
                    arcs[i] = line;
                    arcDirs[i] = Random.onUnitSphere;
                }
                float diameter = ArcBolt.ArcBoltReliabilityRules.CoreDiameter(KitTuning.ArcBoltRadius);
                core = VfxParticles.Loop(transform, VfxAssets.Flash, 60f, 0.08f, Vector2.zero, new Vector2(diameter, diameter), HsPalette.WhiteHot);
                var main = core.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                var shape = core.shape; shape.enabled = false; // core stays at the collision center
                contours = new LineRenderer[2];
                for (int i = 0; i < contours.Length; i++)
                {
                    var ring = new GameObject("collisionContour" + i).AddComponent<LineRenderer>();
                    ring.transform.SetParent(transform, false);
                    ring.useWorldSpace = false;
                    ring.loop = true;
                    ring.positionCount = 32;
                    ring.sharedMaterial = VfxAssets.ArcCore;
                    ring.widthMultiplier = ArcBolt.ArcBoltReliabilityRules.ContourWidth(KitTuning.ArcBoltRadius);
                    ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    float radius = ArcBolt.ArcBoltReliabilityRules.ContourRadius(KitTuning.ArcBoltRadius);
                    for (int j = 0; j < ring.positionCount; j++)
                    {
                        float angle = j * Mathf.PI * 2f / ring.positionCount;
                        ring.SetPosition(j, i == 0
                            ? new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius
                            : new Vector3(0f, Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                    }
                    contours[i] = ring;
                }
            }
            var trail = GetComponent<TrailRenderer>();
            if (trail) trail.Clear();
            core.Clear(true);
            UpdateArcs(true); // place the arcs before their first draw (never from the origin)
        }

        private void LateUpdate()
        {
            var next = Ghosts.PaletteFor(gameObject) ?? palette ?? SkinFxPalette.ForIndex(0);
            if (next != palette)
            {
                palette = next;
                Ghosts.TintRoot(gameObject, palette);
                foreach (var arc in arcs) if (arc) arc.SetPalette(palette);
                // The central white-hot flash stays distinct across all skins. Only
                // the contour, crackle and trail take the owner's arc color.
                foreach (var ring in contours)
                {
                    ring.sharedMaterial = VfxAssets.ArcCore;
                    ring.startColor = ring.endColor = palette.Arc;
                }
            }
            UpdateArcs(false);
        }

        // Anchors follow the orb every frame; the direction each arc reaches in is re-rolled
        // a few times per second so the ball crackles without strobing.
        private void UpdateArcs(bool force)
        {
            if (arcs == null) return;
            Vector3 p = transform.position;
            for (int i = 0; i < arcs.Length; i++)
            {
                if (!arcs[i]) continue;
                arcRoll[i] -= Time.deltaTime;
                if (force || arcRoll[i] <= 0f)
                {
                    arcRoll[i] = Random.Range(0.04f, 0.09f);
                    arcDirs[i] = Random.onUnitSphere;
                }
                arcs[i].start = p + arcDirs[i] * (KitTuning.ArcBoltRadius * 0.08f);
                arcs[i].end = p + arcDirs[i] * (KitTuning.ArcBoltRadius * 0.8f);
            }
        }
    }

    /// <summary>The spear: a tapered white-hot shaft with a cyan glow sheath and a small
    /// arc crawling along it.</summary>
    [DefaultExecutionOrder(172)] // after the held copy chooses visibility; before weapon current.
    public sealed class LanceGhost : MonoBehaviour
    {
        private LineRenderer shaft;
        private LineRenderer sheath;
        private LightningLine crawl;
        private Vector3 crawlDir;
        private float crawlRoll;
        private SkinFxPalette palette;
        private float flare;
        private float scale = 1f;
        private CharacterBody ownerBody;
        private GameObject fitted;
        private SpearVisual fittedFx;
        private bool sized;

        private static readonly System.Collections.Generic.List<LanceGhost> live = new System.Collections.Generic.List<LanceGhost>();

        /// <summary>Cosmetic: brief width/brightness flare on the lance ghost nearest
        /// <paramref name="point"/> (within <paramref name="maxDistance"/>).</summary>
        public static void FlareNear(Vector3 point, float maxDistance, float amount)
        {
            LanceGhost best = null;
            float bestD = maxDistance * maxDistance;
            for (int i = 0; i < live.Count; i++)
            {
                var g = live[i];
                if (!g) continue;
                Vector3 mid = g.transform.position - g.transform.forward * 0.7f;
                float d = (mid - point).sqrMagnitude;
                if (d <= bestD) { bestD = d; best = g; }
            }
            if (best) best.flare = Mathf.Max(best.flare, Mathf.Clamp01(amount));
        }

        // v0.9.1 ionized wake: one lightning line from the launch point that follows the spear and
        // lingers after impact, so a 150 m/s throw reads as a path instead of a few-frame flicker.
        private const float WakeLife = 0.4f;
        private LightningLine wake;
        private float wakeUntil;
        private bool wakeStarted;

        private void UpdateWake()
        {
            if (!wakeStarted)
            {
                wakeStarted = true;
                Vector3 p = transform.position, f = transform.forward;
                // The ghost has already travelled a frame or two: start the wake back at the thrower's
                // side of the flight line (projected from the owner's core), capped at 6 m.
                float back = ownerBody ? Mathf.Clamp(Vector3.Dot(p - ownerBody.corePosition, f), 0f, 6f) : 1.5f;
                wake = LightningLine.Spawn(p - f * back, p, WakeLife, 0.45f * scale, 1, 0.05f, 0.06f, palette);
                wake.drawTime = 0f;
                wakeUntil = Time.time + WakeLife;
                return;
            }
            // Only steer the line while it is ours: a pooled line is reused once its life ends.
            if (wake && Time.time < wakeUntil) { wake.end = transform.position; wake.width = 0.45f * scale; }
            else wake = null;
        }

        private void OnDisable()
        {
            live.Remove(this);
            flare = 0f;
            wake = null; // the line keeps its last end and fades on its own
            wakeStarted = false;
        }

        private void OnEnable()
        {
            if (!live.Contains(this)) live.Add(this);
            palette = null;
            scale = 1f;
            ownerBody = null;
            sized = false;
            if (!fitted && FoundationContent.SpearModel)
            {
                fitted = Instantiate(FoundationContent.SpearModel, transform, false);
                var tipMarker = SpearDischarge.SpearCarry.Find(fitted, "SpearTip");
                if (tipMarker) fitted.transform.localRotation = Quaternion.FromToRotation(tipMarker.localPosition.normalized, Vector3.forward);
                fittedFx = fitted.AddComponent<SpearVisual>();
                fittedFx.Powered = true;
            }
            if (shaft == null)
            {
                shaft = Make("shaft", VfxAssets.ArcCore, 0.14f);
                sheath = Make("sheath", VfxAssets.ArcGlow, 0.5f);
                crawl = new GameObject("crawl").AddComponent<LightningLine>();
                crawl.transform.SetParent(transform, false);
                crawl.loop = true;
                crawl.width = 0.45f;
                crawl.branches = 0;
                crawl.jag = 0.25f;
                crawl.drawTime = 0f;
                crawlDir = Random.onUnitSphere;
            }
            PlaceCrawl();
            var trail = GetComponent<TrailRenderer>();
            if (trail) trail.Clear();
        }

        private LineRenderer Make(string name, Material material, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = material;
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.View;
            lr.positionCount = 3;
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0f));
            lr.widthMultiplier = width;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return lr;
        }

        private void LateUpdate()
        {
            var next = Ghosts.PaletteFor(gameObject) ?? palette ?? SkinFxPalette.ForIndex(0);
            if (next != palette)
            {
                palette = next;
                Ghosts.TintRoot(gameObject, palette);
                shaft.sharedMaterial = palette.Material(VfxAssets.ArcCore);
                sheath.sharedMaterial = palette.Material(VfxAssets.ArcGlow);
                crawl.SetPalette(palette);
            }
            Vector3 p = transform.position;
            Vector3 f = transform.forward;
            // v0.9: size follows the charge of the throw that launched this ghost (0.6 to 1.3 of the
            // authored size), x1.5 from the crown. Latched once the owner is known, within a moment of
            // the release, so a later throw never resizes a spear already in flight.
            if (!ownerBody) ownerBody = Ghosts.OwnerFor(gameObject);
            if (fittedFx) fittedFx.Owner = ownerBody;
            if (!sized && ownerBody)
            {
                var sc = ownerBody.GetComponent<Stormspear.StormspearCharge>();
                if (sc)
                {
                    float c = Time.time - sc.LastReleaseTime < 1.5f ? sc.LastReleaseCharge : 0.5f;
                    scale = Mathf.Lerp(0.6f, 1.3f, Mathf.Clamp01(c)) * (sc.LastReleaseForm == Stormspear.SpearForm.Crown && Time.time - sc.LastReleaseTime < 1.5f ? 1.5f : 1f);
                    sized = true;
                }
            }
            UpdateWake();
            var trail = GetComponent<TrailRenderer>();
            if (trail) trail.widthMultiplier = Mathf.Max(0.5f, scale);
            if (fitted)
            {
                fitted.transform.localPosition = -Vector3.forward * (0.95f * scale);
                fitted.transform.localScale = Vector3.one * scale;
            }
            shaft.enabled = sheath.enabled = !fitted;
            Vector3 tail = p - f * (1.8f * scale);
            Vector3 mid = p - f * 0.35f;
            Vector3 tip = p + f * 0.35f;
            shaft.SetPosition(0, tail); shaft.SetPosition(1, mid); shaft.SetPosition(2, tip);
            if (flare > 0f) flare = Mathf.Max(0f, flare - Time.deltaTime * 4f);
            shaft.widthMultiplier = 0.14f * scale * (1f + 1.2f * flare);
            sheath.widthMultiplier = 0.5f * scale * (1f + 1.5f * flare);
            sheath.SetPosition(0, tail); sheath.SetPosition(1, mid); sheath.SetPosition(2, tip);
            crawlRoll -= Time.deltaTime;
            if (crawlRoll <= 0f)
            {
                crawlRoll = Random.Range(0.04f, 0.08f);
                crawlDir = Random.onUnitSphere;
            }
            PlaceCrawl();
        }

        private void PlaceCrawl()
        {
            if (crawl == null) return;
            Vector3 p = transform.position;
            Vector3 f = transform.forward;
            float u = Mathf.Repeat(Time.time * 3f, 1f);
            crawl.start = Vector3.Lerp(p - f * (1.8f * scale), p + f * 0.35f, u);
            crawl.end = crawl.start + crawlDir * 0.35f;
        }
    }
}
