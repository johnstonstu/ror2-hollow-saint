using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    internal static class ChargedStormTargeting
    {
        internal static Vector3 Point(HealthComponent health) => health && health.body ? health.body.corePosition : health ? health.transform.position : Vector3.zero;
        internal static bool Enemy(CharacterBody owner, HealthComponent health) => owner && health && health.alive && health.body &&
            owner.teamComponent && health.body.teamComponent && TeamMask.GetEnemyTeams(owner.teamComponent.teamIndex).HasTeam(health.body.teamComponent.teamIndex);
        internal static bool Clear(Vector3 from, Vector3 to) => !Physics.Linecast(from, to, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
        // 1.3.1 (Stu playtest): the strike zone is a column under the cloud instead of a sphere around
        // the aim point, so flyers and enemies on ledges up to the cloud are struck too.
        internal const float ColumnDepth = 8f, ColumnHeadroom = 6f;
        internal static bool CloudVisible(Vector3 sky, Vector3 center, Vector3 point, float radius)
        {
            float dx = point.x - center.x, dz = point.z - center.z;
            if (dx * dx + dz * dz > radius * radius) return false;
            float top = Mathf.Max(sky.y, center.y) + ColumnHeadroom;
            if (point.y < center.y - ColumnDepth || point.y > top) return false;
            return Clear(sky, point);
        }
        /// <summary>Search radius that covers the whole column from its centre point.</summary>
        internal static float ColumnSearchRange(Vector3 sky, Vector3 center, float radius)
        {
            float tall = Mathf.Max(ColumnDepth, Mathf.Max(sky.y, center.y) + ColumnHeadroom - center.y);
            return Mathf.Sqrt(radius * radius + tall * tall);
        }
        internal static List<HealthComponent> Find(CharacterBody owner, Vector3 point, float range, Vector3 direction, float angle, bool los)
        {
            var result = new List<HealthComponent>();
            if (!owner || !owner.teamComponent) return result;
            var search = new BullseyeSearch
            {
                searchOrigin = point, searchDirection = direction, minDistanceFilter = 0f, maxDistanceFilter = range,
                minAngleFilter = 0f, maxAngleFilter = angle, teamMaskFilter = TeamMask.GetEnemyTeams(owner.teamComponent.teamIndex),
                filterByDistinctEntity = true, filterByLoS = los, sortMode = BullseyeSearch.SortMode.Distance
            };
            search.RefreshCandidates();
            foreach (var box in search.GetResults())
            {
                var health = box ? box.healthComponent : null;
                if (Enemy(owner, health) && !result.Contains(health)) result.Add(health);
            }
            return result;
        }
        internal static Vector3 AimPoint(CharacterBody body, Vector3 direction, float range)
        {
            var origin = KitUtil.EyePosition(body);
            if (Physics.Raycast(origin, direction, out var hit, range, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
                return hit.point;
            return origin + direction * range;
        }
        internal static Vector3 CloudCenter(Vector3 ground, float radius)
        {
            // Higher and flatter than 1.3's first cloud: the storm sits above the fight
            // instead of filling the upper view, and the longer strokes read as sky lightning.
            float height = Mathf.Clamp(radius * .9f, 18f, 28f); // 1.3.1 (Stu): higher; was .6 x radius, 12-21 m
            if (Physics.Raycast(ground + Vector3.up * .2f, Vector3.up, out var hit, height, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                height = Mathf.Max(.5f, hit.distance - .5f);
            return ground + Vector3.up * height;
        }
    }
}
