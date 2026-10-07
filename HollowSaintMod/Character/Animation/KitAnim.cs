using System;
using System.Collections.Generic;
using EntityStates;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit
{

    /// <summary>
    /// Animation helper that never warns. The game bundle currently ships a single-layer
    /// controller, so most kit states do not exist yet; EntityState.PlayAnimation on a
    /// missing layer logs "Invalid Layer Index -1" every shot. This checks first and
    /// logs each missing state once.
    /// </summary>
    public static class KitAnim
    {
        public const string BodyLayer = "Body";
        public const string UpperBodyLayer = "UpperBody";
        public const string OverlayLayer = "Overlay";
        public const string HaloLayer = "Halo";
        /// <summary>bundle06: arms-only gesture layer (no spine/chest) used while moving.</summary>
        public const string UpperArmsLayer = "UpperArms";
        public const string SpearCarryLayer = "SpearCarry";
        public const string PlaybackRateParam = "attackSpeed";

        private static readonly HashSet<string> reported = new HashSet<string>();

        public const string OverlayRateParam = "overlaySpeed";
        public const string HaloRateParam = "haloSpeed";

        // Per-Animator cache of float parameter name hashes, so the per-layer rate params can be
        // detected at runtime (old controller: only attackSpeed; new: overlaySpeed/haloSpeed).
        private static readonly Dictionary<Animator, HashSet<int>> paramCache = new Dictionary<Animator, HashSet<int>>();
        private static readonly int AttackSpeedHash = Animator.StringToHash(PlaybackRateParam);
        private static readonly int OverlaySpeedHash = Animator.StringToHash(OverlayRateParam);
        private static readonly int HaloSpeedHash = Animator.StringToHash(HaloRateParam);
        private const float PlayFade = 0.09f;
        private const float OtherLayerFade = 0.12f;

        private static HashSet<int> ParamsOf(Animator animator)
        {
            HashSet<int> set;
            if (paramCache.TryGetValue(animator, out set)) return set;
            // Drop entries for destroyed animators (Unity null) so the cache cannot grow forever.
            if (paramCache.Count > 16)
            {
                var dead = new List<Animator>();
                foreach (var kv in paramCache) if (kv.Key == null) dead.Add(kv.Key);
                for (int i = 0; i < dead.Count; i++) paramCache.Remove(dead[i]);
            }
            set = new HashSet<int>();
            var ps = animator.parameters;
            for (int i = 0; i < ps.Length; i++)
                if (ps[i].type == AnimatorControllerParameterType.Float) set.Add(ps[i].nameHash);
            paramCache[animator] = set;
            return set;
        }

        private static int RateParamFor(Animator animator, string layer)
        {
            int wanted = layer == SpearCarryLayer ? Animator.StringToHash("spearSpeed") : layer == OverlayLayer ? OverlaySpeedHash : layer == HaloLayer ? HaloSpeedHash : AttackSpeedHash;
            if (wanted != AttackSpeedHash && !ParamsOf(animator).Contains(wanted)) return AttackSpeedHash;
            return wanted;
        }

        private static readonly int EmptyHash = Animator.StringToHash("Empty");

        private static void FadeToEmpty(Animator animator, string layer, float fade)
        {
            int idx = animator.GetLayerIndex(layer);
            if (idx < 0 || !animator.HasState(idx, EmptyHash)) return;
            if (animator.GetCurrentAnimatorStateInfo(idx).shortNameHash == EmptyHash && !animator.IsInTransition(idx) &&
                PendingState(animator, idx) == 0) return;
            animator.CrossFadeInFixedTime(EmptyHash, fade, idx);
            MarkPending(animator, idx, EmptyHash);
        }

        /// <summary>True when no skill gesture is playing on the UpperBody or UpperArms layer.</summary>
        public static bool UpperBodyIdle(CharacterBody body)
        {
            if (body == null || body.modelLocator == null || body.modelLocator.modelTransform == null) return true;
            var animator = body.modelLocator.modelTransform.GetComponent<Animator>();
            if (animator == null) return true;
            return LayerIdle(animator, animator.GetLayerIndex(UpperBodyLayer)) &&
                LayerIdle(animator, animator.GetLayerIndex(UpperArmsLayer));
        }

        /// <summary>True when the layer (by index) is resting in "Empty" and not blending.
        /// A missing layer (index below 0) counts as idle.</summary>
        public static bool LayerIdle(Animator animator, int layerIndex)
        {
            if (animator == null || layerIndex < 0 || layerIndex >= animator.layerCount) return true;
            int requested = PendingState(animator, layerIndex);
            if (requested != 0) return requested == EmptyHash;
            return animator.GetCurrentAnimatorStateInfo(layerIndex).shortNameHash == EmptyHash && !animator.IsInTransition(layerIndex);
        }

        /// <summary>The arm layer a skill gesture should use right now: UpperArms while moving
        /// (ground speed above the walk threshold) or airborne, so the run torso and lean
        /// survive; UpperBody when standing or when the controller has no UpperArms layer.</summary>
        public static string GestureLayerFor(CharacterBody body, Animator animator)
        {
            if (animator == null || animator.GetLayerIndex(UpperArmsLayer) < 0) return UpperBodyLayer;
            if (body == null || body.characterMotor == null) return UpperBodyLayer;
            var motor = body.characterMotor;
            Vector3 v = motor.velocity;
            float planar = new Vector2(v.x, v.z).magnitude;
            return FoundationAnimRules.UseArmsLayer(planar, motor.isGrounded) ? UpperArmsLayer : UpperBodyLayer;
        }

        /// <summary>Plays a skill gesture on the arm layer chosen by GestureLayerFor and fades
        /// the other arm layer to Empty. Falls back to UpperBody when the state is missing on
        /// UpperArms (older controllers). Returns the layer used, or null if nothing played.</summary>
        public static string PlayGesture(CharacterBody body, Animator animator, string state, float duration)
        {
            if (animator == null) return null;
            string layer = GestureLayerFor(body, animator);
            if (layer == UpperArmsLayer && !animator.HasState(animator.GetLayerIndex(UpperArmsLayer), Animator.StringToHash(state)))
                layer = UpperBodyLayer;
            if (!Play(animator, layer, state, duration)) return null;
            FadeToEmpty(animator, layer == UpperArmsLayer ? UpperBodyLayer : UpperArmsLayer, OtherLayerFade);
            return layer;
        }

        /// <summary>True when the state exists on the named layer of this animator.</summary>
        public static bool HasState(Animator animator, string layer, string state)
        {
            if (animator == null) return false;
            int idx = animator.GetLayerIndex(layer);
            return idx >= 0 && animator.HasState(idx, Animator.StringToHash(state));
        }

        public static bool Play(Animator animator, string layer, string state, float duration)
        {
            if (animator == null) return false;
            int layerIndex = animator.GetLayerIndex(layer);
            int stateHash = Animator.StringToHash(state);
            if (layerIndex < 0 || !animator.HasState(layerIndex, stateHash))
            {
                if (reported.Add(layer + "/" + state))
                    Plugin.Log.LogInfo("HOLLOW_SAINT_ANIM_PENDING " + layer + "/" + state +
                        " (not in the current controller; skipped)");
                return false;
            }
            int rate = RateParamFor(animator, layer);
            // Skill gestures own the arms: the Overlay layer shares the upper-body mask and sits
            // above UpperBody, so a Discharge snap / flourish would hide every bolt and spear.
            if (layer == UpperBodyLayer || layer == UpperArmsLayer) FadeToEmpty(animator, OverlayLayer, 0.08f);
            // v0.8: never force an animator evaluation here. The vanilla helper calls
            // animator.Update(0) twice to read the clip length; doing that from LateUpdate (or
            // after the pose passes) re-evaluated the whole rig mid-frame and erased every
            // procedural accent for a frame. The length comes from the controller's clips instead,
            // and the request takes effect at the animator's normal evaluation.
            float length = ClipLength(animator, state);
            float speed = length > 0.001f ? length / Mathf.Max(0.01f, duration) : 1f;
            animator.SetFloat(rate, speed);
            // A weight-0 gesture layer holds a stale pose; start the clip outright and let
            // FoundationLayerWeights fade the layer in from the body pose instead.
            if (FoundationLayerWeights.Managed(layer) && animator.GetLayerWeight(layerIndex) < 0.05f)
                animator.PlayInFixedTime(stateHash, layerIndex, 0f);
            else animator.CrossFadeInFixedTime(stateHash, PlayFade, layerIndex, 0f);
            MarkPending(animator, layerIndex, stateHash);
            return true;
        }

        // ---- v0.8 request bookkeeping (no mid-frame evaluation) ----
        private static readonly Dictionary<RuntimeAnimatorController, Dictionary<string, float>> clipLengths =
            new Dictionary<RuntimeAnimatorController, Dictionary<string, float>>();
        private static readonly Dictionary<long, KeyValuePair<int, int>> pending = new Dictionary<long, KeyValuePair<int, int>>();

        /// <summary>Clip length for a state named after its clip ("Conduit Spear" = Conduit_Spear),
        /// read from the active (override) controller. 0 when unknown.</summary>
        public static float ClipLength(Animator animator, string state)
        {
            var controller = animator ? animator.runtimeAnimatorController : null;
            if (!controller) return 0f;
            Dictionary<string, float> map;
            if (!clipLengths.TryGetValue(controller, out map))
            {
                map = new Dictionary<string, float>();
                foreach (var clip in controller.animationClips)
                    if (clip) map[clip.name.Replace('_', ' ')] = clip.length;
                clipLengths[controller] = map;
            }
            float length;
            if (map.TryGetValue(state, out length)) return length;
            if (reported.Add("length/" + state))
                Plugin.Log.LogWarning("HOLLOW_SAINT_ANIM_LENGTH unknown clip for state '" + state + "'; playing at 1x");
            return 0f;
        }

        private static long PendingKey(Animator animator, int layer) => ((long)animator.GetInstanceID() << 8) | (uint)(layer & 0xff);

        private static void MarkPending(Animator animator, int layer, int stateHash)
        {
            pending[PendingKey(animator, layer)] = new KeyValuePair<int, int>(Time.frameCount, stateHash);
        }

        /// <summary>A request made this frame (or last, from LateUpdate) that the animator has not
        /// evaluated yet. Returns the requested state hash, or 0.</summary>
        public static int PendingState(Animator animator, int layer)
        {
            KeyValuePair<int, int> entry;
            if (!animator || !pending.TryGetValue(PendingKey(animator, layer), out entry)) return 0;
            if (Time.frameCount - entry.Key > 1) return 0;
            return entry.Value;
        }

        /// <summary>Returns a gesture layer to its "Empty" state with a cross-fade. Never use
        /// Play for this: EntityState.PlayAnimation sets the shared rate parameter to
        /// clipLength / duration, which is 0 for an empty state and freezes every gesture.</summary>
        public static void Stop(CharacterBody body, string layer, float fade = 0.2f)
        {
            if (body == null || body.modelLocator == null || body.modelLocator.modelTransform == null) return;
            var animator = body.modelLocator.modelTransform.GetComponent<Animator>();
            if (animator == null) return;
            int layerIndex = animator.GetLayerIndex(layer);
            int empty = Animator.StringToHash("Empty");
            if (layerIndex < 0 || !animator.HasState(layerIndex, empty)) return;
            animator.CrossFadeInFixedTime(empty, fade, layerIndex);
            MarkPending(animator, layerIndex, empty);
        }

        public static bool PlayOnBody(CharacterBody body, string layer, string state, float duration)
        {
            if (body == null || body.modelLocator == null || body.modelLocator.modelTransform == null) return false;
            return Play(body.modelLocator.modelTransform.GetComponent<Animator>(), layer, state, duration);
        }

        public static string PlayGestureOnBody(CharacterBody body, string state, float duration)
        {
            if (body == null || body.modelLocator == null || body.modelLocator.modelTransform == null) return null;
            return PlayGesture(body, body.modelLocator.modelTransform.GetComponent<Animator>(), state, duration);
        }

        /// <summary>Plays a skill gesture (arm layer by movement, like PlayGesture) whose exit hands
        /// off to a "recover" tail. bundle06 bakes the gesture -> recover chain into the controller
        /// (recover runs at 1x after the gesture), so this only checks the tail exists and logs once
        /// when it does not; without it the gesture returns to Empty as before.</summary>
        public static string PlayGestureWithRecover(CharacterBody body, Animator animator, string state, string recoverState, float duration)
        {
            string layer = PlayGesture(body, animator, state, duration);
            if (layer != null && !string.IsNullOrEmpty(recoverState) && !HasState(animator, layer, recoverState) &&
                reported.Add(layer + "/" + recoverState))
                Plugin.Log.LogInfo("HOLLOW_SAINT_ANIM_PENDING " + layer + "/" + recoverState +
                    " (no recover tail in the current controller; gesture returns to Empty)");
            return layer;
        }

        /// <summary>Moves a running gesture from one arm layer to the other at the same normalized
        /// time (used when movement changes mid-gesture). No-op when either layer is missing, the
        /// source is resting or blending, or the target lacks the state.</summary>
        public static bool MoveGesture(Animator animator, int fromLayer, int toLayer, float fade)
        {
            if (animator == null || fromLayer < 0 || toLayer < 0 || fromLayer == toLayer) return false;
            if (fromLayer >= animator.layerCount || toLayer >= animator.layerCount) return false;
            // FixedUpdate may have queued a fresh cast before presentation migrates
            // the previous pose. Wait for that request to evaluate instead of moving
            // stale state over the new cast (or its pending fade to Empty).
            if (PendingState(animator, fromLayer) != 0 || PendingState(animator, toLayer) != 0) return false;
            if (animator.IsInTransition(fromLayer) || animator.IsInTransition(toLayer)) return false;
            var info = animator.GetCurrentAnimatorStateInfo(fromLayer);
            if (info.shortNameHash == EmptyHash || !animator.HasState(toLayer, info.shortNameHash)) return false;
            if (animator.GetCurrentAnimatorStateInfo(toLayer).shortNameHash != EmptyHash) return false;
            float at = info.loop ? Mathf.Repeat(info.normalizedTime, 1f) : Mathf.Min(info.normalizedTime, 0.99f);
            // Same clip, same time on the other arm layer: start it outright (its weight fades in)
            // while the source layer fades out; no blend from a stale retained pose.
            if (animator.GetLayerWeight(toLayer) < 0.05f) animator.Play(info.shortNameHash, toLayer, at);
            else animator.CrossFade(info.shortNameHash, fade, toLayer, at);
            animator.CrossFadeInFixedTime(EmptyHash, fade, fromLayer);
            MarkPending(animator, toLayer, info.shortNameHash);
            MarkPending(animator, fromLayer, EmptyHash);
            return true;
        }

        public static Animator AnimatorOf(CharacterBody body)
        {
            if (body == null || body.modelLocator == null || body.modelLocator.modelTransform == null) return null;
            return body.modelLocator.modelTransform.GetComponent<Animator>();
        }
    }
}
