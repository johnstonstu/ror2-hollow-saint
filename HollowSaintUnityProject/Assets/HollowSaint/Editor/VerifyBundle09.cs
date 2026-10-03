using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    // Success: only the two throws change; boundaries/timing remain; arm casts preserve
    // locomotion and the halo layer. Sample actual imported clips/controller in Unity.
    public static class VerifyBundle09
    {
        private const string Old = "Assets/HollowSaint/GameFoundation07/";
        private const string New = "Assets/HollowSaint/GameFoundation09/";
        private static readonly StringBuilder Log = new StringBuilder();
        private static int failures;

        public static void RunBatch()
        {
            GameObject model = null;
            try
            {
                var paths = Directory.GetFiles(Old + "Clips", "*.anim");
                Check(paths.Length == Directory.GetFiles(New + "Clips", "*.anim").Length, "clip count retained");
                foreach (var path in paths)
                {
                    var a = AssetDatabase.LoadAssetAtPath<AnimationClip>(path.Replace('\\', '/'));
                    var b = AssetDatabase.LoadAssetAtPath<AnimationClip>(New + "Clips/" + Path.GetFileName(path));
                    Check(b && Mathf.Abs(a.length - b.length) < 0.0001f && a.isLooping == b.isLooping,
                        a.name + " duration/loop retained");
                    bool isThrow = a.name.Replace('_', ' ').StartsWith("Arc Bolt ", StringComparison.Ordinal);
                    float full = CurveDifference(a, b, false), boundary = CurveDifference(a, b, true);
                    Check(isThrow ? full > 0.01f && boundary < 0.00001f : full < 0.00001f,
                        a.name + (isThrow ? " changes inside preserved boundaries" : " unchanged") +
                        " difference=" + full + " boundary=" + boundary);
                }
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(New + "Foundation.controller");
                Check(controller.layers.Length == 5, "five layers retained");
                model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(New + "mdlHollowSaint.prefab"));
                var animator = model.GetComponent<Animator>();
                var bones = model.GetComponentsInChildren<Transform>(true);
                Transform Bone(string name) => bones.Single(t => t.name == name);
                var shin = Bone("L shin"); var chest = Bone("chest"); var halo = Bone("halo 1");
                foreach (string side in new[] { "left", "right" })
                {
                    var forearm = Bone((side == "left" ? "L" : "R") + " forearm");
                    foreach (int layer in new[] { 1, 4 })
                    {
                        Quaternion[] Sample(bool cast)
                        {
                            animator.Rebind(); animator.Update(0f);
                            animator.SetFloat("forwardSpeed", layer == 4 ? 6f : 0f);
                            animator.SetFloat("gaitBlend", 1f);
                            animator.Play(layer == 4 ? "Locomotion" : "Idle combat", 0, 0.25f);
                            animator.Play("Open Circuit hold", 3, 0.3f);
                            if (cast) animator.Play("Arc Bolt " + side, layer, 4f / 19f);
                            animator.Update(0f);
                            return new[] { forearm.localRotation, shin.localRotation, chest.localRotation, halo.localRotation };
                        }
                        var baseline = Sample(false); var castPose = Sample(true);
                        string label = side + " layer " + layer;
                        Check(Quaternion.Angle(baseline[0], castPose[0]) > 3f, label + " release drives arm");
                        Check(Quaternion.Angle(baseline[1], castPose[1]) < 0.1f, label + " leaves legs to locomotion");
                        Check(Quaternion.Angle(baseline[3], castPose[3]) < 0.1f, label + " preserves Open Circuit halo");
                        if (layer == 4) Check(Quaternion.Angle(baseline[2], castPose[2]) < 0.1f,
                            label + " preserves running torso");
                    }
                }
            }
            catch (Exception error) { failures++; Log.AppendLine("ERROR " + error); Debug.LogException(error); }
            finally { if (model) UnityEngine.Object.DestroyImmediate(model); }
            File.WriteAllText(Path.GetFullPath("../artifacts/foundation/bundle09-verify.txt"),
                (failures == 0 ? "ALL PASS\n" : failures + " FAILED\n") + Log);
            EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        private static float CurveDifference(AnimationClip a, AnimationClip b, bool boundaries)
        {
            var bindings = AnimationUtility.GetCurveBindings(a);
            if (!b || bindings.Length != AnimationUtility.GetCurveBindings(b).Length) return float.PositiveInfinity;
            float difference = 0f;
            foreach (var binding in bindings)
            {
                var x = AnimationUtility.GetEditorCurve(a, binding);
                var y = AnimationUtility.GetEditorCurve(b, binding);
                if (y == null || x.keys.Length != y.keys.Length) return float.PositiveInfinity;
                if (boundaries)
                {
                    difference = Mathf.Max(difference, Mathf.Abs(x.Evaluate(0) - y.Evaluate(0)),
                        Mathf.Abs(x.Evaluate(a.length) - y.Evaluate(b.length)));
                    continue;
                }
                for (int i = 0; i < x.keys.Length; i++)
                    difference = Mathf.Max(difference, Mathf.Abs(x.keys[i].value - y.keys[i].value),
                        Mathf.Abs(x.keys[i].time - y.keys[i].time),
                        Difference(x.keys[i].inTangent, y.keys[i].inTangent),
                        Difference(x.keys[i].outTangent, y.keys[i].outTangent));
            }
            return difference;
        }
        // Identical infinite tangents are valid constant keys; subtracting them gives NaN.
        private static float Difference(float x, float y) => x == y ? 0f : Mathf.Abs(x - y);
        private static void Check(bool ok, string what)
        { Log.AppendLine((ok ? "PASS " : "FAIL ") + what); if (!ok) failures++; }
    }
}
