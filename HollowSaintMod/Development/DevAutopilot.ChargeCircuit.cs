using System.Collections;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.HollowedOrb;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    internal sealed partial class DevAutopilot
    {
        private IEnumerator ChargeCircuitExpiry()
        {
            // Success: gathering through Circuit's end keeps fuel until release,
            // returns to the forward muzzle, and restores normal native control.
            yield return Segment("charge-circuit-expiry");
            var meter = pilot.GetComponent<DischargeMeter>();
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
            fire4 = true; yield return Wait(.2f); fire4 = false; yield return Wait(1.6f);
            yield return Wait(Mathf.Max(0f, KitTuning.OpenCircuitBuffSeconds - 2.4f));
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
            aimTarget = DummyChest(0); fire2 = true; yield return Wait(.85f);
            Shot("orb-circuit-before-expiry");
            yield return Wait(1.25f);
            ReleaseCheck(!CrownOpen() && StoredChargeState.IsGathering(pilot) && meter.Charge == 5,
                "Circuit expiry during Orb gather preserves bank and gather state");
            var aim = pilot.inputBank.aimDirection.normalized;
            var forwardPoint = OrbCastGeometry.Point(pilot, aim, 5);
            ReleaseCheck(Vector3.Distance(forwardPoint, pilot.corePosition + aim * .9f) < .05f,
                "Circuit expiry returns Orb hand/ball/muzzle geometry to forward cast");
            Shot("orb-circuit-after-expiry"); fire2 = false;
            yield return Wait(4f);
            ReleaseCheck(meter.Charge == 0 && !StoredChargeState.IsGathering(pilot),
                "Orb released after Circuit expiry spends five once and recovers");
            Shot("orb-circuit-expiry-restored");
        }
    }
}
