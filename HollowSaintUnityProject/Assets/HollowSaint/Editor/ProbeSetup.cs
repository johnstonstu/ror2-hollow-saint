using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HollowSaint.Preview.Editor
{
    public static class ProbeSetup
    {
        public const string ScenePath = "Assets/HollowSaint/Scenes/ImportProof04.unity";
        public static string Evidence => Path.GetFullPath("../artifacts/unity-setup/proof04");

        public static void FinishPresentation()
        {
            try
            {
                Directory.CreateDirectory(Evidence);
                EditorSceneManager.OpenScene("Assets/HollowSaint/Scenes/ImportProof03.unity");
                ProbeAssets.Manifest = JsonUtility.FromJson<ProbeManifest>(File.ReadAllText(ProbeAssets.Source + "export-manifest.json"));
                foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { ProbeAssets.Generated }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (!material.HasProperty("_EmissionColor") || material.GetColor("_EmissionColor").maxColorComponent <= 0) continue;
                    File.Copy(path, Path.Combine(Evidence, Path.GetFileName(path) + ".before"));
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                    material.EnableKeyword("_EMISSION");
                    EditorUtility.SetDirty(material);
                }
                GameObject.Find("Key light").transform.rotation = Quaternion.Euler(35, 155, 0);
                var fill = new GameObject("Soft fill").AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.65f;
                fill.transform.rotation = Quaternion.Euler(25, -60, 0);
                var reflection = new Cubemap(16, TextureFormat.RGB24, false);
                var pixels = Enumerable.Repeat(new Color(0.35f, 0.38f, 0.42f), 256).ToArray();
                for (int face = 0; face < 6; face++) reflection.SetPixels(pixels, (CubemapFace)face);
                reflection.Apply();
                AssetDatabase.CreateAsset(reflection, ProbeAssets.Generated + "StudioReflection.cubemap");
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflection = reflection;
                AssetDatabase.SaveAssets();
                var motor = UnityEngine.Object.FindObjectOfType<HollowSaintPreviewMotor>();
                VerifyAndRender(motor.animator.gameObject, motor.animator, motor.followCamera);
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                AssetDatabase.SaveAssets();
            }
            catch (Exception error) { Debug.LogError("Presentation validation failed: " + error); throw; }
        }

        public static void Build()
        {
            try
            {
                Directory.CreateDirectory(Evidence);
                var controller = ProbeAssets.Import();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GraphicsSettings.renderPipelineAsset = null;
                var player = new GameObject("Hollow Saint Preview Player");
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(ProbeAssets.Source + ProbeAssets.Manifest.model);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
                model.transform.SetParent(player.transform, false);
                var animator = model.GetComponent<Animator>();
                ProbeAssets.Require(animator != null, "Model has no Animator");
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                model.AddComponent<HollowSaintPreviewSignals>();
                var body = player.AddComponent<CharacterController>();
                body.height = 2.0f;
                body.radius = 0.3f;
                body.center = Vector3.up;
                body.stepOffset = 0.25f;
                var motor = player.AddComponent<HollowSaintPreviewMotor>();
                motor.animator = animator;
                motor.clipNames = ProbeAssets.Manifest.clips.Select(c => c.title).ToArray();
                BuildStage();
                motor.followCamera = BuildCamera();
                VerifyAndRender(model, animator, motor.followCamera);
                Directory.CreateDirectory("Assets/HollowSaint/Scenes");
                Directory.CreateDirectory("Assets/HollowSaint/Prefabs");
                PrefabUtility.SaveAsPrefabAsset(player, "Assets/HollowSaint/Prefabs/HollowSaintPreview03.prefab");
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                AssetDatabase.SaveAssets();
                Debug.Log("HOLLOW_SAINT_IMPORT_PROOF_COMPLETE");
            }
            catch (Exception error)
            {
                Debug.LogError("Hollow Saint import proof failed: " + error);
                throw;
            }
        }

        private static void BuildStage()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Movement test floor";
            floor.transform.localScale = Vector3.one * 20;
            var material = new Material(Shader.Find("Standard")) { color = new Color(0.12f, 0.14f, 0.17f) };
            material.SetFloat("_Glossiness", 0.15f);
            AssetDatabase.CreateAsset(material, ProbeAssets.Generated + "Floor.mat");
            floor.GetComponent<Renderer>().sharedMaterial = material;
            var light = new GameObject("Key light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.transform.rotation = Quaternion.Euler(40, -30, 0);
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.65f);
        }

        private static Camera BuildCamera()
        {
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(3.2f, 2.3f, 4.3f);
            camera.transform.LookAt(Vector3.up * 1.3f);
            camera.fieldOfView = 35;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.085f);
            return camera;
        }

        private static void VerifyAndRender(GameObject model, Animator animator, Camera camera)
        {
            var bones = model.GetComponentsInChildren<Transform>().ToDictionary(t => t.name, t => t);
            foreach (string name in ProbeAssets.Manifest.bones)
                ProbeAssets.Require(bones.ContainsKey(name), "Bone lost in FBX import: " + name);
            var renderers = model.GetComponentsInChildren<Renderer>();
            ProbeAssets.Require(renderers.Length == ProbeAssets.Manifest.meshCount, "Exported mesh count changed");
            ProbeAssets.Require(renderers.All(r => r.sharedMaterials.All(m => m != null && m.shader != null)), "Missing material");
            ProbeAssets.Require(renderers.All(r => r.sharedMaterials.All(m => AssetDatabase.GetAssetPath(m).StartsWith(ProbeAssets.Generated))), "Unmapped FBX material");
            var report = new List<string> { "Unity " + Application.unityVersion, "Meshes: " + renderers.Length,
                "Bones retained: " + ProbeAssets.Manifest.bones.Length };
            foreach (var data in ProbeAssets.Manifest.clips)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ProbeAssets.Generated + "Clips/" + Path.GetFileNameWithoutExtension(data.file) + ".anim");
                ProbeAssets.Require(AnimationUtility.GetAnimationEvents(clip).Length == data.markers.Length, "Event count: " + data.title);
                float error = 0;
                foreach (var sample in data.samples)
                {
                    clip.SampleAnimation(model, (sample.frame - data.start) / (float)data.fps);
                    foreach (var bone in sample.bones)
                    {
                        Vector3 expected = new Vector3(-bone.position[0], bone.position[2], -bone.position[1]);
                        error = Mathf.Max(error, Vector3.Distance(model.transform.InverseTransformPoint(bones[bone.name].position), expected));
                    }
                }
                report.Add(data.title + ": duration=" + clip.length.ToString("F4") + ", socket/hem error=" + error.ToString("F6") + "m");
                ProbeAssets.Require(error < 0.001f, "Socket or sash position differs from Blender: " + data.title);
                clip.SampleAnimation(model, Mathf.Min(0.2f, clip.length * 0.5f));
                if (data.title == "Idle" || data.title == "Run forward" || data.title == "Glide loop" || data.title == "Arc Bolt right")
                    Render(camera, Path.GetFileNameWithoutExtension(data.file));
            }
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(ProbeAssets.Generated + "Clips/Idle.anim");
            idle.SampleAnimation(model, 0);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            report.Add("Bounds: " + bounds);
            File.WriteAllLines(Path.Combine(Evidence, "import-verification.txt"), report);
            ProbeAssets.Require(bounds.size.y > 1.5f && bounds.size.y < 4, "Model scale is incorrect: " + bounds);
        }

        private static void Render(Camera camera, string name)
        {
            var target = new RenderTexture(800, 1000, 24);
            var old = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(800, 1000, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 800, 1000), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(Evidence, name + ".png"), image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = old;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
