using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    /// <summary>
    /// Game bundle v2: the same model as bundle01 (v31_probe03, so the 140-renderer body
    /// split is unchanged) with all 65 v31 clips and a layered controller.
    ///
    /// Layers:
    ///   0 Body      full body. Direct states driven by FoundationPresentation, plus a
    ///               "Locomotion" 2D freeform blend tree (rightSpeed, forwardSpeed) over
    ///               the 8-direction walk and run clips. Loops scale by "moveRate".
    ///   1 UpperBody override, upper-body mask. "Empty" default; skill gestures played by
    ///               EntityStates, each returning to Empty on exit time.
    ///   2 Overlay   same mask. Discharge snap/full, meter-full flourish.
    ///   3 Halo      halo-bone mask. Open Circuit hold loop while the crown is up.
    ///
    /// Gesture states use "attackSpeed" as their speed multiplier, which is how
    /// EntityState.PlayAnimation scales a clip to a skill's duration.
    ///
    /// Batch: Unity.exe -batchmode -projectPath HollowSaintUnityProject
    ///        -executeMethod HollowSaint.Preview.Editor.FoundationBundleBuilder03.RunBatch -quit -logFile ...
    /// </summary>
    public static class FoundationBundleBuilder03
    {
        private const string ClipSource = "Assets/HollowSaint/Source/v31_clips02/";
        private const string ModelSource = "Assets/HollowSaint/Source/v31_probe03/HollowSaint.fbx";
        private const string Folder = "Assets/HollowSaint/GameFoundation03";
        private static string Output => Path.GetFullPath("../artifacts/foundation/bundle03");
        private static string Report => Path.GetFullPath("../artifacts/foundation/bundle03-report.txt");

        private static readonly string[] UpperBodyGestures = { "Arc Bolt right", "Arc Bolt left", "Conduit Spear", "Open Circuit", "Open Circuit end" };
        private static readonly string[] OverlayGestures = { "Discharge snap", "Discharge", "Meter full flourish" };
        // bundle03: cast + recall also live on the halo-only layer, so the recall always runs to
        // completion there and its last frame (rest) holds; UpperBody keeps them for the arms.
        private static readonly string[] HaloStates = { "Open Circuit hold", "Open Circuit", "Open Circuit end" };
        private static readonly string[] Excluded = { "Aim down", "Aim left", "Aim neutral", "Aim right", "Aim up", "Charge full", "Charge loop" };

        private static readonly HashSet<string> UpperBones = new HashSet<string>
        {
            "spine", "chest", "neck", "head", "head socket", "core socket",
            "L shoulder", "R shoulder", "L scapula", "R scapula", "L pauldron", "R pauldron",
            "L upperarm", "R upperarm", "L forearm", "R forearm", "L forearm twist", "R forearm twist",
            "L hand", "R hand", "L muzzle", "R muzzle"
        };
        private static readonly string[] Fingers = { "index", "middle", "ring", "little", "thumb" };
        private static readonly HashSet<string> HaloBones = new HashSet<string> { "halo root", "halo 1", "halo 2", "halo 3", "halo 4", "halo socket" };

        // (clip title, rightSpeed, forwardSpeed) from the catalog speeds in the anim spec.
        private static readonly (string, float, float)[] Locomotion =
        {
            ("Idle", 0f, 0f),
            ("Run forward", 0f, 6f), ("Run backward", 0f, -4.25f), ("Run right", 3.4f, 0f), ("Run left", -3.4f, 0f),
            ("Run forward right", 3.32f, 3.32f), ("Run forward left", -3.32f, 3.32f),
            ("Run backward right", 2.7f, -2.7f), ("Run backward left", -2.7f, -2.7f),
            ("Walk forward", 0f, 1.5f), ("Walk backward", 0f, -1f), ("Walk right", 0.6f, 0f), ("Walk left", -0.6f, 0f),
            ("Walk forward right", 0.74f, 0.74f), ("Walk forward left", -0.74f, 0.74f),
            ("Walk backward right", 0.57f, -0.57f), ("Walk backward left", -0.57f, -0.57f)
        };

        [Serializable] private class ClipManifest { public ClipEntry[] clips; }
        [Serializable] private class ClipEntry { public string title; public string file; public int start; public int end; public bool loop; public int fps; }

        public static void RunBatch()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                File.WriteAllText(Report, "FAILED\n" + error);
                Debug.LogError("Hollow Saint bundle03 failed: " + error);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Hollow Saint/Build game bundle 03 (per-layer rates, halo recall)")]
        public static void Build()
        {
            if (Directory.Exists(Folder)) throw new InvalidOperationException("Preserve existing " + Folder + "; use a new numbered output.");
            var log = new StringBuilder();
            var manifest = JsonUtility.FromJson<ClipManifest>(File.ReadAllText(ClipSource + "clips-manifest.json"));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelSource).OfType<Avatar>().Single();
            if (!avatar.isValid) throw new InvalidOperationException("Model avatar invalid");

            Directory.CreateDirectory(Folder + "/Clips");
            AssetDatabase.Refresh();

            var clips = new Dictionary<string, AnimationClip>();
            foreach (var entry in manifest.clips)
            {
                if (Excluded.Contains(entry.title)) continue;
                clips[entry.title] = ImportClip(entry, avatar, log);
            }
            log.AppendLine("clips=" + clips.Count);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "/Foundation.controller");
            foreach (string p in new[] { "isMoving", "isGrounded", "isSprinting", "isDeath", "inCombat", "isAirborne" })
                controller.AddParameter(p, AnimatorControllerParameterType.Bool);
            foreach (string p in new[] { "forwardSpeed", "rightSpeed", "upSpeed", "walkSpeed", "aimPitchCycle", "aimYawCycle", "aimWeight", "turnAngle", "jumpPlaybackRate" })
                controller.AddParameter(p, AnimatorControllerParameterType.Float);
            AddFloat(controller, "attackSpeed", 1f);
            AddFloat(controller, "overlaySpeed", 1f); // bundle03: per-layer gesture rates
            AddFloat(controller, "haloSpeed", 1f);
            AddFloat(controller, "moveRate", 1f);

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelSource);
            var upperMask = BuildMask(modelAsset, t => UpperBones.Contains(t) || Fingers.Any(f => t.StartsWith("L " + f) || t.StartsWith("R " + f)), "HS_UpperBody");
            var haloMask = BuildMask(modelAsset, t => HaloBones.Contains(t), "HS_Halo");

            // Layer 0: Body.
            var layers = controller.layers;
            layers[0].name = "Body";
            controller.layers = layers;
            var body = controller.layers[0].stateMachine;
            var locomotionTree = new BlendTree
            {
                name = "Locomotion",
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = "rightSpeed",
                blendParameterY = "forwardSpeed",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(locomotionTree, controller);
            foreach (var (title, x, y) in Locomotion)
                if (clips.ContainsKey(title)) locomotionTree.AddChild(clips[title], new Vector2(x, y));
            var locomotion = body.AddState("Locomotion");
            locomotion.motion = locomotionTree;
            locomotion.speedParameter = "moveRate";
            locomotion.speedParameterActive = true;

            var reserved = new HashSet<string>(UpperBodyGestures.Concat(OverlayGestures).Concat(HaloStates));
            foreach (var pair in clips)
            {
                if (reserved.Contains(pair.Key)) continue;
                var state = body.AddState(pair.Key);
                state.motion = pair.Value;
                state.writeDefaultValues = true;
                if (pair.Value.isLooping)
                {
                    state.speedParameter = "moveRate";
                    state.speedParameterActive = true;
                }
                if (pair.Key == "Idle") body.defaultState = state;
            }

            AddGestureLayer(controller, "UpperBody", upperMask, UpperBodyGestures, clips, returnToEmpty: true);
            AddGestureLayer(controller, "Overlay", upperMask, OverlayGestures, clips, returnToEmpty: true);
            AddGestureLayer(controller, "Halo", haloMask, HaloStates, clips, returnToEmpty: false);
            AssetDatabase.SaveAssets();

            // Model prefab: same model as bundle01, new controller.
            var model = UnityEngine.Object.Instantiate(modelAsset);
            try
            {
                model.name = "mdlHollowSaint";
                var animator = model.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (model.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException("Game model contains unexpected script components");
                log.AppendLine("renderers=" + model.GetComponentsInChildren<Renderer>(true).Length);
                PrefabUtility.SaveAsPrefabAsset(model, Folder + "/mdlHollowSaint.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(model); }
            AssetDatabase.SaveAssets();

            Verify(controller, clips, log);

            Directory.CreateDirectory(Output);
            var bundle = BuildPipeline.BuildAssetBundles(Output, new[] { new AssetBundleBuild
            {
                assetBundleName = "hollowsaintassets",
                assetNames = new[] { Folder + "/mdlHollowSaint.prefab" }
            } }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
            if (!bundle) throw new InvalidOperationException("Unity returned no bundle manifest");
            log.AppendLine("bundle=" + Path.Combine(Output, "hollowsaintassets") + " bytes=" + new FileInfo(Path.Combine(Output, "hollowsaintassets")).Length);
            File.WriteAllText(Report, "SUCCESS Unity " + Application.unityVersion + "\n" + log);
        }

        private static void AddFloat(AnimatorController controller, string name, float value)
        {
            controller.AddParameter(new AnimatorControllerParameter { name = name, type = AnimatorControllerParameterType.Float, defaultFloat = value });
        }

        private static AnimationClip ImportClip(ClipEntry data, Avatar avatar, StringBuilder log)
        {
            string path = ClipSource + data.file;
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new InvalidOperationException("No importer for " + path);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            importer.preserveHierarchy = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.globalScale = 1;
            importer.SaveAndReimport();
            var settings = importer.defaultClipAnimations;
            if (settings.Length != 1) throw new InvalidOperationException("Expected one take: " + path);
            settings[0].name = data.title;
            settings[0].loopTime = data.loop;
            settings[0].loopPose = false;
            importer.clipAnimations = settings;
            importer.SaveAndReimport();
            var source = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
            var clip = UnityEngine.Object.Instantiate(source);
            clip.name = data.title;
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                if (binding.type != typeof(Transform)) AnimationUtility.SetEditorCurve(clip, binding, null);
            AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = data.loop;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
            float expected = (data.end - data.start) / (float)data.fps;
            if (Mathf.Abs(clip.length - expected) > 0.002f)
                throw new InvalidOperationException("Clip duration mismatch: " + data.title + " = " + clip.length + " expected " + expected);
            AssetDatabase.CreateAsset(clip, Folder + "/Clips/" + Path.GetFileNameWithoutExtension(data.file) + ".anim");
            log.AppendLine("clip " + data.title + " length=" + clip.length.ToString("0.000") + " loop=" + data.loop);
            return clip;
        }

        private static AvatarMask BuildMask(GameObject modelAsset, Func<string, bool> include, string name)
        {
            var mask = new AvatarMask { name = name };
            var root = modelAsset.transform;
            var all = root.GetComponentsInChildren<Transform>(true);
            mask.transformCount = all.Length;
            for (int i = 0; i < all.Length; i++)
            {
                mask.SetTransformPath(i, AnimationUtility.CalculateTransformPath(all[i], root));
                mask.SetTransformActive(i, all[i] != root && include(all[i].name));
            }
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            AssetDatabase.CreateAsset(mask, Folder + "/" + name + ".mask");
            return mask;
        }

        private static void AddGestureLayer(AnimatorController controller, string layerName, AvatarMask mask,
            string[] states, Dictionary<string, AnimationClip> clips, bool returnToEmpty)
        {
            controller.AddLayer(layerName);
            var layers = controller.layers;
            var layer = layers[layers.Length - 1];
            layer.avatarMask = mask;
            layer.defaultWeight = 1f;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            controller.layers = layers;
            var machine = controller.layers[layers.Length - 1].stateMachine;
            var empty = machine.AddState("Empty");
            empty.writeDefaultValues = false; // an empty override state must leave lower layers alone
            machine.defaultState = empty;
            foreach (string title in states)
            {
                if (!clips.ContainsKey(title)) throw new InvalidOperationException("Missing gesture clip " + title);
                var state = machine.AddState(title);
                state.motion = clips[title];
                state.writeDefaultValues = false;
                state.speedParameter = layerName == "Overlay" ? "overlaySpeed" : layerName == "Halo" ? "haloSpeed" : "attackSpeed";
                state.speedParameterActive = true;
                if (returnToEmpty)
                {
                    var exit = state.AddTransition(empty);
                    exit.hasExitTime = true;
                    exit.exitTime = 0.95f;
                    exit.hasFixedDuration = true;
                    exit.duration = 0.12f;
                }
            }
            // Open Circuit cast flows into its recall only when the code asks; no automatic chain.
        }

        private static void Verify(AnimatorController controller, Dictionary<string, AnimationClip> clips, StringBuilder log)
        {
            var layers = controller.layers;
            if (layers.Length != 4) throw new InvalidOperationException("Expected 4 layers, got " + layers.Length);
            foreach (var layer in layers)
                log.AppendLine("layer " + layer.name + " states=" + layer.stateMachine.states.Length +
                    " mask=" + (layer.avatarMask ? layer.avatarMask.name + ":" + CountActive(layer.avatarMask) : "none"));
            string[] mustExist = { "Idle", "Locomotion", "Run start", "Run stop", "Jump", "Ascend", "Descend", "Land",
                "Glide enter", "Glide loop", "Glide exit", "Arc Step start", "Arc Step loop", "Arc Step end",
                "Arc Step left start", "Arc Step right start", "Arc Step back start", "Idle combat", "Spawn", "Select intro", "Select idle" };
            foreach (string s in mustExist)
                if (!layers[0].stateMachine.states.Any(c => c.state.name == s)) throw new InvalidOperationException("Body state missing: " + s);
        }

        private static int CountActive(AvatarMask mask)
        {
            int n = 0;
            for (int i = 0; i < mask.transformCount; i++) if (mask.GetTransformActive(i)) n++;
            return n;
        }
    }
}
