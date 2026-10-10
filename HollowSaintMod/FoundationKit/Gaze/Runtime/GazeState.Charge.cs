using HollowSaint.FoundationKit.OpenCircuit;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>
    /// 1.2 charge-up (Stu: "hold to activate, it charges up and consumes the charges with an
    /// animation into the crown, then release and boom, related to how many charges go in").
    /// A skill-activated GazeState starts in this phase while Special is held: stored Static
    /// Charges stream into the crown one at a time. Releasing (or holding past full) hands
    /// over to a fresh GazeState carrying the count; at ignition the server spends that many
    /// entry charges as one opening blast down the beam. Same state type throughout, so the
    /// skill's cooldown-on-end bookkeeping is unchanged. Charges are only spent by the server
    /// ledger at ignition; nothing here touches the bank.
    /// </summary>
    public partial class GazeState
    {
        internal const float ChargeFirstAt = 0.12f, ChargeEach = 0.30f, ChargeFullGrace = 1.2f, ChargeMax = 4f;
        /// <summary>Set by the charge phase; serialized to the server copy of the beam phase.</summary>
        internal int OpeningCharges;
        private bool beamPhase;          // false only for the skill-activated instance
        private bool charging;
        private int chargeAvailable, chargeAbsorbed;
        private float chargeFullAt = -1f;
        private bool chargeGravityHeld, chargeHandedOff, chargeReleasedToBeam;
        private Fx.GazeChargeUpFx chargeFx;

        internal int ChargeAbsorbed => chargeAbsorbed;

        /// <summary>True when this instance is the charge phase (handled entirely here).</summary>
        private bool BeginChargePhase()
        {
            if (beamPhase) return false;
            var meter = characterBody ? characterBody.GetComponent<DischargeMeter>() : null;
            if (meter) meter.SnapshotGazeGather();
            if (!GazeReleaseTuning.Enabled) return false;
            chargeAvailable = Mathf.Clamp(meter ? meter.Charge : 0, 0, Mathf.Max(1, KitTuning.StormChargeMax));
            if (chargeAvailable <= 0) { beamPhase = true; return false; } // nothing to absorb: straight to the beam
            charging = true;
            if (characterBody) characterBody.SetAimTimer(2f);
            KitAnim.PlayGesture(characterBody, GetModelAnimator(), OpenCircuitTuning.CastArmsState, ChargeMax);
            if (characterMotor)
            {
                var gravity = characterMotor.gravityParameters;
                gravity.channeledAntiGravityGranterCount++;
                characterMotor.gravityParameters = gravity;
                chargeGravityHeld = true;
            }
            GazeFallGuard.Hold(characterBody);
            // Standing still to gather: the channel's armor covers the charge-up too.
            if (NetworkServer.active && characterBody && GazeArmor.Def) { characterBody.AddBuff(GazeArmor.Def); armored = true; }
            try { chargeFx = Fx.GazeChargeUpFx.Begin(characterBody, chargeAvailable); }
            catch (System.Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_GAZE_CHARGE_FX " + error.Message); }
            KitLog.Event("GAZE_CHARGE_START", "available=" + chargeAvailable + " authority=" + isAuthority);
            return true;
        }

        private void ChargeFixedUpdate()
        {
            int target = fixedAge < ChargeFirstAt ? 0 :
                Mathf.Min(chargeAvailable, 1 + Mathf.FloorToInt((fixedAge - ChargeFirstAt) / ChargeEach));
            while (chargeAbsorbed < target)
            {
                chargeAbsorbed++;
                if (chargeFx) chargeFx.Absorb(chargeAbsorbed);
            }
            if (chargeAbsorbed >= chargeAvailable && chargeFullAt < 0f) chargeFullAt = fixedAge;
            if (characterBody) characterBody.SetAimTimer(1f);
            if (!isAuthority) return;
            // Hang in place: no fall, little drift, so the gather reads as a deliberate wind-up.
            if (characterMotor)
            {
                var v = characterMotor.velocity;
                characterMotor.velocity = new Vector3(v.x * 0.8f, Mathf.Max(0f, v.y * 0.6f), v.z * 0.8f);
            }
            bool held = inputBank && inputBank.skill4.down;
            bool timedOut = (chargeFullAt >= 0f && fixedAge - chargeFullAt > ChargeFullGrace) || fixedAge > ChargeMax;
            if (!held || timedOut) HandOff();
        }

        private void ChargeUpdate()
        {
            if (!isAuthority || !inputBank) return;
            // 1.3.2 (Stu): Arc Bolt keeps firing while charges feed the crown. The opening
            // count is frozen at entry, so bolts here bank charges for later.
            inputBank.skill2.hasPressBeenClaimed = true;
            // Utility backs out of the charge-up without casting the beam.
            if (inputBank.skill3.justPressed && !chargeHandedOff)
            {
                chargeHandedOff = true;
                outer.SetNextState(new GazeEndState());
            }
        }

        private void HandOff()
        {
            if (chargeHandedOff) return;
            chargeHandedOff = true;
            chargeReleasedToBeam = true;
            KitLog.Event("GAZE_CHARGE_RELEASE", "absorbed=" + chargeAbsorbed + " of " + chargeAvailable);
            outer.SetNextState(new GazeState { beamPhase = true, OpeningCharges = chargeAbsorbed });
        }

        private void ChargeExit()
        {
            if (chargeGravityHeld && characterMotor)
            {
                var gravity = characterMotor.gravityParameters;
                gravity.channeledAntiGravityGranterCount = Mathf.Max(0, gravity.channeledAntiGravityGranterCount - 1);
                characterMotor.gravityParameters = gravity;
                chargeGravityHeld = false;
            }
            // The transition guard also covers Utility cancellation; only an
            // actual beam handoff gets the release burst and firing audio tail.
            if (chargeFx) chargeFx.End(chargeAbsorbed, chargeReleasedToBeam);
            if (armored && NetworkServer.active && characterBody && GazeArmor.Def) characterBody.RemoveBuff(GazeArmor.Def);
            armored = false;
            // The beam phase re-holds the fall guard; a Utility back-out releases it here.
            GazeFallGuard.Release(characterBody);
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(beamPhase);
            writer.Write((byte)Mathf.Clamp(OpeningCharges, 0, 20));
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            beamPhase = reader.ReadBoolean();
            OpeningCharges = reader.ReadByte();
        }
    }
}
