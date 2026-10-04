namespace HollowSaint.FoundationKit.Gaze
{
    internal static class GazeTimerPolicy
    {
        // Fixed fourteen-second scale: earned time visibly increases bar length.
        internal static float Fill(float remaining) => float.IsNaN(remaining) || float.IsInfinity(remaining) ? 0f :
            System.Math.Max(0f, System.Math.Min(1f, remaining / GazeLaunchDurationPolicy.MaximumActualSeconds));
    }
}
