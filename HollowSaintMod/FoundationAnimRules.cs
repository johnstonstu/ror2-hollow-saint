using System;

namespace HollowSaint
{
    // No Unity dependency (tested by tools/tests/Check-AnimRules.ps1): gesture layer choice,
    // run-stop eligibility and the idle fidget timer used by FoundationPresentation/KitAnim.
    internal static class FoundationAnimRules
    {
        /// <summary>Ground speed (m/s) above which a cast uses the arms-only layer. Just under
        /// the authored forward walk (1.5 m/s) so a walk keeps its torso too.</summary>
        internal const float GestureMoveSpeed = 1.2f;
        /// <summary>Recent run speed (m/s) needed for the Run stop clip on release.</summary>
        internal const float RunStopMinSpeed = 3.5f;
        /// <summary>Run stop is a forward clip: the last heading must be this forward (cosine).</summary>
        internal const float RunStopMinForward = 0.7f;
        /// <summary>How fast the remembered run speed decays after release (m/s per second).</summary>
        internal const float RecentSpeedDecay = 20f;
        internal const float FidgetMinSeconds = 8f;
        internal const float FidgetMaxSeconds = 15f;

        /// <summary>Use the same airborne-loop choice on normal frames and dash exit.</summary>
        internal static bool IsAscending(float verticalSpeed) { return verticalSpeed > 0.5f; }

        internal static bool UseArmsLayer(float planarSpeed, bool grounded)
        {
            return !grounded || planarSpeed > GestureMoveSpeed;
        }

        internal static float TrackRecentSpeed(float recent, float speed, float deltaTime)
        {
            return Math.Max(speed, recent - RecentSpeedDecay * Math.Max(0f, deltaTime));
        }

        internal static bool ShouldRunStop(float recentSpeed, float forwardFraction)
        {
            return recentSpeed >= RunStopMinSpeed && forwardFraction >= RunStopMinForward;
        }

        /// <summary>All walk/run loops plant L at phase 0 and R at 0.5; the foot that most
        /// recently planted (or is about to) is the one the stop pivots on.</summary>
        internal static bool RunStopOnRightFoot(float locomotionPhase)
        {
            float p = locomotionPhase - (float)Math.Floor(locomotionPhase);
            return p >= 0.25f && p < 0.75f;
        }

        /// <summary>Picks a fidget index in [0, count) from a roll in [0, 1), never repeating
        /// the previous one when there is a choice. Returns -1 when count is 0.</summary>
        internal static int PickFidget(int count, int previous, float roll)
        {
            if (count <= 0) return -1;
            if (count == 1) return 0;
            bool avoid = previous >= 0 && previous < count;
            int choices = avoid ? count - 1 : count;
            int pick = Math.Min(choices - 1, Math.Max(0, (int)(roll * choices)));
            if (avoid && pick >= previous) pick++;
            return pick;
        }
    }

    /// <summary>Counts down a random 8-15 s wait while the Saint is idle-eligible. Any
    /// ineligible tick (movement, input, gesture, air, combat flip) disarms it, so the next
    /// eligible tick starts a fresh wait.</summary>
    internal sealed class FoundationIdleFidgetTimer
    {
        private readonly Func<float, float, float> range;
        private float remaining = -1f;

        internal FoundationIdleFidgetTimer(Func<float, float, float> range) { this.range = range; }

        internal float Remaining { get { return remaining; } }
        internal bool Armed { get { return remaining >= 0f; } }

        internal void Cancel() { remaining = -1f; }

        internal bool Tick(bool eligible, float deltaTime)
        {
            if (!eligible) { remaining = -1f; return false; }
            if (remaining < 0f)
            {
                float wait = range(FoundationAnimRules.FidgetMinSeconds, FoundationAnimRules.FidgetMaxSeconds);
                remaining = Math.Max(FoundationAnimRules.FidgetMinSeconds, Math.Min(FoundationAnimRules.FidgetMaxSeconds, wait));
                return false;
            }
            remaining -= Math.Max(0f, deltaTime);
            if (remaining > 0f) return false;
            remaining = -1f;
            return true;
        }
    }
}
