using System;
using System.Linq;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    // Single construction path for the CharacterModel / ModelSkinController / SkinDef
    // trio. The playable body and the character-select mannequin both need this, and
    // they must agree: RoR2's SurvivorMannequinSlotController looks up
    // ModelSkinController with GetComponentInChildren and dereferences the result
    // unguarded, so a display prefab without one throws a NullReferenceException on
    // every loadout change.
    internal static class FoundationSkin
    {
        // Other mods append SkinDefs to every survivor's controller (EnemiesReturns adds a
        // hidden Anointed skin), so anything that counts or orders skins must filter to these.
        internal static readonly string[] OwnSkinNames =
            { "HollowSaintDefault", "HollowSaintObsidian", "HollowSaintVerdigris", "HollowSaintSolar", "HollowSaintUmbral" };

        internal static SkinDef[] OwnSkins(ModelSkinController controller)
        {
            if (!controller || controller.skins == null) return Array.Empty<SkinDef>();
            return controller.skins.Where(s => s && Array.IndexOf(OwnSkinNames, s.name) >= 0).ToArray();
        }

        // body is null for the standalone selection display. ModelSkinController.Start
        // guards its whole skin-init block on characterModel.body, and ApplySkinAsync
        // only touches characterModel.forceUpdate, so a display with no body is safe.
        internal static CharacterModel Attach(GameObject model, CharacterBody body)
        {
            var characterModel = model.AddComponent<CharacterModel>();
            characterModel.body = body;
            characterModel.autoPopulateLightInfos = true;
            characterModel.baseRendererInfos = model.GetComponentsInChildren<Renderer>().Select(r => new CharacterModel.RendererInfo
            {
                renderer = r, defaultMaterial = r.sharedMaterial, defaultShadowCastingMode = r.shadowCastingMode,
                // The foundation still uses Unity Standard materials. RoR2's
                // overlays require its own shaders and can corrupt this render path.
                ignoreOverlays = true
            }).ToArray();
            characterModel.itemDisplayRuleSet = ScriptableObject.CreateInstance<ItemDisplayRuleSet>();
            characterModel.itemDisplayRuleSet.name = "HollowSaintFoundationDisplays";

            var skin = ScriptableObject.CreateInstance<SkinDef>();
            skin.name = "HollowSaintDefault";
            skin.rootObject = model;
            skin.baseSkins = Array.Empty<SkinDef>();
            skin.skinDefParams = ScriptableObject.CreateInstance<SkinDefParams>();
            skin.skinDefParams.rendererInfos = characterModel.baseRendererInfos;
            skin.skinDefParams.gameObjectActivations = Array.Empty<SkinDefParams.GameObjectActivation>();
            skin.skinDefParams.meshReplacements = Array.Empty<SkinDefParams.MeshReplacement>();
            skin.skinDefParams.projectileGhostReplacements = Array.Empty<SkinDefParams.ProjectileGhostReplacement>();
            skin.skinDefParams.minionSkinReplacements = Array.Empty<SkinDefParams.MinionSkinReplacement>();
            skin.skinDefParams.lightReplacements = Array.Empty<CharacterModel.LightInfo>();

            skin.nameToken = "HS_SKIN_DEFAULT_NAME";
            skin.icon = SkinIcon(new Color(0.93f, 0.9f, 0.84f), new Color(0.62f, 0.36f, 0.2f), new Color(0.3f, 0.92f, 1f), new Color(0.08f, 0.1f, 0.12f));

            var obsidian = MakeVariant(model, skin, "HollowSaintObsidian", "HS_SKIN_OBSIDIAN_NAME", ObsidianTint);
            obsidian.icon = SkinIcon(new Color(0.07f, 0.075f, 0.09f), new Color(0.6f, 0.62f, 0.66f), new Color(0.3f, 0.92f, 1f), new Color(0.02f, 0.02f, 0.03f));

            var verdigris = MakeVariant(model, skin, "HollowSaintVerdigris", "HS_SKIN_VERDIGRIS_NAME", m => RelicTint(m, 0));
            verdigris.icon = SkinIcon(new Color(0.48f, 0.32f, 0.16f), new Color(0.16f, 0.48f, 0.37f),
                new Color(0.45f, 1f, 0.72f), new Color(0.16f, 0.23f, 0.13f));
            var solar = MakeVariant(model, skin, "HollowSaintSolar", "HS_SKIN_SOLAR_NAME", m => RelicTint(m, 1));
            solar.icon = SkinIcon(new Color(0.85f, 0.65f, 0.27f), new Color(1f, 0.83f, 0.42f),
                new Color(1f, 0.72f, 0.22f), new Color(0.92f, 0.9f, 0.8f));
            var umbral = MakeVariant(model, skin, "HollowSaintUmbral", "HS_SKIN_UMBRAL_NAME", m => RelicTint(m, 2));
            umbral.icon = SkinIcon(new Color(0.07f, 0.055f, 0.12f), new Color(0.27f, 0.19f, 0.36f),
                new Color(0.75f, 0.35f, 1f), new Color(0.11f, 0.065f, 0.17f));

            // ModelSkinController is [RequireComponent(typeof(CharacterModel))] and its
            // ApplySkinAsync dereferences characterModel, so both must exist together.
            // Skin order must match between the body and the select-screen display.
            model.AddComponent<ModelSkinController>().skins = new[] { skin, obsidian, verdigris, solar, umbral };
            FoundationHopoo.Apply(characterModel, new[] { skin, obsidian, verdigris, solar, umbral });
            if (body) model.AddComponent<FoundationEliteTint>();
            model.AddComponent<FoundationSkinAnimation>();
            return characterModel;
        }

        /// <summary>A recolour skin: the same renderers with tinted material clones.</summary>
        private static SkinDef MakeVariant(GameObject model, SkinDef baseSkin, string name, string token, Func<Material, Material> tint)
        {
            var cache = new System.Collections.Generic.Dictionary<Material, Material>();
            var infos = baseSkin.skinDefParams.rendererInfos.Select(info =>
            {
                var copy = info;
                if (info.defaultMaterial)
                {
                    Material tinted;
                    if (!cache.TryGetValue(info.defaultMaterial, out tinted))
                    {
                        tinted = tint(info.defaultMaterial);
                        cache[info.defaultMaterial] = tinted;
                    }
                    copy.defaultMaterial = tinted;
                }
                return copy;
            }).ToArray();
            var variant = ScriptableObject.CreateInstance<SkinDef>();
            variant.name = name;
            variant.nameToken = token;
            variant.rootObject = model;
            variant.baseSkins = Array.Empty<SkinDef>();
            variant.skinDefParams = ScriptableObject.CreateInstance<SkinDefParams>();
            variant.skinDefParams.rendererInfos = infos;
            variant.skinDefParams.gameObjectActivations = Array.Empty<SkinDefParams.GameObjectActivation>();
            variant.skinDefParams.meshReplacements = Array.Empty<SkinDefParams.MeshReplacement>();
            variant.skinDefParams.projectileGhostReplacements = Array.Empty<SkinDefParams.ProjectileGhostReplacement>();
            variant.skinDefParams.minionSkinReplacements = Array.Empty<SkinDefParams.MinionSkinReplacement>();
            variant.skinDefParams.lightReplacements = Array.Empty<CharacterModel.LightInfo>();
            return variant;
        }

        /// <summary>Obsidian Saint: black-glass plates, pewter halo and trim, cyan conductors
        /// untouched so the kit's lightning still matches.</summary>
        private static Material ObsidianTint(Material source)
        {
            string n = source.name.ToLowerInvariant();
            bool emissiveConductor = n.Contains("conductor") || n.Contains("gap light") || n.Contains("core hot");
            if (emissiveConductor) return source;
            var m = new Material(source) { name = source.name + " (Obsidian)" };
            Color tint;
            float metallic, smooth;
            if (n.Contains("ivory") || n.Contains("ceramic")) { tint = new Color(0.075f, 0.08f, 0.095f); metallic = 0.35f; smooth = 0.88f; }
            else if (n.Contains("copper")) { tint = new Color(0.58f, 0.6f, 0.64f); metallic = 0.9f; smooth = 0.7f; }
            else if (n.Contains("tabard")) { tint = new Color(0.12f, 0.12f, 0.14f); metallic = 0.1f; smooth = 0.3f; }
            else { tint = new Color(0.35f, 0.36f, 0.4f); metallic = 0.6f; smooth = 0.6f; } // graphite and baked body
            // Baked body texture materials hold ivory and graphite in one map, so they get a
            // strong uniform darken; flat materials take the tint directly.
            if (m.HasProperty("_Color")) m.color = m.mainTexture ? new Color(0.34f, 0.35f, 0.4f, 1f) : tint;
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            return m;
        }

        // Material variants retain the authored textures and geometry. Emission is
        // confined to existing light pieces; ceramic/cloth never become glowing slabs.
        private static Material RelicTint(Material source, int theme)
        {
            string n = source.name.ToLowerInvariant();
            string suffix = theme == 0 ? "Verdigris" : theme == 1 ? "Solar" : "Umbral";
            var material = new Material(source) { name = source.name + " (" + suffix + ")" };
            bool light = n.Contains("conductor") || n.Contains("core_hot") || n.Contains("core hot") ||
                n.Contains("gap light") || n.Contains("gap_light");
            bool cloth = n.Contains("tabard") && !n.Contains("trim");
            bool trim = n.Contains("copper") || n.Contains("halo bone");
            bool plate = n.Contains("ivory") || n.Contains("ceramic");
            Color tint;
            float metallic, smooth;
            if (theme == 0)
            {
                tint = light ? new Color(0.45f, 1f, 0.72f) : cloth ? new Color(0.22f, 0.3f, 0.16f) :
                    trim ? new Color(0.16f, 0.48f, 0.37f) : plate ? new Color(0.58f, 0.4f, 0.2f) : new Color(0.18f, 0.22f, 0.17f);
                metallic = trim || plate ? 0.7f : 0.2f;
                smooth = 0.42f;
            }
            else if (theme == 1)
            {
                tint = light ? new Color(1f, 0.72f, 0.22f) : cloth ? new Color(0.95f, 0.93f, 0.84f) :
                    trim ? new Color(1f, 0.83f, 0.42f) : plate ? new Color(0.85f, 0.65f, 0.27f) : new Color(0.25f, 0.21f, 0.16f);
                metallic = trim || plate ? 0.85f : 0.15f;
                smooth = 0.7f;
            }
            else
            {
                // v0.9.11: plates and body a step lighter so Umbral keeps its shape in dark scenes
                // (character select, night stages); was plate 0.07/0.055/0.12, body 0.12/0.1/0.18.
                tint = light ? new Color(0.75f, 0.35f, 1f) : cloth ? new Color(0.11f, 0.065f, 0.17f) :
                    trim ? new Color(0.27f, 0.19f, 0.36f) : plate ? new Color(0.1f, 0.08f, 0.17f) : new Color(0.16f, 0.13f, 0.23f);
                metallic = trim ? 0.7f : 0.35f;
                smooth = 0.82f;
            }
            if (material.HasProperty("_Color")) material.color = tint;
            FoundationLightMaps.Apply(material, source, theme, plate);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", cloth ? 0f : metallic);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", cloth ? 0.25f : smooth);
            if (light && material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", tint * 0.82f);
                material.EnableKeyword("_EMISSION");
            }
            return material;
        }

        /// <summary>RoR2-style four-quadrant skin swatch.</summary>
        private static Sprite SkinIcon(Color top, Color right, Color bottom, Color left)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "HS_SkinIcon" };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int dx = x - size / 2, dy = y - size / 2;
                    Color c = Math.Abs(dy) >= Math.Abs(dx) ? (dy > 0 ? top : bottom) : (dx > 0 ? right : left);
                    // thin dark seams between quadrants
                    if (Math.Abs(Math.Abs(dx) - Math.Abs(dy)) < 2) c = Color.Lerp(c, Color.black, 0.6f);
                    pixels[y * size + x] = c;
                }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
