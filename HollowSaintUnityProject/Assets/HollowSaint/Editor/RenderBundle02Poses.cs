using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    /// <summary>Batch-mode stills of the GameFoundation02 controller in combined poses.</summary>
    public static class RenderBundle02Poses
    {
        private static string OutDir => Path.GetFullPath("../artifacts/foundation/bundle02-poses");

        public static void RunBatch()
        {
            int code = 0;
            try { Render(); }
            catch (Exception error) { File.WriteAllText(Path.Combine(OutDir, "error.txt"), error.ToString()); code = 1; }
            EditorApplication.Exit(code);
        }

        private static void Render()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var key = new GameObject("Key").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.1f; key.transform.rotation = Quaternion.Euler(35, 150, 0);
            var fill = new GameObject("Fill").AddComponent<Light>();
            fill.type = LightType.Directional; fill.intensity = 0.5f; fill.transform.rotation = Quaternion.Euler(20, -40, 0);
            RenderSettings.ambientLight = new Color(0.35f, 0.37f, 0.42f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HollowSaint/GameFoundation02/mdlHollowSaint.prefab");
            var go = UnityEngine.Object.Instantiate(prefab);
            var animator = go.GetComponent<Animator>();
            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.17f);
            cam.fieldOfView = 30f;
            var rt = new RenderTexture(768, 768, 24);
            cam.targetTexture = rt;

            void Shot(string name, Action setup, float seconds, float yaw)
            {
                animator.Rebind();
                animator.Update(0f);
                setup();
                for (float t = 0; t < seconds; t += 1f / 30f) animator.Update(1f / 30f);
                var bounds = go.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                Vector3 dir = Quaternion.Euler(8f, yaw, 0f) * Vector3.back;
                cam.transform.position = bounds.center + dir * 5.2f;
                cam.transform.LookAt(bounds.center);
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());
                RenderTexture.active = null;
            }

            // The model faces -Z in Blender, exported to +Z forward; yaw 180 views the front.
            Shot("01_idle", () => animator.Play("Idle", 0, 0f), 0.3f, 180f);
            Shot("02_run_cast", () => { animator.SetFloat("forwardSpeed", 6f); animator.Play("Locomotion", 0, 0.3f); animator.SetFloat("attackSpeed", 1f); animator.Play("Arc Bolt right", 1, 0f); }, 0.2f, 140f);
            Shot("03_strafe_right", () => { animator.SetFloat("rightSpeed", 3.4f); animator.Play("Locomotion", 0, 0.3f); }, 0.3f, 180f);
            Shot("04_spear_throw", () => { animator.SetFloat("forwardSpeed", 1.5f); animator.Play("Locomotion", 0, 0.2f); animator.SetFloat("attackSpeed", 1f); animator.Play("Conduit Spear", 1, 0f); }, 0.3f, 120f);
            Shot("05_crown_open", () => { animator.Play("Idle combat", 0, 0f); animator.Play("Open Circuit hold", 3, 0f); animator.SetFloat("attackSpeed", 1f); }, 0.4f, 160f);
            Shot("06_glide", () => animator.Play("Glide loop", 0, 0.2f), 0.3f, 110f);
            Shot("07_dash_back", () => animator.Play("Arc Step back loop", 0, 0.3f), 0.2f, 150f);
            Shot("08_select_intro", () => animator.Play("Select intro", 0, 0f), 0.6f, 180f);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
