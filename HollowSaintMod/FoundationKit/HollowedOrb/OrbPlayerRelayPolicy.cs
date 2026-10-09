namespace HollowSaint.FoundationKit.HollowedOrb
{
    /// <summary>The owner is a harmless relay, never another damaging victim.
    /// Only exhausted small groups need a relay; fresh enemies always win.</summary>
    internal static class OrbPlayerRelayPolicy
    {
        internal const float Range = 8f;
        internal static bool Allows(int remaining, int nearbyEnemies, bool hasFreshEnemy, float distance, bool clear) =>
            remaining > 0 && nearbyEnemies >= 1 && nearbyEnemies <= 2 && !hasFreshEnemy &&
            distance >= 0f && distance <= Range && clear;
    }
}
