using HollowSaint.FoundationKit.Gaze;
using HollowSaint.FoundationKit.Gaze.Fx;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.OpenCircuit.Fx
{
    /// <summary>Historic component name retained for prefab registration. The dome is gone:
    /// the real crown expands to the damage perimeter, with sparse open lightning sweeps.
    /// Runs after Gaze's pose owner (140), before HaloRing refits (150).</summary>
    [DefaultExecutionOrder(145), DisallowMultipleComponent]
    public sealed partial class OpenCircuitDomeFx : MonoBehaviour
    {
        private const int Points = 17;
        private static readonly string[] ArcNames = { "halo 1", "halo 2", "halo 3", "halo 4" };
        private readonly Transform[] arcs = new Transform[4];
        private readonly OpenCircuitCrownPose pose = new OpenCircuitCrownPose();
        private readonly Stroke[] strokes = new Stroke[6];
        private CharacterBody body;
        private HaloRing ring;
        private OpenCircuitPulseDriver driver;
        private GazeBeam gaze;
        private Transform model;
        private GameObject visualRoot;
        private SkinFxPalette palette;
        private float expansion, nextResolve;
        public float Expansion { get { return pose.Weight; } }
        public bool OwnsPerimeter { get { return pose.Weight > 0.02f; } }
        private sealed class Stroke
        {
            internal LineRenderer glow, core;
            internal readonly Vector3[] points = new Vector3[Points];
        }
        private void Awake() { body = GetComponent<CharacterBody>(); ring = HaloRing.For(body); }
        private void Update()
        {
            bool wasExpanded = pose.Weight > 0f;
            pose.Restore(); // Before Animator; never accumulate overrides.
            // Gaze captures its centre before HaloRing's LateUpdate; discard the expanded cache.
            if (wasExpanded && ring) ring.EnsureFitted(true);
        }
        private void OnDisable() { Release(); DestroyVisuals(); }
        private void OnDestroy() { Release(); DestroyVisuals(); }
        private void Release() { pose.Release(); expansion = 0f; ResetEnvironment(); SetVisible(false); }
        private bool Resolve()
        {
            var current = body && body.modelLocator ? body.modelLocator.modelTransform : null;
            if (current == model && arcs[0] && arcs[1] && arcs[2] && arcs[3]) return true;
            if (current == model && Time.unscaledTime < nextResolve) return false;
            pose.Release(); expansion = 0f; ResetEnvironment(); model = current; nextResolve = Time.unscaledTime + 1f;
            for (int i = 0; i < arcs.Length; i++) arcs[i] = null;
            if (!model) { pose.Bind(arcs); return false; }
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
                for (int i = 0; i < arcs.Length; i++) if (t.name == ArcNames[i]) arcs[i] = t;
            pose.Bind(arcs);
            return pose.Fit();
        }
        private void LateUpdate()
        {
            if (NetworkServer.active && !NetworkClient.active) { Release(); return; }
            if (!body || !body.healthComponent || !body.healthComponent.alive) { Release(); return; }
            if (!gaze) gaze = GetComponent<GazeBeam>();
            // Gaze owns these bones throughout its windup, beam and return.
            if (gaze && gaze.Current != GazeBeam.Phase.Idle) { Release(); return; }
            if (!Resolve() || !pose.Fit()) { Release(); return; }
            var characterModel = model.GetComponent<CharacterModel>();
            if (characterModel && characterModel.invisibilityCount > 0) { Release(); return; }
            float radius = KitTuning.OpenCircuitRadius;
            if (!OpenCircuitDomeGeometry.IsValidRadius(radius)) { Release(); return; }
            if (!driver) driver = GetComponent<OpenCircuitPulseDriver>();
            bool open = (OpenCircuitBuff.Def && body.HasBuff(OpenCircuitBuff.Def)) || (driver && driver.CrownOpen);
            // Authored back-to-overhead unfold leads; expand only once the ring lies flat.
            float flat = Mathf.Abs(Vector3.Dot(pose.Shape.Normal, Vector3.up));
            float target = open ? Mathf.SmoothStep(0f, 1f, (flat - 0.45f) / 0.35f) : 0f;
            expansion = Mathf.MoveTowards(expansion, target, Time.deltaTime / (open ? 0.65f : 0.3f));
            if (!pose.Apply(body.corePosition, radius, expansion)) { Release(); return; }
            if (expansion <= 0.02f) { ResetEnvironment(); SetVisible(false); return; }
            if (!visualRoot) Build();
            palette = SkinFxPalette.ForBody(body);
            visualRoot.SetActive(true);
            worldChecks = 0;
            RefreshTerrain(radius);
            RenderStrikes();
            RenderPerimeter(radius);
        }
        private void Build()
        {
            GazeContrastAssets.Load();
            visualRoot = new GameObject("HS_OpenCircuitCrownPerimeter");
            visualRoot.transform.SetParent(transform, false);
            for (int i = 0; i < strokes.Length; i++)
            {
                var stroke = new Stroke(); strokes[i] = stroke;
                stroke.glow = MakeLine("CrownLightningGlow", GazeContrastAssets.Glow);
                stroke.core = MakeLine("CrownLightningCore", GazeContrastAssets.Core);
            }
            BuildStrikes();
        }
        private LineRenderer MakeLine(string name, Material material)
        {
            var go = new GameObject(name); go.transform.SetParent(visualRoot.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material; line.useWorldSpace = true; line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch; line.positionCount = Points;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.25f), new Keyframe(0.2f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0.25f));
            line.enabled = false; return line;
        }
        private void RenderPerimeter(float radius)
        {
            var shape = pose.Shape;
            float clock = Time.time * 3.2f;
            int hop = (int)clock;
            float progress = clock - hop;
            for (int i = 0; i < strokes.Length; i++)
            {
                var stroke = strokes[i];
                bool upward = i >= 4;
                float flash = upward ? Mathf.Max(0f, Mathf.Sin(Time.time * 4.7f + i * 2.6f) - 0.65f) / 0.35f :
                    i == (hop & 3) ? 1f : i == ((hop + 3) & 3) ? (1f - progress) * 0.2f : 0f;
                if (flash <= 0.001f) { stroke.glow.enabled = stroke.core.enabled = false; continue; }
                float start = shape.Angle[i & 3];
                float travel = i == (hop & 3) ? progress : 1f;
                if (!upward && !GroundBranch(stroke, i, radius, travel)) { stroke.glow.enabled = stroke.core.enabled = false; continue; }
                for (int j = 0; upward && j < Points; j++)
                {
                    float t = j / (float)(Points - 1);
                    float sweep = t * 0.14f;
                    float angle = start + shape.Direction * sweep;
                    float rise = Mathf.Min(2.1f, radius * 0.26f) * t;
                    float r = Mathf.Max(0f, shape.Radius - 0.07f);
                    float radial = Mathf.Sqrt(Mathf.Max(0f, r * r - rise * rise));
                    float teeth = Mathf.Sin(t * Mathf.PI) * (0.025f + 0.025f * Mathf.Sin(j * 2.7f + (int)(Time.time * 16f) + i));
                    Vector3 point = shape.Center + shape.Direction3(angle) * Mathf.Max(0f, radial - teeth) + Vector3.up * rise;

                    stroke.points[j] = body.corePosition + Vector3.ClampMagnitude(point - body.corePosition, Mathf.Max(0f, radius - 0.07f));
                }
                // Suppress the complete stroke at walls/ceilings; never draw a false connection.
                if (!ClearPath(stroke.points, 0.065f)) { stroke.glow.enabled = stroke.core.enabled = false; continue; }
                Draw(stroke.glow, stroke.points, upward ? 0.075f : 0.12f, palette.Arc, flash * expansion * 0.48f);
                Draw(stroke.core, stroke.points, upward ? 0.025f : 0.04f, upward ? palette.Secondary : palette.Core, flash * expansion * 0.85f);
            }
        }
        private static void Draw(LineRenderer line, Vector3[] points, float width, Color color, float alpha)
        {
            line.enabled = true; line.SetPositions(points); line.widthMultiplier = width;
            color.a = alpha; line.startColor = line.endColor = color;
        }
        private void SetVisible(bool visible) { if (visualRoot) visualRoot.SetActive(visible); }
        private void DestroyVisuals()
        {
            if (visualRoot) { visualRoot.SetActive(false); Destroy(visualRoot); }
            visualRoot = null;
            for (int i = 0; i < strokes.Length; i++) strokes[i] = null;
            DestroyStrikes();
        }
    }
}
