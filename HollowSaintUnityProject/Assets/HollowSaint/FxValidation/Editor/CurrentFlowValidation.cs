using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RoR2;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.Vfx;
using HollowSaint.FoundationKit.SpearDischarge;
using HollowSaint.FoundationKit.OpenCircuit;

namespace HollowSaint.PreviewValidation
{
    // Success: core-feed side changes stay below 7 rig units/sec at 30/60/144Hz;
    // active routes join without gaps, follow the final pose and ring, retain all
    // five palettes, and disappear on idle/death/invisibility. Native controller
    // and production pose/VFX sources; explicit ability/buff/motor adapters.
    public static class CurrentFlowValidation
    {
        internal static readonly string Output = Path.GetFullPath("../artifacts/current-flow02");
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static int checks, cases;
        private static readonly List<string> samples = new List<string>();
        private static void Require(bool ok, string why) { checks++; if (!ok) throw new InvalidOperationException(why); }
        internal static void Invoke(object o, string method) => o.GetType().GetMethod(method, Private).Invoke(o, null);
        private static object Field(object o, string name) => o.GetType().GetField(name, Private).GetValue(o);

        internal sealed class Fixture : IDisposable
        {
            internal readonly ArmFlowValidation.Fixture pose = new ArmFlowValidation.Fixture();
            internal readonly LivePresentation live;
            internal readonly BodyCurrentFx current;
            internal readonly HaloRing ring;
            internal readonly CharacterModel cm;
            internal readonly FoundationSkinAnimation skinLights;
            internal float time = 10f;
            private readonly HashSet<int> started = new HashSet<int>();
            internal Fixture(uint skin)
            {
                pose.body.skinIndex = skin; pose.root.AddComponent<EntityStateMachine>();
                cm = pose.model.AddComponent<CharacterModel>(); cm.body = pose.body;
                FoundationMaterials.Apply(pose.model);
                foreach (var r in pose.model.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(m => m ? ModelTints.Apply(m, skin) : m).ToArray();
                cm.baseRendererInfos = pose.model.GetComponentsInChildren<Renderer>().Select(r => new CharacterModel.RendererInfo { renderer = r, defaultMaterial = r.sharedMaterial }).ToArray();
                live = pose.model.AddComponent<LivePresentation>(); Invoke(live, "Start");
                current = pose.root.AddComponent<BodyCurrentFx>(); if (Field(current, "body") == null) Invoke(current, "Awake");
                ring = HaloRing.For(pose.body); if (Field(ring, "body") == null) Invoke(ring, "Awake");
                skinLights = pose.model.AddComponent<FoundationSkinAnimation>(); Invoke(skinLights, "Start");
                for (int i = 0; i < 300 && Field(live, "phase").ToString() == "Spawn"; i++) Tick(1f / 60f);
                Require(Field(live, "phase").ToString() == "Idle", "Spawn never settles");
            }
            internal LightningLine Line(int i) => pose.root.GetComponentsInChildren<LightningLine>(true).Single(l => l.name == "HS_BodyCurrent" + i);
            internal AbilityCurrentWindow Window(string name) => (AbilityCurrentWindow)Field(current, name);
            internal void Mode(string mode)
            {
                pose.Restore();
                Window("left").Clear(); Window("right").Clear();
                bool wasCrown = pose.body.HasBuff(OpenCircuitBuff.Def);
                bool held = mode == "fan" || mode == "catch" || mode == "crown" ||
                    ((mode == "dash" || mode == "idle") && pose.carry.HandVisible);
                pose.body.SetBuffCount(OpenCircuitBuff.Def.buffIndex, mode == "crown" ? 1 : 0);
                typeof(SpearCarry).GetProperty("Channeling").SetValue(pose.carry, mode == "fan");
                typeof(SpearCarry).GetProperty("HandVisible").SetValue(pose.carry, held);
                pose.body.SetBuffCount(ConduitSpearAnchor.OutBuff.buffIndex, held ? 0 : 1);
                pose.spear.SetActive(pose.carry.HandVisible); pose.body.Dashing = mode == "dash";
                pose.animator.CrossFadeInFixedTime(mode == "left" || mode == "right" ? "Arc Bolt " + mode : mode == "crown" ? "Open Circuit arms hold" : mode == "fan" ? "Spear fan loop" : "Empty", 0.09f, 4);
                // Mirror the real buff driver's halo path: the closing clip owns
                // the ring to completion, even when dash/other skills take the body.
                // Empty with write-defaults off can retain the crown translations.
                if (mode == "crown") pose.animator.PlayInFixedTime("Open Circuit hold", 3, 0f);
                else if (wasCrown) pose.animator.PlayInFixedTime("Open Circuit end", 3, 0f);
                pose.animator.CrossFadeInFixedTime(mode == "fan" ? "Spear fan loop" : mode == "crown" ? "Spear crown held" : held ? "Spear held" : "Empty", 0.09f, 5);
                if (mode == "left" || mode == "right" || mode == "catch") Window(mode == "left" ? "left" : "right").Begin(time - 0.08f, 2f, 0.7f);
            }
            internal void Tick(float dt)
            {
                pose.Restore(); PreviewFrame.DeltaTime = dt; Invoke(live, "Update");
                pose.animator.Update(dt); Invoke(live, "LateUpdate");
                pose.presentation.IsPresentingAlive = live.IsPresentingAlive;
                pose.presentation.GroundedForPresentation = live.GroundedForPresentation; pose.presentation.GlideWeight = live.GlideWeight;
                pose.Tick(dt, time); Invoke(ring, "LateUpdate"); current.Tick(time, dt);
                skinLights.Tick(time, dt);
                foreach (var line in pose.root.GetComponentsInChildren<LightningLine>(true))
                    if (started.Add(line.GetInstanceID())) { Invoke(line, "Start"); line.Tick(0f); }
                time += dt;
            }
            internal void Move(string name, int frame, int fps)
            {
                float t = frame / (float)fps; Vector3 direction = name == "strafe" ? Vector3.left : Vector3.forward;
                pose.root.transform.rotation = Quaternion.Euler(0, t * 35f, 0);
                pose.body.characterMotor.velocity = pose.root.transform.TransformDirection(direction) * (name == "idle" ? 0f : 8f);
                pose.body.characterMotor.isGrounded = name != "jump";
                if (name == "jump") pose.body.characterMotor.velocity += Vector3.up * (6f - t * 5f);
                pose.root.transform.position += pose.body.characterMotor.velocity / fps;
                pose.body.isSprinting = name == "sprint"; pose.body.inputBank.moveVector = pose.root.transform.TransformDirection(direction);
                pose.body.inputBank.aimDirection = (pose.root.transform.forward + Vector3.up * (0.4f * Mathf.Sin(t * 3f))).normalized;
            }
            internal float FeedSide()
            {
                Vector3 shoulder = (pose.Bone("L upperarm").position + pose.Bone("R upperarm").position) * 0.5f;
                return Vector3.Dot(Line(0).end - shoulder, pose.root.transform.right) / ring.Unit;
            }
            public void Dispose() { Invoke(live, "OnDisable"); pose.Dispose(); }
        }

        public static void RunBatch()
        {
            try
            {
                Directory.CreateDirectory(Output); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                OpenCircuitBuff.Def = KitContent.MakeBuff("Circuit", Color.white, false, false, true);
                ConduitSpearAnchor.OutBuff = KitContent.MakeBuff("Out", Color.white, false, false, true);
                samples.Clear(); samples.Add("skin,fps,from,to,step_rig_units,limit_rig_units");
                foreach (uint skin in new uint[] { 0, 1, 2, 3, 4 })
                foreach (int fps in new[] { 30, 60, 144 }) Handoff(skin, fps);
                foreach (uint skin in new uint[] { 0, 1, 2, 3, 4 })
                foreach (int fps in new[] { 30, 60, 144 })
                foreach (string move in new[] { "idle", "run", "strafe", "jump", "sprint" }) Sequence(skin, fps, move);
                CurrentFlowCapture.Run();
                File.WriteAllLines(Output + "/handoff-steps.csv", samples);
                File.WriteAllText(Output + "/verification.txt", "ALL PASS\nassertions=" + checks + " cases=" + cases + "\nNative model" + NativeRig.Version + "/controller and exact production pose/current/skin-light sources with explicit ability/buff/motor adapters. Actual game material tuning and tints. Preview line shader; not gameplay, networking or actual bloom.\n");
                Debug.Log("CURRENT_FLOW_PASS checks=" + checks + " cases=" + cases); EditorApplication.Exit(0);
            }
            catch (Exception error) { File.WriteAllLines(Output + "/handoff-steps.csv", samples); File.WriteAllText(Output + "/verification.txt", "FAILED\n" + error); Debug.LogException(error); EditorApplication.Exit(1); }
        }

        private static void Handoff(uint skin, int fps)
        {
            using (var f = new Fixture(skin))
            {
                f.Mode("left"); f.Tick(1f / fps); float before = f.FeedSide();
                f.Mode("right"); f.Tick(1f / fps); float step = Mathf.Abs(f.FeedSide() - before);
                samples.Add(skin + "," + fps + ",left,right," + step + "," + 7f / fps);
                Require(step < 7f / fps, "Core feed jumps across torso: skin" + skin + "/" + fps + "Hz step=" + step);
                for (int frame = 0; frame < Mathf.CeilToInt(0.3f * fps); frame++) f.Tick(1f / fps);
                Require(f.FeedSide() > 0.17f, "Feed never settles toward the right casting arm");
                CheckRoutes(f); cases++;
            }
        }

        private static void Sequence(uint skin, int fps, string move)
        {
            using (var f = new Fixture(skin))
            {
                float before = 0f; bool wasActive = false;
                string[] modes = { "left", "right", "fan", "catch", "crown", "dash", "idle" };
                int span = Mathf.CeilToInt(0.3f * fps);
                for (int frame = 0; frame < span * modes.Length; frame++)
                {
                    if (frame % span == 0)
                    {
                        f.Mode(modes[frame / span]);
                        if (modes[frame / span] == "dash" || modes[frame / span] == "idle")
                            Require(f.pose.carry.HandVisible && !f.pose.body.HasBuff(ConduitSpearAnchor.OutBuff), "Preview drops a held spear without a throw");
                    }
                    f.Move(move, frame, fps); f.Tick(1f / fps);
                    Require(f.current.IsActive == (frame / span < modes.Length - 1), "Current active/idle gate mismatch");
                    if (!f.current.IsActive) { wasActive = false; continue; }
                    float side = f.FeedSide();
                    Require(Mathf.Abs(side) <= 0.1801f, "Feed escapes torso bounds");
                    if (wasActive) Require(Mathf.Abs(side - before) < 7f / fps, "Moving handoff snaps in body space: " + move);
                    before = side; wasActive = true; CheckRoutes(f);
                }
                f.Mode("right"); f.Tick(1f / fps); f.cm.invisibilityCount = 1; f.Tick(1f / fps);
                Require(!f.current.IsActive && Active(f).Length == 0, "Invisible body retains current");
                f.cm.invisibilityCount = 0; f.Tick(1f / fps); Require(f.current.IsActive, "Current fails to recover from invisibility");
                f.pose.body.healthComponent.alive = false; f.Tick(1f / fps);
                Require(!f.current.IsActive && Active(f).Length == 0, "Dead body retains current");
                f.pose.body.healthComponent.alive = true; f.Mode("idle"); f.Tick(1f / fps);
                Require(!f.current.IsActive, "Death/reset leaves idle current"); cases++;
            }
        }

        private static LightningLine[] Active(Fixture f) => f.pose.root.GetComponentsInChildren<LightningLine>(true).Where(l => l.name.StartsWith("HS_BodyCurrent") && l.gameObject.activeSelf).ToArray();
        private static void CheckRoutes(Fixture f)
        {
            var lines = Active(f); var palette = SkinFxPalette.ForBody(f.pose.body);
            Require(f.pose.root.GetComponentsInChildren<LightningLine>(true).Count(l => l.name.StartsWith("HS_BodyCurrent")) <= 26, "Current exceeds its reusable line budget");
            Require((f.Line(0).start - f.pose.Bone("core socket").position).magnitude < 0.001f, "Core feed lags final animated source");
            Vector3 shoulder = (f.pose.Bone("L upperarm").position + f.pose.Bone("R upperarm").position) * 0.5f;
            Vector3 flank = f.Line(0).end - shoulder;
            Require((flank - Vector3.Project(flank, f.pose.root.transform.right)).magnitude < 0.001f, "Torso feed lags world movement instead of following final pose");
            Require((f.Line(0).end - f.Line(1).start).magnitude < 0.001f && (f.Line(1).end - f.Line(2).start).magnitude < 0.001f, "Core-to-ring joins have gaps");
            Require((f.Line(2).end - f.ring.Shape.Nearest(f.Line(2).end)).magnitude < 0.001f, "Feed misses the final animated ring");
            foreach (var line in lines)
            {
                Require(!float.IsNaN(line.start.sqrMagnitude + line.end.sqrMagnitude) && !float.IsInfinity(line.start.sqrMagnitude + line.end.sqrMagnitude), "Invalid current endpoints");
                var core = line.GetComponentsInChildren<LineRenderer>().Single(r => r.name == "core");
                Require(core.sharedMaterial == palette.Material(VfxAssets.ArcCore), "Current handoff retains a foreign palette");
                Require(core.positionCount <= 21, "Current line exceeds geometry budget");
                if (core.enabled) Require((core.GetPosition(0) - line.start).magnitude < 0.001f && (core.GetPosition(core.positionCount - 1) - line.end).magnitude < 0.001f, "Rendered current endpoints trail the final pose");
            }
        }
    }
}
