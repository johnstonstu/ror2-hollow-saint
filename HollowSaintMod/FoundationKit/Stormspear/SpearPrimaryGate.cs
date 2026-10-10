using EntityStates;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Stormspear
{
    [DisallowMultipleComponent]
    internal sealed class SpearPrimaryGate : MonoBehaviour
    {
        private CharacterBody body;
        private EntityStateMachine spear;
        private readonly SpearPrimaryInputPolicy input = new SpearPrimaryInputPolicy();
        private void Awake()
        {
            body = GetComponent<CharacterBody>();
            FindSpear();
        }
        private void FindSpear()
        {
            foreach (var machine in GetComponents<EntityStateMachine>())
                if (machine.customName == "Spear") { spear = machine; break; }
        }
        internal static bool Allows(CharacterBody body)
        {
            if (!body) return true;
            var gate = body.GetComponent<SpearPrimaryGate>();
            if (!gate) gate = body.gameObject.AddComponent<SpearPrimaryGate>();
            return gate.Observe();
        }
        private bool Observe()
        {
            if (!body || !body.healthComponent || !body.healthComponent.alive) { input.Reset(); return false; }
            if (!spear) FindSpear();
            var bank = body.inputBank;
            bool action = spear && (spear.state is StormspearChargeState || spear.state is StormspearThrowState);
            // Native input processing may consider primary before secondary in the same tick.
            // GenericSkill.CanExecute does not include the native input claim check.
            // A refunded mustKeyPress spear cannot restart from its claimed held press.
            var secondary = body.skillLocator ? body.skillLocator.secondary : null;
            // 1.3.2: Gaze's charge-up claims Secondary itself, so a tap there starts no spear and
            // must not cancel a bolt that is now allowed to fire through the charge.
            if (!action && bank && bank.skill2.down && secondary && !GazeOwnsCrown() &&
                secondary.skillDef is StormspearSkillDef &&
                (!secondary.skillDef.mustKeyPress || !bank.skill2.hasPressBeenClaimed) && secondary.CanExecute()) action = true;
            return input.Observe(action, StormspearCharge.InCrown(body), bank && bank.skill1.down);
        }
        private EntityStateMachine crown;
        private bool GazeOwnsCrown()
        {
            if (!crown) crown = EntityStateMachine.FindByCustomName(gameObject, KitRegistration.CrownMachineName);
            return crown && crown.state is Gaze.GazeState;
        }
        private void FixedUpdate() { Observe(); }
        private void OnDisable() { input.Reset(); }
    }
}
