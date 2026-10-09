using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    /// <summary>Same native state throughout gather/release. Only an authenticated server reply commits.</summary>
    public abstract class StoredChargeState : BaseSkillState
    {
        internal abstract byte Kind { get; }
        internal bool Released { get; private set; }
        private uint cast;
        private int available, loaded, requested;
        private bool ending, awaiting, cancelRequested, refunded, rejected;
        private float nextRequest, releaseEnd, requestAt;
        private Vector3 releaseDirection;
        private DischargeMeter meter;
        private StoredChargeDriver driver;
        private StoredChargeChargeFx visual;
        private StoredChargeHover hover;
        private GenericSkill admittedSlot;
        private RoR2.Skills.SkillDef admittedDefinition;
        private float FirstChargeAt => Kind == 1 ? HollowedOrb.OrbCastFlow.FirstChargeAt : Kind == 0 && AllowsEmpty ? Thundercloud.ThundercloudSchedule.FreeCastGrace : StoredChargeCastLedger.FirstChargeAt;
        /// <summary>Orb always, Thundercloud when its free cast is enabled: an empty bank still casts.</summary>
        internal bool AllowsEmpty => Kind == 1 || (Kind == 0 && ChargedStormTuning.CloudFreeCast);

        internal static bool IsGathering(CharacterBody body)
        {
            if (!body) return false;
            foreach (var machine in body.GetComponents<EntityStateMachine>())
                if (machine.state is StoredChargeState s && !s.Released) return true;
            return false;
        }

        internal static bool BlocksPrimary(CharacterBody body)
        {
            if (!body) return false;
            foreach (var machine in body.GetComponents<EntityStateMachine>())
                if (machine.state is StoredChargeState s && !s.Released &&
                    !(s.Kind == 1 && Stormspear.StormspearCharge.InCrown(body))) return true;
            var bank = body ? body.inputBank : null;
            var slots = body ? body.skillLocator : null;
            if (!bank || !slots) return false;
            // Native input can inspect Primary before Secondary/Special in the same tick.
            return Pending(slots.secondary, bank.skill2.down, bank.skill2.hasPressBeenClaimed) ||
                Pending(slots.special, bank.skill4.down, bank.skill4.hasPressBeenClaimed);
        }
        private static bool Pending(GenericSkill slot, bool down, bool claimed)
            => down && !claimed && slot && slot.skillDef is StoredChargeSkillDef def &&
                !(def.allowsUnchargedCast && Stormspear.StormspearCharge.InCrown(slot.characterBody)) && slot.CanExecute();

        internal static bool OtherGatherOwnsBody(CharacterBody body)
        {
            if (!body) return false;
            if (Gaze.GazeFuelController.OwnsPresentation(body)) return true;
            foreach (var machine in body.GetComponents<EntityStateMachine>())
                if (machine.state is Gaze.GazeState || machine.state is Stormspear.StormspearChargeState ||
                    machine.state is Stormspear.StormspearThrowState || machine.state is ArcStep.ArcStepState) return true;
            return IsGathering(body);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            meter = characterBody ? characterBody.GetComponent<DischargeMeter>() : null;
            driver = characterBody ? characterBody.GetComponent<StoredChargeDriver>() : null;
            if (isAuthority && driver) cast = driver.NextToken();
            if (driver) driver.RememberToken(cast);
            admittedSlot = activatorSkillSlot ? activatorSkillSlot : Kind == 1 ? skillLocator?.secondary : skillLocator?.special;
            admittedDefinition = admittedSlot ? admittedSlot.skillDef : null;
            if (meter && available == 0) available = Mathf.Clamp(meter.Charge, 0, ChargedStormTuning.CastLimit);
            if (NetworkServer.active && !Released)
            {
                if (!meter || !meter.BeginStoredCast(cast, AllowsEmpty)) { Reject(); return; }
                available = Mathf.Min(ChargedStormTuning.CastLimit, meter.StoredCastEntry);
            }
            if (Released) { releaseEnd += fixedAge; return; }
            if (characterBody) characterBody.SetAimTimer(2f);
            try { visual = StoredChargeChargeFx.Begin(characterBody, Kind, available); }
            catch (System.Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_STORED_CHARGE_FX " + error); }
            if (Kind == 0) { hover = new StoredChargeHover(); hover.Begin(characterBody, isAuthority, GetAimRay().direction); }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (ending) { if (isAuthority) outer.SetNextStateToMain(); return; }
            if (!characterBody || !characterBody.healthComponent || !characterBody.healthComponent.alive) return;
            if (Released)
            {
                if (isAuthority && fixedAge >= releaseEnd) outer.SetNextStateToMain();
                return;
            }
            int gathered = awaiting ? loaded : StoredChargeCastLedger.Gathered(fixedAge, available, FirstChargeAt);
            while (loaded < gathered) { loaded++; if (visual) visual.Gather(loaded); }
            if (hover != null) hover.Tick(characterBody, isAuthority, fixedAge);
            if (characterBody) characterBody.SetAimTimer(1f);
            if (!isAuthority) return;
            if (!awaiting)
            {
                bool down = inputBank && (Kind == 1 ? inputBank.skill2.down : inputBank.skill4.down);
                float max = FirstChargeAt + Mathf.Max(0, available - 1) * StoredChargeCastLedger.ChargeInterval + 1.2f;
                if (!down || fixedAge > Mathf.Max(4f, max)) BeginRequest(false);
            }
            if (awaiting && fixedAge >= nextRequest)
            {
                nextRequest = fixedAge + .15f;
                StoredChargeTransport.Request(characterBody, new StoredChargeTransport.Packet
                { cast = cast, kind = Kind, count = (byte)requested, cancel = cancelRequested, direction = releaseDirection });
                if (!Released && !ending && fixedAge - requestAt > 3f)
                {
                    Plugin.Log.LogWarning("HOLLOW_SAINT_STORED_CHARGE_TIMEOUT cast=" + cast);
                    ending = true;
                }
            }
        }

        public override void Update()
        {
            base.Update();
            if (!isAuthority || !inputBank || Released || ending) return;
            if (Kind != 1 || !Stormspear.StormspearCharge.InCrown(characterBody)) inputBank.skill1.hasPressBeenClaimed = true;
            if (Kind != 1) inputBank.skill2.hasPressBeenClaimed = true;
            else inputBank.skill4.hasPressBeenClaimed = true;
            if (inputBank.skill3.justPressed && !awaiting)
            {
                inputBank.skill3.hasPressBeenClaimed = true;
                BeginRequest(true);
                StoredChargeUtilityExit.Queue(characterBody);
            }
        }
        private void BeginRequest(bool cancel)
        {
            awaiting = true; cancelRequested = cancel || (!AllowsEmpty && loaded == 0);
            if (hover != null) hover.Freeze(characterBody);
            requested = loaded; releaseDirection = GetAimRay().direction; requestAt = fixedAge;
            // Counts/aim freeze on release. Cloud completes its crown spiral;
            // the Orb uses a short minimum windup even for a quick tap.
            nextRequest = cancelRequested ? fixedAge : Kind == 1
                ? HollowedOrb.OrbCastFlow.RequestAt(fixedAge) : fixedAge + .26f;
        }

        internal void ServerRequest(StoredChargeTransport.Packet p, NetworkConnection connection, bool host)
        {
            if (!NetworkServer.active || Released || ending || p.reply || p.cast != cast || p.kind != Kind ||
                !characterBody || !characterBody.healthComponent || !characterBody.healthComponent.alive || !driver) return;
            bool authenticated = host && isAuthority;
            if (!host && connection != null && connection.isReady && characterBody.master)
            {
                var controller = characterBody.master.playerCharacterMasterController;
                var user = controller ? controller.networkUser : null;
                authenticated = user && user.connectionToClient == connection;
            }
            if (!authenticated) return;
            if (p.cancel) { Reject(); return; }
            if (!StoredChargeDriver.Finite(p.direction) || p.direction.sqrMagnitude < .1f || p.direction.sqrMagnitude > 4f) { Reject(); return; }
            if (Kind == 1 && fixedAge < HollowedOrb.OrbCastFlow.MinimumWindup) return;
            int count = Mathf.Min(p.count, StoredChargeCastLedger.Gathered(fixedAge, available, FirstChargeAt));
            if ((!AllowsEmpty && count < 1) || !driver.Prepare(Kind, count, p.direction.normalized)) { Reject(); return; }
            if (!meter || !meter.SpendStoredCast(cast, count, fixedAge, out int spent, AllowsEmpty, FirstChargeAt)) { Reject(); return; }
            // Once committed, a launch failure still confirms spending and cannot refund stock.
            var reply = new StoredChargeTransport.Packet { cast = cast, kind = Kind, count = (byte)spent, duration = .45f };
            try { reply.duration = driver.Launch(Kind, cast, spent, p.direction.normalized); }
            catch (System.Exception error)
            {
                Plugin.Log.LogError("HOLLOW_SAINT_STORED_CHARGE_LAUNCH kind=" + Kind + " cast=" + cast + " " + error);
                throw;
            }
            finally { ReceiveReply(reply); StoredChargeTransport.Reply(characterBody, reply); }
            KitLog.Event("STORED_CHARGE_CAST", "kind=" + Kind + " spent=" + spent + " remaining=" + meter.Charge);
        }
        private void Reject()
        {
            if (ending || Released) return;
            ending = true;
            if (meter) meter.CancelStoredCast(cast);
            if (NetworkServer.active && characterBody)
                StoredChargeTransport.Reply(characterBody, new StoredChargeTransport.Packet { cast = cast, kind = Kind, cancel = true });
        }
        internal void AbortForBodyLoss()
        {
            Reject();
            if (visual) visual.End(false);
            if (hover != null) { hover.End(characterBody); hover = null; }
        }
        protected virtual void OnReleased(int count) { }
        internal void ReceiveReply(StoredChargeTransport.Packet p)
        {
            if (p.cast != cast || p.kind != Kind || Released) return;
            if (p.cancel) { ending = true; rejected = true; return; }
            loaded = Mathf.Clamp(p.count, 0, ChargedStormTuning.CastLimit);
            Released = true; releaseEnd = fixedAge + Mathf.Clamp(p.duration, .3f, Kind == 0 ? 20f : 5f);
            if (visual) { visual.Confirm(loaded); visual.End(true); }
            if (hover != null) { hover.End(characterBody); hover = null; }
            OnReleased(loaded);
        }
        public override void OnExit()
        {
            if (meter) meter.CancelStoredCast(cast);
            if (visual) visual.End(false);
            if (hover != null) hover.End(characterBody);
            // An unacknowledged throw may already be committed remotely. Only confirmed
            // cancellation or an exit before requesting a throw can return native stock.
            if (!Released && (!awaiting || cancelRequested || rejected) && !refunded && isAuthority && characterBody && characterBody.healthComponent && characterBody.healthComponent.alive)
            {
                refunded = true;
                var slot = admittedSlot;
                if (slot && slot.skillDef == admittedDefinition && slot.stock < slot.maxStock)
                { float clock = slot.rechargeStopwatch; slot.AddOneStock(); slot.rechargeStopwatch = clock; }
            }
            base.OnExit();
        }
        public override void OnSerialize(NetworkWriter w)
        { base.OnSerialize(w); w.Write(cast); w.Write((byte)available); w.Write(Released); w.Write(Mathf.Max(0f, releaseEnd - fixedAge)); }
        public override void OnDeserialize(NetworkReader r)
        { base.OnDeserialize(r); cast = r.ReadUInt32(); available = Mathf.Min(20, r.ReadByte()); Released = r.ReadBoolean(); releaseEnd = r.ReadSingle(); }
        public override InterruptPriority GetMinimumInterruptPriority() => Released ? InterruptPriority.Skill : InterruptPriority.PrioritySkill;
    }
}
