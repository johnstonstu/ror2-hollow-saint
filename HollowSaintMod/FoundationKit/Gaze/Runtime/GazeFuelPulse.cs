using System;
using HollowSaint.FoundationKit.Storm;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>One immutable launch geometry and at most eight distinct weak strikes.
    /// No area damage accompanies the expanding cosmetic ground wave.</summary>
    internal sealed class GazeFuelPulse
    {
        public const int MaxVictims = 8;
        private readonly HealthComponent[] victims = new HealthComponent[MaxVictims];
        private readonly HurtBox[] boxes = new HurtBox[MaxVictims];
        private readonly Vector3[] points = new Vector3[MaxVictims];
        private readonly float[] due = new float[MaxVictims];
        private readonly bool[] resolved = new bool[MaxVictims];
        private readonly bool[] direct = new bool[MaxVictims];
        private int count;
        private float damage, spreadSeconds, proc;
        private int group;
        private bool crit;
        public int Phase { get; private set; }
        public Vector3 Origin, Impact, Ground, Normal;
        public bool HasGround;
        public float Travel, Radius;

        public void Clear() { count = 0; Array.Clear(victims, 0, victims.Length); Array.Clear(boxes, 0, boxes.Length); }

        public void Launch(CharacterBody body, Vector3 origin, Vector3 direction, int phase,
            int group, int capacity, float age, float spreadRadius, RaycastHit[] scratch, float travelExtra = 0f, float damageScale = 1f)
        {
            Clear();
            Phase = phase;
            Origin = origin;
            var world = GazeServer.Trace(origin, direction);
            Impact = world.Point;
            Normal = world.Normal;
            float length = Vector3.Distance(origin, Impact);
            var team = body.teamComponent ? body.teamComponent.teamIndex : TeamIndex.None;
            int found = Physics.SphereCastNonAlloc(origin, GazeTuning.Radius, direction, scratch, length,
                LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal);
            HurtBox primary = null;
            float nearest = length + 1f;
            // NonAlloc hits are unordered. Pick the nearest eligible intersection, bounded by world.
            for (int i = 0; i < found; i++)
            {
                var hit = scratch[i];
                var box = hit.collider ? hit.collider.GetComponent<HurtBox>() : null;
                var health = box ? box.healthComponent : null;
                if (!Eligible(health, body, team) || hit.distance >= nearest) continue;
                nearest = hit.distance;
                primary = box;
                Impact = hit.point;
            }
            Travel = GazeReleaseTuning.Enabled ? GazeReleaseTuning.Travel(Vector3.Distance(origin, Impact)) :
                GazeFuelSchedule.Travel(Vector3.Distance(origin, Impact));
            // The opening pulse rides a visible wave down the beam; damage lands when it arrives.
            Travel += travelExtra;
            spreadSeconds = GazeReleaseTuning.Enabled ? GazeReleaseTuning.SpreadSeconds : GazeFuelSchedule.SpreadDuration;
            this.group = group;
            proc = GazeReleaseTuning.Enabled ? GazeReleaseTuning.Proc(group) : 0f;
            Radius = Mathf.Max(0.1f, spreadRadius);
            RaycastHit terrain;
            HasGround = Physics.Raycast(Impact + Vector3.up * 2f, Vector3.down, out terrain, 8f,
                LayerIndex.world.mask, QueryTriggerInteraction.Ignore) && terrain.normal.y >= 0.35f;
            Ground = HasGround ? terrain.point : Impact;
            if (HasGround) Normal = terrain.normal;
            damage = body.damage * (GazeReleaseTuning.Enabled ?
                GazeReleaseTuning.DamagePerCharge * group : GazeFuelSchedule.Coefficient(group, capacity)) * damageScale;
            crit = body.RollCrit();
            if (primary) Add(primary, Impact, age + Travel, true);
            if (!HasGround) return;

            var search = new BullseyeSearch
            {
                searchOrigin = Ground, searchDirection = Vector3.up, minAngleFilter = 0f, maxAngleFilter = 180f,
                minDistanceFilter = 0f, maxDistanceFilter = Radius, teamMaskFilter = TeamMask.GetEnemyTeams(team),
                filterByLoS = false, filterByDistinctEntity = true, sortMode = BullseyeSearch.SortMode.Distance
            };
            search.RefreshCandidates();
            search.FilterOutGameObject(body.gameObject);
            foreach (var box in search.GetResults())
            {
                if (count >= MaxVictims) break;
                var health = box ? box.healthComponent : null;
                if (!Eligible(health, body, team) || Contains(health)) continue;
                Vector3 point = box.collider ? box.collider.bounds.center : box.transform.position;
                Vector3 targetGround;
                if (!GroundRoute(Ground, point, out targetGround)) continue;
                float distance = Vector3.Distance(Ground, targetGround);
                if (distance > Radius) continue;
                Add(box, point, GazeFuelSchedule.StrikeAt(age, Travel, distance, Radius, spreadSeconds));
            }
        }

        private Vector3 Shove(Vector3 point)
        {
            if (!GazeReleaseTuning.Enabled) return Vector3.zero;
            Vector3 away = point - (HasGround ? Ground : Impact); away.y = 0f;
            away = away.sqrMagnitude > .01f ? away.normalized : Vector3.zero;
            return (away + Vector3.up * .35f).normalized * GazeReleaseTuning.ForcePerCharge * Math.Min(group, 3);
        }

        private static bool Eligible(HealthComponent health, CharacterBody body, TeamIndex team) =>
            health && health != body.healthComponent && health.alive && FriendlyFireManager.ShouldDirectHitProceed(health, team);
        private bool Contains(HealthComponent health)
        {
            for (int i = 0; i < count; i++) if (victims[i] == health) return true;
            return false;
        }
        private void Add(HurtBox box, Vector3 point, float at, bool primary = false)
        {
            if (count >= MaxVictims || Contains(box.healthComponent)) return;
            victims[count] = box.healthComponent; boxes[count] = box; points[count] = point;
            due[count] = at; resolved[count] = false; direct[count] = primary; count++;
        }

        /// <summary>Sample real terrain at <=1m spacing. Gaps, cliffs and walls stop the
        /// wave; a missing downward ray never becomes an invented flat floor.</summary>
        private static bool GroundRoute(Vector3 from, Vector3 target, out Vector3 end)
        {
            end = from;
            Vector3 planar = target - from; planar.y = 0f;
            int steps = Mathf.Clamp(Mathf.CeilToInt(planar.magnitude), 1, 64);
            for (int i = 1; i <= steps; i++)
            {
                Vector3 sample = from + planar * (i / (float)steps);
                RaycastHit hit;
                if (!Physics.Raycast(sample + Vector3.up * 2f, Vector3.down, out hit, 4f,
                    LayerIndex.world.mask, QueryTriggerInteraction.Ignore) || hit.normal.y < 0.35f) return false;
                if (Mathf.Abs(hit.point.y - end.y) > 1.5f || Physics.Linecast(end + Vector3.up * 0.15f,
                    hit.point + Vector3.up * 0.15f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) return false;
                end = hit.point;
            }
            // The endpoint strike also has to reach the target without crossing a wall.
            return !Physics.Linecast(end + Vector3.up * 0.15f, target, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
        }

        public void Resolve(CharacterBody body, float age, GazeFuelController controller)
        {
            for (int i = 0; i < count; i++)
            {
                if (resolved[i] || age < due[i]) continue;
                resolved[i] = true;
                var victim = victims[i];
                if (!victim || !victim.alive) continue;
                var box = boxes[i];
                Vector3 point = box ? (box.collider ? box.collider.bounds.center : box.transform.position) :
                    (victim.body ? victim.body.corePosition : points[i]);
                if (direct[i])
                {
                    if (Vector3.Distance(Origin, point) > GazeTuning.Range || Physics.Linecast(Origin, point,
                        LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) continue;
                }
                else
                {
                    Vector3 currentGround;
                    if (!GroundRoute(Ground, point, out currentGround) ||
                        Vector3.Distance(Ground, currentGround) > Radius) continue;
                }
                var info = new DamageInfo
                {
                    damage = damage, crit = crit, attacker = body.gameObject, inflictor = body.gameObject,
                    position = point, force = Shove(point), procCoefficient = proc,
                    damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special),
                    damageColorIndex = DamageColorIndex.Electrocution, inflictedHurtbox = boxes[i]
                };
                StormServer.BeginStormDamage();
                try
                {
                    victim.TakeDamage(info);
                    // Same pattern as Electrocute: on-hit items under the storm guard.
                    if (proc > 0f) KitUtil.ReportHit(info, victim.gameObject);
                }
                finally { StormServer.EndStormDamage(); }
                // Spenders prime: Gaze blasts and surges leave Static for the beam core and Arc Bolt to finish.
                if (!info.rejected && victim.alive)
                    StormServer.PrimeStatic(victim, body, StaticPrimePolicy.Amount(ChargedStorm.ChargedStormTuning.GazeStaticPrime, 1f));
                if (!info.rejected) controller.ConfirmStrike(Phase, group, point, age);
            }
        }
    }
}
