using RoR2;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    internal static class ChargedStormDamage
    {
        internal static void Hit(CharacterBody owner, HealthComponent health, float damage, bool crit, float proc, DamageSource source)
        {
            if (!NetworkServer.active || !ChargedStormTargeting.Enemy(owner, health)) return;
            var info = new DamageInfo
            {
                damage = damage, crit = crit, procCoefficient = proc, attacker = owner.gameObject,
                inflictor = owner.gameObject, position = ChargedStormTargeting.Point(health),
                damageColorIndex = DamageColorIndex.Electrocution,
                damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, source),
                inflictedHurtbox = health.body.mainHurtBox
            };
            // Spending skills cannot directly regenerate their own Static Charge cost.
            Storm.StormServer.BeginStormDamage();
            try { health.TakeDamage(info); KitUtil.ReportHit(info, health.gameObject); }
            finally { Storm.StormServer.EndStormDamage(); }
        }
    }
}
