using System;

namespace HollowSaint.FoundationKit.ArcBolt
{
    /// <summary>Range and visible envelope shared by the projectile and its cosmetic ghost.</summary>
    internal static class ArcBoltReliabilityRules
    {
        internal const float PreviousSpeed = 80f;

        // Faster custom settings change arrival time without extending the old range.
        // Slower settings retain their previous speed * template lifetime semantics.
        internal static float Lifetime(float templateLifetime, float speed) =>
            templateLifetime * Math.Min(1f, PreviousSpeed / speed);

        internal static float ContourRadius(float collisionRadius) => collisionRadius * 0.96f;
        internal static float ContourWidth(float collisionRadius) => collisionRadius * 0.08f;
        internal static float CoreDiameter(float collisionRadius) => collisionRadius * 1.2f;
    }
}
