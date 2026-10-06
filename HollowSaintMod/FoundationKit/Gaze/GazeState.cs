using EntityStates;
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
    /// the authority owns movement and aim; a native Primary override requests discrete fuel taps. The
    /// server deals the damage from its own copy; GazeBeam draws the same beam everywhere.
    /// </summary>
    public partial class GazeState : BaseSkillState
    {
        private GazeBeam beam;
        private EntityStateMachine weapon, spear;
        private bool gravityHeld, ignited;
        private float targetFootY, tickTimer, forkTimer, armsRest;
        private bool armored;
        private bool endRequested;
        private float beamDuration, progressionDuration;
        private int rampSteps;
        private GazeSkillOverrides controls;
        private GazeFuelController fuel;
        private GazeFuelEndReason fuelEndReason = GazeFuelEndReason.Interrupted;
        private readonly GazeExitEdges exitEdges = new GazeExitEdges();
        private readonly GazeMappedCancel mappedCancel = new GazeMappedCancel();
        private bool utilityExit;

        private float BeamEnd => GazeTuning.WindupSeconds + beamDuration;
        // EntityState.fixedAge is protected in the real engine. Read it legally
        // inside this subclass rather than through the publicized build reference.
        internal float AuthoritativeCastAge => fixedAge;
        internal float RemainingBeamSeconds => Mathf.Max(0f, BeamEnd - fixedAge);
        internal float ActualBeamSeconds => beamDuration;
        internal int SuccessfulLaunches => rampSteps;
        internal bool TimerVisible => ignited && !endRequested;
        internal bool FuelAdmissionOpen => !endRequested && !GazeManualLifetime.StopBeforeWork(fixedAge, BeamEnd);
        internal bool PrimaryPulseReady => FuelAdmissionOpen && fixedAge >= GazeTuning.WindupSeconds &&
            fuel && fuel.CanAdmitPulse(fixedAge, BeamEnd);
        internal void ApplyServerDuration(float duration)
        {
            if (!GazeLaunchDurationPolicy.ValidActualDuration(duration)) return;
            beamDuration = duration;
            if (beam) beam.SetBeamDuration(duration);
        }
        internal void ApplyServerBaseline(float duration)
        {
            if (!(GazeReleaseTuning.Enabled && duration == GazeReleaseTuning.BeamSeconds) && !GazeDurationPolicy.ValidSnapshot(duration)) return;
            progressionDuration = duration;
            if (beam) beam.SetProgressionDuration(duration);
        }
        internal void ApplyRampSteps(int successfulLaunches)
        {
            if (endRequested) return;
            rampSteps = GazeRampPolicy.Steps(successfulLaunches);
            if (beam) beam.SetRampSteps(rampSteps);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            if (BeginChargePhase()) return;
            exitEdges.Begin(inputBank && inputBank.skill4.down, inputBank && inputBank.skill3.down);
            mappedCancel.Begin(characterBody);
            rampSteps = 0;
            ClaimOtherCombat();
            progressionDuration = beamDuration = GazeReleaseTuning.Enabled ? GazeReleaseTuning.BeamSeconds :
                GazeDurationPolicy.ForLevel(GazeTuning.BeamSeconds, characterBody ? characterBody.level : 1f);
            beam = GazeBeam.For(characterBody);
            if (beam) beam.Begin(GetAimRay().direction, beamDuration);
            fuel = characterBody ? characterBody.GetComponent<GazeFuelController>() : null;
            if (fuel) fuel.BeginLocalCast(this);
            if (fuel && NetworkServer.active) fuel.BeginCast(this, beamDuration);
            controls = GazeSkillOverrides.For(characterBody);
            if (controls) controls.Begin(this, fuel);
            GazeFallGuard.Hold(characterBody);
            weapon = EntityStateMachine.FindByCustomName(gameObject, "Weapon");
            spear = EntityStateMachine.FindByCustomName(gameObject, StormspearRegistration.MachineName);
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
            if (charging) { ChargeFixedUpdate(); return; }
            if (endRequested) return;
            if (GazeManualLifetime.StopBeforeWork(fixedAge, BeamEnd))
            {
                // Cancellation wins a shared fixed-step boundary over intake spend,
                // pulse arrival and baseline damage, including the remote server copy.
                endRequested = true;
                fuelEndReason = GazeFuelEndReason.Completed;
                if (fuel && NetworkServer.active) fuel.EndCast(fuelEndReason);
                if (isAuthority)
                {
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
                // 1.2 charge-up: the absorbed charges open the beam as one big blast.
                // 1.2 charge-up: the absorbed charges wait in the crown for the first Primary press.
                if (NetworkServer.active && fuel && OpeningCharges > 0) fuel.ServerPrime(OpeningCharges);
                if (fuel && OpeningCharges > 0) fuel.LocalPrime(OpeningCharges);
                KitLog.Event("GAZE_IGNITE");
            }
            if (ignited && NetworkServer.active && fixedAge < BeamEnd && beam) ServerTick(dt);
            if (fuel && NetworkServer.active) fuel.Tick(fixedAge, beam);

        }

        /// <summary>The native Primary override owns pulse execution. Other combat
        /// presses are claimed even when empty; movement, jump and aim remain available.</summary>
        public override void Update()
        {
            base.Update();
            if (charging) { ChargeUpdate(); return; }
            if (!isAuthority || !inputBank) return;
            var exit = exitEdges.Observe(inputBank.skill4.down, inputBank.skill3.down);
            bool cancel = mappedCancel.Observe(characterBody);
            if (!endRequested && (exit != GazeExitAction.None || cancel))
            {
                endRequested = true;
                utilityExit = exit == GazeExitAction.Utility;
                fuelEndReason = GazeFuelEndReason.Interrupted;
                outer.SetNextState(new GazeEndState());
            }
            if (GazeReleaseTuning.Enabled && fuel)
                fuel.ObserveReleaseInput(inputBank.skill1.down, PrimaryPulseReady,
                    !endRequested && controls && controls.OwnsPrimary);
            else if (controls) controls.ObservePrimary();
            ClaimOtherCombat();
            // A higher external override must not become another combat action.
            if (!controls || !controls.OwnsPrimary) inputBank.skill1.hasPressBeenClaimed = true;
        }
        private void ClaimOtherCombat()
        {
            if (!isAuthority || !inputBank) return;
            inputBank.skill2.hasPressBeenClaimed = true;
            inputBank.skill3.hasPressBeenClaimed = true;
            inputBank.skill4.hasPressBeenClaimed = true;
        }

        private void Hover(float dt)
        {
            var motor = characterMotor;
            if (!motor || !characterBody) return;
            motor.walkSpeedPenaltyCoefficient = GazeTuning.DriftSpeedMultiplier * (GazeReleaseTuning.Enabled ? 1.1f : 1f);
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
            float progress = GazeLaunchDurationPolicy.Progress(fixedAge - GazeTuning.WindupSeconds, progressionDuration);
            GazeServer.Reach = Mathf.Lerp(GazeTuning.ReachStart, GazeTuning.ReachEnd, progress);
            Vector3 origin = beam.Origin;
            Vector3 direction = beam.Direction;
            tickTimer -= dt;
            if (tickTimer <= 0f)
            {
                tickTimer += Mathf.Max(0.05f, GazeTuning.TickSeconds / Mathf.Max(0.1f, attackSpeedStat));
                GazeServer.CoreTick(characterBody, origin, direction,
                    GazeTuning.DamagePerSecond * GazeTuning.TickSeconds * GazeRampPolicy.DamageMultiplier(rampSteps));
            }
            forkTimer -= dt;
            if (forkTimer <= 0f)
            {
                forkTimer += Mathf.Max(0.1f, GazeTuning.ForkInterval);
                GazeServer.Forks(characterBody, GazeServer.Trace(origin, direction), GazeRampPolicy.DamageMultiplier(rampSteps));
            }
        }

        public override void OnExit()
        {
            if (charging) { ChargeExit(); base.OnExit(); return; }
            rampSteps = 0;
            if (controls) controls.End();
            if (fuel && NetworkServer.active)
            {
                if (!characterBody || !characterBody.healthComponent || !characterBody.healthComponent.alive)
                    fuelEndReason = GazeFuelEndReason.Death;
                else if (fixedAge >= BeamEnd) fuelEndReason = GazeFuelEndReason.Completed;
                fuel.EndCast(fuelEndReason);
            }
            if (fuel) fuel.EndLocalCast(this);
            if (gravityHeld && characterMotor)
            {
                var gravity = characterMotor.gravityParameters;
                gravity.channeledAntiGravityGranterCount = Mathf.Max(0, gravity.channeledAntiGravityGranterCount - 1);
                characterMotor.gravityParameters = gravity;
                gravityHeld = false;
            }
            if (isAuthority && characterMotor) characterMotor.walkSpeedPenaltyCoefficient = 1f;
            if (isAuthority)
            {
                if (weapon && weapon.state is GazeLockState) weapon.SetNextStateToMain();
                if (spear && spear.state is GazeLockState) spear.SetNextStateToMain();
            }
            if (beam) beam.End();
            CrownGestureFlow.Cancel(characterBody);
            if (armored && NetworkServer.active && characterBody && GazeArmor.Def) characterBody.RemoveBuff(GazeArmor.Def);
            armored = false;
            GazeFallGuard.Release(characterBody);
            if (utilityExit && isAuthority) GazeUtilityExit.Queue(characterBody);
            base.OnExit();
        }

        // Native hurt/disable transitions still invoke OnExit and restore all slots.
        public override InterruptPriority GetMinimumInterruptPriority() { return InterruptPriority.Frozen; }
    }

    /// <summary>The crown returns to the head through a networked transition.</summary>
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
