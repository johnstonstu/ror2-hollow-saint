using HollowSaint.FoundationKit.Gaze.Fx;
using HollowSaint.FoundationKit.Storm;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>Per-body server resource ownership and bounded scheduling. Every exit
    /// stops pending fueled damage. Launch is the spend boundary; cancellation refunds
    /// only entry fuel which has not launched, merged with separately earned reserve.</summary>
    [DisallowMultipleComponent]
    internal sealed partial class GazeFuelController : MonoBehaviour
    {
        private static uint nextCast;
        private CharacterBody body;
        private DischargeMeter meter;
        private GazeEmpowermentFx presentation;
        private readonly GazeFuelLedger ledger = new GazeFuelLedger();
        private readonly GazeRecoveryBudget recovery = new GazeRecoveryBudget();
        private readonly GazeFuelSchedule schedule = new GazeFuelSchedule();
        private readonly GazeFuelPulse[] pulses = new GazeFuelPulse[GazeFuelSchedule.MaxPhases];
        private readonly RaycastHit[] traceScratch = new RaycastHit[128];
        private readonly GazeFuelSequence receiver = new GazeFuelSequence();
        private readonly GazeManualRequestPolicy requests = new GazeManualRequestPolicy();
        private readonly GazePulseAudio pulseAudio = new GazePulseAudio();
        private readonly GazePulseKick pulseKick = new GazePulseKick();
        private uint cast, sequence, clientCast, clientRequestSequence;
        private float age, presentationRelease, beamDuration, frozenBeamDuration, nextClientRequest;
        private bool presentationOwned;
        private GazeState activeState;
        private GazeState localState, acknowledgedState;
        private int clientAvailableEntry, clientEntryCapacity, clientPendingIntakes, clientRampSteps;
        private float clientBeamDuration, clientProgressionDuration;
        internal int AvailableEntry => NetworkServer.active && ledger.Active ?
            Mathf.Max(0, ledger.Unspent - schedule.PendingCount) : clientAvailableEntry;
        internal int EntryCapacity => NetworkServer.active && ledger.Active ? ledger.Capacity : Mathf.Max(2, clientEntryCapacity);
        internal bool PulseRequestReady => Time.unscaledTime >= nextClientRequest &&
            (!GazeReleaseTuning.Enabled || !NetworkServer.active || releaseHold.Ready(age));
        internal bool CanAdmitPulse(float castAge, float actualEnd) => GazeReleaseTuning.Enabled ?
            GazeReleaseTuning.ArrivalFits(castAge, actualEnd) : GazeLaunchDurationPolicy.CanAdmit(castAge, actualEnd,
            GazeTuning.WindupSeconds,
            NetworkServer.active ? schedule.PendingCount : clientPendingIntakes);

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
            meter = GetComponent<DischargeMeter>();
            for (int i = 0; i < pulses.Length; i++) pulses[i] = new GazeFuelPulse();
        }

        internal static bool OwnsPresentation(CharacterBody owner)
        {
            var driver = owner ? owner.GetComponent<GazeFuelController>() : null;
            if (!driver) return false;
            var beam = owner.GetComponent<GazeBeam>();
            return driver.ledger.Active || driver.presentationOwned || Time.time < driver.presentationRelease ||
                (beam && beam.Current != GazeBeam.Phase.Idle);
        }

        internal void BeginLocalCast(GazeState state)
        {
            localState = state;
            // The reliable Begin channel can arrive before Crown's state transition.
            // Keep that one accepted, unbound ACK rather than erasing its cast/count.
            if (acknowledgedState != null)
            {
                clientCast = 0;
                clientAvailableEntry = 0;
                clientRampSteps = 0;
            }
            acknowledgedState = clientCast != 0 ? state : null;
            if (acknowledgedState != null)
            {
                state.ApplyServerBaseline(clientProgressionDuration);
                state.ApplyServerDuration(clientBeamDuration);
                state.ApplyRampSteps(clientRampSteps);
            }
            clientRequestSequence = 0;
            nextClientRequest = 0f;
            releaseInput.Begin(body && body.inputBank && body.inputBank.skill1.down);
            clientLoaded = 0;
        }

        internal void EndLocalCast(GazeState state)
        {
            if (localState != state) return;
            localState = null;
            clientRampSteps = 0;
            if (acknowledgedState == state)
            {
                clientAvailableEntry = 0;
                // Retain the binding until reliable End so it can finish the crown
                // return. A future Begin replaces it; BeginLocal discards a bound ACK.
            }
        }

        internal void BeginCast(GazeState state, float duration)
        {
            if (!NetworkServer.active || !body || !meter) return;
            if (ledger.Active) EndCast(GazeFuelEndReason.Interrupted);
            cast = ++nextCast;
            if (cast == 0) cast = ++nextCast;
            sequence = 0; age = 0f;
            activeState = state;
            releaseHold.Reset(); lastReleaseSequence = 0; lastLoaded = 0;
            activeState.ApplyRampSteps(0);
            frozenBeamDuration = beamDuration = duration;
            meter.ClaimGazeFuel(ledger);
            recovery.Begin(body.healthComponent ? body.healthComponent.fullHealth : 0f, ledger.Capacity, ledger.Entry);
            var passive = body.GetComponent<ThunderboltDriver>();
            if (passive) passive.ClaimForGaze();
            schedule.Begin(ledger.Entry);
            requests.Begin(cast);
            var packet = Packet(GazeFuelTransport.Kind.Begin);
            packet.count = (byte)ledger.Entry; packet.capacity = (byte)ledger.Capacity;
            packet.full = ledger.Entry == ledger.Capacity;
            packet.beamDuration = beamDuration;
            Send(packet);
        }

        /// <summary>Input edges request admission only; no client fuel or visuals
        /// change until the server acknowledges intake.</summary>
        internal void RequestPulse()
        {
            if (GazeReleaseTuning.Enabled) return;
            if (!body || !body.hasEffectiveAuthority || Time.unscaledTime < nextClientRequest) return;
            nextClientRequest = Time.unscaledTime + GazeManualRequestPolicy.MinimumInterval;
            uint id = NetworkServer.active ? cast : clientCast;
            if (id == 0) return;
            uint requestSequence = ++clientRequestSequence;
            if (requestSequence == 0) requestSequence = ++clientRequestSequence;
            if (NetworkServer.active) ServerRequest(id, requestSequence, null, true);
            else GazeFuelTransport.Request(body, id, requestSequence);
        }

        internal void ServerRequest(uint id, uint requestSequence, NetworkConnection connection, bool localHost = false)
        {
            if (GazeReleaseTuning.Enabled) return;
            if (!NetworkServer.active || !body || !isActiveAndEnabled) return;
            bool authenticated = localHost && body.hasEffectiveAuthority;
            if (!localHost && connection != null && connection.isReady && body.master)
            {
                var controller = body.master.playerCharacterMasterController;
                var user = controller ? controller.networkUser : null;
                authenticated = user && user.connectionToClient == connection;
            }
            var machine = EntityStateMachine.FindByCustomName(gameObject, KitRegistration.CrownMachineName);
            bool current = ledger.Active && activeState != null && machine && machine.state == activeState &&
                activeState.FuelAdmissionOpen &&
                body.healthComponent && body.healthComponent.alive;
            float requestAge = activeState != null ? activeState.AuthoritativeCastAge : age;
            current = current && CanAdmitPulse(requestAge, GazeTuning.WindupSeconds + beamDuration);
            float prospectiveEnd = GazeLaunchDurationPolicy.ProspectiveEnd(GazeTuning.WindupSeconds + beamDuration,
                GazeTuning.WindupSeconds, schedule.PendingCount);
            if (!requests.TryAccept(id, requestSequence, authenticated, current, requestAge, GazeTuning.WindupSeconds,
                prospectiveEnd, ledger.Unspent - schedule.PendingCount)) return;
            int phase;
            if (!schedule.QueueIntake(requestAge, out phase)) return;
            age = requestAge;
            var packet = Packet(GazeFuelTransport.Kind.Swallow);
            packet.phase = packet.orbIndex = (byte)phase; packet.count = 1;
            packet.unspent = (byte)ledger.Unspent; packet.reserve = (byte)ledger.Reserve;
            packet.travel = GazeFuelSchedule.IntakeDuration;
            Send(packet);
        }

        internal void Tick(float elapsedSinceEntry, GazeBeam beam)
        {
            if (!NetworkServer.active || !ledger.Active || !body) return;
            age = elapsedSinceEntry;
            if (!body.healthComponent || !body.healthComponent.alive) { EndCast(GazeFuelEndReason.Death); return; }
            float beamAge = age - GazeTuning.WindupSeconds;
            if (age >= GazeTuning.WindupSeconds + beamDuration) { EndCast(GazeFuelEndReason.Completed); return; }
            if (GazeReleaseTuning.Enabled) UpdateLoaded();
            int phase;
            while (beam && schedule.TakeLaunch(age, out phase))
            {
                // A fixed-step hitch may defer an acknowledged intake. Keep its
                // entry orb refundable instead of spending into an impossible arrival.
                bool fits = GazeReleaseTuning.Enabled ? age + GazeFuelSchedule.MaximumTravel +
                    GazeFuelSchedule.SpreadDuration + .05f < GazeTuning.WindupSeconds + beamDuration :
                    GazeLaunchDurationPolicy.CanLaunch(age, GazeTuning.WindupSeconds + beamDuration, GazeTuning.WindupSeconds);
                if (!fits)
                {
                    KitLog.Event("GAZE_FUEL_LATE_INTAKE", "orb=" + phase);
                    continue;
                }
                int group = schedule.GroupSize(phase);
                if (!ledger.TrySpend(group)) continue;
                float heal = 0f;
                for (int spent = ledger.Spent - group + 1; spent <= ledger.Spent; spent++) heal += recovery.Claim(spent);
                // Native Heal preserves healing items, Corpsebloom and healing-disabled
                // behavior. Claim even at full health: no banked recovery or barrier farming.
                if (heal > 0f && body.healthComponent && body.healthComponent.alive &&
                    body.healthComponent.health < body.healthComponent.fullHealth)
                {
                    try
                    {
                        body.healthComponent.Heal(heal, default(ProcChainMask), true);
                        KitLog.Event("GAZE_FUEL_RECOVERY", "spent=" + ledger.Spent + " base=" + heal.ToString("0.###"));
                    }
                    catch (System.Exception error)
                    {
                        // A third-party healing callback must not lose the already
                        // spent pulse. Its base heal remains claimed, with no retry.
                        Plugin.Log.LogError("HOLLOW_SAINT_GAZE_RECOVERY_ERROR " + error);
                    }
                }
                int ramp = GazeReleaseTuning.Enabled ? 0 : ledger.Spent;
                if (activeState != null) activeState.ApplyRampSteps(ramp);
                if (beam) beam.SetRampSteps(ramp);
                beamDuration = GazeReleaseTuning.Enabled ? frozenBeamDuration : GazeLaunchDurationPolicy.Duration(frozenBeamDuration, ledger.Spent);
                // Earned duration changes before any damage or cosmetic registration.
                // The existing reliable Launch event acknowledges it to all peers.
                if (activeState != null) activeState.ApplyServerDuration(beamDuration);
                if (beam) beam.SetBeamDuration(beamDuration);
                float radius = GazeFuelSchedule.SpreadRadius(beamAge, frozenBeamDuration,
                    GazeTuning.ForkRange, GazeTuning.ReachStart, GazeTuning.ReachEnd);
                var pulse = pulses[phase];
                pulse.Launch(body, beam.Origin, beam.Direction, phase, group, ledger.Capacity, age, radius, traceScratch);
                var packet = Packet(GazeFuelTransport.Kind.Launch);
                packet.phase = (byte)phase; packet.origin = pulse.Origin; packet.impact = pulse.Impact;
                packet.groundPoint = pulse.Ground; packet.normal = pulse.Normal; packet.ground = pulse.HasGround;
                packet.radius = pulse.Radius; packet.travel = pulse.Travel; packet.spread = GazeFuelSchedule.SpreadDuration;
                packet.count = (byte)group;
                packet.full = GazeReleaseTuning.Enabled && group >= GazeReleaseTuning.MaximumLoaded;
                packet.beamDuration = beamDuration;
                packet.spent = (byte)ledger.Spent;
                Send(packet);
                KitLog.Event("GAZE_SURGE_LAUNCH", "group=" + group + " spent=" + ledger.Spent + " remaining=" + ledger.Unspent);
            }
            for (int i = 0; i < schedule.Count; i++) pulses[i].Resolve(body, age, this);
        }

        internal void ReserveChanged()
        {
            if (!ledger.Active) return;
            var packet = Packet(GazeFuelTransport.Kind.Reserve);
            packet.reserve = (byte)ledger.Reserve;
            Send(packet);
        }

        internal void ConfirmStrike(int phase, Vector3 point, float elapsed)
        {
            var packet = Packet(GazeFuelTransport.Kind.Strike);
            packet.phase = (byte)phase; packet.impact = point; packet.age = elapsed;
            Send(packet);
        }

        internal void EndCast(GazeFuelEndReason reason)
        {
            if (!NetworkServer.active || !ledger.Active) return;
            bool alive = reason != GazeFuelEndReason.Death && reason != GazeFuelEndReason.Disabled &&
                body && body.healthComponent && body.healthComponent.alive;
            var packet = Packet(GazeFuelTransport.Kind.End);
            packet.reason = (byte)reason; packet.unspent = (byte)ledger.Unspent;
            packet.reserve = (byte)ledger.Reserve; packet.spent = (byte)ledger.Spent;
            int accepted = ledger.AcceptedGains, rejected = ledger.RejectedGains, entry = ledger.Entry;
            schedule.Cancel();
            releaseHold.Reset(); clientLoaded = 0;
            recovery.Clear();
            requests.Cancel();
            for (int i = 0; i < pulses.Length; i++) pulses[i].Clear();
            int retained = meter.ReleaseGazeFuel(alive);
            packet.retained = (byte)retained;
            Send(packet);
            activeState = null;
            presentationRelease = Time.time + GazeTuning.EndSeconds;
            KitLog.Event("GAZE_FUEL_END", "cast=" + cast + " reason=" + reason + " entry=" + entry +
                " accepted=" + accepted + " rejected=" + rejected + " spent=" + packet.spent + " retained=" + retained);
        }

        private GazeFuelTransport.Packet Packet(GazeFuelTransport.Kind kind) =>
            new GazeFuelTransport.Packet { cast = cast, sequence = ++sequence, kind = kind, age = age };
        private void Send(GazeFuelTransport.Packet packet) => GazeFuelTransport.Send(body, packet);

        private void FixedUpdate()
        {
            GazeFuelTransport.Flush(body);
            if (body && body.healthComponent && body.healthComponent.alive) return;
            if (ledger.Active) EndCast(GazeFuelEndReason.Death);
            ClearPresentation();
        }
        private void ClearPresentation()
        {
            receiver.Retire(); presentationOwned = false; presentationRelease = 0f;
            clientCast = 0; clientAvailableEntry = 0;
            clientPendingIntakes = 0;
            clientLoaded = 0; releaseHold.Reset();
            ApplyReceivedRamp(0);
            localState = acknowledgedState = null;
            if (!presentation) return;
            try { presentation.Clear(); }
            catch (System.Exception error) { GazeFuelTransport.Warn("presentation clear failed", error); }
        }
        private void OnDisable()
        {
            if (ledger.Active) EndCast(GazeFuelEndReason.Disabled);
            ClearPresentation();
            GazeFuelTransport.Forget(body);
        }
    }
}
