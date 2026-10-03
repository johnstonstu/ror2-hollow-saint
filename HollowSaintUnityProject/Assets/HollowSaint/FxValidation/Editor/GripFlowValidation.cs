using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.SpearDischarge;

namespace HollowSaint.PreviewValidation
{
    // Success: independent triangle-surface checks find no intersections between
    // all 27 baked right-hand parts and the built molded grip, after the actual
    // motion -> arm -> aim layers. Socket/finger locals alone are insufficient.
    public static class GripFlowValidation
    {
        [Serializable] private class Part { public string name; public Vector3[] vertices; public int[] triangles; }
        [Serializable] private class Geometry { public string label; public Part[] hand, grip; }
        private static readonly string Bundle = Environment.GetEnvironmentVariable("HS_GRIP_BUNDLE") ?? "11";
        private static readonly string Output = Path.GetFullPath("../artifacts/" + (Bundle == "13" ? "grip-flow03" : Bundle == "12" ? "grip-flow02" : "grip-flow01"));
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Set(object o, string n, object value) => o.GetType().GetField(n, Private | BindingFlags.Public).SetValue(o, value);

        public static void RunBatch()
        {
            AssetBundle bundle = null;
            try
            {
                Directory.CreateDirectory(Output);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (Bundle != "11" && Bundle != "12" && Bundle != "13") throw new InvalidOperationException("Unsupported grip bundle");
                bundle = AssetBundle.LoadFromFile(Path.GetFullPath("../artifacts/foundation/bundle" + Bundle + "/hollowsaintassets"));
                if (!bundle) throw new InvalidOperationException("Built bundle failed to load");
                ConduitSpearAnchor.OutBuff = KitContent.MakeBuff("Out", Color.white, false, false, true);
                ConduitSpearAnchor.RecallBuff = KitContent.MakeBuff("Recall", Color.white, false, false, true);
                var model = bundle.LoadAsset<GameObject>("mdlHollowSaint");
                var spear = bundle.LoadAsset<GameObject>("mdlConduitSpear");
                if (Bundle == "12" || Bundle == "13") VerifyMarkerGrasp(model, spear);
                int samples = 0;
                foreach (float intensity in new[] { 1f, 2f })
                foreach (string move in new[] { "Idle combat", "Run forward", "Run backward", "Run left", "Run right", "Jump", "Glide loop", "Arc Step loop" })
                foreach (string role in new[] { "held", "fan", "crown", "throw", "catch" })
                {
                    FoundationArmPose.LifeIntensity = FoundationArmPose.ReactionIntensity = FoundationArmPose.AirIntensity =
                        FoundationArmPose.IdleSwayIntensity = FoundationArmPose.FollowThrough = intensity;
                    using (var f = new ArmFlowValidation.Fixture(model, spear))
                    {
                        f.presentation.GroundedForPresentation = move != "Jump" && move != "Glide loop" && move != "Arc Step loop";
                        f.presentation.GlideWeight = move == "Glide loop" ? 1f : 0f;
                        f.body.Dashing = move == "Arc Step loop";
                        if (role == "fan") { f.carry.StartFan(0.18f); Set(f.carry, "fanStarted", 0f); }
                        if (role == "throw") { f.carry.BeginThrow(1f, 6f / 19f); Set(f.carry, "throwUntil", 100f); Set(f.carry, "throwRelease", 100f); }
                        if (role == "catch") { Set(f.carry, "catchAt", 0.01f); Set(f.carry, "exclusiveUntil", 100f); }
                        float[] phases = role == "throw" ? new[] { 0.05f, 0.2f, 0.3f } : role == "catch" ? new[] { 0.7f, 0.85f, 0.99f } : new[] { 0.15f, 0.55f, 0.9f };
                        foreach (float phase in phases)
                        {
                            for (int frame = 0; frame < 45; frame++)
                            {
                                float t = frame / 60f;
                                f.root.transform.rotation = Quaternion.Euler(0, t * 110f, 0);
                                Vector3 travel = move == "Run backward" ? Vector3.back : move == "Run left" ? Vector3.left : move == "Run right" ? Vector3.right : Vector3.forward;
                                f.body.characterMotor.velocity = f.root.transform.TransformDirection(travel) * (move == "Idle combat" ? 0f : Mathf.Clamp01(t / 0.12f) * 12f);
                                if (!f.presentation.GroundedForPresentation) f.body.characterMotor.velocity += Vector3.up * (8f - t * 18f);
                                f.body.inputBank.aimDirection = (f.root.transform.forward + Vector3.up * (phase - 0.5f) * 3f).normalized;
                                f.Pose(move, role, phase);
                                if (role == "throw" || role == "catch") { f.animator.Play(role == "throw" ? "Conduit Spear" : "Spear catch", 5, phase); f.animator.Update(0f); }
                                f.Tick(1f / 60f, t);
                            }
                            string label = "life" + intensity + "-" + move.Replace(' ', '-') + "-" + role + "-" + phase.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                            Export(f, label);
                            if (intensity == 1f && move == "Idle combat" && (role == "throw" && phase == 0.3f || role == "catch" && phase == 0.7f || role == "held" && phase == 0.55f)) Capture(f, label);
                            samples++;
                        }
                    }
                }
                File.WriteAllText(Output + "/export.txt", "EXPORTED\nsamples=" + samples + "\nActual bundle" + Bundle + ", skinned mesh bake after production pose layers; state adapters, not gameplay.\n");
                Debug.Log("GRIP_FLOW_EXPORTED samples=" + samples);
                EditorApplication.Exit(0);
            }
            catch (Exception error) { File.WriteAllText(Output + "/export.txt", "FAILED\n" + error); Debug.LogException(error); EditorApplication.Exit(1); }
            finally { if (bundle) bundle.Unload(true); }
        }

        private static void VerifyMarkerGrasp(GameObject model, GameObject spear)
        {
            using (var f = new ArmFlowValidation.Fixture(model, spear))
            {
                f.Pose("Idle combat", "held", 0f);
                var fingers = f.bones.Where(b => new[] { "R index.", "R middle.", "R ring.", "R little.", "R thumb." }.Any(b.name.StartsWith)).ToArray();
                if (fingers.Length != 15) throw new InvalidOperationException("Expected 15 fingers");
                var grasp = fingers.Select(b => b.localRotation).ToArray();
                int checks = 0;
                foreach (string state in new[] { "Conduit Spear", "Spear catch" })
                for (int step = 0; step <= 120; step++)
                {
                    float phase = state == "Conduit Spear" ? step / 120f * 6f / 19f : Mathf.Lerp(13f / 19f, 1f, step / 120f);
                    f.Restore(); f.animator.Rebind(); f.animator.Update(0f);
                    f.animator.Play("Idle combat", 0, 0f); f.animator.Play(state, 5, phase); f.animator.Update(0f);
                    for (int i = 0; i < fingers.Length; i++)
                    {
                        if (Quaternion.Angle(grasp[i], fingers[i].localRotation) > 0.05f) throw new InvalidOperationException("Marker grasp changed: " + state + "/" + phase + "/" + fingers[i].name);
                        checks++;
                    }
                }
                File.WriteAllText(Output + "/marker-grasp.txt", "ALL PASS\n" + checks + " finger checks across 242 pre-release/post-arrival poses.\n");
            }
        }

        private static void Capture(ArmFlowValidation.Fixture f, string label)
        {
            var hand = f.model.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("R HAND |", StringComparison.Ordinal)).ToArray();
            var weapon = f.spear.GetComponentsInChildren<Renderer>();
            var others = f.model.GetComponentsInChildren<Renderer>().Except(hand).Except(weapon).ToArray();
            foreach (var r in others) r.enabled = false;
            // Neutral inspection colors expose geometry without depending on the
            // game's material setup/bloom. These renders are geometry QA only.
            var renders = hand.Concat(weapon).ToArray();
            var materials = renders.Select(r => r.sharedMaterials).ToArray();
            var skin = new Material(Shader.Find("Standard")) { color = new Color(0.45f, 0.52f, 0.58f) };
            var grip = new Material(Shader.Find("Standard")) { color = new Color(0.7f, 0.35f, 0.1f) };
            foreach (var r in renders) r.sharedMaterial = r.name.StartsWith("Custom grip", StringComparison.Ordinal) ? grip : skin;
            var points = hand.SelectMany(r => Mesh(r, Matrix4x4.identity).vertices).ToArray();
            Bounds bounds = new Bounds(points[0], Vector3.zero); foreach (var p in points) bounds.Encapsulate(p);
            foreach (var r in hand.OfType<SkinnedMeshRenderer>()) r.updateWhenOffscreen = true;
            var key = new GameObject("Grasp key light").AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 1.2f;
            RenderSettings.ambientLight = new Color(0.35f, 0.38f, 0.4f);
            var camera = new GameObject("Fitted grasp closeup").AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 0.19f; camera.nearClipPlane = 0.01f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.08f, 0.085f, 0.1f);
            var target = new RenderTexture(640, 640, 24); camera.targetTexture = target;
            var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
            Transform socket = f.Bone("SpearGripSocket");
            foreach (int side in new[] { -1, 1 })
            {
                camera.transform.position = bounds.center + socket.TransformDirection(new Vector3(side, -0.5f, 0.2f)).normalized * 0.8f;
                camera.transform.LookAt(bounds.center, socket.up);
                key.transform.rotation = camera.transform.rotation * Quaternion.Euler(25, -20, 0);
                camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); image.Apply();
                File.WriteAllBytes(Output + "/" + label + "-view" + side + ".png", image.EncodeToPNG()); RenderTexture.active = null;
            }
            camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(key.gameObject);
            for (int i = 0; i < renders.Length; i++) renders[i].sharedMaterials = materials[i];
            UnityEngine.Object.DestroyImmediate(skin); UnityEngine.Object.DestroyImmediate(grip);
            foreach (var r in others) r.enabled = true;
        }

        public static void CaptureBatch()
        {
            AssetBundle bundle = null;
            try
            {
                Directory.CreateDirectory(Output);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                bundle = AssetBundle.LoadFromFile(Path.GetFullPath("../artifacts/foundation/bundle" + Bundle + "/hollowsaintassets"));
                if (!bundle) throw new InvalidOperationException("Inspection bundle failed to load");
                ConduitSpearAnchor.OutBuff = KitContent.MakeBuff("Out", Color.white, false, false, true);
                ConduitSpearAnchor.RecallBuff = KitContent.MakeBuff("Recall", Color.white, false, false, true);
                using (var f = new ArmFlowValidation.Fixture(bundle.LoadAsset<GameObject>("mdlHollowSaint"), bundle.LoadAsset<GameObject>("mdlConduitSpear")))
                foreach (string role in new[] { "held", "throw", "catch" })
                {
                    float phase = role == "throw" ? 0.3f : role == "catch" ? 0.7f : 0.55f;
                    f.Pose("Idle combat", role, phase);
                    if (role != "held") { f.animator.Play(role == "throw" ? "Conduit Spear" : "Spear catch", 5, phase); f.animator.Update(0f); }
                    f.Tick(1f / 60f, 0.75f);
                    Capture(f, "inspection-" + role);
                }
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
            finally { if (bundle) bundle.Unload(true); }
        }

        private static void Export(ArmFlowValidation.Fixture f, string label)
        {
            Matrix4x4 toGrip = f.Bone("SpearGripSocket").worldToLocalMatrix;
            var hand = f.model.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("R HAND |", StringComparison.Ordinal)).Select(r => Mesh(r, toGrip)).ToArray();
            var grip = f.spear.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith("Custom grip", StringComparison.Ordinal)).Select(r => Mesh(r, toGrip)).ToArray();
            if (hand.Length != 27 || grip.Length != 1) throw new InvalidOperationException("Hand/grip mesh inventory changed");
            File.WriteAllText(Output + "/" + label + ".json", JsonUtility.ToJson(new Geometry { label = label, hand = hand, grip = grip }));
        }

        private static Part Mesh(Renderer renderer, Matrix4x4 toGrip)
        {
            bool baked = renderer is SkinnedMeshRenderer;
            Mesh mesh = baked ? new Mesh() : renderer.GetComponent<MeshFilter>().sharedMesh;
            if (baked) ((SkinnedMeshRenderer)renderer).BakeMesh(mesh);
            Matrix4x4 relative = toGrip * renderer.transform.localToWorldMatrix;
            var result = new Part { name = renderer.name, vertices = mesh.vertices.Select(relative.MultiplyPoint3x4).ToArray(), triangles = mesh.triangles };
            if (baked) UnityEngine.Object.DestroyImmediate(mesh);
            return result;
        }
    }
}
