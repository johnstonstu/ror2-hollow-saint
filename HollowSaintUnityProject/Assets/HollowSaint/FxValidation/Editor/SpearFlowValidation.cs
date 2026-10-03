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
    // Success: smooth interruption/restart and a completed bounded recovery; exact
    // throw release; the fitted socket/finger locals stay unchanged; no additive drift.
    // Actual production carry/aim classes, native bundle11 rig, explicit game bindings.
    public static class SpearFlowValidation
    {
        private static int checks, cases;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly string Output = Path.GetFullPath("../artifacts/spear-flow01");
        private static void Require(bool ok, string reason) { checks++; if (!ok) throw new InvalidOperationException(reason); }
        private static void Field(object o, string n, object v) => o.GetType().GetField(n, Private).SetValue(o, v);
        private static void Property(object o, string n, object v) => o.GetType().GetProperty(n).SetValue(o, v);

        public static void Run()
        {
            Directory.CreateDirectory(Output);
            MathContracts(); NativeCarry();
            File.WriteAllText(Output + "/verification.txt", "ALL PASS\nassertions=" + checks + " posedCases=" + cases +
                "\nExact production SpearCarry/SpearAimPose/SpearMode on native model" + NativeRig.Version + ". Explicit game-state/animation adapters; not game footage or networking verification.\n");
            Debug.Log("SPEAR_FLOW_PASS checks=" + checks + " cases=" + cases);
        }

        private static void MathContracts()
        {
            Quaternion correction = Quaternion.Euler(-70f, 25f, 11f);
            foreach (int fps in new[] { 15, 30, 60, 144 })
            {
                var pose = new SpearAimPose(); pose.Begin();
                float dt = 1f / fps;
                for (int i = 0; i < fps; i++) pose.Channel(correction, i * dt / 0.18f, dt);
                Require(Quaternion.Angle(pose.Offset, correction) < 0.01f, "Steady fan alignment " + fps);
                pose.Recover(); Quaternion before = pose.Offset;
                pose.Relax(0.01f);
                Require(Quaternion.Angle(before, pose.Offset) < 1f && Quaternion.Angle(pose.Offset, Quaternion.identity) > 40f, "Fan-off snaps instead of easing");
                // Repeated interruption/invalid-input cleanup must not restart the tail forever.
                for (int i = 0; i < fps; i++) { pose.Recover(); pose.Relax(dt); }
                Require(Quaternion.Angle(pose.Offset, Quaternion.identity) < 0.001f, "Recovery never completes " + fps);
                pose.Begin(); pose.Throw(correction, 1f, 0f);
                Require(Quaternion.Angle(pose.Offset, correction) < 0.001f, "Throw release delayed by aim smoothing");
                pose.Throw(correction, 1f, 0.125f);
                Require(Quaternion.Angle(pose.Offset, correction) > 20f, "Throw remains pinned after release");
                pose.Throw(correction, 1f, 0.25f);
                Require(Quaternion.Angle(pose.Offset, Quaternion.identity) < 0.001f, "Throw recovery misses carry");
                pose.Begin(); pose.Throw(correction, 1f, 0f); pose.Recover(); pose.Relax(0.06f);
                before = pose.Offset; pose.Begin(); pose.Channel(correction, 0f, dt);
                Require(Quaternion.Angle(pose.Offset, before) < 0.001f, "Rapid restart discards prior pose");
                pose.Reset(); pose.Relax(float.NaN); pose.Relax(float.PositiveInfinity);
                Require(pose.Offset == Quaternion.identity, "Bad frame time poisons pose");
            }
        }

        private static void NativeCarry()
        {
            foreach (string move in new[] { "Idle combat", "Run forward", "Run backward", "Run left", "Run right", "Jump", "Glide loop", "Arc Step loop" })
            foreach (Vector3 aim in new[] { Vector3.forward, new Vector3(0, 0.7f, 1).normalized, new Vector3(0, -0.6f, 1).normalized })
            foreach (float facing in new[] { 0f, 90f, 180f, 270f })
            {
                var root = new GameObject("Native carry " + move);
                root.transform.rotation = Quaternion.Euler(0, facing, 0);
                var body = root.AddComponent<CharacterBody>();
                body.inputBank = root.AddComponent<InputBankTest>(); body.modelLocator = root.AddComponent<ModelLocator>(); body.healthComponent = root.AddComponent<HealthComponent>();
                var model = UnityEngine.Object.Instantiate(NativeRig.Model, root.transform);
                body.modelLocator.modelTransform = model.transform;
                Transform Bone(string n) => model.GetComponentsInChildren<Transform>(true).Single(t => t.name == n);
                var upper = Bone("R upperarm"); var socket = Bone("SpearGripSocket");
                var spear = UnityEngine.Object.Instantiate(NativeRig.Spear, socket, false);
                var carry = root.AddComponent<SpearCarry>();
                Field(carry, "body", body); Field(carry, "model", model.transform); Field(carry, "upperarm", upper); Field(carry, "socket", socket);
                Property(carry, "Tip", SpearCarry.Find(spear, "SpearTip")); Property(carry, "Contact", SpearCarry.Find(spear, "SpearContact"));
                var animator = model.GetComponent<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Require(animator.HasState(0, Animator.StringToHash(move)), "Missing movement fixture " + move);
                var fingers = model.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("R ") && t.name.Contains(".")).ToArray();
                Quaternion[] fingerLocals = null;
                Quaternion authored = Quaternion.identity;
                Vector3 socketPosition = socket.localPosition; Quaternion socketRotation = socket.localRotation;
                void Pose(float phase, string role = "Spear fan loop")
                {
                    animator.Rebind(); animator.Update(0f); animator.Play(move, 0, phase); animator.Play(role, 5, phase); animator.Update(0f);
                    authored = upper.localRotation; fingerLocals = fingers.Select(t => t.localRotation).ToArray();
                }
                void CheckGrip()
                {
                    Require(socket.localPosition == socketPosition && Quaternion.Angle(socket.localRotation, socketRotation) < 0.001f, "Aim changes fitted socket");
                    Require(fingers.Select((t, i) => Quaternion.Angle(t.localRotation, fingerLocals[i])).All(a => a < 0.001f), "Aim uncurls fitted fingers");
                }
                body.inputBank.aimDirection = root.transform.rotation * aim;
                float start = Time.time; carry.StartFan(0.18f);
                for (int frame = 0; frame < 90; frame++)
                {
                    Pose(0.5f); carry.TickAim(start + frame / 60f, 1f / 60f); CheckGrip();
                }
                Require(Vector3.Angle(carry.ShaftDirection, body.inputBank.aimDirection) < 0.1f, "Native fan tip misses aim " + move);
                Quaternion aimed = upper.localRotation;
                carry.EndFan(); Pose(0.5f); carry.TickAim(start + 1.5f, 0.01f);
                Require(Quaternion.Angle(aimed, upper.localRotation) < 1.5f, "Native fan-off pose snap " + move);
                for (int frame = 0; frame < 16; frame++) { Pose(0.5f); carry.TickAim(start + 1.5f + frame / 60f, 1f / 60f); CheckGrip(); }
                Require(Quaternion.Angle(authored, upper.localRotation) < 0.001f, "Native carry never regains authored pose");
                carry.StartFan(0.18f); Field(carry, "fanStarted", start);
                for (int frame = 0; frame < 30; frame++) { Pose(0.5f); carry.TickAim(start + 1f, 1f / 60f); }
                carry.EndFan(); Pose(0.5f); carry.TickAim(start + 1.5f, 0.06f); Quaternion recovering = upper.localRotation;
                carry.StartFan(0.18f); Field(carry, "fanStarted", start + 2f);
                Pose(0.5f); carry.TickAim(start + 2f, 1f / 60f);
                Require(Quaternion.Angle(recovering, upper.localRotation) < 0.001f, "Native rapid restart snaps");
                carry.EndFan(); carry.BeginThrow(0.6f, 6f / 19f);
                float releaseAt = (float)typeof(SpearCarry).GetField("throwRelease", Private).GetValue(carry);
                Pose(6f / 19f, "Conduit Spear"); carry.TickAim(releaseAt, 1f / 60f); CheckGrip();
                Require(Vector3.Angle(carry.ShaftDirection, body.inputBank.aimDirection) < 0.06f, "Native throw release alignment changed");
                Pose(0.8f, "Conduit Spear"); carry.TickAim(releaseAt + 0.26f, 1f / 60f);
                Require(Quaternion.Angle(authored, upper.localRotation) < 0.001f, "Native throw tail pinned to aim");
                carry.FinishThrow(true); body.inputBank.aimDirection = new Vector3(float.NaN, 0, 1);
                for (int frame = 0; frame < 30; frame++) { Pose(0.5f); carry.TickAim(start + 3f, 1f / 60f); }
                Require(Quaternion.Angle(authored, upper.localRotation) < 0.001f, "Native bad-input recovery stuck");
                if ((move == "Idle combat" || move == "Run forward") && facing == 0f && aim.y > 0.5f)
                    CaptureSequence(root, body, carry, animator, move);
                cases++; UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CaptureSequence(GameObject root, CharacterBody body, SpearCarry carry, Animator animator, string move)
        {
            var camera = new GameObject("Spear flow camera").AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 1.9f; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.065f, 0.075f, 0.085f);
            camera.transform.position = new Vector3(-3.5f, 2.1f, -3.6f); camera.transform.LookAt(new Vector3(0, 1.3f, 0));
            var target = new RenderTexture(480, 640, 24); camera.targetTexture = target;
            var image = new Texture2D(480, 640, TextureFormat.RGB24, false);
            Transform weaponRoot = carry.Tip;
            while (weaponRoot.parent && weaponRoot.parent.name != "SpearGripSocket") weaponRoot = weaponRoot.parent;
            string folder = Output + "/" + (move == "Run forward" ? "running" : "standing"); Directory.CreateDirectory(folder);
            float start = Time.time; carry.StartFan(0.18f); Field(carry, "fanStarted", start);
            for (int frame = 0; frame < 120; frame++)
            {
                float now = start + frame / 30f;
                if (frame == 30) carry.EndFan();
                if (frame == 45) { carry.StartFan(0.18f); Field(carry, "fanStarted", now); }
                if (frame == 60)
                {
                    carry.EndFan(); carry.BeginThrow(0.6f, 6f / 19f);
                    Field(carry, "throwRelease", now + 0.6f * 6f / 19f); Field(carry, "throwUntil", now + 0.6f);
                }
                if (frame == 78) carry.FinishThrow(true);
                string role = frame < 30 ? "Spear fan loop" : frame < 38 ? "Spear fan end" : frame < 45 ? "Spear held" :
                    frame < 51 ? "Spear fan start" : frame < 60 ? "Spear fan loop" : frame < 78 ? "Conduit Spear" : "Empty";
                float phase = role == "Spear fan end" ? (frame - 30) / 7.5f : role == "Spear fan start" ? (frame - 45) / 5.4f :
                    role == "Conduit Spear" ? (frame - 60) / 18f : Mathf.Repeat(frame / 30f, 1f);
                animator.Rebind(); animator.Update(0f); animator.Play(move, 0, Mathf.Repeat(frame / 24f, 1f)); animator.Play(role, 5, phase); animator.Update(0f);
                body.inputBank.aimDirection = new Vector3(Mathf.Sin(frame * 0.035f) * 0.18f, 0.55f, 1f).normalized;
                carry.TickAim(now, 1f / 30f);
                // Visual study of pose transitions: spear flight/return is not simulated.
                weaponRoot.gameObject.SetActive(frame < 66);
                camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 480, 640), 0, 0); image.Apply();
                File.WriteAllBytes(folder + "/frame" + frame.ToString("D3") + ".png", image.EncodeToPNG()); RenderTexture.active = null;
            }
            camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }
    }
}
