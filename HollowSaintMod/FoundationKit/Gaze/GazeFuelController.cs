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
    internal sealed class GazeFuelController : MonoBehaviour
    {
        private static uint nextCast;
        private CharacterBody body;
        private DischargeMeter meter;
        private GazeEmpowermentFx presentation;
        private readonly GazeFuelLedger ledger = new GazeFuelLedger();
        private readonly GazeFuelSchedule schedule = new GazeFuelSchedule();
        private readonly GazeFuelPulse[] pulses = new GazeFuelPulse[GazeFuelSchedule.MaxPhases];
        private readonly RaycastHit[] traceScratch = new RaycastHit[128];
        private readonly GazeFuelSequence receiver = new GazeFuelSequence();
        private readonly GazeManualRequestPolicy requests = new GazeManualRequestPolicy();
        private uint cast, sequence, clientCast, clientRequestSequence;
        private float age, presentationRelease, beamDuration, frozenBeamDuration, nextClientRequest;
        private bool presentationOwned;
        private GazeState activeState;
        private GazeState localState, acknowledgedState;
        private int clientAvailableEntry, clientEntryCapacity, clientPendingIntakes;
        private float clientBeamDuration, clientProgressionDuration;
        internal int AvailableEntry => NetworkServer.active && ledger.Active ?
            Mathf.Max(0, ledger.Unspent - schedule.PendingCount) : clientAvailableEntry;
        internal int EntryCapacity => NetworkServer.active && ledger.Active ? ledger.Capacity : Mathf.Max(2, clientEntryCapacity);
        internal bool PulseRequestReady => Time.unscaledTime >= nextClientRequest;
        internal bool CanAdmitPulse(float castAge, float actualEnd) => GazeLaunchDurationPolicy.CanAdmit(castAge, actualEnd,
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
            }
            acknowledgedState = clientCast != 0 ? state : null;
            if (acknowledgedState != null)
            {
                state.ApplyServerBaseline(clientProgressionDuration);
                state.ApplyServerDuration(clientBeamDuration);
            }
            clientRequestSequence = 0;
            nextClientRequest = 0f;
        }

        internal void EndLocalCast(GazeState state)
        {
            if (localState != state) return;
            localState = null;
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
            frozenBeamDuration = beamDuration = duration;
            meter.ClaimGazeFuel(ledger);
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
            int phase;
            while (beam && schedule.TakeLaunch(age, out phase))
            {
                // A fixed-step hitch may defer an acknowledged intake. Keep its
                // entry orb refundable instead of spending into an impossible arrival.
                if (!GazeLaunchDurationPolicy.CanLaunch(age, GazeTuning.WindupSeconds + beamDuration, GazeTuning.WindupSeconds))
                {
                    KitLog.Event("GAZE_FUEL_LATE_INTAKE", "orb=" + phase);
                    continue;
                }
                if (!ledger.TrySpend(1)) continue;
                beamDuration = GazeLaunchDurationPolicy.Duration(frozenBeamDuration, ledger.Spent);
                // Earned duration changes before any damage or cosmetic registration.
                // The existing reliable Launch event acknowledges it to all peers.
                if (activeState != null) activeState.ApplyServerDuration(beamDuration);
                if (beam) beam.SetBeamDuration(beamDuration);
                float radius = GazeFuelSchedule.SpreadRadius(beamAge, frozenBeamDuration,
                    GazeTuning.ForkRange, GazeTuning.ReachStart, GazeTuning.ReachEnd);
                var pulse = pulses[phase];
                pulse.Launch(body, beam.Origin, beam.Direction, phase, 1, ledger.Capacity, age, radius, traceScratch);
                var packet = Packet(GazeFuelTransport.Kind.Launch);
                packet.phase = (byte)phase; packet.origin = pulse.Origin; packet.impact = pulse.Impact;
                packet.groundPoint = pulse.Ground; packet.normal = pulse.Normal; packet.ground = pulse.HasGround;
                packet.radius = pulse.Radius; packet.travel = pulse.Travel; packet.spread = GazeFuelSchedule.SpreadDuration;
                packet.full = false; // Prototype deliberately has no finale bonus/presentation.
                packet.beamDuration = beamDuration;
                Send(packet);
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

        internal void Receive(GazeFuelTransport.Packet packet)
        {
            if (!isActiveAndEnabled || !body || !body.healthComponent || !body.healthComponent.alive) return;
            if (!receiver.Accept(packet.cast, packet.sequence, packet.kind == GazeFuelTransport.Kind.Begin,
                packet.kind == GazeFuelTransport.Kind.End)) return;
            if (packet.kind == GazeFuelTransport.Kind.Begin)
            {
                presentationOwned = true;
                clientCast = packet.cast;
                clientAvailableEntry = packet.count;
                clientEntryCapacity = packet.capacity;
                clientPendingIntakes = 0;
                acknowledgedState = localState;
                if (GazeDurationPolicy.ValidSnapshot(packet.beamDuration))
                {
                    clientProgressionDuration = packet.beamDuration;
                    var beam = GetComponent<GazeBeam>();
                    if (beam) beam.SetProgressionDuration(packet.beamDuration);
                    var machine = EntityStateMachine.FindByCustomName(gameObject, KitRegistration.CrownMachineName);
                    var state = machine ? machine.state as GazeState : null;
                    if (state != null) state.ApplyServerBaseline(packet.beamDuration);
                }
            }
            bool durationValid = packet.kind == GazeFuelTransport.Kind.Begin ?
                GazeDurationPolicy.ValidSnapshot(packet.beamDuration) : GazeLaunchDurationPolicy.ValidActualDuration(packet.beamDuration);
            if ((packet.kind == GazeFuelTransport.Kind.Begin || packet.kind == GazeFuelTransport.Kind.Launch) && durationValid)
            {
                clientBeamDuration = packet.beamDuration;
                var beam = GetComponent<GazeBeam>();
                if (beam) beam.SetBeamDuration(packet.beamDuration);
                var machine = EntityStateMachine.FindByCustomName(gameObject, KitRegistration.CrownMachineName);
                var state = machine ? machine.state as GazeState : null;
                if (state != null) state.ApplyServerDuration(packet.beamDuration);
            }
            if (packet.kind == GazeFuelTransport.Kind.Swallow)
            {
                clientAvailableEntry = Mathf.Max(0, clientAvailableEntry - packet.count);
                clientPendingIntakes += packet.count;
            }
            if (packet.kind == GazeFuelTransport.Kind.Launch)
            {
                clientPendingIntakes = Mathf.Max(0, clientPendingIntakes - 1);
            }
            if (packet.kind == GazeFuelTransport.Kind.End)
            {
                presentationOwned = false;
                clientCast = 0;
                clientAvailableEntry = 0;
                clientPendingIntakes = 0;
                acknowledgedState = null;
                presentationRelease = Time.time + GazeTuning.EndSeconds;
            }
            try
            {
                if (!presentation) presentation = GetComponent<GazeEmpowermentFx>() ?? gameObject.AddComponent<GazeEmpowermentFx>();
                float eventAge = GazeFuelTransport.EventAge(packet);
                switch (packet.kind)
                {
                    case GazeFuelTransport.Kind.Begin: presentation.BeginCast(packet.cast, packet.count, packet.capacity, packet.full, eventAge); break;
                    case GazeFuelTransport.Kind.Swallow:
                        for (int i = 0; i < packet.count; i++) presentation.Swallow(packet.cast, (int)packet.sequence * 20 + i, packet.orbIndex + i, eventAge, packet.travel);
                        break;
                    case GazeFuelTransport.Kind.Launch: presentation.LaunchPulse(packet.cast, packet.phase, packet.origin, packet.impact,
                        packet.groundPoint, packet.normal, packet.ground, eventAge, packet.travel, packet.radius, packet.spread, packet.full); break;
                    case GazeFuelTransport.Kind.Strike: presentation.ConfirmStrike(packet.cast, (int)packet.sequence, packet.impact, eventAge); break;
                    case GazeFuelTransport.Kind.Reserve: presentation.SetReserve(packet.cast, packet.reserve); break;
                    case GazeFuelTransport.Kind.End: presentation.EndCast(packet.cast, packet.unspent, packet.reserve, packet.retained, PresentationReason((GazeFuelEndReason)packet.reason)); break;
                }
            }
            catch (System.Exception error) { GazeFuelTransport.Warn("presentation event failed", error); }
        }

        private static GazeEmpowermentFx.EndReason PresentationReason(GazeFuelEndReason reason)
        {
            switch (reason)
            {
                case GazeFuelEndReason.Completed: return GazeEmpowermentFx.EndReason.Completed;
                case GazeFuelEndReason.Death: return GazeEmpowermentFx.EndReason.Death;
                case GazeFuelEndReason.Disabled: return GazeEmpowermentFx.EndReason.Despawn;
                case GazeFuelEndReason.Interrupted: return GazeEmpowermentFx.EndReason.Interrupted;
                default: return GazeEmpowermentFx.EndReason.Cancelled;
            }
        }

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
