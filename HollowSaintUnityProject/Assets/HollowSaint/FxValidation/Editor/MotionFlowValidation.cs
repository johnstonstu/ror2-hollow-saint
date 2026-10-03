using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RoR2;
using HollowSaint.FoundationKit.SpearDischarge;

namespace HollowSaint.PreviewValidation
{
    // Success: bounded directional torso reaction, reduced during casts, restored
    // every frame without drift; ground feet/root unchanged; glide handoff eases,
    // death/disable/teleport/bad velocity reset. Native rig, explicit state adapters.
    public static class MotionFlowValidation
    {
        private static int checks, cases;
        private static readonly string Output = Path.GetFullPath("../artifacts/motion-flow01");
        private static readonly string[] Directions = { "forward", "forward right", "right", "backward right", "backward", "backward left", "left", "forward left" };
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Require(bool ok, string reason) { checks++; if (!ok) throw new InvalidOperationException(reason); }
        private static void Invoke(object o, string n) => o.GetType().GetMethod(n, Private).Invoke(o, null);
        private static float Scalar(object o, string n) => (float)o.GetType().GetField(n, Private).GetValue(o);

        private sealed class Fixture : IDisposable
        {
            internal readonly GameObject root, model;
            internal readonly CharacterBody body;
            internal readonly FoundationPresentation presentation;
            internal readonly FoundationMotionPose motion;
            internal readonly Animator animator;
            internal readonly Transform[] bones;
            internal readonly Transform spine, head, pelvis, leftFoot, rightFoot;
            internal Fixture(bool casting)
            {
                root = new GameObject("Native motion"); body = root.AddComponent<CharacterBody>();
                body.characterMotor = root.AddComponent<CharacterMotor>(); body.characterDirection = root.AddComponent<CharacterDirection>();
                body.modelLocator = root.AddComponent<ModelLocator>(); body.healthComponent = root.AddComponent<HealthComponent>();
                model = UnityEngine.Object.Instantiate(NativeRig.Model, root.transform);
                body.modelLocator.modelTransform = model.transform;
                presentation = model.AddComponent<FoundationPresentation>(); motion = model.AddComponent<FoundationMotionPose>(); Invoke(motion, "Start");
                var carry = root.AddComponent<SpearCarry>(); typeof(SpearCarry).GetProperty("Channeling").SetValue(carry, casting);
                animator = model.GetComponent<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                bones = model.GetComponentsInChildren<Transform>(true);
                Transform Bone(string n) => bones.Single(t => t.name == n);
                spine = Bone("spine"); head = Bone("head"); pelvis = Bone("pelvis"); leftFoot = Bone("L foot"); rightFoot = Bone("R foot");
            }
            internal void Pose(string state, float phase)
            {
                Invoke(motion, "Update"); animator.Rebind(); animator.Update(0f); animator.Play(state, 0, phase);
                animator.Play("Spear held", 5, phase); animator.Update(0f);
            }
            public void Dispose() { UnityEngine.Object.DestroyImmediate(root); }
        }

        public static void Run()
        {
            Directory.CreateDirectory(Output);
            foreach (int fps in new[] { 15, 30, 60, 144 })
            for (int direction = 0; direction < 8; direction++)
            foreach (string mode in new[] { "ground", "air", "glide" })
            {
                float free = Sequence(fps, direction, mode, false);
                float casting = Sequence(fps, direction, mode, true);
                if (mode != "glide") Require(casting < free * 0.9f, "Cast does not soften torso reaction " + fps + "/" + direction);
            }
            Interruptions(); Capture();
            File.WriteAllText(Output + "/verification.txt", "ALL PASS\nassertions=" + checks + " posedCases=" + cases +
                "\nExact production FoundationMotionPose on native model" + NativeRig.Version + ". Explicit motor/presentation/skill adapters; not gameplay/network verification.\n");
            Debug.Log("MOTION_FLOW_PASS checks=" + checks + " cases=" + cases);
        }

        private static float Sequence(int fps, int direction, string mode, bool casting)
        {
            using (var f = new Fixture(casting))
            {
                float dt = 1f / fps, peak = 0f;
                f.presentation.GroundedForPresentation = mode == "ground";
                f.presentation.GlideWeight = mode == "glide" ? 1f : 0f;
                Vector3 travel = Quaternion.Euler(0, direction * 45f, 0) * Vector3.forward;
                for (int frame = 0; frame < fps * 3; frame++)
                {
                    float t = frame * dt;
                    float speed = t < 0.5f ? Mathf.Clamp01(t / 0.2f) * 6f : Mathf.Clamp01((0.8f - t) / 0.3f) * 6f;
                    f.body.characterMotor.velocity = travel * speed;
                    f.root.transform.rotation = Quaternion.Euler(0, Mathf.Clamp01((t - 0.3f) / 0.3f) * 90f, 0);
                    f.Pose(mode == "ground" ? "Run " + Directions[direction] : mode == "air" ? "Ascend" : "Glide loop", Mathf.Repeat(t, 1f));
                    Quaternion[] baseline = f.bones.Select(b => b.localRotation).ToArray();
                    Vector3 root = f.root.transform.position, left = f.leftFoot.position, right = f.rightFoot.position;
                    Quaternion spine = f.spine.localRotation, pelvis = f.pelvis.localRotation;
                    f.motion.TickPose(dt);
                    float accent = Quaternion.Angle(spine, f.spine.localRotation); peak = Mathf.Max(peak, accent);
                    Require(!float.IsNaN(accent) && accent <= (mode == "glide" ? 8f : 5f), "Unbounded spine accent " + mode);
                    Require(f.root.transform.position == root, "Posture moved gameplay root");
                    if (mode != "glide")
                    {
                        Require((f.leftFoot.position - left).sqrMagnitude < 0.0000001f && (f.rightFoot.position - right).sqrMagnitude < 0.0000001f, "Torso reaction moves planted feet");
                        Require(Quaternion.Angle(pelvis, f.pelvis.localRotation) < 0.001f, "Torso reaction moves ground pelvis");
                    }
                    Require(f.bones.Select((b, i) => new { b, i }).Where(v => v.b.name.Contains(".")).All(v => Quaternion.Angle(v.b.localRotation, baseline[v.i]) < 0.001f), "Posture changes fitted finger locals");
                    Invoke(f.motion, "Update");
                    Require(f.bones.Select((b, i) => Quaternion.Angle(b.localRotation, baseline[i])).All(a => a < 0.06f), "Saved offsets accumulate/restore an already modified pose");
                    if (t > 2.5f && mode != "glide") Require(accent < 0.08f, "Stopped torso never settles");
                }
                Require(peak > 0.1f, "Torso fixture never reacted"); cases++; return peak;
            }
        }

        private static void Interruptions()
        {
            using (var f = new Fixture(false))
            {
                f.presentation.GlideWeight = 1f; f.body.characterMotor.velocity = Vector3.forward * 12f;
                for (int i = 0; i < 60; i++) { f.Pose("Glide loop", 0.5f); f.motion.TickPose(1f / 60f); }
                f.presentation.GlideWeight = 0f; f.Pose("Arc Step loop", 0.5f); f.motion.TickPose(1f / 60f);
                Require(Scalar(f.motion, "glidePoseWeight") > 0.7f && Scalar(f.motion, "glidePoseWeight") < 1f, "Glide-to-dash accent drops in one frame");
                for (int i = 0; i < 5; i++) { f.Pose("Arc Step loop", 0.5f); f.motion.TickPose(1f / 60f); }
                Require(Scalar(f.motion, "glidePoseWeight") == 0f, "Glide tail keeps overriding dash");
                f.Pose("Glide loop", 0.5f); Quaternion[] baseline = f.bones.Select(b => b.localRotation).ToArray();
                f.presentation.GlideWeight = 1f; f.motion.TickPose(0.1f); f.presentation.IsPresentingAlive = false; f.motion.TickPose(0.016f);
                Require(f.bones.Select((b, i) => Quaternion.Angle(b.localRotation, baseline[i])).All(a => a < 0.06f), "Death does not restore pose");
                f.presentation.IsPresentingAlive = true; f.body.characterMotor.velocity = Vector3.forward * 999f; f.motion.TickPose(0.016f);
                Require(Scalar(f.motion, "glidePoseWeight") == 0f, "Teleport flings posture");
                f.body.characterMotor.velocity = new Vector3(float.NaN, 0, 0); f.motion.TickPose(0.016f);
                Require(Scalar(f.motion, "pitch") == 0f, "Bad velocity poisons posture");
                f.body.characterMotor.velocity = Vector3.forward * 8f; f.motion.TickPose(float.NaN); f.motion.TickPose(float.PositiveInfinity);
                Require(Scalar(f.motion, "pitch") == 0f, "Bad frame time poisons posture");
                f.motion.TickPose(0.016f); Invoke(f.motion, "OnDisable");
                Require(f.bones.Select((b, i) => Quaternion.Angle(b.localRotation, baseline[i])).All(a => a < 0.06f), "Disable does not restore pose");
            }
        }

        private static void Capture()
        {
            using (var f = new Fixture(false))
            {
                var socket = f.bones.Single(b => b.name == "SpearGripSocket");
                UnityEngine.Object.Instantiate(NativeRig.Spear, socket, false);
                var camera = new GameObject("Motion study camera").AddComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 1.9f; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.065f, 0.075f, 0.085f);
                camera.transform.position = new Vector3(-3.5f, 2.1f, -3.6f); camera.transform.LookAt(new Vector3(0, 1.3f, 0));
                var target = new RenderTexture(480, 640, 24); camera.targetTexture = target;
                var image = new Texture2D(480, 640, TextureFormat.RGB24, false);
                string folder = Output + "/turn-stop"; Directory.CreateDirectory(folder);
                f.animator.Rebind(); f.animator.Update(0f); f.animator.Play("Idle combat", 0, 0f); f.animator.Play("Spear held", 5, 0f); f.animator.Update(0f);
                for (int frame = 0; frame < 120; frame++)
                {
                    float t = frame / 30f;
                    float speed = t < 1.5f ? Mathf.Clamp01(t / 0.3f) * 6f : Mathf.Clamp01((2f - t) / 0.5f) * 6f;
                    f.root.transform.rotation = Quaternion.Euler(0, Mathf.Clamp01((t - 0.6f) / 0.8f) * 60f, 0);
                    f.body.characterMotor.velocity = f.root.transform.forward * speed;
                    Invoke(f.motion, "Update");
                    if (frame == 1) f.animator.CrossFadeInFixedTime("Run forward", 0.12f, 0, 0f);
                    if (frame == 60) f.animator.CrossFadeInFixedTime("Run stop", 0.08f, 0, 0f);
                    if (frame == 90) f.animator.CrossFadeInFixedTime("Idle combat", 0.3f, 0, 0f);
                    f.animator.Update(1f / 30f); f.motion.TickPose(1f / 30f);
                    camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 480, 640), 0, 0); image.Apply();
                    File.WriteAllBytes(folder + "/frame" + frame.ToString("D3") + ".png", image.EncodeToPNG()); RenderTexture.active = null;
                }
                folder = Output + "/glide-dash"; Directory.CreateDirectory(folder);
                Invoke(f.motion, "OnDisable");
                f.animator.Rebind(); f.animator.Update(0f); f.animator.Play("Glide loop", 0, 0f); f.animator.Play("Spear held", 5, 0f); f.animator.Update(0f);
                f.presentation.GroundedForPresentation = false;
                for (int frame = 0; frame < 120; frame++)
                {
                    float t = frame / 30f;
                    f.root.transform.rotation = Quaternion.Euler(0, Mathf.Clamp01(t / 1.5f) * 45f, 0);
                    f.body.characterMotor.velocity = frame < 60 ? f.root.transform.forward * 12f : frame < 78 ? -f.root.transform.right * 20f : Vector3.zero;
                    f.presentation.GlideWeight = frame < 60 ? Mathf.Clamp01(t / 0.2f) : 0f;
                    Invoke(f.motion, "Update");
                    if (frame == 60) f.animator.CrossFadeInFixedTime("Arc Step left start", 0.04f, 0, 0f);
                    if (frame == 64) f.animator.CrossFadeInFixedTime("Arc Step left loop", 0.12f, 0, 0f);
                    if (frame == 78) f.animator.CrossFadeInFixedTime("Descend", 0.2f, 0, 0f);
                    f.animator.Update(1f / 30f); f.motion.TickPose(1f / 30f);
                    camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 480, 640), 0, 0); image.Apply();
                    File.WriteAllBytes(folder + "/frame" + frame.ToString("D3") + ".png", image.EncodeToPNG()); RenderTexture.active = null;
                }
                camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }
        }
    }
}
