using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.OpenCircuit
{
    /// <summary>
    /// Open Circuit cast: the 1.21 s unfold gesture. At the "Crown active" marker the
    /// server copy of this state applies the 8 s buff; OpenCircuitPulseDriver does the
    /// pulsing off that buff, so the character is free to act for the whole window.
    /// </summary>
    public class OpenCircuitState : BaseSkillState
    {
        private float duration;
        private bool hasUnfolded;
        private bool hasOpenedCircuit;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = OpenCircuitTuning.CastClipSeconds;
            Vfx.BodyCurrentFx.PulseCore(characterBody, duration);
            var animator = GetModelAnimator();
            // v0.8: the Halo layer unfolds from OpenCircuitPulseDriver's crownOpen bool, which
            // is true while this state runs; nothing plays Halo states directly.
            // bundle06: the arms raise on the gesture layer and chain into "Open Circuit arms hold"
            // (controller transition) for the buff window. Older controllers log once and skip.
            KitAnim.PlayGesture(characterBody, animator, OpenCircuitTuning.CastArmsState, duration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!hasUnfolded && fixedAge >= duration * OpenCircuitTuning.UnfoldNormalizedTime)
            {
                hasUnfolded = true;
                OpenCircuitVfxHooks.RaiseUnfold(characterBody);
                Vfx.KitFx.Local(Vfx.Beat.CircuitUnfold, characterBody, Vfx.KitFx.Socket(characterBody, "Halo"));
            }

            if (!hasOpenedCircuit && fixedAge >= duration * OpenCircuitTuning.CrownActiveNormalizedTime)
            {
                hasOpenedCircuit = true;
                // The server runs its own copy of this state (for a remote player too),
                // so this is the one place the buff is applied.
                if (NetworkServer.active && characterBody && OpenCircuitBuff.Def != null &&
                    characterBody.healthComponent && characterBody.healthComponent.alive)
                {
                    characterBody.AddTimedBuff(OpenCircuitBuff.Def, KitTuning.OpenCircuitBuffSeconds);
                    Plugin.Log.LogInfo("HOLLOW_SAINT_OPEN_CIRCUIT_OPENED seconds=" + KitTuning.OpenCircuitBuffSeconds);
                }
                OpenCircuitVfxHooks.RaiseCrownActivated(characterBody);
            }

            if (isAuthority && fixedAge >= duration)
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            // Interrupted after the unfold but before the crown lit (e.g. by another
            // PrioritySkill): open it anyway so a spent cooldown always yields the window.
            if (!hasOpenedCircuit && hasUnfolded && NetworkServer.active && characterBody &&
                OpenCircuitBuff.Def != null && characterBody.healthComponent && characterBody.healthComponent.alive)
            {
                hasOpenedCircuit = true;
                characterBody.AddTimedBuff(OpenCircuitBuff.Def, KitTuning.OpenCircuitBuffSeconds);
            }
            // A pre-unfold interruption has no buff to drive a later close. Do not
            // leave the authored cast -> arms-hold chain running indefinitely.
            if (!hasUnfolded || !characterBody || !characterBody.healthComponent || !characterBody.healthComponent.alive)
                CrownGestureFlow.Cancel(characterBody);
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return hasOpenedCircuit ? InterruptPriority.Skill : InterruptPriority.PrioritySkill;
        }
    }
}
