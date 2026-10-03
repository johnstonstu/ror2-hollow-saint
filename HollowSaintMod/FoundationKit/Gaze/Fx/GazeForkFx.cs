using System.Collections;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    /// <summary>
    /// Lightning that races across the ground: a chain of short bolts hugging the terrain, drawn
    /// one segment after another so it reads as crawling outward from the beam's impact. Used by
    /// the ambient impact forks (local) and the fork hits (networked, ending in an upward strike
    /// into the enemy). Off the ground (beam into the air) it becomes a plain arc.
    /// </summary>
    public static class GazeForkFx
    {
        private const float SegmentLength = 1.1f;
        private const float SegmentDelay = 0.022f;
        private const float GroundLift = 0.15f;

        /// <summary>Ambient fork in a random direction along the surface. Visual only.</summary>
        public static void Ambient(MonoBehaviour host, Vector3 from, Vector3 normal, SkinFxPalette palette)
        {
            Vector3 up = normal.sqrMagnitude > 0.01f ? normal.normalized : Vector3.up;
            Vector3 dir = Vector3.ProjectOnPlane(Random.onUnitSphere, up);
            if (dir.sqrMagnitude < 0.01f) return;
            Vector3 to = from + dir.normalized * Random.Range(4f, 8f);
            host.StartCoroutine(Crawl(from, to, null, palette, 0.9f, 0f));
        }

        /// <summary>Fork from the impact to an enemy: crawl to its feet, then strike up into it.</summary>
        public static void ToTarget(MonoBehaviour host, Vector3 from, Vector3 target, SkinFxPalette palette, float delay)
        {
            Vector3 feet;
            bool grounded = Ground(target, 4f, out feet);
            Vector3 fromGround;
            if (!grounded || !Ground(from, 1.5f, out fromGround))
            {
                host.StartCoroutine(AirArc(from, target, palette, delay));
                return;
            }
            host.StartCoroutine(Crawl(fromGround, feet, target, palette, 1.1f, delay));
        }

        private static IEnumerator AirArc(Vector3 from, Vector3 to, SkinFxPalette palette, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            LightningLine.Spawn(from, to, 0.3f, 1.4f, 2, 0.16f, palette: palette);
            LightningLine.Spawn(from, to, 0.55f, 0.45f, 0, 0.03f, 10f, palette: palette);
            Strike(to, palette, 1f);
        }

        private static IEnumerator Crawl(Vector3 from, Vector3 to, Vector3? strikeAt, SkinFxPalette palette, float width, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.2f) yield break;
            int segments = Mathf.Clamp(Mathf.CeilToInt(length / SegmentLength), 2, 12);
            Vector3 side = Vector3.Cross(Vector3.up, delta / length);
            if (side.sqrMagnitude < 1e-3f) side = Vector3.right;
            side.Normalize();
            Vector3 previous = from + Vector3.up * GroundLift;
            for (int i = 1; i <= segments; i++)
            {
                float u = i / (float)segments;
                Vector3 p = from + delta * u;
                // Wander sideways mid-path, pinned at both ends.
                if (i < segments) p += side * Random.Range(-0.45f, 0.45f) * Mathf.Sin(u * Mathf.PI);
                Vector3 g;
                p = Ground(p, 2.5f, out g) ? g + Vector3.up * GroundLift : p;
                float taper = Mathf.Lerp(1f, 0.6f, u);
                LightningLine.Spawn(previous, p, 0.42f, width * taper, i % 3 == 1 ? 1 : 0, 0.22f, 0.03f, palette);
                previous = p;
                yield return new WaitForSeconds(SegmentDelay);
            }
            VfxParticles.Burst(previous, Quaternion.identity, palette.Material(VfxAssets.Spark), 6, 0.25f,
                new Vector2(2f, 6f), new Vector2(0.06f, 0.12f), palette.Core, stretch: 0.07f);
            if (strikeAt.HasValue)
            {
                LightningLine.Spawn(previous, strikeAt.Value, 0.3f, 1.3f, 1, 0.14f, palette: palette);
                Strike(strikeAt.Value, palette, 1f);
            }
        }

        public static void Strike(Vector3 at, SkinFxPalette palette, float scale)
        {
            VfxParticles.Burst(at, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.12f, Vector2.zero, new Vector2(0.6f, 0.8f) * scale, palette.Arc);
            VfxParticles.Burst(at, Quaternion.identity, palette.Material(VfxAssets.Spark), 12, 0.3f, new Vector2(5f, 13f), new Vector2(0.08f, 0.16f), palette.Core, stretch: 0.09f);
            VfxParticles.FlashLight(at, palette.Arc, 1.6f * scale, 5f, 0.12f);
        }

        private static bool Ground(Vector3 p, float reach, out Vector3 point)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * reach * 0.5f, Vector3.down, out hit, reach * 1.5f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                return true;
            }
            point = p;
            return false;
        }
    }
}
