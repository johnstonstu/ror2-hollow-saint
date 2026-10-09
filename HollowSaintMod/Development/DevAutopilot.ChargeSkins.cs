using System.Collections;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    internal sealed partial class DevAutopilot
    {
        private IEnumerator ChargeSkinSegments()
        {
            var model = pilot.modelLocator.modelTransform.GetComponent<ModelSkinController>();
            var special = pilot.skillLocator.special;
            if (!model) { ReleaseCheck(false, "skin controller present"); yield break; }
            int original = (int)pilot.skinIndex;
            special.SetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
            for (int i = 0; i < model.skins.Length; i++)
            {
                pilot.skinIndex = (uint)i; model.ApplySkin(i);
                yield return Segment("charge-skin-" + i);
                trace.AppendLine("CHARGE_SKIN index=" + i + " name=" + model.skins[i].name + " arc=" + SkinFxPalette.ForBody(pilot).Arc);
                pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
                aimTarget = DummyChest(0); fire2 = true; yield return Wait(.9f); Shot("orb-skin-" + i);
                fire2 = false; yield return Wait(1.5f);
                pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
                fire4 = true; yield return Wait(1.4f); fire4 = false;
                yield return Wait(1.35f); ChargeWideShot("cloud-skin-" + i);
                yield return Wait(2f);
                ReleaseCheck(!StoredChargeState.IsGathering(pilot), "skin cast recovers " + i);
            }
            pilot.skinIndex = (uint)original; model.ApplySkin(original);
            special.UnsetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
            foreach (var slot in new[] { pilot.skillLocator.primary, pilot.skillLocator.secondary, pilot.skillLocator.utility, pilot.skillLocator.special })
            {
                string text = Language.GetString(slot.skillDef.skillDescriptionToken);
                ReleaseCheck(!text.Contains("{") && text != slot.skillDef.skillDescriptionToken, "native formatted skill description " + slot.skillDef.skillName);
                trace.AppendLine("CHARGE_DESCRIPTION " + slot.skillDef.skillName + " " + text);
            }
        }
    }
}
