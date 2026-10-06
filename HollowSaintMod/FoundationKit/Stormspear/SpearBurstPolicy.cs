using System;

namespace HollowSaint.FoundationKit.Stormspear
{
    internal static class SpearBurstPolicy
    {
        // Retain the near-impact budget; the outer edge receives half the burst.
        internal const float EdgeFraction = 0.5f;
        internal static float Scale(float distance, float radius)
        {
            if (float.IsNaN(distance) || float.IsInfinity(distance) || float.IsNaN(radius) ||
                float.IsInfinity(radius) || radius <= 0f || distance < 0f || distance > radius) return 0f;
            return 1f - (1f - EdgeFraction) * distance / radius;
        }
        internal static bool Eligible(bool alive, bool primary, bool visible, float distance, float radius)
            => alive && !primary && visible && Scale(distance, radius) > 0f;
    }
}
