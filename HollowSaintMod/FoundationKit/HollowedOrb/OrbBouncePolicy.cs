using System;
using System.Collections.Generic;

namespace HollowSaint.FoundationKit.HollowedOrb
{
    /// <summary>Call with eligible candidates sorted by distance; fresh targets precede revisits.</summary>
    internal sealed class OrbBouncePolicy<T> where T : class
    {
        private readonly Dictionary<T, int> hits = new Dictionary<T, int>();
        internal const double RepeatRetain = .75;
        internal int Remaining { get; private set; }
        internal OrbBouncePolicy(int budget) { Remaining = Math.Max(0, Math.Min(16, budget)); }
        internal int PriorHits(T target) => target != null && hits.TryGetValue(target, out int count) ? count : 0;
        internal T Choose(IEnumerable<T> candidates, T last)
        {
            if (Remaining == 0) return null;
            T revisit = null;
            foreach (T candidate in candidates)
            {
                if (candidate == null || ReferenceEquals(candidate, last)) continue;
                if (PriorHits(candidate) == 0) return candidate;
                if (revisit == null) revisit = candidate;
            }
            return revisit;
        }
        internal bool Hit(T target, out float damageScale, out float proc)
        {
            damageScale = proc = 0f;
            if (Remaining < 1 || target == null) return false;
            int prior = PriorHits(target);
            damageScale = (float)Math.Pow(RepeatRetain, prior); proc = prior == 0 ? .8f : .25f; // 1.3.1: was .5/.1 (items barely triggered); .3 is the burst
            hits[target] = prior + 1; Remaining--; return true;
        }
    }
}
