using System;

namespace HollowSaint.FoundationKit.Stormspear
{
    internal static class SpearFeedbackPolicy
    {
        internal const float FundedStrikeMultiplier = .85f, RechargeMultiplier = 1.2f;
        internal static float Direct(float rawCoefficient, float charge) =>
            KitDamagePolicy.Effective(rawCoefficient) * (1f - .1f * Math.Max(0f, Math.Min(1f, charge)));
        internal static float Recharge(float rawSeconds) => rawSeconds * RechargeMultiplier;
    }
}
