namespace HollowSaint.FoundationKit.Storm
{
    /// <summary>Server flight deadline and impact-based cooldown; contains no presentation state.</summary>
    internal sealed class ThunderboltFlight
    {
        internal bool Pending { get; private set; }
        private float impactAt;
        private float lastImpact = float.NegativeInfinity;

        internal bool Ready(float now, float cooldown) { return !Pending && now - lastImpact >= cooldown; }

        internal bool Launch(float now, float cooldown)
        {
            if (!Ready(now, cooldown)) return false;
            Pending = true;
            impactAt = now + UnityEngine.Mathf.Max(0.1f, KitTuning.ThunderboltFlightSeconds);
            return true;
        }

        internal bool Impact(float now)
        {
            if (!Pending || now < impactAt) return false;
            Pending = false; // close before damage/on-hit callbacks can re-enter
            lastImpact = now;
            return true;
        }

        internal bool Cancel()
        {
            bool pending = Pending;
            Pending = false;
            return pending;
        }
    }
}
