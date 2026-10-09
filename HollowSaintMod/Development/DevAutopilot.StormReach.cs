using System;
using System.Collections;
using System.Linq;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.Thundercloud;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    internal sealed partial class DevAutopilot
    {
        private readonly int[] stormStrokeCounts = new int[ThundercloudSchedule.ReturnStrokeCount];
        private bool recordingStormStrokes;
        private void RecordStormStroke(int index)
        {
            if (!recordingStormStrokes) return;
            stormStrokeCounts[index]++;
            trace.AppendLine("CLOUD_RETURN_STROKE index=" + index + " time=" + scriptTime);
        }
        private IEnumerator StormReachCloud(int charges, bool solo)
        {
            string name = solo ? "cloud-return-solo" : "cloud-return-pack";
            SetCameraDistance(25f, 10f); cameraWant = facing;
            if (solo) yield return NativeRelayTarget(name, 10f);
            else yield return ReviewPack(name, 10f);
            int victims = Alive().Count;
            Array.Clear(stormStrokeCounts, 0, stormStrokeCounts.Length);
            chargeSource = DamageSource.Special; chargeReports.Clear(); recordingCharges = recordingStormStrokes = true;
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
            Clip(name, true); fire4 = true; yield return AimAtLive(.2f + (charges - 1) * .3f);
            fire4 = false; trace.AppendLine("CLOUD_RELEASE time=" + scriptTime);
            yield return Wait(.9f); Shot(name + "-rise");
            yield return Wait(.5f); Shot(name + "-first");
            yield return Wait(.6f); Shot(name + "-return");
            yield return Wait(1.4f); Shot(name + "-sweep");
            yield return Wait(2f); Shot(name + "-last");
            yield return Wait(1.5f); Clip(name, false); recordingCharges = recordingStormStrokes = false;
            ReleaseCheck(victims == (solo ? 1 : 6), "storm fixture has intended distinct victims " + name);
            ReleaseCheck(chargeReports.Count == victims && chargeReports.Select(r => r.victim).Distinct().Count() == victims,
                "slow cloud damages each victim once " + name);
            for (int i = 0; i < stormStrokeCounts.Length; i++)
                ReleaseCheck(stormStrokeCounts[i] == victims, "cloud visual return stroke " + i + " reaches each victim " + name);
            ReleaseCheck(pilot.GetComponent<DischargeMeter>().Charge == 5 - charges, "slow cloud spends gathered charges once " + name);
        }
        private IEnumerator StormReachPair()
        {
            ClearLive(); yield return Segment("wide-orb-fixture");
            var prefab = MasterCatalog.FindMasterPrefab("LemurianMaster");
            foreach (float distance in new[] { 10f, 15f, 20f, 26f, 30f })
            {
                ClearLive();
                foreach (float side in new[] { -13f, 13f })
                {
                    Vector3 spot = TopGround(pilot.footPosition + facing * distance + Right * side);
                    var master = new MasterSummon { masterPrefab = prefab, position = spot, rotation = Quaternion.LookRotation(-facing),
                        teamIndexOverride = TeamIndex.Monster, ignoreTeamMemberLimit = true,
                        preSpawnSetupCallback = m => isolatedMasters.Add(m) }.Perform();
                    if (!master) { ReleaseCheck(false, "wide Orb fixture master exists"); continue; }
                    foreach (var ai in master.GetComponents<RoR2.CharacterAI.BaseAI>()) ai.SetBaseAIEnabled(false);
                    StartCoroutine(TrackLive(master, spot));
                }
                yield return Wait(1.5f);
                var bodies = Alive();
                bool clear = bodies.Count == 2 && WideOrbLaneClear(bodies[0], bodies[1]);
                trace.AppendLine("WIDE_ORB_PLACEMENT forward=" + distance + " clear=" + clear);
                if (clear) break;
            }
            foreach (var body in Alive())
            {
                body.inventory.GiveItemPermanent(RoR2Content.Items.BoostHp, 1000);
                body.RecalculateStats(); body.healthComponent.HealFraction(1f, default(ProcChainMask));
                body.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            }
        }
        private bool WideOrbLaneClear(CharacterBody a, CharacterBody b)
        {
            Vector3 delta = b.corePosition - a.corePosition;
            int mask = LayerIndex.world.mask;
            foreach (var body in new[] { a, b })
            {
                Vector3 aim = (body.corePosition - KitUtil.EyePosition(pilot)).normalized;
                Vector3 muzzle = HollowSaint.FoundationKit.HollowedOrb.OrbCastGeometry.Point(pilot, aim, 5);
                Vector3 launch = body.corePosition - muzzle;
                if (Physics.CheckSphere(muzzle, .4f, mask, QueryTriggerInteraction.Ignore) ||
                    Physics.SphereCastAll(muzzle, .4f, launch.normalized, launch.magnitude, mask, QueryTriggerInteraction.Ignore).Length != 0) return false;
            }
            return delta.magnitude > 18f && delta.magnitude < 36f &&
                !Physics.CheckSphere(a.corePosition, .4f, mask, QueryTriggerInteraction.Ignore) &&
                !Physics.CheckSphere(b.corePosition, .4f, mask, QueryTriggerInteraction.Ignore) &&
                ChargedStormTargeting.Clear(a.corePosition, b.corePosition) &&
                Physics.SphereCastAll(a.corePosition, .4f, delta.normalized, delta.magnitude, mask, QueryTriggerInteraction.Ignore).Length == 0;
        }
        private IEnumerator StormReachOrb(int charges)
        {
            yield return StormReachPair();
            string name = "wide-orb-" + charges;
            yield return Segment(name); SetCameraDistance(12f, 2f); cameraWant = facing;
            var victims = Alive().OrderBy(b => Vector3.Dot(b.corePosition - pilot.corePosition, Right)).ToArray();
            ReleaseCheck(victims.Length == 2, "wide Orb has exactly two targets");
            if (victims.Length != 2) yield break;
            float gap = Vector3.Distance(victims[0].corePosition, victims[1].corePosition);
            trace.AppendLine("WIDE_ORB_GAP distance=" + gap + " reach=" + ChargedStormTuning.BounceRange(charges));
            ReleaseCheck(gap > ChargedStormTuning.BounceRange(1) && gap < ChargedStormTuning.BounceRange(5) &&
                WideOrbLaneClear(victims[0], victims[1]), "wide pair bridges only empowered reach with clear world path");
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
            chargeSource = DamageSource.Secondary; chargeReports.Clear(); recordingCharges = true;
            aimTarget = victims[0].corePosition; Clip(name, true); fire2 = true;
            yield return Wait(charges == 0 ? .28f : .57f + (charges - 1) * .3f);
            Shot(name + "-gather"); fire2 = false;
            yield return Wait(2f); Shot(name + "-bounce"); yield return Wait(6f);
            Clip(name, false); recordingCharges = false;
            int expected = charges == 0 ? 1 : ChargedStormTuning.HitBudget(charges);
            ReleaseCheck(chargeReports.Count == expected, "wide Orb finite hit budget " + charges + " actual=" + chargeReports.Count);
            ReleaseCheck(chargeReports.Count > 0 && chargeReports[0].victimBody == victims[0], "wide Orb respects aimed first victim");
            if (charges == 5)
                for (int i = 0; i < chargeReports.Count; i++)
                    ReleaseCheck(chargeReports[i].victimBody == victims[i % 2], "wide Orb alternates real distant victims " + i);
            ReleaseCheck(pilot.GetComponent<DischargeMeter>().Charge == 5 - charges, "wide Orb spends only gathered fuel " + charges);
        }
        private IEnumerator StormReachSegments()
        {
            // Controlled mapped casts: presentation pulse counts, once-only damage,
            // broad visible target separation and finite charge-dependent reach.
            isolateReviewFixtures = true; Plugin.HideBuildTag = true;
            foreach (var dummy in dummies) if (dummy) { dummy.healthComponent.godMode = false; dummy.healthComponent.Suicide(); }
            dummies.Clear(); StartCoroutine(CameraFollow());
            syncFlashUntil = Time.realtimeSinceStartup + .25f;
            trace.AppendLine("SYNC utc=" + DateTime.UtcNow.ToString("o")); yield return Wait(1f);
            var secondary = pilot.skillLocator.secondary; var special = pilot.skillLocator.special;
            var previous = ChargedStormStrikeFx.DiagnosticStroke;
            var previousFlight = HollowSaint.FoundationKit.HollowedOrb.ServerHollowedOrb.DiagnosticTrace;
            secondary.SetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
            special.SetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
            GlobalEventManager.onServerDamageDealt += RecordChargeDamage;
            ChargedStormStrikeFx.DiagnosticStroke = RecordStormStroke;
            HollowSaint.FoundationKit.HollowedOrb.ServerHollowedOrb.DiagnosticTrace = message => trace.AppendLine("WIDE_ORB_FLIGHT " + message);
            try
            {
                yield return StormReachCloud(1, true);
                yield return StormReachCloud(5, false);
                yield return StormReachOrb(0);
                yield return StormReachOrb(5);
            }
            finally
            {
                fire1 = fire2 = fire3 = fire4 = recordingCharges = recordingStormStrokes = false;
                ChargedStormStrikeFx.DiagnosticStroke = previous;
                HollowSaint.FoundationKit.HollowedOrb.ServerHollowedOrb.DiagnosticTrace = previousFlight;
                GlobalEventManager.onServerDamageDealt -= RecordChargeDamage;
                secondary.UnsetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
                special.UnsetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
                ClearLive();
            }
        }
    }
}
