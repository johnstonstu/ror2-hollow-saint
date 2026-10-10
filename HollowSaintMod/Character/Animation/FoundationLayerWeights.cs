using HollowSaint.FoundationKit;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// v0.8: drives the weight of the write-defaults-off gesture layers. An idle layer resting in
    /// "Empty" still holds the last pose it wrote (measured: 7.4 cm of stale arm pose after an
    /// Arc Bolt), and a gesture cross-faded in from that state starts from the stale pose. So an
    /// idle layer is faded to weight 0 (the body pose shows through), and a gesture on a weight-0
    /// layer starts instantly while the weight fades in. Runs before the Animator every frame.
    ///
    /// 1.3.2: also owns "Arc Bolt over a held crown pose" (KitAnim.PlayBoltGesture). The bolt
    /// replaces the two-arm hold on the same arm layer, and near the end of the throw this
    /// cross-fades back into the hold at the time it would have reached, so the arms return to
    /// the crown instead of dropping to Empty and re-raising. FoundationArmPose keeps the
    /// non-throwing arm on the hold pose meanwhile (PinTarget).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class FoundationLayerWeights : MonoBehaviour
    {
        private static readonly string[] Layers = { KitAnim.UpperBodyLayer, KitAnim.OverlayLayer, KitAnim.UpperArmsLayer, KitAnim.SpearCarryLayer };
        private const float FadeIn = 0.1f, FadeOut = 0.2f;
        private static readonly int EmptyHash = Animator.StringToHash("Empty");
        private static readonly int RateHash = Animator.StringToHash(KitAnim.PlaybackRateParam);
        private Animator animator;
        private int[] indices;

        public static bool Managed(string layer)
        {
            for (int i = 0; i < Layers.Length; i++) if (Layers[i] == layer) return true;
            return false;
        }

        /// <summary>The body this model presents. ModelLocator can detach the model from the body
        /// object, so the body is handed in (FoundationPresentation, PlayBoltGesture) rather than
        /// looked up through the parent chain.</summary>
        internal void Bind(CharacterBody owner)
        {
            if (!owner || owner == body) return;
            body = owner;
            crown = null; crownResolved = false; ownerFx = null;
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
            upperIdx = animator.GetLayerIndex(KitAnim.UpperBodyLayer);
            armsIdx = animator.GetLayerIndex(KitAnim.UpperArmsLayer);
            if (!body) body = GetComponentInParent<CharacterBody>();
        }

        private void Update()
        {
            if (!animator || !animator.isActiveAndEnabled) return;
            ApplyDeferredRate();
            TickBoltOverHold();
            float dt = Time.deltaTime;
            for (int i = 0; i < indices.Length; i++)
            {
                int layer = indices[i];
                if (layer < 0) continue;
                bool active = Active(layer) || BoltStreamGrace(layer);
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

        // ---------------- 1.3.2: continuous Arc Bolt fire ----------------

        /// <summary>Seconds past a bolt's end during which its layer keeps full weight while Primary
        /// is still held. The controller hands a bolt to Empty at 0.85, which started the 0.2 s fade
        /// before the next bolt of a stream began (the arms dipped about 37% toward the body pose
        /// between every pair of bolts).</summary>
        private const float BoltStreamGraceSeconds = 0.12f;
        private int streamLayer = -1;
        private float streamUntil;

        /// <summary>Called by KitAnim.PlayBoltGesture for every bolt gesture it plays.</summary>
        internal void NoteBolt(CharacterBody owner, string layerName, float duration)
        {
            Bind(owner);
            if (!animator || string.IsNullOrEmpty(layerName)) return;
            streamLayer = animator.GetLayerIndex(layerName);
            streamUntil = Time.time + Mathf.Max(0.05f, duration) + BoltStreamGraceSeconds;
        }

        private bool BoltStreamGrace(int layer)
        {
            if (layer != streamLayer) return false;
            if (Time.time > streamUntil) { streamLayer = -1; return false; }
            // Only while the stream can continue; a released trigger fades out as before. The layer
            // must still be resting after the bolt (Empty), not taken by another gesture's request.
            var bank = body ? body.inputBank : null;
            if (!bank || !bank.skill1.down) return false;
            int requested = Requested(layer);
            return requested == EmptyHash || IsBolt(requested);
        }

        // ---------------- 1.3.2: Arc Bolt over a held crown pose ----------------

        private static readonly int CastArmsHash = Animator.StringToHash(FoundationKit.OpenCircuit.OpenCircuitTuning.CastArmsState);
        private static readonly int HoldArmsHash = Animator.StringToHash(FoundationKit.OpenCircuit.OpenCircuitTuning.HoldArmsState);
        private static readonly int BoltRightHash = Animator.StringToHash(FoundationKit.ArcBolt.ArcBoltState.StateRight);
        private static readonly int BoltLeftHash = Animator.StringToHash(FoundationKit.ArcBolt.ArcBoltState.StateLeft);
        /// <summary>Bolt normalized time at which the arms head back to the hold. The controller's
        /// bolt exit to Empty starts at 0.85, so this must stay below that.</summary>
        private const float ResumeAt = 0.7f;
        /// <summary>Seconds for the throwing arm to return to the crown pose.</summary>
        private const float ResumeFade = 0.15f;
        /// <summary>The rise chains into the hold loop at 0.92 (controller exit); resume just short of it.</summary>
        private const float CastResumeCap = 0.9f;

        private int upperIdx = -1, armsIdx = -1;
        // Memo: a bolt is standing in for the hold on memoLayer.
        private int memoLayer = -1;      // arm layer the hold (and now the bolt) plays on; -1 = inactive
        private int memoHold;            // hold state hash to return to
        private int memoArm;             // throwing hand of the latest bolt: -1 left, +1 right
        private float memoHoldTime;      // hold normalized time when the first bolt cut it
        private float memoHoldPeriod;    // real seconds per normalized unit of the hold
        private float memoHoldSpeed;     // rate parameter value the hold was playing at
        private float memoStartedAt;
        // Resume: resumeLayer is cross-fading from the bolt back into the hold.
        private int resumeLayer = -1, resumeHold, resumeArm;
        private float resumeHoldTime;
        // The bolt and the rise share the attackSpeed parameter. Setting the rise's slow rate at the
        // resume slowed the bolt tail (and its fade) to the Gaze rise rate, so it is applied once
        // the bolt has faded out.
        private int rateLayer = -1;
        private float rateValue, rateAt;
        private CharacterBody body;
        private EntityStateMachine crown;
        private bool crownResolved;
        private FoundationKit.ChargedStorm.StoredChargeChargeFx ownerFx;

        private static bool IsCrownHold(int hash) { return hash == CastArmsHash || hash == HoldArmsHash; }
        private static bool IsBolt(int hash) { return hash == BoltRightHash || hash == BoltLeftHash; }
        private static string HoldName(int hash)
        {
            return hash == HoldArmsHash ? FoundationKit.OpenCircuit.OpenCircuitTuning.HoldArmsState : FoundationKit.OpenCircuit.OpenCircuitTuning.CastArmsState;
        }

        /// <summary>The state a layer is heading to: a request not yet evaluated, the target of a
        /// running transition, or the current state.</summary>
        private int Requested(int layer)
        {
            if (layer < 0) return EmptyHash;
            int pending = KitAnim.PendingState(animator, layer);
            if (pending != 0) return pending;
            return animator.IsInTransition(layer) ? animator.GetNextAnimatorStateInfo(layer).shortNameHash :
                animator.GetCurrentAnimatorStateInfo(layer).shortNameHash;
        }

        /// <summary>The crown charge that raised the arms is still running: Gaze of the Hollow
        /// (charge or beam phase) on the Crown machine, or a Thundercloud / Open Circuit stored
        /// charge gather (its charge-up visual lives until release). Once the owner is gone the
        /// bolt simply finishes to Empty. The gather visual is looked up only when a bolt starts
        /// (search), then held by reference.</summary>
        private bool HoldOwnerAlive(bool search)
        {
            if (!body) return false;
            if (!crownResolved)
            {
                crownResolved = true;
                crown = EntityStateMachine.FindByCustomName(body.gameObject, KitRegistration.CrownMachineName);
            }
            if (crown && crown.state is FoundationKit.Gaze.GazeState) return true;
            if (ownerFx != null) return true;
            if (!search) return false;
            ownerFx = body.GetComponentInChildren<FoundationKit.ChargedStorm.StoredChargeChargeFx>();
            return ownerFx != null;
        }

        /// <summary>Which arm layer the bolt should cut: the one holding the crown pose, or the one
        /// an earlier bolt of this memo is still playing on. Sets hold to the crown state hash
        /// when the cut is fresh.</summary>
        private int HoldLayer(int layer, ref int hold)
        {
            if (layer < 0) return -1;
            int requested = Requested(layer);
            if (IsCrownHold(requested)) { hold = requested; return layer; }
            if (memoLayer >= 0 && IsBolt(requested)) return layer;
            return -1;
        }

        /// <summary>Normalized time of the crown hold on a layer that is heading to it.</summary>
        private float HoldTimeNow(int layer)
        {
            if (KitAnim.PendingState(animator, layer) != 0) return layer == resumeLayer ? resumeHoldTime : 0f;
            var info = animator.IsInTransition(layer) ? animator.GetNextAnimatorStateInfo(layer) : animator.GetCurrentAnimatorStateInfo(layer);
            return info.normalizedTime;
        }

        /// <summary>Called by KitAnim.PlayBoltGesture. Plays the bolt on the arm layer that holds the
        /// crown pose (or on which an earlier bolt already replaced it) and remembers the hold.
        /// False when there is no such hold; the caller then plays a normal gesture.</summary>
        internal bool TryPlayBoltOverHold(CharacterBody owner, string state, float duration, out string layerName)
        {
            layerName = null;
            Bind(owner);
            if (!animator || !isActiveAndEnabled || upperIdx < 0) return false;
            int hold = 0;
            int layer = HoldLayer(armsIdx, ref hold);
            if (layer < 0) layer = HoldLayer(upperIdx, ref hold);
            if (layer < 0 || !HoldOwnerAlive(true)) return false;
            string name = layer == armsIdx ? KitAnim.UpperArmsLayer : KitAnim.UpperBodyLayer;
            if (!KitAnim.HasState(animator, name, state)) return false;
            if (hold != 0)
            {
                // Fresh cut into the hold: remember where it was and how fast it ran. A rise that was
                // just resumed may not have been handed its own rate back yet.
                memoHold = hold;
                memoHoldTime = HoldTimeNow(layer);
                memoHoldSpeed = rateLayer == layer ? rateValue : animator.GetFloat(RateHash);
                float length = KitAnim.ClipLength(animator, HoldName(hold));
                // "Open Circuit arms hold" runs at a fixed 1x; the rise follows the rate parameter.
                float speed = hold == HoldArmsHash ? 1f : Mathf.Max(0.05f, memoHoldSpeed);
                memoHoldPeriod = length > 0.01f ? length / speed : 1f;
                memoStartedAt = Time.time;
            }
            rateLayer = -1;   // the bolt owns the shared rate parameter now
            resumeLayer = -1;
            if (!KitAnim.Play(animator, name, state, duration)) return false;
            memoLayer = layer;
            memoArm = Animator.StringToHash(state) == BoltLeftHash ? -1 : 1;
            layerName = name;
            return true;
        }

        private void TickBoltOverHold()
        {
            if (memoLayer < 0) return;
            int requested = Requested(memoLayer);
            if (!IsBolt(requested))
            {
                // FoundationPresentation may have moved the gesture to the other arm layer.
                int other = memoLayer == upperIdx ? armsIdx : upperIdx;
                if (requested == EmptyHash && other >= 0 && IsBolt(Requested(other))) { memoLayer = other; requested = Requested(other); }
                else if (requested != EmptyHash) { memoLayer = -1; return; } // another gesture, or the crown's own end, took the layer
            }
            if (!HoldOwnerAlive(false)) { memoLayer = -1; return; }
            int pending = KitAnim.PendingState(animator, memoLayer);
            if (pending == EmptyHash) { memoLayer = -1; return; } // stopped on purpose (crown cancel, death, dash)
            if (pending != 0) return;                                // the bolt request is not evaluated yet
            if (IsBolt(requested))
            {
                if (animator.IsInTransition(memoLayer)) return;      // still blending in
                if (animator.GetCurrentAnimatorStateInfo(memoLayer).normalizedTime < ResumeAt) return;
            }
            // Past the resume point, or the bolt already left for Empty (high attack speed or a long
            // frame skipped the window, which used to leave the arms down for the rest of the charge).
            Resume();
        }

        private void Resume()
        {
            float at = memoHoldTime + (Time.time - memoStartedAt) / memoHoldPeriod;
            at = memoHold == HoldArmsHash ? Mathf.Repeat(at, 1f) : Mathf.Min(at, CastResumeCap);
            float clip = KitAnim.ClipLength(animator, HoldName(memoHold));
            float target = at;
            if (memoHold == CastArmsHash && clip > 0.01f)
            {
                // The rise runs at the bolt's rate until the fade ends; start it that much earlier.
                float boltRate = animator.GetFloat(RateHash);
                target = Mathf.Max(0f, at - Mathf.Max(0f, boltRate - memoHoldSpeed) * ResumeFade / clip);
                rateLayer = memoLayer; rateValue = memoHoldSpeed; rateAt = Time.time + ResumeFade;
            }
            KitAnim.ResumeHold(animator, memoLayer, memoHold, target, clip, ResumeFade);
            resumeLayer = memoLayer; resumeHold = memoHold; resumeArm = memoArm; resumeHoldTime = target;
            DebugLastResume = "at=" + at.ToString("0.000") + " target=" + target.ToString("0.000") + " clip=" + clip.ToString("0.000") +
                " rate=" + memoHoldSpeed.ToString("0.000") + " boltRate=" + animator.GetFloat(RateHash).ToString("0.000");
            memoLayer = -1;
        }

        private void ApplyDeferredRate()
        {
            if (rateLayer < 0) return;
            if (Requested(rateLayer) != CastArmsHash) { rateLayer = -1; return; }
            bool blending = KitAnim.PendingState(animator, rateLayer) != 0 || animator.IsInTransition(rateLayer);
            if (blending && Time.time < rateAt) return;
            animator.SetFloat(RateHash, rateValue);
            rateLayer = -1;
        }

        /// <summary>A bolt is currently standing in for a crown hold on this arm layer.
        /// A queued replacement has already taken ownership, even before Update clears the memo.</summary>
        internal bool BoltOverHoldActive(string layer)
        {
            if (!animator || memoLayer < 0) return false;
            int index = animator.GetLayerIndex(layer);
            if (index < 0 || !IsBolt(Requested(index))) return false;
            if (index == memoLayer) return true;
            // Presentation can migrate the bolt before this component gets its next Update.
            return Requested(memoLayer) == EmptyHash && (index == upperIdx || index == armsIdx);
        }

        /// <summary>For FoundationArmPose, after the Animator: the throwing hand (-1 left, +1 right)
        /// while a bolt stands in for a crown hold or the arms are blending back into it, else 0.
        /// holdHash / holdTime give the hold state and the normalized time it is at (or would be
        /// at, had the bolt not replaced it), so the other arm can be held on that pose.</summary>
        internal int PinTarget(out int holdHash, out float holdTime)
        {
            holdHash = 0; holdTime = 0f;
            if (!animator) return 0;
            if (memoLayer >= 0)
            {
                int hash = Requested(memoLayer);
                if (hash == EmptyHash)
                {
                    int other = memoLayer == upperIdx ? armsIdx : upperIdx;
                    if (other >= 0 && IsBolt(Requested(other))) { memoLayer = other; hash = Requested(other); }
                }
                // A replacement/cancel can arrive after our Update, before the pose pass.
                // It owns both arms now; do not pin one back to the interrupted crown pose.
                bool finishedNaturally = hash == EmptyHash && KitAnim.PendingState(animator, memoLayer) == 0;
                if (!IsBolt(hash) && !finishedNaturally) { memoLayer = -1; return 0; }
                holdHash = memoHold;
                float v = memoHoldTime + (Time.time - memoStartedAt) / memoHoldPeriod;
                holdTime = memoHold == HoldArmsHash ? Mathf.Repeat(v, 1f) : Mathf.Min(v, CastResumeCap);
                return hash == BoltLeftHash ? -1 : hash == BoltRightHash ? 1 : memoArm;
            }
            if (resumeLayer < 0) return 0;
            int requested = Requested(resumeLayer);
            int pending = KitAnim.PendingState(animator, resumeLayer);
            if (!IsCrownHold(requested) || (pending == 0 && !animator.IsInTransition(resumeLayer))) { resumeLayer = -1; return 0; }
            holdHash = requested;
            holdTime = pending != 0 ? resumeHoldTime : animator.GetNextAnimatorStateInfo(resumeLayer).normalizedTime;
            if (holdHash == HoldArmsHash) holdTime = Mathf.Repeat(holdTime, 1f);
            return resumeArm;
        }

        /// <summary>For FoundationArmPose: a crown hold is playing on its own (no bolt, no blend),
        /// so the arm pose is worth remembering (fallback when the hold clip cannot be sampled).</summary>
        internal bool HoldSteady(out int holdHash, out float holdTime)
        {
            holdHash = 0; holdTime = 0f;
            if (memoLayer >= 0 || !animator) return false;
            int layer = Steady(upperIdx) ? upperIdx : Steady(armsIdx) ? armsIdx : -1;
            if (layer < 0) return false;
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            holdHash = info.shortNameHash;
            holdTime = holdHash == HoldArmsHash ? Mathf.Repeat(info.normalizedTime, 1f) : info.normalizedTime;
            return true;
        }

        /// <summary>A crown hold is on (or heading to) an arm layer, or a bolt stands in for one.</summary>
        internal bool HoldPresent()
        {
            if (!animator) return false;
            return memoLayer >= 0 || resumeLayer >= 0 || IsCrownHold(Requested(upperIdx)) || IsCrownHold(Requested(armsIdx));
        }

        private bool Steady(int layer)
        {
            if (layer < 0 || KitAnim.PendingState(animator, layer) != 0 || animator.IsInTransition(layer)) return false;
            return IsCrownHold(animator.GetCurrentAnimatorStateInfo(layer).shortNameHash) && animator.GetLayerWeight(layer) > 0.95f;
        }

        // ---- dev diagnostics (DevAutopilot) ----
        internal string DebugLastResume = "";

        internal string DebugState()
        {
            if (!animator) return "no-animator";
            return "memo=" + (memoLayer >= 0 ? memoLayer + "/" + StateName(memoHold) + " t0=" + memoHoldTime.ToString("0.000") +
                    " period=" + memoHoldPeriod.ToString("0.00") + " arm=" + memoArm : "-") +
                " resume=" + (resumeLayer >= 0 ? resumeLayer + "/" + StateName(resumeHold) + " arm=" + resumeArm : "-") +
                " pendingRate=" + (rateLayer >= 0 ? rateValue.ToString("0.000") : "-") +
                " rate=" + animator.GetFloat(RateHash).ToString("0.000") +
                " owner=" + HoldOwnerAlive(false) + " body=" + (body != null) +
                " parentBody=" + (GetComponentInParent<CharacterBody>() != null) +
                " | up " + LayerDesc(upperIdx) + " | arms " + LayerDesc(armsIdx);
        }

        private string LayerDesc(int layer)
        {
            if (layer < 0) return "none";
            var cur = animator.GetCurrentAnimatorStateInfo(layer);
            string s = StateName(cur.shortNameHash) + "@" + cur.normalizedTime.ToString("0.000") + " len=" + cur.length.ToString("0.000") +
                " spd=" + cur.speed.ToString("0.00") + "x" + cur.speedMultiplier.ToString("0.00");
            if (animator.IsInTransition(layer))
            {
                var next = animator.GetNextAnimatorStateInfo(layer);
                s += " -> " + StateName(next.shortNameHash) + "@" + next.normalizedTime.ToString("0.000") +
                    " tr=" + animator.GetAnimatorTransitionInfo(layer).normalizedTime.ToString("0.00");
            }
            int pending = KitAnim.PendingState(animator, layer);
            if (pending != 0) s += " pend=" + StateName(pending);
            return s + " w=" + animator.GetLayerWeight(layer).ToString("0.00");
        }

        private static string StateName(int hash)
        {
            if (hash == EmptyHash) return "Empty";
            if (hash == CastArmsHash) return "Cast";
            if (hash == HoldArmsHash) return "Hold";
            if (hash == BoltLeftHash) return "BoltL";
            if (hash == BoltRightHash) return "BoltR";
            return hash.ToString();
        }

        private void OnDisable() { memoLayer = resumeLayer = rateLayer = streamLayer = -1; }
    }
}
