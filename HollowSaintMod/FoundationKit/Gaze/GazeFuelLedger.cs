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

    /// <summary>At most five pulses, even with the supported twenty-orb cap. A full
    /// cast always contributes 2.5 damage coefficient per struck enemy, without a finale.</summary>
    internal sealed class GazeFuelSchedule
    {
        public const int MaxPhases = 5;
        public const float IntakeDuration = 0.32f;
        public const float SpreadDuration = 0.30f;
        private readonly int[] groups = new int[MaxPhases];
        private int intakes, launches;
        public int Count { get; private set; }
        public bool Active { get; private set; }
        public void Begin(int entry)
        {
            entry = Math.Max(0, Math.Min(20, entry));
            Count = Math.Min(MaxPhases, entry);
            for (int i = 0; i < MaxPhases; i++)
                groups[i] = i < Count ? entry / Count + (i < entry % Count ? 1 : 0) : 0;
            intakes = launches = 0;
            Active = true;
        }
        public int Group(int phase) => phase >= 0 && phase < Count ? groups[phase] : 0;
        public float IntakeAt(int phase) => 0.15f + phase * 0.70f;
        public bool TakeIntake(float beamAge, out int phase)
        {
            phase = intakes;
            if (!Active || intakes >= Count || beamAge < IntakeAt(intakes)) return false;
            intakes++;
            return true;
        }
        public bool TakeLaunch(float beamAge, out int phase)
        {
            phase = launches;
            if (!Active || launches >= intakes || beamAge < IntakeAt(launches) + IntakeDuration) return false;
            launches++;
            return true;
        }
        public void Cancel() { Active = false; }
        public static float Travel(float distance) => Math.Max(0.12f, Math.Min(0.22f, distance / 275f));
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
