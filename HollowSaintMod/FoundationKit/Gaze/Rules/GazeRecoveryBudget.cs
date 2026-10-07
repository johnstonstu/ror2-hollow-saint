using System;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>Base healing only. Native items may amplify, defer or block it.
    /// A claimed spend never earns deferred credit for missing health or blocked healing.</summary>
    internal sealed class GazeRecoveryBudget
    {
        internal const float FullBankFraction = .05f;
        private int capacity, entry, claimed;
        private double maximum, issued;

        internal void Begin(float maximumHealth, int frozenCapacity, int entryFuel)
        {
            capacity = Math.Max(2, Math.Min(20, frozenCapacity));
            entry = Math.Max(0, Math.Min(capacity, entryFuel));
            claimed = 0;
            maximum = float.IsNaN(maximumHealth) || float.IsInfinity(maximumHealth) ? 0d : Math.Max(0d, maximumHealth) * FullBankFraction;
            issued = 0d;
        }

        // The caller supplies the ledger's absolute count AFTER a successful spend.
        // Duplicate/old counts, reserve gains and counts beyond entry cannot heal.
        internal float Claim(int successfulSpends)
        {
            if (successfulSpends <= claimed || successfulSpends > entry) return 0f;
            claimed = successfulSpends;
            double amount = Math.Max(0d, Math.Min(maximum / capacity, maximum - issued));
            issued += amount;
            return (float)amount;
        }

        internal void Clear() { entry = claimed = 0; maximum = issued = 0d; }
    }
}
