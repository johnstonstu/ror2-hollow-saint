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
    /// Game bundle v9: the v34 controller with only Arc Bolt left/right replaced by
    /// the accepted v36 ring-fed throw. Layers/timings and model (v31_probe03) retained.
    /// Clip resolution per title: v34_clips01 if it ships that title, else the v32 Run forward
    /// robe correction, else v31_clips02. New v33-only titles are optional: a missing one is
    /// logged in the report and its state is simply not built (runtime code falls back).
    ///
    /// Layers:
    ///   0 Body      full body. Direct states driven by FoundationPresentation, plus a
    ///               "Locomotion" 2D freeform blend tree (rightSpeed, forwardSpeed) over
    ///               the 8-direction walk and run clips. Loops scale by "moveRate".
    ///   1 UpperBody override, upper-body mask. "Empty" default; skill gestures played by
    ///               EntityStates, each returning to Empty on exit time.
    ///   2 Overlay   same mask. Discharge snap/full, meter-full flourish.
    ///   3 Halo      halo-bone mask. Open Circuit hold loop while the crown is up.
    ///   4 UpperArms bundle06: arms-only mask (shoulders, scapulae, pauldrons, arms, hands,
    ///               fingers; no spine/chest/neck/head). Duplicates the UpperBody gestures so a
    ///               cast while moving keeps the run torso and lean. Code picks one layer per cast.
    ///
    /// bundle06 gesture timing: exit at 85% with a 0.2 s fade (was 95% / 0.12 s). Gestures with
    /// a "<name> recover" clip hand off to it instead of Empty; recover and hold states run at 1x.
    ///
    /// Gesture states use "attackSpeed" as their speed multiplier, which is how
    /// EntityState.PlayAnimation scales a clip to a skill's duration.
    ///
    /// Batch: Unity.exe -batchmode -projectPath HollowSaintUnityProject
    ///        -executeMethod HollowSaint.Preview.Editor.FoundationBundleBuilder09.RunBatch -quit -logFile ...
    /// </summary>
    public static class FoundationBundleBuilder09
    {
        private const string ClipSource = "Assets/HollowSaint/Source/v31_clips02/";
        private const string RunFixSource = "Assets/HollowSaint/Source/v32_clips01/";
        private const string PolishSource = "Assets/HollowSaint/Source/v34_clips01/";
        private const string ThrowSource = "Assets/HollowSaint/Source/v36_clips01/";
        private const string ModelSource = "Assets/HollowSaint/Source/v31_probe03/HollowSaint.fbx";
        private const string Folder = "Assets/HollowSaint/GameFoundation09";
        private static string Output => Path.GetFullPath("../artifacts/foundation/bundle09");
        private static string Report => Path.GetFullPath("../artifacts/foundation/bundle09-report.txt");

        private static readonly string[] UpperBodyGestures = { "Arc Bolt right", "Arc Bolt left", "Conduit Spear", "Open Circuit", "Open Circuit end",
            "Conduit Spear recover", "Open Circuit arms", "Open Circuit arms hold" };
        private static readonly string[] OverlayGestures = { "Discharge snap", "Discharge", "Meter full flourish", "Discharge recover" };
        // bundle06: new v33 titles. Optional, so a missing one never fails the build.
        private static readonly HashSet<string> Optional = new HashSet<string>
        {
            "Combat ready", "Combat relax", "Idle fidget 1", "Idle fidget 2", "Idle fidget 3", "Idle combat fidget",
            "Conduit Spear recover", "Discharge recover", "Open Circuit arms", "Open Circuit arms hold"
        };
        // Gesture -> the state it hands off to instead of Empty (when both exist).
        private static readonly Dictionary<string, string> Chains = new Dictionary<string, string>
        {
            { "Conduit Spear", "Conduit Spear recover" },
            { "Discharge", "Discharge recover" },
            { "Open Circuit arms", "Open Circuit arms hold" }
        };
        // Held or tail states: 1x playback, no automatic return on hold loops.
        private static readonly HashSet<string> FixedRate = new HashSet<string> { "Conduit Spear recover", "Discharge recover", "Open Circuit arms hold" };
        private const float GestureExitTime = 0.85f;
        private const float GestureExitFade = 0.2f;
        private const float ChainExitTime = 0.92f;
        private const float ChainFade = 0.08f;
        // bundle05: cast + recall also live on the halo-only layer, so the recall always runs to
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
        // Arms-only: the UpperBones set minus spine, chest, neck, head and the body sockets.
        private static readonly HashSet<string> ArmBones = new HashSet<string>
        {
            "L shoulder", "R shoulder", "L scapula", "R scapula", "L pauldron", "R pauldron",
            "L upperarm", "R upperarm", "L forearm", "R forearm", "L forearm twist", "R forearm twist",
            "L hand", "R hand", "L muzzle", "R muzzle"
        };
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
                Debug.LogError("Hollow Saint bundle09 failed: " + error);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Hollow Saint/Build game bundle 09 (ring-fed throw)")]
        public static void Build()
        {
            if (Directory.Exists(Folder)) throw new InvalidOperationException("Preserve existing " + Folder + "; use a new numbered output.");
            var log = new StringBuilder();
            var manifest = JsonUtility.FromJson<ClipManifest>(File.ReadAllText(ClipSource + "clips-manifest.json"));
            if (!File.Exists(PolishSource + "clips-manifest.json")) throw new InvalidOperationException("Missing " + PolishSource + "clips-manifest.json");
            var polish = JsonUtility.FromJson<ClipManifest>(File.ReadAllText(PolishSource + "clips-manifest.json"));
            var polishTitles = new HashSet<string>(polish.clips.Select(c => c.title));
            var throws = JsonUtility.FromJson<ClipManifest>(File.ReadAllText(ThrowSource + "clips-manifest.json"));
            var throwTitles = new HashSet<string>(throws.clips.Select(c => c.title));
            if (throws.clips.Length != 2 || !throwTitles.SetEquals(new[] { "Arc Bolt left", "Arc Bolt right" }))
                throw new InvalidOperationException("v36 must supply exactly both Arc Bolt throws");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelSource).OfType<Avatar>().Single();
            if (!avatar.isValid) throw new InvalidOperationException("Model avatar invalid");

            Directory.CreateDirectory(Folder + "/Clips");
            AssetDatabase.Refresh();

            var clips = new Dictionary<string, AnimationClip>();
            foreach (var entry in manifest.clips)
            {
                if (Excluded.Contains(entry.title) || polishTitles.Contains(entry.title)) continue;
                string source = entry.title == "Run forward" ? RunFixSource : ClipSource;
                clips[entry.title] = ImportClip(entry, source, avatar, log);
            }
            foreach (var entry in polish.clips)
            {
                if (Excluded.Contains(entry.title) || throwTitles.Contains(entry.title)) continue;
                clips[entry.title] = ImportClip(entry, PolishSource, avatar, log);
                log.AppendLine("v34 " + entry.title);
            }
            foreach (var entry in throws.clips)
            {
                clips[entry.title] = ImportClip(entry, ThrowSource, avatar, log);
                log.AppendLine("v36 " + entry.title);
            }
            foreach (string title in Optional)
                if (!clips.ContainsKey(title)) log.AppendLine("OPTIONAL_MISSING " + title + " (state not built; runtime falls back)");
            log.AppendLine("clips=" + clips.Count);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "/Foundation.controller");
            foreach (string p in new[] { "isMoving", "isGrounded", "isSprinting", "isDeath", "inCombat", "isAirborne" })
                controller.AddParameter(p, AnimatorControllerParameterType.Bool);
            foreach (string p in new[] { "forwardSpeed", "rightSpeed", "upSpeed", "walkSpeed", "aimPitchCycle", "aimYawCycle", "aimWeight", "turnAngle", "jumpPlaybackRate" })
                controller.AddParameter(p, AnimatorControllerParameterType.Float);
            AddFloat(controller, "attackSpeed", 1f);
            AddFloat(controller, "overlaySpeed", 1f); // bundle05: per-layer gesture rates
            AddFloat(controller, "haloSpeed", 1f);
            AddFloat(controller, "moveRate", 1f);
            AddFloat(controller, "gaitBlend", 0f);

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelSource);
            var upperMask = BuildMask(modelAsset, t => UpperBones.Contains(t) || Fingers.Any(f => t.StartsWith("L " + f) || t.StartsWith("R " + f)), "HS_UpperBody");
            var haloMask = BuildMask(modelAsset, t => HaloBones.Contains(t), "HS_Halo");
            var armsMask = BuildMask(modelAsset, t => ArmBones.Contains(t) || Fingers.Any(f => t.StartsWith("L " + f) || t.StartsWith("R " + f)), "HS_UpperArms");

            // Layer 0: Body.
            var layers = controller.layers;
            layers[0].name = "Body";
            controller.layers = layers;
            var body = controller.layers[0].stateMachine;
            var locomotionTree = BuildLocomotion(controller, clips);
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
            AddGestureLayer(controller, "UpperArms", armsMask, UpperBodyGestures, clips, returnToEmpty: true);
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

        private static BlendTree BuildLocomotion(AnimatorController controller, Dictionary<string, AnimationClip> clips)
        {
            // The moving state contains no Idle weight. Magnitude controls cadence
            // in code; unit direction controls phase-aligned foot placement here.
            var gait = new BlendTree { name = "Locomotion", blendType = BlendTreeType.Simple1D,
                blendParameter = "gaitBlend", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(gait, controller);
            foreach (string prefix in new[] { "Walk", "Run" })
            {
                var direction = new BlendTree { name = prefix + " directions", blendType = BlendTreeType.SimpleDirectional2D,
                    blendParameter = "rightSpeed", blendParameterY = "forwardSpeed", useAutomaticThresholds = false };
                AssetDatabase.AddObjectToAsset(direction, controller);
                foreach (var (title, x, y) in Locomotion)
                    if (title.StartsWith(prefix + " ", StringComparison.Ordinal))
                    {
                        if (!clips.ContainsKey(title)) throw new InvalidOperationException("Missing directional clip " + title);
                        direction.AddChild(clips[title], new Vector2(x, y).normalized);
                    }
                if (direction.children.Length != 8) throw new InvalidOperationException("Expected eight " + prefix + " directions");
                gait.AddChild(direction, prefix == "Walk" ? 0f : 1f);
            }
            return gait;
        }

        private static AnimationClip ImportClip(ClipEntry data, string source, Avatar avatar, StringBuilder log)
        {
            // Preserve the exact clips used by the working bundle. Reimporting current
            // FBX sources also changes twelve unrelated clips; those are a separate pass.
            if (data.title != "Arc Bolt left" && data.title != "Arc Bolt right")
            {
                string file = Path.GetFileNameWithoutExtension(data.file) + ".anim";
                var frozen = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    "Assets/HollowSaint/GameFoundation07/Clips/" + file);
                if (!frozen) throw new InvalidOperationException("Missing frozen bundle07 clip " + file);
                var retained = UnityEngine.Object.Instantiate(frozen);
                AssetDatabase.CreateAsset(retained, Folder + "/Clips/" + file);
                log.AppendLine("retained07 " + data.title);
                return retained;
            }
            string path = source + data.file;
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
            var imported = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
            var clip = UnityEngine.Object.Instantiate(imported);
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
            var built = new Dictionary<string, AnimatorState>();
            foreach (string title in states)
            {
                if (!clips.ContainsKey(title))
                {
                    if (Optional.Contains(title)) continue;
                    throw new InvalidOperationException("Missing gesture clip " + title);
                }
                var state = machine.AddState(title);
                state.motion = clips[title];
                state.writeDefaultValues = false;
                if (!FixedRate.Contains(title))
                {
                    state.speedParameter = layerName == "Overlay" ? "overlaySpeed" : layerName == "Halo" ? "haloSpeed" : "attackSpeed";
                    state.speedParameterActive = true;
                }
                built[title] = state;
            }
            foreach (var pair in built)
            {
                string next;
                if (Chains.TryGetValue(pair.Key, out next) && built.ContainsKey(next))
                {
                    var chain = pair.Value.AddTransition(built[next]);
                    chain.hasExitTime = true;
                    chain.exitTime = ChainExitTime;
                    chain.hasFixedDuration = true;
                    chain.duration = ChainFade;
                    continue;
                }
                // A held loop waits for code (Open Circuit end); everything else returns.
                if (!returnToEmpty || pair.Value.motion is AnimationClip clip && clip.isLooping) continue;
                var exit = pair.Value.AddTransition(empty);
                exit.hasExitTime = true;
                exit.exitTime = GestureExitTime;
                exit.hasFixedDuration = true;
                exit.duration = GestureExitFade;
            }
            // Open Circuit cast flows into its recall only when the code asks; no automatic chain.
        }

        private static void Verify(AnimatorController controller, Dictionary<string, AnimationClip> clips, StringBuilder log)
        {
            var layers = controller.layers;
            if (layers.Length != 5) throw new InvalidOperationException("Expected 5 layers, got " + layers.Length);
            if (layers[4].name != "UpperArms") throw new InvalidOperationException("Layer 4 must be UpperArms");
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
