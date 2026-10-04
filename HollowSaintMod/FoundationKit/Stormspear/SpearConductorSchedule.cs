using System;

namespace HollowSaint.FoundationKit.Stormspear
{
    /// <summary>Immutable projectile initialization snapshot; four weak ticks, at most two distinct victims per tick.</summary>
    internal readonly struct SpearConductorSchedule
    {
        internal const int TickCount = 4, VictimsPerTick = 2, SearchCapacity = 64;
        internal const float Interval = 0.75f, Lifetime = TickCount * Interval;
        internal const float TapCoefficient = 0.20f, FullCoefficient = 0.35f;
        internal const float TapRadius = 4f, FullRadius = 6f;
        internal readonly float Radius, Damage;

        internal SpearConductorSchedule(float charge, float directDamage, float directCoefficient)
        {
            float c = Math.Max(0f, Math.Min(1f, charge));
            Radius = TapRadius + (FullRadius - TapRadius) * c;
            // directCoefficient must include the launch policy, recovering the frozen
            // damage stat without inheriting a second reduction from the direct hit.
            Damage = directCoefficient > 0f ? Math.Max(0f, directDamage) / directCoefficient *
                KitDamagePolicy.Effective(TapCoefficient + (FullCoefficient - TapCoefficient) * c) : 0f;
        }

        internal static bool Due(int completedTicks, float age)
        {
            return completedTicks < TickCount && age + 0.0001f >= (completedTicks + 1) * Interval;
        }
    }
}
