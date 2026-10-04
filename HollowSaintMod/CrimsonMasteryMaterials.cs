using System;
using System.Collections.Generic;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>Cached, setup-only material/atlas construction. Reuses existing meshes,
    /// body_base and HS_BodyLightMask; no asset downloads, new shaders or runtime ticking.</summary>
    internal static class CrimsonMasteryMaterials
    {
        private static Texture2D lightMask;
        private static bool warned;
        private static readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>();
        private static readonly Dictionary<Material, Color> intendedAlbedos = new Dictionary<Material, Color>();
        private static readonly Dictionary<Texture, Texture2D[]> atlases = new Dictionary<Texture, Texture2D[]>();

        /// <summary>Call beside FoundationLightMaps.Load, before MakeVariant. Already shipped in the bundle.</summary>
        internal static void Load(AssetBundle bundle)
        {
            if (lightMask) return;
            if (!bundle) throw new ArgumentNullException(nameof(bundle));
            lightMask = bundle.LoadAsset<Texture2D>("HS_BodyLightMask");
            if (!lightMask) throw new InvalidOperationException("Crimson Vow needs the existing HS_BodyLightMask");
        }

        /// <summary>Pass as MakeVariant's tint delegate BEFORE optional FoundationHopoo conversion.</summary>
        internal static Material Tint(Material source)
        {
            if (!source) return source;
            Material cached;
            if (materials.TryGetValue(source, out cached) && cached) return cached;
            var style = CrimsonMasteryVisuals.Describe(source.name);
            var result = new Material(source) { name = source.name + CrimsonMasteryVisuals.MaterialSuffix };
            intendedAlbedos[result] = style.Albedo;
            bool maskedBodyEmission = false;
            try
            {
                if (result.HasProperty("_Color")) result.SetColor("_Color", style.Albedo);
                if (result.HasProperty("_Metallic")) result.SetFloat("_Metallic", style.Metallic);
                if (result.HasProperty("_Glossiness")) result.SetFloat("_Glossiness", style.Smoothness);
                if (result.HasProperty("_EmissionColor")) result.SetColor("_EmissionColor", style.Emission);
                if (style.IsLight) result.EnableKeyword("_EMISSION");
                else result.DisableKeyword("_EMISSION");

                if (source.mainTexture && source.mainTexture.name == "body_base")
                {
                    if (!lightMask) throw new InvalidOperationException("Call CrimsonMasteryMaterials.Load before Tint");
                    // The GPU emission mask does not require CPU diffuse readback.
                    // Install it first so an atlas failure retains the red light pixels.
                    if (result.HasProperty("_EmissionMap") && result.HasProperty("_EmissionColor"))
                    {
                        result.SetTexture("_EmissionMap", lightMask);
                        result.SetColor("_EmissionColor", CrimsonMasteryVisuals.Arc * 0.72f);
                        result.EnableKeyword("_EMISSION"); // neutral mask excludes armor and cloth
                        maskedBodyEmission = true;
                    }
                    else result.DisableKeyword("_EMISSION"); // never emit across an unmasked body
                    bool headless = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
                    result.mainTexture = headless ? Texture2D.whiteTexture : Atlas(source.mainTexture, style.Kind == CrimsonMasteryVisuals.Role.Plate);
                    if (!headless && result.HasProperty("_Color")) result.SetColor("_Color", Color.white);
                }
                materials[source] = result;
                return result;
            }
            catch (Exception error)
            {
                // Cosmetic readback/mask failure must not disable survivor registration.
                if (!warned) { warned = true; Plugin.Log.LogWarning("Crimson Vow atlas fallback: " + error.Message); }
                if (source.mainTexture && source.mainTexture.name == "body_base")
                {
                    result.mainTexture = source.mainTexture;
                    if (result.HasProperty("_Color")) result.SetColor("_Color", style.Albedo);
                    if (!maskedBodyEmission) result.DisableKeyword("_EMISSION");
                }
                materials[source] = result;
                return result;
            }
        }

        // Atlas pixels already contain the skin tint, so _Color becomes white. Shader
        // conversion must still classify the authored albedo rather than that neutral tint.
        internal static bool TryGetIntendedAlbedo(Material material, out Color albedo) =>
            intendedAlbedos.TryGetValue(material, out albedo);

        private static Texture2D Atlas(Texture source, bool plate)
        {
            Texture2D[] variants;
            if (!atlases.TryGetValue(source, out variants)) { variants = new Texture2D[2]; atlases.Add(source, variants); }
            int index = plate ? 1 : 0;
            if (variants[index]) return variants[index];
            // Bound setup cost. Readback handles bundle textures that are not CPU-readable.
            int width = Mathf.Min(2048, source.width), height = Mathf.Min(2048, source.height);
            Color[] pixels = ReadPixels(source, width, height);
            Color[] mask = ReadPixels(lightMask, width, height);
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = CrimsonMasteryVisuals.AtlasPixel(pixels[i], Mathf.Max(mask[i].r, Mathf.Max(mask[i].g, mask[i].b)), plate);
            var atlas = new Texture2D(width, height, TextureFormat.RGBA32, true)
            { name = "HS_CrimsonVow_" + (plate ? "Plate" : "Body"), wrapMode = source.wrapMode, filterMode = source.filterMode };
            atlas.SetPixels(pixels); atlas.Apply(true, true);
            variants[index] = atlas;
            return atlas;
        }

        private static Color[] ReadPixels(Texture source, int width, int height)
        {
            var previous = RenderTexture.active;
            var temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D readable = null;
            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                readable = new Texture2D(width, height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                return readable.GetPixels();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
                if (readable) UnityEngine.Object.Destroy(readable);
            }
        }
    }
}
