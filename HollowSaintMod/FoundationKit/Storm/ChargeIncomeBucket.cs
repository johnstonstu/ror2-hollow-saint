using System;

namespace HollowSaint.FoundationKit.Storm
{
    /// <summary>Late-game flood guard: a token bucket over banked charges. Holds up to
    /// max(1, rate) tokens and refills at rate per second, so a single burst still banks
    /// but sustained income is capped. Rate 0 or less disables the guard.</summary>
    internal sealed class ChargeIncomeBucket
    {
        private float tokens = -1f, last;
        internal static float Capacity(float rate) => Math.Max(1f, rate);
        private void Refill(float rate, float now)
        {
            float cap = Capacity(rate);
            if (tokens < 0f || float.IsNaN(tokens)) { tokens = cap; last = now; return; }
            if (now > last) tokens = Math.Min(cap, tokens + (now - last) * rate);
            last = Math.Max(last, now);
        }
        internal bool Ready(float rate, float now)
        {
            if (!(rate > 0f) || float.IsInfinity(rate)) return true;
            Refill(rate, now);
            return tokens >= 1f;
        }
        internal bool TryTake(float rate, float now)
        {
            if (!(rate > 0f) || float.IsInfinity(rate)) return true;
            Refill(rate, now);
            if (tokens < 1f) return false;
            tokens -= 1f;
            return true;
        }
    }
}
