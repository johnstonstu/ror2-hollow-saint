using System;
using R2API;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Storm
{
    /// <summary>Royal Capacitor's impact presentation, without its damaging orb.</summary>
    internal static class RoyalCapacitorFx
    {
        private static GameObject impact;
        private static GameObject customImpact;
        private static readonly GameObject[] tintedImpact = new GameObject[6];
        private static readonly GameObject[] tintedCustomImpact = new GameObject[6];
        private static readonly GameObject[] splashImpact = new GameObject[6];
        private static bool loaded;

        internal static void Load()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                impact = Addressables.LoadAssetAsync<GameObject>(
                    RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Lightning.LightningStrikeImpact_prefab).WaitForCompletion();
                if (impact == null) throw new InvalidOperationException("LightningStrikeImpact resolved to null.");
                var effect = impact.GetComponent<EffectComponent>();
                if (effect == null) throw new InvalidOperationException("LightningStrikeImpact has no EffectComponent.");
                // Clone only our presentation: equipment/item sounds remain untouched.
                customImpact = PrefabAPI.InstantiateClone(impact, "HollowSaintThunderImpact", false);
                customImpact.GetComponent<EffectComponent>().soundName = "Play_HS_ThunderStrike";
                TrimFlash(customImpact);
                KitContent.AddEffect(customImpact);
                // v0.9.1: skins 0/1 also get an owned house-palette copy (the raw Capacitor strike is
                // royal blue). Index 1 shares index 0's copies.
                for (uint i = 0; i < tintedImpact.Length; i++)
                {
                    if (i == 1) { tintedImpact[1] = tintedImpact[0]; tintedCustomImpact[1] = tintedCustomImpact[0]; splashImpact[1] = splashImpact[0]; continue; }
                    tintedImpact[i] = PrefabAPI.InstantiateClone(impact, "HS_ThunderTheme" + i, false);
                    SkinFxPalette.ForIndex(i).TintHierarchy(tintedImpact[i], force: true);
                    if (i == 0) DescribeOnce(tintedImpact[0]);
                    TrimFlash(tintedImpact[i]);
                    KitContent.AddEffect(tintedImpact[i]);
                    tintedCustomImpact[i] = PrefabAPI.InstantiateClone(tintedImpact[i], "HS_ThunderCustomTheme" + i, false);
                    tintedCustomImpact[i].GetComponent<EffectComponent>().soundName = "Play_HS_ThunderStrike";
                    KitContent.AddEffect(tintedCustomImpact[i]);
                    splashImpact[i] = MakeSplash(tintedImpact[i], "HS_SpearSplashTheme" + i);
                }
                Plugin.Log.LogInfo("HOLLOW_SAINT_CAPACITOR_FX_READY prefab=" + impact.name +
                    " fallbackSound=" + effect.soundName + " customSound=" + customImpact.GetComponent<EffectComponent>().soundName);
            }
            catch (Exception error)
            {
                impact = null;
                customImpact = null;
                // Presentation is optional: keep the existing custom strike as an explicit fallback.
                Plugin.Log.LogError("HOLLOW_SAINT_CAPACITOR_FX_LOAD_FAILED; using custom ThunderStrike. " + error);
            }
        }

        /// <summary>Logs the strike's renderers once (name, type, bounds size) so an oversized
        /// screen-filling flash can be found and trimmed from the log alone.</summary>
        private static void DescribeOnce(GameObject root)
        {
            var parts = new System.Text.StringBuilder();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var ps = r.GetComponent<ParticleSystem>();
                float size = ps ? ps.main.startSizeMultiplier : 0f;
                parts.Append(r.name).Append('[').Append(r.GetType().Name).Append(" size=").Append(size.ToString("0.0")).Append("] ");
            }
            foreach (var l in root.GetComponentsInChildren<Light>(true)) parts.Append("light:").Append(l.name).Append(" range=").Append(l.range.ToString("0")).Append(' ');
            Plugin.Log.LogInfo("HOLLOW_SAINT_THUNDER_PARTS " + parts);
        }

        /// <summary>v0.9.1: the Capacitor strike's "Flash" billboard is 30 m and tints the whole screen
        /// cyan for several frames when it lands within ~10 m (every crown Thunderbolt). Capping its size
        /// was not enough (polish02), so our owned copies drop it; the bolt, ribbon, ring, sphere,
        /// distortion and light still sell the strike.</summary>
        private static void TrimFlash(GameObject root)
        {
            foreach (var system in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (system.name != "Flash") continue;
                var renderer = system.GetComponent<ParticleSystemRenderer>();
                if (renderer) renderer.enabled = false;
                var emission = system.emission;
                emission.enabled = false;
            }
            // The full-screen cyan is a post-process volume driven by a weight curve
            // (RoR2.PostProcessDuration). Scale that curve down rather than removing the flash.
            var log = new System.Text.StringBuilder();
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (!component) continue;
                var type = component.GetType();
                if (!type.Name.Contains("PostProcess")) continue;
                log.Append(component.name).Append(':').Append(type.Name);
                var curveField = type.GetField("ppWeightCurve", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var curve = curveField != null ? curveField.GetValue(component) as AnimationCurve : null;
                if (curve != null)
                {
                    var keys = curve.keys;
                    for (int i = 0; i < keys.Length; i++) { keys[i].value *= ScreenFlashScale; keys[i].inTangent *= ScreenFlashScale; keys[i].outTangent *= ScreenFlashScale; }
                    curveField.SetValue(component, new AnimationCurve(keys) { preWrapMode = curve.preWrapMode, postWrapMode = curve.postWrapMode });
                    log.Append("(curve x").Append(ScreenFlashScale).Append(") ");
                }
                else log.Append(' ');
            }
            if (!loggedScreenFlash) { loggedScreenFlash = true; Plugin.Log.LogInfo("HOLLOW_SAINT_THUNDER_SCREENFLASH " + (log.Length > 0 ? log.ToString() : "none")); }
        }
        private const float ScreenFlashScale = 0.3f;

        /// <summary>1.2: the Stormspear burst borrows the strike's 3D body. Silent (the spear beat owns
        /// audio), no sky ribbon (that is the Thunderbolt's signature), no screen flash, and scalable
        /// so the bloom matches the burst radius.</summary>
        private static GameObject MakeSplash(GameObject source, string name)
        {
            var splash = PrefabAPI.InstantiateClone(source, name, false);
            var effect = splash.GetComponent<EffectComponent>();
            effect.soundName = "";
            effect.applyScale = true;
            foreach (var system in splash.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = system.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                // Keep the sphere, ring and distortion; drop the sky ribbon and the radial flash
                // lines (the Thunderbolt's signatures) so the spear reads as its own strike.
                if (system.name != "LightningRibbon" && system.name != "Flash Lines") continue;
                var renderer = system.GetComponent<ParticleSystemRenderer>();
                if (renderer) renderer.enabled = false;
                var emission = system.emission;
                emission.enabled = false;
            }
            foreach (var component in splash.GetComponentsInChildren<Component>(true))
            {
                if (!component || !component.GetType().Name.Contains("PostProcess")) continue;
                if (component is Behaviour behaviour) behaviour.enabled = false;
            }
            KitContent.AddEffect(splash);
            return splash;
        }

        /// <summary>Local-only cosmetic (runs on each client from the SpearBurst beat).</summary>
        internal static void Splash(Vector3 position, SkinFxPalette palette, float scale)
        {
            if (palette == null) return;
            var prefab = splashImpact[palette.Index];
            if (!prefab || EffectCatalog.FindEffectIndexFromPrefab(prefab) == EffectIndex.Invalid) return;
            EffectManager.SpawnEffect(prefab, new EffectData { origin = position, scale = scale }, false);
        }
        private static bool loggedScreenFlash;

        internal static void VerifyCatalog()
        {
            SkinFxPaletteAudit.Verify();
            ChainLightningFx.VerifyCatalog();
            if (!customImpact) return;
            if (EffectCatalog.FindEffectIndexFromPrefab(customImpact) == EffectIndex.Invalid)
            {
                customImpact = null;
                Plugin.Log.LogError("HOLLOW_SAINT_THUNDER_IMPACT_NOT_REGISTERED; using original Capacitor presentation.");
                return;
            }
            for (int i = 0; i < tintedImpact.Length; i++)
                if (EffectCatalog.FindEffectIndexFromPrefab(tintedImpact[i]) == EffectIndex.Invalid ||
                    EffectCatalog.FindEffectIndexFromPrefab(tintedCustomImpact[i]) == EffectIndex.Invalid)
                    throw new InvalidOperationException("Thunder skin effect missing from catalog: " + i);
            Plugin.Log.LogInfo("HOLLOW_SAINT_THUNDER_IMPACT_READY customSound=Play_HS_ThunderStrike");
        }

        internal static void Strike(Vector3 position, CharacterBody owner)
        {
            if (!NetworkServer.active) return;
            var palette = SkinFxPalette.ForBody(owner);
            if (impact != null)
            {
                // Mirrors LightningStrikeOrb.OnArrival's EFFECT only. Its separate BlastAttack
                // must never run: ThunderboltDriver already owns damage, procs and splash.
                // EffectManager also dispatches the prefab's registered sound on each client.
                var presentation = CustomSoundBank.Ready && customImpact ? tintedCustomImpact[palette.Index] : tintedImpact[palette.Index];
                EffectManager.SpawnEffect(presentation, new EffectData { origin = position }, true);
            }
            else KitFx.Server(Beat.ThunderStrike, position, default(Vector3), 1f, sound: true, owner: owner);
        }
    }
}
