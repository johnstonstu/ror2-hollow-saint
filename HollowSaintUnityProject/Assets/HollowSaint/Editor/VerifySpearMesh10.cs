using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    /// <summary>Export actual imported hand/grip surfaces for an independent intersection check.</summary>
    public static class VerifySpearMesh10
    {
        [Serializable] private class Part { public string name; public Vector3[] vertices; public int[] triangles; public Matrix4x4 worldToLocal; }
        [Serializable] private class Geometry { public Part[] hand, grip; public int fixedFingerSamples; }
        private static string Folder = "Assets/HollowSaint/GameFoundation10r1/";
        private static string ReportName = "spear10-imported-geometry.json";
        private static bool fromBundle;
        public static void RunFinalBatch()
        {
            Folder = "Assets/HollowSaint/GameFoundation11/";
            ReportName = "spear11-animated-imported-geometry.json";
            fromBundle = true;
            RunBatch();
        }
        public static void RunBatch()
        {
            GameObject model = null, spear = null;
            AssetBundle bundle = null;
            try
            {
                if (fromBundle)
                {
                    bundle = AssetBundle.LoadFromFile(Path.GetFullPath("../artifacts/foundation/bundle11/hollowsaintassets"));
                    if (!bundle) throw new InvalidOperationException("Final bundle failed to load");
                }
                var modelAsset = fromBundle ? bundle.LoadAsset<GameObject>("mdlHollowSaint") : AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "mdlHollowSaint.prefab");
                model = UnityEngine.Object.Instantiate(modelAsset);
                var transforms = model.GetComponentsInChildren<Transform>(true);
                var socket = transforms.Single(t => t.name == "SpearGripSocket");
                var spearAsset = fromBundle ? bundle.LoadAsset<GameObject>("mdlConduitSpear") : AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "mdlConduitSpear.prefab");
                spear = UnityEngine.Object.Instantiate(spearAsset, socket, false);
                var animator = model.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var fingers = transforms.Where(t => new[] { "R index.", "R middle.", "R ring.", "R little.", "R thumb." }.Any(t.name.StartsWith)).ToArray();
                if (fingers.Length != 15) throw new InvalidOperationException("Expected all 15 right finger bones");
                void Sample(string title, float phase)
                {
                    animator.Rebind(); animator.Update(0);
                    animator.Play("Idle combat", 0, 0);
                    animator.Play(title, 5, phase); animator.Update(0);
                }
                Sample("Spear held", 0);
                var grasp = fingers.Select(f => f.localRotation).ToArray();
                int samples = 0;
                foreach (string title in new[] { "Spear held", "Spear crown held", "Spear fan start", "Spear fan loop", "Spear fan end" })
                foreach (float phase in new[] { 0f, 0.25f, 0.5f, 0.75f, 0.99f })
                {
                    Sample(title, phase);
                    for (int i = 0; i < fingers.Length; i++)
                    {
                        if (Quaternion.Angle(grasp[i], fingers[i].localRotation) > 0.05f) throw new InvalidOperationException("Grip changed: " + title + "/" + fingers[i].name);
                        samples++;
                    }
                }
                Sample("Spear held", 0);
                var hand = model.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("R HAND |", StringComparison.Ordinal)).Select(Mesh).ToArray();
                var grip = spear.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith("Custom grip", StringComparison.Ordinal)).Select(Mesh).ToArray();
                if (hand.Length < 16 || grip.Length != 1) throw new InvalidOperationException("Hand/grip geometry inventory incorrect: " + hand.Length + "/" + grip.Length);
                var geometry = new Geometry { hand = hand, grip = grip, fixedFingerSamples = samples };
                File.WriteAllText(Path.GetFullPath("../artifacts/foundation/" + ReportName), JsonUtility.ToJson(geometry));
                Debug.Log("SPEAR_GEOMETRY_EXPORTED hand=" + hand.Length + " fingerSamples=" + samples);
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
            finally { if (spear) UnityEngine.Object.DestroyImmediate(spear); if (model) UnityEngine.Object.DestroyImmediate(model); if (bundle) bundle.Unload(true); }
        }
        private static Part Mesh(Renderer renderer)
        {
            Mesh mesh = null;
            bool baked = renderer is SkinnedMeshRenderer;
            if (baked) { mesh = new Mesh(); ((SkinnedMeshRenderer)renderer).BakeMesh(mesh); }
            else mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            var result = new Part { name = renderer.name, vertices = mesh.vertices.Select(renderer.transform.TransformPoint).ToArray(), triangles = mesh.triangles, worldToLocal = renderer.transform.worldToLocalMatrix };
            if (baked) UnityEngine.Object.DestroyImmediate(mesh);
            return result;
        }
    }
}
