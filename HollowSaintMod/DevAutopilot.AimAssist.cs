using System.Collections;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ArcBolt;
using HollowSaint.FoundationKit.Stormspear;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace HollowSaint
{
    internal sealed partial class DevAutopilot
    {
        private string assistTestPrefab;
        private int assistTestHits;

        // Success: actual prefab flight acquires and directly hits a near-miss target,
        // spends <=8 degrees, and misses the same shot with assistance disabled.
        // Test damage is not balanced here; spear AoE is temporarily disabled so it
        // cannot make a missed direct hit look like a successful guided throw.
        private IEnumerator AimAssistSegments()
        {
            float boltCone = KitTuning.ArcBoltAssistConeDegrees;
            float spearCone = StormspearTuning.AssistConeDegrees;
            float burst = StormspearTuning.BurstDamageFraction;
            On.RoR2.GlobalEventManager.OnHitEnemy += ObserveAssistHit;
            try
            {
                fire1 = fire2 = false;
                StormspearTuning.BurstDamageFraction = 0f;
                foreach (bool spear in new[] { false, true })
                {
                    foreach (bool assisted in new[] { false, true })
                    {
                        KitTuning.ArcBoltAssistConeDegrees = assisted ? 3f : 0f;
                        StormspearTuning.AssistConeDegrees = assisted ? 3f : 0f;
                        yield return AssistShot(spear, assisted);
                    }
                }
            }
            finally
            {
                On.RoR2.GlobalEventManager.OnHitEnemy -= ObserveAssistHit;
                KitTuning.ArcBoltAssistConeDegrees = boltCone;
                StormspearTuning.AssistConeDegrees = spearCone;
                StormspearTuning.BurstDamageFraction = burst;
                assistTestPrefab = null;
            }
        }

        private IEnumerator AssistShot(bool spear, bool assisted)
        {
            string label = (spear ? "spear" : "bolt") + (assisted ? "-assisted" : "-straight");
            yield return Segment("aim-" + label);
            if (dummies.Count == 0 || !dummies[0])
            { AssistCheck(false, label + " missing target"); yield break; }
            var victim = dummies[0];
            var box = victim.mainHurtBox;
            Vector3 point = box && box.collider ? box.collider.bounds.center : victim.corePosition;
            // Elevated test origin isolates flight from the hand/ground and gives
            // a clear long path. Normal production release is tested by the pose run.
            Vector3 origin = point - facing * 40f + Vector3.up * 15f;
            Vector3 exact = (point - origin).normalized;
            Vector3 direction = Quaternion.AngleAxis(-2.8f, Right) * exact;
            if (Physics.Linecast(origin, point, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            { AssistCheck(false, label + " test lane obstructed"); yield break; }
            var prefab = spear ? StormspearProjectile.Prefab : ArcBoltProjectile.Prefab;
            assistTestPrefab = prefab.name;
            assistTestHits = 0;
            ProjectileManager.instance.FireProjectile(new FireProjectileInfo
            {
                projectilePrefab = prefab, position = origin,
                rotation = Util.QuaternionSafeLookRotation(direction), owner = pilot.gameObject,
                damage = pilot.damage * (spear ? StormspearTuning.TapDamage : KitTuning.ArcBoltDamageCoefficient)
            });
            float end = scriptTime + 1.2f, turn = 0f;
            bool acquired = false;
            while (scriptTime < end)
            {
                yield return Wait(0.02f);
                foreach (var flight in FindObjectsOfType<ProjectileAimForgiveness>())
                {
                    if (!flight || !flight.name.StartsWith(assistTestPrefab)) continue;
                    if (flight.LockedTarget) acquired = true;
                    turn = Mathf.Max(turn, flight.TurnUsed);
                }
            }
            trace.AppendLine("AIM_FLIGHT " + label + " acquired=" + acquired + " turn=" + turn.ToString("0.00") + " directHits=" + assistTestHits);
            AssistCheck(turn <= 8.01f, label + " exceeded turn budget");
            AssistCheck(assisted ? acquired && turn > 0f && assistTestHits == 1 : !acquired && turn == 0f && assistTestHits == 0,
                label + " near-miss flight result incorrect");
            assistTestPrefab = null;
        }

        private void ObserveAssistHit(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig,
            GlobalEventManager self, DamageInfo info, GameObject victim)
        {
            if (assistTestPrefab != null && info.attacker == pilot.gameObject && info.inflictor &&
                info.inflictor.name.StartsWith(assistTestPrefab)) assistTestHits++;
            orig(self, info, victim);
        }

        private void AssistCheck(bool ok, string why)
        {
            if (ok) return;
            errors++;
            trace.AppendLine("AIM_FAIL " + why);
            Plugin.Log.LogError("AIM_CHECK " + why);
        }
    }
}
