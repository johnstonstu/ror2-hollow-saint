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
            new SkinFxPalette(4, new Color(0.75f, 0.35f, 1f)),
            new SkinFxPalette(5, CrimsonMasteryVisuals.Arc, CrimsonMasteryVisuals.Outer, CrimsonMasteryVisuals.Core)
        };
        public readonly int Index;
        public readonly Color Arc, Outer, Core, Secondary;
        private readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>();
        private readonly Dictionary<Material, Material> secondaryMaterials = new Dictionary<Material, Material>();
        private static readonly Dictionary<Material, Material> originals = new Dictionary<Material, Material>();
        private static readonly Dictionary<Material, SkinFxPalette> owners = new Dictionary<Material, SkinFxPalette>();
        private Texture2D ramp;
        private Texture2D secondaryRamp;

        private SkinFxPalette(int index, Color arc)
        {
            Index = index; Arc = arc;
            // A coloured highlight instead of a near-white centre; primary identity stays dominant.
            Core = Color.Lerp(arc, Color.white, 0.28f);
            switch (index)
            {
                case 2: Secondary = new Color(0.18f, 0.58f, 0.95f); break; // mint / blue
                case 3: Secondary = new Color(0.62f, 0.32f, 0.92f); break; // gold / violet
                case 4: Secondary = new Color(0.18f, 0.82f, 0.95f); break; // violet / ice
                case 5: Secondary = new Color(1f, 0.56f, 0.16f); break; // crimson / amber
                default: Secondary = new Color(0.62f, 0.38f, 0.95f); break; // cyan / lilac, house + Obsidian
            }
            Color outer = index < 2 ? HsPalette.OuterCyan : arc * 0.65f;
            outer.a = 1f; Outer = outer;
        }
        private SkinFxPalette(int index, Color arc, Color outer, Color core) : this(index, arc)
        { Outer = outer; Core = Color.Lerp(arc, core, 0.28f); }

        /// <summary>Cached complementary material for thin forks/sparks only. Neutral vertex
        /// colour avoids multiplying the secondary hue by the primary skin ramp.</summary>
        public Material SecondaryMaterial(Material source)
        {
            Material original;
            if (source && originals.TryGetValue(source, out original)) source = original;
            if (!source) return null;
            Material result;
            if (secondaryMaterials.TryGetValue(source, out result)) return result;
            result = new Material(source) { name = source.name + "_HSSecondary" + Index };
            foreach (string property in new[] { "_TintColor", "_Color", "_EmissionColor" })
                if (result.HasProperty(property)) result.SetColor(property, Secondary);
            if (!secondaryRamp)
            {
                secondaryRamp = new Texture2D(64, 1, TextureFormat.RGBA32, false) { name = "HS_SecondaryRamp" + Index, wrapMode = TextureWrapMode.Clamp };
                for (int i = 0; i < 64; i++)
                {
                    float t = i / 63f;
                    // Neutral ramp: the material tint supplies the hue exactly once.
                    secondaryRamp.SetPixel(i, 0, new Color(t, t, t, t));
                }
                secondaryRamp.Apply(false, true);
            }
            if (result.HasProperty("_RemapTex")) result.SetTexture("_RemapTex", secondaryRamp);
            secondaryMaterials.Add(source, result); originals.Add(result, source); owners.Add(result, this);
            return result;
        }

        public static SkinFxPalette ForBody(CharacterBody body) => ForIndex(KitUtil.IsHollowSaint(body) ? body.skinIndex : 0u);
        internal static SkinFxPalette ForModel(CharacterModel model)
        {
            if (!model) return themes[0];
            foreach (var info in model.baseRendererInfos)
            {
                string name = info.defaultMaterial ? info.defaultMaterial.name : "";
                if (CrimsonMasteryVisuals.MatchesMaterial(name)) return themes[5];
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
                bool secondary = renderer.name.IndexOf("spark", System.StringComparison.OrdinalIgnoreCase) >= 0;
                var source = renderer.sharedMaterials;
                for (int i = 0; i < source.Length; i++) source[i] = secondary ? SecondaryMaterial(source[i]) : Material(source[i], force);
                renderer.sharedMaterials = source;
                var line = renderer as LineRenderer;
                if (line) line.colorGradient = secondary ? NeutralGradient(line.colorGradient) : TintGradient(line.colorGradient);
                var trail = renderer as TrailRenderer;
                if (trail) trail.colorGradient = secondary ? NeutralGradient(trail.colorGradient) : TintGradient(trail.colorGradient);
            }
            foreach (var system in clone.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = system.main;
                bool secondary = system.name.IndexOf("spark", System.StringComparison.OrdinalIgnoreCase) >= 0;
                main.startColor = secondary ? NeutralParticleColor(main.startColor) : TintParticleColor(main.startColor);
                var fade = system.colorOverLifetime;
                if (fade.enabled) fade.color = secondary ? NeutralParticleColor(fade.color) : TintParticleColor(fade.color);
            }
            foreach (var light in clone.GetComponentsInChildren<Light>(true)) light.color = Arc;
        }

        private Color TintColor(Color source)
        {
            Color result = Color.Lerp(Arc, Core, Mathf.Clamp01(source.grayscale));
            result.a = source.a;
            return result;
        }

        private static ParticleSystem.MinMaxGradient NeutralParticleColor(ParticleSystem.MinMaxGradient source)
        {
            // Keep authored alpha and variation, while the secondary material owns RGB.
            var result = source;
            if (source.mode == ParticleSystemGradientMode.Color) result.color = new Color(1f, 1f, 1f, source.color.a);
            else if (source.mode == ParticleSystemGradientMode.TwoColors)
            {
                result.colorMin = new Color(1f, 1f, 1f, source.colorMin.a);
                result.colorMax = new Color(1f, 1f, 1f, source.colorMax.a);
            }
            else if (source.mode == ParticleSystemGradientMode.TwoGradients)
            {
                result.gradientMin = NeutralGradient(source.gradientMin);
                result.gradientMax = NeutralGradient(source.gradientMax);
            }
            else result.gradient = NeutralGradient(source.gradient);
            result.mode = source.mode;
            return result;
        }

        private static Gradient NeutralGradient(Gradient source)
        {
            if (source == null) return null;
            var result = new Gradient();
            result.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, source.alphaKeys);
            result.mode = source.mode;
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
