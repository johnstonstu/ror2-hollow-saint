using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    /// <summary>Keep bundle11; correct imported finger interpolation at release/catch.</summary>
    public static class FoundationBundleBuilder12
    {
        private const string Previous = "Assets/HollowSaint/GameFoundation11/";
        private const string Folder = "Assets/HollowSaint/GameFoundation12/";
        private const string Clips = "Assets/HollowSaint/GameFoundation10r1/Clips/";

        public static void RunBatch()
        {
            GameObject model = null;
            string report = Path.GetFullPath("../artifacts/foundation/bundle12-report.txt");
            try
            {
                if (Directory.Exists(Folder)) throw new InvalidOperationException("Preserve existing " + Folder);
                Directory.CreateDirectory(Folder);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Previous + "mdlHollowSaint.prefab"));
                model.name = "mdlHollowSaint";
                var animator = model.GetComponent<Animator>();
                var controller = new AnimatorOverrideController(animator.runtimeAnimatorController) { name = "HS_FittedSpearTransitions" };
                var held = AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips + "Spear_held.anim");
                foreach (string title in new[] { "Conduit_Spear", "Spear_catch" })
                {
                    var original = AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips + title + ".anim");
                    var copy = UnityEngine.Object.Instantiate(original); copy.name = original.name;
                    FixFingers(copy, held, title == "Conduit_Spear");
                    VerifyRetained(original, copy);
                    AssetDatabase.CreateAsset(copy, Folder + title + ".anim");
                    controller[original] = copy;
                }
                AssetDatabase.CreateAsset(controller, Folder + "FittedTransitions.overrideController");
                animator.runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(model, Folder + "mdlHollowSaint.prefab");
                AssetDatabase.SaveAssets();
                string output = Path.GetFullPath("../artifacts/foundation/bundle12");
                Directory.CreateDirectory(output);
                var manifest = BuildPipeline.BuildAssetBundles(output, new[] { new AssetBundleBuild {
                    assetBundleName = "hollowsaintassets", assetNames = new[] { Folder + "mdlHollowSaint.prefab", Previous + "mdlConduitSpear.prefab" }
                } }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
                if (!manifest) throw new InvalidOperationException("Bundle build returned no manifest");
                File.WriteAllText(report, "SUCCESS\nOnly 15 right-finger quaternion tracks in throw/catch changed.\nExact grasp through release (6/19), and after arrival (13/19); zero marker tangents avoid imported overshoot.\nOther curves, duration, events, model, spear mesh, controller states and masks retained.\n");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { File.WriteAllText(report, "FAILED\n" + error); Debug.LogException(error); EditorApplication.Exit(1); }
            finally { if (model) UnityEngine.Object.DestroyImmediate(model); }
        }

        private static bool Finger(EditorCurveBinding b)
        {
            string bone = b.path.Split('/').Last();
            return b.type == typeof(Transform) && b.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal) &&
                new[] { "R index.", "R middle.", "R ring.", "R little.", "R thumb." }.Any(bone.StartsWith);
        }

        private static void FixFingers(AnimationClip clip, AnimationClip held, bool throwing)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip).Where(Finger).ToArray();
            if (bindings.Length != 60) throw new InvalidOperationException("Expected 15 finger quaternion tracks");
            float marker = clip.length * (throwing ? 6f / 19f : 13f / 19f);
            foreach (var group in bindings.GroupBy(b => b.path))
            {
                var channels = group.OrderBy(b => "xyzw".IndexOf(b.propertyName.Last())).ToArray();
                float[] grasp = channels.Select(b => AnimationUtility.GetEditorCurve(held, b).Evaluate(0f)).ToArray();
                float[] atMarker = channels.Select(b => AnimationUtility.GetEditorCurve(clip, b).Evaluate(marker)).ToArray();
                float sign = grasp.Select((v, i) => v * atMarker[i]).Sum() < 0f ? -1f : 1f;
                for (int i = 0; i < channels.Length; i++)
                {
                    var binding = channels[i]; var old = AnimationUtility.GetEditorCurve(clip, binding);
                    float value = grasp[i] * sign;
                    var keys = old.keys.Where(k => throwing ? k.time > marker + 0.00001f : k.time < marker - 0.00001f).ToList();
                    keys.Add(new Keyframe(marker, value, 0f, 0f));
                    keys.Add(new Keyframe(throwing ? 0f : clip.length, value, 0f, 0f));
                    var curve = new AnimationCurve(keys.OrderBy(k => k.time).ToArray()) { preWrapMode = old.preWrapMode, postWrapMode = old.postWrapMode };
                    AnimationUtility.SetEditorCurve(clip, binding, curve);
                }
            }
        }

        private static void VerifyRetained(AnimationClip original, AnimationClip copy)
        {
            if (Mathf.Abs(original.length - copy.length) > 0.00001f) throw new InvalidOperationException("Clip duration changed");
            foreach (var binding in AnimationUtility.GetCurveBindings(original).Where(b => !Finger(b)))
            {
                var before = AnimationUtility.GetEditorCurve(original, binding); var after = AnimationUtility.GetEditorCurve(copy, binding);
                if (after == null || !before.keys.SequenceEqual(after.keys)) throw new InvalidOperationException("Unrelated curve changed: " + binding.path + "/" + binding.propertyName);
            }
        }
    }
}
