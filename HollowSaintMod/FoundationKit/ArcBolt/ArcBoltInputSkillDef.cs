using RoR2;
using RoR2.Skills;

namespace HollowSaint.FoundationKit.ArcBolt
{
    // Arc Bolt alone owns this restriction; Gaze's temporary primary override
    // keeps its own input and fuel semantics. Reject before native stock use.
    internal sealed class ArcBoltInputSkillDef : SkillDef
    {
        public override bool CanExecute(GenericSkill slot)
            => Stormspear.SpearPrimaryGate.Allows(slot.characterBody) && base.CanExecute(slot);
        public override bool IsReady(GenericSkill slot)
            => Stormspear.SpearPrimaryGate.Allows(slot.characterBody) && base.IsReady(slot);
    }
}
