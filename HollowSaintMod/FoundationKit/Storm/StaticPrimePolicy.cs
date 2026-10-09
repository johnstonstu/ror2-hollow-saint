using System;

namespace HollowSaint.FoundationKit.Storm
{
    /// <summary>Charge-spending hits prime Static without ever completing it.
    /// The finishing hit has to come from an ordinary Static source (Arc Bolt,
    /// spear, Circuit), so a cast never refunds its own cost directly.</summary>
    internal static class StaticPrimePolicy
    {
        internal const float MaxCap = .95f;
        internal static float Cap(float configured) => float.IsNaN(configured) || float.IsInfinity(configured)
            ? MaxCap : Math.Max(0f, Math.Min(MaxCap, configured));
        internal static float Amount(float configured, float scale) =>
            float.IsNaN(configured) || float.IsInfinity(configured) || float.IsNaN(scale) || float.IsInfinity(scale)
                ? 0f : Math.Max(0f, Math.Min(1f, configured)) * Math.Max(0f, Math.Min(1f, scale));
        /// <summary>New Static value; never lowers existing Static and never reaches full.</summary>
        internal static float Apply(float current, float amount, float cap)
        {
            if (float.IsNaN(current) || float.IsInfinity(current)) current = 0f;
            float limit = Cap(cap);
            if (amount <= 0f || current >= limit) return current;
            return Math.Min(limit, current + amount);
        }
    }
}
