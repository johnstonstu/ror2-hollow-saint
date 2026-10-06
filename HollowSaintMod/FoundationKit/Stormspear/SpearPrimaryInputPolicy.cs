namespace HollowSaint.FoundationKit.Stormspear
{
    internal sealed class SpearPrimaryInputPolicy
    {
        internal bool Observe(bool spearAction, bool circuit, bool primaryDown)
        {
            // Native held-primary polling resumes once the actual spear action ends.
            // Nothing is queued here and native stock/cadence still owns execution.
            return circuit || !spearAction;
        }
        internal void Reset() { }
    }
}
