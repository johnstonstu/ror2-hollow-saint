using System;

namespace HollowSaint
{
    // No Unity dependency: these are the authored stride lengths and phase rules.
    internal static class FoundationLocomotionMath
    {
        internal const float WalkDuration = 26f / 24f;
        internal const float RunDuration = 16f / 24f;
        private static readonly float[] WalkSpeeds = { 1.5f, 1.05f, 0.6f, 0.8f, 1f, 0.8f, 0.6f, 1.05f };
        private static readonly float[] RunSpeeds = { 6f, 4.7f, 3.4f, 3.825f, 4.25f, 3.825f, 3.4f, 4.7f };

        internal readonly struct Sample
        {
            internal readonly float Gait, Rate, Duration, ReferenceSpeed;
            internal Sample(float gait, float rate, float duration, float referenceSpeed)
            { Gait = gait; Rate = rate; Duration = duration; ReferenceSpeed = referenceSpeed; }
        }

        internal static Sample Evaluate(float right, float forward, float actualSpeed)
        {
            float angle = (float)(Math.Atan2(right, forward) * 4.0 / Math.PI);
            if (angle < 0f) angle += 8f;
            int a = (int)Math.Floor(angle) % 8;
            int b = (a + 1) % 8;
            float t = angle - (float)Math.Floor(angle);
            // Adjacent feet slide along different headings. Use their projection
            // onto the requested direction so diagonals do not gain free stride.
            float projectionA = (float)Math.Cos(t * Math.PI / 4.0);
            float projectionB = (float)Math.Cos((1f - t) * Math.PI / 4.0);
            float walk = WalkSpeeds[a] * (1f - t) * projectionA + WalkSpeeds[b] * t * projectionB;
            float run = RunSpeeds[a] * (1f - t) * projectionA + RunSpeeds[b] * t * projectionB;
            float speed = Math.Max(0f, actualSpeed);
            float gait = Clamp((speed - walk) / (run - walk), 0f, 1f);
            float duration = Lerp(WalkDuration, RunDuration, gait);
            // Unity synchronizes normalized phases across both trees. Blend stride
            // distance and cycle duration, rather than assuming every clip is 6m/s.
            float stride = Lerp(walk * WalkDuration, run * RunDuration, gait);
            float reference = stride / duration;
            return new Sample(gait, speed / reference, duration, reference);
        }

        internal static float MovingEntryPhase(string previous, float retainedPhase)
        {
            if (previous == "Run start") return 0.5f; // authored end: R contact, run f9
            if (previous == "Glide exit") return 0f; // authored end: L contact, run f1
            return retainedPhase - (float)Math.Floor(retainedPhase);
        }

        internal static bool TryGetAuthoredStride(string clipName, out float right, out float forward, out float duration)
        {
            right = forward = duration = 0f;
            bool walk = clipName.StartsWith("Walk ", StringComparison.Ordinal);
            bool run = clipName.StartsWith("Run ", StringComparison.Ordinal);
            if (!walk && !run) return false;
            int index;
            switch (clipName.Substring(walk ? 5 : 4))
            {
                case "forward": index = 0; break;
                case "forward right": index = 1; break;
                case "right": index = 2; break;
                case "backward right": index = 3; break;
                case "backward": index = 4; break;
                case "backward left": index = 5; break;
                case "left": index = 6; break;
                case "forward left": index = 7; break;
                default: return false;
            }
            duration = walk ? WalkDuration : RunDuration;
            float distance = (walk ? WalkSpeeds[index] : RunSpeeds[index]) * duration;
            right = (float)Math.Sin(index * Math.PI / 4.0) * distance;
            forward = (float)Math.Cos(index * Math.PI / 4.0) * distance;
            return true;
        }

        private static float Lerp(float a, float b, float t) { return a + (b - a) * t; }
        private static float Clamp(float v, float lo, float hi) { return Math.Max(lo, Math.Min(hi, v)); }
    }

    internal sealed class FoundationFootContactClock
    {
        private bool initialized;
        private int halfCycle;

        internal void Reset() { initialized = false; }

        // All walk/run directions share L at phase0 and R at phase0.5. Never
        // burst old contacts after a hitch, state entry or animator time rewind.
        internal bool Advance(float normalizedTime)
        {
            int next = (int)Math.Floor(normalizedTime * 2f + 0.00001f);
            bool contact = initialized && next > halfCycle && next - halfCycle <= 2;
            halfCycle = next;
            initialized = true;
            return contact;
        }
    }
}
