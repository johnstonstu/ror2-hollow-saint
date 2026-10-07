using HollowSaint.FoundationKit.Vfx;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    public sealed partial class GazeEmpowermentFx
    {
        private bool crownDriven;
        private float chargeFrom, chargeChanged;
        private readonly Stroke[] chargingArcs = new Stroke[4];
        // 1.2: each loaded charge snaps the crown open with an overshoot (ease-out-back),
        // so 1/2/3 read as three distinct beats rather than one slow swell.
        private float ChargeExpansion
        {
            get
            {
                float t = Mathf.Clamp01((Time.time - chargeChanged) / (prepared > 0 ? .14f : .16f));
                if (prepared <= 0) return Mathf.SmoothStep(chargeFrom, 0f, t);
                const float back = 2.2f;
                float u = t - 1f, eased = 1f + (back + 1f) * u * u * u + back * u * u;
                return chargeFrom + (prepared / 3f - chargeFrom) * eased;
            }
        }
        private float surgeAt = -100f;
        private int surgeTier;

        // 1.2 charge-up: charges drawn in before the beam sit primed in the crown (held open and
        // breathing) until the first Primary press fires them; then a big swell and a recoil.
        private int primed, openingTier;
        private float primedAt = -100f, openingAt = -100f;
        public int PrimedCharges => crownDriven && active ? primed : 0;
        public void SetPrimed(uint id, int count)
        {
            if (!active || id != castId) return;
            primed = Mathf.Clamp(count, 0, 20); primedAt = Time.time;
        }
        public void NoteOpening(uint id, int count)
        {
            if (!active || id != castId) return;
            primed = 0;
            openingTier = Mathf.Clamp(count, 1, 5); openingAt = Time.time;
            NoteSurge(1); // light beam flare; the crown swell and the wave carry the size
        }
        private float PrimedHold
        {
            get
            {
                if (primed <= 0) return 0f;
                float settle = Mathf.Clamp01((Time.time - primedAt) / .18f);
                return settle * (.3f + .09f * (primed < 5 ? primed : 5) + .06f * Mathf.Sin(Time.time * 9f));
            }
        }
        /// <summary>Opening swell: snaps the crown far open (up to ~2.7x the load expansion), then settles.</summary>
        private float OpeningSwell
        {
            get
            {
                float age = Time.time - openingAt;
                if (openingTier <= 0 || age < 0f || age > .7f) return 0f;
                float amp = 1.2f + .3f * openingTier;
                float rise = Mathf.Clamp01(age / .05f);
                float fall = 1f - Mathf.Clamp01((age - .05f) / .65f);
                return amp * rise * fall * fall;
            }
        }
        /// <summary>Crown recoil (m, back toward the body) on the opening: a hard kick that springs back.</summary>
        public float CrownRecoil
        {
            get
            {
                float age = Time.time - openingAt;
                if (!crownDriven || !active || openingTier <= 0 || age < 0f || age > .45f) return 0f;
                float decay = 1f - age / .45f;
                return (.35f + .08f * openingTier) * decay * decay * Mathf.Cos(age * 16f);
            }
        }
        /// <summary>0..1 energy of the opening for beam-side readouts.</summary>
        public float OpeningEnergy => crownDriven && active ? Mathf.Clamp01(OpeningSwell / 2f) : 0f;
        // Beam-side readouts for the default (behind) camera, where the crown is hidden by the body.
        /// <summary>Loaded charges, 0..3 (hold/release mode only).</summary>
        public int PreparedCharges => crownDriven && active ? prepared : 0;
        public float LoadFlash => crownDriven && active ? ChargeFlash : 0f;
        /// <summary>Release flare: tier-scaled spike that decays over ~0.4 s.</summary>
        public float SurgeFlare => crownDriven ? surgeTier * Mathf.Pow(1f - Mathf.Clamp01((Time.time - surgeAt) / (.25f + .07f * surgeTier)), 2f) : 0f;
        private void NoteSurge(int tier) { surgeAt = Time.time; surgeTier = Mathf.Clamp(tier, 1, 5); }
        // 0..1 flash right after a charge loads; decays in 0.2 s.
        private float ChargeFlash => prepared > 0 ? 1f - Mathf.Clamp01((Time.time - chargeChanged) / .2f) : 0f;

        // The physical ring is posed by GazeCrownMount before HaloRing fits its live radius.
        // Use that same radius for the corona and snapshot it for the departing wave.
        private float LiveCrownRadius()
        {
            var ring = body ? HaloRing.For(body) : null;
            float radius = ring && ring.Valid ? ring.Shape.Radius * 1.12f : .55f * CrownApertureScale;
            return Mathf.Clamp(radius, .25f, Mathf.Max(.25f, GazeTuning.Radius - .16f));
        }

        private void RenderChargingCrown()
        {
            // Keep the authoritative entry/return bookkeeping without displaying ammunition icons.
            for (int i = 0; i < MaxCharges; i++)
            {
                var orb = fuel[i];
                if (orb.swallowing && Time.time - orb.at >= orb.duration) orb.visible = orb.swallowing = false;
                orb.stroke.Hide(); orb.outline.Hide(); orb.filament.Hide();
                reserve[i].stroke.Hide(); reserve[i].outline.Hide(); reserve[i].filament.Hide();
            }
            for (int i = 0; i < intakeTrails.Length; i++)
            { intakeTrails[i].Hide(); intakeOutlines[i].Hide(); intakeFilaments[i].Hide(); }
            crownRim.Hide();
            float expansion = CrownExpansion;
            float radius = LiveCrownRadius();
            for (int i = 0; i < chargingArcs.Length; i++)
            {
                if (!active || expansion <= .01f) { chargingArcs[i].Hide(); continue; }
                // Four open electrical arcs echo the four physical crown pieces.
                float flash = ChargeFlash, full = prepared >= 3 ? 1f : 0f;
                // Spin quickens per tier; full charge adds a steady hot shimmer.
                float spin = 4.54f + 3.5f * prepared + 4f * full;
                float width = .035f + .055f * expansion + .07f * flash + .025f * full * (.6f + .4f * Mathf.Sin(Time.time * 37f));
                Color tint = Color.Lerp(accent, Color.white, .55f * flash + .2f * full);
                chargingArcs[i].Arc(crown, direction, radius, Time.time * spin + i * Mathf.PI * .5f,
                    1.25f + .35f * full, width, tint, Mathf.Min(1f, expansion * (ReducedEffects ? .5f : .85f) + .4f * flash));
            }
        }

        private void DrawReleasedCrown(Pulse p, float distance, float radius, float alpha)
        {
            Vector3 center = p.origin + p.direction * distance;
            for (int i = 0; i < p.wave.Length; i++)
                p.wave[i].Arc(center, p.direction, radius, p.rotation + i * Mathf.PI * .5f,
                    1.25f, ReducedEffects ? .055f : .085f, accent * .75f, alpha * (ReducedEffects ? .55f : .9f));
        }
        private static float ReleasedCrownRadius(Pulse p, float age) =>
            Mathf.Lerp(p.crownRadius, Mathf.Max(p.crownRadius, GazeTuning.Radius * .8f), Mathf.Clamp01(age / .12f));
    }
}
