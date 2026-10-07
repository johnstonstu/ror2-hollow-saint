using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// v0.8 (experimental, config "Hopoo shading", applies at game start): converts the Saint's Unity
    /// Standard materials, including every skin variant, to RoR2's HGStandard so the game's
    /// lighting ramp and overlays (elite, cloak, freeze, shields) apply to the body. Names are
    /// kept, so skin palettes and the light animation still recognise each material.
    /// </summary>
    internal static class FoundationHopoo
    {
        // v0.9.11: off by default again. RoR2's shader showed every overlay but crushed the dark skins
        // (Umbral, Obsidian) and every skin on night stages, because it has no environment reflections.
        // The default is now Unity Standard materials WITH overlays (OverlaysOnStandard): cloak, shield,
        // crit, immune and elite overlays all show and the authored look is kept. Only the elite body
        // ramp tint (fire red, ice blue...) needs this shader.
        public static bool Enabled = false;
        private static Shader shader;
        private static readonly Dictionary<Material, Material> cache = new Dictionary<Material, Material>();

        /// <summary>v0.9.11: keep the Standard materials but let RoR2's overlays render on them.</summary>
        public static bool OverlaysOnStandard = true;

        internal static void Apply(CharacterModel model, IEnumerable<SkinDef> skins)
        {
            if (!Enabled && OverlaysOnStandard)
            {
                model.baseRendererInfos = AllowOverlays(model.baseRendererInfos);
                foreach (var skin in skins)
                    if (skin && skin.skinDefParams) skin.skinDefParams.rendererInfos = AllowOverlays(skin.skinDefParams.rendererInfos);
                Plugin.Log.LogInfo("HOLLOW_SAINT_HOPOO standard materials, overlays on");
                return;
            }
            if (!Enabled) return;
            if (!shader)
            {
                try { shader = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Shader>("RoR2/Base/Shaders/HGStandard.shader").WaitForCompletion(); }
                catch (System.Exception e) { Plugin.Log.LogWarning("HOLLOW_SAINT_HOPOO shader load failed: " + e.Message); }
                if (!shader) { Plugin.Log.LogWarning("HOLLOW_SAINT_HOPOO HGStandard unavailable; keeping Standard materials"); return; }
            }
            int converted = 0;
            model.baseRendererInfos = Convert(model.baseRendererInfos, true, ref converted);
            foreach (var skin in skins)
                if (skin && skin.skinDefParams) skin.skinDefParams.rendererInfos = Convert(skin.skinDefParams.rendererInfos, false, ref converted);
            Plugin.Log.LogInfo("HOLLOW_SAINT_HOPOO converted materials=" + converted + " overlays on");
        }

        internal static Color AlbedoWarm = new Color(1.05f, 1.0f, 0.92f, 1f);
        internal static float DarkThreshold = 0.2f, DarkLift = 2.4f, Saturation = 0.8f;
        // Between the shader default (1 / 1, a heavy sky rim that turned ivory lavender) and Commando's
        // 1.34 / 0.37 (too little rim for a near-black body against the sky).
        internal static float FresnelPower = 1.2f, FresnelBoost = 0.6f;
        private static CharacterModel.RendererInfo[] AllowOverlays(CharacterModel.RendererInfo[] infos)
        {
            if (infos == null) return infos;
            var copy = (CharacterModel.RendererInfo[])infos.Clone();
            for (int i = 0; i < copy.Length; i++) copy[i].ignoreOverlays = false;
            return copy;
        }

        private static CharacterModel.RendererInfo[] Convert(CharacterModel.RendererInfo[] infos, bool assign, ref int converted)
        {
            if (infos == null) return infos;
            var copy = (CharacterModel.RendererInfo[])infos.Clone();
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i].ignoreOverlays = false;
                var source = copy[i].defaultMaterial;
                if (!source || source.shader == shader) continue;
                Material result;
                if (!cache.TryGetValue(source, out result)) { result = ToHopoo(source); cache[source] = result; converted++; }
                copy[i].defaultMaterial = result;
                if (assign && copy[i].renderer) copy[i].renderer.sharedMaterial = result;
            }
            return copy;
        }

        private static Material ToHopoo(Material s)
        {
            var m = new Material(shader) { name = s.name };
            Color intendedAlbedo;
            bool crimsonAlbedo = CrimsonMasteryMaterials.TryGetIntendedAlbedo(s, out intendedAlbedo);
            // v0.9.10: RoR2's ramp lighting reads cooler than Unity Standard under the same sky (ivory came
            // out lavender, measured R/G/B about 0.85/0.92/1.04 of the Standard look), so the albedo tint is
            // warmed back a little. Overlays and elite ramps are unaffected.
            // Near-black plates are lifted a little too: the ramp crushed Umbral and Obsidian to a flat
            // silhouette from the gameplay camera.
            if (s.HasProperty("_Color"))
            {
                var c = s.color;
                // Lift fades in below DarkThreshold, strongest for the darkest plates.
                float darkness = crimsonAlbedo ? intendedAlbedo.maxColorComponent : c.maxColorComponent;
                float lift = Mathf.Lerp(DarkLift, 1f, Mathf.Clamp01(darkness / DarkThreshold));
                var warm = new Color(c.r * AlbedoWarm.r * lift, c.g * AlbedoWarm.g * lift, c.b * AlbedoWarm.b * lift, c.a);
                // The ramp also pushes saturation (Umbral's navy halo went royal blue, Solar went orange).
                float grey = warm.r * 0.3f + warm.g * 0.59f + warm.b * 0.11f;
                warm = new Color(Mathf.Lerp(grey, warm.r, Saturation), Mathf.Lerp(grey, warm.g, Saturation), Mathf.Lerp(grey, warm.b, Saturation), c.a);
                m.SetColor("_Color", warm);
            }
            if (s.mainTexture) { m.SetTexture("_MainTex", s.mainTexture); m.SetTextureScale("_MainTex", s.mainTextureScale); m.SetTextureOffset("_MainTex", s.mainTextureOffset); }
            if (s.HasProperty("_BumpMap") && s.GetTexture("_BumpMap")) { m.SetTexture("_NormalTex", s.GetTexture("_BumpMap")); m.SetFloat("_NormalStrength", 1f); }
            bool emissive = s.IsKeywordEnabled("_EMISSION") && s.HasProperty("_EmissionColor") && s.GetColor("_EmissionColor").maxColorComponent > 0f;
            if (emissive)
            {
                var tex = s.HasProperty("_EmissionMap") ? s.GetTexture("_EmissionMap") : null;
                m.SetTexture("_EmTex", tex ? tex : Texture2D.whiteTexture);
                m.SetColor("_EmColor", s.GetColor("_EmissionColor"));
                m.SetFloat("_EmPower", 1f);
            }
            else m.SetColor("_EmColor", Color.black);
            float smooth = s.HasProperty("_Glossiness") ? s.GetFloat("_Glossiness") : 0.4f;
            float metal = s.HasProperty("_Metallic") ? s.GetFloat("_Metallic") : 0f;
            m.SetFloat("_Smoothness", smooth);
            // Dark plates need some specular or the ramp crushes them to a flat silhouette (Umbral, Obsidian).
            // Glossy dark skins (Umbral 0.82, Obsidian 0.88) read through their highlights in Standard; HG
            // needs a strong, fairly tight specular and more rim to keep that shape.
            bool dark = crimsonAlbedo ? intendedAlbedo.maxColorComponent < DarkThreshold
                : s.HasProperty("_Color") && s.color.maxColorComponent < DarkThreshold;
            m.SetFloat("_SpecularStrength", dark ? Mathf.Lerp(0.6f, 1.3f, smooth) : Mathf.Lerp(0.35f, 0.8f, Mathf.Max(smooth, metal)));
            m.SetFloat("_SpecularExponent", Mathf.Lerp(3f, 12f, smooth));
            m.EnableKeyword("DITHER"); // fades like vanilla bodies when the camera gets close
            m.SetFloat("_DitherOn", 1f);
            // v0.9.10: vanilla body fresnel (Commando's matCommandoDualies). The shader default (power 1,
            // boost 1) washed a strong sky-blue rim over the whole body and turned ivory lavender.
            m.SetFloat("_FresnelPower", FresnelPower);
            m.SetFloat("_FresnelBoost", dark ? FresnelBoost * 1.8f : FresnelBoost);
            return m;
        }
    }
}
