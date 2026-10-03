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
    /// <summary>Retain bundle09, replace only spear clips; add a hand-owned carry layer.</summary>
    public static class FoundationBundleBuilder10
    {
        private const string Previous = "Assets/HollowSaint/GameFoundation09";
        private const string Folder = "Assets/HollowSaint/GameFoundation10r1";
        private const string Source = "Assets/HollowSaint/Source/v37_clips01/";
        private const string SpearSource = "Assets/HollowSaint/Source/spear02/ConduitSpear.fbx";
        private const string ModelSource = "Assets/HollowSaint/Source/v31_probe03/HollowSaint.fbx";
        private static readonly string Output = Path.GetFullPath("../artifacts/foundation/bundle10r1");
        private static readonly string Report = Path.GetFullPath("../artifacts/foundation/bundle10r1-report.txt");
        [Serializable] private class Manifest { public Entry[] clips; }
        [Serializable] private class Entry { public string title, file; public int start, end, fps; public bool loop; }

        public static void RunBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception error)
            {
                File.WriteAllText(Report, "FAILED\n" + error);
                Debug.LogError("Bundle10: " + error);
                EditorApplication.Exit(1);
            }
        }

        public static void Build()
        {
            if (Directory.Exists(Folder)) throw new InvalidOperationException("Preserve " + Folder);
            var log = new StringBuilder();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Source + "clips-manifest.json")).clips;
            var changes = new HashSet<string>(entries.Select(e => e.title));
            if (changes.Count != 8) throw new InvalidOperationException("Expected eight spear clips");
            Directory.CreateDirectory(Folder + "/Clips");
            AssetDatabase.Refresh();
            var clips = new Dictionary<string, AnimationClip>();
            foreach (string path in Directory.GetFiles(Previous + "/Clips", "*.anim"))
            {
                var previous = AssetDatabase.LoadAssetAtPath<AnimationClip>(path.Replace('\\', '/'));
                string title = previous.name.Replace('_', ' ');
                if (changes.Contains(title)) continue;
                var copy = UnityEngine.Object.Instantiate(previous);
                copy.name = previous.name;
                AssetDatabase.CreateAsset(copy, Folder + "/Clips/" + Path.GetFileName(path));
                clips.Add(title, copy);
                VerifySame(previous, copy);
            }
            int retained = clips.Count;
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelSource).OfType<Avatar>().Single();
            foreach (var entry in entries) clips.Add(entry.title, Import(entry, avatar));
            if (retained != 66 || clips.Count != 74) throw new InvalidOperationException("Clip inventory changed unexpectedly: " + retained + "/" + clips.Count);
            log.AppendLine("retained09=" + retained + " total=" + clips.Count);
            string controllerPath = Folder + "/Foundation.controller";
            if (!AssetDatabase.CopyAsset(Previous + "/Foundation.controller", controllerPath)) throw new InvalidOperationException("Cannot copy controller");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            var seen = new HashSet<BlendTree>();
            foreach (var layer in controller.layers) Rebind(layer.stateMachine, clips, seen);
            controller.AddParameter(new AnimatorControllerParameter { name = "spearSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1 });
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelSource);
            AddCarry(controller, modelAsset, clips);
            var arms = controller.layers.Single(l => l.name == "UpperArms").stateMachine;
            foreach (string title in new[] { "Spear fan start", "Spear fan loop", "Spear fan end" })
            {
                var state = arms.AddState(title);
                state.motion = clips[title];
                state.writeDefaultValues = false;
                state.speedParameter = "attackSpeed";
                state.speedParameterActive = true;
            }
            BuildModels(modelAsset, controller, log);
            if (controller.layers.Length != 6 || controller.layers[5].name != "SpearCarry") throw new InvalidOperationException("Carry layer missing");
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Output);
            var result = BuildPipeline.BuildAssetBundles(Output, new[] { new AssetBundleBuild {
                assetBundleName = "hollowsaintassets",
                assetNames = new[] { Folder + "/mdlHollowSaint.prefab", Folder + "/mdlConduitSpear.prefab" }
            } }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
            if (!result) throw new InvalidOperationException("No bundle manifest");
            File.WriteAllText(Report, "SUCCESS Unity " + Application.unityVersion + "\n" + log);
        }

        private static AnimationClip Import(Entry data, Avatar avatar)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(Source + data.file);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            importer.preserveHierarchy = true;
            importer.importCameras = importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.globalScale = 1;
            importer.SaveAndReimport();
            var takes = importer.defaultClipAnimations;
            if (takes.Length != 1) throw new InvalidOperationException("Expected one take: " + data.title);
            takes[0].name = data.title;
            takes[0].loopTime = data.loop;
            takes[0].loopPose = false;
            importer.clipAnimations = takes;
            importer.SaveAndReimport();
            var clip = UnityEngine.Object.Instantiate(AssetDatabase.LoadAllAssetsAtPath(Source + data.file)
                .OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)));
            clip.name = data.title;
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                if (binding.type != typeof(Transform)) AnimationUtility.SetEditorCurve(clip, binding, null);
            AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = data.loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            if (Mathf.Abs(clip.length - (data.end - data.start) / (float)data.fps) > 0.002f)
                throw new InvalidOperationException("Duration changed: " + data.title);
            AssetDatabase.CreateAsset(clip, Folder + "/Clips/" + Path.GetFileNameWithoutExtension(data.file) + ".anim");
            return clip;
        }

        private static void Rebind(AnimatorStateMachine machine, Dictionary<string, AnimationClip> clips, HashSet<BlendTree> seen)
        {
            foreach (var state in machine.states) state.state.motion = RebindMotion(state.state.motion, clips, seen);
            foreach (var child in machine.stateMachines) Rebind(child.stateMachine, clips, seen);
        }
        private static Motion RebindMotion(Motion motion, Dictionary<string, AnimationClip> clips, HashSet<BlendTree> seen)
        {
            if (motion is AnimationClip clip) return clips[clip.name.Replace('_', ' ')];
            if (motion is BlendTree tree && seen.Add(tree))
            {
                var children = tree.children;
                for (int i = 0; i < children.Length; i++) children[i].motion = RebindMotion(children[i].motion, clips, seen);
                tree.children = children;
            }
            return motion;
        }
        private static void AddCarry(AnimatorController controller, GameObject model, Dictionary<string, AnimationClip> clips)
        {
            var transforms = model.GetComponentsInChildren<Transform>(true);
            var mask = new AvatarMask { name = "HS_SpearCarry", transformCount = transforms.Length };
            for (int i = 0; i < transforms.Length; i++)
            {
                string n = transforms[i].name;
                mask.SetTransformPath(i, AnimationUtility.CalculateTransformPath(transforms[i], model.transform));
                bool active = n.StartsWith("R ", StringComparison.Ordinal) &&
                    new[] { "shoulder", "scapula", "pauldron", "upperarm", "forearm", "hand", "muzzle", "index", "middle", "ring", "little", "thumb" }.Any(part => n.Substring(2).StartsWith(part, StringComparison.Ordinal));
                mask.SetTransformActive(i, active);
            }
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++) mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            AssetDatabase.CreateAsset(mask, Folder + "/HS_SpearCarry.mask");
            controller.AddLayer("SpearCarry");
            var layers = controller.layers;
            layers[5].avatarMask = mask;
            layers[5].defaultWeight = 1;
            layers[5].blendingMode = AnimatorLayerBlendingMode.Override;
            controller.layers = layers;
            var machine = layers[5].stateMachine;
            var empty = machine.AddState("Empty");
            empty.writeDefaultValues = false;
            machine.defaultState = empty;
            var states = new Dictionary<string, AnimatorState>();
            foreach (string title in new[] { "Spear held", "Spear crown held", "Spear catch", "Conduit Spear", "Conduit Spear recover", "Spear fan start", "Spear fan loop", "Spear fan end" })
            {
                var state = machine.AddState(title);
                state.motion = clips[title];
                state.writeDefaultValues = false;
                state.speedParameter = "spearSpeed";
                state.speedParameterActive = title != "Conduit Spear recover";
                states.Add(title, state);
            }
            foreach (var pair in new[] { ("Spear catch", "Spear held"), ("Spear fan end", "Spear held"), ("Conduit Spear", "Conduit Spear recover") })
            {
                var transition = states[pair.Item1].AddTransition(states[pair.Item2]);
                transition.hasExitTime = transition.hasFixedDuration = true;
                transition.exitTime = 0.99f;
                transition.duration = 0.1f;
            }
            var exit = states["Conduit Spear recover"].AddTransition(empty);
            exit.hasExitTime = exit.hasFixedDuration = true;
            exit.exitTime = 0.9f; exit.duration = 0.15f;
        }

        private static Transform Find(GameObject root, string name) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        private static void BuildModels(GameObject modelAsset, AnimatorController controller, StringBuilder log)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(SpearSource);
            importer.optimizeGameObjects = false;
            importer.preserveHierarchy = true;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var calibration = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SpearSource));
            var model = UnityEngine.Object.Instantiate(modelAsset);
            try
            {
                var calibratedHand = Find(calibration, "R hand");
                var hand = Find(model, "R hand");
                if (Vector3.Distance(hand.position, calibratedHand.position) > 0.001f || Quaternion.Angle(hand.rotation, calibratedHand.rotation) > 0.1f)
                    throw new InvalidOperationException("Hand rest axes differ; cannot fit grip");
                var exportedSocket = Find(calibration, "SpearGripSocket");
                if (exportedSocket.parent != calibratedHand) throw new InvalidOperationException("Socket lost bone parent");
                var socket = new GameObject("SpearGripSocket").transform;
                socket.SetParent(hand, false);
                socket.localPosition = exportedSocket.localPosition;
                socket.localRotation = exportedSocket.localRotation;
                socket.localScale = exportedSocket.localScale;
                var spear = UnityEngine.Object.Instantiate(exportedSocket.gameObject);
                try
                {
                    spear.name = "mdlConduitSpear";
                    spear.transform.SetParent(null, false);
                    spear.transform.localPosition = Vector3.zero;
                    spear.transform.localRotation = Quaternion.identity;
                    spear.transform.localScale = Vector3.one;
                    foreach (var renderer in spear.GetComponentsInChildren<Renderer>())
                    {
                        string name = renderer.name;
                        bool energy = name.Contains("energy") || name.Contains("lightning");
                        var material = new Material(Shader.Find("Standard")) { name = energy ? "HS_SpearEnergy" : name.Contains("grip") && !name.Contains("collar") ? "HS_SpearGrip" : "HS_SpearCopper" };
                        material.color = energy ? new Color(0.6f, 0.95f, 1) : material.name == "HS_SpearGrip" ? new Color(0.025f, 0.035f, 0.04f) : new Color(0.62f, 0.36f, 0.20f);
                        material.SetFloat("_Metallic", energy ? 0 : 0.65f);
                        material.SetFloat("_Glossiness", 0.55f);
                        if (energy) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", new Color(0.5f, 0.95f, 1) * 1.4f); }
                        AssetDatabase.CreateAsset(material, Folder + "/" + name.Replace(' ', '_').Replace('.', '_') + ".mat");
                        renderer.sharedMaterial = material;
                    }
                    PrefabUtility.SaveAsPrefabAsset(spear, Folder + "/mdlConduitSpear.prefab");
                    log.AppendLine("spear renderers=" + spear.GetComponentsInChildren<Renderer>().Length + " tip=" + Find(spear, "SpearTip").localPosition);
                }
                finally { UnityEngine.Object.DestroyImmediate(spear); }
                model.name = "mdlHollowSaint";
                model.GetComponent<Animator>().runtimeAnimatorController = controller;
                model.GetComponent<Animator>().applyRootMotion = false;
                PrefabUtility.SaveAsPrefabAsset(model, Folder + "/mdlHollowSaint.prefab");
                log.AppendLine("socket local=" + socket.localPosition + " rotation=" + socket.localEulerAngles);
            }
            finally { UnityEngine.Object.DestroyImmediate(calibration); UnityEngine.Object.DestroyImmediate(model); }
        }

        private static void VerifySame(AnimationClip before, AnimationClip after)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(before))
            {
                var a = AnimationUtility.GetEditorCurve(before, binding).keys;
                var b = AnimationUtility.GetEditorCurve(after, binding).keys;
                if (!a.SequenceEqual(b)) throw new InvalidOperationException("Retained curve changed: " + before.name);
            }
        }
    }
}
