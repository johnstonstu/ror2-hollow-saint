using System;
using System.Collections.Generic;
using System.Text;
using HollowSaint.FoundationKit.ArcBolt;
using HollowSaint.FoundationKit.ArcStep;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// Body-layer presentation. A small state machine that follows CharacterMotor and the
    /// kit's skill states and cross-fades between body clips with proper in-betweens
    /// (run start/stop, jump, land, glide enter/exit, directional Arc Step start/loop/end).
    /// Never reads keys, moves the body or changes input.
    ///
    /// Works with both bundles:
    ///  * bundle02 (layered): locomotion is the "Locomotion" 8-direction blend tree driven
    ///    by rightSpeed/forwardSpeed, loops scale with "moveRate", and skill gestures play
    ///    on their own layers, so this class leaves the arms alone.
    ///  * bundle01 (single layer): falls back to "Run forward", global animator speed, the
    ///    left-dash clips for every dash and the full-body bolt cast while planted.
    /// </summary>
    public sealed class FoundationPresentation : MonoBehaviour
    {
        private const float MoveThreshold = 0.6f;
        private const float StopThreshold = 0.35f;
        private const float RunClipSpeed = 6f;      // Run forward covers 6 m/s at 1x
        private const float SpeedSmoothing = 12f;
        private const float AuthoredFrameRate = 24f;

        // IdleOneShot: a standing body clip that returns to Idle (combat ready/relax, fidgets).
        private enum Phase { Idle, RunStart, Moving, Pivot, RunStop, Jump, Ascend, Descend, Land, GlideEnter, GlideLoop, GlideExit, DashStart, DashLoop, DashEnd, Cast, Spawn, SelectIntro, SelectIdle, IdleOneShot }

        // bundle06 polish fades (seconds).
        private const float CombatFlipFade = 0.35f;     // Idle <-> Idle combat, ready/relax in and out
        private const float FidgetFade = 0.3f;
        private const float MoveToIdleCombatFade = 0.3f;
        // Walk (or a slow run below the Run stop speed) -> relaxed Idle used the generic 0.18 s
        // Idle fade, well under the 12-frame (0.5 s) Walk -> Idle seam tested in
        // art/anim/wip/transitions/TRANSITIONS.md, so the arms snapped from the swing to the
        // sides. 0.3 s matches the combat-idle path and keeps stops responsive.
        private const float MoveToIdleFade = 0.3f;
        private const float GlideToJumpFade = 0.12f;
        private const float RunToGlideFade = 0.2f;
        private const float ArmsSwitchDelay = 0.2f;     // movement must hold this long before a gesture changes arm layer
        private const float ArmsSwitchFade = 0.15f;
        private static readonly string[] FidgetClips = { "Idle fidget 1", "Idle fidget 2", "Idle fidget 3" };
        private const string CombatFidgetClip = "Idle combat fidget";

        private bool hasRunStop, hasCombatReady, hasCombatRelax, hasCombatFidget;
        private readonly List<string> fidgets = new List<string>(3);
        private readonly FoundationIdleFidgetTimer fidgetTimer = new FoundationIdleFidgetTimer((min, max) => UnityEngine.Random.Range(min, max));
        private int lastFidget = -1;
        private bool fidgetDue;
        private string oneShotState = "Idle";
        private bool oneShotIsFidget;
        private bool combatKnown, lastInCombat, combatFlipped;
        private bool skillInput;
        private float recentRunSpeed;
        private float lastMoveForward = 1f;
        private int upperIdx = -1, armsIdx = -1, overlayIdx = -1;
        private float armsSwitchTimer;

        private Animator animator;
        private CharacterBody body;
        private EntityStateMachine weapon;
        private bool layered;
        private bool hasLocomotion;
        private bool normalizedLocomotion;
        private FoundationLocomotionMath.Sample locomotion;
        private bool sprintGliding;
        private Vector3 smoothedWorldVelocity;
        private bool warnedInvalidVelocity;
        private float retainedLocomotionPhase;
        private readonly FoundationFootContactClock footContacts = new FoundationFootContactClock();
        private readonly List<AnimatorClipInfo> movingClips = new List<AnimatorClipInfo>(16);
        private readonly Dictionary<AnimationClip, Vector3> authoredStrides = new Dictionary<AnimationClip, Vector3>();
        private float measuredReferenceSpeed, measuredGait;
        private Vector2 measuredDirection;
        public float GlideWeight { get; private set; }
        public bool GroundedForPresentation { get; private set; }
        public bool IsPresentingAlive => body && body.characterMotor && body.healthComponent && body.healthComponent.alive;
        private Phase phase = Phase.Idle;
        private string currentState;
        private float stateTime;
        private bool wasGrounded = true;
        private bool wasMoving;
        private bool wasDashing;
        private string dashDir = "left";
        private string dashStartState = "Arc Step left start";
        private string dashLoopState = "Arc Step left loop";
        private string dashEndState = "Arc Step left end";
        private float dashTime;
        private bool dashNeedsRelatch;
        private float airTime;
        private float landAirTime;
        private int lastJumpCount;
        private bool airJumped;
        private bool inputMoving;
        private Vector2 lastWorldVel;
        private bool latchedStopR;
        private bool latchedPivotLeft;
        private GameObject glideLoopObject;
        // Cached state-existence lookups (layer 0), filled once in Start.
        private bool hasIdleCombat, hasRunStopR, hasPivot, hasSelectIntro, hasSelectIdle;
        private Vector2 smoothed;
        private Vector3 lastHeading = Vector3.forward;
        private float headingTimer;
        private bool pivotLeft;
        private bool pivotRequested;
        private readonly Dictionary<string, float> lengths = new Dictionary<string, float>();
        private readonly HashSet<string> reportedMissing = new HashSet<string>();
        private static readonly int ForwardHash = Animator.StringToHash("forwardSpeed");
        private static readonly int RightHash = Animator.StringToHash("rightSpeed");
        private static readonly int MoveRateHash = Animator.StringToHash("moveRate");
        private static readonly int GaitHash = Animator.StringToHash("gaitBlend");
        private static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
        private const float RunFootstepSpeed = 4.5f;

        private void Start()
        {
            animator = GetComponent<Animator>();
            body = GetComponentInParent<CharacterBody>();
            if (!animator) throw new InvalidOperationException("Hollow Saint model lacks Animator");
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (body) weapon = EntityStateMachine.FindByCustomName(body.gameObject, "Weapon");
            layered = animator.layerCount > 1;
            hasLocomotion = Has("Locomotion");
            foreach (var parameter in animator.parameters)
                if (parameter.nameHash == GaitHash) normalizedLocomotion = true;
            if (body && !GetComponent<FoundationMotionPose>()) gameObject.AddComponent<FoundationMotionPose>();
            if (body && !GetComponent<FoundationArmPose>()) gameObject.AddComponent<FoundationArmPose>();
            if (body && !GetComponent<FoundationAimPose>()) gameObject.AddComponent<FoundationAimPose>();
            if (layered && !GetComponent<FoundationLayerWeights>()) gameObject.AddComponent<FoundationLayerWeights>();
            CacheStateFlags();
            SetDashDir("left");
            CacheLengths();
            DumpControllerContract();
            if (!body) Go(hasSelectIntro ? Phase.SelectIntro : Phase.Idle);
            else Go(Has("Spawn") ? Phase.Spawn : Phase.Idle);
        }

        private void CacheStateFlags()
        {
            hasIdleCombat = Has("Idle combat");
            hasRunStopR = Has("Run stop R");
            hasPivot = Has("Run pivot 180 left");
            hasSelectIntro = Has("Select intro");
            hasSelectIdle = Has("Select idle");
            hasRunStop = Has("Run stop");
            hasCombatReady = Has("Combat ready");
            hasCombatRelax = Has("Combat relax");
            hasCombatFidget = Has(CombatFidgetClip);
            fidgets.Clear();
            foreach (string clip in FidgetClips) if (Has(clip)) fidgets.Add(clip);
            upperIdx = animator.GetLayerIndex(FoundationKit.KitAnim.UpperBodyLayer);
            armsIdx = animator.GetLayerIndex(FoundationKit.KitAnim.UpperArmsLayer);
            overlayIdx = animator.GetLayerIndex(FoundationKit.KitAnim.OverlayLayer);
        }

        private void SetDashDir(string dir)
        {
            dashDir = dir;
            string prefix = dir == "fwd" ? "Arc Step " : "Arc Step " + dir + " ";
            dashStartState = ResolveDash(prefix, "start");
            dashLoopState = ResolveDash(prefix, "loop");
            dashEndState = ResolveDash(prefix, "end");
        }

        private string ResolveDash(string prefix, string part)
        {
            string name = prefix + part;
            return Has(name) ? name : "Arc Step left " + part; // bundle01 only has the left set
        }

        private void OnEnable()
        {
            // The select-screen mannequin is re-enabled when re-picked; replay its intro.
            if (animator && !body && hasSelectIntro) Go(Phase.SelectIntro);
            if (animator && body)
            {
                phase = Phase.Idle;
                currentState = null;
                stateTime = airTime = retainedLocomotionPhase = 0f;
                wasMoving = wasDashing = false;
                fidgetTimer.Cancel();
                combatKnown = false;
                recentRunSpeed = armsSwitchTimer = 0f;
                wasGrounded = body.characterMotor && body.characterMotor.isGrounded;
                Go(Phase.Idle);
            }
        }

        private void CacheLengths()
        {
            var controller = animator.runtimeAnimatorController;
            if (!controller) return;
            foreach (var clip in controller.animationClips)
                if (clip)
                {
                    string title = clip.name.Replace('_', ' ');
                    lengths[title] = clip.length;
                    float right, forward, duration;
                    if (FoundationLocomotionMath.TryGetAuthoredStride(title, out right, out forward, out duration))
                        authoredStrides[clip] = new Vector3(right, forward, duration);
                }
        }

        private bool Has(string state)
        {
            return animator && animator.HasState(0, Animator.StringToHash(state));
        }

        private float Length(string state)
        {
            float length;
            return state != null && lengths.TryGetValue(state, out length) ? length : 0.4f;
        }

        private void Update()
        {
            if (!animator) return;
            stateTime += Time.deltaTime;

            if (!body)
            {
                if (phase == Phase.SelectIntro && stateTime >= Length(currentState) * 0.97f) Go(Phase.SelectIdle);
                return;
            }
            if (!body.characterMotor || !body.healthComponent || !body.healthComponent.alive)
            {
                GlideWeight = 0f;
                GroundedForPresentation = false;
                footContacts.Reset();
                if (phase == Phase.GlideEnter || phase == Phase.GlideLoop)
                {
                    StopGlideLoop(body.gameObject);
                    phase = Phase.Idle;
                }
                return;
            }

            var motor = body.characterMotor;
            Vector3 velocity = motor.velocity;
            float velocitySquared = velocity.sqrMagnitude;
            if (float.IsNaN(velocitySquared) || float.IsInfinity(velocitySquared))
            {
                // Do not let a transient bad motor sample permanently poison the
                // world-space smoother or normalized blend-tree parameters.
                if (!warnedInvalidVelocity)
                    Plugin.Log.LogWarning("HOLLOW_SAINT_PRESENTATION invalid motor velocity on " + name + "; retaining last valid movement pose until the motor recovers.");
                warnedInvalidVelocity = true;
                return;
            }
            warnedInvalidVelocity = false;
            Vector3 forward = body.characterDirection ? body.characterDirection.forward : ((Component)body).transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector2 local = new Vector2(Vector3.Dot(velocity, right), Vector3.Dot(velocity, forward));
            // Smooth in world space before projecting onto the current facing.
            // Smoothing two different local coordinate frames invented sideways
            // velocity when the character turned, making the planted feet skate.
            Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);
            smoothedWorldVelocity = Vector3.Lerp(smoothedWorldVelocity, planarVelocity, 1f - Mathf.Exp(-SpeedSmoothing * Time.deltaTime));
            smoothed = new Vector2(Vector3.Dot(smoothedWorldVelocity, right), Vector3.Dot(smoothedWorldVelocity, forward));
            float planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            locomotion = FoundationLocomotionMath.Evaluate(smoothed.x, smoothed.y, planarSpeed);
            bool alreadyGliding = phase == Phase.GlideEnter || phase == Phase.GlideLoop;
            sprintGliding = body.isSprinting && planarSpeed > (alreadyGliding ? 2.5f : 3.5f);

            // Airborne debounce: a single ungrounded frame (ramps, ledges) is not a fall.
            bool groundedRaw = motor.isGrounded;
            if (!groundedRaw) airTime += Time.deltaTime;
            bool airborne = !groundedRaw && (airTime > 0.1f || velocity.y > 1f);
            bool grounded = !airborne;
            GroundedForPresentation = grounded;
            landAirTime = airTime;
            Vector2 worldVel = new Vector2(velocity.x, velocity.z);
            if (!normalizedLocomotion) TrackHeading(forward, worldVel);
            bool moving = wasMoving ? planarSpeed > (normalizedLocomotion ? 0.08f : StopThreshold) : planarSpeed > (normalizedLocomotion ? 0.15f : MoveThreshold);
            inputMoving = body.inputBank && body.inputBank.moveVector.sqrMagnitude > 0.01f;
            bool dashing = ArcStepState.IsBodyDashing(body);
            // Air jump (double jump) replays the jump clip.
            int jumps = motor.jumpCount;
            airJumped = !groundedRaw && jumps > lastJumpCount && jumps >= 2;
            lastJumpCount = groundedRaw ? 0 : jumps;
            bool casting = weapon != null && weapon.state is ArcBoltState;
            if (dashing)
            {
                dashTime = wasDashing ? dashTime + Time.deltaTime : 0f;
                if (!wasDashing || (dashNeedsRelatch && dashTime < 0.08f)) LatchDashDirection(local);
            }
            TrackPolishInputs(planarSpeed, grounded, moving);

            Phase next = Decide(grounded, moving, dashing, casting, velocity.y);
            Go(next);
            UpdateGestureLayer(planarSpeed, grounded);
            Drive(planarSpeed);
            float glideTarget = phase == Phase.GlideEnter || phase == Phase.GlideLoop ? 1f : 0f;
            GlideWeight = dashing ? 0f : Mathf.MoveTowards(GlideWeight, glideTarget, Time.deltaTime / (glideTarget > 0f ? 0.2f : 0.14f));
            if (groundedRaw) airTime = 0f;

            wasGrounded = grounded;
            wasMoving = moving;
            wasDashing = dashing;
        }

        /// <summary>Per-frame inputs for the bundle06 polish: combat flag flips, skill/jump input,
        /// recent run speed and heading (for Run stop) and the idle fidget timer.</summary>
        private void TrackPolishInputs(float planarSpeed, bool grounded, bool moving)
        {
            bool inCombat = !body.outOfCombat;
            combatFlipped = combatKnown && inCombat != lastInCombat;
            combatKnown = true;
            lastInCombat = inCombat;

            var bank = body.inputBank;
            skillInput = bank && (bank.skill1.down || bank.skill2.down || bank.skill3.down || bank.skill4.down || bank.jump.down);

            recentRunSpeed = FoundationAnimRules.TrackRecentSpeed(recentRunSpeed, planarSpeed, Time.deltaTime);
            if (moving && smoothed.sqrMagnitude > 0.01f) lastMoveForward = smoothed.normalized.y;

            bool haveFidget = inCombat ? hasCombatFidget : fidgets.Count > 0;
            bool eligible = haveFidget && phase == Phase.Idle && grounded && !moving && Calm();
            fidgetDue = fidgetTimer.Tick(eligible, Time.deltaTime);
        }

        /// <summary>No movement or skill input, and no gesture on the arm/overlay layers.</summary>
        private bool Calm()
        {
            return !inputMoving && !skillInput &&
                FoundationKit.KitAnim.LayerIdle(animator, upperIdx) &&
                FoundationKit.KitAnim.LayerIdle(animator, armsIdx) &&
                FoundationKit.KitAnim.LayerIdle(animator, overlayIdx);
        }

        /// <summary>The grounded, not-moving outcome: a combat ready/relax clip on a flag flip
        /// while standing, a due fidget, a still-running one-shot, or plain Idle.</summary>
        private Phase IdleOrOneShot()
        {
            bool standing = phase == Phase.Idle || phase == Phase.IdleOneShot;
            if (combatFlipped && standing)
            {
                string clip = lastInCombat ? (hasCombatReady ? "Combat ready" : null) : (hasCombatRelax ? "Combat relax" : null);
                if (clip == null) return Phase.Idle; // Idle <-> Idle combat cross-fade
                oneShotState = clip;
                oneShotIsFidget = false;
                return Phase.IdleOneShot;
            }
            if (phase == Phase.IdleOneShot && !Done(0.9f) && (!oneShotIsFidget || Calm())) return phase;
            if (phase == Phase.Idle && fidgetDue)
            {
                string clip = null;
                if (lastInCombat) clip = hasCombatFidget ? CombatFidgetClip : null;
                else
                {
                    int pick = FoundationAnimRules.PickFidget(fidgets.Count, lastFidget, UnityEngine.Random.value);
                    if (pick >= 0) { lastFidget = pick; clip = fidgets[pick]; }
                }
                if (clip != null)
                {
                    oneShotState = clip;
                    oneShotIsFidget = true;
                    return Phase.IdleOneShot;
                }
            }
            return Phase.Idle;
        }

        /// <summary>bundle06: when movement changes mid-gesture (standing <-> moving/airborne) and
        /// stays changed for ArmsSwitchDelay, move the gesture to the other arm layer at the same
        /// normalized time. Skipped while either layer is blending and on older controllers.</summary>
        private void UpdateGestureLayer(float planarSpeed, bool grounded)
        {
            if (armsIdx < 0 || upperIdx < 0) return;
            bool onUpper = !FoundationKit.KitAnim.LayerIdle(animator, upperIdx);
            bool onArms = !FoundationKit.KitAnim.LayerIdle(animator, armsIdx);
            bool wantArms = FoundationAnimRules.UseArmsLayer(planarSpeed, grounded);
            if (onUpper == onArms || wantArms == onArms) { armsSwitchTimer = 0f; return; }
            armsSwitchTimer += Time.deltaTime;
            if (armsSwitchTimer < ArmsSwitchDelay) return;
            armsSwitchTimer = 0f;
            FoundationKit.KitAnim.MoveGesture(animator, onArms ? armsIdx : upperIdx, onArms ? upperIdx : armsIdx, ArmsSwitchFade);
        }

        /// <summary>Flags a pivot when the facing swings more than 130 degrees within
        /// 0.2 s while running (a hard reversal), so the pivot clip can bridge it.</summary>
        private void TrackHeading(Vector3 forward, Vector2 worldVel)
        {
            headingTimer += Time.deltaTime;
            if (headingTimer < 0.2f) return;
            float angle = Vector3.SignedAngle(lastHeading, forward, Vector3.up);
            // A pivot needs the run direction to actually reverse, not just the facing to swing.
            bool reversed = Vector2.Dot(worldVel, lastWorldVel) < 0f;
            if (Mathf.Abs(angle) > 130f && reversed && smoothed.magnitude > 3.5f && !body.isSprinting)
            {
                pivotRequested = true;
                pivotLeft = angle < 0f;
            }
            lastHeading = forward;
            lastWorldVel = worldVel;
            headingTimer = 0f;
        }

        private void LatchDashDirection(Vector2 local)
        {
            Vector3 blend;
            Vector2 dir = local;
            dashNeedsRelatch = true;
            if (ArcStepState.TryGetDashBlend(body, out blend) && (blend.x * blend.x + blend.z * blend.z) > 0.01f)
            {
                dir = new Vector2(blend.x, blend.z);
                dashNeedsRelatch = false;
            }
            string d = DashDirection(dir);
            if (d != dashDir) SetDashDir(d);
        }

        private float hitPauseUntil;
        /// <summary>v0.8: a heavy hit freezes the whole pose for a few frames (visual only).</summary>
        public void HitPause(float seconds) { hitPauseUntil = Mathf.Max(hitPauseUntil, Time.time + Mathf.Clamp(seconds, 0f, 0.15f)); }

        private void Drive(float planarSpeed)
        {
            if (layered)
            {
                animator.speed = Time.time < hitPauseUntil ? 0.04f : 1f;
                if (normalizedLocomotion)
                {
                    Vector2 direction = smoothed.sqrMagnitude > 0.0001f ? smoothed.normalized : Vector2.up;
                    animator.SetFloat(RightHash, direction.x);
                    animator.SetFloat(ForwardHash, direction.y);
                    animator.SetFloat(GaitHash, locomotion.Gait);
                    // No minimum 1x playback: a slow walk retains its planted stride
                    // and slows its cadence. Hover breathing also follows travel speed.
                    bool measured = measuredReferenceSpeed > 0.05f && Vector2.Dot(direction, measuredDirection) > 0.92f &&
                        Mathf.Abs(locomotion.Gait - measuredGait) < 0.15f;
                    float cadence = measured ? planarSpeed / measuredReferenceSpeed : locomotion.Rate;
                    float moveRate = phase == Phase.Moving ? cadence :
                        phase == Phase.GlideLoop ? Mathf.Clamp(planarSpeed / 8.7f, 0.65f, 1.65f) : 1f;
                    animator.SetFloat(MoveRateHash, moveRate);
                    return;
                }
                // Blend-tree positions are authored in m/s up to 6; beyond that, speed the loop up.
                float mag = smoothed.magnitude;
                Vector2 clamped = mag > RunClipSpeed ? smoothed * (RunClipSpeed / mag) : smoothed;
                animator.SetFloat(RightHash, clamped.x);
                animator.SetFloat(ForwardHash, clamped.y);
                float rate = phase == Phase.Moving ? Mathf.Clamp(mag / RunClipSpeed, 1f, 2.2f) : 1f;
                if (phase == Phase.Moving && !hasLocomotion) rate = Mathf.Clamp(planarSpeed / RunClipSpeed, 0.5f, 2.2f);
                animator.SetFloat(MoveRateHash, rate);
                Footsteps(rate);
            }
            else
            {
                animator.speed = phase == Phase.Moving ? Mathf.Clamp(planarSpeed / 7f, 0.5f, 2.2f) : 1f;
                Footsteps(animator.speed);
            }
        }

        private static string DashDirection(Vector2 local)
        {
            if (local.sqrMagnitude < 0.01f) return "fwd";
            if (Mathf.Abs(local.x) > Mathf.Abs(local.y)) return local.x > 0f ? "right" : "left";
            return local.y >= 0f ? "fwd" : "back";
        }

        private bool Done(float fraction = 0.95f) { return stateTime >= Length(currentState) * fraction; }

        private Phase Decide(bool grounded, bool moving, bool dashing, bool casting, float upSpeed)
        {
            // Dash owns the body: start, loop, then end on release.
            if (dashing)
            {
                if (!wasDashing) return Phase.DashStart;
                if (phase == Phase.DashStart && !Done()) return phase;
                return Phase.DashLoop;
            }
            if (wasDashing) return grounded ? Phase.DashEnd :
                FoundationAnimRules.IsAscending(upSpeed) ? Phase.Ascend : Phase.Descend;
            if (phase == Phase.DashEnd && !Done(0.87f) && !inputMoving && grounded) return phase;

            if (phase == Phase.Spawn && !Done() && !moving && grounded) return phase;

            if (!grounded)
            {
                pivotRequested = false;
                if (wasGrounded) return upSpeed > 1f ? Phase.Jump : Phase.Descend;
                if (airJumped) { phase = Phase.Descend; return Phase.Jump; } // force a restart of the jump clip
                if (phase == Phase.Jump && !Done() && upSpeed > 0f) return phase;
                return FoundationAnimRules.IsAscending(upSpeed) ? Phase.Ascend : Phase.Descend;
            }

            if (!wasGrounded && landAirTime > 0.25f) return Phase.Land;
            // The authored land-to-run blend starts at the compress pose (frame 6).
            if (phase == Phase.Land && stateTime < (moving ? Frames(6f) : Length(currentState) * 0.9f)) return phase;

            if ((normalizedLocomotion ? sprintGliding : body.isSprinting) && moving)
            {
                if (phase != Phase.GlideEnter && phase != Phase.GlideLoop) return Phase.GlideEnter;
                if (phase == Phase.GlideEnter && !Done()) return phase;
                return Phase.GlideLoop;
            }
            if (phase == Phase.GlideLoop || phase == Phase.GlideEnter)
                return moving ? Phase.GlideExit : normalizedLocomotion ? Phase.Idle : Phase.RunStop;
            // A released glide may be cancelled at once if the Saint has already stopped;
            // otherwise let the authored f14 contact hand off to the run/stop seam.
            if (phase == Phase.GlideExit && !Done() && moving) return phase;

            // Native acceleration/reversal is unconstrained by the authored start,
            // stop and pivot trajectories. Keep the same normalized locomotion phase
            // while changing direction; an unrelated canned pivot breaks contact.
            // Stopping from run speed plays the Run stop (foot latched from the gait phase); new
            // movement input cancels it straight back into the phase-matched Moving tree.
            if (normalizedLocomotion)
            {
                if (moving) return Phase.Moving;
                if (phase == Phase.RunStop)
                {
                    if (inputMoving) return Phase.Moving;
                    if (!Done(0.9f)) return phase;
                }
                else if (phase == Phase.Moving && hasRunStop && FoundationAnimRules.ShouldRunStop(recentRunSpeed, lastMoveForward))
                    return Phase.RunStop;
                return IdleOrOneShot();
            }

            // Single-layer fallback only: the bolt gesture needs the whole body.
            if (!layered && casting && !moving)
                return phase != Phase.Cast || Done() ? Phase.Cast : phase;
            if (phase == Phase.Cast && !Done() && !moving) return phase;

            if (moving && pivotRequested && layered && hasPivot)
            {
                pivotRequested = false;
                return Phase.Pivot;
            }
            pivotRequested = false;
            if (phase == Phase.Pivot && moving && !Done(0.9f)) return phase;

            if (moving)
            {
                if (phase == Phase.Idle || phase == Phase.IdleOneShot || phase == Phase.RunStop || phase == Phase.DashEnd || phase == Phase.Spawn)
                    return smoothed.y > 2f ? Phase.RunStart : Phase.Moving; // run start is a forward clip
                if (phase == Phase.RunStart && !Done() && smoothed.y > 1f) return phase;
                return Phase.Moving;
            }
            if (phase == Phase.Moving || phase == Phase.RunStart || phase == Phase.GlideExit || phase == Phase.Pivot)
                return smoothed.magnitude > 2.5f ? Phase.RunStop : Phase.Idle;
            if (phase == Phase.RunStop && !Done()) return phase;
            return IdleOrOneShot();
        }

        private string StateFor(Phase p)
        {
            switch (p)
            {
                case Phase.Idle: return !body.outOfCombat && hasIdleCombat ? "Idle combat" : "Idle";
                case Phase.RunStart: return "Run start";
                case Phase.Pivot: return latchedPivotLeft ? "Run pivot 180 left" : "Run pivot 180 right";
                case Phase.Moving: return hasLocomotion ? "Locomotion" : "Run forward";
                case Phase.RunStop: return latchedStopR ? "Run stop R" : "Run stop";
                case Phase.Jump: return "Jump";
                case Phase.Ascend: return "Ascend";
                case Phase.Descend: return "Descend";
                case Phase.Land: return "Land";
                case Phase.GlideEnter: return "Glide enter";
                case Phase.GlideLoop: return "Glide loop";
                case Phase.GlideExit: return "Glide exit";
                case Phase.DashStart: return dashStartState;
                case Phase.DashLoop: return dashLoopState;
                case Phase.DashEnd: return dashEndState;
                case Phase.Cast: return "Arc Bolt right";
                case Phase.Spawn: return "Spawn";
                case Phase.SelectIntro: return "Select intro";
                case Phase.SelectIdle: return hasSelectIdle ? "Select idle" : "Idle";
                case Phase.IdleOneShot: return oneShotState;
                default: return "Idle";
            }
        }

        private string DisplayState(Phase p)
        {
            if (p == Phase.SelectIntro && hasSelectIntro) return "Select intro";
            if (p == Phase.SelectIdle && hasSelectIdle) return "Select idle";
            return "Idle";
        }

        private static float Frames(float frames) { return frames / AuthoredFrameRate; }

        private static float FadeInto(Phase from, Phase to, out float destinationOffset)
        {
            destinationOffset = 0f;
            // Pair-specific lengths follow the 24 fps contract in TRANSITIONS.md.
            // Keep the special-case list narrow so jump/land responsiveness is preserved
            // outside the tested locomotion and contact handoffs.
            if (from == Phase.GlideLoop && to == Phase.GlideExit) return Frames(4f);
            if (from == Phase.Descend && to == Phase.Land) return Frames(3f);
            if (from == Phase.Moving && to == Phase.Jump)
            {
                destinationOffset = Frames(3f); // run -> Jump f4 (one-based)
                return Frames(6f);
            }
            if (from == Phase.Land && to == Phase.Moving) return Frames(6f);
            if (from == Phase.Idle && to == Phase.Moving) return Frames(8f);
            if (from == Phase.GlideExit && (to == Phase.Moving || to == Phase.RunStop || to == Phase.Idle))
                return to == Phase.Idle ? 0.18f : 0.12f;

            return FadeInto(to);
        }

        private static float FadeInto(Phase p)
        {
            switch (p)
            {
                case Phase.Land: return 0.04f;
                case Phase.Jump: return 0.05f;
                case Phase.DashStart: return 0.04f;
                case Phase.Cast: return 0.06f;
                case Phase.RunStop: return 0.08f;
                case Phase.Idle: return 0.18f;
                case Phase.Descend: return 0.2f;
                case Phase.SelectIntro: return 0f;
                default: return 0.12f;
            }
        }

        /// <summary>bundle06 polish fades layered over FadeInto: slower combat stance changes,
        /// fidget in/out, run -> Idle combat, glide -> jump and run -> glide enter.</summary>
        private float PolishFade(Phase from, Phase to, string state, float fade)
        {
            bool combatStance = state == "Idle combat" || state == "Combat ready" || state == "Combat relax";
            if (to == Phase.IdleOneShot) return oneShotIsFidget ? FidgetFade : CombatFlipFade;
            if (from == Phase.IdleOneShot && to == Phase.Idle) return oneShotIsFidget ? FidgetFade : CombatFlipFade;
            if (from == Phase.Idle && to == Phase.Idle && currentState != null) return CombatFlipFade; // Idle <-> Idle combat flag flip
            if ((from == Phase.Moving || from == Phase.RunStop) && to == Phase.Idle && combatStance) return MoveToIdleCombatFade;
            if (from == Phase.Moving && to == Phase.Idle) return MoveToIdleFade;
            if ((from == Phase.GlideLoop || from == Phase.GlideEnter) && to == Phase.Jump) return GlideToJumpFade;
            if (from == Phase.Moving && to == Phase.GlideEnter) return RunToGlideFade;
            return fade;
        }

        private void Go(Phase next)
        {
            if (next != phase)
            {
                // Latch side choices on phase entry so the state cannot flip mid-clip.
                // Normalized locomotion: the stop pivots on the foot the gait phase last planted.
                latchedStopR = hasRunStopR && (normalizedLocomotion
                    ? FoundationAnimRules.RunStopOnRightFoot(retainedLocomotionPhase)
                    : smoothed.x > 0.5f);
                latchedPivotLeft = pivotLeft;
            }
            string state = body ? StateFor(next) : DisplayState(next);
            if (next == phase && state == currentState) return;
            int hash = Animator.StringToHash(state);
            if (!animator.HasState(0, hash))
            {
                if (reportedMissing.Add(state))
                    Plugin.Log.LogWarning("HOLLOW_SAINT_ANIM_STATE_MISSING_AT_PLAY state='" + state + "'");
                phase = next;
                currentState = state;
                stateTime = 99f; // treat as finished so the next frame moves on
                return;
            }
            float destinationOffset;
            float transitionDuration = FadeInto(phase, next, out destinationOffset);
            if (normalizedLocomotion && next == Phase.Moving)
            {
                float normalized = FoundationLocomotionMath.MovingEntryPhase(currentState, retainedLocomotionPhase);
                destinationOffset = normalized * locomotion.Duration;
                transitionDuration = phase == Phase.Idle || phase == Phase.IdleOneShot ? 0.12f : 0.1f;
            }
            else transitionDuration = PolishFade(phase, next, state, transitionDuration);
            animator.CrossFadeInFixedTime(hash, transitionDuration, 0, destinationOffset);
            if (body && next != phase) PhaseSound(phase, next);
            phase = next;
            currentState = state;
            stateTime = destinationOffset;
        }

        private void LateUpdate()
        {
            if (!normalizedLocomotion || !animator || !IsPresentingAlive ||
                phase != Phase.Moving || !body.characterMotor.isGrounded)
            { footContacts.Reset(); return; }
            var state = animator.GetCurrentAnimatorStateInfo(0);
            bool useNext = false;
            if (animator.IsInTransition(0))
            {
                var next = animator.GetNextAnimatorStateInfo(0);
                if (next.shortNameHash == LocomotionHash) { state = next; useNext = true; }
            }
            if (state.shortNameHash != LocomotionHash) { footContacts.Reset(); return; }
            MeasureStride(useNext);
            retainedLocomotionPhase = Mathf.Repeat(state.normalizedTime, 1f);
            if (footContacts.Advance(state.normalizedTime))
                Util.PlaySound(locomotion.Gait >= 0.5f ? FoundationKit.Vfx.KitSfx.FootstepRun :
                    FoundationKit.Vfx.KitSfx.Footstep, body.gameObject);
        }

        private void MeasureStride(bool nextState)
        {
            if (nextState) animator.GetNextAnimatorClipInfo(0, movingClips);
            else animator.GetCurrentAnimatorClipInfo(0, movingClips);
            Vector2 direction = smoothed.sqrMagnitude > 0.0001f ? smoothed.normalized : Vector2.up;
            float stride = 0f, duration = 0f, total = 0f;
            foreach (var clip in movingClips)
            {
                Vector3 authored;
                if (!clip.clip || !authoredStrides.TryGetValue(clip.clip, out authored)) continue;
                stride += clip.weight * (authored.x * direction.x + authored.y * direction.y);
                duration += clip.weight * authored.z;
                total += clip.weight;
            }
            // Normalize crossfade contributions implicitly (the same weight sum
            // cancels in distance/duration). Use actual Unity directional weights,
            // not an assumption about its SimpleDirectional2D interpolation.
            if (total > 0.001f && stride > 0.001f && duration > 0.001f)
            {
                measuredReferenceSpeed = stride / duration;
                measuredDirection = direction;
                measuredGait = locomotion.Gait;
            }
        }

        private float stepTimer;

        private void PhaseSound(Phase from, Phase to)
        {
            var go = body.gameObject;
            if (to == Phase.Land) Util.PlaySound(FoundationKit.Vfx.KitSfx.Land, go);
            if (to == Phase.GlideEnter)
            {
                Util.PlaySound(FoundationKit.Vfx.KitSfx.GlideEnter, go);
                Util.PlaySound(FoundationKit.Vfx.KitSfx.GlideLoopStart, go);
                glideLoopObject = go;
            }
            bool wasGliding = from == Phase.GlideEnter || from == Phase.GlideLoop;
            bool isGliding = to == Phase.GlideEnter || to == Phase.GlideLoop;
            if (wasGliding && !isGliding) { StopGlideLoop(go); Util.PlaySound(FoundationKit.Vfx.KitSfx.GlideExit, go); }
        }

        /// <summary>Stops the glide loop on the object it was started on, whatever the body's
        /// state is now. Falls back to the given object if none was recorded.</summary>
        private void StopGlideLoop(GameObject fallback)
        {
            GameObject target = glideLoopObject != null ? glideLoopObject : fallback;
            glideLoopObject = null;
            if (target != null) Util.PlaySound(FoundationKit.Vfx.KitSfx.GlideLoopStop, target);
        }

        /// <summary>Footsteps from the run cycle, since the game clips carry no animation
        /// events: two steps per loop, scaled by the playback rate.</summary>
        private void Footsteps(float rate)
        {
            if (phase != Phase.Moving && phase != Phase.RunStart && phase != Phase.RunStop && phase != Phase.Pivot) { stepTimer = 0f; return; }
            stepTimer -= Time.deltaTime * Mathf.Max(0.5f, rate);
            if (stepTimer > 0f) return;
            float mag = smoothed.magnitude;
            stepTimer = mag > RunFootstepSpeed ? (16f / AuthoredFrameRate) * 0.5f : (26f / AuthoredFrameRate) * 0.5f; // run or walk half-cycle
            Util.PlaySound(mag > RunFootstepSpeed ? FoundationKit.Vfx.KitSfx.FootstepRun : FoundationKit.Vfx.KitSfx.Footstep, body.gameObject);
        }

        private void OnDisable()
        {
            if (glideLoopObject != null) StopGlideLoop(null);
            GlideWeight = 0f;
            GroundedForPresentation = false;
            smoothedWorldVelocity = Vector3.zero;
            measuredReferenceSpeed = 0f;
            footContacts.Reset();
        }

        private void OnDestroy()
        {
            if (glideLoopObject != null) StopGlideLoop(null);
        }

        private void DumpControllerContract()
        {
            var controller = animator.runtimeAnimatorController;
            if (!controller)
            {
                Plugin.Log.LogWarning("HOLLOW_SAINT_ANIM_NO_CONTROLLER on " + name);
                return;
            }
            var sb = new StringBuilder();
            sb.Append("HOLLOW_SAINT_ANIM_CONTRACT controller=").Append(controller.name)
              .Append(" layerCount=").Append(animator.layerCount).Append(" clips=").Append(lengths.Count);
            for (int i = 0; i < animator.layerCount; i++) sb.Append(" | layer[").Append(i).Append("]=").Append(animator.GetLayerName(i));
            Plugin.Log.LogInfo(sb.ToString());
            string[] expected = { "Idle", "Run start", "Run stop", "Jump", "Ascend", "Descend", "Land",
                "Glide enter", "Glide loop", "Glide exit", "Arc Step left start", "Arc Step left loop", "Arc Step left end" };
            int missing = 0;
            foreach (string s in expected) if (!Has(s)) { missing++; Plugin.Log.LogWarning("HOLLOW_SAINT_ANIM_STATE_MISSING state='" + s + "'"); }
            Plugin.Log.LogInfo("HOLLOW_SAINT_ANIM_CONTRACT_DONE layered=" + layered + " locomotionTree=" + hasLocomotion +
                " driven=" + expected.Length + " missing=" + missing);
            // bundle06 polish is optional: anything absent falls back to the bundle05 behavior.
            string[] polish = { "Combat ready", "Combat relax", "Idle fidget 1", "Idle fidget 2", "Idle fidget 3", CombatFidgetClip, "Run stop R" };
            var absent = new StringBuilder();
            foreach (string s in polish) if (!Has(s)) absent.Append(absent.Length > 0 ? "," : "").Append(s);
            string[] gestures = { "Conduit Spear recover", "Open Circuit arms", "Open Circuit arms hold" };
            foreach (string s in gestures)
                if (upperIdx < 0 || !animator.HasState(upperIdx, Animator.StringToHash(s))) absent.Append(absent.Length > 0 ? "," : "").Append(s);
            if (overlayIdx < 0 || !animator.HasState(overlayIdx, Animator.StringToHash("Discharge recover")))
                absent.Append(absent.Length > 0 ? "," : "").Append("Discharge recover");
            Plugin.Log.LogInfo("HOLLOW_SAINT_ANIM_POLISH upperArms=" + (armsIdx >= 0) + " fidgets=" + fidgets.Count +
                " absent=" + (absent.Length > 0 ? absent.ToString() : "none"));
        }
    }
}
