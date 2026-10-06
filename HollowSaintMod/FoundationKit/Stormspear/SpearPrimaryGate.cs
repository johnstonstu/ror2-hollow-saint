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
            // Anticipate a ready spear press without changing either input or stock.
            var secondary = body.skillLocator ? body.skillLocator.secondary : null;
            if (!action && bank && bank.skill2.down && secondary &&
                secondary.skillDef is StormspearSkillDef && secondary.CanExecute()) action = true;
            return input.Observe(action, StormspearCharge.InCrown(body), bank && bank.skill1.down);
        }
        private void FixedUpdate() { Observe(); }
        private void OnDisable() { input.Reset(); }
    }
}
