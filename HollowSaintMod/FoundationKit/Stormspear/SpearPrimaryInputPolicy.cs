namespace HollowSaint.FoundationKit.Stormspear
{
    internal sealed class SpearPrimaryInputPolicy
    {
        private bool requireRelease;
        internal bool Observe(bool spearAction, bool circuit, bool primaryDown)
        {
            if (!primaryDown || circuit) requireRelease = false;
            if (spearAction && !circuit && primaryDown) requireRelease = true;
            return circuit || (!spearAction && !requireRelease);
        }
        internal void Reset() { requireRelease = false; }
    }
}
