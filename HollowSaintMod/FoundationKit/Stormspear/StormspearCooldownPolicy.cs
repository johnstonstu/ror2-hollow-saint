namespace HollowSaint.FoundationKit.Stormspear
{
    /// <summary>Pause the existing recharge queue, without resetting progress or spending spare stocks.</summary>
    internal static class StormspearCooldownPolicy
    {
        internal static bool Pause(bool charging, bool throwing, bool released)
        {
            return charging || (throwing && !released);
        }

        internal static bool CanRefund(bool released, bool authority, bool alive, int stock, int maxStock)
        {
            return !released && authority && alive && stock < maxStock;
        }
    }
}
