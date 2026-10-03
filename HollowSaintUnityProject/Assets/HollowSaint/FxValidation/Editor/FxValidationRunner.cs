using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RoR2;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.Vfx;
using HollowSaint.FoundationKit.SpearDischarge;
using HollowSaint.FoundationKit.OpenCircuit;
using Storm = HollowSaint.FoundationKit.Storm;

namespace HollowSaint.PreviewValidation
{
    // Success: isolated colors; moving pinned endpoints; finite, bounded geometry;
    // idle/dead/invisible current hidden; reuse without material/line churn;
    // source-color snapshots replace independently and expire with their debuffs.
    public static class FxValidationRunner
    {
        private static readonly string Output = Path.GetFullPath("../artifacts/vfx-flow01");
        private static readonly HashSet<int> initialized = new HashSet<int>();
        private static int checks, poses;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static void RunBatch()
        {
            try
            {
                Directory.CreateDirectory(Output);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                VfxAssets.Load();
                Materials(); Rhythm(); VictimColors(); Lines(); PosedBodies();
                SpearFlowValidation.Run();
                MotionFlowValidation.Run();
                ArmFlowValidation.Run();
                File.WriteAllText(Output + "/verification.txt", "ALL PASS\nassertions=" + checks + " posedCases=" + poses +
                    "\n24 exact production source copies plus fixed-clock presentation/gesture/tint methods, Unity native geometry/material tests, explicit game-state adapters.\nPreview shader approximates additive light; actual game bloom/network/feel not tested.\n");
                Debug.Log("FX_FLOW_PASS assertions=" + checks + " posedCases=" + poses);
                EditorApplication.Exit(0);
            }
            catch (Exception error) { File.WriteAllText(Output + "/verification.txt", "FAILED\n" + error); Debug.LogException(error); EditorApplication.Exit(1); }
        }
        private static void Require(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException(message); }
        private static void Invoke(object target, string name) => target.GetType().GetMethod(name, Private).Invoke(target, null);
        private static void InitLines(GameObject root)
        {
            foreach (var line in root.GetComponentsInChildren<LightningLine>(true))
                if (initialized.Add(line.GetInstanceID())) Invoke(line, "Start");
        }
        private static void Materials()
        {
            foreach (var source in new[] { VfxAssets.ArcCore, VfxAssets.ArcGlow, VfxAssets.Spark, VfxAssets.Flash, VfxAssets.Trail, VfxAssets.Ring, VfxAssets.Afterimage })
            {
                Color original = source.GetColor("_TintColor");
                foreach (uint skin in new uint[] { 0, 1, 2, 3, 4 })
                {
                    var p = SkinFxPalette.ForIndex(skin);
                    var material = p.Material(source);
                    Require(p.Material(material) == material, "Material cache churn " + skin);
                    Require(SkinFxPalette.ForIndex(0).Material(material) == source, "Theme reset retains foreign color");
                    Require(SkinFxPalette.FromNetwork(p.NetworkColor).Arc == p.Arc, "Palette color snapshot " + skin);
                    Require(source.GetColor("_TintColor") == original, "Shared material modified");
                    Color tint = material.GetColor("_TintColor");
                    Require(tint.a == original.a && tint.maxColorComponent >= 0.8f, "Dim or opaque theme variant");
                }
            }
            foreach (uint skin in new uint[] { 2, 3, 4 })
            {
                var p = SkinFxPalette.ForIndex(skin);
                var color = p.TintParticleColor(new ParticleSystem.MinMaxGradient(new Color(0, 1, 1, 0.2f), new Color(1, 1, 1, 0.7f)));
                Require(color.colorMin.a == 0.2f && color.colorMax.a == 0.7f, "Particle fade erased");
                Require(p.Core.grayscale > p.Arc.grayscale, "Bright core no longer separates from colored glow");
            }
        }
        private static void Rhythm()
        {
            for (int frame = 0; frame <= 240; frame++)
            {
                float t = frame / 120f;
                float value = LightningRhythm.Pulse(t);
                Require(value >= 0f && value <= 1f, "Rhythm brightness bound");
                Require(Mathf.Abs(value - LightningRhythm.Pulse(t + 0.125f, 1f)) < 0.0001f, "Pulse does not propagate at 8 m/s");
                Require(Mathf.Abs(value - LightningRhythm.Pulse(t + LightningRhythm.Period)) < 0.0001f, "Pulse loses phase on repeat");
            }
        }
        private static void VictimColors()
        {
            VictimFxTheme.Register();
            var root = new GameObject("Color contracts");
            var target = root.AddComponent<CharacterBody>();
            var source = new GameObject("Color source").AddComponent<CharacterBody>();
            Storm.StormServer.StaticBuff = KitContent.MakeBuff("Static", Color.white, true, true, true);
            ConductorMarkServer.ConductorMarkBuff = KitContent.MakeBuff("Mark", Color.white, false, true, false);
            target.SetBuffCount(Storm.StormServer.StaticBuff.buffIndex, 3);
            target.SetBuffCount(ConductorMarkServer.ConductorMarkBuff.buffIndex, 1);
            CharacterBody.readOnlyInstancesList.Add(target);
            source.skinIndex = 4; VictimFxTheme.Remember(target, source);
            source.skinIndex = 2; VictimFxTheme.Remember(target, source, true);
            Require(VictimFxTheme.ForVictim(target).Index == 4 && VictimFxTheme.ForVictim(target, true).Index == 2, "Static and mark colors bleed");
            UnityEngine.Networking.NetworkServer.active = false;
            source.skinIndex = 3; VictimFxTheme.Remember(target, source);
            Require(VictimFxTheme.ForVictim(target).Index == 4, "Client overwrites source snapshot");
            UnityEngine.Networking.NetworkServer.active = true;
            VictimFxTheme.Remember(target, source);
            Require(VictimFxTheme.ForVictim(target).Index == 3, "Last contributor color not replaced");
            VictimFxTheme.Tick(1f);
            Require(target.GetBuffCount(Storm.StormServer.StaticBuff) == 3 && VictimFxTheme.ForVictim(target).Index == 3, "Color changed gameplay tier");
            target.SetBuffCount(Storm.StormServer.StaticBuff.buffIndex, 0); VictimFxTheme.Tick(1f);
            Require(VictimFxTheme.ForVictim(target).Index == 0 && VictimFxTheme.ForVictim(target, true).Index == 2, "Expired Static clears active mark");
            target.SetBuffCount(ConductorMarkServer.ConductorMarkBuff.buffIndex, 0); VictimFxTheme.Tick(1f);
            Require(VictimFxTheme.ForVictim(target, true).Index == 0, "Expired mark color persists");
            CharacterBody.readOnlyInstancesList.Clear();
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(source.gameObject);
        }
        private static void Lines()
        {
            var go = new GameObject("Line contracts");
            var line = go.AddComponent<LightningLine>(); line.loop = line.manualTick = true; line.branches = 2;
            line.start = Vector3.one; line.end = Vector3.one + Vector3.forward;
            InitLines(go);
            for (int frame = 0; frame < 120; frame++)
            {
                line.start = new Vector3(frame * 0.05f, Mathf.Sin(frame), 0f);
                line.end = line.start + Quaternion.Euler(frame, frame * 3, 0) * Vector3.forward * (0.2f + frame * 0.1f);
                line.SetPalette(SkinFxPalette.ForIndex((uint)(frame % 5))); line.Tick(1f / 60f);
                var main = go.GetComponentsInChildren<LineRenderer>().Single(r => r.name == "core");
                Require((main.GetPosition(0) - line.start).magnitude < 0.0001f && (main.GetPosition(main.positionCount - 1) - line.end).magnitude < 0.0001f, "Moving endpoints lag");
                Require(main.positionCount <= 21 && main.sharedMaterial == SkinFxPalette.ForIndex((uint)(frame % 5)).Material(VfxAssets.ArcCore), "Geometry budget or wrong color");
            }
            line.end = line.start; line.Tick(0.016f);
            Require(go.GetComponentsInChildren<LineRenderer>().All(r => !r.enabled), "Collapsed line retains stale geometry");
            line.end += Vector3.up; line.Tick(0.016f);
            Require(go.GetComponentsInChildren<LineRenderer>().Any(r => r.enabled), "Collapsed line does not recover");
            go.SetActive(false); go.SetActive(true); line.width = 0f; line.Tick(0.016f);
            Require(go.GetComponentsInChildren<LineRenderer>().All(r => !r.enabled), "Zero-width tail remains visible");
            line.width = 1f; line.end = new Vector3(float.NaN, 0, 0); line.Tick(0.016f);
            Require(go.GetComponentsInChildren<LineRenderer>().All(r => !r.enabled), "Invalid endpoints leave a visible arc");
            UnityEngine.Object.DestroyImmediate(go);
        }
        private static void PosedBodies()
        {
            OpenCircuitBuff.Def = KitContent.MakeBuff("Circuit", Color.white, false, false, true);
            var key = new GameObject("Key").AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 1.2f; key.transform.rotation = Quaternion.Euler(30, 140, 0);
            RenderSettings.ambientLight = new Color(0.3f, 0.33f, 0.36f);
            var camera = new GameObject("Camera").AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.orthographic = true; camera.orthographicSize = 1.8f;
            camera.targetTexture = new RenderTexture(480, 640, 24);
            foreach (uint skin in new uint[] { 0, 1, 2, 3, 4 })
            {
                var root = new GameObject("Skin " + skin);
                var body = root.AddComponent<CharacterBody>(); body.skinIndex = skin;
                body.healthComponent = root.AddComponent<HealthComponent>(); body.characterDirection = root.AddComponent<CharacterDirection>(); body.inputBank = root.AddComponent<InputBankTest>(); body.modelLocator = root.AddComponent<ModelLocator>();
                var model = UnityEngine.Object.Instantiate(NativeRig.Model, root.transform);
                body.modelLocator.modelTransform = model.transform;
                var cm = model.AddComponent<CharacterModel>(); cm.body = body;
                foreach (var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(m => m ? ModelTints.Apply(m, skin) : m).ToArray();
                cm.baseRendererInfos = model.GetComponentsInChildren<Renderer>().Select(r => new CharacterModel.RendererInfo { renderer = r, defaultMaterial = r.sharedMaterial }).ToArray();
                Require(SkinFxPalette.ForModel(cm).Arc == SkinFxPalette.ForIndex(skin).Arc, "Display skin detection");
                var socket = KitUtil.ResolveSocket(body, "SpearGripSocket");
                var spear = UnityEngine.Object.Instantiate(NativeRig.Spear, socket, false);
                var carry = root.AddComponent<SpearCarry>();
                typeof(SpearCarry).GetProperty("Contact").SetValue(carry, SpearCarry.Find(spear, "SpearContact"));
                var weapon = spear.AddComponent<SpearVisual>(); weapon.Owner = body;
                if (typeof(SpearVisual).GetField("tip", Private).GetValue(weapon) == null) Invoke(weapon, "Awake");
                var fan = spear.AddComponent<SpearFanFx>(); fan.Owner = body; fan.Tip = SpearCarry.Find(spear, "SpearTip"); Invoke(fan, "Start");
                var current = root.AddComponent<BodyCurrentFx>();
                if (typeof(BodyCurrentFx).GetField("body", Private).GetValue(current) == null) Invoke(current, "Awake");
                var ring = HaloRing.For(body);
                if (typeof(HaloRing).GetField("body", Private).GetValue(ring) == null) Invoke(ring, "Awake");
                var crown = root.AddComponent<CircuitCrownFx>(); Invoke(crown, "Start");
                var animator = model.GetComponent<Animator>();
                Pose(animator, "Idle combat", "throw", 0f);
                var upperarm = KitUtil.ResolveSocket(body, "R upperarm");
                Quaternion throwStart = upperarm.localRotation;
                Pose(animator, "Idle combat", "throw", 0.5f);
                Require(Quaternion.Angle(throwStart, upperarm.localRotation) > 1f, "Throw pose did not advance in preview");
                Pose(animator, "Idle combat", "crown", 0.75f); Invoke(ring, "LateUpdate");
                Require(ring.Openness > 0.4f, "Crown pose did not unfold in preview: " + ring.Openness);
                foreach (string move in new[] { "Idle combat", "Run forward", "Jump", "Glide loop", "Arc Step loop" })
                foreach (string role in new[] { "idle", "arc", "fan", "throw", "catch", "crown", "dash" })
                foreach (float phase in new[] { 0f, 0.25f, 0.5f, 0.75f })
                {
                    Pose(model.GetComponent<Animator>(), move, role, phase);
                    root.transform.position = new Vector3(phase * 2, phase, -phase);
                    root.transform.rotation = Quaternion.Euler(0, phase * 360, 0);
                    body.inputBank.aimDirection = root.transform.forward;
                    if (role == "fan" || role == "throw")
                    {
                        var upper = KitUtil.ResolveSocket(body, "R upperarm");
                        var shaft = fan.Tip.position - socket.position;
                        upper.rotation = Quaternion.FromToRotation(shaft.normalized, body.inputBank.aimDirection) * upper.rotation;
                    }
                    body.Dashing = role == "dash"; body.SetBuffCount(OpenCircuitBuff.Def.buffIndex, role == "crown" ? 1 : 0);
                    typeof(SpearCarry).GetProperty("HandVisible").SetValue(carry, role == "fan" || role == "idle" || role == "crown" || role == "catch");
                    typeof(SpearCarry).GetProperty("Channeling").SetValue(carry, role == "fan");
                    foreach (string field in new[] { "left", "right" })
                    {
                        var window = (AbilityCurrentWindow)typeof(BodyCurrentFx).GetField(field, Private).GetValue(current);
                        window.Clear();
                        if (role == "arc" && field == "left" || (role == "throw" || role == "catch") && field == "right") window.Begin(Time.time - 0.06f, 0.5f, 0.21f);
                    }
                    Invoke(ring, "LateUpdate"); Invoke(current, "LateUpdate"); InitLines(root); Invoke(current, "LateUpdate");
                    typeof(CircuitCrownFx).GetField("feedWeight", Private).SetValue(crown, role == "crown" ? 1f : 0f);
                    typeof(CircuitCrownFx).GetField("ringWeight", Private).SetValue(crown, role == "crown" ? 1f : 0f);
                    if (role == "crown") { Invoke(crown, "LateUpdate"); InitLines(root); Invoke(crown, "LateUpdate"); }
                    weapon.Powered = fan.Active = role == "fan";
                    spear.SetActive(carry.HandVisible);
                    if (carry.HandVisible) { Invoke(weapon, "LateUpdate"); fan.Tick(0.06f); InitLines(root); Invoke(weapon, "LateUpdate"); fan.Tick(0.016f); }
                    Require(current.IsActive == (role != "idle"), "Current mode gate " + role);
                    foreach (var line in root.GetComponentsInChildren<LightningLine>())
                        Require(!float.IsNaN(line.start.x + line.end.x) && (line.end - line.start).magnitude < 30, "Broken posed anchors " + role);
                    poses++;
                    if (move == "Run forward" && phase == 0.5f && role != "idle" && role != "catch") Capture(camera, root.transform.position, skin, role);
                }
                spear.SetActive(true); fan.Active = true; fan.Tick(0.06f); InitLines(root); fan.Tick(0.016f);
                fan.Active = false; fan.Tick(0.04f);
                Require(spear.GetComponentsInChildren<LightningLine>().Where(l => l.name.StartsWith("HS_SpearFanRay")).Any(), "Fan afterglow missing before end");
                fan.Tick(0.06f);
                Require(spear.GetComponentsInChildren<LightningLine>(true).Where(l => l.name.StartsWith("HS_SpearFanRay")).All(l => !l.gameObject.activeSelf), "Fan afterglow persists beyond 90ms");
                body.healthComponent.alive = false; Invoke(current, "LateUpdate");
                Require(!current.IsActive && root.GetComponentsInChildren<LightningLine>(true).Where(l => l.name.StartsWith("HS_BodyCurrent")).All(l => !l.gameObject.activeSelf), "Dead current remains");
                body.healthComponent.alive = true; cm.invisibilityCount = 1;
                typeof(SpearCarry).GetProperty("Channeling").SetValue(carry, true); Invoke(current, "LateUpdate");
                Require(!current.IsActive, "Invisible current remains");
                cm.invisibilityCount = 0; Invoke(current, "LateUpdate"); InitLines(root); Invoke(current, "LateUpdate");
                var reused = root.GetComponentsInChildren<LightningLine>(true).Where(l => l.name.StartsWith("HS_BodyCurrent")).ToArray();
                body.skinIndex = (skin + 1) % 5; Invoke(current, "LateUpdate");
                Require(reused.All(l => l && l.transform.parent == root.transform), "Skin change destroys reused current");
                Require(reused.Where(l => l.gameObject.activeSelf).All(l => l.GetComponentsInChildren<LineRenderer>().Where(r => r.name == "core").All(r => r.sharedMaterial == SkinFxPalette.ForBody(body).Material(VfxAssets.ArcCore))), "Skin change leaves old-color core");
                UnityEngine.Object.DestroyImmediate(root); initialized.Clear();
            }
        }
        private static void Pose(Animator animator, string move, string role, float phase)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind(); animator.Update(0); animator.Play(move, 0, phase);
            animator.Play("Empty", 5, 0);
            if (role == "fan") animator.Play("Spear fan loop", 5, phase);
            else if (role == "throw") animator.Play("Conduit Spear", 5, phase);
            else if (role == "catch") animator.Play("Spear catch", 5, phase);
            else if (role == "idle") animator.Play("Spear held", 5, phase);
            else if (role == "arc") animator.Play("Arc Bolt left", 4, phase);
            else if (role == "crown") { animator.Play("Open Circuit hold", 3, phase); animator.Play("Spear crown held", 5, phase); }
            animator.Update(0);
        }
        private static void Capture(Camera camera, Vector3 root, uint skin, string role)
        {
            camera.orthographicSize = role == "fan" ? 3.5f : 1.8f;
            camera.transform.position = root + (role == "fan" ? new Vector3(-7f, 3.2f, 4f) : new Vector3(-3.5f, 2.1f, -3.2f));
            camera.transform.LookAt(root + new Vector3(0, 1.3f, role == "fan" ? -2.5f : 0f));
            foreach (bool bright in new[] { false, true })
            {
                camera.backgroundColor = bright ? new Color(0.55f, 0.57f, 0.6f) : new Color(0.04f, 0.055f, 0.07f);
                camera.Render(); RenderTexture.active = camera.targetTexture;
                var tex = new Texture2D(480, 640, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 480, 640), 0, 0); tex.Apply();
                File.WriteAllBytes(Output + "/skin" + skin + "-" + role + (bright ? "-bright" : "-dark") + ".png", tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex); RenderTexture.active = null;
            }
        }
    }
}
