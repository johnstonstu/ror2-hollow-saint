using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Stormspear.Fx
{
    /// <summary>
    /// Impact of a thrown Stormspear (Beat.SpearBurst). The spear bursts into lightning that runs
    /// outward along the ground to the burst radius: small for a tap, big for a full charge.
    /// origin = impact point, start = surface normal (may be zero), scale = burst radius in metres.
    /// One-shot pooled lines only (LightningLine's budget applies); no per-frame cost.
    /// </summary>
    internal static class SpearBurstFx
    {
        private const float MinRadius = 3f, MaxRadius = 9f;

        internal static void Play(Vector3 origin, Vector3 normal, float scale, SkinFxPalette palette)
        {
            float radius = Mathf.Clamp(scale, MinRadius, MaxRadius);
            float t = Mathf.InverseLerp(MinRadius, MaxRadius, radius);
            if (normal.sqrMagnitude < 0.01f) normal = Vector3.up;
            normal.Normalize();
            Vector3 center = origin + normal * 0.15f;

            // Flash and hot core at the impact.
            VfxParticles.Burst(center, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.2f, Vector2.zero, new Vector2(1.2f, 1.5f) * (1f + 1.2f * t), palette.Core);
            VfxParticles.Burst(center, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.35f, Vector2.zero, new Vector2(radius * 0.45f, radius * 0.55f), palette.Arc);
            VfxParticles.Burst(center, Quaternion.identity, palette.Material(VfxAssets.Spark), 14 + (int)(22f * t), 0.45f, new Vector2(4f, 9f + 8f * t), new Vector2(0.07f, 0.15f), palette.Core, stretch: 0.09f);
            VfxParticles.FlashLight(center + normal * 0.5f, palette.Core, 4f + 6f * t, radius * 1.8f, 0.28f);
            VfxParticles.Ring(GroundUnder(origin), Vector3.up, radius * 0.2f, radius, 0.3f, 0.06f + 0.05f * t, palette.Material(VfxAssets.Trail), palette: palette);

            // Radial arcs spreading along the ground; each crawls on over drawTime so they visibly spread.
            int arcs = 5 + Mathf.RoundToInt(7f * t);
            float phase = Random.value * Mathf.PI * 2f;
            Vector3 tangentA = Vector3.Cross(normal, Mathf.Abs(normal.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 tangentB = Vector3.Cross(normal, tangentA);
            for (int i = 0; i < arcs; i++)
            {
                float a = phase + (i + Random.Range(-0.3f, 0.3f)) * Mathf.PI * 2f / arcs;
                Vector3 dir = tangentA * Mathf.Cos(a) + tangentB * Mathf.Sin(a);
                float reach = radius * Random.Range(0.65f, 1f);
                Vector3 end = Snap(center + dir * reach);
                var line = LightningLine.Spawn(center, end, 0.22f + 0.1f * t, 0.45f + 0.35f * t, 1, 0.14f, palette: palette);
                line.drawTime = 0.1f + 0.06f * t;
                if (i % 2 == 0)
                {
                    // A shorter second hop forking off partway out.
                    Vector3 mid = Vector3.Lerp(center, end, 0.55f);
                    Vector3 side = Vector3.Cross(dir, normal) * Random.Range(-1f, 1f);
                    Vector3 hop = Snap(mid + (dir * 0.5f + side).normalized * (radius * 0.3f));
                    var fork = LightningLine.Spawn(Snap(mid), hop, 0.2f, 0.3f + 0.2f * t, 0, 0.16f, palette: palette);
                    fork.drawTime = 0.08f;
                }
                if (i % 3 == 0)
                    VfxParticles.Burst(end, Quaternion.identity, palette.Material(VfxAssets.Spark), 4, 0.25f, new Vector2(2f, 5f), new Vector2(0.05f, 0.1f), palette.Arc, stretch: 0.06f);
            }
            // A few arcs thrown up into the air so the burst reads from a distance.
            for (int i = 0; i < 2 + (int)(3f * t); i++)
                LightningLine.Spawn(center, center + (normal * 1.6f + Random.onUnitSphere * 1.1f) * (0.8f + 0.5f * t), 0.16f, 0.5f, 1, 0.2f, palette: palette);
            Volume(center, radius, t, palette);
        }

        /// <summary>v0.9.16 (Stu: "a little more 3D to the impact"): the burst also fills the air.
        /// An expanding shell of tilted rings reads as a sphere from any angle, arcs leap out over a
        /// dome instead of only along the ground, and a column of lightning erupts straight up
        /// (taller with charge), so the hit has height as well as reach.</summary>
        private static void Volume(Vector3 center, float radius, float t, SkinFxPalette palette)
        {
            var trail = palette.Material(VfxAssets.Trail);
            float shell = radius * (0.55f + 0.2f * t);
            float yaw = Random.value * 360f;
            for (int i = 0; i < 3; i++)
            {
                Vector3 n = Quaternion.Euler(0f, yaw + i * 60f, 0f) * Quaternion.Euler(62f, 0f, 0f) * Vector3.up;
                VfxParticles.Ring(center, n, 0.3f, shell, 0.24f + 0.08f * t, 0.05f + 0.04f * t, trail, palette: palette);
            }
            VfxParticles.Ring(center + Vector3.up * shell * 0.45f, Vector3.up, 0.2f, shell * 0.85f, 0.26f, 0.04f + 0.03f * t, trail, palette: palette);

            // Dome arcs: out and up over the burst, crawling on so they visibly leap.
            int dome = 4 + Mathf.RoundToInt(6f * t);
            float phase = Random.value * Mathf.PI * 2f;
            for (int i = 0; i < dome; i++)
            {
                float a = phase + (i + Random.Range(-0.35f, 0.35f)) * Mathf.PI * 2f / dome;
                float elevation = Random.Range(22f, 70f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(a) * Mathf.Cos(elevation), Mathf.Sin(elevation), Mathf.Sin(a) * Mathf.Cos(elevation));
                Vector3 end = center + dir * shell * Random.Range(0.75f, 1.05f);
                var arc = LightningLine.Spawn(center, end, 0.2f + 0.08f * t, 0.35f + 0.2f * t, 1, 0.2f, palette: palette);
                arc.drawTime = 0.07f;
                if (i % 2 == 0)
                    VfxParticles.Burst(end, Quaternion.identity, palette.Material(VfxAssets.Spark), 3, 0.22f, new Vector2(1.5f, 4f), new Vector2(0.04f, 0.09f), palette.Core, stretch: 0.06f);
            }

            // Eruption: a column of lightning straight up from the impact.
            float height = 2.5f + 5f * t;
            Vector3 top = center + Vector3.up * height;
            var column = LightningLine.Spawn(center, top, 0.18f + 0.1f * t, 0.8f + 0.8f * t, 2, 0.1f, palette: palette);
            column.drawTime = 0.06f;
            for (int i = 0; i < 2; i++)
                LightningLine.Spawn(center + Random.insideUnitSphere * 0.3f, top + Random.insideUnitSphere * height * 0.25f, 0.14f, 0.35f + 0.3f * t, 1, 0.18f, palette: palette).drawTime = 0.05f;
            VfxParticles.Burst(top, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.16f, Vector2.zero, new Vector2(0.6f, 0.9f) * (1f + t), palette.Arc);
            VfxParticles.Burst(center + Vector3.up * 0.4f, Quaternion.identity, palette.Material(VfxAssets.Spark), 10 + (int)(18f * t), 0.55f, new Vector2(6f, 12f + 8f * t), new Vector2(0.06f, 0.14f), palette.Arc, stretch: 0.1f);
        }

        private static Vector3 GroundUnder(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out hit, 4.5f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.08f;
            return p;
        }

        /// <summary>Drops a point onto the ground within a few metres so arcs hug the terrain.</summary>
        private static Vector3 Snap(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out hit, 6f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.12f;
            return p;
        }
    }
}
