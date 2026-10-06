using System;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>Palette from art/concepts/kit-v2 (white-hot core, arc cyan, outer cyan, aged copper).</summary>
    public static class HsPalette
    {
        public static readonly Color WhiteHot = new Color(1f, 0.98f, 0.94f);
        public static readonly Color ArcCyan = new Color(0.30f, 0.92f, 1f);
        public static readonly Color OuterCyan = new Color(0.04f, 0.62f, 0.85f);
        public static readonly Color Copper = new Color(0.62f, 0.36f, 0.20f);
    }

    /// <summary>
    /// Materials for the code-built VFX. Each is a clone of a vanilla FX material (so it
    /// uses the game's own additive "Cloud Remap" shaders and bloom behaves), with its
    /// remap ramp replaced by the Hollow Saint palette so every effect shares one look.
    /// Falls back to Sprites/Default if a key ever stops resolving.
    /// </summary>
    public static class VfxAssets
    {
        public static Material ArcCore { get; private set; }
        public static Material ArcGlow { get; private set; }
        public static Material Flash { get; private set; }
        public static Material Ring { get; private set; }
        public static Material Spark { get; private set; }
        public static Material Trail { get; private set; }
        public static Material Afterimage { get; private set; }
        public static Texture2D PaletteRamp { get; private set; }

        private static bool loaded;

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            PaletteRamp = BuildRamp();
            // Addressable keys are GUIDs in this game version; RoR2BepInExPack's generated
            // constants map asset names to them (compile-time strings, no runtime dependency).
            ArcCore = Make(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_VFX.matTracerBright_mat, HsPalette.WhiteHot, 1.3f);
            ArcGlow = Make(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_VFX.matLightningLongBlue_mat, HsPalette.ArcCyan, 1f);
            Flash = Make(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_VFX.matGenericFlash_mat, HsPalette.ArcCyan, 1.1f);
            Ring = Make(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Huntress.matOmniRing2Huntress_mat, HsPalette.ArcCyan, 1.2f);
            // 1.2: arc-tinted, not white-hot. The hitspark texture is a big spiky starburst; white,
            // it blew out into opaque white splats that hid the enemy (full-kit review).
            Spark = Make(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Huntress.matOmniHitspark1Huntress_mat, HsPalette.ArcCyan, 1.0f);
            Trail = Make(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_VFX.matTracerBright_mat, HsPalette.ArcCyan, 1f);
            Afterimage = Make(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_VFX.matTracerBrightTransparent_mat, HsPalette.OuterCyan, 0.8f);
            Plugin.Log.LogInfo("Hollow Saint VFX materials ready.");
        }

        private static Material Make(string key, Color tint, float intensity)
        {
            // Never throws: a failure here runs inside the content load, where an exception
            // hangs the game on its loading screen. Missing materials just log and return null.
            Material source = null;
            try { source = Addressables.LoadAssetAsync<Material>(key).WaitForCompletion(); }
            catch (Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_VFX_MATERIAL_MISSING " + key + ": " + error.Message); }
            if (source == null)
            {
                Plugin.Log.LogWarning("HOLLOW_SAINT_VFX_MATERIAL_MISSING " + key);
                return null;
            }
            var material = new Material(source);
            if (material.HasProperty("_RemapTex")) material.SetTexture("_RemapTex", PaletteRamp);
            material.name = "HS_" + source.name;
            Color hdr = tint * intensity;
            hdr.a = 1f;
            if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", hdr);
            if (material.HasProperty("_Color")) material.SetColor("_Color", hdr);
            return material;
        }

        /// <summary>Black to copper to outer cyan to arc cyan to white-hot, the meter ramp
        /// from the kit-v2 VFX library sheet. Cloud Remap shaders map intensity through it.</summary>
        private static Texture2D BuildRamp()
        {
            var ramp = new Texture2D(256, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "HS_PaletteRamp" };
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.black, 0f),
                    new GradientColorKey(HsPalette.Copper * 0.6f, 0.18f),
                    new GradientColorKey(HsPalette.OuterCyan, 0.45f),
                    new GradientColorKey(HsPalette.ArcCyan, 0.75f),
                    new GradientColorKey(HsPalette.WhiteHot, 1f)
                },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.6f, 0.3f), new GradientAlphaKey(1f, 1f) });
            for (int x = 0; x < 256; x++) ramp.SetPixel(x, 0, gradient.Evaluate(x / 255f));
            ramp.Apply(false, true);
            return ramp;
        }

        /// <summary>A prefab root that is not in any scene, for effect and ghost prefabs.</summary>
        public static GameObject NewPrefab(string name, bool networked = false)
        {
            var temp = new GameObject(name);
            var prefab = PrefabAPI.InstantiateClone(temp, name, networked);
            UnityEngine.Object.Destroy(temp);
            return prefab;
        }
    }
}
