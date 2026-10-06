using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Stormspear.Fx
{
    internal static class SpearBurstFx
    {
        /// <summary>1.2 (Stu: "needs a 3D splash, an AOE impact like the lightning bolt"): the burst is
        /// now a volume, not a decal. Layers, all scaled by the burst radius (3 m tap .. 10 m crown):
        /// the house-tinted Capacitor strike body (sphere, ring, radial streaks, distortion; no sky
        /// bolt, no sound, no screen flash), a dome of arcs thrown up and out to the blast edge, a
        /// terrain-hugging shockwave at the real radius, an upward spark fountain and a light.
        /// Cosmetic and local only; audio stays on the existing SpearBurst beat.</summary>
        internal static void Play(Vector3 origin, Vector3 normal, float scale, SkinFxPalette palette)
        {
            if (!SpearAfterglowFx.Valid(origin) || float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0.15f) return;
            if (!SpearAfterglowFx.Valid(normal) || normal.sqrMagnitude < 0.01f) normal = Vector3.up;
            normal = normal.normalized;
            float radius = Mathf.Clamp(scale, 1f, 12f);
            float k = Mathf.Clamp01((radius - 1.5f) / 8.5f); // 0 = uncharged (ground tap), 1 = full crown burst
            float size = Mathf.Min(0.8f, scale * 0.2f);
            VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.12f, Vector2.zero, new Vector2(size * 0.8f, size), palette.Arc);

            // Strike body: the same 3D bloom the crown Thunderbolt lands with.
            Storm.RoyalCapacitorFx.Splash(origin + normal * 0.15f, palette, Mathf.Lerp(0.4f, 1.0f, k));

            // Spear signature: a lance of light driven up out of the impact, plus a thin echo,
            // taller and thicker the more charged the throw was.
            float lance = Mathf.Lerp(1.8f, 7f, k);
            LightningLine.Spawn(origin, origin + normal * lance, 0.22f + 0.1f * k, Mathf.Lerp(0.35f, 0.7f, k), 0, 0.04f, 10f, palette: palette);
            LightningLine.Spawn(origin, origin + normal * lance * 1.3f, 0.14f, Mathf.Lerp(0.12f, 0.22f, k), 1, 0.1f, palette: palette);

            // Dome: arcs leap up and out and land on the blast edge, so the radius reads in 3D.
            int arcs = Mathf.RoundToInt(Mathf.Lerp(4f, 10f, k));
            float seed = origin.x * 0.37f + origin.z * 0.61f;
            Vector3 side = Vector3.Cross(normal, Mathf.Abs(normal.y) > 0.9f ? Vector3.forward : Vector3.up).normalized;
            Vector3 fwd = Vector3.Cross(side, normal);
            for (int i = 0; i < arcs; i++)
            {
                float a = (i + 0.35f * Mathf.Sin(seed + i * 2.1f)) * Mathf.PI * 2f / arcs;
                Vector3 radial = side * Mathf.Cos(a) + fwd * Mathf.Sin(a);
                float reach = radius * (0.7f + 0.2f * Mathf.Sin(seed * 1.7f + i));
                Vector3 apex = origin + radial * reach * 0.45f + normal * reach * (0.45f + 0.15f * Mathf.Cos(seed + i * 1.3f));
                Vector3 land = origin + radial * reach + normal * 0.2f;
                RaycastHit hit;
                if (Physics.Raycast(land + normal * 2f, -normal, out hit, 4f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                    land = hit.point + hit.normal * 0.1f;
                float life = 0.16f + 0.08f * k;
                float width = Mathf.Lerp(0.18f, 0.32f, k);
                LightningLine.Spawn(origin + normal * 0.2f, apex, life, width, i % 2, 0.2f, palette: palette);
                LightningLine.Spawn(apex, land, life + 0.04f, width * 0.8f, 0, 0.22f, palette: palette);
            }

            // Shockwave at the actual damage radius plus a quick inner ring.
            // On an enemy the burst sits at chest height: drop the shockwave to the floor below.
            Vector3 ground = origin + normal * 0.05f, groundNormal = normal;
            RaycastHit floor;
            if (Physics.Raycast(origin + Vector3.up * 0.3f, Vector3.down, out floor, 4f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) && floor.normal.y > 0.35f)
            { ground = floor.point + floor.normal * 0.05f; groundNormal = floor.normal; }
            VfxParticles.Ring(ground, groundNormal, 0.4f, radius, 0.32f + 0.1f * k, Mathf.Lerp(0.14f, 0.24f, k), palette.Material(VfxAssets.Trail), palette);
            VfxParticles.Ring(ground, groundNormal, 0.2f, radius * 0.45f, 0.18f, 0.1f, palette.Material(VfxAssets.Trail), palette);

            // Splash: a fountain of sparks thrown up off the surface, and a wide low flash.
            VfxParticles.Burst(origin, Quaternion.LookRotation(normal), palette.Material(VfxAssets.Spark), Mathf.RoundToInt(Mathf.Lerp(18f, 40f, k)),
                0.45f + 0.15f * k, new Vector2(6f, 12f + 8f * k), new Vector2(0.06f, 0.14f), palette.Core, stretch: 0.08f, spreadAngle: 40f);
            VfxParticles.Burst(origin + normal * 0.6f, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.2f + 0.06f * k,
                Vector2.zero, Vector2.one * Mathf.Lerp(2.4f, 5.5f, k), palette.Arc);
            VfxParticles.FlashLight(origin + normal * 1.2f, palette.Arc, Mathf.Lerp(4f, 8f, k), radius * 1.6f, 0.28f);

            SpearAfterglowFx.Play(origin, normal, scale, palette);
        }
    }
}
