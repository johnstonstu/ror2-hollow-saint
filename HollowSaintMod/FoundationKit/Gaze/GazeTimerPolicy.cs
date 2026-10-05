namespace HollowSaint.FoundationKit.Gaze
{
    internal static class GazeTimerPolicy
    {
        // Caller snapshots scale at entry and successful grants, never each countdown frame.
        internal static float Fill(float remaining, float scale = GazeLaunchDurationPolicy.MaximumActualSeconds) =>
            float.IsNaN(remaining) || float.IsInfinity(remaining) || float.IsNaN(scale) || float.IsInfinity(scale) ? 0f :
            System.Math.Max(0f, System.Math.Min(1f, remaining / System.Math.Max(.1f, scale)));
    }
}
