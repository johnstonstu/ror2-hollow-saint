using EntityStates;
using RoR2;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>
    /// Parks the "Weapon" and "Spear" machines while the beam runs, so Arc Bolt (priority Any) and
    /// Stormspear (priority Skill) cannot start. Arc Step lives on "Body" and stays usable; it
    /// cancels the beam instead. The authority leaves as soon as the Crown machine is no longer
    /// in GazeState (the transition networks to the other machines).
    /// </summary>
    public class GazeLockState : BaseState
    {
        private const float SafetySeconds = 10f;
        private EntityStateMachine crown;

        public override void OnEnter()
        {
            base.OnEnter();
            crown = EntityStateMachine.FindByCustomName(gameObject, KitRegistration.CrownMachineName);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && (!(crown && crown.state is GazeState) || fixedAge > SafetySeconds))
                outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() { return InterruptPriority.PrioritySkill; }
    }
}
