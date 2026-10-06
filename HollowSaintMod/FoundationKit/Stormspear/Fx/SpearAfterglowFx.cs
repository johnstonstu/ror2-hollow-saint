using HollowSaint.FoundationKit.Gaze.Fx;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Stormspear.Fx
{
    /// <summary>Bounded scene-owned cosmetic pool. No colliders, damage, audio or event emission.</summary>
    internal sealed class SpearAfterglowFx : MonoBehaviour
    {
        private const int Slots = 4, Branches = 8, Points = 17;
        private const float Life = 0.65f;
        private static readonly SpearAfterglowFx[] pool = new SpearAfterglowFx[Slots];
        private readonly Vector3[][] paths = new Vector3[Branches][];
        private readonly Vector3[] shown = new Vector3[Points];
        private readonly int[] counts = new int[Branches];
        private readonly LineRenderer[] glow = new LineRenderer[Branches], core = new LineRenderer[Branches];
        private float started;
        private bool live;
        private SkinFxPalette palette;
        internal static bool Valid(Vector3 p) => !(float.IsNaN(p.x) || float.IsInfinity(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.y) || float.IsNaN(p.z) || float.IsInfinity(p.z));

        internal static bool Play(Vector3 origin, Vector3 normal, float radius, SkinFxPalette colors)
        {
            if (!Valid(origin) || !Valid(normal) || float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0.15f || normal.sqrMagnitude < 0.01f) return false;
            normal = normal.normalized;
            if (normal.y <= 0.35f) return false; // Wall/air impacts never invent a horizontal floor ring.
            int slot = -1;
            for (int i = 0; i < Slots; i++) if (!pool[i] || !pool[i].live) { slot = i; break; }
            if (slot < 0) return false;
            RaycastHit root;
            if (!Physics.Raycast(origin + normal * 0.20f, -normal, out root, 0.40f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) || root.normal.y <= 0.35f) return false;
            if (!pool[slot])
            {
                var fx = new GameObject("HS_SpearSurfaceAfterglow").AddComponent<SpearAfterglowFx>();
                fx.Build(); pool[slot] = fx;
            }
            pool[slot].Begin(origin, root.point, root.normal, radius, colors);
            return true;
        }
        private void Build()
        {
            GazeContrastAssets.Load();
            for (int i = 0; i < Branches; i++)
            {
                paths[i] = new Vector3[Points];
                glow[i] = Make("GroundEnergy", GazeContrastAssets.Glow);
                core[i] = Make("GroundFilament", GazeContrastAssets.Core);
            }
        }
        private LineRenderer Make(string name, Material material)
        {
            var obj = new GameObject(name); obj.transform.SetParent(transform, false);
            var line = obj.AddComponent<LineRenderer>();
            line.sharedMaterial = material; line.useWorldSpace = true; line.positionCount = Points;
            line.alignment = LineAlignment.View; line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
            line.enabled = false; return line;
        }
        private void Begin(Vector3 origin, Vector3 surface, Vector3 normal, float radius, SkinFxPalette colors)
        {
            started = Time.time; palette = colors; live = true;
            for (int i = 0; i < Branches; i++)
            {
                counts[i] = 0; glow[i].enabled = core[i].enabled = false;
                Vector3 previous = surface + normal * 0.09f, n = normal;
                if (Vector3.Distance(previous, origin) > radius - 0.06f) continue;
                paths[i][counts[i]++] = previous;
                float angle = i * Mathf.PI * 2f / Branches + 0.17f * Mathf.Sin(i * 2.3f);
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 side = new Vector3(-radial.z, 0f, radial.x);
                float step = Mathf.Min(0.45f, radius * (0.7f + 0.15f * Mathf.Sin(i * 3.1f)) / (Points - 1));
                for (int j = 1; j < Points; j++)
                {
                    Vector3 heading = Vector3.ProjectOnPlane(radial + side * (0.25f * Mathf.Sin(j * 2.2f + i)), n).normalized;
                    Vector3 sample = previous - n * 0.09f + heading * step;
                    RaycastHit hit;
                    if (!Physics.Raycast(sample + Vector3.up * 0.5f, Vector3.down, out hit, 1f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) || hit.normal.y <= 0.35f) break;
                    Vector3 point = hit.point + hit.normal * 0.09f;
                    if (Vector3.Distance(point, origin) > radius - 0.06f || Vector3.Distance(point, previous) > 0.65f ||
                        Physics.CheckCapsule(previous, point, 0.045f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) break;
                    paths[i][counts[i]++] = point; previous = point; n = hit.normal;
                }
            }
            Render();
        }
        private void Update() { if (live) Render(); }
        private void Render()
        {
            float age = Mathf.Max(0f, Time.time - started);
            if (age >= Life) { OnDisable(); return; }
            float fade = 1f - Mathf.SmoothStep(0f, 1f, (age - 0.08f) / (Life - 0.08f));
            float reveal = Mathf.Clamp01(age / 0.10f);
            for (int i = 0; i < Branches; i++)
            {
                int count = counts[i];
                if (count < 2 || reveal <= 0f) { glow[i].enabled = core[i].enabled = false; continue; }
                float head = reveal * (count - 1); int end = (int)head;
                int written = 0;
                for (int j = 0; j <= end; j++) shown[written++] = paths[i][j];
                if (end < count - 1) shown[written++] = Vector3.Lerp(paths[i][end], paths[i][end + 1], head - end);
                for (int j = written; j < Points; j++) shown[j] = shown[written - 1];
                Draw(glow[i], palette.Arc, 0.08f, fade * 0.55f);
                Draw(core[i], i % 3 == 0 ? palette.Secondary : palette.Core, 0.022f, fade * 0.85f);
            }
        }
        private void Draw(LineRenderer line, Color color, float width, float alpha)
        {
            line.enabled = true; line.widthMultiplier = width; line.SetPositions(shown);
            color.a = alpha; line.startColor = line.endColor = color;
        }
        private void OnDisable()
        {
            live = false;
            for (int i = 0; i < Branches; i++) if (glow[i]) { glow[i].enabled = false; core[i].enabled = false; }
        }
    }
}
