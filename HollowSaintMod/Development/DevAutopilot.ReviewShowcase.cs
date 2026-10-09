using System;
using System.Collections;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.OpenCircuit;
using HollowSaint.FoundationKit.Stormspear;
using HollowSaint.FoundationKit.Gaze;
using System.Linq;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    internal sealed partial class DevAutopilot
    {
        private IEnumerator ReviewPack(string name, float distance = 6f)
        {
            ClearLive(); yield return Segment(name);
            SpawnLive("LemurianMaster", 6, distance, .8f); yield return Wait(1.5f);
            foreach (var body in Alive())
            {
                body.inventory.GiveItemPermanent(RoR2Content.Items.BoostHp, 1000);
                body.RecalculateStats(); body.healthComponent.HealFraction(1f, default(ProcChainMask));
                body.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            }
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
            aimTarget = LiveCentre();
        }
        private IEnumerator ReviewShowcaseSegments()
        {
            isolateReviewFixtures = true;
            // Real mapped casts with preloaded fuel and durable stationary packs;
            // visual/audio review footage, separate from itemless balance evidence.
            Plugin.HideBuildTag = true; RoR2.UI.HUD.cvHudEnable.SetBool(false);
            Application.quitting += () => RoR2.UI.HUD.cvHudEnable.SetBool(true);
            foreach (var dummy in dummies) if (dummy) { dummy.healthComponent.godMode = false; dummy.healthComponent.Suicide(); } dummies.Clear();
            StartCoroutine(CameraFollow()); SetCameraDistance(8.5f, .5f);
            syncFlashUntil = Time.realtimeSinceStartup + .25f;
            trace.AppendLine("SYNC utc=" + DateTime.UtcNow.ToString("o")); yield return Wait(1f);
            var secondary = pilot.skillLocator.secondary; var special = pilot.skillLocator.special;
            secondary.SetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
            special.SetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
            GlobalEventManager.onServerDamageDealt += RecordChargeDamage;
            try
            {
                foreach (int count in new[] { 0, 3, 5 })
                {
                    yield return ReviewPack("hollowed-orb-13-" + count);
                    chargeSource = DamageSource.Secondary; chargeReports.Clear(); recordingCharges = true;
                    Clip(segment, true); fire2 = true;
                    yield return AimAtLive(count == 0 ? .28f : .57f + (count - 1) * .3f);
                    Shot(segment + "-gather"); fire2 = false; yield return Wait(4f);
                    Clip(segment, false); recordingCharges = false;
                    ReleaseCheck(chargeReports.Count == ChargedStormTuning.HitBudget(count), "review Orb native hit budget " + count);
                }
                foreach (int count in new[] { 1, 3, 5 })
                {
                    SetCameraDistance(25f, 10f); cameraWant = facing;
                    yield return ReviewPack("thundercloud-13-" + count);
                    chargeSource = DamageSource.Special; chargeReports.Clear(); recordingCharges = true;
                    Clip(segment, true); fire4 = true; yield return AimAtLive(.2f + (count - 1) * .3f);
                    Shot(segment + "-gather"); fire4 = false; yield return Wait(.75f);
                    Shot(segment + "-rise"); yield return Wait(.65f); Shot(segment + "-strike");
                    yield return Wait(5.3f); Clip(segment, false); recordingCharges = false;
                    ReleaseCheck(chargeReports.Count == Alive().Count, "review cloud hits each standing victim once " + count);
                }
                SetCameraDistance(8.5f, .5f); cameraWant = null;
                special.UnsetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
                special.SetSkillOverride(this, OpenCircuitRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
                foreach (int count in new[] { 1, 3, 5 })
                {
                    yield return ReviewPack("circuit-overhead-orb-13-" + count, 5f);
                    Clip(segment, true); fire4 = true; yield return Wait(.2f + (count - 1) * .3f);
                    Shot(segment + "-gather"); fire4 = false; yield return Wait(1.6f);
                    ReleaseCheck(CrownOpen(), "review charged Circuit opens " + count);
                    pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
                    fire1 = fire2 = true; yield return AimAtLive(1.8f);
                    Shot(segment + "-overhead"); fire2 = false; yield return AimAtLive(3f);
                    fire1 = false; Clip(segment, false); yield return WaitCrownEnd();
                }
                special.UnsetSkillOverride(this, OpenCircuitRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
                secondary.UnsetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
                // Saved profile selections are Orb/Circuit. Select the named kit
                // explicitly so review labels cannot silently capture another skill.
                secondary.SetSkillOverride(this, StormspearRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
                yield return ReviewPack("stormspear-13"); Clip(segment, true);
                chargeSource = DamageSource.Secondary; chargeReports.Clear(); recordingCharges = true;
                fire2 = true; yield return AimAtLive(1f);
                ReleaseCheck(secondary.skillDef == StormspearRegistration.SkillDef &&
                    pilot.GetComponents<EntityStateMachine>().Any(m => m.state is StormspearChargeState), "review actually enters Stormspear charge");
                yield return AimAtLive(1.2f); fire2 = false; yield return Wait(3f); Clip(segment, false);
                recordingCharges = false; ReleaseCheck(chargeReports.Count > 0, "review Stormspear produces real damage");
                secondary.UnsetSkillOverride(this, StormspearRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
                special.SetSkillOverride(this, GazeRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
                chargeSource = DamageSource.Special; chargeReports.Clear(); recordingCharges = true;
                yield return GazeTake("gaze-13", null, 7f, .5f);
                recordingCharges = false; ReleaseCheck(chargeReports.Count > 0, "review Gaze produces real damage");
                special.UnsetSkillOverride(this, GazeRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
                yield return ReviewPack("arc-step-13"); Clip(segment, true);
                yield return Press(3); yield return Wait(.6f); aimPitch = 35f; aimTarget = null;
                yield return Press(3); yield return Wait(1.3f); Clip(segment, false);
            }
            finally
            {
                fire1 = fire2 = fire3 = fire4 = recordingCharges = false;
                GlobalEventManager.onServerDamageDealt -= RecordChargeDamage;
                secondary.UnsetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
                special.UnsetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
                special.UnsetSkillOverride(this, OpenCircuitRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
                secondary.UnsetSkillOverride(this, StormspearRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
                special.UnsetSkillOverride(this, GazeRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
                RoR2.UI.HUD.cvHudEnable.SetBool(true); ClearLive();
            }
        }
    }
}
