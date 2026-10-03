using System.IO;
using System.Linq;
using UnityEngine;
using HollowSaint.FoundationKit.SpearDischarge;

namespace HollowSaint.PreviewValidation
{
    // Native mesh/animation study with neutral inspection materials and a camera
    // following the hand. Does not represent gameplay, skin colors or bloom.
    internal static class FingerFlowCapture
    {
        internal static void Run(string output)
        {
            FoundationArmPose.LifeIntensity = FoundationArmPose.ReactionIntensity = FoundationArmPose.AirIntensity = FoundationArmPose.IdleSwayIntensity = 1f;
            using (var f = new ArmFlowValidation.Fixture())
            {
                f.body.SetBuffCount(ConduitSpearAnchor.OutBuff.buffIndex, 1); f.spear.SetActive(false);
                var hand = f.model.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("L HAND |" )).ToArray();
                if (hand.Length == 0) throw new System.InvalidOperationException("Native left hand renderers missing");
                foreach (var r in f.model.GetComponentsInChildren<Renderer>()) r.enabled = hand.Contains(r);
                var material = new Material(Shader.Find("Standard")) { color = new Color(0.48f, 0.55f, 0.62f) };
                foreach (var r in hand) { r.sharedMaterial = material; if (r is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true; }
                var light = new GameObject("Finger inspection light").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.3f;
                RenderSettings.ambientLight = new Color(0.35f, 0.38f, 0.4f);
                var camera = new GameObject("Native finger closeup").AddComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 0.18f; camera.nearClipPlane = 0.01f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f);
                var target = new RenderTexture(640, 640, 24); camera.targetTexture = target;
                var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
                string folder = output + "/free-fingers"; Directory.CreateDirectory(folder);
                f.animator.Rebind(); f.animator.Update(0f); f.animator.Play("Idle combat", 0); f.animator.Update(0f);
                for (int frame = 0; frame < 30; frame++) { f.Restore(); f.animator.Update(1f / 30f); f.Tick(1f / 30f, 3f + frame / 30f); }
                for (int frame = 0; frame < 120; frame++)
                {
                    f.Restore(); Gesture(f, frame); f.animator.Update(1f / 30f); f.Tick(1f / 30f, 4f + frame / 30f);
                    Frame(camera, light, f); camera.Render(); RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); image.Apply();
                    File.WriteAllBytes(folder + "/frame" + frame.ToString("D3") + ".png", image.EncodeToPNG()); RenderTexture.active = null;
                }
                camera.targetTexture = null; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
                Object.DestroyImmediate(camera.gameObject); Object.DestroyImmediate(light.gameObject); Object.DestroyImmediate(material);
            }
        }

        private static void Gesture(ArmFlowValidation.Fixture f, int frame)
        {
            string state = frame == 15 ? "Arc Bolt left" : frame == 40 ? "Arc Bolt right" : frame == 65 ? "Open Circuit arms hold" :
                frame == 30 || frame == 55 || frame == 95 ? "Empty" : null;
            if (state != null) f.animator.CrossFadeInFixedTime(state, 0.09f, 4);
        }

        private static void Frame(Camera camera, Light light, ArmFlowValidation.Fixture f)
        {
            Vector3 wrist = f.Bone("L hand").position, middle = f.Bone("L middle.1").position;
            Vector3 along = (middle - wrist).normalized;
            Vector3 across = (f.Bone("L index.1").position - f.Bone("L little.1").position).normalized;
            Vector3 normal = Vector3.Cross(along, across).normalized;
            Vector3 center = Vector3.Lerp(wrist, f.Bone("L middle.3").position, 0.55f);
            camera.transform.position = center + normal * 0.65f + across * 0.15f;
            camera.transform.LookAt(center, along); light.transform.rotation = camera.transform.rotation * Quaternion.Euler(20, -25, 0);
        }
    }
}
