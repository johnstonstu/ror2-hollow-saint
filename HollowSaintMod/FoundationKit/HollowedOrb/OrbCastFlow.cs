using System;

namespace HollowSaint.FoundationKit.HollowedOrb
{
    internal static class OrbCastFlow
    {
        internal const float FirstChargeAt = .50f;
        internal const float MinimumWindup = .18f;
        internal const float ReleaseSettle = .06f;
        internal const float Recovery = .30f;
        internal const float AimAssistAngle = 12f;
        internal static float RequestAt(float releasedAt) => Math.Max(MinimumWindup, releasedAt) + ReleaseSettle;
    }
}
