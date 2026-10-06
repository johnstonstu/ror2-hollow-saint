using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Gaze
{
    internal sealed partial class GazeFuelController
    {
        private readonly GazeReleaseInput releaseInput = new GazeReleaseInput();
        private readonly GazeReleaseHold releaseHold = new GazeReleaseHold();
        private uint lastReleaseSequence;
        private int lastLoaded, clientLoaded;
        internal int LoadedCharges => NetworkServer.active && ledger.Active ?
            releaseHold.Loaded(age, AvailableEntry) : clientLoaded;

        internal void ObserveReleaseInput(bool down, bool canBegin, bool active)
        {
            if (!GazeReleaseTuning.Enabled || !body || !body.hasEffectiveAuthority) return;
            var edge = releaseInput.Observe(down, canBegin && PulseRequestReady && AvailableEntry > 0, active);
            if (edge == GazeReleaseEdge.None) return;
            uint id = NetworkServer.active ? cast : clientCast;
            if (id == 0) return;
            uint seq = ++clientRequestSequence;
            if (seq == 0) seq = ++clientRequestSequence;
            if (edge == GazeReleaseEdge.Release) nextClientRequest = Time.unscaledTime + GazeReleaseTuning.RecoverySeconds;
            if (NetworkServer.active) ServerReleaseRequest(id, seq, edge, null, true);
            else GazeFuelTransport.RequestRelease(body, id, seq, edge);
        }

        internal void ServerReleaseRequest(uint id, uint seq, GazeReleaseEdge edge,
            NetworkConnection connection, bool localHost = false)
        {
            if (!GazeReleaseTuning.Enabled || !NetworkServer.active || !body || !isActiveAndEnabled ||
                !ledger.Active || id != cast || seq == 0 || seq <= lastReleaseSequence) return;
            bool authenticated = localHost && body.hasEffectiveAuthority;
            if (!localHost && connection != null && connection.isReady && body.master)
            {
                var controller = body.master.playerCharacterMasterController;
                var user = controller ? controller.networkUser : null;
                authenticated = user && user.connectionToClient == connection;
            }
            if (!authenticated) return;
            lastReleaseSequence = seq;
            var machine = EntityStateMachine.FindByCustomName(gameObject, KitRegistration.CrownMachineName);
            var controls = GetComponent<GazeSkillOverrides>();
            bool active = activeState != null && machine && machine.state == activeState &&
                activeState.FuelAdmissionOpen && body.healthComponent && body.healthComponent.alive &&
                controls && controls.OwnsPrimary;
            age = activeState != null ? activeState.AuthoritativeCastAge : age;
            bool fits = active && age >= GazeTuning.WindupSeconds &&
                GazeReleaseTuning.ArrivalFits(age, GazeTuning.WindupSeconds + beamDuration);
            if (edge == GazeReleaseEdge.Begin && fits) releaseHold.Begin(age, AvailableEntry);
            else if (edge == GazeReleaseEdge.Release)
            {
                int count = releaseHold.Release(age, AvailableEntry, fits);
                if (count > 0) QueueRelease(count);
            }
            else if (edge == GazeReleaseEdge.Cancel || !active) releaseHold.Cancel();
            UpdateLoaded();
        }

        private void QueueRelease(int count)
        {
            int phase;
            if (!schedule.QueueIntake(age, count, GazeReleaseTuning.ReleaseIntake, out phase))
            {
                Plugin.Log.LogWarning("HOLLOW_SAINT_GAZE_RELEASE queue rejected count=" + count);
                return;
            }
            var packet = Packet(GazeFuelTransport.Kind.Swallow);
            packet.phase = (byte)phase; packet.orbIndex = (byte)schedule.OrbStart(phase);
            packet.count = (byte)count; packet.unspent = (byte)ledger.Unspent;
            packet.reserve = (byte)ledger.Reserve; packet.travel = GazeReleaseTuning.ReleaseIntake;
            Send(packet);
        }

        private void UpdateLoaded()
        {
            // Expiry releases preparation without spending. A new hold requires a fresh edge.
            if (!GazeReleaseTuning.ArrivalFits(age, GazeTuning.WindupSeconds + beamDuration)) releaseHold.Cancel();
            int loaded = releaseHold.Loaded(age, AvailableEntry);
            if (loaded == lastLoaded) return;
            lastLoaded = loaded;
            var packet = Packet(GazeFuelTransport.Kind.Load);
            packet.count = (byte)loaded;
            Send(packet);
        }
    }
}
