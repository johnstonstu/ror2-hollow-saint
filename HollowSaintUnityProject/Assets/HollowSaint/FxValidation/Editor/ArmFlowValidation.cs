using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RoR2;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.SpearDischarge;
using HollowSaint.FoundationKit.OpenCircuit;

namespace HollowSaint.PreviewValidation
{
    // Success: actual motion->arm->aim layers restore the complete rig without
    // accumulating offsets; held fingers/socket remain fitted; free fingers fade
    // in after release; cast masks recover correctly; invalid input/teleport clears
    // reactive history. Native Unity rig, explicit motor/skill/presentation adapters.
    public static class ArmFlowValidation
    {
        private static int checks, cases;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly string Output = Path.GetFullPath("../artifacts/arm-flow01");
        private static void Require(bool ok, string reason) { checks++; if (!ok) throw new InvalidOperationException(reason); }
        private static void Invoke(object o, string n) => o.GetType().GetMethod(n, Private).Invoke(o, null);
        private static object Field(object o, string n) => o.GetType().GetField(n, Private | BindingFlags.Public).GetValue(o);
        private static void Set(object o, string n, object value) => o.GetType().GetField(n, Private | BindingFlags.Public).SetValue(o, value);
        private static float ArmScalar(FoundationArmPose arms, string side, string n) => (float)Field(Field(arms, side), n);

        internal sealed class Fixture : IDisposable
        {
            internal readonly GameObject root, model, spear;
            internal readonly CharacterBody body;
            internal readonly Animator animator;
            internal readonly FoundationPresentation presentation;
            internal readonly FoundationMotionPose motion;
            internal readonly FoundationArmPose arms;
            internal readonly SpearCarry carry;
            internal readonly Transform[] bones;
            internal Fixture(GameObject modelAsset = null, GameObject spearAsset = null)
            {
                root = new GameObject("Whole pose pipeline"); body = root.AddComponent<CharacterBody>();
                body.modelLocator = root.AddComponent<ModelLocator>(); body.characterMotor = root.AddComponent<CharacterMotor>();
                body.characterDirection = root.AddComponent<CharacterDirection>(); body.healthComponent = root.AddComponent<HealthComponent>(); body.inputBank = root.AddComponent<InputBankTest>();
                model = UnityEngine.Object.Instantiate(modelAsset ? modelAsset : NativeRig.Model, root.transform);
                body.modelLocator.modelTransform = model.transform;
                animator = model.GetComponent<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                bones = model.GetComponentsInChildren<Transform>(true);
                var socket = Bone("SpearGripSocket");
                spear = UnityEngine.Object.Instantiate(spearAsset ? spearAsset : NativeRig.Spear, socket, false);
                carry = root.AddComponent<SpearCarry>(); Set(carry, "body", body); Set(carry, "model", model.transform); Set(carry, "socket", socket); Set(carry, "upperarm", Bone("R upperarm"));
                typeof(SpearCarry).GetProperty("Tip").SetValue(carry, SpearCarry.Find(spear, "SpearTip"));
                typeof(SpearCarry).GetProperty("Contact").SetValue(carry, SpearCarry.Find(spear, "SpearContact"));
                presentation = model.AddComponent<FoundationPresentation>();
                motion = model.AddComponent<FoundationMotionPose>(); arms = model.AddComponent<FoundationArmPose>();
                Invoke(motion, "Start"); Invoke(arms, "Start");
                // Runtime desynchronizes life using instance IDs. Stable preview
                // seeds make its amplitude assertions independent of asset allocations.
                Set(Field(arms, "left"), "wristSeed", 101.1f); Set(Field(arms, "right"), "wristSeed", 203.7f);
            }
            internal Transform Bone(string n) => bones.Single(b => b.name == n);
            internal void Restore() { Invoke(motion, "Update"); Invoke(arms, "Update"); }
            internal void Pose(string move, string role, float phase)
            {
                Restore(); animator.Rebind(); animator.Update(0f); animator.Play(move, 0, phase);
                animator.Play(role == "held" || role == "crown" ? "Spear held" : role == "fan" ? "Spear fan loop" : "Empty", 5, phase);
                if (role == "left" || role == "right") animator.Play("Arc Bolt " + role, 4, phase);
                if (role == "crown") animator.Play("Open Circuit hold", 3, phase);
                if (role == "fan") animator.Play("Spear fan loop", 4, phase);
                animator.Update(0f);
            }
            internal void Tick(float dt, float time) { motion.TickPose(dt); arms.TickPose(dt, time); }
            public void Dispose() { UnityEngine.Object.DestroyImmediate(root); }
        }

        public static void Run()
        {
            Directory.CreateDirectory(Output);
            ConduitSpearAnchor.OutBuff = KitContent.MakeBuff("Out", Color.white, false, false, true);
            ConduitSpearAnchor.RecallBuff = KitContent.MakeBuff("Recall", Color.white, false, false, true);
            foreach (int fps in new[] { 30, 60, 144 })
            foreach (string move in new[] { "Idle combat", "Run forward", "Run backward", "Run left", "Run right", "Jump", "Glide loop", "Arc Step loop" })
            foreach (string role in new[] { "held", "free", "left", "right", "fan", "crown" }) Sequence(fps, move, role);
            FingerRelease(); Interruptions(); Capture();
            File.WriteAllText(Output + "/verification.txt", "ALL PASS\nassertions=" + checks + " posedCases=" + cases +
                "\nExact production arm/motion/carry/aim pipeline on native model" + NativeRig.Version + "; explicit game state adapters, not gameplay/network verification.\n");
            Debug.Log("ARM_FLOW_PASS checks=" + checks + " cases=" + cases);
        }

        private static void Sequence(int fps, string move, string role)
        {
            using (var f = new Fixture())
            {
                bool held = role == "held" || role == "fan" || role == "crown";
                f.body.SetBuffCount(ConduitSpearAnchor.OutBuff.buffIndex, held ? 0 : 1); f.spear.SetActive(held);
                f.presentation.GroundedForPresentation = move != "Jump" && move != "Glide loop" && move != "Arc Step loop";
                f.presentation.GlideWeight = move == "Glide loop" ? 1f : 0f; f.body.Dashing = move == "Arc Step loop";
                if (role == "fan") { f.carry.StartFan(0.18f); Set(f.carry, "fanStarted", 0f); }
                float freeMotion = 0f;
                for (int frame = 0; frame < fps; frame++)
                {
                    float t = frame / (float)fps, dt = 1f / fps;
                    f.root.transform.rotation = Quaternion.Euler(0, t * 70f, 0);
                    f.body.characterMotor.velocity = f.root.transform.forward * (move == "Idle combat" ? 0f : Mathf.Clamp01(t / 0.2f) * 8f);
                    if (!f.presentation.GroundedForPresentation) f.body.characterMotor.velocity += Vector3.up * (6f - t * 10f);
                    f.body.inputBank.aimDirection = (f.root.transform.forward + Vector3.up * 0.3f).normalized;
                    f.Pose(move, role, 0.5f); Quaternion[] baseline = f.bones.Select(b => b.localRotation).ToArray();
                    var fingers = f.bones.Select((b, i) => new { b, i }).Where(v => v.b.name.StartsWith("R ") && v.b.name.Contains(".")).ToArray();
                    Transform socket = f.Bone("SpearGripSocket"); Quaternion socketRotation = socket.localRotation; Vector3 socketPosition = socket.localPosition;
                    f.Tick(dt, t);
                    Require(f.bones.All(b => !float.IsNaN(b.localRotation.x + b.localRotation.y + b.localRotation.z + b.localRotation.w)), "Pipeline produces invalid rotation");
                    if (held) Require(fingers.All(v => Quaternion.Angle(v.b.localRotation, baseline[v.i]) < 0.001f), "Held grasp changed " + role);
                    Require(socket.localPosition == socketPosition && Quaternion.Angle(socket.localRotation, socketRotation) < 0.001f, "Pipeline moves molded socket");
                    freeMotion = Mathf.Max(freeMotion, Quaternion.Angle(f.Bone("L hand").localRotation, baseline[Array.IndexOf(f.bones, f.Bone("L hand"))]));
                    f.Restore();
                    Require(f.bones.Select((b, i) => Quaternion.Angle(b.localRotation, baseline[i])).All(a => a < 0.06f), "Motion/arm/aim restore order drifts " + role + "/" + move);
                }
                Require(freeMotion > 0.1f, "Free hand fixture never moves: " + fps + "/" + move + "/" + role + " max=" + freeMotion);
                if (role == "left") Require(ArmScalar(f.arms, "left", "cast") == 0.25f && ArmScalar(f.arms, "right", "cast") == 1f, "Left cast suppresses wrong arm");
                if (role == "right") Require(ArmScalar(f.arms, "right", "cast") == 0.25f && ArmScalar(f.arms, "left", "cast") == 1f, "Right cast suppresses wrong arm");
                cases++;
            }
        }

        private static void FingerRelease()
        {
            using (var f = new Fixture())
            {
                for (int i = 0; i < 30; i++) { f.Pose("Idle combat", "held", 0.5f); f.Tick(1f / 60f, i / 60f); }
                Require(ArmScalar(f.arms, "right", "fingersWeight") == 0f, "Gripped finger life is not fully off");
                f.body.SetBuffCount(ConduitSpearAnchor.OutBuff.buffIndex, 1);
                f.Pose("Idle combat", "free", 0.5f); f.Tick(1f / 60f, 0.5f);
                Require(ArmScalar(f.arms, "right", "fingersWeight") > 0f && ArmScalar(f.arms, "right", "fingersWeight") < 0.1f, "Release enables full finger life immediately");
                for (int i = 0; i < 14; i++) { f.Pose("Idle combat", "free", 0.5f); f.Tick(1f / 60f, 0.5f + i / 60f); }
                Require(ArmScalar(f.arms, "right", "fingersWeight") == 1f, "Released fingers never regain sway");
                f.body.SetBuffCount(ConduitSpearAnchor.OutBuff.buffIndex, 0); f.Pose("Idle combat", "held", 0.5f); f.Tick(1f / 60f, 1f);
                Require(ArmScalar(f.arms, "right", "fingersWeight") == 0f, "Regrip permits noise in molded grasp");
            }
        }

        private static void Interruptions()
        {
            using (var f = new Fixture())
            {
                f.body.hasEffectiveAuthority = false;
                FoundationArmPose.IdleSwayIntensity = 0f;
                for (int frame = 0; frame < 90; frame++)
                { f.root.transform.position += Vector3.forward * 0.15f; f.Pose("Run forward", "held", 0.5f); f.Tick(1f / 60f, frame / 60f); }
                var chain = (ArmChain)Field(Field(f.arms, "left"), "chain");
                Require(Mathf.Abs(chain.Swing[0].X) > 1f, "Remote interpolated travel never trails arms");
                f.root.transform.position += Vector3.right * 50f; f.Pose("Idle combat", "held", 0.5f); f.Tick(1f / 60f, 2f);
                Require(Mathf.Abs(chain.Swing[0].X) < 0.001f && Mathf.Abs(chain.Swing[0].V) < 0.001f, "Teleport retains pre-snap spring energy");
                f.body.hasEffectiveAuthority = true; f.body.characterMotor.velocity = new Vector3(float.NaN, 0, 0);
                f.Pose("Idle combat", "held", 0.5f); f.Tick(1f / 60f, 3f);
                Require(!float.IsNaN(chain.Swing[0].X) && Mathf.Abs(chain.Swing[0].X) < 0.001f, "Invalid motor velocity retains motion history");
                f.body.characterMotor.velocity = Vector3.zero; f.Pose("Idle combat", "held", 0.5f);
                Quaternion[] baseline = f.bones.Select(b => b.localRotation).ToArray();
                f.arms.TickPose(float.NaN, 3f); f.arms.TickPose(float.PositiveInfinity, 3f);
                Require(f.bones.Select((b, i) => Quaternion.Angle(b.localRotation, baseline[i])).All(a => a < 0.06f), "Invalid time changes rig");
                f.Tick(1f / 60f, 3f); Invoke(f.arms, "OnDisable"); Invoke(f.motion, "OnDisable");
                Require(f.bones.Select((b, i) => Quaternion.Angle(b.localRotation, baseline[i])).All(a => a < 0.06f), "Disabling pipeline leaves additive pose");
                FoundationArmPose.IdleSwayIntensity = 1f;
                FoundationArmPose.LifeEnabled = false;
                for (int i = 0; i < 20; i++) { f.Pose("Idle combat", "held", 0.5f); f.Tick(1f / 60f, 4f + i / 60f); }
                Require((float)Field(f.arms, "weight") == 0f, "Arm-life option never fades off");
                FoundationArmPose.LifeEnabled = true;
            }
        }

        private static void Capture()
        {
            using (var f = new Fixture())
            {
                var camera = new GameObject("Whole pose camera").AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.9f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.065f, 0.075f, 0.085f);
                camera.transform.position = new Vector3(-3.5f, 2.1f, -3.6f); camera.transform.LookAt(new Vector3(0, 1.3f, 0));
                var target = new RenderTexture(480, 640, 24); camera.targetTexture = target;
                var image = new Texture2D(480, 640, TextureFormat.RGB24, false);
                string folder = Output + "/free-hand"; Directory.CreateDirectory(folder);
                f.animator.Rebind(); f.animator.Update(0f); f.animator.Play("Idle combat", 0); f.animator.Play("Spear held", 5); f.animator.Update(0f);
                for (int frame = 0; frame < 120; frame++)
                {
                    float t = frame / 30f; f.Restore();
                    f.body.characterMotor.velocity = Vector3.forward * (frame < 60 ? Mathf.Clamp01(t / 0.3f) * 6f : Mathf.Clamp01((2.3f - t) / 0.3f) * 6f);
                    if (frame == 1) f.animator.CrossFadeInFixedTime("Run forward", 0.12f, 0);
                    if (frame == 60) f.animator.CrossFadeInFixedTime("Run stop", 0.08f, 0);
                    if (frame == 90) f.animator.CrossFadeInFixedTime("Idle combat", 0.3f, 0);
                    f.animator.Update(1f / 30f); f.Tick(1f / 30f, t);
                    camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 480, 640), 0, 0); image.Apply();
                    File.WriteAllBytes(folder + "/frame" + frame.ToString("D3") + ".png", image.EncodeToPNG()); RenderTexture.active = null;
                }
                camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }
        }
    }
}
