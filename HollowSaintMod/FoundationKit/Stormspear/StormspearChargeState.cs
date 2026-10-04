using EntityStates;
using HollowSaint.FoundationKit.OpenCircuit;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Stormspear
{
    /// <summary>
    /// Stormspear charge. Runs on the "Spear" machine on every machine, so Arc Bolt on "Weapon"
    /// keeps firing. The authority holds while secondary is down and hands off to
    /// StormspearThrowState (carrying the exact charge) on release. Every machine integrates the
    /// charge locally and mirrors it into StormspearCharge, which drives all presentation.
    /// No animation, sound or VFX is played here.
    /// </summary>
    public class StormspearChargeState : BaseSkillState
    {
        private StormspearCharge comp;
        private float charge;
        private SpearForm form;
        private bool thrown;
        private bool spearLeft = true, gotHand;

        // The owner's spear hand rides on the charge (and throw) state: it can only change while no spear
        // is out, so every machine has it before the spear shows, late joiners included.
        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(SpearDischarge.SpearCarry.NetworkHandOf(characterBody));
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            spearLeft = reader.ReadBoolean();
            gotHand = true;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            if (gotHand) SpearDischarge.SpearCarry.ApplyNetworkHand(characterBody, spearLeft);
            comp = StormspearCharge.Of(characterBody);
            form = StormspearCharge.InCrown(characterBody) ? SpearForm.Crown : SpearForm.Hand;
            if (comp) comp.Begin(form);
            if (characterBody) characterBody.SetAimTimer(2f);
            KitLog.Event("STORMSPEAR_CHARGE", "form=" + form);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            form = StormspearCharge.InCrown(characterBody) ? SpearForm.Crown : SpearForm.Hand;
            float rate = attackSpeedStat * (form == SpearForm.Crown ? StormspearTuning.CrownChargeMultiplier : 1f);
            charge = Mathf.Clamp01(charge + GetDeltaTime() * rate / Mathf.Max(0.05f, StormspearTuning.ChargeSeconds));
            if (comp) { comp.SetForm(form); comp.SetCharge(charge); }
            if (characterBody) characterBody.SetAimTimer(0.5f);

            if (isAuthority && !(inputBank && inputBank.skill2.down))
            {
                thrown = true;
                outer.SetNextState(new StormspearThrowState { charge = charge, form = form, activatorSkillSlot = activatorSkillSlot });
            }
        }

        public override void OnExit()
        {
            if (!thrown && comp)
            {
                // The authority knows it was not a throw. Other machines cannot tell an
                // interruption from the networked hand-off to the throw state (same frame),
                // so they cancel on a short delay that a Release cancels.
                if (isAuthority) comp.Cancel(); else comp.CancelDeferred();
            }
            if (!thrown && isAuthority && characterBody && characterBody.healthComponent && characterBody.healthComponent.alive)
            {
                var slot = activatorSkillSlot ? activatorSkillSlot : (skillLocator ? skillLocator.secondary : null);
                if (slot && StormspearCooldownPolicy.CanRefund(thrown, isAuthority, true, slot.stock, slot.maxStock))
                {
                    float progress = slot.rechargeStopwatch;
                    slot.AddOneStock();
                    slot.rechargeStopwatch = progress; // vanilla AddOneStock resets an existing spare-stock queue
                    KitLog.Event("STORMSPEAR_INTERRUPTED", "refunded before release");
                }
            }
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() { return InterruptPriority.Skill; }
    }
}
