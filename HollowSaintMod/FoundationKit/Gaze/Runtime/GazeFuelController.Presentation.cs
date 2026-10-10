using HollowSaint.FoundationKit.Gaze.Fx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze
{
    internal sealed partial class GazeFuelController
    {
        internal void Receive(GazeFuelTransport.Packet packet)
        {
            if (!isActiveAndEnabled || !body || !body.healthComponent || !body.healthComponent.alive) return;
            if (!receiver.Accept(packet.cast, packet.sequence, packet.kind == GazeFuelTransport.Kind.Begin,
                packet.kind == GazeFuelTransport.Kind.End)) return;
            if (packet.kind == GazeFuelTransport.Kind.Begin)
            {
                presentationOwned = true;
                clientCast = packet.cast;
                releaseFeel.Begin();
                clientAvailableEntry = packet.count;
                clientEntryCapacity = packet.capacity;
                clientPendingIntakes = 0;
                ApplyReceivedRamp(0);
                acknowledgedState = localState;
                if ((GazeReleaseTuning.Enabled && packet.beamDuration == GazeReleaseTuning.BeamSeconds) || GazeDurationPolicy.ValidSnapshot(packet.beamDuration))
                {
                    clientProgressionDuration = packet.beamDuration;
                    var beam = GetComponent<GazeBeam>();
                    if (beam) beam.SetProgressionDuration(packet.beamDuration);
                    var machine = EntityStateMachine.FindByCustomName(gameObject, KitRegistration.CrownMachineName);
                    var state = machine ? machine.state as GazeState : null;
                    if (state != null) state.ApplyServerBaseline(packet.beamDuration);
                }
            }
            bool durationValid = GazeReleaseTuning.Enabled ? packet.beamDuration == GazeReleaseTuning.BeamSeconds :
                packet.kind == GazeFuelTransport.Kind.Begin ? GazeDurationPolicy.ValidSnapshot(packet.beamDuration) :
                GazeLaunchDurationPolicy.ValidActualDuration(packet.beamDuration);
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
            if (packet.kind == GazeFuelTransport.Kind.Launch && localPrimed > 0 && GazeReleaseTuning.Enabled)
                try { LocalOpeningLaunch(packet.count, packet.travel, packet.origin, packet.impact); }
                catch (System.Exception error) { localPrimed = 0; GazeFuelTransport.Warn("opening launch cue failed", error); }
            if (packet.kind == GazeFuelTransport.Kind.Launch)
            {
                clientPendingIntakes = Mathf.Max(0, clientPendingIntakes - Mathf.Max(1, packet.count));
                ApplyReceivedRamp(GazeReleaseTuning.Enabled ? 0 : packet.spent);
                // Only the added pulse cue changes. Use one electrical discharge;
                // clustered delayed launches coalesce without restarting Gaze loops.
                int strength = GazeReleaseTuning.Enabled ? Mathf.Max(1, packet.count * 2 - 1) : packet.spent;
                try { pulseAudio.Play(body.gameObject, strength); }
                catch (System.Exception error) { GazeFuelTransport.Warn("pulse launch sound failed", error); }
                try { pulseKick.Play(body, strength); }
                catch (System.Exception error) { GazeFuelTransport.Warn("pulse launch shake failed", error); }
            }
            if (packet.kind == GazeFuelTransport.Kind.End)
            {
                localPrimed = 0;
                releaseFeel.End();
                presentationOwned = false;
                clientCast = 0;
                clientAvailableEntry = 0;
                clientPendingIntakes = 0;
                ApplyReceivedRamp(0);
                acknowledgedState = null;
                presentationRelease = Time.time + GazeTuning.EndSeconds;
                clientLoaded = 0;
            }
            if (packet.kind == GazeFuelTransport.Kind.Load)
            {
                clientLoaded = packet.count;
                if (GazeReleaseTuning.Enabled)
                    try { releaseFeel.Loaded(body.gameObject, packet.count); }
                    catch (System.Exception error) { GazeFuelTransport.Warn("load cue failed", error); }
            }
            if (packet.kind == GazeFuelTransport.Kind.Strike && GazeReleaseTuning.Enabled)
                try
                {
                    if (releaseFeel.Hit(body, packet.cast, packet.phase, packet.count))
                    {
                        if (!presentation) presentation = GetComponent<GazeEmpowermentFx>() ?? gameObject.AddComponent<GazeEmpowermentFx>();
                        presentation.SurgeImpact(packet.cast, packet.impact, packet.count);
                    }
                }
                catch (System.Exception error) { GazeFuelTransport.Warn("surge hit cue failed", error); }
            try
            {
                if (!presentation) presentation = GetComponent<GazeEmpowermentFx>() ?? gameObject.AddComponent<GazeEmpowermentFx>();
                float eventAge = GazeFuelTransport.EventAge(packet);
                switch (packet.kind)
                {
                    case GazeFuelTransport.Kind.Begin:
                        presentation.BeginCast(packet.cast, packet.count, packet.capacity, packet.full, eventAge, GazeReleaseTuning.Enabled);
                        presentation.SetReserve(packet.cast, packet.reserve);
                        break;
                    case GazeFuelTransport.Kind.Swallow:
                        for (int i = 0; i < packet.count; i++) presentation.Swallow(packet.cast, (int)packet.sequence * 20 + i, packet.orbIndex + i, eventAge, packet.travel);
                        break;
                    case GazeFuelTransport.Kind.Launch: presentation.LaunchPulse(packet.cast, packet.phase, packet.origin, packet.impact,
                        packet.groundPoint, packet.normal, packet.ground, eventAge, packet.travel, packet.radius, packet.spread, packet.full, Mathf.Max(1, packet.count)); break;
                    case GazeFuelTransport.Kind.Load: presentation.SetPrepared(packet.cast, packet.count); break;
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

        private void ApplyReceivedRamp(int successfulLaunches)
        {
            // Absolute snapshots are idempotent. Cache even before local state entry,
            // but never revive the width of a crown which has already started returning.
            clientRampSteps = GazeRampPolicy.Steps(successfulLaunches);
            var beam = GetComponent<GazeBeam>();
            if (beam && (clientRampSteps == 0 || beam.Current == GazeBeam.Phase.Windup || beam.Current == GazeBeam.Phase.Beam))
                beam.SetRampSteps(clientRampSteps);
            var machine = EntityStateMachine.FindByCustomName(gameObject, KitRegistration.CrownMachineName);
            var state = machine ? machine.state as GazeState : null;
            if (state != null) state.ApplyRampSteps(clientRampSteps);
        }

    }
}
