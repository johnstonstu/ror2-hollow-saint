namespace HollowSaint.FoundationKit.Stormspear
{
    /// <summary>Fixed capacity nearest distinct entity selection, shared with deterministic checks.</summary>
    internal sealed class SpearConductorTargets
    {
        private readonly int[] ids = new int[SpearConductorSchedule.VictimsPerTick];
        private readonly float[] distances = new float[SpearConductorSchedule.VictimsPerTick];
        internal int Count { get; private set; }
        internal void Clear() { Count = 0; }

        internal int Select(int id, float distanceSquared)
        {
            for (int i = 0; i < Count; i++)
                if (ids[i] == id)
                {
                    if (distanceSquared >= distances[i]) return -1;
                    distances[i] = distanceSquared;
                    return i;
                }
            int index = Count;
            if (Count == ids.Length)
            {
                index = distances[0] > distances[1] ? 0 : 1;
                if (distanceSquared >= distances[index]) return -1;
            }
            else Count++;
            ids[index] = id; distances[index] = distanceSquared;
            return index;
        }
    }
}
