using System;

namespace HollowSaint.FoundationKit.OpenCircuit
{
    internal struct CircuitDwellPolicy
    {
        internal const float RequiredSeconds = 3f, RawZapCoefficient = 3f;
        internal const int Capacity = 64;
        internal float Seconds { get; private set; }
        internal bool Spent { get; private set; }
        internal float Progress => Math.Min(1f, Seconds / RequiredSeconds);
        internal bool Tick(bool inside, float dt)
        {
            if (!inside) { Seconds = 0f; return false; }
            if (Spent || float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0f) return false;
            Seconds = Math.Min(RequiredSeconds, Seconds + Math.Min(.1f, dt));
            if (Seconds + .0001f < RequiredSeconds) return false;
            Spent = true;
            return true;
        }
    }
}
