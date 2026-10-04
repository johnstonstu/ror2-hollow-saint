using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    public sealed partial class GazeEmpowermentFx
    {
        private sealed class GroundStroke
        {
            internal Stroke stroke, outline;
            internal readonly Vector3[] path = new Vector3[9], shown = new Vector3[9];
            internal readonly float[] distance = new float[9];
            internal int count;
        }
        private sealed class Pulse
        {
            internal bool active, ground, finale;
            internal float at, travel, spread, radius, length;
            internal Vector3 origin, end, groundPoint, direction, normal;
            internal Stroke sleeve, spine, front, outline;
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
            p.origin = origin; p.end = endpoint; p.length = Vector3.Distance(origin, endpoint);
            p.direction = p.length > 0.001f ? (endpoint - origin) / p.length : direction;
            p.groundPoint = groundPoint;
            p.normal = groundNormal.sqrMagnitude > 0.001f ? groundNormal.normalized : Vector3.zero;
            p.travel = travel; p.spread = spread;
            p.radius = Mathf.Clamp(spreadRadius, 0f, 30f); // cosmetic ceiling only
            p.finale = finale && fullEntry;
            p.ground = hasGround && p.normal.y > 0.35f;
            p.sleeve.Hide(); p.spine.Hide(); p.front.Hide(); p.outline.Hide();
            for (int i = 0; i < p.roots.Length; i++)
            {
                var g = p.roots[i]; g.count = 0; g.stroke.Hide(); g.outline.Hide();
                if (p.ground) BuildGround(p, g, i, sequence);
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
                float jag = Mathf.Sin(t * Mathf.PI) * (i % 2 == 0 ? 0.16f : -0.16f);
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

        private static void BuildGround(Pulse p, GroundStroke g, int index, int seed)
        {
            float angle = (index / 2) * Mathf.PI / 3f + (seed % 13) * 0.07f;
            Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 side = new Vector3(-radial.z, 0f, radial.x);
            bool branch = index % 2 == 1;
            float radius = Mathf.Max(0f, p.radius - 0.3f); // includes the wider outline half-width
            var parent = branch ? p.roots[index - 1] : null;
            if (branch && parent.count < 5) return;
            Vector3 branchStart = branch ? parent.path[4] - p.groundPoint : Vector3.zero;
            Vector3 previous = p.groundPoint;
            for (int j = 0; j < g.path.Length; j++)
            {
                float t = j / (float)(g.path.Length - 1);
                Vector3 offset = branch ? Vector3.Lerp(branchStart, (radial * 0.86f + side * 0.32f) * radius, t) :
                    radial * t * radius + side * Mathf.Sin(t * Mathf.PI) * (j % 2 == 0 ? 0.08f : -0.08f) * radius;
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
                g.distance[g.count] = Vector3.Distance(point, p.groundPoint);
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
                    float tail = Mathf.Max(0f, head - Mathf.Min(5f, p.length * 0.3f));
                    for (int j = 0; j < p.line.Length; j++) p.line[j] = p.origin + p.direction * Mathf.Lerp(tail, head, j / (float)(p.line.Length - 1));
                    float scale = p.finale ? 1.16f : 1f;
                    p.outline.Draw(p.line, p.line.Length, 3.15f * scale, GazeContrastAssets.Ink, 0.36f);
                    p.sleeve.Draw(p.line, p.line.Length, 2.85f * scale, accent, ReducedEffects ? 0.45f : 0.62f);
                    p.spine.Draw(p.line, p.line.Length, 0.24f * scale, accentEdge, ReducedEffects ? 0.4f : 0.65f);
                    if (p.finale) p.front.Diamond(p.origin + p.direction * head, p.direction, 1.22f * scale, 0.12f, accentEdge, 0.85f);
                    else p.front.Loop(p.origin + p.direction * head, p.direction, 1.22f, 0.11f, accentEdge, 0.8f);
                    continue;
                }
                p.sleeve.Hide(); p.spine.Hide(); p.front.Hide(); p.outline.Hide();
                float spreadAge = age - p.travel;
                float progress = Mathf.Clamp01(spreadAge / p.spread);
                float fade = 1f - Mathf.Clamp01((spreadAge - p.spread * 0.6f) / (p.spread * 0.4f + 0.18f));
                for (int i = 0; i < p.roots.Length; i++)
                {
                    var g = p.roots[i];
                    if (!p.ground || (ReducedEffects && i % 2 == 1)) { g.stroke.Hide(); g.outline.Hide(); continue; }
                    float wave = progress * p.radius;
                    int count = 0;
                    for (int j = 0; j < g.count; j++)
                    {
                        if (g.distance[j] <= wave) g.shown[count++] = g.path[j];
                        else
                        {
                            if (j > 0 && count > 0)
                            {
                                float f = Mathf.InverseLerp(g.distance[j - 1], g.distance[j], wave);
                                g.shown[count++] = Vector3.Lerp(g.path[j - 1], g.path[j], f);
                            }
                            break;
                        }
                    }
                    // Only colored roots: white belongs to confirmed target strikes.
                    float width = (i % 2 == 0 ? 0.3f : 0.12f) * (p.finale ? 1.2f : 1f);
                    g.outline.Draw(g.shown, count, width + 0.16f, GazeContrastAssets.Ink, fade * 0.62f);
                    g.stroke.Draw(g.shown, count, width, accent, fade * (ReducedEffects ? 0.5f : 0.75f));
                    Color tip = accentEdge; tip.a = fade * 0.8f;
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
                s.stamp.Diamond(s.path[s.path.Length - 1], direction, 0.2f + 0.22f * (1f - fade), 0.055f, accentEdge, fade * 0.8f);
                if (ReducedEffects) s.branch.Hide();
                else s.branch.Draw(s.fork, s.fork.Length, 0.075f, accent, fade * 0.6f);
                if (age >= 0.26f) s.active = false;
            }
        }
    }
}
