using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RoR2;
using HollowSaint.FoundationKit;

namespace HollowSaint.PreviewValidation
{
    // Success: real controller/Animator handoffs preserve normalized travel,
    // debounce short ground gaps, restart air jumps, land, exit directional dashes
    // into ascent/fall, recover sprint and stop its sound loop once. Pose layers
    // consume the controller outputs through an explicit three-property bridge.
    public static class PresentationFlowValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly string Output = Path.GetFullPath("../artifacts/presentation-flow01");
        private static int checks, cases;
        private static void Require(bool ok, string why) { checks++; if (!ok) throw new InvalidOperationException(why); }
        private static void Invoke(object o, string method) => o.GetType().GetMethod(method, Private).Invoke(o, null);
        private static object Field(object o, string name) => o.GetType().GetField(name, Private).GetValue(o);

        private sealed class Fixture : IDisposable
        {
            internal readonly ArmFlowValidation.Fixture pose = new ArmFlowValidation.Fixture();
            internal readonly LivePresentation live;
            internal float time;
            internal Fixture()
            {
                pose.root.AddComponent<EntityStateMachine>(); live = pose.model.AddComponent<LivePresentation>();
                Invoke(live, "Start");
                pose.animator.Play("Spear held", 5, 0f); pose.animator.Update(0f);
                for (int i = 0; i < 300 && Phase == "Spawn"; i++) Tick(1f / 60f);
                Require(Phase == "Idle", "Spawn never settles to idle: " + Phase);
                Util.Sounds.Clear();
            }
            internal string Phase => Field(live, "phase").ToString();
            internal string State => (string)Field(live, "currentState");
            internal void Move(Vector3 velocity, bool grounded = true)
            { pose.body.characterMotor.velocity = velocity; pose.body.characterMotor.isGrounded = grounded; pose.body.inputBank.moveVector = Vector3.ProjectOnPlane(velocity, Vector3.up).normalized; }
            internal void Tick(float dt)
            {
                pose.Restore(); PreviewFrame.DeltaTime = dt;
                Invoke(live, "Update"); pose.animator.Update(dt); Invoke(live, "LateUpdate");
                pose.presentation.IsPresentingAlive = live.IsPresentingAlive;
                pose.presentation.GroundedForPresentation = live.GroundedForPresentation;
                pose.presentation.GlideWeight = live.GlideWeight;
                pose.Tick(dt, time); time += dt;
                Require(pose.bones.All(b => !float.IsNaN(b.localRotation.x + b.localRotation.y + b.localRotation.z + b.localRotation.w) && !float.IsInfinity(b.localRotation.x + b.localRotation.y + b.localRotation.z + b.localRotation.w)), "Controller/pose pipeline invalid rotation");
                foreach (var p in pose.animator.parameters.Where(p => p.type == AnimatorControllerParameterType.Float))
                {
                    float value = pose.animator.GetFloat(p.nameHash);
                    Require(!float.IsNaN(value) && !float.IsInfinity(value), "Controller produces invalid animator float: " + p.name);
                }
            }
            internal void Wait(float seconds, int fps)
            { for (int i = 0; i < Mathf.CeilToInt(seconds * fps); i++) Tick(1f / fps); }
            public void Dispose() { Invoke(live, "OnDisable"); pose.Dispose(); }
        }

        public static void RunBatch()
        {
            try
            {
                Directory.CreateDirectory(Output); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                FoundationKit.SpearDischarge.ConduitSpearAnchor.OutBuff = KitContent.MakeBuff("Out", Color.white, false, false, true);
                foreach (int fps in new[] { 30, 60, 144 })
                {
                    foreach (Vector3 direction in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
                        (Vector3.forward + Vector3.left).normalized, (Vector3.forward + Vector3.right).normalized,
                        (Vector3.back + Vector3.left).normalized, (Vector3.back + Vector3.right).normalized }) Travel(fps, direction);
                    JumpLand(fps); Sprint(fps); InvalidMotion(fps);
                    Gesture(fps, "Arc Bolt right"); Gesture(fps, "Open Circuit arms hold");
                    foreach (Vector3 direction in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                    foreach (float vertical in new[] { 5f, -5f }) Dash(fps, direction, vertical);
                }
                Capture();
                File.WriteAllText(Output + "/verification.txt", "ALL PASS\nassertions=" + checks + " transitionCases=" + cases + "\nProduction presentation decisions and native model12 Animator, fixed frame clock; explicit motor/skill/sound adapters and pose-output bridge. Not game input/network verification.\n");
                Debug.Log("PRESENTATION_FLOW_PASS checks=" + checks + " cases=" + cases); EditorApplication.Exit(0);
            }
            catch (Exception error) { File.WriteAllText(Output + "/verification.txt", "FAILED\n" + error); Debug.LogException(error); EditorApplication.Exit(1); }
        }

        private static void Travel(int fps, Vector3 direction)
        {
            using (var f = new Fixture())
            {
                f.Move(direction * 6f); f.Wait(0.8f, fps);
                Require(f.Phase == "Moving" && f.State == "Locomotion", "Native travel not phase-preserved: " + f.Phase);
                f.Move(-direction * 6f); f.Wait(0.15f, fps);
                Require(f.Phase == "Moving", "Reversal invokes canned body clip");
                f.Move(Vector3.zero); f.Wait(1.2f, fps);
                Require(f.Phase == "Idle", "Stopping leaves body moving: " + f.Phase);
                cases++;
            }
        }

        private static void JumpLand(int fps)
        {
            using (var f = new Fixture())
            {
                f.Move(Vector3.forward * 5f); f.Wait(0.4f, fps);
                f.Move(Vector3.forward * 5f, false); f.Wait(0.06f, fps);
                Require(f.Phase == "Moving" && f.live.GroundedForPresentation, "Short ground gap triggers fall");
                f.Move(Vector3.forward * 5f); f.Tick(1f / fps);
                f.pose.body.characterMotor.jumpCount = 1; f.Move(Vector3.forward * 5f + Vector3.up * 8f, false); f.Tick(1f / fps);
                Require(f.Phase == "Jump" && !f.live.GroundedForPresentation, "Jump is delayed");
                f.Wait(0.35f, fps); f.pose.body.characterMotor.jumpCount = 2; f.Move(Vector3.forward * 5f + Vector3.up * 8f, false); f.Tick(1f / fps);
                Require(f.Phase == "Jump" && (float)Field(f.live, "stateTime") < 0.02f, "Air jump does not restart jump animation");
                f.Move(Vector3.forward * 5f + Vector3.down * 5f, false); f.Wait(0.2f, fps);
                Require(f.Phase == "Descend", "Falling keeps ascent animation");
                f.Move(Vector3.forward * 5f); f.Tick(1f / fps); Require(f.Phase == "Land", "Long fall skips compression");
                f.Wait(0.5f, fps); Require(f.Phase == "Moving", "Landing never returns to travel");
                cases++;
            }
        }

        private static void Sprint(int fps)
        {
            using (var f = new Fixture())
            {
                f.Move(Vector3.forward * 9f); f.pose.body.isSprinting = true; f.Wait(0.8f, fps);
                Require(f.Phase == "GlideLoop" && f.live.GlideWeight > 0.99f, "Sprint never reaches glide");
                f.pose.body.isSprinting = false; f.Tick(1f / fps); Require(f.Phase == "GlideExit", "Sprint release skips recovery");
                f.Wait(0.8f, fps); Require(f.Phase == "Moving" && f.live.GlideWeight == 0f, "Glide recovery sticks");
                Require(Util.Sounds.Count(s => s == "GlideLoopStart") == 1 && Util.Sounds.Count(s => s == "GlideLoopStop") == 1, "Glide audio loop unbalanced");
                cases++;
            }
        }

        private static void Dash(int fps, Vector3 direction, float vertical)
        {
            using (var f = new Fixture())
            {
                f.Move(Vector3.forward * 9f); f.pose.body.isSprinting = true; f.Wait(0.8f, fps);
                f.pose.body.Dashing = true; f.pose.body.DashBlend = direction;
                f.Move(direction * 15f + Vector3.up * vertical, false); f.Tick(1f / fps);
                string suffix = direction == Vector3.left ? "left" : direction == Vector3.right ? "right" : direction == Vector3.back ? "back" : "fwd";
                Require(f.Phase == "DashStart" && f.State == "Arc Step " + (suffix == "fwd" ? "" : suffix + " ") + "start", "Wrong dash direction: " + f.State);
                Require(f.live.GlideWeight == 0f, "Dash retains sprint weight"); f.Wait(0.15f, fps);
                f.pose.body.Dashing = false; f.Tick(1f / fps);
                Require(f.Phase == (vertical > 0 ? "Ascend" : "Descend"), "Dash exit chooses wrong air pose: " + f.Phase);
                Require(Util.Sounds.Count(s => s == "GlideLoopStop") == 1, "Dash interruption leaves glide loop");
                cases++;
            }
        }

        private static void InvalidMotion(int fps)
        {
            using (var f = new Fixture())
            {
                f.Move(Vector3.forward * 6f); f.Wait(0.4f, fps);
                string state = f.State;
                foreach (Vector3 invalid in new[] { new Vector3(float.NaN, 0, 0), new Vector3(float.PositiveInfinity, 0, 0), new Vector3(1e30f, 0, 0) })
                {
                    int warnings = Plugin.Log.Warnings.Count(s => s.Contains("PRESENTATION invalid motor"));
                    f.pose.body.characterMotor.velocity = invalid;
                    for (int i = 0; i < 3; i++) f.Tick(1f / fps);
                    Require(f.State == state, "Invalid motor frame replaces last valid movement pose");
                    Require(Plugin.Log.Warnings.Count(s => s.Contains("PRESENTATION invalid motor")) == warnings + 1, "Bad motor span is silent or spams warnings");
                    f.Move(Vector3.forward * 6f); f.Wait(0.15f, fps);
                    Require(f.Phase == "Moving" && f.State == state, "Valid travel never recovers after invalid motor frame");
                }
                f.Move(Vector3.zero); f.Wait(1.2f, fps); Require(f.Phase == "Idle", "Invalid motor sample poisons stopping");
                cases++;
            }
        }

        private static void Gesture(int fps, string state)
        {
            using (var f = new Fixture())
            {
                int upper = f.pose.animator.GetLayerIndex(KitAnim.UpperBodyLayer), arms = f.pose.animator.GetLayerIndex(KitAnim.UpperArmsLayer);
                Require(f.pose.animator.HasState(upper, Animator.StringToHash(state)) && f.pose.animator.HasState(arms, Animator.StringToHash(state)), "Missing migrated gesture " + state);
                if (state == "Arc Bolt right")
                {
                    f.pose.body.SetBuffCount(FoundationKit.SpearDischarge.ConduitSpearAnchor.OutBuff.buffIndex, 1);
                    f.pose.spear.SetActive(false); f.pose.animator.Play("Empty", 5, 0f);
                    f.pose.root.GetComponent<EntityStateMachine>().state = new FoundationKit.ArcBolt.ArcBoltState();
                }
                f.pose.body.inputBank.skill1.down = true;
                f.pose.animator.Play(state, upper, 0.1f); f.pose.animator.Update(0f);
                f.Move(Vector3.forward * 6f); f.Wait(0.12f, fps);
                Require(f.pose.animator.GetCurrentAnimatorStateInfo(upper).IsName(state), "Brief movement prematurely migrates gesture");
                CheckMigration(f, fps, upper, arms, state);
                f.Move(Vector3.zero); CheckMigration(f, fps, arms, upper, state);
                Require(f.State != "Idle combat fidget", "Skill input starts idle fidget");
                cases++;
            }
        }

        private static void CheckMigration(Fixture f, int fps, int from, int to, string state)
        {
            for (int frame = 0; frame < Mathf.CeilToInt(0.35f * fps); frame++)
            {
                f.Tick(1f / fps);
                var target = f.pose.animator.IsInTransition(to) ? f.pose.animator.GetNextAnimatorStateInfo(to) : f.pose.animator.GetCurrentAnimatorStateInfo(to);
                if (!target.IsName(state)) continue;
                var source = f.pose.animator.GetCurrentAnimatorStateInfo(from);
                Require(target.normalizedTime > 0.1f, "Movement restarts gesture at its beginning");
                Require(Mathf.Abs(target.normalizedTime - source.normalizedTime) < 0.08f, "Migrated gesture loses its timing: " + target.normalizedTime + "/" + source.normalizedTime);
                return;
            }
            Require(false, "Movement fails to migrate gesture: " + state);
        }

        private static void Capture()
        {
            using (var f = new Fixture())
            {
                var light = new GameObject("Transition key").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(30, 140, 0);
                RenderSettings.ambientLight = new Color(0.3f, 0.33f, 0.36f);
                var camera = new GameObject("Live transition camera").AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.9f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.065f, 0.075f, 0.085f);
                camera.transform.position = new Vector3(-3.5f, 2.1f, -3.6f); camera.transform.LookAt(new Vector3(0, 1.3f, 0));
                var target = new RenderTexture(480, 640, 24); camera.targetTexture = target; var image = new Texture2D(480, 640, TextureFormat.RGB24, false);
                string folder = Output + "/sprint-jump-dash"; Directory.CreateDirectory(folder);
                for (int frame = 0; frame < 120; frame++)
                {
                    float t = frame / 30f; f.pose.body.isSprinting = frame < 45;
                    f.pose.body.Dashing = frame >= 60 && frame < 69; f.pose.body.DashBlend = Vector3.right;
                    float up = frame < 45 ? 0f : frame < 60 ? 8f - (t - 1.5f) * 8f : frame < 69 ? 4f : frame < 95 ? -6f : 0f;
                    f.Move((frame < 60 ? Vector3.forward : Vector3.right) * (frame < 105 ? 8f : 0f) + Vector3.up * up, frame < 45 || frame >= 95);
                    f.pose.body.characterMotor.jumpCount = frame >= 45 && frame < 95 ? 1 : 0;
                    f.Tick(1f / 30f); camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 480, 640), 0, 0); image.Apply();
                    File.WriteAllBytes(folder + "/frame" + frame.ToString("D3") + ".png", image.EncodeToPNG()); RenderTexture.active = null;
                }
                camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(camera.gameObject); UnityEngine.Object.DestroyImmediate(light.gameObject);
            }
        }
    }
}
