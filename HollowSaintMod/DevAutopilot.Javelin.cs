using System.Collections;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.SpearDischarge;
using HollowSaint.FoundationKit.Stormspear;
using RoR2;
using RoR2.CameraModes;
using UnityEngine;

namespace HollowSaint
{
    internal sealed partial class DevAutopilot
    {
        private bool javelinChecking;

        private void Update()
        {
            if (!javelinChecking || !pilot || !pilot.inputBank) return;
            Vector3 aim = Quaternion.AngleAxis(aimPitch, Right) * facing;
            pilot.inputBank.aimDirection = aim;
            // The normal camera mode also writes aim every frame. Match its public
            // look state so it agrees with the fixed input instead of overwriting it.
            foreach (var rig in CameraRigController.readOnlyInstancesList)
            {
                if (!rig || rig.targetBody != pilot || rig.cameraMode == null) continue;
                var context = new CameraModeBase.CameraModeContext();
                context.cameraInfo.cameraRigController = rig;
                var state = rig.currentCameraState;
                state.rotation = Quaternion.LookRotation(aim, Vector3.up);
                rig.cameraMode.MatchState(in context, in state);
            }
        }

        // Success (v0.9.8): after the draw settles, the fitted grip is >=0.50 arm lengths out, >=0.36 up,
        // >=0.12 back, clear of the head from the rear camera (grip >=60 px, tip >=25 px right of it), the elbow bends 45-110 degrees, and the spear projects
        // >=45 pixels long from an aim-aligned rear camera (1280x720, 60 degree FOV).
        // Use straight-ahead aim, not the offset dummies that flattered the old pose.
        private IEnumerator JavelinSegments()
        {
            javelinChecking = true;
            yield return Segment("javelin-level");
            fire2 = true;
            yield return Wait(0.12f); yield return JavelinShot("j-draw", false);
            yield return Wait(0.23f); yield return JavelinShot("j-early", true);
            yield return Wait(0.4f); yield return JavelinShot("j-third", true);
            yield return Wait(0.65f); yield return JavelinShot("j-two-thirds", true);
            yield return Wait(0.85f); yield return JavelinShot("j-full", true);
            fire1 = true;
            yield return Wait(0.65f); yield return JavelinShot("j-offhand", true);
            fire1 = false;

            // Review the actual over-shoulder whip at reduced speed, not a synthetic bone pose.
            // v0.9.3+: the spear leaves the hand at the whip apex (HandReleaseDelay 0.083 s).
            Time.timeScale = 0.1f;
            fire2 = false;
            yield return Wait(0.025f); yield return JavelinShot("j-whip", false);
            yield return Wait(0.045f); yield return JavelinShot("j-apex", false);
            yield return Wait(0.035f); yield return JavelinShot("j-release", false);
            yield return Wait(0.07f); yield return JavelinShot("j-follow", false);
            yield return Wait(0.12f); yield return JavelinShot("j-follow-late", false);
            yield return Wait(0.3f); yield return JavelinShot("j-recovered", false);
            Time.timeScale = 1f;

            // v0.9.14: one right-hand charge for comparison (the option switches live), then back.
            yield return Segment("javelin-righthand");
            bool wasLeft = SpearCarry.LeftHanded;
            SpearCarry.LeftHanded = false;
            yield return Wait(0.3f);
            fire2 = true;
            yield return Wait(2.3f); yield return JavelinShot("r-full", true);
            fire2 = false;
            yield return Wait(0.8f);
            SpearCarry.LeftHanded = wasLeft;
            yield return Wait(0.3f);

            foreach (float pitch in new[] { -40f, 40f })
            {
                string label = pitch < 0f ? "up" : "down";
                yield return Segment("javelin-" + label);
                aimPitch = pitch;
                fire2 = true;
                yield return Wait(0.4f); yield return JavelinShot("j-" + label + "-early", true);
                yield return Wait(1.85f); yield return JavelinShot("j-" + label + "-full", true);
                fire2 = false;
                yield return Wait(0.7f);
            }

            yield return Segment("javelin-moving");
            fire2 = true; move = Right;
            yield return Wait(0.8f); yield return JavelinShot("j-strafe", false);
            move = Vector3.zero; jump = true;
            yield return Wait(0.25f); jump = false;
            yield return Wait(0.15f); yield return JavelinShot("j-jump", false);
            yield return Wait(1.1f); yield return JavelinShot("j-land", true);
            fire2 = false;
            yield return Wait(0.8f);
            javelinChecking = false;
        }

        private IEnumerator JavelinShot(string name, bool checkReady)
        {
            // Freeze only after the production pose passes have run. This avoids
            // multi-camera screenshot stalls advancing the charge/throw between views.
            yield return new WaitForEndOfFrame();
            float speed = Time.timeScale;
            Time.timeScale = 0f;
            try
            {
                Vector3 aim = pilot.inputBank.aimDirection.normalized;
                Vector3 flat = Vector3.ProjectOnPlane(aim, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, flat);
                Vector3 center = pilot.corePosition + Vector3.up * 0.2f;
                float fov = shotCamera.fieldOfView;
                shotCamera.fieldOfView = 60f;
                Render(name + "_rear", center - aim * 4f, center);
                trace.AppendLine(scriptTime.ToString("000.00") + " SHOT " + name + SpearMetrics());
                if (checkReady) CheckJavelinSilhouette(name, flat, right);
                Render(name + "_side", center + right * 4f, center);
                Render(name + "_front", center + flat * 4f + right, center);
                // v0.9.14: close look at the gripping hand (finger curl on the procedural left grip).
                var gripCarry = pilot.GetComponent<SpearCarry>();
                if (gripCarry && gripCarry.HandVisible)
                {
                    Vector3 grip = gripCarry.GripPosition;
                    float s = gripCarry.Left ? -1f : 1f;
                    Render(name + "_grip", grip + flat * 0.9f + right * (0.5f * s) + Vector3.up * 0.15f, grip);
                    Render(name + "_gripout", grip + right * (1.0f * s) - flat * 0.2f, grip);
                }
                var rigs = CameraRigController.readOnlyInstancesList;
                if (rigs.Count > 0 && rigs[0].sceneCam)
                {
                    var source = rigs[0].sceneCam;
                    shotCamera.fieldOfView = source.fieldOfView;
                    Render(name + "_player", source.transform.position,
                        source.transform.position + source.transform.forward * 10f);
                }
                shotCamera.fieldOfView = fov;
            }
            finally { Time.timeScale = speed; }
        }

        private void CheckJavelinSilhouette(string name, Vector3 flat, Vector3 right)
        {
            var carry = pilot.GetComponent<SpearCarry>();
            var charge = StormspearCharge.Of(pilot);
            // v0.9.14: the spear arm is the left one by default; "out" and the head clearance mirror with it.
            string arm = carry && carry.Left ? "L " : "R ";
            float sideSign = carry && carry.Left ? -1f : 1f;
            var shoulder = KitUtil.ResolveSocket(pilot, arm + "upperarm");
            var elbow = KitUtil.ResolveSocket(pilot, arm + "forearm");
            var hand = KitUtil.ResolveSocket(pilot, arm + "hand");
            if (!carry || !carry.HandVisible || !carry.Tail || !carry.Tip || !shoulder || !elbow || !hand
                || !charge || !charge.Charging || charge.Form != SpearForm.Hand)
            { JavelinCheck(false, name + " missing hand charge/rig"); return; }

            float length = Vector3.Distance(shoulder.position, elbow.position) + Vector3.Distance(elbow.position, hand.position);
            Vector3 actualAim = pilot.inputBank.aimDirection.normalized;
            Vector3 wantedAim = Quaternion.AngleAxis(aimPitch, Right) * facing;
            JavelinCheck(Vector3.Dot(actualAim, wantedAim) > 0.995f, name + " camera overwrote scripted aim");
            JavelinCheck(pilot.characterDirection && Vector3.Dot(flat, pilot.characterDirection.forward) > 0.98f,
                name + " camera is not behind body facing");
            Vector3 delta = carry.GripPosition - shoulder.position;
            float out01 = sideSign * Vector3.Dot(delta, right) / length;
            float back01 = -Vector3.Dot(delta, flat) / length;
            float up01 = Vector3.Dot(delta, Vector3.up) / length;
            float elbowBend = Vector3.Angle(elbow.position - shoulder.position, hand.position - elbow.position);
            Vector3 a = shotCamera.WorldToViewportPoint(carry.Tail.position);
            Vector3 b = shotCamera.WorldToViewportPoint(carry.Tip.position);
            float pixels = new Vector2((b.x - a.x) * 1280f, (b.y - a.y) * 720f).magnitude;
            // v0.9.8: from the gameplay (rear) camera the grip and tip must sit clear of the head/halo.
            Transform head = null;
            var model = pilot.modelLocator ? pilot.modelLocator.modelTransform : null;
            if (model) foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == "head") { head = t; break; }
            Vector3 hv = shotCamera.WorldToViewportPoint(head ? head.position : pilot.corePosition + Vector3.up * 0.6f);
            Vector3 gv = shotCamera.WorldToViewportPoint(carry.GripPosition);
            float sepGrip = sideSign * (gv.x - hv.x) * 1280f, sepTip = sideSign * (b.x - hv.x) * 1280f;
            trace.AppendLine("JAVELIN_METRICS " + name + " charge=" + charge.Charge01.ToString("0.00")
                + " out=" + out01.ToString("0.00") + " back=" + back01.ToString("0.00")
                + " up=" + up01.ToString("0.00") + " elbowBend=" + elbowBend.ToString("0") + " shaftPixels=" + pixels.ToString("0")
                + " hand=" + arm.Trim() + " sepGrip=" + sepGrip.ToString("0") + " sepTip=" + sepTip.ToString("0"));
            JavelinCheck(out01 >= 0.50f, name + " grip not out beside the body");
            JavelinCheck(back01 >= 0.12f, name + " grip not behind shoulder");
            JavelinCheck(up01 >= 0.36f, name + " grip not above shoulder");
            JavelinCheck(sepGrip >= 60f && sepTip >= 25f, name + " spear overlaps the head from the gameplay camera");
            JavelinCheck(elbowBend >= 45f && elbowBend <= 110f, name + " elbow not cocked for throw");
            JavelinCheck(a.z > 0f && b.z > 0f && pixels >= 45f, name + " rear shaft too foreshortened");
        }

        private void JavelinCheck(bool ok, string why)
        {
            if (ok) return;
            errors++;
            trace.AppendLine("JAVELIN_FAIL " + why);
            Plugin.Log.LogError("JAVELIN_CHECK " + why);
        }
    }
}
