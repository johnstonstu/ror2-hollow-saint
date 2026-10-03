using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.OpenCircuit;
using HollowSaint.FoundationKit.SpearDischarge;

namespace HollowSaint.PreviewValidation
{
    // Diagnostic: reproduce the reported end-of-preview pose and inspect actual
    // local attachment translations, scales, controller layers and clip curves.
    public static class EndPoseAudit
    {
        public static void RunBatch()
        {
            string output = Path.GetFullPath("../artifacts/end-pose01");
            Directory.CreateDirectory(output);
            try
            {
                OpenCircuitBuff.Def = KitContent.MakeBuff("Circuit", Color.white, false, false, true);
                ConduitSpearAnchor.OutBuff = KitContent.MakeBuff("Out", Color.white, false, false, true);
                var report = new List<string>();
                using (var f = new CurrentFlowValidation.Fixture(3))
                {
                    var asset = NativeRig.Model.GetComponentsInChildren<Transform>(true);
                    var rest = asset.ToDictionary(t => AnimationUtility.CalculateTransformPath(t, NativeRig.Model.transform));
                    RuntimeAnimatorController rc = f.pose.animator.runtimeAnimatorController;
                    while (rc is AnimatorOverrideController ov) rc = ov.runtimeAnimatorController;
                    var controller = (AnimatorController)rc;
                    var stateNames = controller.layers.SelectMany(l => l.stateMachine.states).Select(s => s.state.name).Distinct().ToDictionary(n => Animator.StringToHash(n));
                    foreach (var layer in controller.layers)
                        report.Add("LAYER " + layer.name + " mask=" + (layer.avatarMask ? layer.avatarMask.name : "none") + " default=" + layer.defaultWeight);
                    var camera = new GameObject("End pose diagnostic camera").AddComponent<Camera>();
                    camera.orthographic = true; camera.orthographicSize = 1.8f; camera.backgroundColor = new Color(0.05f, 0.065f, 0.08f); camera.clearFlags = CameraClearFlags.SolidColor;
                    var target = new RenderTexture(640, 800, 24); camera.targetTexture = target;
                    var image = new Texture2D(640, 800, TextureFormat.RGB24, false);
                    var light = new GameObject("End pose light").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(30, 140, 0); RenderSettings.ambientLight = new Color(0.3f, 0.33f, 0.36f);
                    void Save(string title)
                    {
                        Vector3 center = f.pose.Bone("chest").position;
                        Vector3 front = Vector3.ProjectOnPlane(f.pose.Bone("core socket").position - center, Vector3.up).normalized;
                        Vector3 side = Vector3.Cross(Vector3.up, front);
                        camera.transform.position = center + front * 3.2f - side * 2.4f + Vector3.up * 0.7f; camera.transform.LookAt(center - Vector3.up * 0.1f);
                        camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 640, 800), 0, 0); image.Apply(); File.WriteAllBytes(output + "/" + title + ".png", image.EncodeToPNG()); RenderTexture.active = null;
                    }
                    string[] modes = { "left", "right", "fan", "catch", "crown", "dash", "idle" };
                    for (int frame = 0; frame < 180; frame++)
                    {
                        string mode = modes[Mathf.Min(6, frame / 15)];
                        if (frame % 15 == 0 && frame <= 90) f.Mode(mode);
                        if (mode == "left" || mode == "right") f.Window(mode).Begin(f.time - 0.105f, 0.5f, 0.2105f);
                        if (mode == "catch") f.Window("right").Begin(f.time - 0.3f, 0.3f * 19f / 13f, 13f / 19f);
                        f.Move(frame < 45 ? "strafe" : frame < 75 ? "jump" : "sprint", frame, 30); f.Tick(1f / 30f);
                        if (frame == 119)
                        {
                            Save("a-original");
                            foreach (var r in f.pose.model.GetComponentsInChildren<SkinnedMeshRenderer>()) r.updateWhenOffscreen = true;
                            Save("b-offscreen");
                            for (int layer = 1; layer < f.pose.animator.layerCount; layer++) f.pose.animator.SetLayerWeight(layer, 0f);
                            f.pose.Restore(); f.pose.animator.Update(0f); f.pose.Tick(1f / 30f, f.time);
                            Save("c-layers-disabled");
                        }
                        if (new[] { 0, 59, 74, 80, 89, 95, 119, 179 }.Contains(frame))
                        {
                            report.Add("FRAME " + frame + " " + mode);
                            for (int i = 0; i < f.pose.animator.layerCount; i++)
                            {
                                var state = f.pose.animator.GetCurrentAnimatorStateInfo(i);
                                report.Add(" state=" + i + ":" + (stateNames.TryGetValue(state.shortNameHash, out var n) ? n : state.shortNameHash.ToString()) + " weight=" + f.pose.animator.GetLayerWeight(i) + " time=" + state.normalizedTime);
                            }
                            foreach (string name in new[] { "chest", "L upperarm", "L forearm", "L hand", "R forearm", "R hand", "halo root", "halo 1", "halo 2", "halo 3", "halo 4" })
                            {
                                var bone = f.pose.Bone(name);
                                var original = rest[AnimationUtility.CalculateTransformPath(bone, f.pose.model.transform)];
                                report.Add(name + " parent=" + bone.parent.name + " local=" + bone.localPosition.ToString("F4") + " rest=" + original.localPosition.ToString("F4") + " scale=" + bone.localScale.ToString("F4") + " world=" + bone.position.ToString("F4"));
                            }
                            foreach (var bone in f.pose.bones)
                            {
                                string path = AnimationUtility.CalculateTransformPath(bone, f.pose.model.transform);
                                if (!rest.TryGetValue(path, out var original)) continue;
                                float distance = Vector3.Distance(bone.localPosition, original.localPosition);
                                if (distance > 0.02f && bone.GetComponent<Renderer>()) report.Add("MESH_OFFSET " + path + " local=" + bone.localPosition.ToString("F4") + " rest=" + original.localPosition.ToString("F4"));
                            }
                            foreach (var renderer in f.pose.model.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.name.Contains("HAND") || r.name.Contains("FOREARM")))
                            {
                                var mesh = new Mesh(); renderer.BakeMesh(mesh);
                                var points = mesh.vertices.Select(v => renderer.transform.TransformPoint(v)).ToArray();
                                string side = renderer.name.Contains("LEFT") || renderer.name.Contains("L ") ? "L" : "R";
                                foreach (string handSide in new[] { "L", "R" }) report.Add("MESH " + renderer.name + " minTo=" + handSide + " hand:" + points.Min(v => Vector3.Distance(v, f.pose.Bone(handSide + " hand").position)));
                                UnityEngine.Object.DestroyImmediate(mesh);
                            }
                        }
                    }
                    foreach (var clip in f.pose.animator.runtimeAnimatorController.animationClips.Distinct())
                    foreach (var b in AnimationUtility.GetCurveBindings(clip).Where(b => b.propertyName.StartsWith("m_LocalPosition") && (b.path.EndsWith(" hand") || b.path.EndsWith("halo root") || b.path.EndsWith("halo 1") || b.path.Contains("HF HAND") || b.path.Contains("HF FOREARM"))))
                    {
                        var curve = AnimationUtility.GetEditorCurve(clip, b);
                        report.Add("CURVE " + clip.name + " " + b.path.Split('/').Last() + " " + b.propertyName + " first=" + curve.Evaluate(0) + " last=" + curve.Evaluate(clip.length));
                    }
                    camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(camera.gameObject); UnityEngine.Object.DestroyImmediate(light.gameObject);
                }
                File.WriteAllLines(output + "/audit.txt", report);
                EditorApplication.Exit(0);
            }
            catch (Exception error) { File.WriteAllText(output + "/failure.txt", error.ToString()); Debug.LogException(error); EditorApplication.Exit(1); }
        }
    }
}
