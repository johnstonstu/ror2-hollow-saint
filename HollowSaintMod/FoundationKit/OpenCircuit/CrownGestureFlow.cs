using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.OpenCircuit
{
    /// <summary>Only the crown's own gestures may be replaced by its recovery.</summary>
    internal static class CrownGestureFlow
    {
        private static readonly string[] Layers = { KitAnim.UpperBodyLayer, KitAnim.UpperArmsLayer };
        private static readonly int Empty = Animator.StringToHash("Empty");
        private static readonly int Cast = Animator.StringToHash(OpenCircuitTuning.CastArmsState);
        private static readonly int Hold = Animator.StringToHash(OpenCircuitTuning.HoldArmsState);

        private static int RequestedState(Animator animator, int layer)
        {
            if (layer < 0) return Empty;
            int pending = KitAnim.PendingState(animator, layer);
            if (pending != 0) return pending;
            return animator.IsInTransition(layer) ? animator.GetNextAnimatorStateInfo(layer).shortNameHash :
                animator.GetCurrentAnimatorStateInfo(layer).shortNameHash;
        }

        private static bool Owned(int state) { return state == Cast || state == Hold; }

        internal static void Cancel(CharacterBody body)
        {
            var animator = KitAnim.AnimatorOf(body);
            if (!animator) return;
            foreach (string layer in Layers)
                if (Owned(RequestedState(animator, animator.GetLayerIndex(layer))))
                    KitAnim.Stop(body, layer);
        }

        internal static void Recover(CharacterBody body)
        {
            var animator = KitAnim.AnimatorOf(body);
            if (!body || !animator) return;
            // A lingering Open Circuit buff may expire during Gaze. Its close must
            // not lower Gaze's arms; GazeEndState owns that eventual handoff.
            var crown = EntityStateMachine.FindByCustomName(body.gameObject, KitRegistration.CrownMachineName);
            if (crown && crown.state is Gaze.GazeState) return;
            foreach (string layer in Layers)
            {
                int state = RequestedState(animator, animator.GetLayerIndex(layer));
                if (state != Empty && !Owned(state)) return;
            }
            // Death and an Arc Step cancel release the arms into the body pose,
            // without adding a crown-close gesture on top of the new movement.
            if (!body.healthComponent || !body.healthComponent.alive || ArcStep.ArcStepState.IsBodyDashing(body))
            {
                Cancel(body);
                return;
            }
            KitAnim.PlayGestureOnBody(body, OpenCircuitTuning.EndAnimState, OpenCircuitTuning.EndClipSeconds);
        }
    }
}