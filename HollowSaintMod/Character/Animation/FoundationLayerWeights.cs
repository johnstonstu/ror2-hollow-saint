using HollowSaint.FoundationKit;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// v0.8: drives the weight of the write-defaults-off gesture layers. An idle layer resting in
    /// "Empty" still holds the last pose it wrote (measured: 7.4 cm of stale arm pose after an
    /// Arc Bolt), and a gesture cross-faded in from that state starts from the stale pose. So an
    /// idle layer is faded to weight 0 (the body pose shows through), and a gesture on a weight-0
    /// layer starts instantly while the weight fades in. Runs before the Animator every frame.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class FoundationLayerWeights : MonoBehaviour
    {
        private static readonly string[] Layers = { KitAnim.UpperBodyLayer, KitAnim.OverlayLayer, KitAnim.UpperArmsLayer, KitAnim.SpearCarryLayer };
        private const float FadeIn = 0.1f, FadeOut = 0.2f;
        private static readonly int EmptyHash = Animator.StringToHash("Empty");
        private Animator animator;
        private int[] indices;

        public static bool Managed(string layer)
        {
            for (int i = 0; i < Layers.Length; i++) if (Layers[i] == layer) return true;
            return false;
        }

        private void Start()
        {
            animator = GetComponent<Animator>();
            if (!animator) { enabled = false; return; }
            indices = new int[Layers.Length];
            for (int i = 0; i < Layers.Length; i++)
            {
                indices[i] = animator.GetLayerIndex(Layers[i]);
                if (indices[i] >= 0) animator.SetLayerWeight(indices[i], 0f); // nothing playing at spawn
            }
        }

        private void Update()
        {
            if (!animator || !animator.isActiveAndEnabled) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < indices.Length; i++)
            {
                int layer = indices[i];
                if (layer < 0) continue;
                bool active = Active(layer);
                float weight = animator.GetLayerWeight(layer);
                float target = active ? 1f : 0f;
                if (dt <= 0f) continue;
                weight = Mathf.MoveTowards(weight, target, dt / (active ? FadeIn : FadeOut));
                animator.SetLayerWeight(layer, weight);
            }
        }

        private bool Active(int layer)
        {
            int requested = KitAnim.PendingState(animator, layer);
            if (requested != 0) return requested != EmptyHash;
            var current = animator.GetCurrentAnimatorStateInfo(layer);
            if (animator.IsInTransition(layer)) return animator.GetNextAnimatorStateInfo(layer).shortNameHash != EmptyHash;
            return current.shortNameHash != EmptyHash;
        }
    }
}
