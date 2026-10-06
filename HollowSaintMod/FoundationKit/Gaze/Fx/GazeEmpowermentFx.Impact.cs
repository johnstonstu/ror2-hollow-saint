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
            tier = Mathf.Clamp(tier, 1, 3);
            float scale = ReducedEffects ? .6f : 1f;
            VfxParticles.Burst(point, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, .16f + .05f * tier,
                Vector2.zero, Vector2.one * (1.6f + 1.3f * (tier - 1)) * scale, palette.Core);
            VfxParticles.Burst(point, Quaternion.identity, palette.Material(VfxAssets.Spark), Mathf.RoundToInt((10 + 12 * tier) * scale),
                .3f + .08f * tier, new Vector2(4f, 9f + 4f * tier), new Vector2(.05f, .12f), palette.Arc, stretch: .07f);
            int arcs = tier == 1 ? 0 : tier == 2 ? 3 : 6;
            float reach = 1.8f + 1.4f * (tier - 1);
            for (int i = 0; i < arcs; i++)
            {
                float angle = (i + Noise((int)id, i) * .6f) * Mathf.PI * 2f / arcs;
                Vector3 to = point + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * reach * (.75f + .4f * Noise((int)id, i + 9));
                LightningLine.Spawn(point + Vector3.up * .2f, to, .22f + .04f * tier, .16f * scale, 1, .22f, palette: palette);
            }
            if (tier >= 3)
            {
                // Full charge: a thunderbolt answers from the sky onto the impact.
                LightningLine.Spawn(point + Vector3.up * 16f + direction * -2f, point, .3f, .55f * scale, 2, .18f, palette: palette);
            }
        }
    }
}
