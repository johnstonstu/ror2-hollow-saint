using System.Collections.Generic;
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
        private const float ExtraStrikeDelay = 0.2f;
        private struct ExtraStrike
        {
            internal Vector3 point;
            internal HealthComponent victim;
            internal float due;
            internal PrayerStrikeSnapshot snapshot;
        }
        private readonly List<ExtraStrike> extraStrikes = new List<ExtraStrike>(8);
        private void Awake() { body = GetComponent<CharacterBody>(); }

        /// <summary>Legacy unfunded Crown bonus preserves its short warning/delay.
        /// It does not consume stored Prayer or affect any funded projectile.</summary>
        public void ServerStrikeAt(Vector3 point, HealthComponent victim)
        {
            if (!NetworkServer.active || !body || extraStrikes.Count >= 8) return;
            extraStrikes.Add(new ExtraStrike
            {
                point = point, victim = victim, due = Time.time + ExtraStrikeDelay,
                snapshot = new PrayerStrikeSnapshot(body.damage, KitTuning.ThunderboltDamageCoefficient,
                    KitTuning.ThunderboltSplashFraction, KitTuning.ThunderboltSplashRadius, body.RollCrit())
            });
            KitFx.Server(Beat.ThunderTelegraph, point, default(Vector3), ExtraStrikeDelay + 0.1f, sound: false, owner: body);
        }

        /// <summary>A funded landing resolves immediately from its launch snapshot;
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

        private void FixedUpdate()
        {
            if (!NetworkServer.active || !body) return;
            float now = Time.time;
            for (int i = extraStrikes.Count - 1; i >= 0; i--)
            {
                var strike = extraStrikes[i];
                if (now < strike.due) continue;
                extraStrikes.RemoveAt(i);
                try { Impact(strike.point, strike.victim, strike.snapshot); }
                catch (System.Exception error) { Plugin.Log.LogError("HOLLOW_SAINT_EXTRA_STRIKE_ERROR " + error); }
            }
        }

        private void Impact(Vector3 point, HealthComponent victim, PrayerStrikeSnapshot snapshot)
        {
            var victimBody = victim ? victim.body : null;
            // Cosmetic asset failure must not prevent an already committed hit.
            try { RoyalCapacitorFx.Strike(point, body); }
            catch (System.Exception error) { Plugin.Log.LogError("HOLLOW_SAINT_PRAYER_STRIKE_FX_ERROR " + error); }
            StormTelemetry.RecordStrike();
            KitLog.Event("THUNDERBOLT", "spear damage=" + snapshot.Damage.ToString("0.0") + " crit=" + snapshot.Crit);
            StormServer.BeginStormDamage();
            try
            {
                if (victim && victim.alive && victimBody)
                {
                    var info = new DamageInfo
                    {
                        damage = snapshot.Damage, crit = snapshot.Crit,
                        attacker = body.gameObject, inflictor = body.gameObject,
                        position = point, force = Vector3.zero, procCoefficient = 1f,
                        damageColorIndex = DamageColorIndex.Electrocution,
                        damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.NoneSpecified),
                        inflictedHurtbox = victimBody.mainHurtBox
                    };
                    victim.TakeDamage(info);
                    KitUtil.ReportHit(info, victim.gameObject);
                }
                if (snapshot.SplashDamage > 0f)
                {
                    var others = StormServer.FindEnemies(body, point, snapshot.SplashRadius, 8, victim);
                    for (int i = 0; i < others.Count; i++)
                    {
                        var health = others[i].healthComponent;
                        if (!health || !health.alive) continue;
                        var splash = StormServer.MakeInfo(body, others[i], snapshot.SplashDamage, snapshot.Crit, 0.5f);
                        health.TakeDamage(splash);
                        KitUtil.ReportHit(splash, health.gameObject);
                    }
                }
            }
            finally { StormServer.EndStormDamage(); }
            if (victim && victim.alive && victimBody) StormServer.ElectrocuteFromStrike(victim, victimBody, body);
        }
        private void OnDisable() { extraStrikes.Clear(); }
    }
}
