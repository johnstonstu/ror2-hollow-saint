using System;

namespace HollowSaint.FoundationKit.Gaze
{
    internal enum GazeFuelEndReason : byte { Completed, Recast, ArcStep, ControllerCancel, Interrupted, Death, Disabled }

    /// <summary>Server-owned entry fuel and a separate reserve bank. Intake is cosmetic;
    /// only launch spends fuel. Capacity is frozen for the entire cast.</summary>
    internal sealed class GazeFuelLedger
    {
        public int Capacity { get; private set; }
        public int Entry { get; private set; }
        public int Unspent { get; private set; }
        public int Reserve { get; private set; }
        public int Spent { get; private set; }
        public int AcceptedGains { get; private set; }
        public int RejectedGains { get; private set; }
        public bool HoldAfterMerge { get; private set; }
        public bool Active { get; private set; }
        public static int ClampCapacity(int configured) => Math.Max(2, Math.Min(20, configured));

        public void Begin(int entry, int capacity)
        {
            // A legitimately retained bank survives a live config reduction. The lower
            // configured cap takes full effect once this bank has decreased.
            Capacity = Math.Max(ClampCapacity(capacity), Math.Max(0, Math.Min(20, entry)));
            Entry = Unspent = Math.Max(0, Math.Min(Capacity, entry));
            Reserve = Spent = AcceptedGains = RejectedGains = 0;
            HoldAfterMerge = false;
            Active = true;
        }

        public bool TryGain()
        {
            if (!Active) return false;
            if (Unspent + Reserve >= Capacity) { RejectedGains++; return false; }
            Reserve++;
            AcceptedGains++;
            return true;
        }

        public bool TrySpend(int count)
        {
            if (!Active || count < 1 || count > Unspent) return false;
            Unspent -= count;
            Spent += count;
            return true;
        }

        public int End(bool alive)
        {
            if (!Active) return 0;
            Active = false;
            int retained = alive ? Unspent + Reserve : 0;
            HoldAfterMerge = alive && retained >= Capacity;
            Unspent = Reserve = 0;
            return retained;
        }
    }

    /// <summary>Manual taps queue one entry orb each. Intake is acknowledged before
    /// launch, but is refundable until the server spends it. Storage is fixed at twenty.</summary>
    internal sealed class GazeFuelSchedule
    {
        public const int MaxPhases = 20;
        public const float IntakeDuration = 0.32f;
        public const float SpreadDuration = 0.30f;
        public const float MaximumTravel = 0.55f;
        private readonly float[] launchAt = new float[MaxPhases];
        private readonly int[] groups = new int[MaxPhases], orbStarts = new int[MaxPhases];
        private int entry, launches, reserved, launchedOrbs;
        public int Count { get; private set; }
        public int PendingCount => reserved - launchedOrbs;
        public int GroupSize(int phase) => groups[phase];
        public int OrbStart(int phase) => orbStarts[phase];
        public bool Active { get; private set; }
        public void Begin(int entryCount)
        {
            entry = Math.Max(0, Math.Min(MaxPhases, entryCount));
            Count = launches = reserved = launchedOrbs = 0;
            Active = true;
        }
        public bool QueueIntake(float castAge, out int phase)
            => QueueIntake(castAge, 1, IntakeDuration, out phase);
        public bool QueueIntake(float castAge, int count, float intake, out int phase)
        {
            phase = Count;
            if (!Active || Count >= MaxPhases || count < 1 || reserved + count > entry) return false;
            groups[Count] = count; orbStarts[Count] = reserved; reserved += count;
            launchAt[Count++] = castAge + intake;
            return true;
        }
        public bool TakeLaunch(float castAge, out int phase)
        {
            phase = launches;
            if (!Active || launches >= Count || castAge < launchAt[launches]) return false;
            launchedOrbs += groups[launches++];
            return true;
        }
        public void Cancel() { Active = false; }
        // Server strike timing and transported presentation use this same duration.
        public static float Travel(float distance) => 0.35f + 0.20f * Math.Max(0f, Math.Min(1f, distance / 60f));
        public static float SpreadRadius(float beamAge, float beamSeconds, float baseRange, float reachStart, float reachEnd)
        {
            float progress = Math.Max(0f, Math.Min(1f, beamAge / Math.Max(0.1f, beamSeconds)));
            return Math.Max(0.1f, baseRange * (reachStart + (reachEnd - reachStart) * progress));
        }
        public static float Coefficient(int orbs, int capacity) => 0.5f * orbs * 5f / Clamp(capacity);
        private static int Clamp(int capacity) => GazeFuelLedger.ClampCapacity(capacity);
        public static float StrikeAt(float launchAge, float travel, float groundDistance, float radius) =>
            launchAge + travel + Math.Max(0f, Math.Min(1f, groundDistance / Math.Max(0.01f, radius))) * SpreadDuration;
    }
}
