using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    // v0.8: bundle14's prefab/controller with the seven 1024 px skin light atlases rebuilt from the
    // same sources and stored BC7 (about 1/4 of the uncompressed RGBA32 GPU memory).
    public static class FoundationBundleBuilder15
    {
        internal const string Folder = "Assets/HollowSaint/GameFoundation15/";

        public static void RunBatch()
        {
            string reportPath = Path.GetFullPath("../artifacts/foundation/bundle15-report.txt");
            try
            {
                if (Directory.Exists(Folder)) throw new InvalidOperationException("Preserve existing " + Folder);
                Directory.CreateDirectory(Folder); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                long before = 0, after = 0;
                MakeAtlases(ref before, ref after);
                AssetDatabase.SaveAssets();
                string output = Path.GetFullPath("../artifacts/foundation/bundle15"); Directory.CreateDirectory(output);
                var assets = new List<string> { FoundationBundleBuilder14.Folder + "mdlHollowSaint.prefab", "Assets/HollowSaint/GameFoundation11/mdlConduitSpear.prefab" };
                assets.AddRange(Directory.GetFiles(Folder, "*.asset").Select(p => p.Replace('\\', '/')));
                var result = BuildPipeline.BuildAssetBundles(output, new[] { new AssetBundleBuild { assetBundleName = "hollowsaintassets", assetNames = assets.ToArray() } }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
                if (!result) throw new InvalidOperationException("Bundle build returned no manifest");
                File.WriteAllText(reportPath, "SUCCESS\natlas GPU bytes RGBA32=" + before + " BC7=" + after + "\nprefab/controller from GameFoundation14\n");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); File.WriteAllText(reportPath, "FAILED\n" + error); EditorApplication.Exit(1); }
        }

        private static Color[] Pixels(string file)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!t.LoadImage(File.ReadAllBytes(file))) throw new InvalidOperationException("Cannot decode " + file);
            var pixels = t.GetPixels(); UnityEngine.Object.DestroyImmediate(t); return pixels;
        }

        // Same bake as FoundationBundleBuilder13.MakeAtlases (kept identical on purpose).
        private static void MakeAtlases(ref long before, ref long after)
        {
            const string source = "Assets/HollowSaint/Source/v31_probe03/";
            var original = AssetDatabase.LoadAssetAtPath<Texture2D>(source + "body_base.png");
            var diffuse = Pixels(source + "body_base.png"); var emission = Pixels(source + "body_emission.png");
            var neutral = emission.Select(c => new Color(c.maxColorComponent, c.maxColorComponent, c.maxColorComponent, c.a)).ToArray();
            Save("HS_BodyLightMask", original.width, original.height, neutral, ref before, ref after);
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
                    Color tinted = (c.linear * armor[theme, plate].linear).gamma;
                    bool cyan = c.g > c.r * 1.25f && c.b > c.r * 1.25f;
                    if (cyan && emission[i].maxColorComponent > 1f / 255f)
                        tinted = (new Color(c.maxColorComponent, c.maxColorComponent, c.maxColorComponent).linear * arc[theme].linear).gamma;
                    tinted.a = c.a; result[i] = tinted;
                }
                Save("HS_BodySkin" + theme + "_" + plate, original.width, original.height, result, ref before, ref after);
            }
        }

        private static void Save(string name, int width, int height, Color[] pixels, ref long before, ref long after)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true, false) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            texture.SetPixels(pixels); texture.Apply(true, false);
            before += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
            EditorUtility.CompressTexture(texture, TextureFormat.BC7, TextureCompressionQuality.Best);
            if (texture.format != TextureFormat.BC7) throw new InvalidOperationException(name + " did not compress (" + texture.format + ")");
            texture.Apply(false, true);
            after += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
            AssetDatabase.CreateAsset(texture, Folder + name + ".asset");
        }
    }
}
