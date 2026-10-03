using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    public static class ProbeAssets
    {
        public const string Source = "Assets/HollowSaint/Source/v31_probe03/";
        public const string Generated = "Assets/HollowSaint/Generated03/";
        public static ProbeManifest Manifest;

        public static AnimatorController Import()
        {
            Manifest = JsonUtility.FromJson<ProbeManifest>(File.ReadAllText(Source + "export-manifest.json"));
            Directory.CreateDirectory(Generated + "Materials");
            Directory.CreateDirectory(Generated + "Clips");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var materials = Manifest.materials.Select(BuildMaterial).ToArray();
            string path = Source + Manifest.model;
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            Configure(importer, false);
            for (int i = 0; i < materials.Length; i++)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), Manifest.materials[i].name.Replace('|', '_')), materials[i]);
            importer.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().Single();
            Require(avatar.isValid, "Imported Generic avatar is invalid");
            string controllerPath = Generated + "HollowSaintPreview.controller";
            Require(!File.Exists(controllerPath), "Generated controller already exists; preserve it before rebuilding");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            foreach (var data in Manifest.clips)
            {
                AnimationClip clip = ImportClip(data, avatar);
                var state = controller.layers[0].stateMachine.AddState(data.title);
                state.motion = clip;
                state.writeDefaultValues = true;
                if (data.title == "Idle") controller.layers[0].stateMachine.defaultState = state;
            }
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void Configure(ModelImporter importer, bool animation)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = animation;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            importer.preserveHierarchy = true;
            importer.isReadable = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.globalScale = 1;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        }

        private static AnimationClip ImportClip(ProbeClip data, Avatar avatar)
        {
            string path = Source + data.file;
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            Configure(importer, true);
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            importer.SaveAndReimport();
            var settings = importer.defaultClipAnimations;
            Require(settings.Length == 1, "Expected one take: " + path);
            settings[0].name = data.title;
            settings[0].loopTime = data.loop;
            settings[0].loopPose = false;
            importer.clipAnimations = settings;
            importer.SaveAndReimport();
            var source = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
            var clip = UnityEngine.Object.Instantiate(source);
            clip.name = data.title;
            foreach (var curve in data.curves)
            {
                var keys = curve.values.Select((v, i) => new Keyframe(i / (float)data.fps, v)).ToArray();
                var animation = new AnimationCurve(keys);
                for (int i = 0; i < keys.Length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(animation, i, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(animation, i, AnimationUtility.TangentMode.Linear);
                }
                AnimationUtility.SetEditorCurve(clip,
                    EditorCurveBinding.FloatCurve("", typeof(HollowSaintPreviewSignals), curve.name), animation);
            }
            AnimationUtility.SetAnimationEvents(clip, data.markers.Select(m => new AnimationEvent
            {
                functionName = "OnClipMarker", stringParameter = m.name,
                time = (m.frame - data.start) / (float)data.fps
            }).ToArray());
            string output = Generated + "Clips/" + Path.GetFileNameWithoutExtension(data.file) + ".anim";
            Require(!File.Exists(output), "Clip already exists: " + output);
            AssetDatabase.CreateAsset(clip, output);
            Require(Mathf.Abs(clip.length - (data.end - data.start) / (float)data.fps) < 0.002f,
                "Clip duration mismatch: " + data.title + " = " + clip.length);
            return clip;
        }

        private static Material BuildMaterial(ProbeMaterial data)
        {
            var material = new Material(Shader.Find("Standard")) { name = data.name };
            material.color = new Color(data.color[0], data.color[1], data.color[2], data.color[3]);
            material.SetFloat("_Metallic", data.metallic);
            material.SetFloat("_Glossiness", 1 - data.roughness);
            if (!string.IsNullOrEmpty(data.baseMap))
            {
                material.color = Color.white;
                material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Source + data.baseMap);
            }
            Color emission = new Color(data.emission[0], data.emission[1], data.emission[2]) * data.emissionStrength;
            if (!string.IsNullOrEmpty(data.emissionMap))
            {
                material.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Source + data.emissionMap));
                emission = Color.white;
            }
            if (emission.maxColorComponent > 0)
            {
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            string safe = string.Concat(data.name.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
            string path = Generated + "Materials/" + safe + ".mat";
            Require(!File.Exists(path), "Material already exists: " + path);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
