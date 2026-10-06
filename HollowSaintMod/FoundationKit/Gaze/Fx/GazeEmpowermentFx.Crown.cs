using HollowSaint.FoundationKit.Vfx;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    public sealed partial class GazeEmpowermentFx
    {
        private bool crownDriven;
        private float chargeFrom, chargeChanged;
        private readonly Stroke[] chargingArcs = new Stroke[4];
        private float ChargeExpansion => Mathf.SmoothStep(chargeFrom, prepared / 3f,
            (Time.time - chargeChanged) / (prepared > 0 ? .18f : .16f));

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
                chargingArcs[i].Arc(crown, direction, radius, Time.time * 4.54f + i * Mathf.PI * .5f,
                    1.25f, .035f + .055f * expansion, accent, expansion * (ReducedEffects ? .5f : .85f));
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
