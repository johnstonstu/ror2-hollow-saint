using HollowSaint.FoundationKit.Vfx;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    /// <summary>Gaze-only secondary colors. Neutral remap prevents the primary skin ramp
    /// multiplying the accent back into green/cyan. Shared originals are never modified.</summary>
    public static class GazeContrastAssets
    {
        public static readonly Color Ink = new Color(0.018f, 0.025f, 0.05f);
        public static Material Glow { get; private set; }
        public static Material Core { get; private set; }
        public static Material Outline { get; private set; }
        private static Texture2D ramp;
        private static bool loaded;

        public static Color Accent(int skin)
        {
            switch (skin)
            {
                case 1: return new Color(1f, 0.52f, 0.19f); // Obsidian cyan -> amber
                case 2: return new Color(0.64f, 0.25f, 1f); // Verdigris mint -> violet
                case 3: return new Color(0.24f, 0.48f, 1f); // Solar gold -> cobalt
                case 4: return new Color(0.16f, 0.82f, 1f); // Umbral violet -> ice blue
                case 5: return CrimsonMasteryVisuals.Pulse;
                default: return new Color(0.72f, 0.28f, 1f); // default cyan -> violet
            }
        }
        public static Color Edge(int skin) { return skin == 5 ? CrimsonMasteryVisuals.PulseEdge : Color.Lerp(Accent(skin), new Color(0.88f, 0.9f, 0.94f), 0.5f); }

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            VfxAssets.Load();
            ramp = new Texture2D(64, 1, TextureFormat.RGBA32, false) { name = "HS_GazeNeutralRamp", wrapMode = TextureWrapMode.Clamp };
            for (int i = 0; i < 64; i++)
            {
                float t = i / 63f;
                ramp.SetPixel(i, 0, new Color(t, t, t, t));
            }
            ramp.Apply(false, true);
            Glow = Neutral(VfxAssets.ArcGlow);
            Core = Neutral(VfxAssets.ArcCore);
            // Native alpha-blended shader gives the glyphs a dark silhouette.
            // Additive lightning cannot draw a dark outline, even when colored black.
            try
            {
                // This generated GUID and its ParticleSimpleAlpha asset path are verified in
                // the installed game catalog. Bypass LegacyShaderAPI's unsupported name lookup.
                var shader = Addressables.LoadAssetAsync<Shader>(
                    RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Shaders.ParticleSimpleAlpha_switch_shader).WaitForCompletion();
                if (shader)
                {
                    Outline = new Material(shader) { name = "HS_GazeInk", renderQueue = 3098 };
                    if (Outline.HasProperty("_MainTex")) Outline.SetTexture("_MainTex", Texture2D.whiteTexture);
                    if (Outline.HasProperty("_TintColor")) Outline.SetColor("_TintColor", Color.white);
                    if (Outline.HasProperty("_Color")) Outline.SetColor("_Color", Color.white);
                }
            }
            catch (System.Exception error)
            {
                // An unavailable optional addressable must not discard already-built
                // core/glow or abort a cast.
                Plugin.Log.LogWarning("HOLLOW_SAINT_GAZE_OPTIONAL_OUTLINE " + error.Message);
            }
        }

        private static Material Neutral(Material source)
        {
            if (!source) return null;
            var material = new Material(source) { name = source.name + "_GazeNeutral", renderQueue = 3100 };
            foreach (string property in new[] { "_TintColor", "_Color", "_EmissionColor" })
                if (material.HasProperty(property)) material.SetColor(property, Color.white);
            if (material.HasProperty("_RemapTex")) material.SetTexture("_RemapTex", ramp);
            return material;
        }
    }
}
