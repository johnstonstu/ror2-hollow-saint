using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.OpenCircuit;
using HollowSaint.FoundationKit.SpearDischarge;

namespace HollowSaint.PreviewValidation
{
    // Independent visual check: advance one pose and render it on the next editor
    // update. Ordinary SkinnedMeshRenderers remain in use, without baked snapshots.
    public static class EndPoseFrameCapture
    {
        private static readonly bool Baseline = Environment.GetEnvironmentVariable("HS_END_POSE_BASELINE") == "1";
        private static readonly string Output = Path.GetFullPath("../artifacts/" + (Baseline ? "end-pose-baseline" : "end-pose02"));
        private static CurrentFlowValidation.Fixture fixture;
        private static Camera camera;
        private static Light light;
        private static RenderTexture target;
        private static Texture2D image;
        private static int frame;
        private static bool posed;
        public static void RunBatch()
        {
            try
            {
                Directory.CreateDirectory(Output + "/native-frames");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                OpenCircuitBuff.Def = KitContent.MakeBuff("Circuit", Color.white, false, false, true);
                ConduitSpearAnchor.OutBuff = KitContent.MakeBuff("Out", Color.white, false, false, true);
                fixture = new CurrentFlowValidation.Fixture(3);
                foreach (var r in fixture.pose.model.GetComponentsInChildren<SkinnedMeshRenderer>()) r.updateWhenOffscreen = true;
                camera = new GameObject("Native frame camera").AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.8f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.05f, 0.065f, 0.08f);
                target = new RenderTexture(480, 640, 24); camera.targetTexture = target; image = new Texture2D(480, 640, TextureFormat.RGB24, false);
                light = new GameObject("Native frame light").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(30, 140, 0);
                RenderSettings.ambientLight = new Color(0.3f, 0.33f, 0.36f);
                frame = 0; posed = false; EditorApplication.update += Step;
            }
            catch (Exception error) { Fail(error); }
        }
        private static void Step()
        {
            try
            {
                if (!posed)
                {
                    string[] modes = { "left", "right", "fan", "catch", "crown", "dash", "idle" };
                    string mode = modes[Mathf.Min(6, frame / 15)]; if (frame % 15 == 0) fixture.Mode(mode);
                    if (Baseline && frame == 75) fixture.pose.animator.PlayInFixedTime("Empty", 3, 0f);
                    if (mode == "left" || mode == "right") fixture.Window(mode).Begin(fixture.time - 0.105f, 0.5f, 0.2105f);
                    if (mode == "catch") fixture.Window("right").Begin(fixture.time - 0.3f, 0.3f * 19f / 13f, 13f / 19f);
                    fixture.Move(frame < 45 ? "strafe" : frame < 75 ? "jump" : "sprint", frame, 30); fixture.Tick(1f / 30f);
                    Vector3 center = fixture.pose.Bone("chest").position;
                    Vector3 front = Vector3.ProjectOnPlane(fixture.pose.Bone("core socket").position - center, Vector3.up).normalized;
                    camera.transform.position = center + front * 3.2f - Vector3.Cross(Vector3.up, front) * 2.4f + Vector3.up * 0.7f;
                    camera.transform.LookAt(center - Vector3.up * 0.1f); posed = true; return;
                }
                camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 480, 640), 0, 0); image.Apply(); RenderTexture.active = null;
                File.WriteAllBytes(Output + "/native-frames/frame" + frame.ToString("D3") + ".png", image.EncodeToPNG());
                if (++frame < 120) { posed = false; return; }
                var authored = NativeRig.Model.GetComponentsInChildren<Transform>();
                foreach (string name in new[] { "halo 1", "halo 2", "halo 3", "halo 4" })
                    if (Vector3.Distance(fixture.pose.Bone(name).localPosition, authored.Single(t => t.name == name).localPosition) > 0.005f) throw new InvalidOperationException("Ring did not close: " + name);
                File.WriteAllText(Output + "/verification.txt", "ALL PASS\n120 poses rendered across separate native editor updates using ordinary skinned renderers. Halo arc translations return within 5 mm of rest after closing. Explicit skill/motor adapters, not gameplay.\n");
                Cleanup(); EditorApplication.Exit(0);
            }
            catch (Exception error) { Fail(error); }
        }
        private static void Fail(Exception error) { File.WriteAllText(Output + "/verification.txt", "FAILED\n" + error); Debug.LogException(error); Cleanup(); EditorApplication.Exit(1); }
        private static void Cleanup()
        {
            EditorApplication.update -= Step;
            if (camera) { camera.targetTexture = null; UnityEngine.Object.DestroyImmediate(camera.gameObject); }
            if (target) { target.Release(); UnityEngine.Object.DestroyImmediate(target); } if (image) UnityEngine.Object.DestroyImmediate(image);
            if (light) UnityEngine.Object.DestroyImmediate(light.gameObject); fixture?.Dispose(); fixture = null;
        }
    }
}
