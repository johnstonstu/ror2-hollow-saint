using System;

namespace HollowSaint.FoundationKit
{
    // Pure geometry limits shared by acquisition, steering and the offline checks.
    internal static class AimForgivenessRules
    {
        internal const float Range = 80f;
        internal const float TurnDegreesPerSecond = 45f;
        internal const float TotalTurnDegrees = 8f;

        internal static bool CanAcquire(float cosine, float distance, float coneDegrees, bool visible)
        {
            if (!visible || coneDegrees <= 0f || distance <= 0f || distance > Range) return false;
            double threshold = Math.Cos(Math.Min(6f, coneDegrees) * Math.PI / 180.0);
            return cosine > 0f && cosine >= threshold;
        }

        internal static float TurnStep(float errorDegrees, float deltaTime, float budget)
        {
            return Math.Max(0f, Math.Min(errorDegrees,
                Math.Min(TurnDegreesPerSecond * Math.Max(0f, deltaTime), budget)));
        }
    }
}
