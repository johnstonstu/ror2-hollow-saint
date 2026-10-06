using System.Collections;
using System.Collections.Generic;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.OpenCircuit.Fx;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>HS_SEGMENTS=circuit-terrain: finds a slope, a wall, a cliff edge, rocky and flat
    /// ground near the arena, opens Open Circuit on each, captures the ground lightning (behind,
    /// top-down) and logs per-branch reach.</summary>
    internal sealed partial class DevAutopilot
    {
        private IEnumerator CircuitTerrainSegments()
        {
            var found = FindTerrainSpots();
            foreach (var kv in found) trace.AppendLine("TERRAIN_SPOT " + kv.Key + " at=" + kv.Value.Key + " face=" + kv.Value.Value);
            DumpLines("idle-before-circuit");
            var slot = pilot.skillLocator.special;
            if (!KitRegistration.OpenCircuitDef) { trace.AppendLine("TERRAIN no Open Circuit def"); errors++; yield break; }
            slot.SetSkillOverride(this, KitRegistration.OpenCircuitDef, GenericSkill.SkillOverridePriority.Replacement);
            Vector3 savedFacing = facing;
            foreach (var kv in found)
            {
                yield return Segment("circuit-terrain-" + kv.Key);
                Vector3 spot = kv.Value.Key; facing = kv.Value.Value;
                TeleportHelper.TeleportBody(pilot, spot);
                if (pilot.characterMotor) pilot.characterMotor.velocity = Vector3.zero;
                if (pilot.characterDirection) pilot.characterDirection.forward = facing;
                aimTarget = spot + facing * 10f + Vector3.up;
                yield return Wait(0.5f);
                pilot.skillLocator.ResetSkills();
                yield return Press(4); yield return Wait(1.5f);
                var dome = pilot.GetComponent<OpenCircuitDomeFx>();
                for (int k = 0; k < 3; k++)
                {
                    Shot("ct-" + kv.Key + "-" + k);
                    if (k == 1)
                    {
                        TopShot("ct-" + kv.Key + "-top");
                        // Native gameplay-camera frame: separates real artifacts from harness-camera ones.
                        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(output, "ct-" + kv.Key + "-native.png"));
                    }
                    yield return Wait(0.11f);
                }
                trace.AppendLine("TERRAIN_BRANCHES " + kv.Key + " " + (dome ? dome.DebugBranches() : "no dome"));
                DumpLines("terrain-" + kv.Key);
                yield return WaitCrownEnd();
            }
            facing = savedFacing;
            slot.UnsetSkillOverride(this, KitRegistration.OpenCircuitDef, GenericSkill.SkillOverridePriority.Replacement);
        }

        private void TopShot(string name)
        {
            lastShotReal = Time.realtimeSinceStartup;
            StartCoroutine(TopShotRoutine(name));
        }

        private IEnumerator TopShotRoutine(string name)
        {
            yield return new WaitForEndOfFrame();
            if (!shotCamera || !pilot) yield break;
            Vector3 foot = pilot.footPosition;
            Render(name, foot + Vector3.up * 13f - facing * 5f, foot + facing * 1f);
        }

        private static bool Ground(Vector3 at, out RaycastHit hit) =>
            Physics.Raycast(at + Vector3.up * 30f, Vector3.down, out hit, 60f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);

        private Dictionary<string, KeyValuePair<Vector3, Vector3>> FindTerrainSpots()
        {
            var spots = new Dictionary<string, KeyValuePair<Vector3, Vector3>>();
            var dirs = new Vector3[8];
            for (int i = 0; i < 8; i++) dirs[i] = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
            for (float r = 4f; r <= 46f && spots.Count < 5; r += 1.5f)
                for (int a = 0; a < 48; a++)
                {
                    Vector3 p = mark + Quaternion.Euler(0f, a * 7.5f, 0f) * Vector3.forward * r;
                    RaycastHit hit;
                    if (!Ground(p, out hit) || Mathf.Abs(hit.point.y - mark.y) > 12f) continue;
                    Vector3 g = hit.point; float ny = hit.normal.y;
                    float minH = 0f, maxH = 0f; int missing = 0; Vector3 lowDir = Vector3.zero; float lowest = 0f;
                    Vector3 wallDir = Vector3.zero;
                    foreach (var d in dirs)
                    {
                        RaycastHit n;
                        if (Ground(g + d * 3f, out n)) { float dh = n.point.y - g.y; minH = Mathf.Min(minH, dh); maxH = Mathf.Max(maxH, dh); if (dh < lowest) { lowest = dh; lowDir = d; } }
                        else { missing++; lowDir = d; lowest = -99f; }
                        if (wallDir == Vector3.zero && Physics.Raycast(g + Vector3.up * 1f, d, 2.5f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) wallDir = d;
                    }
                    Vector3 face = Vector3.ProjectOnPlane(mark - g, Vector3.up).normalized;
                    if (face == Vector3.zero) face = Vector3.forward;
                    if (!spots.ContainsKey("slope") && ny > 0.55f && ny < 0.82f) spots["slope"] = Pair(g, Vector3.ProjectOnPlane(-hit.normal, Vector3.up).normalized * -1f);
                    else if (!spots.ContainsKey("wall") && ny > 0.9f && wallDir != Vector3.zero) spots["wall"] = Pair(g, wallDir);
                    else if (!spots.ContainsKey("edge") && ny > 0.9f && lowest < -4f) spots["edge"] = Pair(g, lowDir);
                    else if (!spots.ContainsKey("rocky") && ny > 0.75f && maxH - minH > 1.0f && maxH - minH < 3f && wallDir == Vector3.zero) spots["rocky"] = Pair(g, face);
                    else if (!spots.ContainsKey("flat") && ny > 0.97f && maxH - minH < 0.25f && wallDir == Vector3.zero && missing == 0) spots["flat"] = Pair(g, face);
                }
            return spots;
        }

        private static KeyValuePair<Vector3, Vector3> Pair(Vector3 at, Vector3 face) =>
            new KeyValuePair<Vector3, Vector3>(at + Vector3.up * 0.2f, face.sqrMagnitude > 0.01f ? face.normalized : Vector3.forward);
    }
}
