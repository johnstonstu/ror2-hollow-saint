namespace HollowSaint.FoundationKit.Stormspear
{
    /// <summary>Custom SpearStuck EffectData word: low six bits are beat 41; upper 26 are owner net ID.
    /// Victim stays in the packet's native networked-object reference. Oversized owner IDs fail short.</summary>
    internal static class SpearStuckEvent
    {
        internal const uint BeatId = 41u, MaxOwnerId = 0x03FFFFFFu;
        internal static uint Encode(uint ownerId)
        { return ((ownerId <= MaxOwnerId ? ownerId : 0u) << 6) | BeatId; }
        internal static bool IsStuck(uint word) { return (word & 63u) == BeatId; }
        internal static uint OwnerId(uint word) { return IsStuck(word) ? word >> 6 : 0u; }
    }
}
