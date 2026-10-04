using System.Collections.Generic;
using UnityEngine;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>
    /// Procedural lightning between two points: a related coloured core over the skin's
    /// primary glow, with thin complementary forks. Per-point offsets persist and are
    /// blended toward new random values every rejagInterval, so the bolt writhes instead
    /// of strobing; the points are rebuilt from start/end every frame so the bolt never
    /// lags behind moving anchors. The ends can follow transforms.
    /// </summary>
    [DefaultExecutionOrder(195)] // after the pose passes (100-170) and the current (180) move the anchors
    public sealed class LightningLine : MonoBehaviour
    {
        public Vector3 start;
        public Vector3 end;
        public Transform startAnchor;
        public Transform endAnchor;
        public float lifetime = 0.2f;
        public float width = 1f;
        public float rootWidth = 1f;
        /// <summary>v0.9.1: white-hot core width relative to the glow. Below 1 the line reads as a
        /// cyan feed rather than a white bolt, so the one white-hot element (the spear) stands out.</summary>
        public float coreScale = 1f;
        public float jag = 0.12f;       // jaggedness as a fraction of length
        public int branches = 1;
        public float rejagInterval = 0.045f;
        public bool loop;               // stay alive until destroyed
        public float drawTime = 0.05f;  // crawl-on time for one-shot bolts (0 = instant, use 0 for loops)
        private SkinFxPalette palette = SkinFxPalette.ForIndex(0);

        private const float MaxAmplitude = 0.6f;
        private const float BlendToNew = 0.65f;

        private LineRenderer core;
        private LineRenderer glow;
        private readonly List<LineRenderer> forks = new List<LineRenderer>();
        private float age;
        private float rejagTimer;
        private bool shown;
        private Vector3[] points = new Vector3[0];
        private Vector2[] offsets = new Vector2[0];
        private Vector2[] targetOffsets = new Vector2[0];
        private Vector3[][] forkPoints;
        private int[] forkFrom;
        private Vector3[] forkDir;
        private float[] forkLen;
        private Vector3[] forkJitter; // 3 entries per fork (points 1..3)
        private float widthSeed;

        // ---- v0.8 pooling and budget: one-shot bolts are recycled instead of a new GameObject per
        // bolt, and past the budget new bolts are skipped so late-game proc storms stay readable.
        private const int ActiveBudget = 220, PoolCap = 256;
        private static readonly Dictionary<int, Stack<LightningLine>> pool = new Dictionary<int, Stack<LightningLine>>();
        private static int activeOneShots;
        private bool pooled, counted;

        public static LightningLine Spawn(Vector3 from, Vector3 to, float lifetime, float width, int branches = 1, float jag = 0.12f, float rejagInterval = 0.045f, SkinFxPalette palette = null)
        {
            LightningLine line = null;
            Stack<LightningLine> stack;
            if (pool.TryGetValue(branches, out stack))
                while (stack.Count > 0 && !line) line = stack.Pop();
            if (line) { line.gameObject.SetActive(true); line.ResetForReuse(); }
            else
            {
                var go = new GameObject("HS_Lightning");
                line = go.AddComponent<LightningLine>();
            }
            line.pooled = true;
            if (activeOneShots >= ActiveBudget) { lifetime = 0.001f; width = 0f; }
            if (!line.counted) { line.counted = true; activeOneShots++; }
            line.start = from;
            line.end = to;
            line.lifetime = lifetime;
            line.width = width;
            line.branches = branches;
            line.jag = jag;
            line.rejagInterval = rejagInterval;
            line.SetPalette(palette);
            return line;
        }

        public void SetPalette(SkinFxPalette value)
        {
            var next = value ?? SkinFxPalette.ForIndex(0);
            if (next == palette) return;
            palette = next;
            if (core)
            {
                core.sharedMaterial = palette.Material(VfxAssets.ArcCore);
                core.startColor = core.endColor = palette.Core;
            }
            if (glow) glow.sharedMaterial = palette.Material(VfxAssets.ArcGlow);
            foreach (var fork in forks) if (fork) fork.sharedMaterial = palette.SecondaryMaterial(VfxAssets.ArcCore);
        }

        private void Start()
        {
            widthSeed = GetInstanceID() * 0.13f;
            glow = MakeRenderer("glow", VfxAssets.ArcGlow);
            core = MakeRenderer("core", VfxAssets.ArcCore);
            forkPoints = new Vector3[branches][];
            forkFrom = new int[branches];
            forkDir = new Vector3[branches];
            forkLen = new float[branches];
            forkJitter = new Vector3[branches * 3];
            for (int i = 0; i < branches; i++)
            {
                forks.Add(MakeRenderer("fork" + i, VfxAssets.ArcCore));
                forks[i].sharedMaterial = palette.SecondaryMaterial(VfxAssets.ArcCore);
                forks[i].startColor = forks[i].endColor = Color.white;
                forkPoints[i] = new Vector3[4];
            }
            Rebuild(true, 1f);
            ApplyWidth();
        }

        private LineRenderer MakeRenderer(string name, Material material)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var lr = child.AddComponent<LineRenderer>();
            lr.sharedMaterial = palette.Material(material);
            if (material == VfxAssets.ArcCore) lr.startColor = lr.endColor = palette.Core;
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.View;
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCapVertices = 1;
            lr.numCornerVertices = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.enabled = false; // shown once a valid (non-degenerate) build exists
            return lr;
        }

        /// <summary>When set, the owner moves the ends after its own late pose work and calls
        /// Tick itself, so a looping line never trails its anchors by a frame.</summary>
        public bool manualTick;
        private void OnEnable()
        {
            if (loop) age = 0f;
            rejagTimer = 0f;
        }
        private void OnDisable() { HideRenderers(); }

        private void OnDestroy() { if (counted) { counted = false; activeOneShots--; } }

        private void ResetForReuse()
        {
            age = 0f; rejagTimer = 0f; shown = false;
            startAnchor = endAnchor = null; loop = false; manualTick = false;
            drawTime = 0.05f; rootWidth = 1f; coreScale = 1f;
            HideRenderers();
        }

        private void Release()
        {
            if (counted) { counted = false; activeOneShots--; }
            if (!pooled || loop) { Destroy(gameObject); return; }
            Stack<LightningLine> stack;
            if (!pool.TryGetValue(branches, out stack)) pool[branches] = stack = new Stack<LightningLine>();
            if (stack.Count >= PoolCap) { Destroy(gameObject); return; }
            transform.SetParent(null, false);
            DontDestroyOnLoad(gameObject);
            gameObject.SetActive(false);
            stack.Push(this);
        }
        private void HideRenderers()
        {
            shown = false;
            if (core) core.enabled = false;
            if (glow) glow.enabled = false;
            foreach (var fork in forks) if (fork) fork.enabled = false;
        }

        private void LateUpdate()
        {
            if (!manualTick) Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (core == null) return; // Start has not run yet
            age += deltaTime;
            if (!loop && age >= lifetime * 1.4f)
            {
                Release();
                return;
            }
            if (startAnchor) start = startAnchor.position;
            if (endAnchor) end = endAnchor.position;
            rejagTimer -= deltaTime;
            bool rejag = rejagTimer <= 0f;
            if (rejag) rejagTimer = Mathf.Max(0.005f, rejagInterval);
            Rebuild(rejag, deltaTime);
            ApplyWidth();
        }

        private void ApplyWidth()
        {
            if (!shown) return;
            // Hard onset pop, fast-fading white core, slower-fading cyan afterglow.
            float pop = 1f + 0.8f * Mathf.Clamp01(1f - age / 0.04f);
            float coreFade = 1f;
            float glowFade = 1f;
            if (!loop)
            {
                float life = Mathf.Max(0.01f, lifetime);
                float tc = Mathf.Clamp01(age / life);
                float k = 1f - Mathf.Clamp01(tc / 0.45f);
                coreFade = k * k;
                float tg = Mathf.Clamp01(age / (life * 1.4f));
                glowFade = 1f - tg * tg;
            }
            float flicker = 0.75f + 0.25f * Mathf.PerlinNoise(age * 40f, widthSeed);
            float w = width * flicker * pop;
            SetWidth(core, 0.07f * w * coreFade * Mathf.Clamp01(coreScale));
            SetWidth(glow, 0.26f * w * glowFade);
            for (int i = 0; i < forks.Count; i++) SetWidth(forks[i], 0.035f * w * coreFade * Mathf.Lerp(0.4f, 1f, Mathf.Clamp01(coreScale)));
            if (width <= 0.001f) HideRenderers();
        }

        private void SetWidth(LineRenderer lr, float w)
        {
            if (!lr) return;
            lr.startWidth = w * Mathf.Clamp01(rootWidth);
            lr.endWidth = w * 0.7f;
        }

        /// <summary>Rebuilds the bolt from start/end plus the persistent offsets. When
        /// reroll is set the offsets (and fork shapes) first blend toward new randoms.</summary>
        private void Rebuild(bool reroll, float deltaTime)
        {
            Vector3 delta = end - start;
            float length = delta.magnitude;
            if (length < 0.001f || float.IsNaN(length) || float.IsInfinity(length))
            { HideRenderers(); return; }
            int segments = Mathf.Clamp(Mathf.CeilToInt(length / 0.5f), 4, 20);
            // Keep subdivision stable as moving bones cross a half-metre boundary.
            if (points.Length > 0 && Mathf.Abs(segments - (points.Length - 1)) <= 1) segments = points.Length - 1;
            if (points.Length != segments + 1)
            {
                points = new Vector3[segments + 1];
                offsets = new Vector2[segments + 1];
                targetOffsets = new Vector2[segments + 1];
                for (int i = 0; i < offsets.Length; i++) offsets[i] = targetOffsets[i] = Random.insideUnitCircle;
                reroll = true; // forks must pick anchors inside the new point range
            }
            else if (reroll)
            {
                for (int i = 0; i < offsets.Length; i++)
                    targetOffsets[i] = Vector2.Lerp(targetOffsets[i], Random.insideUnitCircle, BlendToNew);
            }
            float follow = 1f - Mathf.Exp(-60f * Mathf.Max(0f, deltaTime));
            for (int i = 0; i < offsets.Length; i++) offsets[i] = Vector2.Lerp(offsets[i], targetOffsets[i], follow);

            Vector3 dir = delta / length;
            Vector3 side = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 up = Vector3.Cross(side, dir);
            float amp = Mathf.Min(length * jag, MaxAmplitude);
            for (int i = 0; i <= segments; i++)
            {
                float u = i / (float)segments;
                float envelope = Mathf.Sin(u * Mathf.PI); // pinned at both ends
                Vector2 r = offsets[i] * (amp * envelope);
                points[i] = start + delta * u + side * r.x + up * r.y;
            }

            // Draw-on crawl: the visible point count grows from 2 to full over drawTime.
            int count = points.Length;
            if (!loop && drawTime > 0.0001f && age < drawTime)
                count = Mathf.Clamp(Mathf.CeilToInt(Mathf.Lerp(2f, points.Length, age / drawTime)), 2, points.Length);
            Set(core, count);
            Set(glow, count);

            for (int f = 0; f < forks.Count; f++)
            {
                if (reroll)
                {
                    forkFrom[f] = Random.Range(1, Mathf.Max(2, segments - 1));
                    forkDir[f] = (dir + Random.insideUnitSphere * 0.9f).normalized;
                    forkLen[f] = length * Random.Range(0.12f, 0.3f);
                    for (int j = 0; j < 3; j++) forkJitter[f * 3 + j] = Random.insideUnitSphere * 0.2f;
                }
                // Only show a fork once the main bolt has crawled past its root.
                if (forkFrom[f] >= count)
                {
                    if (forks[f]) forks[f].enabled = false;
                    continue;
                }
                Vector3 origin = points[forkFrom[f]];
                Vector3[] fp = forkPoints[f];
                fp[0] = origin;
                for (int i = 1; i < 4; i++)
                    fp[i] = origin + forkDir[f] * (forkLen[f] * (i / 3f)) + forkJitter[f * 3 + (i - 1)] * forkLen[f];
                Set(forks[f], fp, fp.Length);
                if (forks[f]) forks[f].enabled = true;
            }
            if (!shown)
            {
                shown = true;
                if (core) core.enabled = true;
                if (glow) glow.enabled = true;
            }
        }

        private void Set(LineRenderer lr, int count)
        {
            Set(lr, points, count);
        }

        private static void Set(LineRenderer lr, Vector3[] p, int count)
        {
            if (!lr) return;
            if (count >= p.Length)
            {
                if (lr.positionCount != p.Length) lr.positionCount = p.Length;
                lr.SetPositions(p);
            }
            else
            {
                lr.positionCount = count;
                for (int i = 0; i < count; i++) lr.SetPosition(i, p[i]);
            }
        }
    }
}
