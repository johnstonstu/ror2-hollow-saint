using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Storm
{
    /// <summary>Explicit spear strikes only. A full Prayer bank never searches for
    /// a target, telegraphs or discharges automatically, including after a Gaze merge.</summary>
    [DisallowMultipleComponent]
    public sealed class ThunderboltDriver : MonoBehaviour
    {
        private CharacterBody body;
        private void Awake() { body = GetComponent<CharacterBody>(); }

        /// <summary>A qualifying landing resolves immediately from its launch snapshot;
        /// it cannot disappear into a saturated delayed-strike queue.</summary>
        internal void ServerPrayerStrikeAt(Vector3 point, HealthComponent victim, PrayerStrikeSnapshot snapshot)
        {
            if (!NetworkServer.active || !body || !isActiveAndEnabled || !snapshot.Empowered ||
                !body.healthComponent || !body.healthComponent.alive) return;
            Impact(point, victim, snapshot);
        }

        // Kept for the existing Gaze resource hook. There is no automatic telegraph
        // or passive flight left to cancel; explicit spear strikes are independent.
        internal void ClaimForGaze() { }

        private void Impact(Vector3 point, HealthComponent victim, PrayerStrikeSnapshot snapshot)
        {
            var victimBody = victim ? victim.body : null;
            // Cosmetic asset failure must not prevent an already committed hit.
            try { RoyalCapacitorFx.Strike(point, body); }
            catch (System.Exception error) { Plugin.Log.LogError("HOLLOW_SAINT_PRAYER_STRIKE_FX_ERROR " + error); }
            StormTelemetry.RecordStrike();
            KitLog.Event("THUNDERBOLT", "spear damage=" + snapshot.Damage.ToString("0.0") + " crit=" + snapshot.Crit + " funded=" + snapshot.Funded);
            var primedSplash = new System.Collections.Generic.List<HealthComponent>();
            StormServer.BeginStormDamage();
            try
            {
                if (victim && victim.alive && victimBody)
                {
                    var info = new DamageInfo
                    {
                        damage = snapshot.Damage, crit = snapshot.Crit,
                        attacker = body.gameObject, inflictor = body.gameObject,
                        position = point, force = Vector3.zero, procCoefficient = snapshot.Funded ? 1f : 0f,
                        damageColorIndex = DamageColorIndex.Electrocution,
                        damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.NoneSpecified),
                        inflictedHurtbox = victimBody.mainHurtBox
                    };
                    victim.TakeDamage(info);
                    if (snapshot.Funded) KitUtil.ReportHit(info, victim.gameObject);
                }
                if (snapshot.SplashDamage > 0f)
                {
                    var others = StormServer.FindEnemies(body, point, snapshot.SplashRadius, 8, victim);
                    for (int i = 0; i < others.Count; i++)
                    {
                        var health = others[i].healthComponent;
                        if (!health || !health.alive) continue;
                        var splash = StormServer.MakeInfo(body, others[i], snapshot.SplashDamage, snapshot.Crit, snapshot.Funded ? 0.5f : 0f);
                        health.TakeDamage(splash);
                        if (snapshot.Funded) KitUtil.ReportHit(splash, health.gameObject);
                        if (snapshot.Funded) primedSplash.Add(health);
                    }
                }
            }
            finally { StormServer.EndStormDamage(); }
            if (snapshot.Funded && victim && victim.alive && victimBody) StormServer.ElectrocuteFromStrike(victim, victimBody, body);
            // The full-bank Thunderbolt primes the pack around its Electrocuted target.
            foreach (var health in primedSplash)
                if (health && health.alive) StormServer.PrimeStatic(health, body, StaticPrimePolicy.Amount(ChargedStorm.ChargedStormTuning.ThunderboltSplashPrime, 1f));
        }
    }
}
