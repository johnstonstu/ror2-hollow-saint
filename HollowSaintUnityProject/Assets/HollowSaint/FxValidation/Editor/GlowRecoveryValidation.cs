using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RoR2;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.Vfx;
using HollowSaint.FoundationKit.OpenCircuit;
using HollowSaint.FoundationKit.SpearDischarge;

namespace HollowSaint.PreviewValidation
{
    // Success: full-strength model-light activation takes >=60 ms and recovery <=90 ms (one
    // frame tolerance); frame steps obey those rates at 30/60/144 Hz. Death,
    // invisibility and disable restore immediately. Live skin switches use the
    // selected palette without altering other property-block writers. Native
    // model/materials/components; explicit replicated-current flags, not gameplay.
    public static class GlowRecoveryValidation
    {
        private static readonly string Output = Path.GetFullPath("../artifacts/glow-recovery01");
        private static readonly MethodInfo Tick = typeof(FoundationSkinAnimation).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
        private static int checks, cases;
        private static readonly System.Collections.Generic.List<string> steps = new System.Collections.Generic.List<string>();
        private static void Require(bool ok, string why) { checks++; if (!ok) throw new InvalidOperationException(why); }
        private static void LightTick(FoundationSkinAnimation fx, float now, float dt)
        {
            Tick.Invoke(fx, Tick.GetParameters().Length == 1 ? new object[] { now } : new object[] { now, dt });
        }
        private static void Current(BodyCurrentFx fx, bool active, float strength = 1f)
        {
            typeof(BodyCurrentFx).GetProperty("IsActive").SetValue(fx, active);
            typeof(BodyCurrentFx).GetProperty("Intensity").SetValue(fx, active ? strength : 0f);
        }
        private static bool Atlas(Material m) => m.GetTexture("_EmissionMap") && m.IsKeywordEnabled("_EMISSION") && m.GetColor("_EmissionColor").maxColorComponent > 0f;
        private static Color Emission(CharacterModel.RendererInfo info)
        {
            var block = new MaterialPropertyBlock(); info.renderer.GetPropertyBlock(block);
            Require(Mathf.Abs(block.GetFloat("_HS_OtherWriter") - 0.63f) < 0.00001f, "Other property writer erased");
            return block.GetColor("_EmissionColor");
        }
        private static void Sentinel(CharacterModel cm)
        {
            foreach (var info in cm.baseRendererInfos)
            {
                var block = new MaterialPropertyBlock(); info.renderer.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", info.defaultMaterial.GetColor("_EmissionColor"));
                block.SetFloat("_HS_OtherWriter", 0.63f); info.renderer.SetPropertyBlock(block);
            }
        }
        private static float Boost(CharacterModel cm, int index)
        {
            var info = cm.baseRendererInfos[index];
            return Emission(info).maxColorComponent / info.defaultMaterial.GetColor("_EmissionColor").maxColorComponent - 1f;
        }
        private static void Rest(CharacterModel cm)
        {
            foreach (var info in cm.baseRendererInfos.Where(i => Atlas(i.defaultMaterial)))
                Require(Vector4.Distance(Emission(info), info.defaultMaterial.GetColor("_EmissionColor")) < 0.0001f, "Atlas did not return to authored rest");
        }
        private static void Skin(CurrentFlowValidation.Fixture f, CharacterModel.RendererInfo[] original, uint skin)
        {
            f.pose.body.skinIndex = skin;
            f.cm.baseRendererInfos = original.Select(i => { var copy = i; copy.defaultMaterial = ModelTints.Apply(i.defaultMaterial, skin); copy.renderer.sharedMaterials = new[] { copy.defaultMaterial }; return copy; }).ToArray();
        }

        public static void RunBatch()
        {
            try
            {
                Directory.CreateDirectory(Output);
                steps.Clear(); steps.Add("skin,fps,first_onset_boost,first_recovery_boost");
                OpenCircuitBuff.Def = KitContent.MakeBuff("Circuit", Color.white, false, false, true);
                ConduitSpearAnchor.OutBuff = KitContent.MakeBuff("Out", Color.white, false, false, true);
                foreach (int fps in new[] { 30, 60, 144 })
                foreach (uint skin in new uint[] { 0, 1, 2, 3, 4 }) Envelope(fps, skin);
                Display();
                File.WriteAllLines(Output + "/transfer-steps.csv", steps);
                File.WriteAllText(Output + "/verification.txt", "ALL PASS\nassertions=" + checks + " cases=" + cases + "\nNative model13 and exact skin-current sources; fixed pulse phase isolates transfer rates, explicit current flags. Not gameplay, actual game bloom or skill cadence.\n");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { File.WriteAllText(Output + "/verification.txt", "FAILED\n" + error); Debug.LogException(error); EditorApplication.Exit(1); }
        }

        private static void Envelope(int fps, uint skin)
        {
            using (var f = new CurrentFlowValidation.Fixture(0))
            {
                var original = f.cm.baseRendererInfos.ToArray(); Skin(f, original, skin); Sentinel(f.cm);
                int index = Array.FindIndex(f.cm.baseRendererInfos, i => Atlas(i.defaultMaterial));
                Require(index >= 0, "No atlas emitter in bundle13");
                float now = 4.8f + index * 0.035f / LightningRhythm.Speed, dt = 1f / fps;
                Current(f.current, false); LightTick(f.skinLights, now, dt); Rest(f.cm);
                Current(f.current, true); LightTick(f.skinLights, now, dt);
                float first = Boost(f.cm, index);
                Require(first > 0f && first <= 1.6f * Mathf.Min(1f, dt / 0.06f) + 0.003f, "Abrupt ability onset at " + fps + " Hz: boost=" + first);
                LightTick(f.skinLights, now, 0f); Require(Mathf.Abs(Boost(f.cm, index) - first) < 0.0001f, "Paused envelope advanced");
                for (int frame = 0; frame < Mathf.CeilToInt(0.2f * fps); frame++) LightTick(f.skinLights, now, dt);
                Require(Mathf.Abs(Boost(f.cm, index) - 1.6f) < 0.003f, "Steady ability strength changed");
                Current(f.current, false); LightTick(f.skinLights, now, dt);
                float after = Boost(f.cm, index);
                steps.Add(skin + "," + fps + "," + first.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," + after.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                Require(after > 0f && 1.6f - after <= 1.6f * dt / 0.09f + 0.003f, "Abrupt ability recovery at " + fps + " Hz: boost=" + after);
                Current(f.current, true); LightTick(f.skinLights, now, dt);
                float restarted = Boost(f.cm, index);
                Require(restarted >= after && restarted - after <= 1.6f * dt / 0.06f + 0.003f, "Rapid restart cleared transfer history");
                Current(f.current, false);
                for (int frame = 0; frame < Mathf.CeilToInt(0.1f * fps); frame++) LightTick(f.skinLights, now, dt);
                Rest(f.cm);
                Current(f.current, true, 0.55f); for (int frame = 0; frame < Mathf.CeilToInt(0.2f * fps); frame++) LightTick(f.skinLights, now, dt);
                Require(Mathf.Abs(Boost(f.cm, index) - 1.6f * 0.55f) < 0.003f, "Open Circuit's steady current strength changed");
                Current(f.current, true); for (int frame = 0; frame < Mathf.CeilToInt(0.2f * fps); frame++) LightTick(f.skinLights, now, dt);
                f.pose.body.healthComponent.alive = false; LightTick(f.skinLights, now, dt); Rest(f.cm);
                f.pose.body.healthComponent.alive = true; Current(f.current, false); LightTick(f.skinLights, now, dt); Rest(f.cm);
                Current(f.current, true); for (int frame = 0; frame < Mathf.CeilToInt(0.2f * fps); frame++) LightTick(f.skinLights, now, dt);
                f.cm.invisibilityCount = 1; LightTick(f.skinLights, now, dt); Rest(f.cm);
                f.cm.invisibilityCount = 0; Current(f.current, false); LightTick(f.skinLights, now, dt); Rest(f.cm);
                Current(f.current, true); for (int frame = 0; frame < Mathf.CeilToInt(0.2f * fps); frame++) LightTick(f.skinLights, now, dt);
                uint nextSkin = (skin + 1) % 5; Skin(f, original, nextSkin); LightTick(f.skinLights, now, dt);
                var active = f.cm.baseRendererInfos[index]; Color expected = active.defaultMaterial.GetColor("_EmissionColor") * 2.6f;
                Require(Vector4.Distance(Emission(active), expected) < 0.001f, "Skin swap mixed old/new palette");
                for (uint next = 0; next < 5; next++)
                {
                    Skin(f, original, skin); LightTick(f.skinLights, now, dt);
                    Skin(f, original, next); LightTick(f.skinLights, now, dt);
                    foreach (var info in f.cm.baseRendererInfos.Where(i => Atlas(i.defaultMaterial)))
                    {
                        int k = Array.IndexOf(f.cm.baseRendererInfos, info);
                        Color pulse = info.defaultMaterial.GetColor("_EmissionColor") * (1f + 1.6f * LightningRhythm.Pulse(now, k * 0.035f));
                        Require(Vector4.Distance(Emission(info), pulse) < 0.001f, "Active swatch retained another skin's emission");
                    }
                }
                CurrentFlowValidation.Invoke(f.skinLights, "OnDisable"); Rest(f.cm);
                Current(f.current, false);
                for (uint next = 0; next < 5; next++)
                {
                    Skin(f, original, skin); LightTick(f.skinLights, now, dt); Rest(f.cm);
                    Skin(f, original, next); LightTick(f.skinLights, now, dt); Rest(f.cm);
                }
                cases++;
            }
        }

        private static void Display()
        {
            var model = UnityEngine.Object.Instantiate(NativeRig.Model);
            try
            {
                FoundationMaterials.Apply(model); var cm = model.AddComponent<CharacterModel>();
                var original = model.GetComponentsInChildren<Renderer>().Select(r => new CharacterModel.RendererInfo { renderer = r, defaultMaterial = r.sharedMaterial }).ToArray();
                cm.baseRendererInfos = original; Sentinel(cm);
                var fx = model.AddComponent<FoundationSkinAnimation>(); CurrentFlowValidation.Invoke(fx, "Start");
                foreach (uint skin in new uint[] { 0, 3, 1, 4, 2, 0 })
                {
                    cm.baseRendererInfos = original.Select(i => { var copy = i; copy.defaultMaterial = ModelTints.Apply(i.defaultMaterial, skin); copy.renderer.sharedMaterials = new[] { copy.defaultMaterial }; return copy; }).ToArray();
                    LightTick(fx, 10f, 1f / 60f); Rest(cm);
                    cm.invisibilityCount = 1; LightTick(fx, 10f, 1f / 60f); Rest(cm);
                    cm.invisibilityCount = 0; LightTick(fx, 10f, 1f / 60f); Rest(cm);
                    cases++;
                }
                CurrentFlowValidation.Invoke(fx, "OnDisable"); Rest(cm);
            }
            finally { UnityEngine.Object.DestroyImmediate(model); }
        }
    }
}
