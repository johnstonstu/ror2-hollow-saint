using System;
using HollowSaint.FoundationKit.Vfx;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    /// <summary>Beam materials: clones of vanilla beam materials with the Hollow Saint ramp, like
    /// VfxAssets. Skin palettes recolor them through SkinFxPalette.Material.</summary>
    public static class GazeAssets
    {
        /// <summary>Stone Titan laser body.</summary>
        public static Material BeamBody { get; private set; }
        /// <summary>Preon (BFG) beam, used as the wide outer haze.</summary>
        public static Material BeamHaze { get; private set; }

        private static bool loaded;

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            VfxAssets.Load();
            BeamBody = Make(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Titan.matTitanBeam_mat, HsPalette.ArcCyan, 1.2f)
                ?? VfxAssets.ArcGlow;
            BeamHaze = Make(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_BFG.matBeamSphereBeam_mat, HsPalette.OuterCyan, 1f)
                ?? VfxAssets.ArcGlow;
        }

        private static Material Make(string key, Color tint, float intensity)
        {
            Material source = null;
            try { source = Addressables.LoadAssetAsync<Material>(key).WaitForCompletion(); }
            catch (Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_GAZE_MATERIAL_MISSING " + key + ": " + error.Message); }
            if (!source)
            {
                Plugin.Log.LogWarning("HOLLOW_SAINT_GAZE_MATERIAL_MISSING " + key);
                return null;
            }
            var material = new Material(source) { name = "HS_Gaze_" + source.name };
            if (material.HasProperty("_RemapTex") && VfxAssets.PaletteRamp) material.SetTexture("_RemapTex", VfxAssets.PaletteRamp);
            Color hdr = tint * intensity;
            hdr.a = 1f;
            if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", hdr);
            if (material.HasProperty("_Color")) material.SetColor("_Color", hdr);
            return material;
        }
    }
}
