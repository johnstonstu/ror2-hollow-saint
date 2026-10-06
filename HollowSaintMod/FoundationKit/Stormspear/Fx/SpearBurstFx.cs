using HollowSaint.FoundationKit.Vfx;
using UnityEngine;

namespace HollowSaint.FoundationKit.Stormspear.Fx
{
    /// <summary>One local presentation per existing authoritative SpearBurst beat.
    /// scale is the supplied blast radius, not funding. Funded landing strike remains separate.</summary>
    internal static class SpearBurstFx
    {
        internal static void Play(Vector3 origin, Vector3 normal, float scale, SkinFxPalette palette)
        {
            if (!SpearAfterglowFx.Valid(origin) || float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0.15f) return;
            // Compact colored arrival, with no shrapnel, white bloom, pillar or implied extra hit.
            float size = Mathf.Min(0.8f, scale * 0.2f);
            VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.12f,
                Vector2.zero, new Vector2(size * 0.8f, size), palette.Arc);
            SpearAfterglowFx.Play(origin, normal, scale, palette);
        }
    }
}
