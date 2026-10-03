using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    // Preserve the rig and controller; give each body submesh its own renderer so
    // RoR2's one-default-material skin path can address every authored surface.
    public static class FoundationBundleBuilder13
    {
        internal const string Folder = "Assets/HollowSaint/GameFoundation13/";
        public static void RunBatch()
        {
            GameObject model = null;
            try
            {
                if (Directory.Exists(Folder)) throw new InvalidOperationException("Preserve existing " + Folder);
                Directory.CreateDirectory(Folder); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HollowSaint/GameFoundation12/mdlHollowSaint.prefab"));
                model.name = "mdlHollowSaint";
                var body = model.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r => r.sharedMaterials.Length > 1);
                var mesh = body.sharedMesh; var materials = body.sharedMaterials;
                if (mesh.subMeshCount != 4 || materials.Length != 4 || mesh.blendShapeCount != 0)
                    throw new InvalidOperationException("Unexpected body mesh layout");
                int vertices = 0, indices = 0;
                for (int slot = 0; slot < 4; slot++)
                {
                    var split = Split(mesh, slot); vertices += split.vertexCount; indices += split.triangles.Length;
                    AssetDatabase.CreateAsset(split, Folder + "BodySurface" + slot + ".asset");
                    var r = slot == 0 ? body : UnityEngine.Object.Instantiate(body, body.transform.parent);
                    if (slot != 0) r.name = "HS_BodySurface" + slot; // New siblings; the original path stays intact.
                    r.sharedMesh = split; r.sharedMaterials = new[] { materials[slot] };
                }
                MakeAtlases();
                PrefabUtility.SaveAsPrefabAsset(model, Folder + "mdlHollowSaint.prefab"); AssetDatabase.SaveAssets();
                string output = Path.GetFullPath("../artifacts/foundation/bundle13"); Directory.CreateDirectory(output);
                var assets = new List<string> { Folder + "mdlHollowSaint.prefab", "Assets/HollowSaint/GameFoundation11/mdlConduitSpear.prefab" };
                assets.AddRange(Directory.GetFiles(Folder, "*.asset").Where(p => Path.GetFileName(p).StartsWith("HS_Body", StringComparison.Ordinal)).Select(p => p.Replace('\\', '/')));
                var result = BuildPipeline.BuildAssetBundles(output, new[] { new AssetBundleBuild { assetBundleName = "hollowsaintassets", assetNames = assets.ToArray() } }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
                if (!result) throw new InvalidOperationException("Bundle build returned no manifest");
                File.WriteAllText(Path.GetFullPath("../artifacts/foundation/bundle13-report.txt"), "SUCCESS\nbody vertices before=" + mesh.vertexCount + " after=" + vertices + " indices=" + indices + "\nFour compact surfaces; original rig/controller, spear and clips retained. Six skin diffuse atlases and one neutral emission mask.\n");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); File.WriteAllText(Path.GetFullPath("../artifacts/foundation/bundle13-report.txt"), "FAILED\n" + error); EditorApplication.Exit(1); }
            finally { if (model) UnityEngine.Object.DestroyImmediate(model); }
        }

        private static Mesh Split(Mesh source, int slot)
        {
            var oldIndices = source.GetTriangles(slot); var ids = oldIndices.Distinct().OrderBy(i => i).ToArray();
            var map = ids.Select((old, index) => new { old, index }).ToDictionary(x => x.old, x => x.index);
            var mesh = new Mesh { name = "HS_BodySurface" + slot, indexFormat = source.indexFormat };
            var vertices = source.vertices; var normals = source.normals; var tangents = source.tangents; var weights = source.boneWeights;
            mesh.vertices = ids.Select(i => vertices[i]).ToArray();
            if (normals.Length > 0) mesh.normals = ids.Select(i => normals[i]).ToArray();
            if (tangents.Length > 0) mesh.tangents = ids.Select(i => tangents[i]).ToArray();
            mesh.boneWeights = ids.Select(i => weights[i]).ToArray(); mesh.bindposes = source.bindposes;
            var colors = source.colors; if (colors.Length > 0) mesh.colors = ids.Select(i => colors[i]).ToArray();
            for (int channel = 0; channel < 8; channel++)
            {
                var uv = new List<Vector4>(); source.GetUVs(channel, uv);
                if (uv.Count > 0) mesh.SetUVs(channel, ids.Select(i => uv[i]).ToList());
            }
            mesh.triangles = oldIndices.Select(i => map[i]).ToArray(); mesh.bounds = source.bounds;
            return mesh;
        }

        private static Color[] Pixels(string file)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!t.LoadImage(File.ReadAllBytes(file))) throw new InvalidOperationException("Cannot decode " + file);
            var pixels = t.GetPixels(); UnityEngine.Object.DestroyImmediate(t); return pixels;
        }

        private static void MakeAtlases()
        {
            const string source = "Assets/HollowSaint/Source/v31_probe03/";
            var original = AssetDatabase.LoadAssetAtPath<Texture2D>(source + "body_base.png");
            var diffuse = Pixels(source + "body_base.png"); var emission = Pixels(source + "body_emission.png");
            var neutral = emission.Select(c => new Color(c.maxColorComponent, c.maxColorComponent, c.maxColorComponent, c.a)).ToArray();
            Save("HS_BodyLightMask", original.width, original.height, neutral);
            Color[] arc = { new Color(0.45f, 1f, 0.72f), new Color(1f, 0.72f, 0.22f), new Color(0.75f, 0.35f, 1f) };
            Color[,] armor = { { new Color(0.18f, 0.22f, 0.17f), new Color(0.58f, 0.4f, 0.2f) },
                { new Color(0.25f, 0.21f, 0.16f), new Color(0.85f, 0.65f, 0.27f) },
                { new Color(0.12f, 0.1f, 0.18f), new Color(0.07f, 0.055f, 0.12f) } };
            for (int theme = 0; theme < 3; theme++) for (int plate = 0; plate < 2; plate++)
            {
                var result = new Color[diffuse.Length];
                for (int i = 0; i < result.Length; i++)
                {
                    Color c = diffuse[i];
                    // Bake in linear light, matching Standard's sRGB texture/color inputs.
                    Color tinted = (c.linear * armor[theme, plate].linear).gamma;
                    bool cyan = c.g > c.r * 1.25f && c.b > c.r * 1.25f;
                    if (cyan && emission[i].maxColorComponent > 1f / 255f)
                        tinted = (new Color(c.maxColorComponent, c.maxColorComponent, c.maxColorComponent).linear * arc[theme].linear).gamma;
                    tinted.a = c.a; result[i] = tinted;
                }
                Save("HS_BodySkin" + theme + "_" + plate, original.width, original.height, result);
            }
        }

        private static void Save(string name, int width, int height, Color[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true, false) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            texture.SetPixels(pixels); texture.Apply(true, true);
            AssetDatabase.CreateAsset(texture, Folder + name + ".asset");
        }
    }
}
