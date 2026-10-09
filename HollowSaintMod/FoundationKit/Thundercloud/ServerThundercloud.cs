using System.Collections.Generic;
using HollowSaint.FoundationKit.ChargedStorm;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Thundercloud
{
    /// <summary>Server-owned lingering storm over a fixed aimed area. Every pulse strikes each
    /// visible enemy beneath it once (damage, prime, Shock); visuals are networked effects.</summary>
    internal sealed class ServerThundercloud
    {
        private readonly CharacterBody owner;
        private readonly Stage stage;
        private readonly Vector3 center, sky;
        private readonly float radius, damage;
        private readonly int charges, pulses;
        private readonly bool crit;
        private float age;
        private int pulsed;
        internal float Duration => ThundercloudSchedule.CompleteAt(charges);
        internal int Charges => charges;
        private ServerThundercloud(CharacterBody body, int charges, Vector3 center, Vector3 sky, float radius)
        {
            owner = body; stage = Stage.instance; this.center = center; this.sky = sky; this.radius = radius; this.charges = charges;
            pulses = ThundercloudSchedule.Pulses(charges);
            damage = body.damage * KitDamagePolicy.Effective(ChargedStormTuning.CloudCoefficient(charges)); crit = body.RollCrit();
        }
        internal static ServerThundercloud Prepare(CharacterBody owner, int charges, Vector3 direction)
        {
            float radius = ChargedStormTuning.Radius(charges);
            Vector3 center = ChargedStormTargeting.AimPoint(owner, direction, ChargedStormTuning.Bound(ChargedStormTuning.CloudRange, 20f, 120f));
            Vector3 sky = ChargedStormTargeting.CloudCenter(center, radius);
            KitLog.Event("THUNDERCLOUD_PREPARE_" + charges, "charges=" + charges + " center=" + center + " sky=" + sky + " radius=" + radius);
            // A lingering storm can be placed ahead of enemies: no empty-area refusal.
            return new ServerThundercloud(owner, charges, center, sky, radius);
        }
        internal void Begin()
        {
            ChargedStormEffects.Cloud(owner, sky, radius, Duration);
            KitLog.Event("THUNDERCLOUD_BEGIN", "charges=" + charges + " pulses=" + pulses + " radius=" + radius);
        }
        internal bool Tick(float dt)
        {
            if (!owner || !owner.isActiveAndEnabled || !owner.healthComponent || !owner.healthComponent.alive || stage != Stage.instance) return false;
            if (owner.master && owner.master.GetBody() != owner) return false;
            age += dt;
            while (pulsed < pulses && age >= ThundercloudSchedule.PulseAt(pulsed)) { pulsed++; Pulse(); }
            return age < Duration;
        }
        private void Pulse()
        {
            var found = ChargedStormTargeting.Find(owner, center, radius, Vector3.forward, 180f, false);
            found.RemoveAll(h => !ChargedStormTargeting.CloudVisible(sky, center, ChargedStormTargeting.Point(h), radius));
            found.Sort((a, b) => (ChargedStormTargeting.Point(a) - center).sqrMagnitude.CompareTo((ChargedStormTargeting.Point(b) - center).sqrMagnitude));
            int limit = Mathf.Clamp(ChargedStormTuning.CloudTargetLimit, 1, 64);
            for (int i = 0; i < found.Count && i < limit; i++)
            {
                var victim = found[i];
                Vector3 point = ChargedStormTargeting.Point(victim);
                ChargedStormDamage.Hit(owner, victim, damage, crit, ChargedStormTuning.Bound(ChargedStormTuning.CloudProc, 0f, 1f), DamageSource.Special);
                if (victim.alive)
                {
                    Storm.StormServer.PrimeStatic(victim, owner, Storm.StaticPrimePolicy.Amount(ChargedStormTuning.CloudStaticPrime, 1f));
                    if (ChargedStormTuning.CloudShocks) Storm.StormServer.Shock(victim.body);
                }
                // Repeating pulses use a single stroke each; the first pulse keeps the full return strokes.
                ChargedStormEffects.Strike(owner, sky, point, radius, victim, center, pulsed == 1 ? ThundercloudSchedule.ReturnStrokeCount : 1);
            }
        }
    }
}
