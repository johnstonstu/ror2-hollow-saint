using System;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>Private experiment. The environment switch is for paired dev captures,
    /// never a config migration or a change to the stored public tuning.</summary>
    internal static class GazeReleaseTuning
    {
        internal static readonly bool Enabled = Environment.GetEnvironmentVariable("HS_GAZE_BASELINE") != "1";
        internal const float BeamSeconds = 7f;
        internal const float SecondsPerExtraCharge = .45f;
        internal const int MaximumLoaded = 3;
        internal const float RecoverySeconds = .65f;
        internal const float ReleaseIntake = .06f;
        internal const float DamagePerCharge = 4f;
        internal static bool ArrivalFits(float age, float end) =>
            !float.IsNaN(age) && !float.IsInfinity(age) &&
            age + ReleaseIntake + GazeFuelSchedule.MaximumTravel + GazeFuelSchedule.SpreadDuration + .05f < end;
    }

    internal enum GazeReleaseEdge { None, Begin, Release, Cancel }

    /// <summary>Held-on-entry and unavailable presses must be released before loading.
    /// A release is observed independently of native HandleSkill, which only sees down.</summary>
    internal sealed class GazeReleaseInput
    {
        private bool previous, loading;
        internal void Begin(bool held) { previous = held; loading = false; }
        internal GazeReleaseEdge Observe(bool down, bool canBegin, bool active)
        {
            bool pressed = down && !previous, released = !down && previous;
            previous = down;
            if (!active) { bool cancel = loading; loading = false; return cancel ? GazeReleaseEdge.Cancel : GazeReleaseEdge.None; }
            if (pressed && canBegin) { loading = true; return GazeReleaseEdge.Begin; }
            if (released && loading) { loading = false; return GazeReleaseEdge.Release; }
            return GazeReleaseEdge.None;
        }
    }

    /// <summary>Only the server clock determines how many charges a hold earns.
    /// Preparing never spends; full holds do not auto-fire or load another batch.</summary>
    internal sealed class GazeReleaseHold
    {
        private float started, nextReady;
        private int admitted;
        internal bool Active { get; private set; }
        internal bool Ready(float now) => now >= nextReady;
        internal bool Begin(float now, int available)
        {
            if (Active || !Ready(now) || available < 1 || float.IsNaN(now) || float.IsInfinity(now)) return false;
            Active = true; started = now; admitted = Math.Min(available, GazeReleaseTuning.MaximumLoaded);
            return true;
        }
        internal int Loaded(float now, int available)
        {
            if (!Active || now < started || float.IsNaN(now) || float.IsInfinity(now)) return 0;
            int count = 1 + (int)Math.Floor((now - started + .00001f) / GazeReleaseTuning.SecondsPerExtraCharge);
            return Math.Max(0, Math.Min(Math.Min(admitted, available), count));
        }
        internal int Release(float now, int available, bool arrivalFits)
        {
            int count = arrivalFits ? Loaded(now, available) : 0;
            Active = false; admitted = 0;
            if (count > 0) nextReady = now + GazeReleaseTuning.RecoverySeconds;
            return count;
        }
        internal void Cancel() { Active = false; admitted = 0; }
        internal void Reset() { Cancel(); nextReady = 0f; }
    }
}
