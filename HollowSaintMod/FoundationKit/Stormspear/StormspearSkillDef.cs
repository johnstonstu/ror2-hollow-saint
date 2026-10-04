using RoR2;
using RoR2.Skills;

namespace HollowSaint.FoundationKit.Stormspear
{
    /// <summary>GenericSkill.RunRecharge otherwise starts at activation (or at the charge/throw handoff).
    /// Keep its queue untouched until the projectile is actually released, then use vanilla scaling.</summary>
    public sealed class StormspearSkillDef : SkillDef
    {
        public override void OnFixedUpdate(GenericSkill skillSlot, float deltaTime)
        {
            var state = skillSlot.stateMachine ? skillSlot.stateMachine.state : null;
            var throwing = state as StormspearThrowState;
            if (StormspearCooldownPolicy.Pause(state is StormspearChargeState, throwing != null,
                throwing != null && throwing.CooldownReleased)) return;
            base.OnFixedUpdate(skillSlot, deltaTime);
        }
    }
}
