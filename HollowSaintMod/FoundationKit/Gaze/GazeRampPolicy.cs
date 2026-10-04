using System;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>Absolute successful entry-launch count, scoped to one cast. Intake,
    /// reserve gains and duplicate acknowledgements never earn a step.</summary>
    internal static class GazeRampPolicy
    {
        public const int MaximumSteps = 5;
        public const float DamagePerStep = 0.05f;
        public static int Steps(int successfulLaunches) => Math.Max(0, Math.Min(MaximumSteps, successfulLaunches));
        public static float DamageMultiplier(int successfulLaunches) => 1f + DamagePerStep * Steps(successfulLaunches);
    }
}
