using System.Collections;
using System.Collections.Generic;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.HollowedOrb;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    internal sealed partial class DevAutopilot
    {
        private readonly List<DamageReport> chargeReports = new List<DamageReport>();
        private DamageSource chargeSource;
        private bool recordingCharges;

        // Success criteria: native admission/spending, 3/5/7 finite hits with fresh
        // victims first, one cloud hit per victim, correct coefficients/procs,
        // overhead Circuit geometry, cancellation and restoration. These are solo
        // mapped-action trials, not physical-controller or two-client acceptance.
        private IEnumerator ChargedStormSegments()
        {
            var secondary = pilot.skillLocator.secondary;
            var special = pilot.skillLocator.special;
            var meter = pilot.GetComponent<DischargeMeter>();
            secondary.SetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
            special.SetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
            foreach (var dummy in dummies)
            {
                dummy.inventory.GiveItemPermanent(RoR2Content.Items.BoostHp, 1000);
                dummy.RecalculateStats(); dummy.healthComponent.HealFraction(1f, default(ProcChainMask));
                dummy.healthComponent.godMode = false;
                // Relocated fixtures must not accumulate unrelated falling damage.
                dummy.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            }
            GlobalEventManager.onServerDamageDealt += RecordChargeDamage;
            try
            {
                foreach (int count in new[] { 1, 3, 5 })
                {
                    yield return Segment("charge-orb-" + count);
                    pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
                    aimTarget = DummyChest(0); chargeSource = DamageSource.Secondary; chargeReports.Clear(); recordingCharges = true;
                    fire2 = true; yield return Wait(.57f + (count - 1) * .3f);
                    ReleaseCheck(meter.Charge == 5 && StoredChargeState.IsGathering(pilot), "Orb holds bank until release " + count);
                    Shot("orb-loaded-" + count); yield return Wait(.02f); fire2 = false;
                    yield return Wait(.34f); WideShot("orb-flight-" + count);
                    yield return Wait(4f); recordingCharges = false;
                    ReleaseCheck(meter.Charge == 5 - count, "Orb spent exactly " + count);
                    ReleaseCheck(chargeReports.Count == ChargedStormTuning.HitBudget(count), "Orb finite hit budget " + count + " actual=" + chargeReports.Count);
                    if (chargeReports.Count >= 3)
                        ReleaseCheck(chargeReports[0].victim != chargeReports[1].victim && chargeReports[1].victim != chargeReports[2].victim && chargeReports[0].victim != chargeReports[2].victim, "Orb fresh A B C before revisits");
                    if (chargeReports.Count > 0)
                        ReleaseCheck(Mathf.Abs(chargeReports[0].damageInfo.damage / pilot.damage - KitDamagePolicy.Effective(ChargedStormTuning.OrbCoefficient(count))) < .02f, "Orb native damage coefficient " + count);
                    ReleaseCheck(!StoredChargeState.IsGathering(pilot), "Orb returns to native idle");
                    Shot("orb-restored-" + count);
                }
                yield return Segment("charge-orb-two-targets");
                dummies[2].teamComponent.teamIndex = TeamIndex.Player;
                pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
                chargeSource = DamageSource.Secondary; chargeReports.Clear(); recordingCharges = true;
                aimTarget = DummyChest(0); fire2 = true; yield return Wait(1.83f); fire2 = false; yield return Wait(4f);
                recordingCharges = false;
                ReleaseCheck(chargeReports.Count == 7, "two-target Orb still has seven finite hits");
                for (int i = 2; i < chargeReports.Count; i++)
                    ReleaseCheck(chargeReports[i].victim == chargeReports[i - 2].victim && chargeReports[i].victim != chargeReports[i - 1].victim, "two-target A B A B fallback");
                dummies[2].teamComponent.teamIndex = TeamIndex.Monster;
                yield return ChargeMovingTarget();
                yield return ChargeMovingTarget(true);
                foreach (int count in new[] { 1, 3, 5 })
                {
                    yield return Segment("charge-cloud-" + count);
                    foreach (var dummy in dummies)
                        trace.AppendLine("CLOUD_FIXTURE charges=" + count + " body=" + dummy.name + " core=" + dummy.corePosition +
                            " alive=" + dummy.healthComponent.alive + " health=" + dummy.healthComponent.health + " team=" + dummy.teamComponent.teamIndex);
                    pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
                    aimTarget = DummyChest(0); chargeSource = DamageSource.Special; chargeReports.Clear(); recordingCharges = true;
                    fire4 = true; yield return Wait(.2f + (count - 1) * .3f);
                    Shot("cloud-loaded-" + count); fire4 = false;
                    yield return Wait(.7f); ChargeWideShot("cloud-rise-" + count);
                    yield return Wait(.65f); ChargeWideShot("cloud-strike-" + count); DumpEffects("cloud-volume-" + count, pilot.corePosition, 60f);
                    yield return Wait(2f); recordingCharges = false;
                    ReleaseCheck(meter.Charge == 5 - count, "cloud spent exactly " + count);
                    ReleaseCheck(chargeReports.Count == dummies.Count, "cloud hits every eligible dummy once " + count + " actual=" + chargeReports.Count);
                    var seen = new HashSet<HealthComponent>();
                    foreach (var report in chargeReports)
                    {
                        ReleaseCheck(seen.Add(report.victim), "cloud no repeated victim");
                        ReleaseCheck(Mathf.Abs(report.damageInfo.damage / pilot.damage - KitDamagePolicy.Effective(ChargedStormTuning.CloudCoefficient(count))) < .02f, "cloud native damage coefficient " + count);
                        ReleaseCheck(Mathf.Abs(report.damageInfo.procCoefficient - .5f) < .001f, "cloud proc coefficient");
                    }
                    ReleaseCheck(!StoredChargeState.IsGathering(pilot), "cloud ends its one rolling sequence");
                    Shot("cloud-restored-" + count);
                }
                yield return Segment("charge-empty");
                pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 0);
                yield return Press(2); yield return Press(4); yield return Wait(.5f);
                ReleaseCheck(!StoredChargeState.IsGathering(pilot) && secondary.stock == 0 && special.stock == 1 && meter.Charge == 0, "empty bank permits free Orb while cloud preserves stock");
                yield return Segment("charge-utility-cancel");
                pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
                fire2 = true; yield return Wait(.5f); yield return Press(3); fire2 = false; yield return Wait(1f);
                ReleaseCheck(meter.Charge == 5 && secondary.stock == 1 && !StoredChargeState.IsGathering(pilot), "Utility cancels Orb without spending and returns stock");
                yield return Segment("charge-circuit-orb");
                special.UnsetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
                special.SetSkillOverride(this, KitRegistration.OpenCircuitDef, GenericSkill.SkillOverridePriority.Replacement);
                special.Reset(); pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
                fire4 = true; yield return Wait(.2f); fire4 = false; yield return Wait(1.6f);
                pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
                aimTarget = DummyChest(0); fire2 = true; yield return Wait(1.23f);
                var point = OrbCastGeometry.Point(pilot, pilot.inputBank.aimDirection.normalized, 3);
                ReleaseCheck(CrownOpen() && point.y > KitUtil.EyePosition(pilot).y + .3f, "Circuit Orb actual gather/launch point above head");
                trace.AppendLine("ORB_OVERHEAD eye=" + KitUtil.EyePosition(pilot) + " muzzle=" + point);
                Shot("orb-circuit-loaded"); fire2 = false; yield return Wait(.34f); WideShot("orb-circuit-flight");
                yield return Wait(4f); ReleaseCheck(meter.Charge == 2, "Circuit Orb spends three, leaves two");
                yield return WaitCrownEnd(); Shot("orb-circuit-restored");
                yield return ChargeCircuitExpiry();
                special.UnsetSkillOverride(this, KitRegistration.OpenCircuitDef, GenericSkill.SkillOverridePriority.Replacement);
                yield return ChargeSkinSegments();
            }
            finally
            {
                recordingCharges = false; GlobalEventManager.onServerDamageDealt -= RecordChargeDamage;
                secondary.UnsetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
                special.UnsetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
            }
        }

        private void RecordChargeDamage(DamageReport report)
        {
            if (!recordingCharges || report.attackerBody != pilot || report.damageInfo.damageType.damageSource != chargeSource) return;
            chargeReports.Add(report);
            trace.AppendLine("CHARGE_DAMAGE segment=" + segment + " victim=" + (report.victimBody ? report.victimBody.name : "lost") +
                " id=" + report.victim.GetInstanceID() + " offered=" + report.damageInfo.damage + " dealt=" + report.damageDealt + " proc=" + report.damageInfo.procCoefficient);
        }

        private void ChargeWideShot(string name) { StartCoroutine(ChargeWideRoutine(name)); }
        private IEnumerator ChargeWideRoutine(string name)
        {
            yield return new WaitForEndOfFrame();
            var center = pilot.corePosition + facing * 10f;
            Render(name, pilot.corePosition - facing * 25f + Right * 16f + Vector3.up * 10f, center + Vector3.up * 6f);
            lastShotReal = Time.realtimeSinceStartup;
        }
    }
}
