using System;

namespace HollowSaint.FoundationKit.OpenCircuit
{
    /// <summary>Closed Circuit accounting for one crown window. Charges fed into the
    /// crown come back one per nearby Electrocute; whatever is still owed when the
    /// crown closes is discharged as closing strikes. Never returns more than was fed.</summary>
    internal sealed class ClosedCircuitLedger
    {
        internal int Fed { get; private set; }
        internal int Refunded { get; private set; }
        internal bool Active { get; private set; }
        internal int Owed => Active ? Math.Max(0, Fed - Refunded) : 0;

        /// <summary>Starts a window. Returns the previous window's owed count, which the
        /// caller discharges first (a recast during the crown closes the old circuit).</summary>
        internal int Begin(int fed)
        {
            int previous = Close();
            Fed = Math.Max(0, Math.Min(20, fed)); Refunded = 0; Active = Fed > 0;
            return previous;
        }
        internal bool TryRefund()
        {
            if (!Active || Refunded >= Fed) return false;
            Refunded++;
            return true;
        }
        /// <summary>Ends the window and returns the charges still owed.</summary>
        internal int Close()
        {
            int owed = Owed;
            Active = false; Fed = Refunded = 0;
            return owed;
        }
        internal void Cancel() { Active = false; Fed = Refunded = 0; }
    }
}
