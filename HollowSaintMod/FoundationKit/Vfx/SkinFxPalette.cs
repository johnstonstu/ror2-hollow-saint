using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>Immutable per-skin VFX colors and owned material variants. No source material edits.</summary>
    public sealed class SkinFxPalette
    {
        private static readonly SkinFxPalette[] themes = {
            new SkinFxPalette(0, HsPalette.ArcCyan),
            new SkinFxPalette(1, HsPalette.ArcCyan), // Obsidian retains its cyan conductors.
            new SkinFxPalette(2, new Color(0.45f, 1f, 0.72f)),
            new SkinFxPalette(3, new Color(1f, 0.72f, 0.22f)),
            new SkinFxPalette(4, new Color(0.75f, 0.35f, 1f))
        };
        public readonly int Index;
        public readonly Color Arc, Outer, Core;
        private readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>();
        private static readonly Dictionary<Material, Material> originals = new Dictionary<Material, Material>();
        private static readonly Dictionary<Material, SkinFxPalette> owners = new Dictionary<Material, SkinFxPalette>();
        private Texture2D ramp;

        private SkinFxPalette(int index, Color arc)
        {
            Index = index; Arc = arc;
            Core = index < 2 ? HsPalette.WhiteHot : Color.Lerp(arc, Color.white, 0.82f);
            Color outer = index < 2 ? HsPalette.OuterCyan : arc * 0.65f;
            outer.a = 1f; Outer = outer;
        }

        public static SkinFxPalette ForBody(CharacterBody body) => ForIndex(KitUtil.IsHollowSaint(body) ? body.skinIndex : 0u);
        internal static SkinFxPalette ForModel(CharacterModel model)
        {
            if (!model) return themes[0];
            foreach (var info in model.baseRendererInfos)
            {
                string name = info.defaultMaterial ? info.defaultMaterial.name : "";
                if (name.EndsWith(" (Verdigris)", System.StringComparison.Ordinal)) return themes[2];
                if (name.EndsWith(" (Solar)", System.StringComparison.Ordinal)) return themes[3];
                if (name.EndsWith(" (Umbral)", System.StringComparison.Ordinal)) return themes[4];
                if (name.EndsWith(" (Obsidian)", System.StringComparison.Ordinal)) return themes[1];
            }
            return themes[0];
        }
        public static SkinFxPalette ForIndex(uint index) => themes[index < themes.Length ? (int)index : 0];
        internal static SkinFxPalette ForMaterial(Material material)
        {
            SkinFxPalette palette;
            return material && owners.TryGetValue(material, out palette) ? palette : themes[0];
        }
        // RGB is a snapshot carried by public EffectData.color, independent of owner lifetime.
        public Color32 NetworkColor => Arc;
        public static SkinFxPalette FromNetwork(Color32 color)
        {
            for (int i = 2; i < themes.Length; i++)
            {
                Color32 candidate = themes[i].Arc;
                if (candidate.r == color.r && candidate.g == color.g && candidate.b == color.b) return themes[i];
            }
            return themes[0];
        }

        /// <summary>Owned palette variant of <paramref name="source"/>. Our own VFX materials already carry
        /// the house palette, so skins 0 and 1 return them unchanged; pass force for a VANILLA material
        /// (Ukulele, Capacitor, shock overlay) that must be remapped to the house white/cyan too.</summary>
        public Material Material(Material source, bool force = false)
        {
            Material original;
            if (source && originals.TryGetValue(source, out original)) source = original;
            if (!source || (Index < 2 && !force)) return source;
            Material result;
            if (materials.TryGetValue(source, out result)) return result;
            result = new Material(source) { name = source.name + "_HSTheme" + Index };
            bool core = source == VfxAssets.ArcCore || source == VfxAssets.Spark;
            Color color = core ? Core : Arc;
            foreach (string property in new[] { "_TintColor", "_Color", "_EmissionColor" })
                if (result.HasProperty(property))
                {
                    Color old = result.GetColor(property);
                    float intensity = Mathf.Max(1f, old.maxColorComponent);
                    Color tint = color * intensity; tint.a = old.a;
                    result.SetColor(property, tint);
                }
            if (result.HasProperty("_RemapTex")) result.SetTexture("_RemapTex", Ramp());
            materials.Add(source, result);
            originals.Add(result, source);
            owners.Add(result, this);
            return result;
        }

        private Texture2D Ramp()
        {
            if (ramp) return ramp;
            ramp = new Texture2D(256, 1, TextureFormat.RGBA32, false) {
                name = "HS_ThemeRamp" + Index, wrapMode = TextureWrapMode.Clamp };
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.black, 0f),
                new GradientColorKey(Outer * 0.35f, 0.18f), new GradientColorKey(Outer, 0.45f),
                new GradientColorKey(Arc, 0.75f), new GradientColorKey(Core, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.6f, 0.3f), new GradientAlphaKey(1f, 1f) });
            for (int i = 0; i < 256; i++) ramp.SetPixel(i, 0, gradient.Evaluate(i / 255f));
            ramp.Apply(false, true);
            return ramp;
        }

        /// <summary>Only call on an owned effect clone, never a game/shared prefab.</summary>
        internal void TintHierarchy(GameObject clone, bool force = false)
        {
            if (Index < 2 && !force) return;
            foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.sharedMaterials;
                for (int i = 0; i < source.Length; i++) source[i] = Material(source[i], force);
                renderer.sharedMaterials = source;
                var line = renderer as LineRenderer;
                if (line) line.colorGradient = TintGradient(line.colorGradient);
                var trail = renderer as TrailRenderer;
                if (trail) trail.colorGradient = TintGradient(trail.colorGradient);
            }
            foreach (var system in clone.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = system.main;
                main.startColor = TintParticleColor(main.startColor);
                var fade = system.colorOverLifetime;
                if (fade.enabled) fade.color = TintParticleColor(fade.color);
            }
            foreach (var light in clone.GetComponentsInChildren<Light>(true)) light.color = Arc;
        }

        private Color TintColor(Color source)
        {
            Color result = Color.Lerp(Arc, Core, Mathf.Clamp01(source.grayscale));
            result.a = source.a;
            return result;
        }

        private Gradient TintGradient(Gradient source)
        {
            var result = new Gradient();
            var colors = source.colorKeys;
            for (int i = 0; i < colors.Length; i++) colors[i].color = TintColor(colors[i].color);
            result.SetKeys(colors, source.alphaKeys);
            result.mode = source.mode;
            return result;
        }

        internal ParticleSystem.MinMaxGradient TintParticleColor(ParticleSystem.MinMaxGradient source)
        {
            var result = source;
            switch (source.mode)
            {
                case ParticleSystemGradientMode.Color: result.color = TintColor(source.color); break;
                case ParticleSystemGradientMode.TwoColors:
                    result.colorMin = TintColor(source.colorMin); result.colorMax = TintColor(source.colorMax); break;
                case ParticleSystemGradientMode.TwoGradients:
                    result.gradientMin = TintGradient(source.gradientMin); result.gradientMax = TintGradient(source.gradientMax); break;
                default: result.gradient = TintGradient(source.gradient); break;
            }
            result.mode = source.mode;
            return result;
        }
    }
}
