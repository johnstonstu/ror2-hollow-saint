using System.Collections.Generic;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Storm
{
    /// <summary>
    /// Lives on the Hollow Saint body prefab (every machine); only the server acts. When the
    /// Storm charge is full and the cooldown is up it picks a strong enemy in range with line
    /// of sight, telegraphs, then calls the Thunderbolt down on it. With no valid target the
    /// charge is held (the chest core stays white) and the search retries.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThunderboltDriver : MonoBehaviour
    {
        private const float SearchInterval = 0.25f;
        private const int MaxRepicks = 2;

        private CharacterBody body;
        private DischargeMeter meter;
        private readonly ThunderboltFlight flight = new ThunderboltFlight();
        private float nextSearch;
        private bool telegraphing;
        private float strikeAt;
        private int repicks;
        private HealthComponent target;
        private int visualRevision;
        private HealthComponent committedVictim;
        private Vector3 impactPosition;
        private float committedDamage;
        private float committedSplashDamage;
        private float committedSplashRadius;
        private bool committedCrit;

        // ------------------------------------------------------------------ v0.9 extra strikes

        private struct ExtraStrike
        {
            public Vector3 point;
            public HealthComponent victim;
            public float due, damage, splashDamage, splashRadius;
            public bool crit;
        }
        private readonly List<ExtraStrike> extraStrikes = new List<ExtraStrike>();
        private const float ExtraStrikeDelay = 0.2f;

        /// <summary>Server only. Calls a Thunderbolt down at a point (the crown Stormspear). Same
        /// strike visuals, damage and splash as the passive Thunderbolt, but it neither consumes the
        /// Storm meter nor touches the passive's cooldown, telegraph or flight state. A short
        /// warning ring plays at the point and the strike lands after about 0.2 s. It is resolved
        /// even if the owner dies in that window, like the passive strike.</summary>
        public void ServerStrikeAt(Vector3 point, HealthComponent victim)
        {
            if (!NetworkServer.active || body == null) return;
            if (extraStrikes.Count >= 8) return; // runaway guard
            float damage = KitTuning.ThunderboltDamageCoefficient * body.damage;
            extraStrikes.Add(new ExtraStrike
            {
                point = point,
                victim = victim,
                due = Time.time + ExtraStrikeDelay,
                damage = damage,
                splashDamage = damage * KitTuning.ThunderboltSplashFraction,
                splashRadius = KitTuning.ThunderboltSplashRadius,
                crit = body.RollCrit()
            });
            KitFx.Server(Beat.ThunderTelegraph, point, default(Vector3), ExtraStrikeDelay + 0.1f, sound: false, owner: body);
        }

        private void ResolveExtraStrikes(float now)
        {
            for (int i = extraStrikes.Count - 1; i >= 0; i--)
            {
                var strike = extraStrikes[i];
                if (now < strike.due) continue;
                extraStrikes.RemoveAt(i);
                try { ImpactExtra(strike); }
                catch (System.Exception error) { Plugin.Log.LogError("HOLLOW_SAINT_EXTRA_STRIKE_ERROR " + error); }
            }
        }

        private void ImpactExtra(ExtraStrike strike)
        {
            var victim = strike.victim;
            var victimBody = victim != null ? victim.body : null;
            Vector3 point = strike.point;
            RoyalCapacitorFx.Strike(point, body);
            StormTelemetry.RecordStrike();
            KitLog.Event("THUNDERBOLT", "crown point damage=" + strike.damage.ToString("0.0") + " crit=" + strike.crit);

            StormServer.BeginStormDamage();
            try
            {
                if (victim != null && victim.alive && victimBody != null)
                {
                    var info = new DamageInfo
                    {
                        damage = strike.damage,
                        crit = strike.crit,
                        attacker = body.gameObject,
                        inflictor = body.gameObject,
                        position = point,
                        force = Vector3.zero,
                        procCoefficient = 1f,
                        damageColorIndex = DamageColorIndex.Electrocution,
                        damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.NoneSpecified),
                        inflictedHurtbox = victimBody.mainHurtBox
                    };
                    victim.TakeDamage(info);
                    KitUtil.ReportHit(info, victim.gameObject);
                }
                if (strike.splashDamage > 0f)
                {
                    var others = StormServer.FindEnemies(body, point, strike.splashRadius, 8, victim);
                    for (int i = 0; i < others.Count; i++)
                    {
                        var health = others[i].healthComponent;
                        if (health == null || !health.alive) continue;
                        var splash = StormServer.MakeInfo(body, others[i], strike.splashDamage, strike.crit, 0.5f);
                        health.TakeDamage(splash);
                        KitUtil.ReportHit(splash, health.gameObject);
                    }
                }
            }
            finally { StormServer.EndStormDamage(); }

            if (victim != null && victim.alive && victimBody != null) StormServer.ElectrocuteFromStrike(victim, victimBody, body);
        }

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
            meter = GetComponent<DischargeMeter>();
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active || body == null || meter == null) return;
            float now = Time.time;
            if (extraStrikes.Count > 0) ResolveExtraStrikes(now);
            // A launched strike resolves even if the owner dies during the flight (ThunderboltFlightSeconds).
            // New charge gained in flight is left intact; it was already spent at launch.
            if (flight.Pending)
            {
                if (flight.Impact(now)) Impact();
                return;
            }
            if (body.healthComponent == null || !body.healthComponent.alive)
            {
                CancelTelegraph();
                return;
            }
            if (telegraphing)
            {
                if (now < strikeAt) return;
                if (!meter.IsFull) { CancelTelegraph(); return; }
                if (IsValid(target)) { Launch(target); return; }
                // Target died or left range during the telegraph: re-pick, or hold the charge.
                var next = repicks < MaxRepicks ? PickTarget() : null;
                if (next != null) { repicks++; Telegraph(next); }
                else { CancelTelegraph(); nextSearch = now + SearchInterval; }
                return;
            }

            if (!meter.IsFull) return;
            if (!flight.Ready(now, KitTuning.ThunderboltCooldown) || now < nextSearch) return;
            var pick = PickTarget();
            if (pick == null) { nextSearch = now + SearchInterval; return; }
            repicks = 0;
            Telegraph(pick);
        }

        private bool IsValid(HealthComponent health)
        {
            if (health == null || !health.alive || health.body == null) return false;
            float range = KitTuning.ThunderboltRange;
            return (health.body.corePosition - body.corePosition).sqrMagnitude <= range * range;
        }

        private void Telegraph(HealthComponent pick)
        {
            target = pick;
            telegraphing = true;
            strikeAt = Time.time + KitTuning.ThunderboltTelegraphSeconds;
            // The warning ring stays at the target; charge audio follows the gathering halo.
            // scale = how long the ground warning lasts (telegraph + flight).
            KitFx.Server(Beat.ThunderTelegraph, pick.body.footPosition, default(Vector3), KitTuning.ThunderboltTelegraphSeconds + KitTuning.ThunderboltFlightSeconds, sound: false, owner: body);
            KitFx.ServerOnBody(Beat.ThunderGather, body, pick.body.corePosition, KitTuning.ThunderboltTelegraphSeconds, ++visualRevision);
        }

        private void CancelTelegraph()
        {
            if (telegraphing)
                KitFx.ServerOnBody(Beat.ThunderCancel, body, body.corePosition, 0f, ++visualRevision);
            telegraphing = false;
            target = null;
            repicks = 0;
        }

        /// <summary>Bosses first, then elites, then current health; random among the top 3.</summary>
        private HealthComponent PickTarget()
        {
            Vector3 origin = body.corePosition;
            float range = KitTuning.ThunderboltRange;
            var team = body.teamComponent != null ? body.teamComponent.teamIndex : TeamIndex.None;
            var search = new BullseyeSearch
            {
                searchOrigin = origin,
                searchDirection = Vector3.up,
                minAngleFilter = 0f,
                maxAngleFilter = 180f,
                minDistanceFilter = 0f,
                maxDistanceFilter = range,
                teamMaskFilter = TeamMask.GetEnemyTeams(team),
                filterByLoS = false, // our own world-layer raycast below, core to core
                filterByDistinctEntity = true,
                sortMode = BullseyeSearch.SortMode.Distance
            };
            search.RefreshCandidates();
            search.FilterOutGameObject(body.gameObject);

            var candidates = new List<Candidate>();
            foreach (var box in search.GetResults())
            {
                var health = box != null ? box.healthComponent : null;
                if (health == null || !health.alive || health.body == null) continue;
                Vector3 to = health.body.corePosition;
                if ((to - origin).sqrMagnitude > range * range) continue;
                if (Physics.Linecast(origin, to, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) continue;
                candidates.Add(new Candidate
                {
                    health = health,
                    rank = (health.body.isBoss ? 2 : 0) + (health.body.isElite ? 1 : 0),
                    hp = health.combinedHealth
                });
            }
            if (candidates.Count == 0) return null;
            candidates.Sort((a, b) =>
            {
                int byRank = b.rank.CompareTo(a.rank);
                return byRank != 0 ? byRank : b.hp.CompareTo(a.hp);
            });
            int top = Mathf.Min(3, candidates.Count);
            return candidates[Random.Range(0, top)].health;
        }

        private struct Candidate
        {
            public HealthComponent health;
            public int rank;
            public float hp;
        }

        private void Launch(HealthComponent victim)
        {
            if (!flight.Launch(Time.time, KitTuning.ThunderboltCooldown)) return;
            var victimBody = victim.body;
            committedVictim = victim;
            impactPosition = victimBody.mainHurtBox != null ? victimBody.mainHurtBox.transform.position : victimBody.corePosition;
            committedCrit = body.RollCrit();
            committedDamage = KitTuning.ThunderboltDamageCoefficient * body.damage;
            committedSplashDamage = committedDamage * KitTuning.ThunderboltSplashFraction;
            committedSplashRadius = KitTuning.ThunderboltSplashRadius;

            // Commit once at launch. Impact never consumes again or refunds a dead target.
            meter.Consume();
            telegraphing = false;
            target = null;
            repicks = 0;
            KitFx.ServerOnBody(Beat.ThunderRelease, body, impactPosition, KitTuning.ThunderboltFlightSeconds, ++visualRevision);
        }

        private void Impact()
        {
            var victim = committedVictim;
            committedVictim = null;
            var victimBody = victim != null ? victim.body : null;
            if (victim != null && victim.alive && victimBody != null)
                impactPosition = victimBody.mainHurtBox != null ? victimBody.mainHurtBox.transform.position : victimBody.corePosition;
            RoyalCapacitorFx.Strike(impactPosition, body);
            StormTelemetry.RecordStrike();
            KitLog.Event("THUNDERBOLT", "target=" + (victimBody != null ? victimBody.name : "<expired>") +
                " damage=" + committedDamage.ToString("0.0") + " crit=" + committedCrit);

            StormServer.BeginStormDamage();
            try
            {
                if (victim != null && victim.alive && victimBody != null) HitDirect(victim, victimBody);
                // If the original target dies/disappears, the committed world impact still
                // lands and splashes nearby enemies. No re-pick, refund or second launch.
                if (committedSplashDamage > 0f)
                {
                    var others = StormServer.FindEnemies(body, impactPosition, committedSplashRadius, 8, victim);
                    for (int i = 0; i < others.Count; i++)
                    {
                        var health = others[i].healthComponent;
                        if (health == null || !health.alive) continue;
                        var splash = StormServer.MakeInfo(body, others[i], committedSplashDamage, committedCrit, 0.5f);
                        health.TakeDamage(splash);
                        KitUtil.ReportHit(splash, health.gameObject);
                    }
                }
            }
            finally { StormServer.EndStormDamage(); }

            // The strike also counts as an Electrocute on the target (stun or Shocked), without
            // awarding a charge or popping.
            if (victim != null && victim.alive && victimBody != null) StormServer.ElectrocuteFromStrike(victim, victimBody, body);
        }

        private void HitDirect(HealthComponent victim, CharacterBody victimBody)
        {
            var box = victimBody.mainHurtBox;
            var info = new DamageInfo
            {
                damage = committedDamage,
                crit = committedCrit,
                attacker = body.gameObject,
                inflictor = body.gameObject,
                position = impactPosition,
                force = Vector3.zero,
                procCoefficient = 1f,
                damageColorIndex = DamageColorIndex.Electrocution,
                damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.NoneSpecified),
                inflictedHurtbox = box
            };
            victim.TakeDamage(info);
            KitUtil.ReportHit(info, victim.gameObject);
        }

        private void OnDestroy()
        {
            // The body/scene has been removed: cancel rather than using a destroyed attacker.
            // Ordinary owner death is handled above and does not cancel a committed flight.
            if (NetworkServer.active && flight.Cancel())
                Plugin.Log.LogInfo("HOLLOW_SAINT_THUNDERBOLT_CANCELLED reason=owner-destroyed-after-launch");
        }
    }
}
