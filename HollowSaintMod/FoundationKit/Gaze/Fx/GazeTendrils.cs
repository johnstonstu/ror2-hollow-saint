using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    /// <summary>Channel-owned, reusable geometry. No searches for enemies or gameplay writes.</summary>
    public sealed class GazeTendrils : MonoBehaviour
    {
        private const int AmbientCount = 6, HitCount = 12, Points = 9;
        private const float Refresh = 0.1f, Lift = 0.16f;
        private readonly Stroke[] ambient = new Stroke[AmbientCount * 2];
        private readonly Stroke[] hits = new Stroke[HitCount];
        private GazeBeam beam;
        private GazeEmpowermentFx empowerment;
        private SkinFxPalette palette;
        private float refreshAt;
        private int nextHit;

        private sealed class Stroke
        {
            internal LineRenderer glow, core;
            internal readonly Vector3[] points = new Vector3[Points];
            internal int count;
            internal float until;
        }

        public void Begin(GazeBeam owner, SkinFxPalette skin)
        {
            beam = owner;
            palette = skin;
            for (int i = 0; i < ambient.Length; i++)
            {
                if (ambient[i] == null) ambient[i] = Create(false);
                Theme(ambient[i]);
            }
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null) hits[i] = Create(true);
                Theme(hits[i]);
            }
            Clear();
            nextHit = 0;
            refreshAt = 0f;
        }

        private bool Channeling => isActiveAndEnabled && beam && beam.isActiveAndEnabled && beam.Current == GazeBeam.Phase.Beam &&
            beam.Body && beam.Body.healthComponent && beam.Body.healthComponent.alive;

        public void Render(GazeServer.Impact impact)
        {
            if (!Channeling) { Clear(); return; }
            if (!empowerment) empowerment = GetComponent<GazeEmpowermentFx>();
            // Leave confirmed hit strokes intact. Only the decorative roots yield to the
            // accent wave, otherwise two similar footprints obscure its moving front.
            if (empowerment && empowerment.ReadabilityFocus > 0.2f) { HideAmbient(); return; }
            if (Time.time < refreshAt) return;
            refreshAt = Time.time + Refresh;
            float reach = Mathf.Lerp(GazeTuning.ReachStart, GazeTuning.ReachEnd,
                Mathf.Clamp01(beam.PhaseAge / Mathf.Max(0.1f, GazeTuning.BeamSeconds)));
            // Impact arcs stay inside the splash footprint (including their half-width).
            // With no ground at the endpoint, a small dim caster splash signifies channel energy.
            Vector3 center;
            bool atImpact = Ground(impact.Point, 1.5f, out center);
            float radius = Mathf.Min(6f, Mathf.Max(0f, GazeTuning.SplashRadius * reach -
                Vector3.Distance(center, impact.Point) - 0.7f));
            if (!atImpact)
            {
                if (!Ground(beam.Body.footPosition, 12f, out center)) { HideAmbient(); return; }
                radius = 1.5f;
            }
            if (radius < 0.25f) { HideAmbient(); return; }
            float spin = Time.time * 1.3f;
            for (int i = 0; i < AmbientCount; i++)
            {
                float angle = spin + i * Mathf.PI * 2f / AmbientCount;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var main = ambient[i * 2];
                GroundPath(main, center, center + direction * radius, center, radius);
                Show(main, atImpact ? 1.3f : 0.75f, atImpact ? 0.65f : 0.3f, false);
                var branch = ambient[i * 2 + 1];
                if (main.count < 4) { Hide(branch); continue; }
                Vector3 start = main.points[main.count / 2];
                Vector3 side = new Vector3(-direction.z, 0f, direction.x);
                GroundPath(branch, start, center + (direction * 0.7f + side * 0.55f) * radius, center, radius);
                Show(branch, atImpact ? 0.8f : 0.45f, atImpact ? 0.35f : 0.2f, false);
            }
        }

        /// <summary>Called only by a server-generated hit effect. Slots overwrite oldest first.</summary>
        public bool Confirm(Vector3 from, Vector3 target, bool groundRoute)
        {
            if (!Channeling || palette == null) return false;
            var stroke = hits[nextHit++ % HitCount];
            if (stroke == null) return false;
            Vector3 a, b;
            if (groundRoute && Ground(from, 1.5f, out a) && Ground(target, 4f, out b))
            {
                GroundPath(stroke, a, b, a, float.PositiveInfinity);
                // Only connect up from the target's feet if the route actually reached them.
                if (stroke.count > 1 && Vector3.Distance(stroke.points[stroke.count - 1], b) < 0.3f)
                    stroke.points[stroke.count - 1] = target;
                else AirPath(stroke, from, target);
            }
            else AirPath(stroke, from, target);
            stroke.until = Time.time + 0.22f;
            Show(stroke, 1.8f, 1f, true);
            return true;
        }

        private void Update()
        {
            if (!Channeling) { Clear(); return; }
            foreach (var stroke in hits)
                if (stroke != null && Time.time >= stroke.until) Hide(stroke);
        }

        private static void AirPath(Stroke stroke, Vector3 from, Vector3 to)
        {
            stroke.count = Points;
            Vector3 side = Vector3.Cross((to - from).normalized, Vector3.up).normalized;
            for (int i = 0; i < Points; i++)
            {
                float t = i / (float)(Points - 1);
                stroke.points[i] = Vector3.Lerp(from, to, t) + side * Mathf.Sin(t * Mathf.PI) *
                    (i % 2 == 0 ? 0.22f : -0.22f);
            }
        }

        private static void GroundPath(Stroke stroke, Vector3 from, Vector3 to, Vector3 center, float radius)
        {
            stroke.count = 0;
            Vector3 side = Vector3.Cross((to - from).normalized, Vector3.up).normalized;
            for (int i = 0; i < Points; i++)
            {
                float t = i / (float)(Points - 1);
                Vector3 sample = Vector3.Lerp(from, to, t) + side * Mathf.Sin(t * Mathf.PI) *
                    (i % 2 == 0 ? 0.16f : -0.16f);
                Vector3 offset = sample - center;
                offset.y = 0f;
                if (offset.magnitude > radius) sample -= offset - Vector3.ClampMagnitude(offset, radius);
                // Follow the previous ground height so gradual slopes do not exhaust the ray's reach.
                if (i > 0) sample.y = stroke.points[i - 1].y;
                Vector3 point;
                if (!Ground(sample, 0.8f, out point)) break;
                if ((point - center).sqrMagnitude > radius * radius) break;
                if (i > 0 && (Mathf.Abs(point.y - stroke.points[i - 1].y) > 0.7f ||
                    Physics.Linecast(stroke.points[i - 1], point, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))) break;
                stroke.points[stroke.count++] = point;
            }
        }

        private static bool Ground(Vector3 p, float down, out Vector3 point)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out hit, down + 0.5f,
                LayerIndex.world.mask, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.35f)
            {
                point = hit.point + hit.normal * Lift;
                return true;
            }
            point = p;
            return false;
        }

        private Stroke Create(bool confirmed)
        {
            var stroke = new Stroke { glow = MakeLine(confirmed ? "GazeHitGlow" : "GazeTendrilGlow") };
            stroke.core = MakeLine(confirmed ? "GazeHitCore" : "GazeEnergyCore");
            return stroke;
        }

        private LineRenderer MakeLine(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.15f);
            line.enabled = false;
            return line;
        }

        private void Theme(Stroke stroke)
        {
            stroke.glow.sharedMaterial = palette.Material(VfxAssets.ArcGlow);
            if (stroke.core) stroke.core.sharedMaterial = palette.Material(VfxAssets.ArcCore);
        }

        private void Show(Stroke stroke, float width, float alpha, bool confirmed)
        {
            Draw(stroke.glow, stroke, width, confirmed ? palette.Arc : palette.Outer, alpha);
            if (stroke.core) Draw(stroke.core, stroke, width * (confirmed ? 0.24f : 0.16f),
                confirmed ? palette.Core : palette.Arc, confirmed ? alpha : Mathf.Min(1f, alpha * 1.4f));
        }

        private static void Draw(LineRenderer line, Stroke stroke, float width, Color color, float alpha)
        {
            line.enabled = stroke.count > 1 && line.sharedMaterial;
            line.positionCount = stroke.count;
            for (int i = 0; i < stroke.count; i++) line.SetPosition(i, stroke.points[i]);
            line.widthMultiplier = width;
            color.a = alpha;
            line.startColor = color;
            color.a = stroke.core ? alpha : alpha * 0.2f;
            line.endColor = color;
        }

        private static void Hide(Stroke stroke)
        {
            if (stroke == null) return;
            if (stroke.glow) stroke.glow.enabled = false;
            if (stroke.core) stroke.core.enabled = false;
            stroke.until = 0f;
        }

        private void HideAmbient() { foreach (var stroke in ambient) Hide(stroke); }
        public void Clear() { HideAmbient(); foreach (var stroke in hits) Hide(stroke); }
        private void OnDisable() { Clear(); }
        // Geometry is parented to the body: destruction/stage teardown removes it with the owner.
    }
}
