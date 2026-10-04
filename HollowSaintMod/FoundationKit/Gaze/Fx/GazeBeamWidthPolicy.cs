using System;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    /// <summary>Presentation only: settled line widths stay inside the existing hit diameter.</summary>
    internal static class GazeBeamWidthPolicy
    {
        public static float Advance(float shownSteps, int launches, float dt)
        {
            float target = GazeRampPolicy.Steps(launches);
            if (!Finite(shownSteps)) shownSteps = 0f;
            shownSteps = Math.Max(0f, Math.Min(GazeRampPolicy.MaximumSteps, shownSteps));
            float change = Finite(dt) ? Math.Max(0f, dt) * 4f : 0f;
            return shownSteps < target ? Math.Min(target, shownSteps + change) : Math.Max(target, shownSteps - change);
        }

        public static float Body(float steps, float envelope, float radius) => Width(.65f, .40f, steps, envelope, radius);
        public static float Haze(float steps, float envelope, float radius) => Width(.90f, .41f, steps, envelope, radius);
        public static float Sheath(float steps, float envelope, float radius) => Width(.80f, .40f, steps, envelope, radius);
        public static float Core(float steps, float envelope, float radius) => Width(.09f, .016f, steps, envelope, radius);
        public static float Sweep(float envelope, float radius) => Width(2.95f, 0f, 0f, envelope, radius);

        private static float Width(float baseline, float increase, float steps, float envelope, float radius)
        {
            if (!Finite(radius) || radius <= 0f || !Finite(envelope) || envelope <= 0f) return 0f;
            steps = Finite(steps) ? Math.Max(0f, Math.Min(GazeRampPolicy.MaximumSteps, steps)) : 0f;
            return Math.Min(radius * 2f, (baseline + increase * steps) * envelope);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
