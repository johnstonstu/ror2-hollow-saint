using RoR2;
using RoR2.Skills;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    /// <summary>Preserve equipped skill behavior; delay native activation while
    /// a stored-charge gather owns the hands/crown. Utility is queued after cancellation.</summary>
    public sealed class StoredChargeCompatibleSkillDef : SkillDef
    {
        public override bool IsReady(GenericSkill slot)
            => !StoredChargeState.IsGathering(slot.characterBody) && base.IsReady(slot);
        public override bool CanExecute(GenericSkill slot)
            => !StoredChargeState.IsGathering(slot.characterBody) && base.CanExecute(slot);
    }
}
