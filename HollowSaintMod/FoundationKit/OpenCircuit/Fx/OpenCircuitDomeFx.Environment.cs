using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.OpenCircuit.Fx
{
    public sealed partial class OpenCircuitDomeFx
    {
        // 1.2 (Stu: "how the lightning reacts with the terrain"): 16 surface steps per branch sized to
        // the radius, so ground lightning crawls ~92% of the way to the damage edge (was 8 x 0.35 m = 2.7 m).
        private const int BranchSamples = 16, TerrainSamples = 4 * BranchSamples, MaxWorldChecks = 160, StrikeSlots = 4;
        private readonly Vector3[] terrain = new Vector3[TerrainSamples];
        private readonly bool[] terrainValid = new bool[TerrainSamples];
        private readonly Strike[] strikes = new Strike[StrikeSlots];
        private Vector3 terrainCenter;
        private float terrainRadius, nextTerrain;
        private int terrainCursor, worldChecks, terrainGeneration;
        private readonly Vector3[] pendingGround = new Vector3[BranchSamples];
        private readonly float[] groundAt = new float[4];
        private Vector3 groundOrigin;
        private int pendingCount;
        private Vector3 pendingNormal;
        private readonly int[] groundCount = new int[4];
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
            // Ordinary walking keeps the completed world-space paths alive. A teleport or
            // radius change retires them; four new probes per refresh can never stall a frame.
            if (Mathf.Abs(radius - terrainRadius) > 0.01f || Vector3.Distance(body.corePosition, terrainCenter) > radius * 0.5f)
            {
                for (int i = 0; i < terrainValid.Length; i++) terrainValid[i] = false;
                terrainRadius = radius; terrainCursor = 0; nextTerrain = 0f;
            }
            terrainCenter = body.corePosition;
            if (Time.time < nextTerrain) return;
            nextTerrain = Time.time + 0.025f;
            float step = Mathf.Min(0.6f, Mathf.Max(0.3f, radius * 0.92f / BranchSamples));
            for (int n = 0; n < 8; n++)
            {
                int index = terrainCursor++ % TerrainSamples;
                int branch = index / BranchSamples, j = index % BranchSamples;
                if (j == 0) { groundOrigin = body.corePosition; pendingCount = 0; pendingNormal = Vector3.up; terrainGeneration++; }
                float phase = terrainGeneration * 1.71f + branch * 2.13f;
                float angle = branch * Mathf.PI * 0.5f + 0.22f * Mathf.Sin(phase);
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 side = new Vector3(-radial.z, 0f, radial.x);
                // Stay close to the projected foot and grow in short surface-tangent steps.
                // A missed tip retires only the suffix, never the already grounded root.
                if (j == pendingCount)
                {
                    Vector3 heading = (radial + side * (0.22f * Mathf.Sin(j * 1.8f + phase))).normalized;
                    Vector3 tangent = Vector3.ProjectOnPlane(heading, pendingNormal).normalized;
                    Vector3 sample = j == 0 ? groundOrigin + radial * 0.25f :
                        pendingGround[j - 1] - pendingNormal * 0.10f + tangent * step;
                    // Probe from high enough to climb a knee-high rock or step, never a wall.
                    float height = j == 0 ? Mathf.Min(3f, radius * 0.75f) : step * 1.6f + 0.3f;
                    RaycastHit hit;
                    bool found = Physics.Raycast(sample + Vector3.up * height, Vector3.down, out hit,
                        height * 2f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.35f;
                    Vector3 point = found ? hit.point + hit.normal * 0.10f : sample;
                    // Gaps, cliff drops and walls end the crawl: bounded step length and a
                    // clear line from the previous contact (lifted off the surface).
                    if (found && Vector3.Distance(point, body.corePosition) < radius - 0.09f &&
                        (j == 0 || (Vector3.Distance(point, pendingGround[j - 1]) <= step * 1.9f &&
                         !Physics.CheckCapsule(pendingGround[j - 1] + pendingNormal * 0.12f, point + hit.normal * 0.12f, 0.04f,
                            LayerIndex.world.mask, QueryTriggerInteraction.Ignore))))
                    {
                        pendingGround[j] = point; pendingNormal = hit.normal; pendingCount++;
                    }
                }
                if (j == BranchSamples - 1)
                {
                    // Publish an entire path atomically; never mix old and newly probed points.
                    for (int k = 0; k < BranchSamples; k++) { terrain[branch * BranchSamples + k] = pendingGround[k]; terrainValid[branch * BranchSamples + k] = k < pendingCount; }
                    groundCount[branch] = pendingCount;
                    groundAt[branch] = Time.time;
                }
            }
        }
        /// <summary>Dev diagnostic: grounded steps and tip reach per branch.</summary>
        internal string DebugBranches()
        {
            var sb = new System.Text.StringBuilder();
            for (int b = 0; b < 4; b++)
            {
                int n = groundCount[b];
                float reach = n > 0 && body ? Vector3.Distance(terrain[b * BranchSamples + n - 1], body.corePosition) : 0f;
                sb.Append(b).Append(':').Append(n).Append('/').Append(BranchSamples).Append('@').Append(reach.ToString("0.0")).Append("m ");
            }
            return sb.ToString();
        }

        private bool GroundBranch(Stroke stroke, int branch, float radius, float travel)
        {
            int start = branch * BranchSamples;
            if (!terrainValid[start] || Time.time - groundAt[branch] > 0.7f) return false;
            int count = groundCount[branch];
            int first = 0;
            while (first < count && Vector3.Distance(terrain[start + first], body.corePosition) > radius - 0.09f) first++;
            int last = first;
            while (last < count && Vector3.Distance(terrain[start + last], body.corePosition) <= radius - 0.09f) last++;
            if (last - first < 2) return false;
            // Clip the moving window but preserve every original sampled corner inside it.
            // Interpolating a fixed 17-point resample would cut across terrain crests.
            float lo = first + Mathf.Max(0f, travel - 0.55f) * (last - first - 1);
            float hi = first + travel * (last - first - 1);
            if (hi - lo < 0.01f) return false;
            int a = (int)lo, b = (int)hi;
            int shown = 0;
            stroke.points[shown++] = Vector3.Lerp(terrain[start + a], terrain[start + a + 1], lo - a);
            for (int k = a + 1; k <= b; k++) stroke.points[shown++] = terrain[start + k];
            if (hi > b) stroke.points[shown++] = Vector3.Lerp(terrain[start + b], terrain[start + b + 1], hi - b);
            for (int k = shown; k < Points; k++) stroke.points[k] = stroke.points[shown - 1];
            return true;
        }
        private bool ClearGroundPrefix(Stroke stroke)
        {
            int valid = 1;
            for (int j = 1; j < Points; j++)
            {
                if (Vector3.Distance(stroke.points[j - 1], stroke.points[j]) < 0.0001f) break;
                if (!ClearSegment(stroke.points[j - 1], stroke.points[j], 0.065f)) break;
                valid++;
            }
            if (valid < 2) return false;
            // Every retained segment was checked this frame; never join past a blocked segment.
            for (int j = valid; j < Points; j++) stroke.points[j] = stroke.points[valid - 1];
            return true;
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
