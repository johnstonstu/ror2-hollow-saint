using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    /// <summary>Batch stills of each skin. The tint rule mirrors FoundationSkin.ObsidianTint in the mod.</summary>
    public static class RenderSkinPreview
    {
        private static string OutDir => Path.GetFullPath("../artifacts/foundation/skins");

        public static void RunBatch()
        {
            int code = 0;
            try { Render(); }
            catch (Exception error) { Directory.CreateDirectory(OutDir); File.WriteAllText(Path.Combine(OutDir, "error.txt"), error.ToString()); code = 1; }
            EditorApplication.Exit(code);
        }

        private static Material Obsidian(Material source)
        {
            string n = source.name.ToLowerInvariant();
            if (n.Contains("conductor") || n.Contains("gap light") || n.Contains("core hot")) return source;
            var m = new Material(source);
            Color tint; float metallic, smooth;
            if (n.Contains("ivory") || n.Contains("ceramic")) { tint = new Color(0.075f, 0.08f, 0.095f); metallic = 0.35f; smooth = 0.88f; }
            else if (n.Contains("copper")) { tint = new Color(0.58f, 0.6f, 0.64f); metallic = 0.9f; smooth = 0.7f; }
            else if (n.Contains("tabard")) { tint = new Color(0.12f, 0.12f, 0.14f); metallic = 0.1f; smooth = 0.3f; }
            else { tint = new Color(0.35f, 0.36f, 0.4f); metallic = 0.6f; smooth = 0.6f; }
            // Baked body texture materials hold ivory and graphite in one map, so they get a
            // strong uniform darken; flat materials take the tint directly.
            if (m.HasProperty("_Color")) m.color = m.mainTexture ? new Color(0.34f, 0.35f, 0.4f, 1f) : tint;
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            return m;
        }

        private static void Render()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var key = new GameObject("Key").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.1f; key.transform.rotation = Quaternion.Euler(35, 150, 0);
            var fill = new GameObject("Fill").AddComponent<Light>();
            fill.type = LightType.Directional; fill.intensity = 0.5f; fill.transform.rotation = Quaternion.Euler(20, -40, 0);
            RenderSettings.ambientLight = new Color(0.4f, 0.42f, 0.47f);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HollowSaint/GameFoundation02/mdlHollowSaint.prefab");
            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.57f, 0.6f);
            cam.fieldOfView = 30f;
            var rt = new RenderTexture(640, 768, 24);
            cam.targetTexture = rt;
            foreach (bool obsidian in new[] { false, true })
            {
                var go = UnityEngine.Object.Instantiate(prefab);
                var animator = go.GetComponent<Animator>();
                animator.Rebind(); animator.Play("Select idle", 0, 0.1f); animator.Update(0.1f);
                if (obsidian)
                    foreach (var r in go.GetComponentsInChildren<Renderer>())
                        r.sharedMaterials = r.sharedMaterials.Select(m => m ? Obsidian(m) : m).ToArray();
                var bounds = go.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                cam.transform.position = bounds.center + Quaternion.Euler(6f, 200f, 0f) * Vector3.back * 5f;
                cam.transform.LookAt(bounds.center);
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(OutDir, (obsidian ? "obsidian" : "default") + ".png"), tex.EncodeToPNG());
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
