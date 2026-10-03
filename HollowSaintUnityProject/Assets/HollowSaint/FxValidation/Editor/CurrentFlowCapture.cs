using System.IO;
using UnityEngine;

namespace HollowSaint.PreviewValidation
{
    internal static class CurrentFlowCapture
    {
        // BodyCurrent geometry/phase uses its explicit native clock. This study
        // isolates the body route, sustaining cast current at release/arrival
        // markers so the complete connections can be inspected. Spear feed/fan/
        // crown components have separate native checks. This is not skill timing.
        // Preview shader approximates light without game bloom. Body skinning is
        // freshly CPU-baked per pose to avoid same-editor-frame GPU skin caches.
        internal static void Run()
        {
            var light = new GameObject("Connected current key").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(30, 140, 0);
            RenderSettings.ambientLight = new Color(0.3f, 0.33f, 0.36f);
            var camera = new GameObject("Connected current review").AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 1.8f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.05f, 0.065f, 0.08f);
            var target = new RenderTexture(480, 640, 24); camera.targetTexture = target;
            var image = new Texture2D(480, 640, TextureFormat.RGB24, false);
            foreach (uint skin in new uint[] { 0, 1, 2, 3, 4 })
            using (var f = new CurrentFlowValidation.Fixture(skin))
            using (var snapshot = new PosedMeshSnapshot(f.pose.model))
            {
                string folder = CurrentFlowValidation.Output + "/skin" + skin + "-handoffs"; Directory.CreateDirectory(folder);
                string[] modes = { "left", "right", "fan", "catch", "crown", "dash", "idle" };
                for (int frame = 0; frame < 120; frame++)
                {
                    string mode = modes[Mathf.Min(6, frame / 15)];
                    if (frame % 15 == 0) f.Mode(mode);
                    if (mode == "left" || mode == "right") f.Window(mode).Begin(f.time - 0.105f, 0.5f, 0.2105f);
                    if (mode == "catch") f.Window("right").Begin(f.time - 0.3f, 0.3f * 19f / 13f, 13f / 19f);
                    f.Move(frame < 45 ? "strafe" : frame < 75 ? "jump" : "sprint", frame, 30); f.Tick(1f / 30f);
                    snapshot.Show();
                    Position(camera, f, false); Save(camera, image, target, folder + "/frame" + frame.ToString("D3") + ".png");
                    if (frame == 16 || frame == 46 || frame == 64)
                    {
                        Position(camera, f, true); Save(camera, image, target, CurrentFlowValidation.Output + "/skin" + skin + "-rear" + frame + ".png");
                    }
                    snapshot.Hide();
                }
            }
            camera.targetTexture = null; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
            Object.DestroyImmediate(camera.gameObject); Object.DestroyImmediate(light.gameObject);
        }

        private static void Position(Camera camera, CurrentFlowValidation.Fixture f, bool rear)
        {
            Vector3 center = f.pose.Bone("chest").position;
            Vector3 front = Vector3.ProjectOnPlane(f.pose.Bone("core socket").position - center, Vector3.up).normalized;
            if (front.sqrMagnitude < 0.5f) front = -f.pose.root.transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, front);
            camera.transform.position = center + front * (rear ? -3.2f : 3.2f) + side * -2.4f + Vector3.up * 0.7f;
            camera.transform.LookAt(center + Vector3.up * -0.1f);
        }

        private static void Save(Camera camera, Texture2D image, RenderTexture target, string path)
        {
            camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 480, 640), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG()); RenderTexture.active = null;
        }
    }
}
