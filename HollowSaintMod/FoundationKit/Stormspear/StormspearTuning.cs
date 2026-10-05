namespace HollowSaint.FoundationKit.Stormspear
{
    /// <summary>
    /// v0.9 Stormspear (secondary) tuning. Starting values, turned up first and tuned down in
    /// playtest. See project doc claude/v09-kit-rework.md. Bound to config section
    /// "2. Stormspear" by KitConfig.
    /// </summary>
    public static class StormspearTuning
    {
        // Charge
        public static float ChargeSeconds = 2.0f;            // tap to full at 1x attack speed (Artificer Nano-Spear: 2.0 s)
        public static float CrownChargeMultiplier = 2.5f;    // charge speed while Open Circuit is up
        public static float OffHandRateMultiplier = 0.5f;    // Arc Bolt fire-rate multiplier while charging in the hand
        public static float MinThrowInterval = 0.2f;         // seconds between tap throws when dumping stocks
        public static float HandReleaseDelay = 0.083f;       // v0.9.7: hand throws leave at the apex of the whip (5 ticks), not on the press

        // Stocks
        public static float Cooldown = 5f;
        public static int BaseStock = 1;

        // Throw
        public static float TapDamage = 3.5f;                // 350% at zero charge; 12.5% reduction across the throw curve
        public static float FullDamage = 14f;                // 1400% at full charge; burst inherits the reduction, crown Thunderbolt stays independent
        public static float ProcCoefficient = 1f;
        public static float ProjectileSpeed = 150f;
        public static float AssistConeDegrees = 3f;

        // Impact burst (AoE lightning that spreads from the impact point; scales with charge)
        public static float BurstRadiusTap = 3f;
        public static float BurstRadiusFull = 10f;           // v0.9.13: was 9
        public static float BurstDamageFraction = 0.5f;      // of the direct-hit damage, uncharged
        public static float BurstDamageFractionFull = 1f;    // v0.9.13 (Stu: full splash felt weak): fully charged
        public static float BurstProcCoefficient = 0.5f;
        // v0.9.10: the spear lodges in what it hits, crackles, then bursts. On an enemy the burst hits
        // everyone around it except the struck enemy; on the ground it is a weaker fizzle.
        public static float StickSeconds = 0.25f;            // lodged time before the burst
        public static float GroundBurstScale = 0.5f;         // damage and radius multiplier when the spear hits terrain

        // Visuals
        public static float HandSpearLength = 1.6f;          // metres at full charge in the hand
        public static float CrownSpearLength = 3.2f;         // metres at full charge above the crown
        public static float CrownSpearHeight = 0.55f;        // metres above the Halo socket (v0.9.1: sits on the open crown, in view)

        public static float DamageAt(float charge01) { return TapDamage + (FullDamage - TapDamage) * UnityEngine.Mathf.Clamp01(charge01); }
        public static float BurstRadiusAt(float charge01) { return BurstRadiusTap + (BurstRadiusFull - BurstRadiusTap) * UnityEngine.Mathf.Clamp01(charge01); }
        /// <summary>Inverse of DamageAt, used by the server-side impact to recover the throw's charge
        /// from the projectile damage and the owner's damage stat.</summary>
        public static float ChargeFromCoefficient(float coefficient)
        {
            float span = FullDamage - TapDamage;
            return span <= 0.0001f ? 1f : UnityEngine.Mathf.Clamp01((coefficient - TapDamage) / span);
        }
    }
}
