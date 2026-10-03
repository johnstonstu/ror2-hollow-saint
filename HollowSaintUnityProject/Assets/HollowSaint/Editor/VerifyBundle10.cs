using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    // Success: old clips exact, right carry preserves locomotion/left/halo, grasp remains
    // fixed through held/channel poses and transitions, socket/weapon are present in bundle.
    public static class VerifyBundle10
    {
        private const string AnimationFolder = "Assets/HollowSaint/GameFoundation10r1/";
        private static string Folder = AnimationFolder;
        private static string BundleName = "bundle10r1";
        private static int weaponParts = 15;
        private static readonly StringBuilder Log = new StringBuilder();
        private static int failures;
        public static void RunFinalBatch()
        {
            Folder = "Assets/HollowSaint/GameFoundation11/";
            BundleName = "bundle11"; weaponParts = 14;
            RunBatch();
        }
        public static void RunBatch()
        {
            GameObject model = null, spear = null;
            AssetBundle bundle = null;
            try
            {
                foreach (string path in Directory.GetFiles("Assets/HollowSaint/GameFoundation09/Clips", "*.anim"))
                {
                    if (Path.GetFileName(path) == "Conduit_Spear.anim" || Path.GetFileName(path) == "Conduit_Spear_recover.anim") continue;
                    var old = AssetDatabase.LoadAssetAtPath<AnimationClip>(path.Replace('\\', '/'));
                    var current = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationFolder + "Clips/" + Path.GetFileName(path));
                    var bindings = AnimationUtility.GetCurveBindings(old);
                    Check(current && bindings.Length == AnimationUtility.GetCurveBindings(current).Length &&
                        bindings.All(b => AnimationUtility.GetEditorCurve(old, b).keys.SequenceEqual(AnimationUtility.GetEditorCurve(current, b).keys)), "retained " + old.name);
                }
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimationFolder + "Foundation.controller");
                Check(controller.layers.Length == 6 && controller.layers[5].name == "SpearCarry", "six layers and hand ownership");
                var mask = controller.layers[5].avatarMask;
                Check(Enumerable.Range(0, mask.transformCount).Where(mask.GetTransformActive)
                    .All(i => mask.GetTransformPath(i).Split('/').Last().StartsWith("R ", StringComparison.Ordinal)), "carry mask only right arm");
                model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "mdlHollowSaint.prefab"));
                spear = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "mdlConduitSpear.prefab"));
                var bones = model.GetComponentsInChildren<Transform>(true);
                Transform Bone(string name) => bones.Single(t => t.name == name);
                var socket = Bone("SpearGripSocket");
                Check(socket.parent.name == "R hand", "grip bone parent");
                spear.transform.SetParent(socket, false);
                spear.transform.localPosition = Vector3.zero; spear.transform.localRotation = Quaternion.identity;
                var tip = spear.GetComponentsInChildren<Transform>().Single(t => t.name == "SpearTip");
                Check(spear.GetComponentsInChildren<MeshRenderer>().Length == weaponParts && Mathf.Abs(tip.localPosition.magnitude - 1.3f) < 0.001f, "fitted model and tip dimensions");
                var animator = model.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                string[] observed = { "R hand", "R index.1", "R middle.2", "R thumb.1", "L hand", "L shin", "chest", "halo 1" };
                Quaternion[] Sample(string locomotion, string carry, float phase)
                {
                    animator.Rebind(); animator.Update(0);
                    animator.Play(locomotion, 0, 0.25f);
                    animator.Play("Open Circuit hold", 3, 0.3f);
                    if (carry != null) animator.Play(carry, 5, phase);
                    animator.Update(0);
                    return observed.Select(n => Bone(n).localRotation).ToArray();
                }
                var grasp = Sample("Idle combat", "Spear held", 0);
                foreach (string locomotion in new[] { "Idle combat", "Run forward", "Jump", "Glide loop", "Arc Step loop" })
                {
                    var baseline = Sample(locomotion, null, 0);
                    foreach (string state in new[] { "Spear held", "Spear crown held", "Spear fan start", "Spear fan loop", "Spear fan end" })
                    foreach (float phase in new[] { 0f, 0.25f, 0.5f, 0.9f })
                    {
                        var pose = Sample(locomotion, state, phase);
                        Check(Enumerable.Range(1, 3).All(i => Quaternion.Angle(grasp[i], pose[i]) < 0.05f), locomotion + "/" + state + " fixed grasp " + phase);
                        Check(Enumerable.Range(4, 4).All(i => Quaternion.Angle(baseline[i], pose[i]) < 0.05f), locomotion + "/" + state + " preserves left/legs/torso/halo " + phase);
                    }
                }
                var held = Sample("Idle combat", "Spear held", 0);
                var throwStart = Sample("Idle combat", "Conduit Spear", 0);
                var caught = Sample("Idle combat", "Spear catch", 1);
                Check(Enumerable.Range(0, 4).All(i => Quaternion.Angle(held[i], throwStart[i]) < 0.05f), "held to throw seam");
                Check(Enumerable.Range(0, 4).All(i => Quaternion.Angle(held[i], caught[i]) < 0.05f), "catch to held seam");
                foreach (string state in new[] { "Spear held", "Spear crown held", "Spear fan loop" })
                {
                    var a = Sample("Idle combat", state, 0); var b = Sample("Idle combat", state, 0.99999f);
                    Check(Enumerable.Range(0, 4).All(i => Quaternion.Angle(a[i], b[i]) < 0.1f), "loop seam " + state);
                }
                bundle = AssetBundle.LoadFromFile(Path.GetFullPath("../artifacts/foundation/" + BundleName + "/hollowsaintassets"));
                Check(bundle && bundle.LoadAsset<GameObject>("mdlHollowSaint") && bundle.LoadAsset<GameObject>("mdlConduitSpear"), "paired model assets load from built bundle");
            }
            catch (Exception error) { failures++; Log.AppendLine("ERROR " + error); Debug.LogException(error); }
            finally { if (spear) UnityEngine.Object.DestroyImmediate(spear); if (model) UnityEngine.Object.DestroyImmediate(model); if (bundle) bundle.Unload(true); }
            File.WriteAllText(Path.GetFullPath("../artifacts/foundation/" + BundleName + "-verify.txt"), (failures == 0 ? "ALL PASS\n" : failures + " FAILED\n") + Log);
            EditorApplication.Exit(failures == 0 ? 0 : 1);
        }
        private static void Check(bool okay, string detail) { Log.AppendLine((okay ? "PASS " : "FAIL ") + detail); if (!okay) failures++; }
    }
}
