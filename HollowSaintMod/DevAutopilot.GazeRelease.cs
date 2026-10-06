using System.Collections;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.Gaze;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    internal sealed partial class DevAutopilot
    {
        // Uses the real input bank, state machines, server controller and stock restoration.
        // Invulnerable dummies isolate entry-bank accounting from newly earned reserve.
        private IEnumerator GazeReleaseSegments()
        {
            var slot = pilot && pilot.skillLocator ? pilot.skillLocator.special : null;
            if (!slot || !GazeRegistration.SkillDef || !GazeReleaseTuning.Enabled)
            { ReleaseCheck(false, "release trial unavailable"); yield break; }
            slot.SetSkillOverride(this, GazeRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
            var originalPrimary = pilot.skillLocator.primary.skillDef;
            var fuel = pilot.GetComponent<GazeFuelController>();
            var meter = pilot.GetComponent<DischargeMeter>();
            yield return GazeCrownVisualSequence();
            for (int tier = 1; tier <= 3; tier++)
            {
                yield return Segment("gaze-release-" + tier);
                pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
                aimTarget = DummyChest(0);
                yield return Press(4); yield return Wait(1.3f);
                ReleaseCheck(fuel.AvailableEntry == 5, "entry bank five");
                fire1 = true; yield return Wait(tier == 1 ? .12f : tier == 2 ? .58f : 1.05f);
                ReleaseCheck(fuel.LoadedCharges == tier && fuel.AvailableEntry == 5, "holding tier=" + tier + " spends nothing");
                if (tier == 3)
                {
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(output, "release-loaded-hud.png"));
                    Shot("release-loaded");
                    yield return Wait(.15f);
                }
                fire1 = false; yield return Wait(.20f);
                ReleaseCheck(fuel.AvailableEntry == 5 - tier && fuel.LoadedCharges == 0, "release spends tier=" + tier);
                WideShot("release-surge-" + tier);
                yield return Wait(.8f);
                ReleaseCheck(fuel.AvailableEntry == 5 - tier, "no repeated release");
                yield return Press(4); yield return Wait(1.3f);
                ReleaseCheck(meter.Charge == 5 - tier, "unspent bank restored tier=" + tier);
                ReleaseCheck(pilot.skillLocator.primary.skillDef == originalPrimary, "primary restored");
            }
            yield return Segment("gaze-release-three-then-two");
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
            aimTarget = DummyChest(0);
            yield return Press(4); yield return Wait(1.3f);
            fire1 = true; yield return Wait(1.05f); fire1 = false; yield return Wait(.8f);
            ReleaseCheck(fuel.AvailableEntry == 2, "first batch leaves two available");
            fire1 = true; yield return Wait(.58f);
            ReleaseCheck(fuel.LoadedCharges == 2 && fuel.AvailableEntry == 2, "second batch prepares remaining two");
            fire1 = false; yield return Wait(.8f);
            ReleaseCheck(fuel.AvailableEntry == 0 && fuel.LoadedCharges == 0, "second batch spends remaining two");
            fire1 = true; yield return Wait(.6f); fire1 = false; yield return Wait(.2f);
            ReleaseCheck(fuel.AvailableEntry == 0 && fuel.LoadedCharges == 0, "empty bank hold cannot create fuel");
            yield return Press(4); yield return Wait(1.3f);
            ReleaseCheck(meter.Charge == 0, "full bank cannot return spent fuel");

            yield return Segment("gaze-release-cancel");
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
            aimTarget = DummyChest(0);
            yield return Press(4); yield return Wait(1.3f);
            fire1 = true; yield return Wait(1.05f);
            yield return Press(3); fire1 = false; yield return Wait(1.5f);
            ReleaseCheck(meter.Charge == 5, "Arc Step while preparing refunds all five");
            ReleaseCheck(pilot.skillLocator.primary.skillDef == originalPrimary, "cancel restores primary");

            yield return Segment("gaze-release-entry-held");
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
            fire1 = true; yield return Press(4); yield return Wait(1.7f);
            ReleaseCheck(fuel.LoadedCharges == 0 && fuel.AvailableEntry == 5, "held-on-entry never prepares");
            fire1 = false; yield return Wait(.2f);
            ReleaseCheck(fuel.AvailableEntry == 5, "entry-held release never fires");
            yield return Press(4); yield return Wait(1.3f);

            yield return Segment("gaze-release-expiry");
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
            yield return Press(4); yield return Wait(1.3f);
            fire1 = true; yield return Wait(7f); fire1 = false; yield return Wait(1.3f);
            ReleaseCheck(meter.Charge == 5, "expiry while holding refunds all five");
            ReleaseCheck(pilot.skillLocator.primary.skillDef == originalPrimary, "expiry restores primary");
            slot.UnsetSkillOverride(this, GazeRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
        }
        private void ReleaseCheck(bool success, string criterion)
        {
            trace.AppendLine((success ? "RELEASE_PASS " : "RELEASE_FAIL ") + criterion);
            if (!success) errors++;
        }
        private IEnumerator GazeCrownVisualSequence()
        {
            yield return Segment("gaze-crown-animation");
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
            aimTarget = DummyChest(0);
            yield return Press(4); yield return Wait(1.3f);
            var ring = FoundationKit.Vfx.HaloRing.For(pilot);
            float restingRadius = ring.Shape.Radius;
            Shot("crown-00-idle");yield return Wait(.2f);
            fire1=true;yield return Wait(.25f);Shot("crown-01-loading");
            yield return Wait(.4f);Shot("crown-02-loading");
            yield return Wait(.55f);Shot("crown-03-full");
            ReleaseCheck(ring.Shape.Radius>restingRadius*1.45f,"physical crown grows while preparing");
            trace.AppendLine("CROWN_RADIUS idle="+restingRadius+" full="+ring.Shape.Radius);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(output,"crown-no-charge-hud.png"));
            yield return Wait(.15f);fire1=false;
            yield return Wait(.12f);Shot("crown-04-release");WideShot("crown-04-wave");
            yield return Wait(.15f);Shot("crown-05-wave");
            yield return Wait(.7f);
            ReleaseCheck(ring.Shape.Radius<restingRadius*1.1f,"physical crown settles after release");
            yield return Press(4);yield return Wait(1.3f);
        }
    }
}
