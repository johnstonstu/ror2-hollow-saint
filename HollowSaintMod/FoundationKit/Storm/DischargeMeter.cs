using System;
using System.Collections.Generic;
using EntityStates;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit
{

    /// <summary>
    /// The Storm charge meter (Answered Prayer). The charge IS the stack count of a visible
    /// buff on the Saint (one stack per Electrocute, up to StormChargeMax), so it replicates
    /// to every client with no custom networking. Only the server mutates it. The component
    /// runs on every machine and plays the local presentation (charge tick, meter-full
    /// flourish) off stack-count changes. The name is historical: it used to be the
    /// Discharge meter, and BodyFx reads Normalized for the chest core.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DischargeMeter : MonoBehaviour
    {
        public static BuffDef ChargeBuff { get; private set; }

        private CharacterBody owner;
        private int lastSeen;
        private Gaze.GazeFuelLedger gazeFuel;
        // Historical merge marker; every bank is now stored until an explicit claim.
        public bool AutoHeldFromMerge { get; private set; }

        internal static void RegisterBuff()
        {
            if (ChargeBuff != null) return;
            // The stack count is the stored bank, up to the configured capacity.
            ChargeBuff = KitContent.MakeBuff("bdHsStormCharge",
                new Color(0.3f, 0.92f, 1f), canStack: true, isDebuff: false, hidden: false, icon: "buff_discharge_charge");
        }

        public int Charge { get { return owner != null && ChargeBuff != null ? owner.GetBuffCount(ChargeBuff) : 0; } }
        public int Capacity => Storm.StoredPrayerPolicy.Capacity(KitTuning.StormChargeMax);
        public float Normalized { get { return Mathf.Clamp01(Charge / (float)Capacity); } }
        public bool IsFull { get { return Charge >= Capacity; } }

        private void Awake()
        {
            owner = GetComponent<CharacterBody>();
        }

        /// <summary>Server only. Adds one charge. Returns true if this filled the meter.</summary>
        public bool AddCharge()
        {
            if (!NetworkServer.active || owner == null || ChargeBuff == null) return false;
            if (gazeFuel != null && gazeFuel.Active)
            {
                bool accepted = gazeFuel.TryGain();
                if (accepted)
                {
                    owner.SetBuffCount(ChargeBuff.buffIndex, gazeFuel.Reserve);
                    var driver = owner.GetComponent<Gaze.GazeFuelController>();
                    if (driver) driver.ReserveChanged();
                }
                else KitLog.Event("GAZE_FUEL_GAIN_REJECTED", "cast bank at capacity=" + gazeFuel.Capacity);
                return accepted && gazeFuel.Unspent + gazeFuel.Reserve == gazeFuel.Capacity;
            }
            if (IsFull) return false;
            owner.AddBuff(ChargeBuff);
            return IsFull;
        }

        internal void ClaimGazeFuel(Gaze.GazeFuelLedger ledger)
        {
            if (!NetworkServer.active || !owner || !ChargeBuff) return;
            ledger.Begin(Charge, KitTuning.StormChargeMax);
            gazeFuel = ledger;
            AutoHeldFromMerge = false;
            owner.SetBuffCount(ChargeBuff.buffIndex, 0);
        }

        internal int ReleaseGazeFuel(bool alive)
        {
            if (!NetworkServer.active || gazeFuel == null) return 0;
            int retained = gazeFuel.End(alive);
            AutoHeldFromMerge = gazeFuel.HoldAfterMerge;
            gazeFuel = null;
            if (owner && ChargeBuff) owner.SetBuffCount(ChargeBuff.buffIndex, retained);
            return retained;
        }

        /// <summary>Server only. Empties the meter.</summary>
        public void Consume()
        {
            if (!NetworkServer.active || owner == null || ChargeBuff == null) return;
            if (gazeFuel != null && gazeFuel.Active) return;
            AutoHeldFromMerge = false;
            owner.SetBuffCount(ChargeBuff.buffIndex, 0);
        }

        /// <summary>Called only after the initialized server spear exists. There is
        /// no authority-side optimistic spend or refund after a successful creation.</summary>
        internal bool TryClaimSpearPrayer(out int spent)
        {
            spent = 0;
            bool alive = owner && owner.isActiveAndEnabled && owner.healthComponent && owner.healthComponent.alive &&
                (!owner.master || owner.master.GetBody() == owner);
            bool reserved = gazeFuel != null && gazeFuel.Active;
            int bank = Charge;
            if (!ChargeBuff || !Storm.StoredPrayerPolicy.TrySpend(ref bank, KitTuning.StormChargeMax,
                NetworkServer.active, true, alive, reserved, out spent)) return false;
            Consume();
            return true;
        }

        private void FixedUpdate()
        {
            if (owner == null) return;
            int now = Charge;
            if (Gaze.GazeFuelController.OwnsPresentation(owner)) { lastSeen = now; return; }
            if (now == lastSeen) return;
            int max = KitTuning.StormChargeMax;
            if (now >= max && lastSeen < max)
            {
                // Full: the "call the storm" gesture, only when no skill gesture is playing.
                if (KitAnim.UpperBodyIdle(owner))
                    KitAnim.PlayOnBody(owner, KitAnim.OverlayLayer, "Meter full flourish", 1f);
                Vfx.KitFx.Local(Vfx.Beat.MeterFull, owner, Vfx.KitFx.Socket(owner, "Core"));
            }
            else if (now > lastSeen && now < max)
            {
                Vfx.KitFx.Local(Vfx.Beat.ChargeTick, owner, Vfx.KitFx.Socket(owner, "Core"));
            }
            lastSeen = now;
        }
    }
}
