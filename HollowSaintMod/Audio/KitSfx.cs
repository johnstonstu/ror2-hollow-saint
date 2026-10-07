using System;
using System.Collections;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>
    /// Custom Wwise events with vanilla fallbacks if the embedded bank cannot load.
    /// </summary>
    public static class KitSfx
    {
        public static string For(Beat beat)
        {
            // The Ukulele prefab may supply its own audio. Avoid layering another hit.
            if (beat == Beat.ChainHop && ChainLightningFx.HasSound) return null;
            if (CustomSoundBank.Ready)
            {
                // Dedicated impact media reuses ChainHop's crackle at 0.82 source gain.
                // Other ChainHop voices and the global SFX bus retain their original levels.
                if (beat == Beat.BoltImpact) return "Play_HS_BoltImpact";
                switch (beat)
                {
                    case Beat.ArcBoltCast: case Beat.BoltImpact: case Beat.ChainHop:
                    case Beat.SpearThrow: case Beat.SpearImpact:
                    case Beat.ArcStepStart: case Beat.ArcStepEnd:
                    case Beat.CircuitUnfold: case Beat.CircuitOpen:
                    case Beat.CircuitPulse: case Beat.CircuitClose:
                    case Beat.MeterFull: case Beat.StaticTier:
                    case Beat.ChargeTick: case Beat.Electrocute:
                    case Beat.ThunderTelegraph: case Beat.ThunderRelease: case Beat.ThunderStrike:
                    case Beat.SpearRecall: case Beat.SpearPulse: case Beat.SpearStruck: // v0.8 bank
                        return "Play_HS_" + beat;
                }
            }
            switch (beat)
            {
                case Beat.ArcBoltCast: return "Play_mage_m1_cast_lightning";
                case Beat.BoltImpact: return "Play_captain_m2_tazer_bounce";
                case Beat.ChainHop: return "Play_captain_m2_tazer_bounce";
                case Beat.SpearThrow: return "Play_captain_m2_tazer_shoot";
                case Beat.SpearImpact: return "Play_captain_m2_tazer_impact";
                case Beat.ArcStepStart: return "Play_huntress_shift_mini_blink";
                case Beat.ArcStepEnd: return "Play_loader_shift_release";
                case Beat.CircuitUnfold: return "Play_mage_R_start";
                case Beat.CircuitOpen: return "Play_loader_R_activate";
                case Beat.CircuitPulse: return "Play_loader_R_shock";
                case Beat.CircuitClose: return "Play_loader_R_expire";
                case Beat.MeterFull: return "Play_railgunner_R_gun_ready";
                case Beat.StaticTier: return "Play_captain_m2_tazer_bounce";
                case Beat.ChargeTick: return "Play_mage_m1_cast_lightning";
                case Beat.Electrocute: return "Play_captain_m2_tazer_impact";
                case Beat.ThunderTelegraph: return "Play_captain_shift_preImpact";
                case Beat.ThunderStrike: return "Play_captain_shift_impact";
                case Beat.SpearPulse: return "Play_captain_m2_tazer_bounce";
                case Beat.SpearSpread: return "Play_captain_m2_tazer_impact";
                case Beat.SpearRecall: return "Play_mage_m2_zap";
                case Beat.SpearStruck: return "Play_loader_R_shock"; // pylon zap: short and punchy
                case Beat.SpearBurst: return "Play_captain_m2_tazer_impact"; // v0.9 Stormspear impact
                case Beat.SpearStuck: return "Play_captain_m2_tazer_impact"; // v0.9.10 the spear lodges
                default: return null; // arcs ride on their parent beat's sound
            }
        }

        /// <summary>Short existing-bank layers; fallback beats already use these sounds.</summary>
        private static string ExtraFor(Beat beat)
        {
            if (CustomSoundBank.Ready)
            {
                switch (beat)
                {
                    case Beat.ArcBoltCast: return "Play_mage_m1_cast_lightning";
                    case Beat.SpearStuck: return "Play_captain_m2_tazer_impact";
                }
            }
            return beat == Beat.ThunderStrike && !CustomSoundBank.Ready ? "Play_mage_R_lightningBlast" : null;
        }

        private static readonly Dictionary<long, float> lastExtra = new Dictionary<long, float>();
        private static bool AllowExtra(Beat beat, GameObject source, bool persistentSource)
        {
            // Leave the established Thunderbolt sequence alone. New layers cannot
            // stack with attack speed or simultaneous transient hit effects.
            if (beat == Beat.ThunderStrike) return true;
            long key = (long)(uint)beat;
            if (persistentSource) key |= ((long)source.GetInstanceID()) << 8;
            float now = Time.unscaledTime;
            bool tracked = lastExtra.TryGetValue(key, out float last);
            if (tracked && now >= last && now - last < 0.25f) return false;
            if (!tracked && lastExtra.Count >= 64)
            {
                long expired = 0;
                foreach (var entry in lastExtra)
                    if (now < entry.Value || now - entry.Value >= 0.25f) { expired = entry.Key; break; }
                if (expired == 0) return false;
                lastExtra.Remove(expired);
            }
            lastExtra[key] = now;
            return true;
        }

        /// <summary>Banks our placeholder events live in. Loading an already-loaded bank is harmless.</summary>
        internal static readonly string[] Banks = { "char_Mage", "char_captain", "char_loader", "char_Huntress", "char_railgunner", "Char_DroneTech" };

        // Loops and movement sounds (vanilla events, see the bank .txt indexes).
        public static string CrownLoopStart => Sound("Play_HS_CircuitLoop", "Play_loader_R_active_loop");
        public static string CrownLoopStop => Sound("Stop_HS_CircuitLoop", "Stop_loader_R_active_loop");
        public static string GlideLoopStart => Sound("Play_HS_GlideLoop", "Play_DroneTech_Utility_Glide_Loop");
        public static string GlideLoopStop => Sound("Stop_HS_GlideLoop", "Stop_DroneTech_Utility_Glide_Loop");
        public static string GlideEnter => Sound("Play_HS_GlideEnter", "Play_loader_sprint_start");
        public static string GlideExit => Sound("Play_HS_GlideExit", "Play_loader_sprint_end");
        public static string Footstep => Sound("Play_HS_Footstep", "Play_loader_step");
        public static string AirJump => Sound("Play_HS_AirJump", "Play_mage_m2_zap");
        public static string FootstepRun => Sound("Play_HS_FootstepRun", "Play_loader_step_sprint");
        public static string Land => Sound("Play_HS_Land", "Play_loader_step_sprint");
        // v0.8 bank: spear catch and the held fan (no vanilla fallback: silent without the bank).
        public static string SpearCatch => Sound("Play_HS_SpearCatch", null);
        public static string FanStart => Sound("Play_HS_FanStart", null);
        public static string FanLoopStart => Sound("Play_HS_FanLoop", null);
        public static string FanLoopStop => Sound("Stop_HS_FanLoop", null);
        public static string FanEnd => Sound("Play_HS_FanEnd", null);

        internal static void StopThunderGather(GameObject source)
        {
            if (CustomSoundBank.Ready && source) Util.PlaySound("Stop_HS_ThunderTelegraph", source);
        }

        private static string Sound(string custom, string fallback) => CustomSoundBank.Ready ? custom : fallback;

        internal static void LoadBanks()
        {
            CustomSoundBank.Load();
            foreach (var bank in Banks)
            {
                try
                {
                    uint id;
                    var result = AkSoundEngine.LoadBank(bank, out id);
                    Plugin.Log.LogInfo("HOLLOW_SAINT_SFX_BANK " + bank + " result=" + result);
                }
                catch (Exception error)
                {
                    Plugin.Log.LogWarning("HOLLOW_SAINT_SFX_BANK " + bank + " failed: " + error.Message);
                }
            }
        }

        // Per-beat minimum gaps (seconds) so chains and multi-hit bursts do not stack into noise.
        private static float MinGap(Beat beat)
        {
            switch (beat)
            {
                case Beat.ArcBoltCast: return 0.06f;
                case Beat.BoltImpact: return 0.22f;
                case Beat.ChainHop: return 0.05f;
                case Beat.CircuitPulse: return 0.9f;
                case Beat.StaticTier: return 0.25f;
                case Beat.ChargeTick: return 0.12f;
                case Beat.Electrocute: return 0.08f;
                // Accepted gather revisions already deduplicate; a quick re-pick must
                // start its new buildup after stopping the previous one.
                case Beat.ThunderTelegraph: return 0f;
                case Beat.ThunderStrike: return 0.2f;
                case Beat.SpearPulse: return 1.0f;
                case Beat.SpearSpread: return 0.15f;
                case Beat.SpearRecall: return 0.2f;
                case Beat.SpearStruck: return 1f / 6f; // at most 6 per second
                case Beat.SpearBurst: return 0.08f;
                case Beat.SpearStuck: return 0.08f;
                default: return 0f;
            }
        }

        private static readonly Dictionary<long, float> lastPlayed = new Dictionary<long, float>();

        /// <summary>v0.9.1: Stormspear impact sound by burst radius. A charged throw (radius 5 m and up,
        /// about a third charge) gets the sampled lightning strike; a tap keeps the short synth impact.</summary>
        public const float SpearBurstHeavyRadius = 5f;
        public const float SpearHeavyCharge = 0.33f; // the charge at which the burst radius reaches 5 m
        private static string ForScaled(Beat beat, float scale)
        {
            // v0.9.10: the lodge thunks (SpearImpact); the burst after it is the sampled strike when
            // charged and a light crackle for a tap, so a tap does not play the impact twice.
            // v0.9.13 (Stu: impact sound off): one sequence. A tap gets a single punchy impact when it
            // sticks and a silent burst; a charged spear (scale = charge here) gets a light crackle as it
            // sticks and the sampled strike exactly on the burst.
            if (beat == Beat.SpearStuck)
                // v0.9.16 (Stu: light, no zap): every spear hits with the new punchy HS_SpearImpact as it sticks.
                return CustomSoundBank.Ready ? "Play_HS_SpearImpact" : "Play_captain_m2_tazer_impact";
            if (beat == Beat.SpearBurst)
                return scale >= SpearBurstHeavyRadius ? (CustomSoundBank.Ready ? "Play_HS_SpearBurst" : "Play_captain_m2_tazer_impact") : null;
            return For(beat);
        }

        /// <summary>Heavy layer on a charged or crown throw (sampled crackle); null without the bank.</summary>
        public static string SpearThrowHeavy => Sound("Play_HS_SpearThrowHeavy", null);

        /// <summary>Plays a beat's sound, throttled per beat. Pass persistentSource for a
        /// long-lived body so each body throttles independently; transient effect objects
        /// throttle on the beat alone. scale is the beat's scale (Stormspear burst radius).</summary>
        public static void Play(Beat beat, GameObject source, bool persistentSource = false, float scale = 1f)
        {
            string name = ForScaled(beat, scale);
            if (name == null || source == null) return;
            float gap = MinGap(beat);
            if (gap > 0f)
            {
                long key = (long)(uint)beat;
                if (persistentSource) key |= ((long)source.GetInstanceID()) << 8;
                float now = Time.unscaledTime;
                float last;
                if (lastPlayed.TryGetValue(key, out last) && now - last < gap && now >= last) return;
                if (lastPlayed.Count > 64) lastPlayed.Clear();
                lastPlayed[key] = now;
            }
            Util.PlaySound(name, source);
            string extra = ExtraFor(beat);
            if (extra != null && AllowExtra(beat, source, persistentSource)) Util.PlaySound(extra, source);
        }
    }
}
