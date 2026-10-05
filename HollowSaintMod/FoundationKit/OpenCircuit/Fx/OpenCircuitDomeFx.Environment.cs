using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.OpenCircuit.Fx
{
    public sealed partial class OpenCircuitDomeFx
    {
        private const int TerrainSamples = 32, MaxWorldChecks = 96, StrikeSlots = 4;
        private readonly Vector3[] terrain = new Vector3[TerrainSamples];
        private readonly bool[] terrainValid = new bool[TerrainSamples];
        private readonly Strike[] strikes = new Strike[StrikeSlots];
        private Vector3 terrainCenter;
        private float terrainRadius, nextTerrain;
        private int terrainCursor, worldChecks;
        private sealed class Strike
        {
            internal readonly Stroke stroke = new Stroke();
            internal Vector3 target;
            internal float until;
        }

        /// <summary>Presentation only. Call once on each observing client AFTER an authoritative
        /// confirmed zap, using its victim hit/core position in world space. No damage, target
        /// selection, automatic timing, audio or network send. False means inactive/full/invalid.
        /// Accepted events may be suppressed by solid world or the bounded visibility budget.</summary>
        public bool ShowConfirmedStrike(Vector3 victimPosition)
        {
            if (!isActiveAndEnabled || !body || !body.healthComponent || !body.healthComponent.alive ||
                (NetworkServer.active && !NetworkClient.active) || !Finite(victimPosition) ||
                (gaze && gaze.Current != Gaze.GazeBeam.Phase.Idle) ||
                !((OpenCircuitBuff.Def && body.HasBuff(OpenCircuitBuff.Def)) || (driver && driver.CrownOpen))) return false;
            // Build is lazy in LateUpdate. Events arriving before first visible expansion are dropped.
            if (!visualRoot || !OwnsPerimeter) return false;
            for (int i = 0; i < strikes.Length; i++)
            {
                var strike = strikes[i];
                if (strike.until > Time.time) continue;
                strike.target = victimPosition; strike.until = Time.time + 0.20f;
                return true;
            }
            return false;
        }
        private static bool Finite(Vector3 p)
        {
            return !(float.IsNaN(p.x) || float.IsInfinity(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.y) || float.IsNaN(p.z) || float.IsInfinity(p.z));
        }
        private void BuildStrikes()
        {
            for (int i = 0; i < strikes.Length; i++)
            {
                var strike = new Strike(); strikes[i] = strike;
                strike.stroke.glow = MakeLine("ConfirmedCircuitZapGlow", Gaze.Fx.GazeContrastAssets.Glow);
                strike.stroke.core = MakeLine("ConfirmedCircuitZapCore", Gaze.Fx.GazeContrastAssets.Core);
            }
        }
        private void DestroyStrikes() { for (int i = 0; i < strikes.Length; i++) strikes[i] = null; }
        private void ResetEnvironment()
        {
            for (int i = 0; i < terrainValid.Length; i++) terrainValid[i] = false;
            for (int i = 0; i < strikes.Length; i++) if (strikes[i] != null) strikes[i].until = 0f;
            terrainRadius = 0f; terrainCursor = 0; nextTerrain = 0f;
        }
        private void RefreshTerrain(float radius)
        {
            if (Mathf.Abs(radius - terrainRadius) > 0.01f || Vector3.Distance(body.corePosition, terrainCenter) > 0.25f)
            {
                for (int i = 0; i < terrainValid.Length; i++) terrainValid[i] = false;
                terrainCenter = body.corePosition; terrainRadius = radius; terrainCursor = 0; nextTerrain = 0f;
            }
            if (Time.time < nextTerrain) return;
            nextTerrain = Time.time + 0.025f;
            // Four downward probes per refresh, no catch-up loop. Complete ring in eight refreshes.
            float height = Mathf.Min(3f, radius * 0.34f);
            for (int n = 0; n < 4; n++)
            {
                int i = terrainCursor++ % TerrainSamples;
                float angle = i * (Mathf.PI * 2f / TerrainSamples);
                Vector3 sample = terrainCenter + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (radius * 0.94f);
                RaycastHit hit;
                terrainValid[i] = Physics.Raycast(sample + Vector3.up * height, Vector3.down, out hit,
                    height * 2f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.35f;
                if (terrainValid[i])
                {
                    terrain[i] = hit.point + hit.normal * 0.10f;
                    terrainValid[i] = Vector3.Distance(terrain[i], terrainCenter) <= radius - 0.07f;
                }
            }
        }
        private Vector3 Contour(Vector3 point, float radius)
        {
            Vector3 delta = point - terrainCenter;
            float angle = Mathf.Atan2(delta.z, delta.x);
            if (angle < 0f) angle += Mathf.PI * 2f;
            float sample = angle * (TerrainSamples / (Mathf.PI * 2f));
            int a = (int)sample % TerrainSamples, b = (a + 1) % TerrainSamples;
            if (!terrainValid[a] || !terrainValid[b]) return point;
            // Blend only the electrical sweep down/up onto nearby terrain. Metal perimeter is unchanged.
            Vector3 ground = Vector3.Lerp(terrain[a], terrain[b], sample - (int)sample);
            return Vector3.Lerp(point, ground, expansion * 0.9f);
        }
        private bool ClearSegment(Vector3 a, Vector3 b, float thickness)
        {
            if (worldChecks >= MaxWorldChecks) return false;
            worldChecks++;
            // Capsule overlap also catches endpoints embedded in walls (a ray can miss these).
            return !Physics.CheckCapsule(a, b, thickness, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
        }
        private bool ClearPath(Vector3[] points, float thickness)
        {
            for (int j = 1; j < points.Length; j++) if (!ClearSegment(points[j - 1], points[j], thickness)) return false;
            return true;
        }
        private void RenderStrikes()
        {
            for (int i = 0; i < strikes.Length; i++)
            {
                var strike = strikes[i]; var stroke = strike.stroke;
                stroke.glow.enabled = stroke.core.enabled = false;
                if (strike.until <= Time.time) continue;
                Vector3 source = body.corePosition;
                float nearest = float.PositiveInfinity;
                // Choose the closest unobstructed actual crown element or the body itself.
                for (int n = 0; n < 5; n++)
                {
                    Vector3 candidate = n == 4 ? body.corePosition : arcs[n].position;
                    float distance = Vector3.Distance(candidate, strike.target);
                    if (distance < nearest && ClearSegment(candidate, strike.target, 0.045f)) { nearest = distance; source = candidate; }
                }
                if (float.IsInfinity(nearest)) continue;
                Vector3 axis = (strike.target - source).normalized;
                Vector3 side = Vector3.Cross(axis, Vector3.up).normalized;
                for (int j = 0; j < Points; j++)
                {
                    float t = j / (float)(Points - 1);
                    float envelope = Mathf.Sin(t * Mathf.PI);
                    // A small lifted, jagged filament, with exact source and victim endpoints.
                    stroke.points[j] = Vector3.Lerp(source, strike.target, t) +
                        (side * (Mathf.Sin(j * 2.7f + i + (int)(Time.time * 24f)) * 0.10f) + Vector3.up * 0.16f) * envelope;
                }
                stroke.points[0] = source; stroke.points[Points - 1] = strike.target;
                if (!ClearPath(stroke.points, 0.045f)) continue;
                float alpha = Mathf.Clamp01((strike.until - Time.time) / 0.08f);
                Draw(stroke.glow, stroke.points, 0.08f, palette.Arc, alpha * 0.7f);
                Draw(stroke.core, stroke.points, 0.026f, palette.Core, alpha);
            }
        }
    }
}
