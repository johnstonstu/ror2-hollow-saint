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
        private uint cast, sequence;
        private float age, presentationRelease;
        private bool presentationOwned;

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

        internal void BeginCast()
        {
            if (!NetworkServer.active || !body || !meter) return;
            if (ledger.Active) EndCast(GazeFuelEndReason.Interrupted);
            cast = ++nextCast;
            if (cast == 0) cast = ++nextCast;
            sequence = 0; age = 0f;
            meter.ClaimGazeFuel(ledger);
            var passive = body.GetComponent<ThunderboltDriver>();
            if (passive) passive.ClaimForGaze();
            schedule.Begin(ledger.Entry);
            var packet = Packet(GazeFuelTransport.Kind.Begin);
            packet.count = (byte)ledger.Entry; packet.capacity = (byte)ledger.Capacity;
            packet.full = ledger.Entry == ledger.Capacity;
            Send(packet);
        }

        internal void Tick(float elapsedSinceEntry, GazeBeam beam)
        {
            if (!NetworkServer.active || !ledger.Active || !body) return;
            age = elapsedSinceEntry;
            if (!body.healthComponent || !body.healthComponent.alive) { EndCast(GazeFuelEndReason.Death); return; }
            float beamAge = age - GazeTuning.WindupSeconds;
            int phase;
            while (schedule.TakeIntake(beamAge, out phase))
            {
                var packet = Packet(GazeFuelTransport.Kind.Swallow);
                packet.phase = (byte)phase; packet.count = (byte)schedule.Group(phase);
                int orbIndex = 0;
                for (int i = 0; i < phase; i++) orbIndex += schedule.Group(i);
                packet.orbIndex = (byte)orbIndex;
                packet.unspent = (byte)ledger.Unspent; packet.reserve = (byte)ledger.Reserve;
                packet.travel = GazeFuelSchedule.IntakeDuration;
                Send(packet);
            }
            while (beam && schedule.TakeLaunch(beamAge, out phase))
            {
                int group = schedule.Group(phase);
                if (!ledger.TrySpend(group)) continue;
                float radius = GazeFuelSchedule.SpreadRadius(beamAge, GazeTuning.BeamSeconds,
                    GazeTuning.ForkRange, GazeTuning.ReachStart, GazeTuning.ReachEnd);
                var pulse = pulses[phase];
                pulse.Launch(body, beam.Origin, beam.Direction, phase, group, ledger.Capacity, age, radius, traceScratch);
                var packet = Packet(GazeFuelTransport.Kind.Launch);
                packet.phase = (byte)phase; packet.origin = pulse.Origin; packet.impact = pulse.Impact;
                packet.groundPoint = pulse.Ground; packet.normal = pulse.Normal; packet.ground = pulse.HasGround;
                packet.radius = pulse.Radius; packet.travel = pulse.Travel; packet.spread = GazeFuelSchedule.SpreadDuration;
                packet.full = false; // Prototype deliberately has no finale bonus/presentation.
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
            for (int i = 0; i < pulses.Length; i++) pulses[i].Clear();
            int retained = meter.ReleaseGazeFuel(alive);
            packet.retained = (byte)retained;
            Send(packet);
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
            if (packet.kind == GazeFuelTransport.Kind.Begin) presentationOwned = true;
            if (packet.kind == GazeFuelTransport.Kind.End)
            {
                presentationOwned = false;
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
