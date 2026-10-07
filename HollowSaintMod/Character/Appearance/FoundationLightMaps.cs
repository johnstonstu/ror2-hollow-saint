using System;
using UnityEngine;

namespace HollowSaint
{
    // Offline-authored skin atlases: no texture readback or allocations in play.
    internal static class FoundationLightMaps
    {
        private static readonly Texture2D[,] diffuse = new Texture2D[3, 2];
        private static Texture2D emission;

        internal static void Load(AssetBundle bundle)
        {
            emission = bundle.LoadAsset<Texture2D>("HS_BodyLightMask");
            if (!emission) throw new InvalidOperationException("Bundle lacks HS_BodyLightMask; use bundle13 with this DLL");
            for (int theme = 0; theme < 3; theme++)
                for (int plate = 0; plate < 2; plate++)
                {
                    string name = "HS_BodySkin" + theme + "_" + plate;
                    diffuse[theme, plate] = bundle.LoadAsset<Texture2D>(name);
                    if (!diffuse[theme, plate]) throw new InvalidOperationException("Bundle lacks " + name);
                }
        }

        internal static bool Apply(Material material, Material source, int theme, bool plate)
        {
            if (!source.mainTexture || source.mainTexture.name != "body_base" ||
                !source.HasProperty("_EmissionMap") || !source.GetTexture("_EmissionMap")) return false;
            if (!emission || !diffuse[theme, plate ? 1 : 0])
                throw new InvalidOperationException("Hollow Saint body light atlases were not loaded");
            material.mainTexture = diffuse[theme, plate ? 1 : 0];
            material.color = Color.white; // Armor tint is baked; light pixels use the arc palette.
            material.SetTexture("_EmissionMap", emission);
            Color arc = theme == 0 ? new Color(0.45f, 1f, 0.72f) :
                theme == 1 ? new Color(1f, 0.72f, 0.22f) : new Color(0.75f, 0.35f, 1f);
            float brightness = source.GetColor("_EmissionColor").maxColorComponent;
            material.SetColor("_EmissionColor", arc * brightness);
            material.EnableKeyword("_EMISSION");
            return true;
        }
    }
}
