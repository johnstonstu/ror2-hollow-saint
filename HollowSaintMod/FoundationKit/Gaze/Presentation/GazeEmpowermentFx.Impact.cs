using HollowSaint.FoundationKit.Vfx;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    // Runtime-only (uses particle/lightning helpers the presentation test stubs do not provide).
    public sealed partial class GazeEmpowermentFx
    {
        /// <summary>Once per surge, at the first confirmed hit: the "it landed" beat.
        /// Cosmetic only; scaled by tier so 1/2/3 read differently at a glance.</summary>
        public void SurgeImpact(uint id, Vector3 point, int tier)
        {
            if (!active || id != castId || palette == null || !Finite(point)) return;
            tier = Mathf.Clamp(tier, 1, 5);
            float scale = ReducedEffects ? .6f : 1f;
            VfxParticles.Burst(point, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, .16f + .05f * tier,
                Vector2.zero, Vector2.one * (1.6f + 1.3f * (tier - 1)) * scale, palette.Core);
            VfxParticles.Burst(point, Quaternion.identity, palette.Material(VfxAssets.Spark), Mathf.RoundToInt((10 + 12 * tier) * scale),
                .3f + .08f * tier, new Vector2(4f, 9f + 4f * tier), new Vector2(.05f, .12f), palette.Arc, stretch: .07f);
            int arcs = tier == 1 ? 0 : 3 * (tier - 1);
            float reach = Mathf.Min(1.8f + 1.4f * (tier - 1), GazeReleaseTuning.OpeningRadius(tier));
            for (int i = 0; i < arcs; i++)
            {
                float angle = (i + Noise((int)id, i) * .6f) * Mathf.PI * 2f / arcs;
                Vector3 to = point + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * reach * (.75f + .4f * Noise((int)id, i + 9));
                LightningLine.Spawn(point + Vector3.up * .2f, to, .22f + .04f * tier, .16f * scale, 1, .22f, palette: palette);
            }
            if (tier >= 3)
            {
                // Full charge: thunderbolts answer from the sky onto the impact (more for bigger openings).
                for (int b = 0; b < tier - 2; b++)
                {
                    Vector3 off = b == 0 ? Vector3.zero : Quaternion.Euler(0f, b * 137f, 0f) * Vector3.forward * (1.2f + .6f * b);
                    LightningLine.Spawn(point + off + Vector3.up * (16f + 2f * b) + direction * -2f, point + off, .3f + .05f * b, (.55f + .1f * b) * scale, 2, .18f, palette: palette);
                }
            }
            if (tier >= 4)
            {
                // Opening blast: a shockwave over the actual blast radius (terrain-hugging ring).
                float radius = GazeReleaseTuning.OpeningRadius(tier);
                VfxParticles.Ring(point, Vector3.up, .5f, radius, .45f, .18f * scale, palette.Material(VfxAssets.Trail), palette);
                VfxParticles.Burst(point + Vector3.up * .5f, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, .3f,
                    Vector2.zero, Vector2.one * (4f + tier) * scale, palette.Core);
                VfxParticles.FlashLight(point + Vector3.up, palette.Arc, 6f + tier, 14f, .35f);
            }
        }
    }
}
