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
            // 1.2: the first press after a charge-up fires the primed opening instead of loading.
            if (edge == GazeReleaseEdge.Begin && active && primedOpening > 0 && age >= GazeTuning.WindupSeconds)
                ServerFireOpening(age, GazeTuning.WindupSeconds + beamDuration);
            else if (edge == GazeReleaseEdge.Begin && fits) releaseHold.Begin(age, AvailableEntry);
            else if (edge == GazeReleaseEdge.Release)
            {
                int count = releaseHold.Release(age, AvailableEntry, fits);
                if (count > 0) QueueRelease(count);
            }
            else if (edge == GazeReleaseEdge.Cancel || !active) releaseHold.Cancel();
            UpdateLoaded();
        }

        private int openingPhase = -1, primedOpening, localPrimed;
        internal int LocalPrimed => localPrimed;

        /// <summary>Every machine, at ignition: the charge-up's charges sit loaded in the crown
        /// until the first Primary press fires them as one opening pulse.</summary>
        internal void LocalPrime(int count)
        {
            localPrimed = GazeReleaseTuning.Enabled ? Mathf.Clamp(count, 0, GazeReleaseTuning.MaximumOpening) : 0;
            if (localPrimed < 1) return;
            try
            {
                if (!presentation) presentation = GetComponent<Fx.GazeEmpowermentFx>() ?? gameObject.AddComponent<Fx.GazeEmpowermentFx>();
                presentation.SetPrimed(clientCast != 0 ? clientCast : cast, localPrimed);
            }
            catch (System.Exception error) { GazeFuelTransport.Warn("prime cue failed", error); }
        }

        /// <summary>Server, at ignition: hold the absorbed charges for the first Primary press.
        /// Clamped to the entry bank actually claimed for this cast.</summary>
        internal void ServerPrime(int absorbed)
        {
            openingPhase = -1; primedOpening = 0;
            if (!GazeReleaseTuning.Enabled || !NetworkServer.active || !ledger.Active) return;
            primedOpening = Mathf.Min(absorbed, Mathf.Min(AvailableEntry, GazeReleaseTuning.MaximumOpening));
            if (primedOpening > 0) KitLog.Event("GAZE_OPENING_PRIMED", "absorbed=" + absorbed + " primed=" + primedOpening);
        }

        /// <summary>Server, at ignition: the charged opening fires straight away (Stu: you aim while
        /// charging, so the big burst should land at once; RT then drives the minor surges).</summary>
        internal void ServerFireOpeningNow(float castAge, float end)
        {
            if (!GazeReleaseTuning.Enabled || !NetworkServer.active || !ledger.Active || primedOpening < 1) return;
            age = castAge;
            ServerFireOpening(castAge, end);
        }

        private bool ServerFireOpening(float castAge, float end)
        {
            int count = Mathf.Min(primedOpening, Mathf.Min(AvailableEntry, GazeReleaseTuning.MaximumOpening));
            primedOpening = 0;
            if (count < 1) return false;
            if (castAge + GazeReleaseTuning.ReleaseIntake + GazeReleaseTuning.MaximumTravel + GazeReleaseTuning.OpeningTravelExtra +
                GazeReleaseTuning.SpreadSeconds + .05f >= end) return false;
            openingPhase = schedule.Count;
            QueueRelease(count);
            KitLog.Event("GAZE_OPENING", "spent=" + count);
            return true;
        }

        /// <summary>Every machine, when the opening's Launch arrives: crown swell and recoil, the
        /// wave down the beam, shock rings racing along it, then the boom where it lands.</summary>
        private void LocalOpeningLaunch(int count, float travel, Vector3 origin, Vector3 impact)
        {
            localPrimed = 0;
            if (!body) return;
            uint id = clientCast != 0 ? clientCast : cast;
            releaseFeel.Opening(body, count);
            if (!presentation) presentation = GetComponent<Fx.GazeEmpowermentFx>() ?? gameObject.AddComponent<Fx.GazeEmpowermentFx>();
            presentation.NoteOpening(id, count);
            var beam = GetComponent<GazeBeam>();
            if (beam) beam.OpeningWave(count, travel);
            if (body.hasEffectiveAuthority && body.characterMotor && origin != impact)
                body.characterMotor.velocity += (origin - impact).normalized * (3f + 1.2f * Mathf.Min(count, 5));
            StartCoroutine(OpeningBoom(id, count, travel, origin, impact));
        }

        private System.Collections.IEnumerator OpeningBoom(uint id, int count, float travel, Vector3 origin, Vector3 impact)
        {
            float started = Time.time;
            int rings = 3 + Mathf.Min(count, 5);
            int spawned = 0;
            var palette = Vfx.SkinFxPalette.ForBody(body);
            Vector3 dir = (impact - origin).normalized;
            while (Time.time - started < travel)
            {
                float u = (Time.time - started) / Mathf.Max(.01f, travel);
                while (spawned < rings && spawned / (float)rings <= u)
                {
                    float at = (spawned + .5f) / rings;
                    try
                    {
                        Vfx.VfxParticles.Ring(Vector3.Lerp(origin, impact, at), dir, .3f, 1.0f + .25f * Mathf.Min(count, 5) + .5f * at,
                            .22f, .16f + .03f * Mathf.Min(count, 5), Vfx.VfxAssets.Trail, palette);
                    }
                    catch (System.Exception error) { GazeFuelTransport.Warn("opening ring failed", error); }
                    spawned++;
                }
                yield return null;
            }
            if (!body || !body.healthComponent || !body.healthComponent.alive) yield break;
            try
            {
                releaseFeel.PlayBoom(body, count);
                if (!presentation) presentation = GetComponent<Fx.GazeEmpowermentFx>() ?? gameObject.AddComponent<Fx.GazeEmpowermentFx>();
                presentation.SurgeImpact(id, impact, count);
            }
            catch (System.Exception error) { GazeFuelTransport.Warn("opening boom failed", error); }
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
