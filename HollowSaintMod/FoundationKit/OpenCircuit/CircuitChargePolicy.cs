using System;

namespace HollowSaint.FoundationKit.OpenCircuit
{
    internal static class CircuitChargePolicy
    {
        internal static float Density(int charges) => 1f + .25f * Math.Max(0, Math.Min(9, charges) - 1);
        internal static float Interval(float baseInterval, int charges)
        {
            float interval = float.IsNaN(baseInterval) || float.IsInfinity(baseInterval) ? .5f : Math.Max(.05f, baseInterval);
            return Math.Max(Math.Min(interval, .08f), interval / Density(charges));
        }
        internal static int Arcs(int charges) => Math.Min(14, 6 + 2 * Math.Max(0, Math.Min(20, charges) - 1));
    }

    /// <summary>Extra empowered pulses deal damage, but only baseline cadence builds Static.</summary>
    internal sealed class CircuitPulseCadence
    {
        private bool first = true;
        private float pulseAge, staticAge;
        internal void Reset() { first = true; pulseAge = staticAge = 0f; }
        internal bool Tick(float dt, float interval, float baseline, bool immediate, out bool fundsStatic)
        {
            fundsStatic = false;
            if (first && immediate) { first = false; fundsStatic = true; return true; }
            pulseAge += dt; staticAge += dt;
            if (pulseAge + .00001f < interval) return false;
            first = false; pulseAge = Math.Max(0f, pulseAge - interval);
            fundsStatic = staticAge + .00001f >= baseline;
            if (fundsStatic) staticAge = Math.Max(0f, staticAge - baseline);
            return true;
        }
    }
}
