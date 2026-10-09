using System;
using System.Collections;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.HollowedOrb;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    internal sealed partial class DevAutopilot
    {
        private int relayPlayerReports;
        private IEnumerator NativeRelayTarget(string name, float distance)
        {
            ClearLive(); yield return Segment(name); Take(null);
            TeamManager.instance.SetTeamLevel(TeamIndex.Player, 1u);
            TeamManager.instance.SetTeamLevel(TeamIndex.Monster, 1u);
            pilot.baseCrit = 0f; pilot.RecalculateStats();
            pilot.healthComponent.HealFraction(1f, default(ProcChainMask));
            SpawnLive("LemurianMaster", 1, distance); yield return Wait(1.5f);
            balanceVictim = Alive().Count > 0 ? Alive()[0] : null;
            ReleaseCheck(balanceVictim != null, "native relay fixture exists " + name);
            if (!balanceVictim) yield break;
            // Durability only: all damage and repeat accounting still use native hits.
            balanceVictim.inventory.GiveItemPermanent(RoR2Content.Items.BoostHp, 100);
            balanceVictim.RecalculateStats(); balanceVictim.healthComponent.HealFraction(1f, default(ProcChainMask));
            balanceVictim.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            ReleaseCheck(ChargedStormTargeting.Find(pilot, pilot.corePosition, 18f, Vector3.forward, 180f, false).Count == 1,
                "native solo relay has exactly one nearby enemy " + name);
        }
        private void RecordRelayPlayer(DamageReport report)
        {
            if (!recordingCharges || report.victimBody != pilot) return;
            trace.AppendLine("RELAY_PLAYER_DAMAGE attacker=" + (report.attackerBody ? report.attackerBody.name : "none") +
                " source=" + report.damageInfo.damageType.damageSource + " dealt=" + report.damageDealt);
            if (report.attackerBody == pilot) relayPlayerReports++;
        }
        private IEnumerator NativeSoloRelay(int count, float distance, bool tallBoss = false)
        {
            string name = (tallBoss ? "orb-titan-" : "orb-solo-") + count + (distance > 8f ? "-far" : "-relay");
            yield return tallBoss ? BalanceTarget(name, 1.6f, distance) : NativeRelayTarget(name, distance);
            if (!balanceVictim) yield break;
            float actualDistance = Vector3.Distance(pilot.corePosition, balanceVictim.corePosition);
            float proximity = Vector3.Distance(pilot.footPosition, balanceVictim.footPosition);
            int expected = proximity <= 8f && actualDistance <= ChargedStormTuning.BounceRange(count) ? ChargedStormTuning.HitBudget(count) : 1;
            trace.AppendLine("RELAY_FIXTURE distance=" + actualDistance + " proximity=" + proximity + " expected=" + expected);
            if (tallBoss) ReleaseCheck(proximity <= 8f && actualDistance > 8f && expected > 1,
                "native tall Titan is close by feet and eligible despite high core");
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
            float playerHealth = pilot.healthComponent.combinedHealth;
            bool playerGodMode = pilot.healthComponent.godMode;
            pilot.healthComponent.godMode = false;
            chargeSource = DamageSource.Secondary; chargeReports.Clear(); relayPlayerReports = 0; recordingCharges = true;
            Clip(name, true); aimTarget = balanceVictim.corePosition;
            fire2 = true; yield return AimAtLive(count == 0 ? .28f : .57f + (count - 1) * .3f);
            Shot(name + "-gather"); fire2 = false; yield return Wait(5f);
            recordingCharges = false; Clip(name, false);
            pilot.healthComponent.godMode = playerGodMode;
            trace.AppendLine("RELAY_PLAYER_HEALTH before=" + playerHealth + " after=" + pilot.healthComponent.combinedHealth);
            ReleaseCheck(chargeReports.Count == expected, "native solo finite relay budget " + name + " actual=" + chargeReports.Count);
            ReleaseCheck(relayPlayerReports == 0, "native relay never reports player damage " + name);
            ReleaseCheck(Mathf.Abs(pilot.healthComponent.combinedHealth - playerHealth) < .01f,
                "native relay preserves vulnerable player's health " + name);
            ReleaseCheck(pilot.GetComponent<DischargeMeter>().Charge == 5 - count, "native relay spends only gathered charges " + name);
            for (int i = 0; i < chargeReports.Count; i++)
                ReleaseCheck(Mathf.Abs(chargeReports[i].damageInfo.damage / pilot.damage -
                    KitDamagePolicy.Effective(ChargedStormTuning.OrbCoefficient(count)) * Mathf.Pow(.65f, i)) < .02f,
                    "native relay repeat damage " + name + " hit=" + i);
            Shot(name + "-end");
        }
        private IEnumerator NativeTwoRelay()
        {
            ClearLive(); yield return Segment("orb-two-player-relay");
            SpawnLive("LemurianMaster", 2, 4f); yield return Wait(.8f);
            foreach (var body in Alive())
            {
                body.inventory.GiveItemPermanent(RoR2Content.Items.BoostHp, 100);
                body.RecalculateStats(); body.healthComponent.HealFraction(1f, default(ProcChainMask));
                body.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            }
            ReleaseCheck(ChargedStormTargeting.Find(pilot, pilot.corePosition, 18f, Vector3.forward, 180f, false).Count == 2,
                "native two-target relay has exactly two nearby enemies");
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
            chargeSource = DamageSource.Secondary; chargeReports.Clear(); relayPlayerReports = 0; recordingCharges = true;
            Clip(segment, true); fire2 = true; yield return AimAtLive(1.8f); fire2 = false;
            yield return Wait(5f); recordingCharges = false; Clip(segment, false);
            ReleaseCheck(chargeReports.Count == 7 && relayPlayerReports == 0, "native two-enemy relay retains seven harmless finite hits");
            ReleaseCheck(chargeReports.Count >= 2 && chargeReports[0].victim != chargeReports[1].victim, "native two-enemy relay prioritizes fresh second victim");
            for (int i = 2; i < chargeReports.Count; i++)
                ReleaseCheck(chargeReports[i].victim == chargeReports[i - 2].victim && chargeReports[i].victim != chargeReports[i - 1].victim,
                    "native player relay preserves A B A B enemy order " + i);
            Shot("two-player-relay-end");
        }
        private IEnumerator NativeItemlessKill()
        {
            yield return BalanceTarget("itemless-titan-primary-free-orb", 1.6f, 4.5f);
            if (!balanceVictim) yield break;
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 0);
            balanceDamage = 0f; balanceHits = 0; balanceRecording = true;
            float began = scriptTime, nextOrb = began;
            Clip(segment, true); fire1 = true;
            while (balanceVictim && balanceVictim.healthComponent.alive && scriptTime - began < 90f)
            {
                aimTarget = balanceVictim.corePosition;
                if (scriptTime >= nextOrb && pilot.skillLocator.secondary.stock > 0)
                {
                    fire2 = true; yield return Wait(.28f); fire2 = false;
                    nextOrb = scriptTime + 7.8f;
                }
                yield return Wait(.1f);
            }
            fire1 = fire2 = balanceRecording = false; Clip(segment, false);
            trace.AppendLine("ITEMLESS_KILL seconds=" + (scriptTime - began) + " damage=" + balanceDamage + " hits=" + balanceHits +
                " killed=" + (!balanceVictim || !balanceVictim.healthComponent.alive));
            ReleaseCheck((!balanceVictim || !balanceVictim.healthComponent.alive) && scriptTime - began < 75f,
                "level-one no-item Titan killed within 75 seconds using Primary and bank-preserving free Orb");
            Shot("itemless-titan-defeated");
        }
        private IEnumerator EarlyAcceptanceSegments()
        {
            isolateReviewFixtures = true;
            // Native success: harmless finite owner relays for solo/two victims,
            // unchanged repeats/spend, far exclusion, and a real itemless kill.
            // Stationary AI-disabled fixtures / invulnerable pilot; not survival or multiplayer QA.
            foreach (var dummy in dummies) if (dummy) { dummy.healthComponent.godMode = false; dummy.healthComponent.Suicide(); } dummies.Clear();
            StartCoroutine(CameraFollow()); SetCameraDistance(8.5f, .5f);
            syncFlashUntil = Time.realtimeSinceStartup + .25f;
            trace.AppendLine("SYNC utc=" + DateTime.UtcNow.ToString("o")); yield return Wait(1f);
            var secondary = pilot.skillLocator.secondary;
            secondary.SetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
            float oldCrit = pilot.baseCrit;
            ServerHollowedOrb.DiagnosticTrace = text => trace.AppendLine("ORB_NATIVE " + segment + " " + text);
            GlobalEventManager.onServerDamageDealt += RecordBalanceDamage;
            GlobalEventManager.onServerDamageDealt += RecordChargeDamage;
            GlobalEventManager.onServerDamageDealt += RecordRelayPlayer;
            try
            {
                ReleaseCheck(KitTuning.ArcBoltDamageCoefficient == 1.6f, "native migrated Arc Bolt default is 144 percent");
                foreach (int count in new[] { 0, 1, 3, 5 }) yield return NativeSoloRelay(count, 6f);
                yield return NativeSoloRelay(5, 14f);
                yield return NativeTwoRelay();
                yield return NativeSoloRelay(0, 4.5f, true);
                yield return NativeSoloRelay(5, 4.5f, true);
                yield return NativeItemlessKill();
            }
            finally
            {
                fire1 = fire2 = fire4 = recordingCharges = balanceRecording = false;
                pilot.baseCrit = oldCrit;
                GlobalEventManager.onServerDamageDealt -= RecordBalanceDamage;
                GlobalEventManager.onServerDamageDealt -= RecordChargeDamage;
                GlobalEventManager.onServerDamageDealt -= RecordRelayPlayer;
                secondary.UnsetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
                ClearLive();
                ServerHollowedOrb.DiagnosticTrace = null;
            }
        }
    }
}
