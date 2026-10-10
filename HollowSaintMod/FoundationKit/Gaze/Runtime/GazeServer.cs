using System.Collections.Generic;
using HollowSaint.FoundationKit.Gaze.Fx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>
    /// Server-only damage for Gaze of the Hollow. The beam pierces every enemy along its length
    /// (stopped only by world geometry), the impact splashes enemies the core missed, and every
    /// ForkInterval lightning forks race from the impact to nearby enemies and chain one hop.
    /// All hits are tagged DamageSource.Special with a real proc coefficient, so Static,
    /// Electrocute and Thunderbolt follow from the normal Storm rules.
    /// </summary>
    public static class GazeServer
    {
        public struct Impact
        {
            public Vector3 Point;
            public Vector3 Normal;
            public bool HitWorld;
        }

        private static readonly HashSet<HealthComponent> struck = new HashSet<HealthComponent>();

        /// <summary>Dev autopilot only: (victim, multiplier) for each core hit.</summary>
        internal static System.Action<HealthComponent, float> FocusTrace;

        /// <summary>Advances focus for a core hit and returns the damage multiplier; reports a tier crossing.</summary>
        private static float Focus(HealthComponent attacker, HealthComponent victim, float now, out int newTier)
        {
            newTier = 0;
            if (!attacker || !victim) return 1f;
            var tracker = attacker.GetComponent<GazeFocusTracker>();
            if (!tracker) tracker = attacker.gameObject.AddComponent<GazeFocusTracker>();
            return tracker.Hit(victim, now, out newTier);
        }

        /// <summary>v0.9.12+: splash, fork and chain reach multiplier, set by GazeState each tick. It grows
        /// over the channel (GazeTuning.ReachStart to ReachEnd), so a held beam forks further.</summary>
        public static float Reach = 1f;

        public static Impact Trace(Vector3 origin, Vector3 direction)
        {
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, GazeTuning.Range, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                return new Impact { Point = hit.point, Normal = hit.normal, HitWorld = true };
            return new Impact { Point = origin + direction * GazeTuning.Range, Normal = -direction, HitWorld = false };
        }

        /// <summary>One damage tick of the core plus the impact splash. Returns the impact.</summary>
        public static Impact CoreTick(CharacterBody attacker, Vector3 origin, Vector3 direction, float coefficient)
        {
            var impact = Trace(origin, direction);
            if (!NetworkServer.active || !attacker) return impact;
            var team = attacker.teamComponent ? attacker.teamComponent.teamIndex : TeamIndex.None;
            var self = attacker.healthComponent;
            bool crit = attacker.RollCrit();
            float damage = attacker.damage * coefficient;
            float length = Vector3.Distance(origin, impact.Point);

            struck.Clear();
            int shownHits = 0; // Cosmetic budget only; every eligible enemy still takes its normal hit.
            var presentation = attacker.GetComponent<GazeBeam>();
            bool showContacts = presentation && presentation.ClaimContactFxTick();
            var hits = Physics.SphereCastAll(origin, GazeTuning.Radius, direction, length,
                LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal);
            for (int i = 0; i < hits.Length; i++)
            {
                var box = hits[i].collider ? hits[i].collider.GetComponent<HurtBox>() : null;
                var health = box ? box.healthComponent : null;
                if (!health || health == self || !health.alive || struck.Contains(health)) continue;
                if (!FriendlyFireManager.ShouldDirectHitProceed(health, team)) continue;
                struck.Add(health);
                Vector3 contactPoint = Center(box);
                float focused = Focus(self, health, Time.time, out int focusTier);
                FocusTrace?.Invoke(health, focused);
                Hit(attacker, box, damage * focused, crit, GazeTuning.ProcCoefficient);
                if (focusTier > 0)
                {
                    GazeEffect.Server(GazeEffect.Kind.Focus, contactPoint, origin, attacker, true, focusTier / (float)GazeFocusPolicy.Tiers);
                    KitLog.Event("GAZE_FOCUS", "tier=" + focusTier + " victim=" + (health.body ? health.body.name : "?"));
                }
                if (showContacts && shownHits++ < 4) GazeEffect.Server(GazeEffect.Kind.Contact, contactPoint,
                    origin + direction * Mathf.Clamp(Vector3.Dot(contactPoint - origin, direction) - 2f, 0f, length), attacker, false, 0f);
            }

            // Splash: enemies around the impact the core did not touch this tick.
            foreach (var box in Search(impact.Point, GazeTuning.SplashRadius * Reach, team, attacker.gameObject, false))
            {
                var health = box.healthComponent;
                if (struck.Contains(health)) continue;
                struck.Add(health);
                Vector3 contactPoint = Center(box);
                Hit(attacker, box, damage * GazeTuning.SplashFraction, crit, GazeTuning.SplashProc);
                if (showContacts && shownHits++ < 4) GazeEffect.Server(GazeEffect.Kind.Contact, contactPoint, impact.Point, attacker, false, 0f);
            }
            if (struck.Count > 0) KitLog.Event("GAZE_TICK", "hits=" + struck.Count);
            return impact;
        }

        /// <summary>Forks from the impact to up to ForkCount enemies, each chaining one hop.</summary>
        public static void Forks(CharacterBody attacker, Impact impact, float damageMultiplier = 1f)
        {
            if (!NetworkServer.active || !attacker) return;
            var team = attacker.teamComponent ? attacker.teamComponent.teamIndex : TeamIndex.None;
            var forked = new HashSet<HealthComponent>();
            int forks = 0;
            // Lifted off the surface so the line-of-sight test does not start inside the ground.
            Vector3 from = impact.Point + impact.Normal * 0.5f;
            foreach (var box in Search(from, GazeTuning.ForkRange * Reach, team, attacker.gameObject, true))
            {
                if (forks >= GazeTuning.ForkCount) break;
                var health = box.healthComponent;
                if (forked.Contains(health)) continue;
                forked.Add(health);
                bool crit = attacker.RollCrit();
                float damage = attacker.damage * GazeTuning.ForkDamage * damageMultiplier;
                Vector3 to = Center(box);
                Hit(attacker, box, damage, crit, GazeTuning.ForkProc);
                GazeEffect.Server(GazeEffect.Kind.Fork, to, impact.Point, attacker, sound: forks == 0, delay: forks * 0.06f);
                forks++;

                var next = Nearest(to, GazeTuning.ChainRange * Reach, team, attacker.gameObject, forked);
                if (next == null) continue;
                forked.Add(next.healthComponent);
                Vector3 chainTo = Center(next);
                Hit(attacker, next, damage * GazeTuning.ChainFraction, crit, GazeTuning.ChainProc);
                GazeEffect.Server(GazeEffect.Kind.Chain, chainTo, to, attacker, sound: false, delay: forks * 0.06f + 0.12f);
            }
            if (forks > 0) KitLog.Event("GAZE_FORKS", "forks=" + forks);
        }

        private static void Hit(CharacterBody attacker, HurtBox box, float damage, bool crit, float proc)
        {
            var health = box.healthComponent;
            var info = new DamageInfo
            {
                damage = damage,
                crit = crit,
                attacker = attacker.gameObject,
                inflictor = attacker.gameObject,
                position = Center(box),
                force = Vector3.zero,
                procCoefficient = proc,
                damageColorIndex = DamageColorIndex.Default,
                damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special),
                inflictedHurtbox = box
            };
            health.TakeDamage(info);
            KitUtil.ReportHit(info, health.gameObject);
        }

        private static Vector3 Center(HurtBox box)
        {
            return box.collider ? box.collider.bounds.center : box.transform.position;
        }

        private static IEnumerable<HurtBox> Search(Vector3 origin, float radius, TeamIndex team, GameObject self, bool lineOfSight)
        {
            var search = new BullseyeSearch
            {
                searchOrigin = origin,
                searchDirection = Vector3.up,
                minAngleFilter = 0f,
                maxAngleFilter = 180f,
                minDistanceFilter = 0f,
                maxDistanceFilter = radius,
                teamMaskFilter = TeamMask.GetEnemyTeams(team),
                filterByLoS = lineOfSight,
                filterByDistinctEntity = true,
                sortMode = BullseyeSearch.SortMode.Distance
            };
            search.RefreshCandidates();
            search.FilterOutGameObject(self);
            foreach (var box in search.GetResults())
            {
                var health = box ? box.healthComponent : null;
                if (health && health.alive) yield return box;
            }
        }

        private static HurtBox Nearest(Vector3 origin, float radius, TeamIndex team, GameObject self, HashSet<HealthComponent> exclude)
        {
            foreach (var box in Search(origin, radius, team, self, true))
                if (!exclude.Contains(box.healthComponent)) return box;
            return null;
        }
    }
}
