using System;
using HollowSaint.FoundationKit.ChargedStorm;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.HollowedOrb
{
    /// <summary>Server-only flight and collision; each visible segment is a separate network effect.</summary>
    internal sealed class ServerHollowedOrb
    {
        private readonly CharacterBody owner;
        private readonly Stage stage;
        private readonly OrbBouncePolicy<HealthComponent> policy;
        private readonly float diameter, damage, speed, range;
        private readonly bool crit;
        private Vector3 point, direction, destination;
        private HealthComponent target, last;
        private float remaining, age;
        private int segment;
        private bool completed;
        private bool relaying, latched, burst;
        private float nextLatch;
        private readonly int charges;
        private static uint nextFlight;
        internal static Action<string> DiagnosticTrace;
        private uint flight;
        internal ServerHollowedOrb(CharacterBody owner, int charges, Vector3 aim)
        {
            this.owner = owner; stage = Stage.instance; direction = aim; this.charges = charges;
            diameter = ChargedStormTuning.Diameter(charges); speed = ChargedStormTuning.Bound(ChargedStormTuning.OrbSpeed, 10f, 80f);
            range = ChargedStormTuning.BounceRange(charges);
            damage = owner.damage * KitDamagePolicy.Effective(ChargedStormTuning.OrbCoefficient(charges)); crit = owner.RollCrit();
            policy = new OrbBouncePolicy<HealthComponent>(ChargedStormTuning.HitBudget(charges));
            point = OrbCastGeometry.Point(owner, aim, charges);
            direction = OrbCastGeometry.Direction(owner, point, aim);
            remaining = ChargedStormTuning.Bound(ChargedStormTuning.OrbRange, 10f, 100f);
            target = OrbAimTargeting.Select(owner, point, aim, remaining);
            DiagnosticTrace?.Invoke("launch point=" + point + " diameter=" + diameter + " target=" + (target ? target.name : "none"));
        }
        internal void Begin() { SendSegment(); }
        private void SendSegment()
        {
            // Unique segment IDs prevent an old impact's delayed Start/packet
            // from stopping a newly spawned bounce on the same frame.
            if (flight != 0) ChargedStormEffects.OrbImpact(owner, point, diameter, flight, false);
            flight = ++nextFlight; if (flight == 0) flight = ++nextFlight;
            destination = target ? ChargedStormTargeting.Point(target) : point + direction * remaining;
            ChargedStormEffects.Orb(owner, point, destination, diameter, speed, target, flight, segment == 0);
        }
        internal bool Tick(float dt)
        {
            if (completed || !owner || !owner.isActiveAndEnabled || !owner.healthComponent || !owner.healthComponent.alive || stage != Stage.instance) return false;
            if (owner.master && owner.master.GetBody() != owner) return false;
            age += dt;
            if (latched) return TickLatched();
            if (age > 12f || remaining <= 0f) return false;
            if (Physics.CheckSphere(point, diameter * .4f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            { DiagnosticTrace?.Invoke("stop-world-overlap point=" + point + " relay=" + relaying); return false; }
            if (target && !(relaying && target == owner.healthComponent) && !ChargedStormTargeting.Enemy(owner, target))
            {
                target = policy.Choose(ChargedStormTargeting.Find(owner, point, Mathf.Min(range, remaining), Vector3.forward, 180f, true), last);
                if (!target) { Burst(); return false; }
                SendSegment();
            }
            destination = target ? ChargedStormTargeting.Point(target) : destination;
            Vector3 delta = destination - point;
            Vector3 travel = delta.sqrMagnitude > .0001f ? delta.normalized : direction;
            float step = OrbFlightMotion.Travel(delta.magnitude, speed, remaining, dt);
            var hits = Physics.SphereCastAll(point, diameter * .4f, travel, step,
                LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                var box = hit.collider ? hit.collider.GetComponent<HurtBox>() : null;
                var health = box ? box.healthComponent : null;
                if (relaying && health == owner.healthComponent)
                { point = owner.corePosition; return RelayImpact(true); }
                if (health && (health == last || health == owner.healthComponent || !ChargedStormTargeting.Enemy(owner, health))) continue;
                // A path to a fresh victim can cross a previously hit enemy.
                // Keep that crossing from defeating the fresh-before-revisit rule.
                if (health && health != target && policy.PriorHits(health) > 0) continue;
                point = hit.point;
                if (!health) { DiagnosticTrace?.Invoke("stop-collider name=" + (hit.collider ? hit.collider.name : "none") + " layer=" + (hit.collider ? hit.collider.gameObject.layer : -1) + " point=" + point); completed = true; Burst(); return false; }
                return Impact(health);
            }
            point += travel * step; remaining -= step;
            if (delta.magnitude <= step + diameter * .4f)
            {
                if (target) return relaying ? RelayImpact() : Impact(target);
                Burst(); return false;
            }
            return true;
        }
        private bool Impact(HealthComponent victim)
        {
            relaying = false;
            if (!ChargedStormTargeting.Enemy(owner, victim)) return false;
            if (!policy.Hit(victim, out float scale, out float proc)) { Burst(); return false; }
            point = ChargedStormTargeting.Point(victim);
            ChargedStormDamage.Hit(owner, victim, damage * scale, crit, proc, DamageSource.Secondary);
            Storm.StormServer.PrimeStatic(victim, owner, Storm.StaticPrimePolicy.Amount(ChargedStormTuning.OrbStaticPrime, scale));
            ChargedStormEffects.OrbImpact(owner, point, diameter, flight);
            last = victim;
            var candidates = ChargedStormTargeting.Find(owner, point, range, Vector3.forward, 180f, true);
            bool fresh = candidates.Exists(v => policy.PriorHits(v) == 0);
            float playerDistance = Vector3.Distance(point, owner.corePosition);
            float proximity = Vector3.Distance(victim.body.footPosition, owner.footPosition);
            DiagnosticTrace?.Invoke("enemy-impact candidates=" + candidates.Count + " fresh=" + fresh + " distance=" + playerDistance +
                " proximity=" + proximity + " clear=" + ChargedStormTargeting.Clear(point, owner.corePosition) + " ownerCore=" + owner.corePosition + " point=" + point + " budget=" + policy.Remaining +
                " victims=" + string.Join(",", candidates.ConvertAll(v => v.name + ":prior=" + policy.PriorHits(v))));
            target = policy.Choose(candidates, last);
            if (!target)
            {
                // Nothing else to bounce to: cling to this enemy and zap the rest of the budget.
                if (ChargedStormTuning.OrbLatch && policy.Remaining > 0 && victim.alive) { Latch(victim); return true; }
                Burst(); return false;
            }
            NextSegment();
            KitLog.Event("HOLLOWED_ORB_BOUNCE", "segment=" + segment + " remaining=" + policy.Remaining);
            return true;
        }
        private bool RelayImpact(bool hurtBoxContact = false)
        {
            // Do not invoke damage reporting, Hit(), Static, healing or procs.
            // Re-evaluate enemies now: a newly arrived fresh victim still wins.
            point = owner.corePosition;
            DiagnosticTrace?.Invoke("player-contact hurtBox=" + hurtBoxContact + " budget=" + policy.Remaining + " point=" + point);
            ChargedStormEffects.OrbImpact(owner, point, diameter * .55f, flight);
            relaying = false;
            var candidates = ChargedStormTargeting.Find(owner, point, range, Vector3.forward, 180f, true);
            // Keep A/B alternation across the player contact. A lone victim may
            // be revisited only after the visible owner segment has completed.
            target = policy.Choose(candidates, last);
            if (!target && candidates.Count == 1) target = policy.Choose(candidates, null);
            last = null;
            if (!target) return false;
            NextSegment(); return true;
        }
        private void NextSegment()
        {
            direction = (ChargedStormTargeting.Point(target) - point).normalized;
            // Trace from the impact itself; ignore the last victim rather than
            // teleporting a launch offset through nearby terrain.
            remaining = range; segment++; SendSegment();
        }
        private void Latch(HealthComponent victim)
        {
            latched = true; target = victim; nextLatch = age + Mathf.Clamp(ChargedStormTuning.OrbLatchInterval, .08f, 1f);
            flight = ++nextFlight; if (flight == 0) flight = ++nextFlight;
            ChargedStormEffects.Orb(owner, point, ChargedStormTargeting.Point(victim), diameter, speed, victim, flight, false);
            DiagnosticTrace?.Invoke("latch budget=" + policy.Remaining);
            KitLog.Event("HOLLOWED_ORB_LATCH", "remaining=" + policy.Remaining);
        }
        private bool TickLatched()
        {
            if (!ChargedStormTargeting.Enemy(owner, target))
            {
                // Latched enemy died: hop to whatever is left in reach, otherwise burst.
                latched = false;
                var next = policy.Choose(ChargedStormTargeting.Find(owner, point, range, Vector3.forward, 180f, true), target);
                if (!next) { Burst(); return false; }
                last = target; target = next; NextSegment(); return true;
            }
            point = ChargedStormTargeting.Point(target);
            if (age < nextLatch) return true;
            nextLatch = age + Mathf.Clamp(ChargedStormTuning.OrbLatchInterval, .08f, 1f);
            // A fresh enemy arriving within reach breaks the latch: fresh targets still come first.
            var candidates = ChargedStormTargeting.Find(owner, point, range, Vector3.forward, 180f, true);
            var fresh = candidates.Find(v => v != target && policy.PriorHits(v) == 0);
            if (fresh) { latched = false; last = target; target = fresh; NextSegment(); return true; }
            if (!policy.Hit(target, out float scale, out float proc)) { Burst(); return false; }
            ChargedStormDamage.Hit(owner, target, damage * scale, crit, proc, DamageSource.Secondary);
            Storm.StormServer.PrimeStatic(target, owner, Storm.StaticPrimePolicy.Amount(ChargedStormTuning.OrbStaticPrime, scale));
            ChargedStormEffects.OrbImpact(owner, point, diameter * .8f, 0);
            DiagnosticTrace?.Invoke("latch-zap budget=" + policy.Remaining);
            if (policy.Remaining == 0) { Burst(); return false; }
            return true;
        }
        /// <summary>The spent orb bursts where it ends: small free, bigger with charges.</summary>
        private void Burst()
        {
            if (burst || !owner) return;
            burst = true;
            float radius = ChargedStormTuning.BurstRadius(charges);
            float burstDamage = damage * ChargedStormTuning.Bound(ChargedStormTuning.OrbBurstFraction, 0f, 3f);
            var hit = ChargedStormTargeting.Find(owner, point, radius, Vector3.forward, 180f, false);
            foreach (var health in hit)
            {
                ChargedStormDamage.Hit(owner, health, burstDamage, crit, .3f, DamageSource.Secondary);
                if (health.alive) Storm.StormServer.PrimeStatic(health, owner, Storm.StaticPrimePolicy.Amount(ChargedStormTuning.OrbStaticPrime, .5f));
            }
            ChargedStormEffects.OrbBurst(owner, point, radius);
            DiagnosticTrace?.Invoke("burst radius=" + radius + " hits=" + hit.Count);
        }
        internal void End() { if (owner) ChargedStormEffects.OrbImpact(owner, point, diameter, flight, false); }
    }
}
