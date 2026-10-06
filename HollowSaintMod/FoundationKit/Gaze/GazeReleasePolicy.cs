using System;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>Private experiment. The environment switch is for paired dev captures,
    /// never a config migration or a change to the stored public tuning.</summary>
    internal static class GazeReleaseTuning
    {
        internal static bool Enabled { get; private set; } = Environment.GetEnvironmentVariable("HS_GAZE_BASELINE") != "1";
        /// <summary>Dev A/B only (config "Legacy tap pulses", read once at startup). Never
        /// flipped mid-session: packets and casts assume one mode per process.</summary>
        internal static void UseLegacyTapPulses(bool legacy) { if (legacy) Enabled = false; }
        internal const float BeamSeconds = 7f;
        // 1.2 "snap" pass (Stu: charge too slow, release lacks punch, tiers hard to read).
        internal static float SecondsPerExtraCharge = .28f;   // was .45: full at 0.56 s
        internal const int MaximumLoaded = 3;
        internal const float RecoverySeconds = .40f;         // was .65
        internal const float ReleaseIntake = .03f;           // was .06
        internal static float DamagePerCharge = 4f;
        // The surge is a lightning strike, not a projectile: near-instant arrival so the hit
        // lands with the button release (was 0.35-0.55 s travel + 0.30 s ground spread).
        internal const float TravelBase = .06f, TravelPerRange = .10f, MaximumTravel = TravelBase + TravelPerRange;
        internal const float SpreadSeconds = .14f;
        internal const float OpeningTravelExtra = .16f;      // the opening wave is slowed so it can be seen
        // Surges now proc items and shove: 0.4 / 0.8 / 1.0 proc, light knockback per charge.
        internal static float ProcPerCharge = .4f;
        internal const float ForcePerCharge = 450f;
        internal static float Travel(float distance) =>
            TravelBase + TravelPerRange * Math.Max(0f, Math.Min(1f, distance / 60f));
        // 1.2 charge-up opening blast: up to the full bank, wider with every charge.
        internal const int MaximumOpening = 20;
        internal static float OpeningRadius(int group) => 3f + 1.2f * Math.Max(1, group);
        internal static float Proc(int group) => Math.Min(1f, ProcPerCharge * Math.Max(1, group));
        internal static bool ArrivalFits(float age, float end) =>
            !float.IsNaN(age) && !float.IsInfinity(age) &&
            age + ReleaseIntake + MaximumTravel + SpreadSeconds + .05f < end;
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
