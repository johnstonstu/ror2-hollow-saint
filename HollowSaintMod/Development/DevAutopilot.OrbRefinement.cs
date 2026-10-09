using System.Collections;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.HollowedOrb;
using HollowSaint.FoundationKit.OpenCircuit;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    internal sealed partial class DevAutopilot
    {
        private int refinementPrimaryHits;
        private bool recordRefinementPrimary;
        private void RecordRefinementPrimary(DamageReport report)
        {
            if (recordRefinementPrimary && report.attackerBody == pilot && report.damageInfo.damageType.damageSource == DamageSource.Primary)
                refinementPrimaryHits++;
        }
        private IEnumerator OrbRefinementSegments()
        {
            // Success: useful free casts preserve fuel; deliberate holds spend
            // exact tiers; real Primary works during overhead Orb gathering;
            // Circuit pulse density grows with paid fuel at unchanged per-hit damage.
            var secondary = pilot.skillLocator.secondary; var special = pilot.skillLocator.special;
            var meter = pilot.GetComponent<DischargeMeter>();
            float circuitCooldown = OpenCircuitRegistration.SkillDef.baseRechargeInterval;
            secondary.SetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
            special.SetSkillOverride(this, OpenCircuitRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
            foreach (var dummy in dummies)
            {
                dummy.inventory.GiveItemPermanent(RoR2Content.Items.BoostHp, 1000);
                dummy.RecalculateStats(); dummy.healthComponent.HealFraction(1f, default(ProcChainMask));
                dummy.healthComponent.godMode = false; dummy.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            }
            GlobalEventManager.onServerDamageDealt += RecordChargeDamage;
            GlobalEventManager.onServerDamageDealt += RecordRefinementPrimary;
            try
            {
                foreach (int count in new[] { 0, 0, 1, 3, 5 })
                {
                    int bank = segment == "orb-refine-0-bank0" ? 5 : count == 0 ? 0 : 5;
                    yield return Segment("orb-refine-" + count + "-bank" + bank);
                    pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, bank); aimTarget = DummyChest(0);
                    ReleaseCheck(secondary.CanExecute(), "Orb native admission with bank " + bank);
                    chargeSource = DamageSource.Secondary; chargeReports.Clear(); recordingCharges = true;
                    fire2 = true; yield return Wait(count == 0 ? .30f : .57f + (count - 1) * .3f);
                    Shot("orb-refine-gather-" + count + "-bank" + bank); fire2 = false;
                    yield return Wait(.2f); WideShot("orb-refine-release-" + count + "-bank" + bank);
                    yield return Wait(3f); recordingCharges = false;
                    ReleaseCheck(meter.Charge == bank - count, "Orb exact optional spend " + count + " bank=" + bank);
                    ReleaseCheck(chargeReports.Count == ChargedStormTuning.HitBudget(count), "Orb native finite hits at tier " + count);
                    if (chargeReports.Count > 0)
                        ReleaseCheck(Mathf.Abs(chargeReports[0].damageInfo.damage / pilot.damage - KitDamagePolicy.Effective(ChargedStormTuning.OrbCoefficient(count))) < .02f,
                            "Orb native base/empowered damage " + count);
                }
                int basePulses = 0;
                // Short native recharge simulates strong cooldown reduction:
                // it must still wait for gathering and the crown window.
                OpenCircuitRegistration.SkillDef.baseRechargeInterval = .25f;
                foreach (int count in new[] { 1, 3, 5 })
                {
                    yield return Segment("circuit-refine-" + count);
                    TeleportHelper.TeleportBody(pilot, mark + facing * 4f); yield return Wait(.15f);
                    pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5); aimTarget = DummyChest(0);
                    fire4 = true; yield return Wait(.2f + (count - 1) * .3f);
                    ReleaseCheck(special.stock == 0 && special.rechargeStopwatch < .03f, "Circuit cannot recharge stock during gather " + count);
                    Shot("circuit-gather-" + count); fire4 = false;
                    yield return Wait(.34f);
                    ReleaseCheck(meter.Charge == 5 - count, "Circuit exact committed spend before pulse " + count);
                    ReleaseCheck(!pilot.HasBuff(OpenCircuitBuff.Def), "Circuit respects authored activation delay " + count);
                    yield return Wait(1.1f);
                    ReleaseCheck(pilot.HasBuff(OpenCircuitBuff.Def) && OpenCircuitBuff.Charges(pilot) == count, "Circuit native confirmed strength " + count);
                    chargeSource = DamageSource.Special; chargeReports.Clear(); recordingCharges = true;
                    yield return Wait(1.5f); recordingCharges = false;
                    trace.AppendLine("CIRCUIT_DENSITY charges=" + count + " reports=" + chargeReports.Count + " interval=" + CircuitChargePolicy.Interval(KitTuning.OpenCircuitPulseInterval, count));
                    if (count == 1) basePulses = chargeReports.Count;
                    ReleaseCheck(chargeReports.Count >= 6, "Circuit native area pulse damage exists " + count);
                    if (count == 5) ReleaseCheck(chargeReports.Count >= basePulses * 1.6f, "five-charge Circuit increases native area strike density");
                    foreach (var report in chargeReports)
                        ReleaseCheck(Mathf.Abs(report.damageInfo.damage / pilot.damage - KitDamagePolicy.Effective(KitTuning.OpenCircuitPulseDamageCoefficient)) < .02f,
                            "Circuit per-pulse damage remains baseline");
                    ChargeWideShot("circuit-area-" + count);
                    if (count == 5)
                    {
                        refinementPrimaryHits = 0; recordRefinementPrimary = true;
                        pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5); fire1 = fire2 = true;
                        yield return Wait(.9f); Shot("orb-circuit-free-hands");
                        ReleaseCheck(StoredChargeState.IsGathering(pilot) && refinementPrimaryHits > 0,
                            "real Primary hits while overhead Orb gathers");
                        var point = OrbCastGeometry.Point(pilot, pilot.inputBank.aimDirection, 2);
                        ReleaseCheck(point.y > KitUtil.EyePosition(pilot).y + .3f, "Circuit Orb native point above head");
                        fire1 = fire2 = false; recordRefinementPrimary = false;
                        yield return Wait(.3f); Shot("orb-circuit-refined-launch");
                    }
                    yield return WaitCrownEnd(); yield return Wait(.5f);
                }
                yield return ChargeMovingTarget(); yield return ChargeMovingTarget(true);
                yield return ChargeCircuitExpiry();
                foreach (var slot in new[] { secondary, special })
                    ReleaseCheck(!Language.GetString(slot.skillDef.skillDescriptionToken).Contains("{"), "refined native descriptions formatted");
            }
            finally
            {
                recordingCharges = recordRefinementPrimary = false;
                OpenCircuitRegistration.SkillDef.baseRechargeInterval = circuitCooldown;
                GlobalEventManager.onServerDamageDealt -= RecordChargeDamage;
                GlobalEventManager.onServerDamageDealt -= RecordRefinementPrimary;
                secondary.UnsetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
                special.UnsetSkillOverride(this, OpenCircuitRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
            }
        }
    }
}
