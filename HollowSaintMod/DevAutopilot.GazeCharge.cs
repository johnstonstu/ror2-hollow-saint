using System.Collections;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.Gaze;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>HS_SEGMENTS=gaze-charge: hold Special to absorb charges into the crown, release
    /// for the opening blast. Tap / short / full holds against the dummies.</summary>
    internal sealed partial class DevAutopilot
    {
        private IEnumerator GazeChargeSegments()
        {
            var slot = pilot.skillLocator.special;
            slot.SetSkillOverride(this, GazeRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
            var meter = pilot.GetComponent<DischargeMeter>();
            foreach (var hold in new[] { 0.05f, 0.55f, 2.2f })
            {
                yield return Segment("gaze-charge-" + hold.ToString("0.00"));
                pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
                aimTarget = DummyChest(0);
                yield return Wait(0.3f);
                int before = meter ? meter.Charge : -1;
                fire4 = true;
                if (hold > 1f)
                {
                    yield return Wait(0.45f); Shot("gc-gather-2");
                    yield return Wait(0.6f); Shot("gc-gather-4");
                    yield return Wait(hold - 1.05f); Shot("gc-gather-full");
                }
                else yield return Wait(hold);
                fire4 = false;
                string h = hold.ToString("0.0");
                yield return Wait(0.05f); Shot("gc-release-" + h);
                // The opening fires on its own at ignition (windup 0.7 s after the hand-off).
                yield return Wait(0.66f);
                Time.timeScale = 0.25f;
                yield return Wait(0.03f); Shot("gc-fire-" + h);
                yield return Wait(0.06f); Shot("gc-wave1-" + h);
                yield return Wait(0.07f); Shot("gc-wave2-" + h);
                yield return Wait(0.1f); Shot("gc-boom-" + h); WideShot("gc-boom-wide-" + h);
                Time.timeScale = 1f;
                // A minor surge aimed far away: the under-strike should hit the dummies below.
                yield return Wait(1.0f);
                aimTarget = null; aimPitch = 6f;
                yield return Wait(0.3f);
                fire1 = true; yield return Wait(0.6f); fire1 = false;
                yield return Wait(0.22f); Shot("gc-under-" + h); WideShot("gc-under-wide-" + h);
                aimPitch = 0f; aimTarget = DummyChest(0);
                yield return Wait(2.0f);
                trace.AppendLine("GAZE_CHARGE_RESULT hold=" + hold + " bankBefore=" + before + " bankNow=" + (meter ? meter.Charge : -1));
                yield return Press(4); yield return Wait(1.5f);
            }
            slot.UnsetSkillOverride(this, GazeRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
        }
    }
}
