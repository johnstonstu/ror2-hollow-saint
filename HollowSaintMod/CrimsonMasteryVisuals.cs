using System;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>Visual definition only. Registration, indices, localization and mastery
    /// achievement ownership stay with FoundationSkin/content integration.</summary>
    internal static class CrimsonMasteryVisuals
    {
        internal const string SkinName = "HollowSaintCrimsonVow";
        internal const string MaterialSuffix = " (Crimson Vow)";
        internal const string NameToken = "HS_SKIN_CRIMSON_VOW_NAME";
        internal static readonly Color Plate = new Color(0.17f, 0.18f, 0.22f);
        internal static readonly Color Graphite = new Color(0.055f, 0.06f, 0.075f);
        internal static readonly Color Cloth = new Color(0.105f, 0.027f, 0.042f);
        internal static readonly Color Crown = new Color(0.40f, 0.055f, 0.075f);
        internal static readonly Color Trim = new Color(0.62f, 0.39f, 0.20f);
        internal static readonly Color Arc = new Color(1f, 0.075f, 0.12f);
        internal static readonly Color Outer = new Color(0.52f, 0.025f, 0.065f);
        internal static readonly Color Core = new Color(1f, 0.80f, 0.70f);
        internal static readonly Color Pulse = new Color(1f, 0.73f, 0.22f);
        internal static readonly Color PulseEdge = new Color(1f, 0.92f, 0.72f);

        internal enum Role { Body, Plate, Cloth, Crown, Trim, Light, HotCore }
        internal readonly struct Style
        {
            internal readonly Role Kind;
            internal readonly Color Albedo, Emission;
            internal readonly float Metallic, Smoothness;
            internal bool IsLight => Kind == Role.Light || Kind == Role.HotCore;
            internal Style(Role kind, Color albedo, Color emission, float metal, float smooth)
            { Kind = kind; Albedo = albedo; Emission = emission; Metallic = metal; Smoothness = smooth; }
        }

        internal static bool MatchesMaterial(string name) => name != null && name.EndsWith(MaterialSuffix, StringComparison.Ordinal);
        internal static Style Describe(string sourceName)
        {
            string n = (sourceName ?? "").ToLowerInvariant();
            if (n.Contains("core_hot") || n.Contains("core hot"))
                return new Style(Role.HotCore, Arc, new Color(1f, 0.23f, 0.09f) * 0.9f, 0.25f, 0.55f);
            if (n.Contains("conductor") || n.Contains("gap light") || n.Contains("gap_light") ||
                n.Contains("contained_lightning") || n.Contains("energy_point") || n.Contains("pulse_entering"))
                return new Style(Role.Light, Arc * 0.6f, Arc * 0.75f, 0.2f, 0.48f);
            if (n.Contains("tabard") && (n.Contains("trim") || n.Contains("copper")))
                return new Style(Role.Trim, Trim, Color.black, 0.85f, 0.65f);
            if (n.Contains("tabard") || n.Contains("cloth"))
                return new Style(Role.Cloth, Cloth, Color.black, 0f, 0.18f);
            if (n.Contains("copper") || n.Contains("halo bone") || n.Contains("crescent"))
                return new Style(Role.Crown, Crown, Color.black, 0.88f, 0.62f);
            if ((n.Contains("ivory") || n.Contains("ceramic")) && !n.Contains("source_texture"))
                return new Style(Role.Plate, Plate, Color.black, 0.35f, 0.55f);
            return new Style(Role.Body, Graphite, Color.black, 0.22f, 0.38f);
        }

        /// <summary>Reuses source shading/UV detail but removes baked cyan from the new
        /// skin's diffuse atlas. Neutral authored mask reserves red for actual light pixels.</summary>
        internal static Color AtlasPixel(Color source, float lightMask, bool plate)
        {
            float value = Mathf.Clamp01(source.r * 0.2126f + source.g * 0.7152f + source.b * 0.0722f);
            Color low = plate ? Plate * 0.55f : Graphite;
            Color high = plate ? Plate * 1.25f : Plate;
            Color result = Color.Lerp(low, high, value);
            result = Color.Lerp(result, Arc * 0.3f, Mathf.Clamp01(lightMask));
            result.a = source.a;
            return result;
        }
    }
}
