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
                // Beam is up with the charges primed in the crown; RT fires the opening pulse.
                yield return Wait(1.0f); Shot("gc-primed-" + h);
                fire1 = true; yield return Wait(0.03f);
                Time.timeScale = 0.25f;
                yield return Wait(0.03f); Shot("gc-fire-" + h);
                yield return Wait(0.06f); Shot("gc-wave1-" + h);
                yield return Wait(0.07f); Shot("gc-wave2-" + h);
                yield return Wait(0.1f); Shot("gc-boom-" + h); WideShot("gc-boom-wide-" + h);
                Time.timeScale = 1f;
                fire1 = false;
                yield return Wait(2.5f);
                trace.AppendLine("GAZE_CHARGE_RESULT hold=" + hold + " bankBefore=" + before + " bankNow=" + (meter ? meter.Charge : -1));
                yield return Press(4); yield return Wait(1.5f);
            }
            slot.UnsetSkillOverride(this, GazeRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
        }
    }
}
