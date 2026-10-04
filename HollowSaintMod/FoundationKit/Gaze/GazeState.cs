using EntityStates;
using HollowSaint.FoundationKit.ArcStep;
using HollowSaint.FoundationKit.OpenCircuit;
using HollowSaint.FoundationKit.Stormspear;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>
    /// Gaze of the Hollow, on the "Crown" machine. Wind-up (launch to hover height while the halo
    /// leaves the head and floats out in front), then a duration-snapshotted beam. Runs on every machine:
    /// the authority owns movement, aim and later discrete fuel taps; Arc Step/time end it. The
    /// server deals the damage from its own copy; GazeBeam draws the same beam everywhere.
    /// </summary>
    public class GazeState : BaseSkillState
    {
        private GazeBeam beam;
        private EntityStateMachine weapon, spear, bodyMachine;
        private bool gravityHeld, ignited;
        private float targetFootY, tickTimer, forkTimer, armsRest;
        private bool armored;
        private bool endRequested;
        private float beamDuration;
        private readonly GazeTapEdges tapEdges = new GazeTapEdges();
        private GazeFuelController fuel;
        private GazeFuelEndReason fuelEndReason = GazeFuelEndReason.Interrupted;

        private float BeamEnd => GazeTuning.WindupSeconds + beamDuration;
        // EntityState.fixedAge is protected in the real engine. Read it legally
        // inside this subclass rather than through the publicized build reference.
        internal float AuthoritativeCastAge => fixedAge;
        internal bool FuelAdmissionOpen => !endRequested && !GazeManualLifetime.StopBeforeWork(fixedAge, BeamEnd,
            bodyMachine && bodyMachine.state is ArcStepState);
        internal void ApplyServerDuration(float duration)
        {
            if (GazeDurationPolicy.ValidSnapshot(duration)) beamDuration = duration;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            beamDuration = GazeDurationPolicy.ForLevel(GazeTuning.BeamSeconds, characterBody ? characterBody.level : 1f);
            tapEdges.Begin(inputBank && inputBank.skill4.down);
            beam = GazeBeam.For(characterBody);
            if (beam) beam.Begin(GetAimRay().direction, beamDuration);
            fuel = characterBody ? characterBody.GetComponent<GazeFuelController>() : null;
            if (fuel) fuel.BeginLocalCast();
            if (fuel && NetworkServer.active) fuel.BeginCast(this, beamDuration);
            GazeFallGuard.Hold(characterBody);
            weapon = EntityStateMachine.FindByCustomName(gameObject, "Weapon");
            spear = EntityStateMachine.FindByCustomName(gameObject, StormspearRegistration.MachineName);
            bodyMachine = EntityStateMachine.FindByCustomName(gameObject, "Body");
            if (characterBody) characterBody.SetAimTimer(2f);
            KitAnim.PlayGesture(characterBody, GetModelAnimator(), OpenCircuitTuning.CastArmsState, GazeTuning.WindupSeconds);
            if (isAuthority) BeginHover();
            if (NetworkServer.active && characterBody && GazeArmor.Def) { characterBody.AddBuff(GazeArmor.Def); armored = true; }
            KitLog.Event("GAZE_START", "authority=" + isAuthority);
        }

        private void BeginHover()
        {
            var motor = characterMotor;
            if (!motor || !characterBody) return;
            var gravity = motor.gravityParameters;
            gravity.channeledAntiGravityGranterCount++;
            motor.gravityParameters = gravity;
            gravityHeld = true;

            Vector3 foot = characterBody.footPosition;
            float groundY = foot.y;
            RaycastHit hit;
            if (Physics.Raycast(foot + Vector3.up * 0.5f, Vector3.down, out hit, 60f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                groundY = hit.point.y;
            targetFootY = Mathf.Max(foot.y, groundY + GazeTuning.LaunchHeight);
            // Leave headroom under a ceiling; never pull the Saint down.
            float bodyHeight = Mathf.Max(1f, (characterBody.corePosition.y - foot.y) * 2f);
            float rise = targetFootY - foot.y + bodyHeight + GazeTuning.CeilingHeadroom;
            if (rise > 0f && Physics.Raycast(foot + Vector3.up * bodyHeight * 0.5f, Vector3.up, out hit, rise, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                targetFootY = Mathf.Max(foot.y, hit.point.y - GazeTuning.CeilingHeadroom - bodyHeight);
            if (motor.isGrounded && motor.Motor) motor.Motor.ForceUnground(0.1f);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (endRequested) return;
            bool stepped = bodyMachine && bodyMachine.state is ArcStepState;
            if (GazeManualLifetime.StopBeforeWork(fixedAge, BeamEnd, stepped))
            {
                // Cancellation wins a shared fixed-step boundary over intake spend,
                // pulse arrival and baseline damage, including the remote server copy.
                endRequested = true;
                fuelEndReason = stepped ? GazeFuelEndReason.ArcStep : GazeFuelEndReason.Completed;
                if (fuel && NetworkServer.active) fuel.EndCast(fuelEndReason);
                if (isAuthority)
                {
                    if (stepped) KitLog.Event("GAZE_CANCELLED", "arc step");
                    outer.SetNextState(new GazeEndState());
                }
                return;
            }
            float dt = GetDeltaTime();
            if (beam) beam.Steer(GetAimRay().direction, dt, !ignited);
            if (characterBody) characterBody.SetAimTimer(1f);
            if (isAuthority)
            {
                Hover(dt);
                Lock(weapon);
                Lock(spear);
            }
            MaintainArms(dt);

            if (!ignited && fixedAge >= GazeTuning.WindupSeconds)
            {
                ignited = true;
                if (beam) beam.Ignite();
                KitLog.Event("GAZE_IGNITE");
            }
            if (ignited && NetworkServer.active && fixedAge < BeamEnd && beam) ServerTick(dt);
            if (fuel && NetworkServer.active) fuel.Tick(fixedAge, beam);

        }

        /// <summary>Claim every Special edge, even if empty/rejected, so extra stock
        /// cannot restart Gaze. The original cast hold is seeded and never auto-fires.</summary>
        public override void Update()
        {
            base.Update();
            if (!isAuthority || !inputBank) return;
            bool tap = tapEdges.Observe(inputBank.skill4.down);
            if (tap || inputBank.skill4.justPressed) inputBank.skill4.hasPressBeenClaimed = true;
            if (tap && fuel && !endRequested) fuel.RequestPulse();
        }

        private void Hover(float dt)
        {
            var motor = characterMotor;
            if (!motor || !characterBody) return;
            motor.walkSpeedPenaltyCoefficient = GazeTuning.DriftSpeedMultiplier;
            characterBody.isSprinting = false;
            motor.jumpCount = characterBody.maxJumpCount;
            if (ignited) targetFootY -= GazeTuning.SinkPerSecond * dt;
            float gain = fixedAge < GazeTuning.LaunchRiseSeconds ? 8f : 4f;
            float vy = Mathf.Clamp((targetFootY - characterBody.footPosition.y) * gain, -6f, 28f);
            var v = motor.velocity;
            motor.velocity = new Vector3(v.x, vy, v.z);
        }

        private void Lock(EntityStateMachine machine)
        {
            if (!machine) return;
            var state = machine.state;
            // A throw already in flight finishes first; the machine is locked when it returns to idle.
            if (state is GazeLockState || state is StormspearThrowState) return;
            machine.SetNextState(new GazeLockState());
        }

        /// <summary>Arms stay raised toward the crown (Open Circuit hold) while it fires.</summary>
        private void MaintainArms(float dt)
        {
            if (fixedAge < GazeTuning.WindupSeconds) return;
            var animator = GetModelAnimator();
            if (!animator || !KitAnim.HasState(animator, KitAnim.UpperBodyLayer, OpenCircuitTuning.HoldArmsState)) return;
            if (!KitAnim.UpperBodyIdle(characterBody)) { armsRest = 0f; return; }
            armsRest += dt;
            if (armsRest < 0.1f) return;
            armsRest = 0f;
            KitAnim.PlayGesture(characterBody, animator, OpenCircuitTuning.HoldArmsState, OpenCircuitTuning.HoldLoopSeconds);
        }

        private void ServerTick(float dt)
        {
            float progress = Mathf.Clamp01((fixedAge - GazeTuning.WindupSeconds) / Mathf.Max(0.1f, beamDuration));
            GazeServer.Reach = Mathf.Lerp(GazeTuning.ReachStart, GazeTuning.ReachEnd, progress);
            Vector3 origin = beam.Origin;
            Vector3 direction = beam.Direction;
            tickTimer -= dt;
            if (tickTimer <= 0f)
            {
                tickTimer += Mathf.Max(0.05f, GazeTuning.TickSeconds / Mathf.Max(0.1f, attackSpeedStat));
                GazeServer.CoreTick(characterBody, origin, direction, GazeTuning.DamagePerSecond * GazeTuning.TickSeconds);
            }
            forkTimer -= dt;
            if (forkTimer <= 0f)
            {
                forkTimer += Mathf.Max(0.1f, GazeTuning.ForkInterval);
                GazeServer.Forks(characterBody, GazeServer.Trace(origin, direction));
            }
        }

        public override void OnExit()
        {
            if (fuel && NetworkServer.active)
            {
                if (!characterBody || !characterBody.healthComponent || !characterBody.healthComponent.alive)
                    fuelEndReason = GazeFuelEndReason.Death;
                else if (fixedAge >= BeamEnd) fuelEndReason = GazeFuelEndReason.Completed;
                fuel.EndCast(fuelEndReason);
            }
            if (gravityHeld && characterMotor)
            {
                var gravity = characterMotor.gravityParameters;
                gravity.channeledAntiGravityGranterCount = Mathf.Max(0, gravity.channeledAntiGravityGranterCount - 1);
                characterMotor.gravityParameters = gravity;
                gravityHeld = false;
            }
            if (isAuthority && characterMotor) characterMotor.walkSpeedPenaltyCoefficient = 1f;
            if (beam) beam.End();
            CrownGestureFlow.Cancel(characterBody);
            if (armored && NetworkServer.active && characterBody && GazeArmor.Def) characterBody.RemoveBuff(GazeArmor.Def);
            armored = false;
            GazeFallGuard.Release(characterBody);
            base.OnExit();
        }

        // Frozen prevents a later Special tap from spending another stock/restarting
        // the state. Fuel taps are claimed above; Arc Step and natural expiry end it.
        public override InterruptPriority GetMinimumInterruptPriority() { return InterruptPriority.Frozen; }
    }

    /// <summary>The crown returns to the head. Separate state so an authority-side early end
    /// (Arc Step) reaches every machine through the networked transition.</summary>
    public class GazeEndState : BaseState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            CrownGestureFlow.Recover(characterBody);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && fixedAge >= GazeTuning.EndSeconds) outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() { return InterruptPriority.Skill; }
    }
}
