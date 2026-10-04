using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    public sealed partial class GazeEmpowermentFx
    {
        private sealed class GroundStroke
        {
            internal Stroke stroke, outline;
            internal readonly Vector3[] path = new Vector3[9], shown = new Vector3[11];
            internal readonly float[] distance = new float[9];
            internal int count;
            internal float delay;
        }
        private sealed class Pulse
        {
            internal bool active, ground, finale;
            internal float at, travel, spread, radius, length;
            internal Vector3 origin, end, groundPoint, direction, normal;
            internal Stroke sleeve, spine, front, outline;
            internal int seed;
            internal readonly Stroke[] forks = new Stroke[2];
            internal readonly Vector3[][] forkPoints = { new Vector3[7], new Vector3[7] };
            internal readonly Vector3[] line = new Vector3[12];
            internal readonly GroundStroke[] roots = new GroundStroke[12];
        }
        private sealed class Strike
        {
            internal bool active;
            internal float at;
            internal Stroke glow, core, branch, outline, stamp;
            internal readonly Vector3[] path = new Vector3[9], fork = new Vector3[5];
        }

        /// <summary>Frozen authoritative path. Cosmetic spread never selects or damages targets.
        /// endpoint ends the beam sleeve; groundPoint is the independently resolved frozen
        /// spread center. groundNormal and hasGround describe that ground contact, not an enemy
        /// hit. No ground is inferred from endpoint. GroundStroke sampling happens once per
        /// event and is clipped inside the supplied radius around groundPoint.
        /// </summary>
        public void LaunchPulse(uint id, int sequence, Vector3 origin, Vector3 endpoint, Vector3 groundPoint,
            Vector3 groundNormal, bool hasGround, float elapsed, float travelDuration, float spreadRadius,
            float spreadDuration, bool finale)
        {
            if (!Finite(origin) || !Finite(endpoint) || !Finite(groundPoint) || !Finite(groundNormal) || !Finite(spreadRadius) ||
                !Finite(spreadDuration) || spreadDuration <= 0f ||
                !Accept(id, sequence, elapsed, travelDuration, launches)) return;
            float travel = Mathf.Max(0.04f, travelDuration), spread = Mathf.Max(0.08f, spreadDuration);
            if (elapsed >= travel + spread + 0.18f) return;
            var p = pulses[nextPulse++ % pulses.Length];
            p.active = true; p.at = Time.time - Mathf.Max(0f, elapsed);
            p.seed = unchecked((int)id * 397 ^ sequence);
            p.origin = origin; p.end = endpoint; p.length = Vector3.Distance(origin, endpoint);
            p.direction = p.length > 0.001f ? (endpoint - origin) / p.length : direction;
            p.groundPoint = groundPoint;
            p.normal = groundNormal.sqrMagnitude > 0.001f ? groundNormal.normalized : Vector3.zero;
            p.travel = travel; p.spread = spread;
            p.radius = Mathf.Clamp(spreadRadius, 0f, 30f); // cosmetic ceiling only
            p.finale = finale && fullEntry;
            p.ground = hasGround && p.normal.y > 0.35f;
            p.sleeve.Hide(); p.spine.Hide(); p.front.Hide(); p.outline.Hide();
            foreach (var fork in p.forks) fork.Hide();
            for (int i = 0; i < p.roots.Length; i++)
            {
                var g = p.roots[i]; g.count = 0; g.stroke.Hide(); g.outline.Hide();
                if (p.ground) BuildGround(p, g, i, p.seed);
            }
        }

        /// <summary>One authoritative confirmed weak strike. No target search or falling sky column.</summary>
        public void ConfirmStrike(uint id, int sequence, Vector3 targetPosition, float elapsed)
        {
            if (!Finite(targetPosition) || !Accept(id, sequence, elapsed, 0.26f, strikes) || elapsed >= 0.26f) return;
            var s = contacts[nextStrike++ % contacts.Length]; s.active = true;
            s.at = Time.time - Mathf.Max(0f, elapsed);
            Vector3 top = targetPosition + Vector3.up * 1.65f;
            RaycastHit ceiling;
            if (Physics.Raycast(targetPosition, Vector3.up, out ceiling, 1.65f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                top = ceiling.point - Vector3.up * 0.08f;
            Vector3 side = Vector3.Cross(direction, Vector3.up).normalized;
            if (side.sqrMagnitude < 0.1f) side = Vector3.right;
            for (int i = 0; i < s.path.Length; i++)
            {
                float t = i / (float)(s.path.Length - 1);
                float jag = Mathf.Sin(t * Mathf.PI) * (Noise(sequence, i) - 0.5f) * 0.42f;
                s.path[i] = Vector3.Lerp(top, targetPosition, t) + side * jag;
            }
            for (int i = 0; i < s.fork.Length; i++)
            {
                float t = i / (float)(s.fork.Length - 1);
                s.fork[i] = Vector3.Lerp(s.path[3], targetPosition + side * 0.42f, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.12f;
            }
            if (EnableAudio && elapsed < 0.08f && Time.time >= nextSound)
            {
                nextSound = Time.time + 0.12f;
                // pooled target-position object keeps the cue spatial, without a new emitter per hit
                s.glow.line.transform.position = targetPosition;
                Util.PlaySound(GazeSfx.ForkHit, s.glow.line.gameObject);
            }
        }

        // Stable per-event variation; never consumes Unity's shared random state.
        private static float Noise(int seed, int sample)
        {
            uint x = unchecked((uint)seed * 747796405u + (uint)sample * 2891336453u + 277803737u);
            x = (x ^ (x >> 16)) * 2246822519u;
            return (x & 65535u) / 65535f;
        }

        private static void BuildGround(Pulse p, GroundStroke g, int index, int seed)
        {
            int sector = index / 2;
            float angle = sector * Mathf.PI / 3f + Noise(seed, 0) * 6.28f + (Noise(seed, sector + 1) - 0.5f) * 0.65f;
            Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 side = new Vector3(-radial.z, 0f, radial.x);
            bool branch = index % 2 == 1;
            float radius = Mathf.Max(0f, p.radius - 0.3f); // includes the wider outline half-width
            float reach = radius * (0.72f + 0.28f * Noise(seed, sector + 11));
            g.delay = Noise(seed, sector + 21) * 0.12f;
            var parent = branch ? p.roots[index - 1] : null;
            if (branch && parent.count < 5) return;
            Vector3 branchStart = branch ? parent.path[4] - p.groundPoint : Vector3.zero;
            Vector3 previous = p.groundPoint;
            for (int j = 0; j < g.path.Length; j++)
            {
                float t = j / (float)(g.path.Length - 1);
                float tooth = (Noise(seed, index * 19 + j + 40) - 0.5f) * 0.22f;
                float handedness = Noise(seed, sector + 31) < 0.5f ? -1f : 1f;
                Vector3 offset = branch ? Vector3.Lerp(branchStart, (radial * 0.78f + side * handedness * 0.42f) * reach, t) +
                    side * Mathf.Sin(t * Mathf.PI) * tooth * reach :
                    radial * t * reach + side * Mathf.Sin(t * Mathf.PI) * tooth * reach;
                offset = Vector3.ClampMagnitude(offset, radius);
                Vector3 sample = p.groundPoint + offset;
                sample.y = j > 0 ? previous.y : (branch ? parent.path[4].y : p.groundPoint.y);
                RaycastHit hit;
                if (!Physics.Raycast(sample + Vector3.up * 0.55f, Vector3.down, out hit, 1.4f,
                    LayerIndex.world.mask, QueryTriggerInteraction.Ignore) || hit.normal.y <= 0.35f) break;
                Vector3 point = hit.point + hit.normal * 0.07f;
                if (Vector3.Distance(point, p.groundPoint) > radius) break;
                if (j > 0 && (Mathf.Abs(point.y - previous.y) > 0.65f ||
                    Physics.Linecast(previous, point, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))) break;
                g.path[g.count] = point;
                g.distance[g.count] = Mathf.Max(g.count > 0 ? g.distance[g.count - 1] : 0f, Vector3.Distance(point, p.groundPoint));
                g.count++; previous = point;
            }
        }

        private void RenderPulses()
        {
            foreach (var p in pulses)
            {
                if (!p.active) continue;
                float age = Time.time - p.at;
                if (age < p.travel)
                {
                    float t = Mathf.Clamp01(age / p.travel);
                    float head = p.length * t;
                    float tail = Mathf.Max(0f, head - Mathf.Min(7f, p.length * 0.45f));
                    for (int j = 0; j < p.line.Length; j++) p.line[j] = p.origin + p.direction * Mathf.Lerp(tail, head, j / (float)(p.line.Length - 1));
                    float scale = (p.finale ? 1.16f : 1f) * Mathf.SmoothStep(0.35f, 1f, age / 0.1f);
                    p.outline.Draw(p.line, p.line.Length, 4.2f * scale, outlineTint, 0.28f);
                    p.sleeve.Draw(p.line, p.line.Length, 3.85f * scale, accent, ReducedEffects ? 0.22f : 0.32f);
                    Vector3 u, v; Basis(p.direction, out u, out v);
                    int tick = (int)(age * (ReducedEffects ? 8f : 18f));
                    for (int j = 1; j < p.line.Length - 1; j++)
                    {
                        float envelope = Mathf.Sin(j * Mathf.PI / (p.line.Length - 1));
                        p.line[j] += (u * (Noise(p.seed + tick, j) - 0.5f) + v * (Noise(p.seed + tick, j + 20) - 0.5f)) * (0.95f * scale * envelope);
                    }
                    p.spine.Draw(p.line, p.line.Length, 0.14f * scale, accentEdge, ReducedEffects ? 0.5f : 0.8f);
                    for (int branch = 0; branch < p.forks.Length; branch++)
                    {
                        if (ReducedEffects && branch == 1) { p.forks[branch].Hide(); continue; }
                        var points = p.forkPoints[branch];
                        Vector3 start = p.line[3 + branch * 2];
                        Vector3 end = p.line[10] + u * ((branch == 0 ? -1f : 1f) * 0.85f * scale);
                        for (int j = 0; j < points.Length; j++)
                        {
                            float f = j / (float)(points.Length - 1);
                            points[j] = Vector3.Lerp(start, end, f) + v * (Mathf.Sin(f * Mathf.PI) * (Noise(p.seed + tick, j + branch * 9) - 0.5f) * scale);
                        }
                        p.forks[branch].Draw(points, points.Length, 0.055f * scale, palette.Secondary, 0.65f);
                    }
                    p.front.Knot(p.origin + p.direction * head, p.direction, 1.1f * scale, p.seed % 31,
                        0.07f, accentEdge, 0.7f, ReducedEffects);
                    continue;
                }
                p.sleeve.Hide(); p.spine.Hide(); p.front.Hide(); p.outline.Hide();
                foreach (var fork in p.forks) fork.Hide();
                float spreadAge = age - p.travel;
                // A short endpoint punctuation starts at supplied arrival time, never before.
                // It is energy arrival; target-specific white-free strikes still require confirmation.
                float arrival = 1f - Mathf.Clamp01(spreadAge / 0.18f);
                if (arrival > 0f) p.front.Knot(p.end, p.direction, 0.35f + (1f - arrival) * 0.6f,
                    p.seed % 31, 0.065f, accentEdge, arrival * (ReducedEffects ? 0.4f : 0.65f), ReducedEffects);
                float fade = 1f - Mathf.Clamp01((spreadAge - p.spread * 0.6f) / (p.spread * 0.4f + 0.18f));
                for (int i = 0; i < p.roots.Length; i++)
                {
                    var g = p.roots[i];
                    if (!p.ground || (ReducedEffects && i % 2 == 1)) { g.stroke.Hide(); g.outline.Hide(); continue; }
                    float wave = Mathf.Clamp01((spreadAge / p.spread - g.delay) / (1f - g.delay)) * p.radius;
                    float behind = Mathf.Max(0f, (spreadAge / p.spread - g.delay - 0.38f) * p.radius);
                    int count = 0;
                    // A moving lit section lashes out along the prevalidated terrain path.
                    // Its wake disappears instead of leaving a static radial star until fadeout.
                    for (int j = 1; j < g.count; j++)
                    {
                        float a = g.distance[j - 1], b = g.distance[j];
                        if (b < behind || a > wave || b - a < 0.0001f) continue;
                        if (count == 0) g.shown[count++] = Vector3.Lerp(g.path[j - 1], g.path[j], Mathf.InverseLerp(a, b, behind));
                        g.shown[count++] = Vector3.Lerp(g.path[j - 1], g.path[j], Mathf.InverseLerp(a, b, wave));
                    }
                    // Only colored roots: white belongs to confirmed target strikes.
                    float width = (i % 2 == 0 ? 0.3f : 0.12f) * (p.finale ? 1.2f : 1f);
                    float crackle = ReducedEffects ? 0.85f : 0.76f + 0.24f * Mathf.Sin(spreadAge * 75f + i * 2.1f + p.seed % 31);
                    g.outline.Draw(g.shown, count, width + 0.16f, GazeContrastAssets.Ink, fade * crackle * 0.62f);
                    g.stroke.Draw(g.shown, count, width, i % 2 == 0 ? accent : palette.Secondary, fade * crackle * (ReducedEffects ? 0.5f : 0.75f));
                    Color tip = accentEdge; tip.a = fade * crackle * 0.8f;
                    g.stroke.line.endColor = tip;
                }
                if (spreadAge >= p.spread + 0.18f) p.active = false;
            }
        }

        private void RenderStrikes()
        {
            foreach (var s in contacts)
            {
                if (!s.active) continue;
                float age = Time.time - s.at;
                float fade = 1f - Mathf.Clamp01(age / 0.26f);
                s.outline.Draw(s.path, s.path.Length, 0.5f, GazeContrastAssets.Ink, fade * 0.8f);
                s.glow.Draw(s.path, s.path.Length, 0.3f, accent, fade * 0.8f);
                s.core.Draw(s.path, s.path.Length, 0.07f, ReducedEffects ? accent : accentEdge, fade * (ReducedEffects ? 0.5f : 0.9f));
                s.stamp.Knot(s.path[s.path.Length - 1], direction, 0.2f + 0.22f * (1f - fade), s.at, 0.04f, accentEdge, fade * 0.65f, ReducedEffects);
                if (ReducedEffects) s.branch.Hide();
                else s.branch.Draw(s.fork, s.fork.Length, 0.075f, accent, fade * 0.6f);
                if (age >= 0.26f) s.active = false;
            }
        }
    }
}
