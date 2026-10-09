using System.Collections;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    internal sealed partial class DevAutopilot
    {
        private IEnumerator ChargedStormPresentationSegments()
        {
            var secondary = pilot.skillLocator.secondary; var special = pilot.skillLocator.special;
            secondary.SetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
            special.SetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
            foreach (var dummy in dummies)
            {
                dummy.inventory.GiveItemPermanent(RoR2Content.Items.BoostHp, 1000);
                dummy.RecalculateStats(); dummy.healthComponent.HealFraction(1f, default(ProcChainMask));
                dummy.healthComponent.godMode = false;
                dummy.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            }
            GlobalEventManager.onServerDamageDealt += RecordChargeDamage;
            try
            {
                yield return ChargeMovingTarget(); yield return ChargeMovingTarget(true);
                foreach (int count in new[] { 1, 3, 5, 5, 5 })
                {
                    yield return Segment("charge-cloud-presentation-" + count);
                    pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
                    aimTarget = DummyChest(0); chargeSource = DamageSource.Special;
                    chargeReports.Clear(); recordingCharges = true;
                    fire4 = true; yield return Wait(.2f + (count - 1) * .3f);
                    trace.AppendLine("CLOUD_RELEASE charges=" + count + " eye=" + KitUtil.EyePosition(pilot) + " direction=" + pilot.inputBank.aimDirection + " aimed=" + aimTarget);
                    foreach (var dummy in dummies)
                        trace.AppendLine("CLOUD_VICTIM charges=" + count + " core=" + dummy.corePosition + " alive=" + dummy.healthComponent.alive);
                    fire4 = false;
                    yield return Wait(1.12f); ChargeWideShot("cloud-first-strike-" + count);
                    yield return Wait(.45f); ChargeWideShot("cloud-middle-strike-" + count);
                    yield return Wait(.45f); ChargeWideShot("cloud-last-strike-" + count);
                    yield return Wait(1f); recordingCharges = false;
                    ReleaseCheck(pilot.GetComponent<DischargeMeter>().Charge == 5 - count, "presentation cloud spends exact charge count " + count);
                    ReleaseCheck(chargeReports.Count == 3, "presentation cloud retains three native victim hits " + count);
                    ReleaseCheck(!StoredChargeState.IsGathering(pilot), "presentation cloud returns control " + count);
                }
            }
            finally
            {
                recordingCharges = false; GlobalEventManager.onServerDamageDealt -= RecordChargeDamage;
                secondary.UnsetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
                special.UnsetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
            }
        }
    }
}
