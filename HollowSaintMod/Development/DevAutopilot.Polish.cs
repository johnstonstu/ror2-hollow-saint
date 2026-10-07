using System.Collections;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// v0.9.16 (HS_SEGMENTS=polish): Arc Step look lift (height gained looking level, 45 degrees up,
    /// and down from the air) and the spear impact, tap and full charge, filmed close every 0.05 s
    /// from the release through the stick and the burst.
    /// </summary>
    internal sealed partial class DevAutopilot
    {
        private IEnumerator PolishSegments()
        {
            foreach (float pitch in new[] { 0f, -45f })
            {
                yield return Segment("step-" + (pitch < 0f ? "up" : "level"));
                aimPitch = pitch; move = facing;
                yield return Wait(0.2f);
                float y0 = pilot.footPosition.y, peak = y0;
                fire3 = true; yield return Wait(0.1f); fire3 = false;
                for (float t = 0f; t < 0.8f; t += 0.05f) { peak = Mathf.Max(peak, pilot.footPosition.y); yield return Wait(0.05f); }
                move = Vector3.zero;
                trace.AppendLine("STEP_LIFT pitch=" + (-pitch).ToString("0") + " rise=" + (peak - y0).ToString("0.00") + "m");
                yield return Wait(1f);
            }
            yield return Segment("step-air-down");
            jump = true; yield return Wait(0.1f); jump = false; yield return Wait(0.35f);
            aimPitch = 40f; move = facing;
            float a0 = pilot.footPosition.y;
            fire3 = true; yield return Wait(0.1f); fire3 = false;
            yield return Wait(0.3f);
            trace.AppendLine("STEP_LIFT pitch=-40 air dy=" + (pilot.footPosition.y - a0).ToString("0.00") + "m");
            move = Vector3.zero;
            yield return Wait(1.2f);

            foreach (bool full in new[] { false, true })
            {
                yield return Segment(full ? "impact-full" : "impact-tap");
                aimTarget = DummyChest(0);
                fire2 = true;
                yield return Wait(full ? 2.3f : 0.12f);
                fire2 = false;
                for (int k = 0; k < 16; k++)
                {
                    ImpactShot((full ? "m-full-" : "m-tap-") + k.ToString("00"));
                    yield return Wait(0.05f);
                }
                yield return Wait(1f);
            }
        }

        private void ImpactShot(string name) { lastShotReal = Time.realtimeSinceStartup; StartCoroutine(ImpactShotRoutine(name)); }

        private IEnumerator ImpactShotRoutine(string name)
        {
            trace.AppendLine(scriptTime.ToString("000.00") + " SHOT " + name);
            yield return new WaitForEndOfFrame();
            if (!shotCamera || !pilot) yield break;
            Vector3 target = DummyChest(0);
            Render(name, target - facing * 3f + Right * 7f + Vector3.up * 2.5f, target + Vector3.up * 1.2f);
        }
    }
}
