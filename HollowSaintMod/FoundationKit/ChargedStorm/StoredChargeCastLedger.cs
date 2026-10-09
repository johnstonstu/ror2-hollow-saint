using System;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    /// <summary>Server lease over a frozen entry allowance. Gathering never debits the bank.</summary>
    internal sealed class StoredChargeCastLedger
    {
        internal const float FirstChargeAt = .12f, ChargeInterval = .25f;
        internal uint Token { get; private set; }
        internal bool Active { get; private set; }
        internal int Entry { get; private set; }

        internal bool Begin(uint token, int bank, bool allowEmpty = false)
        {
            if (Active || token == 0 || token <= Token || bank < (allowEmpty ? 0 : 1)) return false;
            Token = token; Entry = Math.Min(20, bank); Active = true;
            return true;
        }

        internal static int Gathered(float age, int entry, float firstChargeAt = FirstChargeAt)
        {
            if (float.IsNaN(age) || float.IsInfinity(age) || age < firstChargeAt) return 0;
            return Math.Min(Math.Max(0, Math.Min(20, entry)),
                1 + (int)Math.Floor((Math.Min(age, 30f) - firstChargeAt + .00001f) / ChargeInterval));
        }

        internal bool Spend(uint token, int requested, float serverAge, ref int bank, out int spent,
            bool allowEmpty = false, float firstChargeAt = FirstChargeAt)
        {
            spent = 0;
            if (!Active || token != Token || requested < (allowEmpty ? 0 : 1) || requested > Entry) return false;
            if (allowEmpty && requested == 0) { Active = false; return true; }
            int allowed = Gathered(serverAge, Entry, firstChargeAt);
            int count = Math.Min(requested, allowed);
            if (count < 1 || bank < count) return false;
            bank -= count; spent = count; Active = false;
            return true;
        }

        internal void Cancel(uint token) { if (token == Token) Active = false; }
        internal void Cancel() { Active = false; }
    }
}
