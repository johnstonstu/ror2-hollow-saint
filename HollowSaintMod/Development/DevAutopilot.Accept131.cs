using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// HS_SEGMENTS=accept-131: native in-game acceptance for the 1.3.1 playtest build. Orb Backup
    /// Magazine hits, Gaze focus ramp and knockback immunity, Thundercloud early end and refund.
    /// Stationary fixtures and an invulnerable pilot; not survival or multiplayer QA.
    /// </summary>
    internal sealed partial class DevAutopilot
    {
        private readonly List<float> focusSamples = new List<float>();
        private readonly List<float> focusTimes = new List<float>();
        private CharacterBody focusVictim;
        private void RecordFocus(HealthComponent victim, float multiplier)
        {
            if (focusVictim && victim == focusVictim.healthComponent) { focusSamples.Add(multiplier); focusTimes.Add(scriptTime); }
        }
        private void Toughen(CharacterBody body)
        {
            if (!body) return;
            body.inventory.GiveItemPermanent(RoR2Content.Items.BoostHp, 200);
            body.RecalculateStats(); body.healthComponent.HealFraction(1f, default(ProcChainMask));
            body.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
        }

        private IEnumerator AcceptOrbMagazine()
        {
            yield return BalanceTarget("accept-orb-magazine", KitTuning.ArcBoltDamageCoefficient, 4.5f);
            if (!balanceVictim) yield break;
            Toughen(balanceVictim);
            var secondary = pilot.skillLocator.secondary;
            secondary.SetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
            pilot.inventory.GiveItemPermanent(RoR2Content.Items.SecondarySkillMagazine, 2); pilot.RecalculateStats();
            yield return Wait(.3f);
            ReleaseCheck(secondary.bonusStockFromBody == 2, "two Backup Magazines register as bonus stock");
            ReleaseCheck(secondary.maxStock == 1, "Orb stays a single cast with Backup Magazines");
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 0);
            chargeSource = DamageSource.Secondary; chargeReports.Clear(); recordingCharges = true;
            aimTarget = balanceVictim.corePosition; Clip(segment, true);
            fire2 = true; yield return AimAtLive(.28f); fire2 = false;
            yield return Wait(5f); recordingCharges = false; Clip(segment, false);
            int hits = chargeReports.Count(r => r.victimBody == balanceVictim && Mathf.Abs(r.damageInfo.procCoefficient - .3f) > .001f);
            int expected = ChargedStormTuning.HitBudget(0, 2);
            trace.AppendLine("ACCEPT_ORB_MAGAZINE hits=" + hits + " expected=" + expected);
            ReleaseCheck(hits == expected, "free Orb with two magazines lands " + expected + " hits on a lone target (actual " + hits + ")");
            pilot.inventory.RemoveItemPermanent(RoR2Content.Items.SecondarySkillMagazine, 2); pilot.RecalculateStats();
            secondary.UnsetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
            Shot("accept-orb-magazine-end");
        }

        private IEnumerator AcceptGazeFocus()
        {
            yield return BalanceTarget("accept-gaze-focus", KitTuning.ArcBoltDamageCoefficient, 14f);
            if (!balanceVictim) yield break;
            Toughen(balanceVictim);
            var special = pilot.skillLocator.special;
            var gaze = FoundationKit.Gaze.GazeRegistration.SkillDef;
            special.SetSkillOverride(this, gaze, GenericSkill.SkillOverridePriority.Replacement);
            special.Reset();
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 0);
            focusVictim = balanceVictim; focusSamples.Clear(); focusTimes.Clear();
            FoundationKit.Gaze.GazeServer.FocusTrace = RecordFocus;
            aimTarget = balanceVictim.corePosition; Clip(segment, true);
            yield return Press(4);
            yield return Wait(GazeTuningWindup() + 3.3f);
            int steady = focusSamples.Count;
            float peak = focusSamples.Count > 0 ? focusSamples.Max() : 0f;
            trace.AppendLine("ACCEPT_GAZE_FOCUS samples=" + steady + " first=" + (steady > 0 ? focusSamples[0] : 0f) + " peak=" + peak);
            ReleaseCheck(steady >= 10, "core beam keeps hitting the focused target (" + steady + " hits)");
            ReleaseCheck(steady > 0 && focusSamples[0] < 1.15f, "focus starts near no bonus");
            ReleaseCheck(peak >= 1f + FoundationKit.Gaze.GazeFocusPolicy.MaxBonus - .05f, "focus reaches the full bonus after ~3 s (peak " + peak + ")");

            // Knockback immunity while channeling: a big shove must not move the Saint.
            var motor = pilot.characterMotor;
            Vector3 before = motor.velocity;
            motor.ApplyForce(Vector3.forward * 20000f, true, false);
            yield return null;
            float kicked = (motor.velocity - before).magnitude;
            trace.AppendLine("ACCEPT_GAZE_KNOCKBACK delta=" + kicked);
            ReleaseCheck(kicked < 3f, "no knockback while Gaze channels (velocity change " + kicked + ")");

            // Look well away (the Titan is huge) past the grace and fade, then back: after a real gap
            // in core hits, the first hit back must show focus has faded.
            Vector3 toVictim = balanceVictim.corePosition - pilot.corePosition;
            aimTarget = pilot.corePosition + Quaternion.Euler(0f, 85f, 0f) * toVictim + Vector3.up * 8f;
            yield return Wait(2.4f);
            aimTarget = balanceVictim.corePosition;
            yield return Wait(.8f);
            int gapAt = -1; float gap = 0f;
            for (int i = 1; i < focusTimes.Count; i++)
                if (focusTimes[i] - focusTimes[i - 1] > gap) { gap = focusTimes[i] - focusTimes[i - 1]; gapAt = i; }
            float resumed = gapAt > 0 ? focusSamples[gapAt] : -1f;
            trace.AppendLine("ACCEPT_GAZE_FOCUS_RESUME gap=" + gap + " first=" + resumed + " samples=" + focusSamples.Count);
            ReleaseCheck(gap >= 1.5f, "the beam actually left the target for at least 1.5 s (gap " + gap + ")");
            ReleaseCheck(resumed > 0f && resumed < 1.35f, "focus faded after looking away (resumed at " + resumed + ")");
            yield return Wait(3f); Clip(segment, false);
            FoundationKit.Gaze.GazeServer.FocusTrace = null; focusVictim = null;
            special.UnsetSkillOverride(this, gaze, GenericSkill.SkillOverridePriority.Replacement);

            // Outside Gaze, knockback still applies normally.
            yield return Wait(1.5f);
            before = motor.velocity; motor.ApplyForce(Vector3.forward * 20000f, true, false); yield return null;
            float normal = (motor.velocity - before).magnitude;
            trace.AppendLine("ACCEPT_KNOCKBACK_NORMAL delta=" + normal);
            ReleaseCheck(normal > 3f, "knockback still applies outside Gaze");
            Shot("accept-gaze-end");
        }
        private static float GazeTuningWindup() => FoundationKit.Gaze.GazeTuning.WindupSeconds;

        private IEnumerator AcceptCloudDismiss()
        {
            ClearLive(); yield return Segment("accept-cloud-dismiss"); Take(null);
            SpawnLive("LemurianMaster", 3, 10f); yield return Wait(1.5f);
            foreach (var body in Alive()) Toughen(body);
            var special = pilot.skillLocator.special;
            special.SetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
            special.Reset();
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 0);
            chargeSource = DamageSource.Special; chargeReports.Clear(); recordingCharges = true;
            Clip(segment, true);
            yield return Wait(2.5f);
            var crown = EntityStateMachine.FindByCustomName(pilot.gameObject, KitRegistration.CrownMachineName);
            trace.AppendLine("ACCEPT_CLOUD_READY skill=" + (special.skillDef ? special.skillDef.skillName : "none") + " canExecute=" + special.CanExecute() +
                " stock=" + special.stock + " crown=" + (crown && crown.state != null ? crown.state.GetType().Name : "none") + " live=" + Alive().Count);
            var first = Alive().FirstOrDefault();
            aimTarget = first ? first.footPosition + Vector3.up * .5f : (Vector3?)null;
            yield return Wait(.8f);
            fire4 = true; yield return Wait(.15f); fire4 = false;
            yield return Wait(2.2f);
            int struck = chargeReports.Count;
            ReleaseCheck(struck > 0, "free storm strikes before the dismiss (" + struck + ")");
            Shot("accept-cloud-before-dismiss");
            yield return Press(4);
            yield return Wait(.6f);
            int atDismiss = chargeReports.Count;
            yield return Wait(4f);
            recordingCharges = false; Clip(segment, false);
            trace.AppendLine("ACCEPT_CLOUD_DISMISS before=" + struck + " atDismiss=" + atDismiss + " after=" + chargeReports.Count +
                " stock=" + special.stock + " remaining=" + special.cooldownRemaining + " interval=" + special.CalculateFinalRechargeInterval());
            ReleaseCheck(chargeReports.Count == atDismiss, "dismissed storm stops striking");
            ReleaseCheck(!StoredChargeState.IsGathering(pilot), "storm cast state has ended");
            ReleaseCheck(special.stock > 0 || special.cooldownRemaining < special.CalculateFinalRechargeInterval() - 4.6f - 1.5f,
                "early end refunded part of the cooldown");
            special.UnsetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
            Shot("accept-cloud-end");
        }

        private IEnumerator Accept131Segments()
        {
            isolateReviewFixtures = true;
            foreach (var dummy in dummies) if (dummy) { dummy.healthComponent.godMode = false; dummy.healthComponent.Suicide(); } dummies.Clear();
            StartCoroutine(CameraFollow()); SetCameraDistance(8.5f, .5f);
            float oldCrit = pilot.baseCrit;
            GlobalEventManager.onServerDamageDealt += RecordChargeDamage;
            try
            {
                ReleaseCheck(KitTuning.ArcBoltDamageCoefficient == 1.9f, "1.3.1 Arc Bolt default migrated (190% raw)");
                ReleaseCheck(Mathf.Abs(FoundationKit.Gaze.GazeTuning.Range - 90f) < .01f, "Gaze range 90 m");
                yield return AcceptOrbMagazine();
                yield return AcceptGazeFocus();
                yield return AcceptCloudDismiss();
            }
            finally
            {
                GlobalEventManager.onServerDamageDealt -= RecordChargeDamage;
                FoundationKit.Gaze.GazeServer.FocusTrace = null;
                pilot.baseCrit = oldCrit; pilot.RecalculateStats();
            }
        }
    }
}
