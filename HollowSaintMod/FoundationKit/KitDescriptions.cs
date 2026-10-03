using System;
using System.Collections.Generic;
using System.Globalization;
using R2API;
using RoR2;
using HollowSaint.FoundationKit.Gaze;
using HollowSaint.FoundationKit.Stormspear;

namespace HollowSaint.FoundationKit
{
    /// <summary>
    /// Skill, passive and keyword descriptions generated from KitTuning, so the numbers on the
    /// select screen and the tooltips always match the live config. Refresh() runs once after the
    /// config is bound (before the tokens are first registered), and again on every config change
    /// (KitConfig calls it from each SettingChanged handler), so Risk of Options edits show up in
    /// the loadout tooltips immediately.
    /// </summary>
    public static class KitDescriptions
    {
        private static readonly Dictionary<string, string> current = new Dictionary<string, string>();
        private static bool registered;

        // ---------------------------------------------------------------- number formatting

        /// <summary>Coefficient as a whole percent, "450%". Drops the decimals when the value is whole.</summary>
        private static string Pct(float coefficient) { return Num(coefficient * 100f) + "%"; }

        /// <summary>Multiplier as its bonus, 1.5 -> "50%".</summary>
        private static string Bonus(float multiplier) { return Num((multiplier - 1f) * 100f) + "%"; }

        private static string Num(float value)
        {
            float rounded = (float)Math.Round(value, 1);
            return rounded.ToString(Math.Abs(rounded - Math.Round(rounded)) < 0.001 ? "0" : "0.#", CultureInfo.InvariantCulture);
        }

        private static string Seconds(float v) { return Num(v) + (Math.Abs(v - 1f) < 0.001f ? " second" : " seconds"); }
        private static string Enemies(int n) { return n + (n == 1 ? " enemy" : " enemies"); }
        private static string Dmg(string text) { return "<style=cIsDamage>" + text + "</style>"; }
        private static string Util(string text) { return "<style=cIsUtility>" + text + "</style>"; }

        // ---------------------------------------------------------------- text

        private static Dictionary<string, string> Build()
        {
            var t = new Dictionary<string, string>();

            // ArcBoltMaxChainTargets counts the first enemy hit (ArcBoltChain: hops = targets - 1),
            // so "chains to N more" is targets - 1.
            int hops = Math.Max(0, KitTuning.ArcBoltMaxChainTargets - 1);
            t[KitTokens.ArcBoltDesc] =
                "Snap an arc bolt for " + Dmg(Pct(KitTuning.ArcBoltDamageCoefficient) + " damage") +
                (hops > 0 ? " that " + Dmg("chains") + " to up to " + Dmg(hops.ToString(CultureInfo.InvariantCulture)) + " more " + (hops == 1 ? "enemy" : "enemies") + ". " : ". ") +
                "Each hit builds " + Dmg("Static") + ".";

            string S(float v) { return Num(v); }
            // v0.9.11: trimmed to fit the character select panel; crown details live on Open Circuit.
            t[KitTokens.ConduitSpearDesc] =
                Util("Agile.") + " Charge a spear of lightning for " + Dmg(Pct(StormspearTuning.TapDamage) + "-" + Pct(StormspearTuning.FullDamage) + " damage") +
                ". It " + Util("sticks") + ", then bursts for " + Dmg(Pct(StormspearTuning.BurstDamageFraction) + "-" + Pct(StormspearTuning.BurstDamageFractionFull) + " of its damage") + " around it." +
                (StormspearTuning.BaseStock > 1 ? " Holds " + Util(StormspearTuning.BaseStock + " charges") + "." : "");

            t[KitTokens.ArcStepDesc] =
                Util("Blink") + " a short distance in any direction, even in the air. Jump out of it to keep the momentum. Holds " +
                Util(KitTuning.ArcStepMaxStock + (KitTuning.ArcStepMaxStock == 1 ? " charge" : " charges")) + ".";

            // v0.9.15: two lines on the select panel (longer text gets auto-shrunk). The crown Thunderbolt
            // and the cooldown-after-crown rule are in the changelog and the options.
            t[KitTokens.OpenCircuitDesc] =
                Util("Agile.") + " For " + Util(Num(KitTuning.OpenCircuitBuffSeconds) + " seconds") + ", your crown strikes enemies within " +
                Util(Num(KitTuning.OpenCircuitRadius) + "m") + " for " + Dmg(Pct(KitTuning.OpenCircuitPulseDamageCoefficient) + " damage") + " every " +
                Util(Num(KitTuning.OpenCircuitPulseInterval) + "s") + " and " + Dmg("Stormspear") + " charges " + Util(Num(StormspearTuning.CrownChargeMultiplier) + "x faster") + ".";

            t[Gaze.GazeRegistration.DescToken] =
                "Rise and fire a beam for " + Util(Num(GazeTuning.BeamSeconds) + " seconds") + ", dealing " + Dmg(Pct(GazeTuning.DamagePerSecond) + " damage per second") +
                (GazeTuning.Armor > 0.5f ? ", with " + Util(Num(GazeTuning.Armor) + " bonus armor") : "") + ". Its " + Dmg("forks") + " reach further over time.";

            // v0.9.15 (Stu: numbers, wording and colours read oddly): the storm terms are all one colour
            // (damage yellow, like vanilla Shocking / Stunning / Ignite), plain sentences, whole units,
            // no numbered list. The passive says the loop in one breath; the keyword box has the detail.
            int orbs = KitTuning.StormChargeMax;
            t[KitTokens.StormDesc] =
                "Hits build " + Dmg("Static") + ". Full Static " + Dmg("Electrocutes") + " an enemy. Every " + orbs + " Electrocutes call down a " +
                Dmg("Thunderbolt") + " for " + Dmg(Pct(KitTuning.ThunderboltDamageCoefficient) + " damage") + ".";

            string jolt = KitTuning.ElectrocuteStunSeconds > 0.01f
                ? " is jolted for " + Seconds(KitTuning.ElectrocuteStunSeconds) + " (not bosses) and"
                : "";
            string staticLine = Dmg("Static") + ": every hit charges the enemy. Bigger hits and critical strikes charge it faster. It fades after " +
                Seconds(KitTuning.StaticDecayDelay) + " without a hit." +
                (KitTuning.DeathDischargeStatic > 0.01f && KitTuning.DeathDischargeStatic <= 1f
                    ? " An enemy that dies " + (KitTuning.DeathDischargeStatic >= 0.99f ? "at full Static" : "with " + Pct(KitTuning.DeathDischargeStatic) + " Static or more") + " Electrocutes as it falls."
                    : "");
            string shockLine = Dmg("Electrocute") + ": at full Static the enemy" + jolt + " is " + Dmg("Shocked") + ", taking " +
                Dmg(Bonus(KitTuning.ShockedDamageMultiplier) + " more damage") + " for " + Seconds(KitTuning.ShockedSeconds) + ". The arc jumps to " +
                Enemies(KitTuning.ElectrocutePopTargets) + " nearby for " + Dmg(Pct(KitTuning.ElectrocutePopDamageCoefficient) + " damage") +
                " and charges them too. That enemy can't build Static again for " + Seconds(KitTuning.ElectrocuteImmuneSeconds) + ".";
            string boltLine = Dmg("Thunderbolt") + ": each Electrocute lights an orb on your halo. With all " + orbs +
                " lit, they combine and strike a strong enemy within " + Num(KitTuning.ThunderboltRange) + "m for " +
                Dmg(Pct(KitTuning.ThunderboltDamageCoefficient) + " damage") + ".";
            t[KitTokens.KeywordStorm] = "<style=cKeywordName>The Storm</style><style=cSub>" + staticLine + "\n" + shockLine + "\n" + boltLine + "</style>";
            t[KitTokens.KeywordStatic] = "<style=cKeywordName>Static</style><style=cSub>" + staticLine.Substring(staticLine.IndexOf(':') + 2) + " At full Static the enemy is " + Dmg("Electrocuted") + ".</style>";
            t[KitTokens.KeywordElectrocute] = "<style=cKeywordName>Electrocute</style><style=cSub>" + shockLine.Substring(shockLine.IndexOf(':') + 2) + "</style>";
            t[KitTokens.KeywordShocked] =
                "<style=cKeywordName>Shocked</style><style=cSub>The enemy takes " + Dmg(Bonus(KitTuning.ShockedDamageMultiplier) + " more damage") +
                " from all sources for " + Seconds(KitTuning.ShockedSeconds) + ".</style>";
            return t;
        }

        // ---------------------------------------------------------------- apply

        /// <summary>Rebuilds the description tokens from KitTuning. Before the first
        /// RegisterAll it only stores the text; afterwards it also pushes it into the game's
        /// language so open tooltips and the loadout screen pick it up.</summary>
        public static void Refresh()
        {
            try
            {
                var built = Build();
                foreach (var pair in built)
                {
                    string old;
                    if (current.TryGetValue(pair.Key, out old) && old == pair.Value) continue;
                    current[pair.Key] = pair.Value;
                    if (registered) Push(pair.Key, pair.Value);
                }
            }
            catch (Exception error)
            {
                Plugin.Log.LogWarning("HOLLOW_SAINT_DESCRIPTIONS_FAILED " + error.Message);
            }
        }

        /// <summary>Registers every generated token with R2API's language store (once, from
        /// KitRegistration.RegisterTokens). Later Refresh calls update the store and the live language.</summary>
        internal static void RegisterAll()
        {
            Refresh();
            registered = true;
            foreach (var pair in current) Push(pair.Key, pair.Value);
            Language.onCurrentLanguageChanged += ReapplyLive;
        }

        private static void Push(string token, string text)
        {
            LanguageAPI.Add(token, text);
            SetLive(token, text);
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
                // The R2API store above still has the new text; it applies at the next language load.
            }
        }

        private static void ReapplyLive()
        {
            foreach (var pair in current) SetLive(pair.Key, pair.Value);
        }
    }
}
