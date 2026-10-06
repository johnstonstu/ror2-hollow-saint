using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Stormspear
{
    internal static class SpearBurstDamage
    {
        internal static List<Vector3> Apply(CharacterBody attacker, Vector3 origin, Vector3 sightOrigin, float radius,
            float damage, bool crit, float proc, DamageTypeCombo type, HealthComponent primary, ProcChainMask mask)
        {
            var hits = new List<Vector3>();
            if (!NetworkServer.active || !attacker || radius <= 0f) return hits;
            var search = new BullseyeSearch
            {
                searchOrigin = origin, searchDirection = Vector3.up,
                minAngleFilter = 0f, maxAngleFilter = 180f, maxDistanceFilter = radius,
                teamMaskFilter = TeamMask.GetEnemyTeams(attacker.teamComponent ? attacker.teamComponent.teamIndex : TeamIndex.None),
                filterByLoS = false, filterByDistinctEntity = true, sortMode = BullseyeSearch.SortMode.Distance
            };
            search.RefreshCandidates();
            search.FilterOutGameObject(attacker.gameObject);
            foreach (var box in search.GetResults())
            {
                if (hits.Count >= 64) break;
                var health = box ? box.healthComponent : null;
                if (!health || !health.alive || health == primary) continue;
                Vector3 point = box.collider ? box.collider.bounds.center : box.transform.position;
                float distance = Vector3.Distance(origin, point);
                bool visible = !Physics.Linecast(sightOrigin, point, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
                if (!SpearBurstPolicy.Eligible(health.alive, health == primary, visible, distance, radius)) continue;
                var info = new DamageInfo
                {
                    damage = damage * SpearBurstPolicy.Scale(distance, radius), crit = crit,
                    attacker = attacker.gameObject, inflictor = attacker.gameObject, position = point,
                    force = Vector3.zero, procCoefficient = proc, damageType = type,
                    damageColorIndex = DamageColorIndex.Default, procChainMask = mask, inflictedHurtbox = box
                };
                health.TakeDamage(info);
                KitUtil.ReportHit(info, health.gameObject);
                hits.Add(point);
            }
            return hits;
        }
    }
}
