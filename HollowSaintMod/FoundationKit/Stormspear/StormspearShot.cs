using System;

namespace HollowSaint.FoundationKit.Stormspear
{
    /// <summary>
    /// Per-projectile throw snapshot. RoR2 transports force and comboNumber for remote
    /// FireProjectile calls and assigns them before ProjectileController.onInitialized.
    /// This custom prefab reserves combo 1 for crown throws. Its existing force curve
    /// (8 + 12 * charge) carries the full float charge without byte quantization or an
    /// owner cache. Keep encode/decode together if the knockback curve ever changes.
    /// </summary>
    internal readonly struct StormspearShot
    {
        internal const byte CrownCombo = 1;
        private const float TapForce = 8f, ChargeForce = 12f;

        internal readonly float Charge;
        internal readonly bool FromCrown;

        internal StormspearShot(float force, byte combo)
        {
            Charge = ClampCharge((force - TapForce) / ChargeForce);
            FromCrown = combo == CrownCombo;
        }

        internal static float ForceForCharge(float charge)
        {
            return TapForce + ChargeForce * ClampCharge(charge);
        }

        internal bool CallsThunderbolt(bool enabled, float minimumCharge, bool prayerFunded = false)
        {
            return !prayerFunded && enabled && FromCrown && Charge >= minimumCharge - 0.03f;
        }

        private static float ClampCharge(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }
    }
}
