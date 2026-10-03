using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    [InitializeOnLoad]
    public static class FoundationBundleBuilder
    {
        private const string Folder = "Assets/HollowSaint/GameFoundation01";
        private static string Request => Path.GetFullPath("../artifacts/foundation/build-bundle.request");
        static FoundationBundleBuilder() { EditorApplication.update += CheckRequest; }

        private static void CheckRequest()
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            File.Move(Request, Request + ".started");
            try { Build(); }
            catch (Exception error)
            {
                File.WriteAllText(Path.GetFullPath("../artifacts/foundation/bundle-error.txt"), error.ToString());
                Debug.LogError("Hollow Saint foundation bundle failed: " + error);
            }
        }

        [MenuItem("Hollow Saint/Build game foundation bundle")]
        public static void Build()
        {
            if (Directory.Exists(Folder)) throw new InvalidOperationException("Preserve existing foundation assets; use a new numbered output.");
            Directory.CreateDirectory(Folder + "/Clips");
            AssetDatabase.Refresh();
            var controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "/Foundation.controller");
            var layers = controller.layers;
            layers[0].name = "Body";
            controller.layers = layers;
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { ProbeAssets.Generated + "Clips" }))
            {
                var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
                var clip = UnityEngine.Object.Instantiate(source);
                // The game bundle contains no Assembly-CSharp preview-script references.
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    if (binding.type != typeof(Transform)) AnimationUtility.SetEditorCurve(clip, binding, null);
                AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
                AssetDatabase.CreateAsset(clip, Folder + "/Clips/" + source.name + ".anim");
                var state = controller.layers[0].stateMachine.AddState(source.name.Replace('_', ' '));
                state.motion = clip;
                if (source.name == "Idle") controller.layers[0].stateMachine.defaultState = state;
            }
            foreach (string name in new[] { "isMoving", "isGrounded", "isSprinting", "isDeath", "inCombat", "isAirborne" })
                controller.AddParameter(name, AnimatorControllerParameterType.Bool);
            foreach (string name in new[] { "forwardSpeed", "rightSpeed", "upSpeed", "walkSpeed", "aimPitchCycle", "aimYawCycle", "aimWeight", "turnAngle", "jumpPlaybackRate", "attackSpeed" })
                controller.AddParameter(name, AnimatorControllerParameterType.Float);
            var sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(ProbeAssets.Source + "HollowSaint.fbx");
            var model = UnityEngine.Object.Instantiate(sourceModel);
            try
            {
                model.name = "mdlHollowSaint";
                var animator = model.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (model.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException("Game model contains unexpected script components");
                PrefabUtility.SaveAsPrefabAsset(model, Folder + "/mdlHollowSaint.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(model); }
            AssetDatabase.SaveAssets();
            string output = Path.GetFullPath("../artifacts/foundation/bundle01");
            Directory.CreateDirectory(output);
            var manifest = BuildPipeline.BuildAssetBundles(output, new[] { new AssetBundleBuild
            {
                assetBundleName = "hollowsaintassets",
                assetNames = new[] { Folder + "/mdlHollowSaint.prefab" }
            } }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
            if (!manifest) throw new InvalidOperationException("Unity returned no bundle manifest");
            File.WriteAllText(Path.GetFullPath("../artifacts/foundation/bundle-success.txt"),
                "Unity " + Application.unityVersion + "\nBundle: " + output + "\nNative-only model and transform-only clips; preview scene untouched.\n");
        }
    }
}
