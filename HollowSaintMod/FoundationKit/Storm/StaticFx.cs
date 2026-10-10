using System.Collections.Generic;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Storm
{
    /// <summary>
    /// Client-side crackle on enemies carrying Static. Runs on every machine from
    /// Plugin.Update and reads only replicated state (the bdHsStatic tier buff, plus the
    /// stunned/Shocked marker buff). Cheap by design: it scans the body list ten times a
    /// second, and spawns at most a handful of small arcs per scan.
    /// </summary>
    public static class StaticFx
    {
        private const float ScanInterval = 0.1f;
        private const int MaxArcsPerScan = 6;
        private const float SoundGap = 0.3f;

        // Seconds between crackle arcs per tier (index 0 unused).
        private static readonly float[] Interval = { 0f, 0.6f, 0.35f, 0.2f, 0.12f };

        private static readonly Dictionary<CharacterBody, float> nextArc = new Dictionary<CharacterBody, float>();
        private static float scanTimer;
        private static float lastSound = -99f;

        // ---- v0.8: the vanilla shock overlay on Shocked / Electrocuted enemies ----
        private static readonly Dictionary<CharacterBody, TemporaryOverlayInstance> overlays = new Dictionary<CharacterBody, TemporaryOverlayInstance>();
        private static readonly List<CharacterBody> overlayScratch = new List<CharacterBody>();
        private static Material shockMaterial;
        private static bool shockMaterialTried;

        private static void ShockOverlays(IReadOnlyList<CharacterBody> list)
        {
            var shocked = StormServer.ShockedBuff; var electrocuted = StormServer.ElectrocutedBuff;
            if (!shockMaterialTried)
            {
                shockMaterialTried = true;
                try { shockMaterial = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/matIsShocked.mat").WaitForCompletion(); }
                catch (System.Exception e) { Plugin.Log.LogWarning("HOLLOW_SAINT_SHOCK_OVERLAY unavailable: " + e.Message); }
            }
            if (!shockMaterial) return;
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                if (!b || KitUtil.IsHollowSaint(b)) continue;
                bool want = (shocked != null && b.HasBuff(shocked)) || (electrocuted != null && b.HasBuff(electrocuted));
                bool has = overlays.ContainsKey(b);
                if (want && !has)
                {
                    var model = b.modelLocator ? b.modelLocator.modelTransform : null;
                    var characterModel = model ? model.GetComponent<CharacterModel>() : null;
                    if (!characterModel) continue;
                    var overlay = TemporaryOverlayManager.AddOverlay(model.gameObject);
                    overlay.duration = 9999f;
                    overlay.animateShaderAlpha = false;
                    overlay.destroyComponentOnEnd = true;
                    overlay.originalMaterial = ShockMaterialFor(VictimFxTheme.ForVictim(b));
                    overlay.AddToCharacterModel(characterModel);
                    overlays[b] = overlay;
                }
                else if (!want && has) { Remove(b); }
            }
            // v0.9.1: dead or destroyed victims lose the overlay too (it used to stay on the corpse).
            overlayScratch.Clear();
            foreach (var pair in overlays)
                if (!pair.Key || !pair.Key.healthComponent || !pair.Key.healthComponent.alive) overlayScratch.Add(pair.Key);
            foreach (var dead in overlayScratch) Remove(dead);
        }

        // v0.9.1: the vanilla matIsShocked is a heavy violet-blue that read like spawn ghosts and
        // ignored the skin palette. Each palette gets an owned remap of it, toned down.
        private const float ShockOverlayStrength = 0.6f;
        private static readonly Material[] themedShock = new Material[6]; // one per palette, including Crimson (index 5)
        private static bool shockDescribed;

        private static Material ShockMaterialFor(SkinFxPalette palette)
        {
            int index = palette != null ? palette.Index : 0;
            if (index < 0 || index >= themedShock.Length) index = 0;
            if (themedShock[index]) return themedShock[index];
            try
            {
                if (!shockDescribed)
                {
                    shockDescribed = true;
                    var shader = shockMaterial.shader;
                    var names = new System.Text.StringBuilder();
                    for (int i = 0; i < shader.GetPropertyCount(); i++) names.Append(shader.GetPropertyName(i)).Append(' ');
                    Plugin.Log.LogInfo("HOLLOW_SAINT_SHOCK_OVERLAY shader=" + shader.name + " props=" + names);
                }
                var themed = new Material((palette ?? SkinFxPalette.ForIndex(0)).Material(shockMaterial, force: true));
                themed.name = "HS_ShockOverlay" + index;
                foreach (string property in new[] { "_TintColor", "_Color" })
                    if (themed.HasProperty(property))
                    {
                        Color c = themed.GetColor(property);
                        c.r *= ShockOverlayStrength; c.g *= ShockOverlayStrength; c.b *= ShockOverlayStrength;
                        themed.SetColor(property, c);
                    }
                themedShock[index] = themed;
                return themed;
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning("HOLLOW_SAINT_SHOCK_OVERLAY theme failed, using vanilla: " + e.Message);
                return themedShock[index] = shockMaterial;
            }
        }

        private static void Remove(CharacterBody b)
        {
            TemporaryOverlayInstance overlay;
            if (overlays.TryGetValue(b, out overlay) && overlay != null)
            {
                try { overlay.RemoveFromCharacterModel(); overlay.Destroy(); } catch { }
            }
            overlays.Remove(b);
        }

        public static void Tick(float dt)
        {
            var staticBuff = StormServer.StaticBuff;
            if (staticBuff == null) return;
            scanTimer -= dt;
            if (scanTimer > 0f) return;
            scanTimer = ScanInterval;

            var list = CharacterBody.readOnlyInstancesList;
            ShockOverlays(list);
            int count = list.Count;
            if (count == 0)
            {
                if (nextArc.Count > 0) nextArc.Clear();
                return;
            }
            float now = Time.time;
            int budget = MaxArcsPerScan;
            int startAt = Random.Range(0, count);
            var shocked = StormServer.ShockedBuff;
            var electrocuted = StormServer.ElectrocutedBuff;
            bool soundedThisScan = false;

            for (int n = 0; n < count && budget > 0; n++)
            {
                var b = list[(startAt + n) % count];
                if (b == null) continue;
                int tier = b.GetBuffCount(staticBuff);
                // Stunned / Shocked enemies keep a thin crackle going.
                if (tier < 3 && ((electrocuted != null && b.HasBuff(electrocuted)) || (shocked != null && b.HasBuff(shocked)))) tier = 3;
                if (tier <= 0)
                {
                    if (nextArc.Count > 0) nextArc.Remove(b);
                    continue;
                }
                tier = Mathf.Min(tier, 4);
                float due;
                if (!nextArc.TryGetValue(b, out due))
                {
                    nextArc[b] = now + Random.Range(0f, Interval[tier]);
                    continue;
                }
                if (now < due) continue;
                nextArc[b] = now + Interval[tier] * Random.Range(0.7f, 1.3f);
                budget--;

                float r = Mathf.Clamp(b.radius, 0.3f, 2.5f);
                Vector3 core = b.corePosition;
                Vector3 a = core + Random.onUnitSphere * r;
                Vector3 c = a + Random.onUnitSphere * (0.35f + 0.1f * tier);
                BeatVisuals.Play(Beat.StaticTier, a, c, tier, null, VictimFxTheme.ForVictim(b));
                if (tier >= 3 && !soundedThisScan && now - lastSound >= SoundGap)
                {
                    soundedThisScan = true;
                    lastSound = now;
                    KitSfx.Play(Beat.StaticTier, b.gameObject);
                }
            }

            if (nextArc.Count > 96)
            {
                var dead = new List<CharacterBody>();
                foreach (var pair in nextArc) if (pair.Key == null) dead.Add(pair.Key);
                for (int i = 0; i < dead.Count; i++) nextArc.Remove(dead[i]);
            }
        }
    }
}
