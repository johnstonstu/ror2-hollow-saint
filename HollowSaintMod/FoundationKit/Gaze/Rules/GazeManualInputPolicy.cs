using System;

namespace HollowSaint.FoundationKit.Gaze
{
    internal static class GazeDurationPolicy
    {
        public const float MaximumBeamSeconds = 6f;
        public static float ForLevel(float configuredBase, float level)
        {
            if (float.IsNaN(configuredBase) || float.IsInfinity(configuredBase)) configuredBase = 4f;
            if (float.IsNaN(level) || float.IsInfinity(level)) level = 1f;
            float baseline = Math.Max(1f, Math.Min(MaximumBeamSeconds, configuredBase));
            return Math.Min(MaximumBeamSeconds, baseline + 0.10f * Math.Max(0f, Math.Min(20f, level - 1f)));
        }
        public static bool ValidSnapshot(float seconds) => !float.IsNaN(seconds) && !float.IsInfinity(seconds) && seconds >= 1f && seconds <= MaximumBeamSeconds;
    }

    internal static class GazeManualLifetime
    {
        public static bool StopBeforeWork(float age, float beamEnd) => age >= beamEnd;
    }

    /// <summary>A successful entry launch earns time; an intake only reserves the
    /// possibility of its own grant. Queued intakes never supply unearned time.</summary>
    internal static class GazeLaunchDurationPolicy
    {
        public const float ExtraPerLaunch = 2f;
        public const float MaximumActualSeconds = 14f;
        public static float Duration(float frozenBase, int successfulLaunches) => Math.Min(MaximumActualSeconds,
            frozenBase + ExtraPerLaunch * Math.Max(0, successfulLaunches));
        public static float Progress(float beamAge, float frozenBase) =>
            Math.Max(0f, Math.Min(1f, beamAge / Math.Max(0.1f, frozenBase)));
        public static float ProspectiveEnd(float actualEnd, float windup, int pendingIntakes) => actualEnd +
            Math.Max(0f, Math.Min(ExtraPerLaunch, MaximumActualSeconds - (actualEnd - windup) -
                ExtraPerLaunch * Math.Max(0, pendingIntakes)));
        public static bool CanAdmit(float age, float actualEnd, float windup, int pendingIntakes) =>
            age + GazeFuelSchedule.IntakeDuration + GazeManualRequestPolicy.FixedStepSafety < actualEnd &&
            GazeManualRequestPolicy.HasArrivalRoom(age, ProspectiveEnd(actualEnd, windup, pendingIntakes), true);
        public static bool CanLaunch(float age, float actualEnd, float windup) =>
            age < actualEnd && GazeManualRequestPolicy.HasArrivalRoom(age,
                ProspectiveEnd(actualEnd, windup, 0), false);
        public static bool ValidActualDuration(float seconds) => !float.IsNaN(seconds) && !float.IsInfinity(seconds) &&
            seconds >= 1f && seconds <= MaximumActualSeconds;
    }

    /// <summary>Seed from the cast press. Holding it never becomes a fueled tap;
    /// only a later release-to-press edge can request one orb.</summary>
    internal sealed class GazeTapEdges
    {
        private bool wasDown;
        public void Begin(bool castPressDown) { wasDown = castPressDown; }
        public bool Observe(bool down)
        {
            bool edge = down && !wasDown;
            wasDown = down;
            return edge;
        }
    }

    /// <summary>Polling and native CanExecute may observe the same frame in either
    /// order. Only an eligible mapped Primary edge can reach native OnExecute;
    /// observing an ineligible press discards it rather than queueing a held tap.</summary>
    internal sealed class GazePrimaryTapGate
    {
        private readonly GazeTapEdges edges = new GazeTapEdges();
        private bool pending;
        public void Begin(bool primaryHeld) { edges.Begin(primaryHeld); pending = false; }
        public bool Observe(bool down, bool eligible = true)
        {
            bool fresh = edges.Observe(down);
            if (!down || !eligible) pending = false;
            else if (fresh) pending = true;
            return pending;
        }
        public bool Take(bool down, bool eligible = true)
        {
            if (!Observe(down, eligible)) return false;
            pending = false;
            return true;
        }
    }

    /// <summary>Authoritative admission gate. Connection ownership is supplied from
    /// the live master by the controller, never trusted from the request payload.</summary>
    internal sealed class GazeManualRequestPolicy
    {
        public const float MinimumInterval = 0.25f;
        public const float FixedStepSafety = 0.05f;
        private uint cast, lastSequence;
        private float nextAllowed;
        private bool active;
        public void Begin(uint castId) { cast = castId; lastSequence = 0; nextAllowed = 0f; active = true; }
        public static bool HasArrivalRoom(float age, float beamEnd, bool includeIntake)
        {
            float required = (includeIntake ? GazeFuelSchedule.IntakeDuration : 0f) +
                GazeFuelSchedule.MaximumTravel + GazeFuelSchedule.SpreadDuration + FixedStepSafety;
            return !float.IsNaN(age) && !float.IsInfinity(age) && age + required < beamEnd;
        }
        public bool TryAccept(uint castId, uint sequence, bool authenticatedOwner, bool aliveInCurrentState,
            float age, float windup, float beamEnd, int availableEntry)
        {
            if (!active || !authenticatedOwner || !aliveInCurrentState || castId != cast || sequence == 0 || sequence <= lastSequence) return false;
            // Authenticated duplicates of rejected mash/late requests stay rejected.
            lastSequence = sequence;
            if (float.IsNaN(age) || float.IsInfinity(age) || age < windup || age < nextAllowed || availableEntry <= 0) return false;
            if (!HasArrivalRoom(age, beamEnd, true)) return false;
            nextAllowed = age + MinimumInterval;
            return true;
        }
        public void Cancel() { active = false; }
    }
}
