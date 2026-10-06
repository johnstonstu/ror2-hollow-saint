using System;
using R2API;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>Ukulele presentation only. ArcBoltChainServer remains the sole damage owner.</summary>
    internal static class ChainLightningFx
    {
        // Vanilla LightningOrb.Begin uses 0.1s. Hold our hop slightly longer for readability;
        // the existing server beat still supplies its original 0.07s hop staggering.
        private const float Duration = 0.18f;
        private static GameObject prefab;
        private static readonly GameObject[] variants = new GameObject[6];
        private static bool loaded;
        private static bool catalogChecked;
        internal static bool HasSound => prefab && !string.IsNullOrEmpty(prefab.GetComponent<EffectComponent>().soundName);

        internal static void Load()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                prefab = Addressables.LoadAssetAsync<GameObject>(
                    RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_ChainLightning.ChainLightningOrbEffect_prefab).WaitForCompletion();
                if (!prefab || !prefab.GetComponent<OrbEffect>())
                    throw new InvalidOperationException("Ukulele prefab is missing its OrbEffect presentation component.");
                var effect = prefab.GetComponent<EffectComponent>();
                if (!effect) throw new InvalidOperationException("Ukulele prefab is missing EffectComponent.");
                // v0.9.1: the default and Obsidian skins get an owned house-palette (white/cyan) copy
                // too; the raw Ukulele prefab is royal blue and clashed with the rest of the kit.
                for (uint i = 0; i < variants.Length; i++)
                {
                    if (i == 1) { variants[1] = variants[0]; continue; }
                    var palette = SkinFxPalette.ForIndex(i);
                    variants[i] = PrefabAPI.InstantiateClone(prefab, "HS_ChainTheme" + i, false);
                    palette.TintHierarchy(variants[i], force: true);
                    TintOrbEnds(variants[i], palette, "HS_ChainEnd" + i);
                    KitContent.AddEffect(variants[i]);
                }
                Plugin.Log.LogInfo("HOLLOW_SAINT_CHAIN_FX_READY prefab=" + prefab.name +
                    " sound=" + effect.soundName + " duration=" + Duration);
            }
            catch (Exception error)
            {
                prefab = null;
                Plugin.Log.LogError("HOLLOW_SAINT_CHAIN_FX_LOAD_FAILED; keeping custom chain visuals. " + error);
            }
        }

        /// <summary>The orb's start/end impact effects are separate prefabs outside the hierarchy;
        /// give the clone owned, tinted copies so the hit flash matches the arc.</summary>
        private static void TintOrbEnds(GameObject clone, SkinFxPalette palette, string name)
        {
            var orb = clone.GetComponent<OrbEffect>();
            if (!orb) return;
            if (orb.endEffect)
            {
                var end = PrefabAPI.InstantiateClone(orb.endEffect, name, false);
                palette.TintHierarchy(end, force: true);
                Calm(end);
                if (end.GetComponent<EffectComponent>()) KitContent.AddEffect(end);
                orb.endEffect = end;
            }
            if (orb.startEffect)
            {
                var start = PrefabAPI.InstantiateClone(orb.startEffect, name + "Start", false);
                palette.TintHierarchy(start, force: true);
                if (start.GetComponent<EffectComponent>()) KitContent.AddEffect(start);
                orb.startEffect = start;
            }
            Plugin.Log.LogInfo("HOLLOW_SAINT_CHAIN_ENDS " + name + " end=" + (orb.endEffect ? orb.endEffect.name : "none") +
                " start=" + (orb.startEffect ? orb.startEffect.name : "none"));
        }

        /// <summary>1.2: the vanilla hop impact is a big omni ring + white hitspark starburst
        /// (Ukulele hits one enemy at a time; Arc Bolt and Static hop constantly). At full size
        /// it buried enemies in white (full-kit review). Keep the flash, at a fraction of the size.</summary>
        private static void Calm(GameObject effect)
        {
            int changed = 0;
            foreach (var ps in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.startSizeMultiplier *= .45f;
                main.startSpeedMultiplier *= .6f;
                Color c = main.startColor.color; main.startColor = new Color(c.r * .75f, c.g * .75f, c.b * .75f, c.a);
                var r = ps.GetComponent<ParticleSystemRenderer>();
                if (r && r.renderMode == ParticleSystemRenderMode.Stretch) { r.velocityScale *= .4f; r.lengthScale *= .5f; }
                changed++;
            }
            foreach (var light in effect.GetComponentsInChildren<Light>(true)) light.range *= .5f;
            Plugin.Log.LogInfo("HOLLOW_SAINT_CHAIN_END_CALM " + effect.name + " systems=" + changed);
        }

        /// <summary>Called by the received ChainHop beat on each client. False requests fallback.</summary>
        internal static void VerifyCatalog()
        {
            if (!prefab) return;
            for (int i = 0; i < variants.Length; i++)
                if (EffectCatalog.FindEffectIndexFromPrefab(variants[i]) == EffectIndex.Invalid)
                    throw new InvalidOperationException("Chain skin effect missing from catalog: " + i);
            Plugin.Log.LogInfo("HOLLOW_SAINT_SKIN_CHAIN_READY variants=5 (house palette for skins 0/1; includes Crimson)");
        }

        internal static bool TryPlay(Vector3 from, Vector3 to, float scale, SkinFxPalette palette)
        {
            if (!prefab) return false;
            GameObject endpoint = null;
            try
            {
                if (!catalogChecked)
                {
                    if (EffectCatalog.FindEffectIndexFromPrefab(prefab) == EffectIndex.Invalid)
                        throw new InvalidOperationException("Ukulele prefab is absent from the effect catalog.");
                    catalogChecked = true;
                }
                endpoint = new GameObject("HS_ChainVisualEndpoint");
                endpoint.transform.position = to;
                // OrbEffect.Reset resolves modelChildIndex=-1 directly to rootObject; it
                // needs only a Transform. No HurtBox, health, collider or NetworkIdentity.
                // This snapshot stays valid even if the hop killed/despawned its victim.
                var data = new EffectData
                {
                    origin = from,
                    genericFloat = Duration,
                    scale = Mathf.Max(1f, scale)
                };
                data.SetNetworkedObjectReference(endpoint);
                // The outer ChainHop beat already travelled over the network. Sending this
                // local dummy over it again would discard its non-networked reference.
                EffectManager.SpawnEffect(variants[palette.Index] ? variants[palette.Index] : prefab, data, false);
                UnityEngine.Object.Destroy(endpoint, Duration + 1f);
                return true;
            }
            catch (Exception error)
            {
                if (endpoint) UnityEngine.Object.Destroy(endpoint);
                prefab = null; // log once, then use the existing custom fallback for later hops
                Plugin.Log.LogError("HOLLOW_SAINT_CHAIN_FX_PLAY_FAILED; keeping custom chain visuals. " + error);
                return false;
            }
        }
    }
}
