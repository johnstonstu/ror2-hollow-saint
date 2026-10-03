using HollowSaint.FoundationKit.OpenCircuit;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>
    /// Open Circuit crown, one continuous chain on every machine:
    ///   chest core -> out the back between the shoulder blades -> up into the nearest point
    ///   of the live halo ring -> crackling bridges across the ring's real gaps plus two
    ///   short arcs running around the copper -> (pulse beats) tendrils leap from the ring
    ///   point nearest each target (KitFx CircuitArc).
    /// Driven by the replicated Open Circuit buff, with the feed lighting early on the cast's
    /// Unfold beat (OpenCircuitState runs on every machine). Ring arcs brighten with how far
    /// the ring has actually opened, so they unfold and fold with the animation.
    /// Replaces BodyFx's four world-axis crown lines (read as a square behind the back).
    /// Lines are reused for the whole window and ticked here, after HaloRing (150).
    /// </summary>
    [DefaultExecutionOrder(170)]
    [DisallowMultipleComponent]
    public sealed class CircuitCrownFx : MonoBehaviour
    {
        private const int FeedCore = 0, FeedSpine = 1, GapFirst = 2, CrawlFirst = 6, LineCount = 8;
        private const float CrawlSpan = 0.8f;       // radians of ring each running arc covers
        private const float CrawlSpeed = 3.2f;      // radians per second
        private const float GapOverlap = 6f * Mathf.Deg2Rad;
        private const float UnfoldLead = 1.4f;      // cast unfold -> buff is 0.7 s; allow lag

        private CharacterBody body;
        private HaloRing ring;
        private Transform core;
        private Transform chest;
        private SkinFxPalette palette;
        private LightningLine[] lines;
        private float feedWeight;
        private float ringWeight;
        private float unfoldUntil;
        private float kick;
        private float crawlPhase;

        private static readonly float[] BaseWidth = { 0.28f, 0.5f, 0.3f, 0.45f, 0.3f, 0.42f, 0.26f, 0.26f };
        private static readonly float[] Jag = { 0.2f, 0.16f, 0.35f, 0.18f, 0.35f, 0.2f, 0.22f, 0.22f };

        private void Start()
        {
            body = GetComponent<CharacterBody>();
            ring = HaloRing.For(body);
            VfxAssets.Load();
            palette = SkinFxPalette.ForBody(body);
            OpenCircuitVfxHooks.UnfoldStarted += OnUnfold;
        }

        private void OnDestroy()
        {
            OpenCircuitVfxHooks.UnfoldStarted -= OnUnfold;
            DestroyLines();
        }

        private void OnDisable()
        {
            DestroyLines();
            feedWeight = ringWeight = 0f;
            unfoldUntil = kick = 0f;
        }

        private void OnUnfold(CharacterBody who)
        {
            if (who == body) unfoldUntil = Time.time + UnfoldLead;
        }

        /// <summary>A pulse left the ring: flare the crown briefly (called by the CircuitArc beat).</summary>
        public void Kick()
        {
            kick = 1f;
        }

        private void LateUpdate()
        {
            if (!body || !ring) return;
            bool alive = body.healthComponent && body.healthComponent.alive;
            bool buff = alive && OpenCircuitBuff.Def != null && body.HasBuff(OpenCircuitBuff.Def);
            bool unfolding = alive && !buff && Time.time < unfoldUntil;
            if (buff) unfoldUntil = 0f;
            var model = body.modelLocator && body.modelLocator.modelTransform
                ? body.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
            bool visible = alive && ring.Valid && (!model || model.invisibilityCount <= 0);
            bool on = visible && (buff || unfolding);

            float dt = Time.deltaTime;
            // Feed: in over 0.25 s from the Unfold beat, out over 0.3 s on Recall.
            feedWeight = Mathf.MoveTowards(feedWeight, on ? 1f : 0f, dt / (on ? 0.25f : 0.3f));
            // Ring arcs: follow how far the ring has opened (faint while upright).
            float ringTarget = on ? Mathf.Clamp01(0.3f + ring.Openness) : 0f;
            ringWeight = Mathf.MoveTowards(ringWeight, ringTarget, dt / 0.25f);
            kick = Mathf.Max(0f, kick - dt * 6f);
            if (!visible || (feedWeight <= 0f && ringWeight <= 0f)) { DestroyLines(); return; }

            var current = SkinFxPalette.ForBody(body);
            if (!ReferenceEquals(current, palette))
            {
                palette = current;
                if (lines != null) foreach (var line in lines) if (line) line.SetPalette(palette);
            }
            if (lines == null) CreateLines();
            if (!core) core = KitUtil.ResolveSocket(body, "Core");
            if (!chest) chest = KitUtil.ResolveSocket(body, "Chest");

            var shape = ring.Shape;
            float unit = ring.Unit;
            float flare = (1f + 0.6f * kick) * LightningRhythm.Gain(Time.time, 0.7f * unit);

            // ---- Feed: core -> upper back -> ring dock ----
            Vector3 corePos = core ? core.position : body.corePosition;
            Vector3 back = shape.Center - corePos;
            back.y = 0f;
            if (back.sqrMagnitude < 1e-4f)
                back = body.characterDirection ? -body.characterDirection.forward : -((Component)body).transform.forward;
            back.Normalize();
            Vector3 chestPos = chest ? chest.position : corePos;
            Vector3 spine = chestPos + back * (0.17f * unit) + Vector3.up * (0.06f * unit);
            Vector3 dock = shape.Nearest(spine);
            Place(FeedCore, corePos, spine, feedWeight * flare);
            Place(FeedSpine, spine, dock, feedWeight * flare);

            // ---- Ring: bridges across the four real gaps ----
            for (int i = 0; i < 4; i++)
            {
                float a, b;
                shape.Gap(i, GapOverlap, out a, out b);
                Place(GapFirst + i, shape.PointAt(a), shape.PointAt(b), ringWeight * flare);
            }

            // ---- Ring: two short arcs running around the copper, opposite each other ----
            crawlPhase += dt * CrawlSpeed * (0.6f + 0.4f * ringWeight);
            for (int k = 0; k < 2; k++)
            {
                float a = crawlPhase + k * Mathf.PI;
                Place(CrawlFirst + k, shape.PointAt(a), shape.PointAt(a + shape.Direction * CrawlSpan), ringWeight * flare);
            }
        }

        private void Place(int index, Vector3 from, Vector3 to, float weight)
        {
            var line = lines[index];
            if (!line) return;
            line.start = from;
            line.end = to;
            line.width = BaseWidth[index] * Mathf.Clamp(weight, 0f, 2f);
            line.Tick(Time.deltaTime);
        }

        private void CreateLines()
        {
            lines = new LightningLine[LineCount];
            for (int i = 0; i < LineCount; i++)
            {
                var line = new GameObject("HS_Crown" + i).AddComponent<LightningLine>();
                line.SetPalette(palette);
                line.loop = true;
                line.manualTick = true;
                line.drawTime = 0f;
                line.width = 0f;
                line.branches = i == FeedSpine ? 1 : 0;
                line.jag = Jag[i];
                line.rejagInterval = i < GapFirst ? 0.05f : 0.06f;
                line.transform.SetParent(transform, false);
                lines[i] = line;
            }
        }

        private void DestroyLines()
        {
            if (lines == null) return;
            for (int i = 0; i < lines.Length; i++) if (lines[i]) { lines[i].gameObject.SetActive(false); Destroy(lines[i].gameObject); }
            lines = null;
        }
    }
}
