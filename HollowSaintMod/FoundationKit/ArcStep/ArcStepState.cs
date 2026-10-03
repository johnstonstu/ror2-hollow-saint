using System.Collections.Generic;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.ArcStep
{
    /// <summary>Dash-internal tunables. Every approved Arc Step number lives in KitTuning
    /// and is read from there — nothing here overrides an approved value. Members marked
    /// PROPOSAL are defensible defaults parked for Stuart's M5 pass; they live in this
    /// file because KitShared.cs is append-only and not editable by skill agents.</summary>
    public static class ArcStepStateTuning
    {
        /// <summary>PROPOSAL: movement input below this magnitude latches the model's
        /// forward as the dash direction instead of the (near-zero) input vector, so a
        /// charged stock is never spent on a zero-length step.</summary>
        public const float InputDeadzone = 0.1f;

        /// <summary>PROPOSAL: fraction of KitTuning.ArcStepSpeed the dash still carries at
        /// its end. Speed tapers linearly across the dash so the step flows into a run
        /// instead of stopping dead at 16 m/s.</summary>
        public const float ExitSpeedFraction = 0.5f;

        /// <summary>PROPOSAL: false = the dash is planar — vertical velocity is driven to
        /// zero for the step, so an air dash holds its altitude and falls normally once the
        /// dash ends. true = the vertical velocity present at activation is held for the
        /// whole dash (a glide). The brief only says "usable in the air"; the planar step
        /// is the defensible reading.</summary>
        public static readonly bool PreserveVerticalVelocity = false;
    }

    /// <summary>Arc Step: the utility dash. Latches a planar direction at activation from
    /// InputBankTest.moveVector (world-space, so W/A/S/D give forward/back/left/right and
    /// combined keys give blended diagonals), drives CharacterMotor.velocity for
    /// KitTuning.ArcStepDuration at KitTuning.ArcStepSpeed (tapering linearly to
    /// ArcStepStateTuning.ExitSpeedFraction), and hands control back by simply stopping.
    ///
    /// Control-safety contract: input is never touched and the motor is never disabled —
    /// the dash only overwrites motor velocity while it runs. On every exit path (natural
    /// end, SetInterruptState of any priority, death) OnExit clears the animator flags and
    /// the i-frame buff branch, and the main state (vanilla GenericCharacterMain on the
    /// "Body" machine) owns locomotion again the same tick.
    ///
    /// Server-authority: motor writes, the i-frame buff branch and the VFX hook call are
    /// gated on EntityState.isAuthority. The state deals no damage, consumes no stock and
    /// manipulates no cooldown itself — stock and recharge are the vanilla GenericSkill /
    /// SkillDef path (server-authoritative by construction).
    ///
    /// Networking: the latched direction is serialized so remote machines can drive the
    /// directional animation; movement itself replicates through the motor.
    ///
    /// Animation: parameter-driven only (isDashing + dashBlendX/dashBlendZ). The state
    /// never calls PlayCrossfade, so it cannot fight FoundationPresentation, which
    /// crossfades the Body layer every LateUpdate from motor velocity. The parameter
    /// names are written only if the runtime controller declares them — until the
    /// coordinator imports the dash clips and parameters the dash simply looks like a
    /// fast run, with no animator warnings. The full proposal is in RESULT-ARCSTEP.md.</summary>
    public sealed class ArcStepState : EntityState
    {
        public const string AnimatorParamIsDashing = "isDashing";
        public const string AnimatorParamDashBlendX = "dashBlendX";
        public const string AnimatorParamDashBlendZ = "dashBlendZ";

        private static readonly int IsDashingHash = Animator.StringToHash(AnimatorParamIsDashing);
        private static readonly int DashBlendXHash = Animator.StringToHash(AnimatorParamDashBlendX);
        private static readonly int DashBlendZHash = Animator.StringToHash(AnimatorParamDashBlendZ);

        // Per-machine registry of bodies currently dashing. The state runs on every machine
        // (authority and remote), so the query answers locally wherever it is asked.
        private static readonly Dictionary<GameObject, ArcStepState> DashingBodies = new Dictionary<GameObject, ArcStepState>();

        /// <summary>True while this body's Arc Step state is active on this machine. Read
        /// by FoundationPresentation's proposed dash gate and by other kit skills (the
        /// parked "can Open Circuit strike during Arc Step" question).</summary>
        public static bool IsBodyDashing(CharacterBody body)
        {
            return body != null && DashingBodies.ContainsKey(body.gameObject);
        }

        /// <summary>Model-space dash blend (x = right, z = forward, [-1, 1]) of the body's
        /// active Arc Step on this machine. False when the body is not dashing.</summary>
        public static bool TryGetDashBlend(CharacterBody body, out Vector3 blend)
        {
            ArcStepState state;
            if (body != null && DashingBodies.TryGetValue(body.gameObject, out state) && state != null)
            {
                blend = state.dashBlend;
                return true;
            }
            blend = Vector3.zero;
            return false;
        }

        // World-space, planar, normalized. Latched at OnEnter on the authority machine and
        // replicated to remote machines via OnSerialize/OnDeserialize.
        private Vector3 dashDirection = Vector3.zero;
        // Model-space dash direction for the animator blend tree: x = right, z = forward,
        // both in [-1, 1]. Diagonals keep both components — that is the "diagonals by
        // blend" behaviour, not a snap to the nearest cardinal.
        private Vector3 dashBlend = Vector3.zero;
        private float entryVerticalVelocity;
        private bool startedGrounded;
        private bool iframeBuffApplied; // true only on the machine that added the buff
        private Animator animator;
        private bool animatorParamsAvailable;
        // Own dash clock. EntityState.fixedAge is only maintained by BaseState subclasses;
        // this state extends EntityState directly (like vanilla EntityStates.Commando.
        // DodgeState), so without this timer the dash would never reach its end condition.
        private float dashAge;
        // Authority side: the body was sprinting (or sprint was held) when the dash began, so
        // the sprint is handed back when it ends instead of dropping to a walk.
        private bool wasSprinting;

        public override void OnEnter()
        {
            base.OnEnter();
            if (characterBody != null) DashingBodies[characterBody.gameObject] = this;
            if (isAuthority && characterBody != null)
                wasSprinting = characterBody.isSprinting || (inputBank != null && inputBank.sprint.down);

            if (isAuthority) LatchDirection();
            if (isAuthority) KitLog.Event("ARC_STEP_STARTED");
            if (characterBody)
            {
                Vfx.KitFx.Local(Vfx.Beat.ArcStepStart, characterBody, characterBody.footPosition);
                var fx = characterBody.GetComponent<Vfx.BodyFx>();
                if (fx) fx.BeginDash(KitTuning.ArcStepDuration);
            }
            ComputeBlendFromDirection();

            startedGrounded = characterMotor != null && characterMotor.isGrounded;
            entryVerticalVelocity = characterMotor != null ? characterMotor.velocity.y : 0f;

            CacheAnimatorAndParameters();
            SetDashAnimatorParams(true);

            // PROPOSAL branch: KitTuning.ArcStepGrantsIFrames defaults to false and the
            // constant is never hardcoded around; flipping it in KitShared.cs activates
            // the hidden-invincibility window pending Stuart's M5 decision.
            if (KitTuning.ArcStepGrantsIFrames && isAuthority && characterBody != null)
            {
                characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
                iframeBuffApplied = true;
            }

            // Ground-trail VFX hook, authority side only (spawning is authority work; the
            // VFX owner decides how the effect replicates). See ArcStepVfxHooks.
            if (isAuthority && characterBody != null)
            {
                Vector3 start = ((Component)characterBody).transform.position;
                ArcStepVfxHooks.SpawnGroundTrail(characterBody, start, dashDirection,
                    KitTuning.ArcStepDuration, KitTuning.ArcStepSpeed * Mathf.Max(characterBody.moveSpeed / 7f, 1f), startedGrounded);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            // Movement authority gate: motor writes happen only where the state has
            // authority (the local client for the player, the server for AI bodies).
            // Remote machines receive movement through motor replication and this state
            // through the networked state machine.
            if (!isAuthority) return;
            // Never drive a dead body. Death interrupts this state at InterruptPriority
            // .Death and OnExit still runs; this guard only stops the writes in the
            // window between dying and that transition landing.
            if (characterBody == null || characterBody.healthComponent == null || !characterBody.healthComponent.alive) return;

            dashAge += GetDeltaTime();
            // Keep the sprint flag up for the whole dash so nothing in between drops it.
            if (wasSprinting) characterBody.isSprinting = true;

            // Jump cancels the dash into a jump that keeps the dash's momentum, so
            // Arc Step chains into air movement instead of eating the jump input.
            if (dashAge > 0.06f && inputBank != null && inputBank.jump.justPressed &&
                characterMotor != null && characterMotor.jumpCount < characterBody.maxJumpCount)
            {
                Vector3 keep = characterMotor.velocity;
                EntityStates.GenericCharacterMain.ApplyJumpVelocity(characterMotor, characterBody, 1f, 1f, false);
                // Keep the dash's horizontal momentum through the jump.
                Vector3 jumped = characterMotor.velocity;
                jumped.x = keep.x;
                jumped.z = keep.z;
                characterMotor.velocity = jumped;
                characterMotor.jumpCount++;
                KitLog.Event("ARC_STEP_JUMP_CANCEL");
                if (outer != null) outer.SetNextStateToMain();
                return;
            }
            if (dashAge >= KitTuning.ArcStepDuration)
            {
                if (outer != null) outer.SetNextStateToMain();
                return;
            }

            if (characterMotor == null || dashDirection.sqrMagnitude <= 0f) return;

            float fraction = Mathf.Clamp01(dashAge / KitTuning.ArcStepDuration);
            float k = Mathf.Max(characterBody.moveSpeed / 7f, 1f);
            float speed = Mathf.Lerp(KitTuning.ArcStepSpeed * k, KitTuning.ArcStepSpeed * k * ArcStepStateTuning.ExitSpeedFraction, fraction);
            Vector3 velocity = dashDirection * speed;
            // NOTE: PreserveVerticalVelocity is a compile-time const false, so the
            // restore below is currently dead by design — the dash is planar and
            // intentionally holds altitude. If Stuart ever flips the const to true,
            // the line becomes live and preserves the entry vertical velocity; the
            // comment below describes THAT case, not the current one.
            if (ArcStepStateTuning.PreserveVerticalVelocity) velocity.y = entryVerticalVelocity;
            // v0.9.16 (Stu: "slightly move you in the direction you're looking"): the step follows
            // the crosshair a little up or down, so it can lift you onto a ledge or out of a fall.
            // Up to ArcStepLookLift x speed (about 2 m of rise looking 45 degrees up); downward only
            // in the air. Steering stays on the movement keys.
            if (KitTuning.ArcStepLookLift > 0f && inputBank != null)
            {
                float pitch = Mathf.Clamp(inputBank.aimDirection.normalized.y, -0.6f, 0.85f);
                if (pitch < 0f && characterMotor.isGrounded) pitch = 0f;
                velocity.y = speed * pitch * KitTuning.ArcStepLookLift;
                if (velocity.y > 0.5f && characterMotor.isGrounded && characterMotor.Motor) characterMotor.Motor.ForceUnground(0.1f);
            }
            // As shipped (PreserveVerticalVelocity == false) the planar assignment
            // leaves velocity.y at 0: the dash holds altitude, and because velocity is
            // rewritten every FixedUpdate no gravity accumulates inside the dash
            // window. CharacterMotor stays enabled and owns collision, gravity and
            // grounding throughout — this state only overwrites velocity, which is the
            // project rule (never bypass the motor).
            characterMotor.velocity = velocity;

            // Live samples for the VFX trail owner (see ArcStepTrailSampler).
            ArcStepTrailSampler.PushSample(characterBody, ((Component)characterBody).transform.position, velocity, characterMotor.isGrounded);
        }

        public override void OnExit()
        {
            // Every exit path lands here — natural end, SetInterruptState of any
            // priority, death, machine teardown. Restore in absolute silence: if any of
            // this threw, the player would be stranded in a broken state.
            if (iframeBuffApplied && characterBody != null)
            {
                characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
                iframeBuffApplied = false;
            }
            SetDashAnimatorParams(false);
            // Sprint hand-back (every exit path, including the jump cancel): the dash ends in a
            // sprint if it began in one or sprint is still held, and the body is still moving.
            if (isAuthority && characterBody != null && characterBody.healthComponent != null &&
                characterBody.healthComponent.alive && inputBank != null)
            {
                bool moving = inputBank.moveVector.sqrMagnitude > ArcStepStateTuning.InputDeadzone * ArcStepStateTuning.InputDeadzone;
                if ((wasSprinting || inputBank.sprint.down) && moving) characterBody.isSprinting = true;
            }
            if (characterBody) Vfx.KitFx.Local(Vfx.Beat.ArcStepEnd, characterBody, characterBody.footPosition);
            if (isAuthority) ArcStepVfxHooks.EndGroundTrail(characterBody); // null-tolerant
            if (characterBody != null)
            {
                ArcStepState current;
                if (DashingBodies.TryGetValue(characterBody.gameObject, out current) && current == this)
                    DashingBodies.Remove(characterBody.gameObject);
            }
            base.OnExit();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(dashDirection);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            dashDirection = reader.ReadVector3();
            ComputeBlendFromDirection();
            // Deserialization can land after OnEnter on remote machines; re-apply so the
            // animator blend reflects the replicated direction under either ordering.
            SetDashAnimatorParams(true);
        }

        public override void Reset()
        {
            dashDirection = Vector3.zero;
            dashBlend = Vector3.zero;
            entryVerticalVelocity = 0f;
            startedGrounded = false;
            iframeBuffApplied = false;
            animator = null;
            animatorParamsAvailable = false;
            dashAge = 0f;
            wasSprinting = false;
            base.Reset();
        }

        // Most permissive setting: other skills, pain, stun and death all cancel the dash
        // and OnExit restores control. PROPOSAL: Stuart may prefer InterruptPriority.Skill
        // so primaries cannot cancel a step mid-flight.
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            // Brief commit window so mashing cannot burn both charges instantly.
            return dashAge < 0.3f ? InterruptPriority.PrioritySkill : InterruptPriority.Any;
        }

        private void LatchDirection()
        {
            Vector3 move = inputBank != null ? inputBank.moveVector : Vector3.zero;
            Vector3 planar = new Vector3(move.x, 0f, move.z);
            if (planar.sqrMagnitude < ArcStepStateTuning.InputDeadzone * ArcStepStateTuning.InputDeadzone)
            {
                // No movement input: step along the model's facing, so a charged stock is
                // never spent on a zero-length dash.
                planar = PlanarFacing();
            }
            if (planar.sqrMagnitude <= 1e-8f) planar = Vector3.forward;
            dashDirection = planar.normalized;
        }

        private void ComputeBlendFromDirection()
        {
            if (dashDirection.sqrMagnitude <= 1e-8f)
            {
                dashBlend = Vector3.zero;
                return;
            }
            Vector3 forward = PlanarFacing();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x); // forward yawed +90° in the XZ plane
            dashBlend = new Vector3(Vector3.Dot(dashDirection, right), 0f, Vector3.Dot(dashDirection, forward));
        }

        private Vector3 PlanarFacing()
        {
            if (characterDirection != null && characterDirection.forward.sqrMagnitude > 1e-8f)
            {
                Vector3 forward = characterDirection.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 1e-8f) return forward.normalized;
            }
            Vector3 bodyForward = characterBody != null ? ((Component)characterBody).transform.forward : Vector3.forward;
            bodyForward.y = 0f;
            return bodyForward.sqrMagnitude > 1e-8f ? bodyForward.normalized : Vector3.forward;
        }

        private void CacheAnimatorAndParameters()
        {
            animator = GetModelAnimator();
            animatorParamsAvailable = false;
            if (animator == null) return;
            bool hasIsDashing = false;
            bool hasBlendX = false;
            bool hasBlendZ = false;
            var parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                int hash = Animator.StringToHash(parameters[i].name);
                if (hash == IsDashingHash) hasIsDashing = true;
                else if (hash == DashBlendXHash) hasBlendX = true;
                else if (hash == DashBlendZHash) hasBlendZ = true;
            }
            animatorParamsAvailable = hasIsDashing && hasBlendX && hasBlendZ;
        }

        private void SetDashAnimatorParams(bool dashing)
        {
            // Only write parameters the controller actually declares. Until the
            // coordinator adds them, this is a no-op and the frozen FoundationPresentation
            // keeps full ownership of the animator — the dash looks like a fast run, with
            // zero new animator warnings.
            if (animator == null || !animatorParamsAvailable) return;
            animator.SetBool(IsDashingHash, dashing);
            if (dashing)
            {
                animator.SetFloat(DashBlendXHash, dashBlend.x);
                animator.SetFloat(DashBlendZHash, dashBlend.z);
            }
        }
    }
}