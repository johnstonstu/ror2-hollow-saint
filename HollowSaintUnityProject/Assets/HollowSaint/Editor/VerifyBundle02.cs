using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    /// <summary>
    /// Behavioural check of the GameFoundation02 controller without entering play mode:
    /// steps the Animator manually and compares bone rotations.
    /// </summary>
    public static class VerifyBundle02
    {
        private static string Report => Path.GetFullPath("../artifacts/foundation/bundle02-verify.txt");

        public static void RunBatch()
        {
            var log = new StringBuilder();
            int failures = 0;
            GameObject go = null;
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HollowSaint/GameFoundation02/mdlHollowSaint.prefab");
                go = UnityEngine.Object.Instantiate(prefab);
                var animator = go.GetComponent<Animator>();
                Transform Bone(string n) => go.GetComponentsInChildren<Transform>(true).First(t => t.name == n);
                var shin = Bone("L shin");
                var forearm = Bone("R forearm");
                var halo = Bone("halo 1");

                Quaternion[] Sample(Action setup, float seconds)
                {
                    animator.Rebind();
                    animator.Update(0f);
                    setup();
                    for (float t = 0; t < seconds; t += 1f / 30f) animator.Update(1f / 30f);
                    return new[] { shin.localRotation, forearm.localRotation, halo.localRotation };
                }

                void Check(bool ok, string what)
                {
                    log.AppendLine((ok ? "PASS " : "FAIL ") + what);
                    if (!ok) failures++;
                }

                float Diff(Quaternion a, Quaternion b) => Quaternion.Angle(a, b);

                var idle = Sample(() => animator.Play("Idle", 0, 0f), 0.4f);
                var run = Sample(() => { animator.SetFloat("forwardSpeed", 6f); animator.SetFloat("moveRate", 1f); animator.Play("Locomotion", 0, 0.25f); }, 0.4f);
                Check(Diff(idle[0], run[0]) > 3f, "Locomotion moves the legs vs Idle (shin delta " + Diff(idle[0], run[0]).ToString("0.0") + " deg)");

                var strafe = Sample(() => { animator.SetFloat("rightSpeed", 3.4f); animator.SetFloat("forwardSpeed", 0f); animator.Play("Locomotion", 0, 0.25f); }, 0.4f);
                Check(Diff(run[0], strafe[0]) > 1f, "Strafe blend differs from forward run (shin delta " + Diff(run[0], strafe[0]).ToString("0.0") + ")");

                // Upper layer Empty must not disturb the body pose.
                var idleUpperOff = Sample(() => { animator.SetLayerWeight(1, 0f); animator.Play("Idle", 0, 0f); }, 0.4f);
                animator.SetLayerWeight(1, 1f);
                Check(Diff(idle[1], idleUpperOff[1]) < 0.5f, "UpperBody Empty leaves arms on the body pose (forearm delta " + Diff(idle[1], idleUpperOff[1]).ToString("0.00") + ")");

                // A gesture drives the arms but not the legs.
                var cast = Sample(() =>
                {
                    animator.SetFloat("forwardSpeed", 6f);
                    animator.Play("Locomotion", 0, 0.25f);
                    animator.SetFloat("attackSpeed", 1f);
                    animator.Play("Arc Bolt right", 1, 0f);
                }, 0.25f);
                var runSame = Sample(() => { animator.SetFloat("forwardSpeed", 6f); animator.Play("Locomotion", 0, 0.25f); }, 0.25f);
                Check(Diff(cast[1], runSame[1]) > 3f, "Arc Bolt gesture moves the forearm while running (delta " + Diff(cast[1], runSame[1]).ToString("0.0") + ")");
                Check(Diff(cast[0], runSame[0]) < 0.5f, "Arc Bolt gesture leaves the legs to locomotion (shin delta " + Diff(cast[0], runSame[0]).ToString("0.00") + ")");

                var crown = Sample(() => { animator.Play("Idle", 0, 0f); animator.Play("Open Circuit hold", 3, 0f); }, 0.3f);
                Check(Diff(crown[2], idle[2]) > 1f, "Halo layer moves the halo (delta " + Diff(crown[2], idle[2]).ToString("0.0") + ")");
                Check(Diff(crown[0], idle[0]) < 0.5f, "Halo layer leaves the legs alone");

                // Gesture returns to Empty.
                animator.Rebind(); animator.Update(0f);
                animator.SetFloat("attackSpeed", 1f);
                animator.Play("Conduit Spear", 1, 0f);
                for (int i = 0; i < 40; i++) animator.Update(1f / 30f);
                var info = animator.GetCurrentAnimatorStateInfo(1);
                Check(info.IsName("Empty"), "Conduit Spear returns to Empty after its clip");
            }
            catch (Exception error)
            {
                log.AppendLine("ERROR " + error);
                failures++;
            }
            finally
            {
                if (go) UnityEngine.Object.DestroyImmediate(go);
            }
            File.WriteAllText(Report, (failures == 0 ? "ALL PASS\n" : failures + " FAILED\n") + log);
            EditorApplication.Exit(failures == 0 ? 0 : 1);
        }
    }
}
