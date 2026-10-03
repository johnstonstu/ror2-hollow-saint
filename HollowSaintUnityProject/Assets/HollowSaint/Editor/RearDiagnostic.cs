using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowSaint.Preview.Editor
{
    [InitializeOnLoad]
    public static class RearDiagnostic
    {
        private const string Prefab = "Assets/HollowSaint/GameFoundation01/mdlHollowSaint.prefab";
        private static string Output => Environment.GetEnvironmentVariable("HS_REAR_DIAGNOSTIC_OUTPUT")
            ?? Path.GetFullPath("../artifacts/foundation/playtest-20260927");
        private static string Request => Path.Combine(Output, "rear-diagnostic.request");

        static RearDiagnostic() { EditorApplication.update += CheckRequest; }

        private static void CheckRequest()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Move(Request, Request + ".started");
            try { Capture(); }
            catch (Exception error)
            {
                File.WriteAllText(Path.Combine(Output, "rear-diagnostic-error.txt"), error.ToString());
                Debug.LogError("Hollow Saint rear diagnostic failed: " + error);
            }
        }

        public static void Capture()
        {
            string bundlePath = Environment.GetEnvironmentVariable("HS_REAR_DIAGNOSTIC_BUNDLE");
            AssetBundle bundle = string.IsNullOrEmpty(bundlePath) ? null : AssetBundle.LoadFromFile(bundlePath);
            if (!string.IsNullOrEmpty(bundlePath) && !bundle)
                throw new InvalidOperationException("Could not load diagnostic asset bundle: " + bundlePath);
            var source = bundle ? bundle.LoadAsset<GameObject>("mdlHollowSaint")
                : AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            if (!source) throw new InvalidOperationException("Foundation model prefab is missing");
            Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                var model = bundle ? UnityEngine.Object.Instantiate(source)
                    : (GameObject)PrefabUtility.InstantiatePrefab(source, preview);
                var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single(r => r.name.StartsWith("HF BODY", StringComparison.Ordinal));
                File.WriteAllText(Path.Combine(Output, "rear-diagnostic.txt"),
                    "Body enabled=" + body.enabled + " active=" + body.gameObject.activeInHierarchy + "\n" +
                    "UpdateWhenOffscreen=" + body.updateWhenOffscreen + "\n" +
                    "LocalBounds=" + body.localBounds + " MeshBounds=" + body.sharedMesh.bounds + "\n" +
                    "WorldBounds=" + body.bounds + " BodyMatrix=" + body.localToWorldMatrix + "\n" +
                    "Vertices=" + body.sharedMesh.vertexCount + " Triangles=" +
                    Enumerable.Range(0, body.sharedMesh.subMeshCount).Sum(i => body.sharedMesh.GetTriangles(i).Length / 3) + "\n" +
                    "Materials=" + string.Join(", ", body.sharedMaterials.Select(m => m ? m.name + "/" + m.shader.name : "null")) + "\n");
                Vector3 center = body.bounds.center;
                if (Environment.GetEnvironmentVariable("HS_REAR_SPLIT") == "1")
                    HollowSaint.FoundationMeshSplitter.Split(body);
                if (Environment.GetEnvironmentVariable("HS_REAR_SINGLE_MATERIAL") == "1")
                    foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                        renderer.sharedMaterials = new[] { renderer.sharedMaterial };

                var lightObject = new GameObject("Diagnostic Light");
                SceneManager.MoveGameObjectToScene(lightObject, preview);
                lightObject.transform.rotation = Quaternion.Euler(40, 20, 0);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2f;

                CaptureSide(preview, center, -4f, "unity-rear.png");
                CaptureSide(preview, center, 4f, "unity-front.png");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (bundle) bundle.Unload(true);
            }
        }

        private static void CaptureSide(Scene preview, Vector3 center, float z, string filename)
        {
            var cameraObject = new GameObject("Diagnostic Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            var camera = cameraObject.AddComponent<Camera>();
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.12f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.orthographic = true;
            camera.orthographicSize = 1.5f;
            camera.cullingMask = -1;
            camera.transform.position = center + new Vector3(0, 0, z);
            camera.transform.LookAt(center);
            var target = new RenderTexture(800, 1000, 24);
            var image = new Texture2D(800, 1000, TextureFormat.RGB24, false);
            var prior = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 800, 1000), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(Output, filename), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = prior;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
