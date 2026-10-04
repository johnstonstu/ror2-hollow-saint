using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>Quiet cosmetic light motion, isolated to each model's property blocks.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(201)]
    public sealed class FoundationSkinAnimation : MonoBehaviour
    {
        private static readonly int StandardEmission = Shader.PropertyToID("_EmissionColor");
        private static readonly int HopooEmission = Shader.PropertyToID("_EmColor");
        // v0.8: Standard or HGStandard (config "Hopoo shading"); decided once per game start.
        private static int Emission => FoundationHopoo.Enabled ? HopooEmission : StandardEmission;
        private CharacterModel model;
        private FoundationKit.Vfx.BodyCurrentFx current;
        private MaterialPropertyBlock properties;
        private readonly List<LightTarget> targets = new List<LightTarget>();
        private float currentWeight, deathTime;

        private sealed class LightTarget
        {
            internal readonly int Index;
            internal Material Material;
            internal int Theme;
            internal bool Quiet;
            internal LightTarget(int index, Material material) { Index = index; Refresh(material); }
            internal void Refresh(Material material)
            {
                Material = material;
                Theme = FoundationSkinAnimation.Theme(material.name);
                Quiet = Theme >= 0 && !AtlasLight(material);
            }
        }

        private void Start()
        {
            model = GetComponent<CharacterModel>();
            properties = new MaterialPropertyBlock();
            if (!model) { enabled = false; return; }
            if (model.body) current = model.body.GetComponent<FoundationKit.Vfx.BodyCurrentFx>();
            // Skin application replaces defaultMaterial in these entries, retaining
            // their renderer order. Read the active entries, not body.skinIndex: the
            // character-select mannequin has no CharacterBody.
            for (int i = 0; i < model.baseRendererInfos.Length; i++)
            {
                var material = model.baseRendererInfos[i].defaultMaterial;
                if (!material || !material.HasProperty(Emission)) continue;
                string n = material.name.ToLowerInvariant();
                if (n.Contains("conductor") || n.Contains("core_hot") || n.Contains("core hot") ||
                    n.Contains("gap light") || n.Contains("gap_light") || AtlasLight(material)) targets.Add(new LightTarget(i, material));
            }
        }

        private void LateUpdate() { Tick(Time.time, Time.deltaTime); }

        internal void Tick(float now, float dt)
        {
            if (!model) return;
            if (model.body && (!model.body.healthComponent || !model.body.healthComponent.alive))
            {
                // v0.8: on death the light gutters out over 1.2 s instead of staying lit.
                deathTime += Mathf.Max(0f, dt);
                float dim = Mathf.Lerp(1f, 0.12f, Mathf.SmoothStep(0f, 1f, deathTime / 1.2f));
                foreach (var light in targets)
                {
                    if (light.Index >= model.baseRendererInfos.Length) continue;
                    var info = model.baseRendererInfos[light.Index];
                    if (info.renderer && info.defaultMaterial) SetEmission(info, info.defaultMaterial.GetColor(Emission) * dim);
                }
                currentWeight = 0f;
                return;
            }
            deathTime = 0f;
            if (model.invisibilityCount > 0) { Restore(); return; }
            if (!current && model.body) current = model.body.GetComponent<FoundationKit.Vfx.BodyCurrentFx>();
            float target = current && current.isActiveAndEnabled && current.IsActive ? current.Intensity : 0f;
            // A shared envelope leaves the travelling beat/skin hues intact while
            // removing the hard brightness cut at channel/crown/dash boundaries.
            currentWeight = Mathf.MoveTowards(currentWeight, target, Mathf.Max(0f, dt) /
                (target > currentWeight ? 0.06f : 0.09f));
            foreach (var light in targets)
            {
                int index = light.Index;
                if (index >= model.baseRendererInfos.Length) continue;
                var info = model.baseRendererInfos[index];
                if (!info.renderer || !info.defaultMaterial) continue;
                // Unity's name getter returns a managed string. Classify once per
                // active material, refreshing on skin swaps; read emission live.
                if (!ReferenceEquals(light.Material, info.defaultMaterial)) light.Refresh(info.defaultMaterial);
                int theme = light.Theme;
                bool quiet = light.Quiet;
                if (!quiet && currentWeight <= 0f)
                {
                    // Our emission property block survives skin application. Refresh
                    // the active material even at rest so an old palette cannot stick.
                    SetEmission(info, info.defaultMaterial.GetColor(Emission));
                    continue;
                }
                // 15-22% modulation around a dim baseline. No flashing, material
                // allocations or network/gameplay state changes in the frame loop.
                float phase = theme == 2 ? index * 0.67f : index * 0.07f;
                float speed = theme == 0 ? 1.3f : theme == 1 ? 2.1f : 1.7f;
                float wave = Mathf.Sin(now * speed + phase);
                float idle = !quiet ? 1f : theme == 1 ? 0.88f + 0.12f * wave : 0.85f + 0.15f * wave;
                float flow = FoundationKit.Vfx.LightningRhythm.Pulse(now, index * 0.035f);
                float intensity = idle + currentWeight * (1f - idle + 1.6f * flow);
                SetEmission(info, info.defaultMaterial.GetColor(Emission) * intensity);
            }
        }

        private static int Theme(string name)
        {
            if (CrimsonMasteryVisuals.MatchesMaterial(name)) return 3;
            if (name.EndsWith(" (Verdigris)", StringComparison.Ordinal)) return 0;
            if (name.EndsWith(" (Solar)", StringComparison.Ordinal)) return 1;
            if (name.EndsWith(" (Umbral)", StringComparison.Ordinal)) return 2;
            return -1;
        }

        private static bool AtlasLight(Material material) => FoundationHopoo.Enabled
            ? material.HasProperty("_EmTex") && material.GetTexture("_EmTex") && material.HasProperty(HopooEmission) && material.GetColor(HopooEmission).maxColorComponent > 0f
            : material.HasProperty("_EmissionMap") && material.GetTexture("_EmissionMap") && material.IsKeywordEnabled("_EMISSION") &&
              material.GetColor(StandardEmission).maxColorComponent > 0f;

        private void OnDisable() { Restore(); }

        private void Restore()
        {
            if (model && properties != null)
                foreach (var light in targets)
                {
                    int index = light.Index;
                    if (index >= model.baseRendererInfos.Length) continue;
                    var info = model.baseRendererInfos[index];
                    if (info.renderer && info.defaultMaterial && info.defaultMaterial.HasProperty(Emission))
                        SetEmission(info, info.defaultMaterial.GetColor(Emission));
                }
            currentWeight = 0f;
        }

        private void SetEmission(CharacterModel.RendererInfo info, Color color)
        {
            // Preserve CharacterModel's fade/flash values and any other writers.
            // Restoring the active material's emission on exit resets the one
            // property we own without clearing anyone else's property block.
            info.renderer.GetPropertyBlock(properties);
            properties.SetColor(Emission, color);
            info.renderer.SetPropertyBlock(properties);
        }
    }
}
