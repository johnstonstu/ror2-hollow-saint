using System.Collections;
using HollowSaint.FoundationKit;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// v0.9.15 (HS_SEGMENTS=thunder): Gaze armor check (armor before / during / after the channel),
    /// then a forced Thunderbolt filmed from a far side camera every 0.15 s from the gather to the
    /// strike, so the slower rise, hang, streak and strike can be reviewed frame by frame.
    /// </summary>
    internal sealed partial class DevAutopilot
    {
        private IEnumerator ThunderSegments()
        {
            var special = pilot && pilot.skillLocator ? pilot.skillLocator.special : null;
            var gaze = FoundationKit.Gaze.GazeRegistration.SkillDef;
            if (special && gaze)
            {
                if (crownOverride && KitRegistration.OpenCircuitDef) { special.UnsetSkillOverride(this, KitRegistration.OpenCircuitDef, GenericSkill.SkillOverridePriority.Replacement); crownOverride = false; }
                special.SetSkillOverride(this, gaze, GenericSkill.SkillOverridePriority.Replacement);
                yield return Segment("gaze-armor");
                trace.AppendLine("GAZE_ARMOR before=" + pilot.armor.ToString("0"));
                aimTarget = DummyChest(0);
                yield return Press(4); yield return Wait(1.6f);
                trace.AppendLine("GAZE_ARMOR during=" + pilot.armor.ToString("0"));
                yield return Wait(5f);
                trace.AppendLine("GAZE_ARMOR after=" + pilot.armor.ToString("0"));
                special.UnsetSkillOverride(this, gaze, GenericSkill.SkillOverridePriority.Replacement);
            }

            yield return Segment("thunder");
            aimTarget = null;
            var meter = pilot.GetComponent<DischargeMeter>();
            if (!meter) { trace.AppendLine("THUNDER no meter"); yield break; }
            for (int i = 0; i < 30 && !meter.IsFull; i++) meter.AddCharge();
            trace.AppendLine("THUNDER charged=" + meter.Charge);
            for (int k = 0; k < 18; k++)
            {
                FarShot("t-" + k.ToString("00"));
                yield return Wait(0.15f);
            }
            yield return Wait(1f);
        }

        private void FarShot(string name) { lastShotReal = Time.realtimeSinceStartup; StartCoroutine(FarShotRoutine(name)); }

        private IEnumerator FarShotRoutine(string name)
        {
            trace.AppendLine(scriptTime.ToString("000.00") + " SHOT " + name);
            yield return new WaitForEndOfFrame();
            if (!shotCamera || !pilot) yield break;
            Vector3 focus = mark + facing * 6f + Vector3.up * 7f;
            Render(name, focus - facing * 6f + Right * 22f + Vector3.up * 2f, focus);
        }
    }
}
