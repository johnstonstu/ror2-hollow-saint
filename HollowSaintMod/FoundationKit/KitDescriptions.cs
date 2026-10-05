using System;
using System.Collections.Generic;
using System.Globalization;
using RoR2;
using HollowSaint.FoundationKit.Gaze;
using HollowSaint.FoundationKit.Stormspear;

namespace HollowSaint.FoundationKit
{
    /// <summary>
    /// Skill, passive and keyword descriptions. The sentences live in HollowSaint.language as
    /// templates; the numbers come from KitTuning, so a Risk of Options edit updates the loadout
    /// tooltips in whichever language is active. Refresh runs after the config is bound and again
    /// from each SettingChanged handler. The formatted text is written per language (an overlay),
    /// not by LanguageAPI.Add of an English sentence, which would become the fallback for every
    /// language and hide the translations.
    /// </summary>
    public static class KitDescriptions
    {
        private static bool subscribed;

        private static string Pct(float coefficient) { return Num(coefficient * 100f) + "%"; }
        private static string Bonus(float multiplier) { return Num((multiplier - 1f) * 100f) + "%"; }

        private static string Num(float value)
        {
            float rounded = (float)Math.Round(value, 1);
            return rounded.ToString(Math.Abs(rounded - Math.Round(rounded)) < 0.001 ? "0" : "0.#", CultureInfo.InvariantCulture);
        }

        private static Dictionary<string, string> BuildArgs()
        {
            int hops = Math.Max(0, KitTuning.ArcBoltMaxChainTargets - 1);
            float discharge = KitTuning.DeathDischargeStatic;
            string death = "off";
            if (discharge > 0.01f && discharge <= 1f) death = discharge >= 0.99f ? "full" : "partial";
            var args = new Dictionary<string, string>();
            args["boltDamage"] = Pct(KitDamagePolicy.Effective(KitTuning.ArcBoltDamageCoefficient));
            args["hops"] = hops.ToString(CultureInfo.InvariantCulture);
            args["tap"] = Pct(KitDamagePolicy.Effective(StormspearTuning.TapDamage));
            args["full"] = Pct(SpearFeedbackPolicy.Direct(StormspearTuning.FullDamage, 1f));
            args["burst"] = Pct(StormspearTuning.BurstDamageFraction);
            args["burstFull"] = Pct(StormspearTuning.BurstDamageFractionFull);
            args["spearStock"] = StormspearTuning.BaseStock.ToString(CultureInfo.InvariantCulture);
            args["conductorSeconds"] = Num(SpearConductorSchedule.Lifetime);
            args["conductorTargets"] = SpearConductorSchedule.VictimsPerTick.ToString(CultureInfo.InvariantCulture);
            args["conductorInterval"] = SpearConductorSchedule.Interval.ToString("0.##", CultureInfo.InvariantCulture);
            args["conductorTap"] = Pct(KitDamagePolicy.Effective(SpearConductorSchedule.TapCoefficient));
            args["conductorFull"] = Pct(KitDamagePolicy.Effective(SpearConductorSchedule.FullCoefficient));
            args["stepStock"] = KitTuning.ArcStepMaxStock.ToString(CultureInfo.InvariantCulture);
            args["circuitSeconds"] = Num(KitTuning.OpenCircuitBuffSeconds);
            args["radius"] = Num(KitTuning.OpenCircuitRadius);
            args["pulse"] = Pct(KitDamagePolicy.Effective(KitTuning.OpenCircuitPulseDamageCoefficient));
            args["interval"] = Num(KitTuning.OpenCircuitPulseInterval);
            args["mult"] = Num(StormspearTuning.CrownChargeMultiplier);
            args["gazeSeconds"] = Num(GazeDurationPolicy.ForLevel(GazeTuning.BeamSeconds, 1f));
            args["gazeMaxSeconds"] = Num(GazeDurationPolicy.ForLevel(GazeTuning.BeamSeconds, 21f));
            args["pulseInterval"] = GazeManualRequestPolicy.MinimumInterval.ToString("0.##", CultureInfo.InvariantCulture);
            args["dps"] = Pct(GazeTuning.DamagePerSecond);
            args["armorOn"] = GazeTuning.Armor > 0.5f ? "yes" : "no";
            args["armor"] = Num(GazeTuning.Armor);
            args["orbs"] = KitTuning.StormChargeMax.ToString(CultureInfo.InvariantCulture);
            args["thunder"] = Pct(KitDamagePolicy.Effective(KitTuning.ThunderboltDamageCoefficient));
            args["fundedThunder"] = Pct(KitDamagePolicy.Effective(KitTuning.ThunderboltDamageCoefficient) * SpearFeedbackPolicy.FundedStrikeMultiplier);
            args["ordinaryThunder"] = Pct(KitDamagePolicy.Effective(KitTuning.ThunderboltDamageCoefficient) * SpearFeedbackPolicy.OrdinaryStrikeMultiplier);
            args["thunderRadius"] = Num(KitTuning.ThunderboltSplashRadius);
            args["gazeRecoveryBank"] = Pct(GazeRecoveryBudget.FullBankFraction);
            args["gazeRecoveryEach"] = Pct(GazeRecoveryBudget.FullBankFraction / Storm.StoredPrayerPolicy.Capacity(KitTuning.StormChargeMax));
            args["dwellSeconds"] = Num(OpenCircuit.CircuitDwellPolicy.RequiredSeconds);
            args["dwellZap"] = Pct(KitDamagePolicy.Effective(OpenCircuit.CircuitDwellPolicy.RawZapCoefficient));
            args["decay"] = Num(KitTuning.StaticDecayDelay);
            args["death"] = death;
            args["deathPct"] = Pct(discharge);
            args["jolt"] = KitTuning.ElectrocuteStunSeconds > 0.01f ? "on" : "off";
            args["stun"] = Num(KitTuning.ElectrocuteStunSeconds);
            args["bonus"] = Bonus(KitTuning.ShockedDamageMultiplier);
            args["shocked"] = Num(KitTuning.ShockedSeconds);
            args["targets"] = KitTuning.ElectrocutePopTargets.ToString(CultureInfo.InvariantCulture);
            args["pop"] = Pct(KitTuning.ElectrocutePopDamageCoefficient);
            args["immune"] = Num(KitTuning.ElectrocuteImmuneSeconds);
            args["range"] = Num(KitTuning.ThunderboltRange);
            return args;
        }

        /// <summary>Rebuilds the description tokens from KitTuning and pushes them into every
        /// language the file knows. Open tooltips pick the new numbers up the next time the game
        /// asks for the token.</summary>
        public static void Refresh()
        {
            try
            {
                KitLanguage.Install();
                var doc = KitLanguage.Document;
                Dictionary<string, string> english;
                if (!doc.TryGetValue("strings", out english))
                    throw new InvalidOperationException("HollowSaint.language has no strings section.");
                var args = BuildArgs();
                var formatted = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var lang in doc)
                {
                    string code = lang.Key.Equals("strings", StringComparison.OrdinalIgnoreCase) ? "generic" : lang.Key;
                    string rules = code == "generic" ? "en" : code;
                    var map = new Dictionary<string, string>();
                    for (int t = 0; t < KitLanguage.DynamicTokens.Length; t++)
                    {
                        string token = KitLanguage.DynamicTokens[t];
                        string template;
                        if (!lang.Value.TryGetValue(token, out template) || template.Length == 0)
                            template = english[token];
                        map[token] = Format(template, rules, args, english[token], code, token);
                    }
                    formatted[code] = map;
                }
                KitLanguage.PushOverlay(formatted);
                ApplyLive(formatted);
            }
            catch (Exception error)
            {
                Plugin.Log.LogWarning("HOLLOW_SAINT_DESCRIPTIONS_FAILED " + error.Message);
            }
        }

        private static string Format(string template, string rules, Dictionary<string, string> args, string englishTemplate, string code, string token)
        {
            try { return LangFormat.Apply(template, rules, args); }
            catch (Exception error)
            {
                Plugin.Log.LogWarning("HOLLOW_SAINT_DESCRIPTION_TEMPLATE " + code + " " + token + " " + error.Message);
                return LangFormat.Apply(englishTemplate, "en", args);
            }
        }

        /// <summary>Subscribes to language changes. Later Refresh calls (config edits) update the overlay.</summary>
        internal static void RegisterAll()
        {
            Refresh();
            if (subscribed) return;
            subscribed = true;
            Language.onCurrentLanguageChanged += ReapplyLive;
        }

        private static void ApplyLive(Dictionary<string, Dictionary<string, string>> formatted)
        {
            string code = "generic";
            try
            {
                var language = Language.currentLanguage;
                if (language != null && !string.IsNullOrEmpty(language.name) && formatted.ContainsKey(language.name))
                    code = language.name;
            }
            catch (Exception) { }
            Dictionary<string, string> map;
            if (!formatted.TryGetValue(code, out map) && !formatted.TryGetValue("generic", out map)) return;
            foreach (var pair in map) SetLive(pair.Key, pair.Value);
        }

        private static void SetLive(string token, string text)
        {
            try
            {
                var language = Language.currentLanguage;
                if (language != null) language.SetStringByToken(token, text);
            }
            catch (Exception)
            {
                // The overlay still returns the new text the next time the token is resolved.
            }
        }

        private static void ReapplyLive()
        {
            Refresh();
        }
    }
}
