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
    /// leaves the head and floats out in front), then a fixed-length beam. Runs on every machine:
    /// the authority owns movement, aim and the early exits (recast, Arc Step, time up); the
    /// server deals the damage from its own copy; GazeBeam draws the same beam everywhere.
    /// </summary>
    public class GazeState : BaseSkillState
    {
        private GazeBeam beam;
        private EntityStateMachine weapon, spear, bodyMachine;
        private bool gravityHeld, ignited;
        private float targetFootY, tickTimer, forkTimer, armsRest;
        private bool padCancel, armored;

        private float BeamEnd => GazeTuning.WindupSeconds + GazeTuning.BeamSeconds;

        public override void OnEnter()
        {
            base.OnEnter();
            beam = GazeBeam.For(characterBody);
            if (beam) beam.Begin(GetAimRay().direction);
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

            if (!isAuthority) return;
            bool timeUp = fixedAge >= BeamEnd;
            bool recast = ignited && fixedAge >= GazeTuning.WindupSeconds + GazeTuning.RecastGraceSeconds &&
                inputBank && inputBank.skill4.justPressed;
            bool stepped = bodyMachine && bodyMachine.state is ArcStepState;
            bool pad = padCancel && fixedAge >= 0.25f;
            padCancel = false;
            if (timeUp || recast || stepped || pad)
            {
                if (recast || stepped || pad) KitLog.Event("GAZE_CANCELLED", recast ? "recast" : pad ? "pad B" : "arc step");
                // v0.9.9: the recast press is spent ending the beam, so a spare stock (Lysate Cell)
                // does not also launch a new Gaze from the same press.
                if (recast) inputBank.skill4.hasPressBeenClaimed = true;
                outer.SetNextState(new GazeEndState());
            }
        }

        /// <summary>Per frame (button presses are per frame): latch B on a gamepad for FixedUpdate.</summary>
        public override void Update()
        {
            base.Update();
            if (isAuthority && GazeTuning.PadCancel && !padCancel && PadCancelPressed()) padCancel = true;
        }

        /// <summary>The "B" face button (Circle on PlayStation) of this player's gamepads, read through
        /// Rewired's gamepad template so it is the same physical button on every pad.</summary>
        private bool PadCancelPressed()
        {
            try
            {
                var master = characterBody ? characterBody.master : null;
                var pcmc = master ? master.playerCharacterMasterController : null;
                var user = pcmc && pcmc.networkUser ? pcmc.networkUser.localUser : null;
                var player = user != null ? user.inputPlayer : null;
                if (player == null) return false;
                foreach (var joystick in player.controllers.Joysticks)
                {
                    var pad = joystick != null ? joystick.GetTemplate<Rewired.IGamepadTemplate>() : null;
                    if (pad != null && pad.actionBottomRow2 != null && pad.actionBottomRow2.justPressed) return true;
                }
            }
            catch (System.Exception error)
            {
                if (!warnedPad) { warnedPad = true; Plugin.Log.LogWarning("HOLLOW_SAINT_GAZE_PAD_CANCEL " + error.Message); }
            }
            return false;
        }
        private static bool warnedPad;

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
            float progress = Mathf.Clamp01((fixedAge - GazeTuning.WindupSeconds) / Mathf.Max(0.1f, GazeTuning.BeamSeconds));
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

        // v0.9.9: was PrioritySkill, which let the Gaze skill interrupt itself whenever a spare stock
        // existed (Lysate Cell): the recast press restarted the beam and spent the stock instead of
        // ending it. Only death/freeze may cut it now; recast and Arc Step end it from FixedUpdate.
        public override InterruptPriority GetMinimumInterruptPriority() { return InterruptPriority.Frozen; }
    }

    /// <summary>The crown returns to the head. Separate state so an authority-side early end
    /// (recast, Arc Step) reaches every machine through the networked transition.</summary>
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
