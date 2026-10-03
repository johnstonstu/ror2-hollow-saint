using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    /// <summary>Keep the verified v37 rig/animations; relieve imported grip contacts.</summary>
    public static class FoundationBundleBuilder11
    {
        private const string Previous = "Assets/HollowSaint/GameFoundation10r1/";
        private const string Folder = "Assets/HollowSaint/GameFoundation11/";
        [Serializable] private class Grip { public Vector3[] vertices; public int[] triangles; }
        public static void RunBatch()
        {
            GameObject model = null, spear = null;
            var report = Path.GetFullPath("../artifacts/foundation/bundle11-report.txt");
            try
            {
                if (Directory.Exists(Folder)) throw new InvalidOperationException("Preserve " + Folder);
                Directory.CreateDirectory(Folder);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Previous + "mdlHollowSaint.prefab"));
                spear = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Previous + "mdlConduitSpear.prefab"));
                model.name = "mdlHollowSaint"; spear.name = "mdlConduitSpear";
                var grip = JsonUtility.FromJson<Grip>(File.ReadAllText(Path.GetFullPath("../art/concepts/spear-runtime-fit01/grip-local.json")));
                if (grip.vertices.Length != 1602 || grip.triangles.Length != 3204 * 3) throw new InvalidOperationException("Unexpected refitted mesh inventory");
                var renderer = spear.GetComponentsInChildren<MeshRenderer>().Single(r => r.name.StartsWith("Custom grip", StringComparison.Ordinal));
                var mesh = new Mesh { name = "HS_ImportedHandGrip", vertices = grip.vertices, triangles = grip.triangles };
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, Folder + "ImportedHandGrip.asset");
                renderer.GetComponent<MeshFilter>().sharedMesh = mesh;
                var staticPulse = spear.GetComponentsInChildren<Transform>().Single(t => t.name.StartsWith("Pulse entering spear", StringComparison.Ordinal));
                UnityEngine.Object.DestroyImmediate(staticPulse.gameObject);
                if (spear.GetComponentsInChildren<MeshRenderer>().Length != 14) throw new InvalidOperationException("Unexpected final weapon parts");
                var oldController = AssetDatabase.LoadAssetAtPath<GameObject>(Previous + "mdlHollowSaint.prefab").GetComponent<Animator>().runtimeAnimatorController;
                if (model.GetComponent<Animator>().runtimeAnimatorController != oldController) throw new InvalidOperationException("Animation controller changed");
                PrefabUtility.SaveAsPrefabAsset(model, Folder + "mdlHollowSaint.prefab");
                PrefabUtility.SaveAsPrefabAsset(spear, Folder + "mdlConduitSpear.prefab");
                AssetDatabase.SaveAssets();
                string output = Path.GetFullPath("../artifacts/foundation/bundle11");
                Directory.CreateDirectory(output);
                var result = BuildPipeline.BuildAssetBundles(output, new[] { new AssetBundleBuild {
                    assetBundleName = "hollowsaintassets", assetNames = new[] { Folder + "mdlHollowSaint.prefab", Folder + "mdlConduitSpear.prefab" }
                } }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
                if (!result) throw new InvalidOperationException("Bundle build returned no manifest");
                File.WriteAllText(report, "SUCCESS\nVerified v37 controller unchanged; fitted native grip 1602 vertices / 3204 triangles.\n14 weapon parts; prototype static pulse removed.\n");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { File.WriteAllText(report, "FAILED\n" + error); Debug.LogException(error); EditorApplication.Exit(1); }
            finally { if (spear) UnityEngine.Object.DestroyImmediate(spear); if (model) UnityEngine.Object.DestroyImmediate(model); }
        }
    }
}
