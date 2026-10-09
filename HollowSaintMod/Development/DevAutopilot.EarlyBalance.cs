using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    internal sealed partial class DevAutopilot
    {
        private CharacterBody balanceVictim;
        private bool balanceRecording;
        private float balanceDamage;
        private int balanceHits;

        private void RecordBalanceDamage(DamageReport report)
        {
            if (!balanceRecording || report.attackerBody != pilot || report.victimBody != balanceVictim) return;
            balanceDamage += report.damageDealt; balanceHits++;
            trace.AppendLine("BALANCE_HIT segment=" + segment + " source=" + report.damageInfo.damageType.damageSource +
                " offered=" + report.damageInfo.damage + " dealt=" + report.damageDealt + " crit=" + report.damageInfo.crit);
        }
        private void LogNativePrimaryReference()
        {
            var commando = BodyCatalog.FindBodyPrefab("CommandoBody").GetComponent<CharacterBody>();
            trace.AppendLine("REFERENCE Commando baseDamage=" + commando.baseDamage + " baseAttackSpeed=" + commando.baseAttackSpeed);
            foreach (string name in new[] { "EntityStates.Commando.CommandoWeapon.FirePistol2", "EntityStates.Commando.CommandoWeapon.ReloadPistols" })
            {
                var type = typeof(EntityStates.EntityState).Assembly.GetType(name);
                if (type == null) { trace.AppendLine("REFERENCE unavailable type=" + name); continue; }
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                    if (field.FieldType == typeof(float) || field.FieldType == typeof(int))
                        trace.AppendLine("REFERENCE " + name + "." + field.Name + "=" + field.GetValue(null));
            }
        }
        private IEnumerator BalanceTarget(string name, float coefficient, float distance = 12f)
        {
            ClearLive(); yield return Segment(name);
            Take(null);
            TeamManager.instance.SetTeamLevel(TeamIndex.Player, 1u);
            TeamManager.instance.SetTeamLevel(TeamIndex.Monster, 1u);
            pilot.baseCrit = 0f; pilot.RecalculateStats();
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 0);
            KitTuning.ArcBoltDamageCoefficient = coefficient;
            SpawnLive("TitanMaster", 1, distance); yield return Wait(4.5f);
            balanceVictim = Alive().FirstOrDefault();
            ReleaseCheck(balanceVictim != null, "native itemless Titan exists " + name);
            if (!balanceVictim) yield break;
            balanceVictim.RecalculateStats();
            int items = pilot.inventory.itemAcquisitionOrder.Sum(i => pilot.inventory.GetItemCountPermanent(i));
            ReleaseCheck(items == 0 && Mathf.Abs(pilot.attackSpeed - 1f) < .001f && pilot.crit == 0f && pilot.level == 1f,
                "real level-one no-item no-crit benchmark " + name);
            trace.AppendLine("BALANCE_STATS segment=" + name + " raw=" + coefficient + " pilotLevel=" + pilot.level +
                " damage=" + pilot.damage + " speed=" + pilot.attackSpeed + " items=" + items +
                " targetLevel=" + balanceVictim.level + " hp=" + balanceVictim.healthComponent.fullCombinedHealth +
                " armor=" + balanceVictim.armor + " targetItems=" + balanceVictim.inventory.itemAcquisitionOrder.Count);
            balanceVictim.healthComponent.godMode = false;
            balanceVictim.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            ReleaseCheck(ChargedStormTargeting.Find(pilot, pilot.corePosition, 18f, Vector3.forward, 180f, false).Count == 1,
                "isolated native boss has exactly one nearby enemy " + name);
            aimTarget = balanceVictim.corePosition;
        }
        private IEnumerator PrimaryBalanceWindow(string name, float coefficient)
        {
            yield return BalanceTarget(name, coefficient);
            if (!balanceVictim) yield break;
            balanceDamage = 0f; balanceHits = 0; balanceRecording = true;
            float started = scriptTime, health = balanceVictim.healthComponent.combinedHealth;
            Clip(name, true); fire1 = true;
            yield return AimAtLive(20f); fire1 = false;
            yield return Wait(.25f); balanceRecording = false; Clip(name, false);
            float span = scriptTime - started;
            trace.AppendLine("BALANCE_SUMMARY segment=" + name + " seconds=" + span + " hits=" + balanceHits +
                " damage=" + balanceDamage + " dps=" + balanceDamage / span + " hpDelta=" +
                (health - balanceVictim.healthComponent.combinedHealth) + " charges=" + pilot.GetComponent<DischargeMeter>().Charge);
            ReleaseCheck(balanceHits >= 35 && balanceHits <= 42 && balanceDamage > 400f,
                "native Primary cadence and real damage " + name);
            Shot(name + "-end");
        }
        private IEnumerator EarlyBalanceSegments()
        {
            isolateReviewFixtures = true;
            // Success: measured native level-one itemless hits, armor and cadence;
            // identical stationary Titan fixtures compare only coefficient changes.
            foreach (var dummy in dummies) if (dummy) { dummy.healthComponent.godMode = false; dummy.healthComponent.Suicide(); }
            dummies.Clear(); StartCoroutine(CameraFollow()); SetCameraDistance(8.5f, .5f);
            syncFlashUntil = Time.realtimeSinceStartup + .25f;
            trace.AppendLine("SYNC utc=" + DateTime.UtcNow.ToString("o")); yield return Wait(1f);
            float originalDamage = KitTuning.ArcBoltDamageCoefficient;
            float originalCrit = pilot.baseCrit;
            GlobalEventManager.onServerDamageDealt += RecordBalanceDamage;
            try
            {
                LogNativePrimaryReference();
                yield return PrimaryBalanceWindow("primary-baseline-108", 1.2f);
                yield return PrimaryBalanceWindow("primary-proposal-144", 1.6f);
            }
            finally
            {
                balanceRecording = fire1 = fire2 = fire4 = false;
                KitTuning.ArcBoltDamageCoefficient = originalDamage; pilot.baseCrit = originalCrit;
                GlobalEventManager.onServerDamageDealt -= RecordBalanceDamage;
                ClearLive();
            }
        }
    }
}
