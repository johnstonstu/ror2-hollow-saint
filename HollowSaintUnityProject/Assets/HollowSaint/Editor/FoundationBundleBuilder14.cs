using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    // v0.8.0: bundle13's rig, surfaces and atlases with a state-driven Halo layer.
    // Empty -> Open Circuit -> Open Circuit hold -> Open Circuit end -> Empty, all driven by
    // the bool crownOpen, so the ring can never be left unfolded by an interrupted or skipped
    // clip, and the hold-to-close switch cross-fades instead of jumping.
    public static class FoundationBundleBuilder14
    {
        internal const string Folder = "Assets/HollowSaint/GameFoundation14/";
        private const string Source = "Assets/HollowSaint/GameFoundation13/mdlHollowSaint.prefab";
        private static readonly StringBuilder report = new StringBuilder();

        public static void RunBatch()
        {
            GameObject model = null;
            string reportPath = Path.GetFullPath("../artifacts/foundation/bundle14-report.txt");
            try
            {
                if (Directory.Exists(Folder)) throw new InvalidOperationException("Preserve existing " + Folder);
                Directory.CreateDirectory(Folder); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source));
                model.name = "mdlHollowSaint";
                var animator = model.GetComponent<Animator>();
                var runtime = animator.runtimeAnimatorController;
                var overrides = runtime as AnimatorOverrideController;
                var baseController = (overrides ? overrides.runtimeAnimatorController : runtime) as AnimatorController;
                if (!baseController) throw new InvalidOperationException("No base AnimatorController on " + Source);
                report.AppendLine("source controller=" + AssetDatabase.GetAssetPath(baseController) + " override=" + (overrides ? AssetDatabase.GetAssetPath(overrides) : "none"));
                string controllerPath = Folder + "Foundation.controller";
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(baseController), controllerPath)) throw new InvalidOperationException("Controller copy failed");
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                RebuildHalo(controller);
                EditorUtility.SetDirty(controller);
                RuntimeAnimatorController assigned = controller;
                if (overrides)
                {
                    string overridePath = Folder + "FittedTransitions.overrideController";
                    if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(overrides), overridePath)) throw new InvalidOperationException("Override copy failed");
                    var copy = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
                    var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>(); overrides.GetOverrides(pairs);
                    copy.runtimeAnimatorController = controller;
                    copy.ApplyOverrides(pairs);
                    EditorUtility.SetDirty(copy);
                    int kept = 0; var check = new List<KeyValuePair<AnimationClip, AnimationClip>>(); copy.GetOverrides(check);
                    foreach (var p in check) if (p.Value) kept++;
                    int wanted = pairs.Count(p => p.Value);
                    if (kept != wanted) throw new InvalidOperationException("Override clips lost: " + kept + "/" + wanted);
                    report.AppendLine("override clips kept=" + kept);
                    assigned = copy;
                }
                animator.runtimeAnimatorController = assigned;
                AssetDatabase.SaveAssets();
                PrefabUtility.SaveAsPrefabAsset(model, Folder + "mdlHollowSaint.prefab"); AssetDatabase.SaveAssets();
                string output = Path.GetFullPath("../artifacts/foundation/bundle14"); Directory.CreateDirectory(output);
                var assets = new List<string> { Folder + "mdlHollowSaint.prefab", "Assets/HollowSaint/GameFoundation11/mdlConduitSpear.prefab" };
                assets.AddRange(Directory.GetFiles("Assets/HollowSaint/GameFoundation13/", "*.asset").Where(p => Path.GetFileName(p).StartsWith("HS_Body", StringComparison.Ordinal)).Select(p => p.Replace('\\', '/')));
                var result = BuildPipeline.BuildAssetBundles(output, new[] { new AssetBundleBuild { assetBundleName = "hollowsaintassets", assetNames = assets.ToArray() } }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
                if (!result) throw new InvalidOperationException("Bundle build returned no manifest");
                File.WriteAllText(reportPath, "SUCCESS\n" + report);
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); File.WriteAllText(reportPath, "FAILED\n" + report + error); EditorApplication.Exit(1); }
            finally { if (model) UnityEngine.Object.DestroyImmediate(model); }
        }

        private static void RebuildHalo(AnimatorController controller)
        {
            if (!controller.parameters.Any(p => p.name == "crownOpen"))
                controller.AddParameter("crownOpen", AnimatorControllerParameterType.Bool);
            var layer = controller.layers.FirstOrDefault(l => l.name == "Halo");
            if (layer == null) throw new InvalidOperationException("No Halo layer");
            var machine = layer.stateMachine;
            AnimatorState Find(string name)
            {
                var s = machine.states.Select(c => c.state).FirstOrDefault(x => x.name == name);
                if (!s) throw new InvalidOperationException("Halo layer lacks " + name);
                return s;
            }
            var empty = Find("Empty"); var cast = Find("Open Circuit"); var hold = Find("Open Circuit hold"); var close = Find("Open Circuit end");
            foreach (var s in new[] { empty, cast, hold, close })
            {
                foreach (var t in s.transitions.ToArray()) s.RemoveTransition(t);
                s.writeDefaultValues = false;
                s.speedParameterActive = false; // fixed-length clips; no rate parameter to forget
                s.speed = 1f;
            }
            foreach (var t in machine.anyStateTransitions.ToArray()) machine.RemoveAnyStateTransition(t);
            machine.defaultState = empty;

            // Rest = a constant clip of the end clip's final frame, so the closed ring is held by an
            // explicit pose instead of relying on write-defaults-off retention.
            var restClip = MakeRestClip(close.motion as AnimationClip);
            var restState = machine.AddState("Halo rest", new Vector3(250f, -60f, 0f));
            restState.motion = restClip; restState.writeDefaultValues = false; restState.speed = 1f;
            machine.defaultState = restState;

            Link(restState, cast, true, false, 0f, 0.05f); // open: cast frame 0 is the rest pose
            Link(empty, cast, true, false, 0f, 0.05f);     // legacy entry, unused once rest is default
            Link(cast, restState, false, false, 0f, 0.35f);// interrupted before the crown: fold straight back
            Link(cast, hold, null, true, 1f, 0.05f);       // full unfold, then the crown loop
            Link(hold, close, false, false, 0f, 0.15f);    // crown ends: blend into the close, no jump
            Link(close, hold, true, false, 0f, 0.3f);      // reopened while closing: blend back open
            Link(close, restState, null, true, 1f, 0.05f); // ends exactly on rest
            report.AppendLine("halo states: Empty(default) / Open Circuit / Open Circuit hold / Open Circuit end; 6 transitions on crownOpen");
            foreach (var s in new[] { cast, hold, close })
            {
                var clip = s.motion as AnimationClip;
                report.AppendLine("  " + s.name + " clip=" + (clip ? clip.name + " len=" + clip.length.ToString("0.000") + " loop=" + clip.isLooping : "none"));
            }
        }

        private static AnimationClip MakeRestClip(AnimationClip end)
        {
            if (!end) throw new InvalidOperationException("Open Circuit end has no clip");
            var rest = new AnimationClip { name = "Halo_rest", frameRate = end.frameRate };
            int count = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(end))
            {
                if (!binding.path.Contains("halo")) continue;
                var curve = AnimationUtility.GetEditorCurve(end, binding);
                float value = curve.Evaluate(end.length);
                AnimationUtility.SetEditorCurve(rest, binding, AnimationCurve.Constant(0f, 1f / end.frameRate, value));
                count++;
            }
            if (count == 0) throw new InvalidOperationException("No halo curves in " + end.name);
            var settings = AnimationUtility.GetAnimationClipSettings(rest); settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(rest, settings);
            AssetDatabase.CreateAsset(rest, Folder + "Halo_rest.anim");
            report.AppendLine("rest clip curves=" + count);
            return rest;
        }

        private static void Link(AnimatorState from, AnimatorState to, bool? open, bool exit, float exitTime, float fade)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = exit; t.exitTime = exitTime;
            t.hasFixedDuration = true; t.duration = fade; t.offset = 0f;
            t.interruptionSource = TransitionInterruptionSource.None;
            if (open.HasValue) t.AddCondition(open.Value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "crownOpen");
        }
    }
}
