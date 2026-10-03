using System.Collections.Generic;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.ArcBolt
{
    /// <summary>
    /// Server-side follow-up to a confirmed Arc Bolt hit: up to
    /// ArcBoltMaxChainTargets - 1 further hops, nearest unhit enemy first, each at
    /// ArcBoltChainFalloff times the previous hop. Every hop sends a ChainHop beat so all
    /// clients see the arc and hear the first one.
    /// </summary>
    public static class ArcBoltChainServer
    {
        public static void ResolveConfirmedHit(CharacterBody attackerBody, HealthComponent primaryVictim,
            Vector3 hitPosition, float primaryDamage, bool crit)
        {
            if (!NetworkServer.active || attackerBody == null) return;

            KitLog.Event("ARC_BOLT_HIT", "victim=" + (primaryVictim ? primaryVictim.name : "none"));

            var hit = new HashSet<HealthComponent>();
            if (primaryVictim) hit.Add(primaryVictim);

            var team = attackerBody.teamComponent ? attackerBody.teamComponent.teamIndex : TeamIndex.None;
            Vector3 from = hitPosition;
            float damage = primaryDamage;

            for (int hop = 0; hop < KitTuning.ArcBoltMaxChainTargets - 1; hop++)
            {
                var next = FindNext(from, team, hit);
                if (next == null) break;
                damage *= KitTuning.ArcBoltChainFalloff;
                var health = next.healthComponent;
                hit.Add(health);
                Vector3 to = next.collider ? next.collider.bounds.center : next.transform.position;

                var info = new DamageInfo
                {
                    damage = damage,
                    crit = crit,
                    attacker = attackerBody.gameObject,
                    inflictor = attackerBody.gameObject,
                    position = to,
                    force = Vector3.zero,
                    procCoefficient = KitTuning.ArcBoltChainProc * Mathf.Pow(0.5f, hop), // 0.4, 0.2, 0.1
                    damageColorIndex = DamageColorIndex.Default,
                    damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Primary),
                    inflictedHurtbox = next
                };
                health.TakeDamage(info);
                KitUtil.ReportHit(info, health.gameObject);
                float hopScale = Mathf.Pow(0.85f, hop);
                KitFx.Server(Beat.ChainHop, to, from, hopScale, sound: hop < 2, delay: hop * 0.07f, owner: attackerBody);
                if (hop == 0) KitLog.Event("ARC_BOLT_CHAIN_STARTED");
                from = to;
            }
        }

        private static HurtBox FindNext(Vector3 origin, TeamIndex team, HashSet<HealthComponent> exclude)
        {
            var search = new BullseyeSearch
            {
                searchOrigin = origin,
                searchDirection = Vector3.up,
                minAngleFilter = 0f,
                maxAngleFilter = 180f,
                maxDistanceFilter = KitTuning.ArcBoltChainRange,
                teamMaskFilter = TeamMask.GetEnemyTeams(team),
                filterByLoS = true,
                filterByDistinctEntity = true,
                sortMode = BullseyeSearch.SortMode.Distance
            };
            search.RefreshCandidates();
            HurtBox nearest = null;
            foreach (var box in search.GetResults())
            {
                var health = box ? box.healthComponent : null;
                if (!health || !health.alive || exclude.Contains(health)) continue;
                if (nearest == null) nearest = box;
            }
            return nearest;
        }
    }
}
