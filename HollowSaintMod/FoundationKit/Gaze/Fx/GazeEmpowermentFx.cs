using System.Collections.Generic;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    /// <summary>
    /// Presentation only. Caller supplies authoritative counts, frozen paths and event ages.
    /// elapsed always means seconds since THIS event, not cast age. Sequence IDs are unique
    /// within each event kind/cast; every target strike needs its own sequence ID.
    /// Attach to the CharacterBody. No registration, networking, resource or damage writes.
    /// </summary>
    [DefaultExecutionOrder(170), DisallowMultipleComponent]
    public sealed partial class GazeEmpowermentFx : MonoBehaviour
    {
        public enum EndReason { Completed, Cancelled, Interrupted, Death, Despawn }
        private const int MaxCharges = 20, MaxEvents = 256;
        private readonly HashSet<int> swallows = new HashSet<int>();
        private readonly HashSet<int> launches = new HashSet<int>();
        private readonly HashSet<int> strikes = new HashSet<int>();
        private readonly Orb[] fuel = new Orb[MaxCharges], reserve = new Orb[MaxCharges];
        // .55 travel + .30 spread + .18 fade at .25s manual spacing needs five slots.
        private readonly Pulse[] pulses = new Pulse[5];
        // Three overlapping eight-victim arrival windows; contacts live .26 seconds.
        private readonly Strike[] contacts = new Strike[24];
        private CharacterBody body;
        private GazeBeam beam;
        private SkinFxPalette palette;
        private Color accent, accentEdge, outlineTint;
        private Transform root;
        private uint castId;
        private bool seenCast, active, ending, fullEntry, externalAnchors;
        private int capacity, reserveCount, mergedCount, nextPulse, nextStrike;
        private float started, ended, flashAt = -100f, nextSound;
        private Vector3 crown, direction = Vector3.forward, reserveCenter;
        private Stroke crownRim;
        // .32-second intake at .25-second admission spacing overlaps at most twice.
        private readonly Stroke[] intakeTrails = new Stroke[2], intakeOutlines = new Stroke[2];
        private readonly Vector3[][] trailPoints = { new Vector3[12], new Vector3[12] };
        public bool ReducedEffects { get; set; }
        public bool EnableAudio { get; set; } = true;
        /// <summary>Adapter suppresses the ordinary charge halo while this is true.</summary>
        public bool OwnsChargePresentation { get { return active || ending; } }
        /// <summary>Pose-owner hook, 0..1. Expand physical crown arcs radially about the live
        /// crown center after the base pose; never scale the character or stack transforms.
        /// Rises through the intake, peaks at launch, then returns within 0.22 seconds.
        /// This component deliberately does not write any crown transforms.</summary>
        public float CrownExpansion
        {
            get
            {
                if (!active || !body || !body.healthComponent || !body.healthComponent.alive) return 0f;
                float expansion = 0f;
                foreach (var orb in fuel)
                    if (orb != null && orb.visible && orb.swallowing)
                    {
                        float t = Mathf.Clamp01((Time.time - orb.at) / orb.duration);
                        expansion = Mathf.Max(expansion, Mathf.SmoothStep(0f, 1f, (t - 0.25f) / 0.75f));
                    }
                foreach (var p in pulses)
                    if (p != null && p.active)
                    {
                        float age = Time.time - p.at;
                        if (age >= 0f) expansion = Mathf.Max(expansion, 1f - Mathf.SmoothStep(0f, 1f, age / 0.22f));
                    }
                return expansion;
            }
        }
        public float CrownApertureScale { get { return 1f + 0.6f * CrownExpansion; } }
        /// <summary>Read-only presentation envelope; baseline FX duck their decorations.
        /// Calculated from event clocks so render order cannot add a frame of delay.</summary>
        public float ReadabilityFocus
        {
            get
            {
                if (!active) return 0f;
                float focus = 0f;
                foreach (var orb in fuel)
                    if (orb != null && orb.visible && orb.swallowing)
                    {
                        float age = Time.time - orb.at;
                        if (age >= 0f && age < orb.duration) focus = Mathf.Max(focus, Mathf.Clamp01(age / 0.055f));
                    }
                focus = Mathf.Max(focus, 1f - Mathf.Clamp01((Time.time - flashAt) / 0.18f));
                foreach (var p in pulses)
                    if (p != null && p.active)
                    {
                        float age = Time.time - p.at;
                        if (age >= 0f) focus = Mathf.Max(focus, age < p.travel ? 1f :
                            0.85f * (1f - Mathf.Clamp01((age - p.travel) / (p.spread + 0.18f))));
                    }
                foreach (var s in contacts)
                    if (s != null && s.active) focus = Mathf.Max(focus, 0.65f * (1f - Mathf.Clamp01((Time.time - s.at) / 0.26f)));
                return focus;
            }
        }

        private sealed class Orb
        {
            internal Stroke stroke, outline;
            internal bool visible, swallowing;
            internal int intakeSlot;
            internal float at, duration;
            internal Vector3 p0, p1, mergeFrom;
        }

        public void BeginCast(uint id, int entryCount, int max, bool enteredFull, float elapsed)
        {
            if (!Finite(elapsed) || (seenCast && unchecked((int)(id - castId)) <= 0)) return;
            Clear();
            seenCast = true; castId = id;
            EnsureBuilt();
            if (!root) return;
            palette = SkinFxPalette.ForBody(body);
            // Keep primary skin energy throughout; secondary colour is outline-only.
            accent = palette.Arc;
            outlineTint = Color.Lerp(GazeContrastAssets.Ink, GazeContrastAssets.Accent(palette.Index), 0.25f);
            accentEdge = palette.Arc;
            capacity = Mathf.Clamp(max, 2, MaxCharges);
            entryCount = Mathf.Clamp(entryCount, 0, capacity);
            fullEntry = enteredFull;
            started = Time.time - Mathf.Max(0f, elapsed);
            active = true; ending = false; root.gameObject.SetActive(true);
            swallows.Clear(); launches.Clear(); strikes.Clear();
            nextPulse = nextStrike = reserveCount = 0;
            ResolveAnchors();
            for (int i = 0; i < MaxCharges; i++)
            {
                fuel[i].visible = i < entryCount; fuel[i].swallowing = false;
                reserve[i].visible = false;
            }
        }

        /// <summary>Optional per-frame anchors, in world space. Back of crown is -beamDirection.</summary>
        public void SetAnchors(Vector3 crownCenter, Vector3 beamDirection, Vector3 reserveHaloCenter)
        {
            if (!Finite(crownCenter) || !Finite(beamDirection) || !Finite(reserveHaloCenter) || beamDirection.sqrMagnitude < 0.001f) return;
            externalAnchors = true; crown = crownCenter; direction = beamDirection.normalized; reserveCenter = reserveHaloCenter;
        }

        public void Swallow(uint id, int sequence, int orbIndex, float elapsed, float duration)
        {
            if (!Accept(id, sequence, elapsed, duration, swallows) || orbIndex < 0 || orbIndex >= capacity) return;
            var orb = fuel[orbIndex];
            if (!orb.visible || orb.swallowing) return;
            ResolveAnchors();
            Vector3 u, v; Basis(direction, out u, out v);
            float angle = OrbitAngle(orbIndex);
            orb.p0 = u * Mathf.Cos(angle) * 1.12f + v * Mathf.Sin(angle) * 1.12f - direction * 0.24f;
            orb.p1 = orb.p0 + (-u * Mathf.Sin(angle) + v * Mathf.Cos(angle)) * 0.34f - direction * 0.6f;
            orb.at = Time.time - Mathf.Max(0f, elapsed); orb.duration = Mathf.Max(0.08f, duration);
            // Network intake sequence is packetSequence * 20, so use the sequential
            // entry-orb index rather than its even-valued deduplication key.
            orb.intakeSlot = orbIndex % intakeTrails.Length;
            orb.swallowing = true;
            if (elapsed >= orb.duration) { orb.visible = false; orb.swallowing = false; }
        }

        public void SetReserve(uint id, int count)
        {
            if (!active || id != castId) return;
            reserveCount = Mathf.Clamp(count, 0, capacity);
            for (int i = 0; i < MaxCharges; i++) reserve[i].visible = i < reserveCount;
        }

        /// <summary>Counts are authoritative AFTER overflow/refund rules. Never emits a strike.</summary>
        public void EndCast(uint id, int unspentEntry, int retainedReserve, int retainedTotal, EndReason reason)
        {
            if (!active || id != castId) return;
            if (reason == EndReason.Death || reason == EndReason.Despawn) { Clear(); return; }
            ResolveAnchors();
            active = false; ending = true; ended = Time.time;
            mergedCount = Mathf.Clamp(retainedTotal, 0, capacity);
            int fromFuel = Mathf.Clamp(unspentEntry, 0, mergedCount);
            int fromReserve = Mathf.Clamp(retainedReserve, 0, mergedCount - fromFuel);
            // Preserve the positions of surviving entry orbs; never animate a consumed orb returning.
            int found = 0;
            for (int i = 0; i < capacity && found < fromFuel; i++)
                if (fuel[i].visible)
                {
                    var orb = fuel[i];
                    fuel[found++].mergeFrom = orb.swallowing ? IntakePosition(orb, Mathf.Clamp01((Time.time - orb.at) / orb.duration)) : FuelPosition(i);
                }
            for (int i = found; i < fromFuel; i++) fuel[i].mergeFrom = crown;
            for (int i = fromFuel; i < mergedCount; i++)
                fuel[i].mergeFrom = i - fromFuel < fromReserve ? ReservePosition(i - fromFuel) : crown;
            for (int i = 0; i < MaxCharges; i++) { fuel[i].visible = i < mergedCount; fuel[i].swallowing = false; reserve[i].visible = false; }
            HideTransient();
        }

        public void Clear()
        {
            active = ending = externalAnchors = false;
            flashAt = -100f;
            if (root) root.gameObject.SetActive(false);
            HideTransient();
        }

        private bool Accept(uint id, int sequence, float elapsed, float duration, HashSet<int> seen)
        {
            return active && id == castId && sequence >= 0 && Finite(elapsed) && Finite(duration) &&
                duration > 0f && seen.Count < MaxEvents && seen.Add(sequence);
        }

        private void LateUpdate()
        {
            if (!OwnsChargePresentation) return;
            if (!body || !body.healthComponent || !body.healthComponent.alive) { Clear(); return; }
            ResolveAnchors();
            var model = body.modelLocator && body.modelLocator.modelTransform ? body.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
            root.gameObject.SetActive(!model || model.invisibilityCount <= 0);
            RenderOrbs();
            if (ending)
            {
                if (Time.time - ended >= 0.4f) Clear();
                return;
            }
            RenderPulses(); RenderStrikes();
        }

        private void ResolveAnchors()
        {
            if (externalAnchors || !body) return;
            if (!beam) beam = GetComponent<GazeBeam>();
            var ring = HaloRing.For(body);
            if (ending && ring && ring.Valid) crown = ring.Shape.Center;
            else crown = beam ? beam.Origin : body.corePosition + Vector3.up * 0.75f + transform.forward * 1.25f;
            direction = beam ? beam.Direction : transform.forward;
            reserveCenter = body.corePosition + Vector3.up * 1.1f - transform.forward * 0.28f;
        }

        private float OrbitAngle(int i) { return 2f * Mathf.PI * i / Mathf.Max(2, capacity) + (Time.time - started) * 0.16f; }
        private Vector3 FuelPosition(int i)
        {
            // Match the ordinary halo's radius and global 9 deg/s orbit at hand-back.
            // Its plane smoothing may still need a brief adapter crossfade on the real rig.
            if (ending && body)
            {
                var ring = HaloRing.For(body);
                if (ring && ring.Valid)
                {
                    float returnAngle = 2f * Mathf.PI * i / Mathf.Max(2, capacity) - Time.time * Mathf.PI / 20f;
                    return ring.Shape.Center + (ring.Shape.Binormal * Mathf.Cos(returnAngle) + ring.Shape.Axis * Mathf.Sin(returnAngle)) * (0.78f * ring.RadiusScale);
                }
            }
            Vector3 u, v; Basis(direction, out u, out v);
            float a = OrbitAngle(i);
            return crown + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * 1.12f - direction * 0.24f;
        }
        private Vector3 ReservePosition(int i)
        {
            Vector3 right = body ? ((Component)body).transform.right : Vector3.right;
            float a = Mathf.PI * (0.12f + 0.76f * (i + 0.5f) / Mathf.Max(1, capacity));
            return reserveCenter + right * Mathf.Cos(a) * 1.3f + Vector3.up * Mathf.Sin(a) * 0.55f;
        }
        private Vector3 IntakePosition(Orb orb, float t)
        {
            Vector3 radial = Vector3.ProjectOnPlane(orb.p0, direction).normalized;
            return Bezier(crown + orb.p0, crown + orb.p1, crown - direction * 0.62f + radial * 0.12f, crown - direction * 0.08f, t * t);
        }
        private void RenderOrbs()
        {
            for (int slot = 0; slot < intakeTrails.Length; slot++) { intakeTrails[slot].Hide(); intakeOutlines[slot].Hide(); }
            for (int i = 0; i < MaxCharges; i++)
            {
                var orb = fuel[i];
                if (!orb.visible) { orb.stroke.Hide(); orb.outline.Hide(); }
                else
                {
                    Vector3 p = FuelPosition(i); float size = fullEntry ? 0.22f : 0.20f;
                    if (ending) p = Vector3.Lerp(orb.mergeFrom, p, Mathf.SmoothStep(0f, 1f, (Time.time - ended) / 0.4f));
                    else if (orb.swallowing)
                    {
                        float t = Mathf.Clamp01((Time.time - orb.at) / orb.duration);
                        if (t >= 1f) { orb.visible = orb.swallowing = false; flashAt = Time.time; orb.stroke.Hide(); orb.outline.Hide(); continue; }
                        p = IntakePosition(orb, t); size *= 1f - 0.45f * Mathf.Pow(t, 8f);
                        var points = trailPoints[orb.intakeSlot];
                        for (int j = 0; j < points.Length; j++) points[j] = IntakePosition(orb, Mathf.Max(0f, t - 0.25f + j * 0.25f / (points.Length - 1)));
                        intakeOutlines[orb.intakeSlot].Draw(points, points.Length, 0.18f, outlineTint, 0.8f);
                        intakeTrails[orb.intakeSlot].Draw(points, points.Length, 0.10f, accent, 0.85f);
                    }
                    // Preserve the round orb and its hue all the way into the aperture.
                    orb.outline.Loop(p, direction, size, 0.14f, outlineTint, 0.9f);
                    orb.stroke.Loop(p, direction, size, fullEntry ? 0.09f : 0.08f, accent, 0.95f);
                }
                if (reserve[i].visible && !ending)
                {
                    reserve[i].outline.Loop(ReservePosition(i), direction, 0.14f, 0.09f, outlineTint, 0.65f);
                    reserve[i].stroke.Loop(ReservePosition(i), direction, 0.14f, 0.05f, accent, 0.8f);
                }
                else { reserve[i].stroke.Hide(); reserve[i].outline.Hide(); }
            }
            float flash = 1f - Mathf.Clamp01((Time.time - flashAt) / (ReducedEffects ? 0.2f : 0.12f));
            if (flash > 0f && !ending) crownRim.Loop(crown - direction * 0.07f, direction, 0.38f + flash * 0.1f,
                0.05f + flash * 0.065f, ReducedEffects ? accent : accentEdge, flash * 0.85f);
            else crownRim.Hide();
        }

        private void OnDisable() { Clear(); }
        private void OnDestroy() { if (root) Destroy(root.gameObject); }
        private static bool Finite(float f) { return !float.IsNaN(f) && !float.IsInfinity(f); }
        private static bool Finite(Vector3 v) { return Finite(v.x) && Finite(v.y) && Finite(v.z); }
        private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        { float s = 1f - t; return s * s * s * a + 3f * s * s * t * b + 3f * s * t * t * c + t * t * t * d; }
        private static void Basis(Vector3 normal, out Vector3 u, out Vector3 v)
        { u = Vector3.Cross(normal, Mathf.Abs(normal.y) > 0.95f ? Vector3.forward : Vector3.up).normalized; v = Vector3.Cross(normal, u).normalized; }
    }
}
