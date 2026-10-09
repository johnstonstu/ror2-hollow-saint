using EntityStates;
using HollowSaint.FoundationKit.ChargedStorm;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.OpenCircuit
{
    /// <summary>Gather first, then preserve the authored unfold/activation marker sequence.</summary>
    public class OpenCircuitState : StoredChargeState
    {
        internal override byte Kind => 2;
        private float releasedAt;
        private int charges;
        private bool hasUnfolded, hasOpenedCircuit;

        protected override void OnReleased(int count)
        {
            charges = count; releasedAt = fixedAge;
            Vfx.BodyCurrentFx.PulseCore(characterBody, OpenCircuitTuning.CastClipSeconds);
            KitAnim.PlayGesture(characterBody, GetModelAnimator(), OpenCircuitTuning.CastArmsState, OpenCircuitTuning.CastClipSeconds);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!Released) return;
            float age = fixedAge - releasedAt;
            if (!hasUnfolded && age >= OpenCircuitTuning.CastClipSeconds * OpenCircuitTuning.UnfoldNormalizedTime)
            {
                hasUnfolded = true;
                OpenCircuitVfxHooks.RaiseUnfold(characterBody);
                Vfx.KitFx.Local(Vfx.Beat.CircuitUnfold, characterBody, Vfx.KitFx.Socket(characterBody, "Halo"));
            }
            if (!hasOpenedCircuit && age >= OpenCircuitTuning.CastClipSeconds * OpenCircuitTuning.CrownActiveNormalizedTime)
                Open();
        }

        private void Open()
        {
            hasOpenedCircuit = true;
            if (NetworkServer.active)
            {
                OpenCircuitBuff.Open(characterBody, charges);
                KitLog.Event("OPEN_CIRCUIT_CHARGED", "charges=" + charges + " interval=" +
                    CircuitChargePolicy.Interval(KitTuning.OpenCircuitPulseInterval, charges));
            }
            OpenCircuitVfxHooks.RaiseCrownActivated(characterBody);
        }

        public override void OnExit()
        {
            // Once committed, an interruption cannot turn paid fuel into a lost
            // crown window. Pre-commit cancellation preserves fuel and stock.
            if (Released && !hasOpenedCircuit && characterBody && characterBody.healthComponent && characterBody.healthComponent.alive) Open();
            if (!Released || !characterBody || !characterBody.healthComponent || !characterBody.healthComponent.alive)
                CrownGestureFlow.Cancel(characterBody);
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => hasOpenedCircuit ? InterruptPriority.Skill : InterruptPriority.PrioritySkill;
    }
}
