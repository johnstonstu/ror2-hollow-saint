using System;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    /// <summary>Local 1.3 prototype defaults, deliberately independent of released skill values.</summary>
    public static class ChargedStormTuning
    {
        public static float CloudRadius = 16f, CloudRadiusPerCharge = 3.5f, CloudRange = 80f;
        public static float CloudCooldown = 12f;
        // Lingering storm: per-strike damage, strike cadence and duration all grow with charges.
        public static float CloudStrikeDamage = .9f, CloudStrikeDamagePerCharge = .18f, CloudStrikeInterval = .75f, CloudProc = .4f;
        public static float CloudBaseDuration = 3f, CloudDurationPerCharge = 1f;
        public static bool CloudFreeCast = true;
        public static float OrbDiameter = .6f, OrbDiameterPerCharge = .1f;
        public static float OrbDamage = 4.1f, OrbDamagePerCharge = .8f, OrbCooldown = 7f;
        public static float OrbSpeed = 32f, OrbRange = 70f, OrbBounceRange = 18f;
        public static float OrbBounceRangePerCharge = 4.5f;
        public static int OrbBaseHits = 4, CloudTargetLimit = 48;
        // Latching storm ball: with no other target it clings and zaps its remaining hits, then bursts.
        public static bool OrbLatch = true;
        public static float OrbLatchInterval = .22f, OrbBurstFraction = .6f, OrbBurstRadius = 3f, OrbBurstRadiusPerCharge = .8f;
        internal static float BurstRadius(int charges) => Bound(OrbBurstRadius + OrbBurstRadiusPerCharge * Math.Max(0, Math.Min(20, charges)), 1f, 12f);
        public static int CastChargeLimit = 5;
        // Storm flow: charged hits prime enemy Static (never past the cap) so
        // Arc Bolt/spear follow-ups Electrocute them and refill the bank.
        public static float OrbStaticPrime = .35f, CloudStaticPrime = .35f, StaticPrimeCap = .95f;
        public static float StaticPrimeHold = 1f;
        public static bool CloudShocks = true;
        // The rest of the kit joins the loop: every charge spender primes, Circuit pays back.
        public static float GazeStaticPrime = .35f, ThunderboltSplashPrime = .35f;
        public static bool ClosedCircuit = true, CircuitFeedsPrimed = true;
        public static float SpearPrimedMultiplier = 2f;
        public static bool ThunderboltNeedsFullCharge = true;
        public static float ChargeIncomePerSecond = 2f;
        public static float ClosedCircuitReach = 12f, ClosingNovaDamage = 1.6667f, ClosingNovaRadius = 12f, ClosingStrikePrime = .35f;
        internal static int CastLimit => Math.Max(1, Math.Min(20, CastChargeLimit));
        internal static int DescriptionChargeLimit => Math.Min(CastLimit, Math.Max(2, Math.Min(20, KitTuning.StormChargeMax)));

        internal static float Extra(int charges) => Math.Max(0, Math.Min(20, charges) - 1);
        internal static float Radius(int charges) => Bound((CloudRadius + CloudRadiusPerCharge * Extra(charges)) * (charges <= 0 ? .75f : 1f), 6f, 40f);
        internal static float CloudDuration(int charges) => Bound(CloudBaseDuration + CloudDurationPerCharge * Math.Max(0, Math.Min(20, charges)), 1f, 15f);
        internal static float Diameter(int charges) => Bound(OrbDiameter + OrbDiameterPerCharge * Extra(charges), .3f, 1.5f);
        internal static float BounceRange(int charges) => Bound(Bound(OrbBounceRange, 4f, 30f) +
            Bound(OrbBounceRangePerCharge, 0f, 10f) * Extra(charges), 4f, 60f);
        internal static float CloudCoefficient(int charges) => Bound(CloudStrikeDamage + CloudStrikeDamagePerCharge * Math.Max(0, Math.Min(20, charges)), .05f, 20f);
        internal static float OrbCoefficient(int charges) => Bound(OrbDamage + OrbDamagePerCharge * OrbExtra(charges), .1f, 20f);
        internal static int HitBudget(int charges) => Math.Max(1, Math.Min(16, OrbBaseHits + OrbExtra(charges)));
        private static int OrbExtra(int charges) => Math.Max(0, Math.Min(20, charges)) - 1;
        internal static float Bound(float value, float min, float max) =>
            float.IsNaN(value) || float.IsInfinity(value) ? min : Math.Max(min, Math.Min(max, value));
    }
}
