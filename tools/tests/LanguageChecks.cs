using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace HollowSaint.FoundationKit
{
    /// <summary>
    /// Offline checks for HollowSaint.language. Compiled together with LanguageJson.cs and
    /// LangFormat.cs by tools/tests/Check-Language.ps1 (C# 5, for Windows PowerShell 5.1).
    /// </summary>
    public static class LanguageChecks
    {
        static readonly string[] RequiredLanguages = { "strings", "zh-CN", "ru", "pt-BR" };
        static readonly string[] Tokens = {
            "HS_NAME", "HS_SUBTITLE", "HS_DESCRIPTION", "HS_OUTRO", "HS_OUTRO_FAILURE",
            "HS_SKILL_ARCBOLT_NAME", "HS_SKILL_ARCBOLT_DESC",
            "HS_SKILL_SPEAR_NAME", "HS_SKILL_SPEAR_DESC",
            "HS_SKILL_ARCSTEP_NAME", "HS_SKILL_ARCSTEP_DESC",
            "HS_SKILL_CIRCUIT_NAME", "HS_SKILL_CIRCUIT_DESC",
            "HS_SKILL_GAZE_NAME", "HS_SKILL_GAZE_DESC",
            "HS_PASSIVE_STORM_NAME", "HS_PASSIVE_STORM_DESC",
            "HS_KEYWORD_STORM", "HS_KEYWORD_STATIC", "HS_KEYWORD_ELECTROCUTE", "HS_KEYWORD_SHOCKED",
            "HS_LORE", "HS_BODY_LORE",
            "HS_SKIN_DEFAULT_NAME", "HS_SKIN_OBSIDIAN_NAME", "HS_SKIN_VERDIGRIS_NAME", "HS_SKIN_SOLAR_NAME", "HS_SKIN_UMBRAL_NAME",
            "HS_SKIN_CRIMSON_VOW_NAME", "ACHIEVEMENT_HOLLOWSAINTCLEARGAMEMONSOON_NAME", "ACHIEVEMENT_HOLLOWSAINTCLEARGAMEMONSOON_DESCRIPTION",
            "HS_SKILL_GAZE_PULSE_NAME", "HS_SKILL_GAZE_PULSE_DESC", "HS_SKILL_GAZE_LOCK_NAME", "HS_SKILL_GAZE_LOCK_DESC",
            "HS_GAZE_TIMER_LABEL",
            "HS_OPTION_SPEAR_HAND_NAME", "HS_OPTION_SPEAR_HAND_DESC",
            "HS_OPTION_SPEAR_HAND_AUTO", "HS_OPTION_SPEAR_HAND_LEFT", "HS_OPTION_SPEAR_HAND_RIGHT"
        };
        static readonly string[] Dynamic = {
            "HS_SKILL_ARCBOLT_DESC", "HS_SKILL_SPEAR_DESC", "HS_SKILL_ARCSTEP_DESC", "HS_SKILL_CIRCUIT_DESC",
            "HS_SKILL_GAZE_DESC", "HS_PASSIVE_STORM_DESC",
            "HS_KEYWORD_STORM", "HS_KEYWORD_STATIC", "HS_KEYWORD_ELECTROCUTE", "HS_KEYWORD_SHOCKED"
        };

        public static string Run(string path)
        {
            Check(File.Exists(path), "Missing " + path);
            var doc = LanguageJson.Parse(File.ReadAllText(path));
            foreach (string lang in RequiredLanguages) Check(doc.ContainsKey(lang), "Missing language section " + lang);
            Dictionary<string, string> english = doc["strings"];
            Check(english.Count == Tokens.Length, "Expected " + Tokens.Length + " tokens, got " + english.Count);
            foreach (string token in Tokens) Check(english.ContainsKey(token), "English is missing " + token);
            Check(english.Count == Tokens.Length, "Unexpected extra English tokens.");

            foreach (var lang in doc)
            {
                Check(lang.Value.Count == english.Count, lang.Key + " has " + lang.Value.Count + " tokens, English has " + english.Count);
                foreach (string token in Tokens)
                {
                    string value;
                    Check(lang.Value.TryGetValue(token, out value), lang.Key + " is missing " + token);
                    Check(value != null && value.Length > 0, lang.Key + " " + token + " is empty");
                    CompareShape(english[token], value, lang.Key + " " + token);
                    CheckPluralPolicy(value, lang.Key, token);
                }
                Check(lang.Value["HS_LORE"] == lang.Value["HS_BODY_LORE"], lang.Key + " HS_BODY_LORE must match HS_LORE");
            }

            PluralCategories();
            EnglishMatchesLegacy(english);
            EveryLanguageFormats(doc);
            return "LANGUAGE_PASS: " + Tokens.Length + " tokens, " + doc.Count + " sections, placeholders, tags, plurals and English legacy text.";
        }

        static void CompareShape(string english, string other, string label)
        {
            var enNames = new List<string>();
            var enTags = new List<string>();
            var enPlural = new List<string>();
            var enSelect = new List<string>();
            var otNames = new List<string>();
            var otTags = new List<string>();
            var otPlural = new List<string>();
            var otSelect = new List<string>();
            LangFormat.Inspect(english, enNames, enTags, enPlural, enSelect);
            LangFormat.Inspect(other, otNames, otTags, otPlural, otSelect);
            Check(SameSet(enNames, otNames), label + " placeholders differ: [" + Join(enNames) + "] vs [" + Join(otNames) + "]");
            Check(SameSet(enTags, otTags), label + " tags differ: [" + Join(enTags) + "] vs [" + Join(otTags) + "]");
            Check(Balanced(english) && Balanced(other), label + " has unbalanced style or color tags");
            Check(SameSelects(enSelect, otSelect), label + " select branches differ: [" + Join(enSelect) + "] vs [" + Join(otSelect) + "]");
        }

        static void CheckPluralPolicy(string template, string language, string token)
        {
            var plural = new List<string>();
            LangFormat.Inspect(template, new List<string>(), new List<string>(), plural, new List<string>());
            foreach (string block in plural)
            {
                int eq = block.IndexOf('=');
                string keys = eq < 0 ? block : block.Substring(eq + 1);
                Check(HasKey(keys, "other"), language + " " + token + " plural block lacks other: " + block);
                if (language.Equals("ru", StringComparison.OrdinalIgnoreCase))
                {
                    Check(HasKey(keys, "few") && HasKey(keys, "many"), language + " " + token + " plural block needs few and many: " + block);
                }
            }
        }

        static bool HasKey(string keys, string key)
        {
            foreach (string part in keys.Split(',')) if (part == key) return true;
            return false;
        }

        static bool SameSelects(List<string> a, List<string> b)
        {
            return SameSet(SelectMap(a), SelectMap(b));
        }

        static List<string> SelectMap(List<string> blocks)
        {
            var list = new List<string>();
            foreach (string block in blocks)
            {
                int eq = block.IndexOf('=');
                string name = block.Substring(0, eq);
                string[] parts = block.Substring(eq + 1).Split(',');
                Array.Sort(parts, StringComparer.Ordinal);
                list.Add(name + "=" + string.Join(",", parts));
            }
            return list;
        }

        static bool SameSet(List<string> a, List<string> b)
        {
            var left = new List<string>(a);
            var right = new List<string>(b);
            left.Sort(StringComparer.Ordinal);
            right.Sort(StringComparer.Ordinal);
            left = Unique(left);
            right = Unique(right);
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++) if (left[i] != right[i]) return false;
            return true;
        }

        static List<string> Unique(List<string> sorted)
        {
            var list = new List<string>();
            foreach (string item in sorted)
                if (list.Count == 0 || list[list.Count - 1] != item) list.Add(item);
            return list;
        }

        static bool Balanced(string text)
        {
            return Count(text, "<style") == Count(text, "</style>") && Count(text, "<color") == Count(text, "</color>");
        }

        static int Count(string text, string needle)
        {
            int n = 0, i = 0;
            while (true)
            {
                i = text.IndexOf(needle, i, StringComparison.Ordinal);
                if (i < 0) return n;
                n++;
                i += needle.Length;
            }
        }

        static void PluralCategories()
        {
            Check(LangFormat.Category("en", 0) == "other", "en 0");
            Check(LangFormat.Category("en", 1) == "one", "en 1");
            Check(LangFormat.Category("en", 1.0) == "one", "en 1.0");
            Check(LangFormat.Category("en", 1.5) == "other", "en 1.5");
            Check(LangFormat.Category("en", 2) == "other", "en 2");
            Check(LangFormat.Category("pt-BR", 0) == "other", "pt 0");
            Check(LangFormat.Category("pt-BR", 1) == "one", "pt 1");
            Check(LangFormat.Category("pt-BR", 2) == "other", "pt 2");
            Check(LangFormat.Category("zh-CN", 1) == "other", "zh 1");
            Check(LangFormat.Category("zh-CN", 2) == "other", "zh 2");
            Check(LangFormat.Category("generic", 1) == "one", "generic 1");
            Check(LangFormat.Category("ru", 1) == "one", "ru 1");
            Check(LangFormat.Category("ru", 21) == "one", "ru 21");
            Check(LangFormat.Category("ru", 11) == "many", "ru 11");
            Check(LangFormat.Category("ru", 111) == "many", "ru 111");
            Check(LangFormat.Category("ru", 2) == "few", "ru 2");
            Check(LangFormat.Category("ru", 4) == "few", "ru 4");
            Check(LangFormat.Category("ru", 22) == "few", "ru 22");
            Check(LangFormat.Category("ru", 12) == "many", "ru 12");
            Check(LangFormat.Category("ru", 5) == "many", "ru 5");
            Check(LangFormat.Category("ru", 0) == "many", "ru 0");
            Check(LangFormat.Category("ru", 1.5) == "other", "ru 1.5");
            var ru = new Dictionary<string, string>();
            ru["n"] = "1";
            Check(LangFormat.Apply("{n, plural, one {один} few {несколько} many {много} other {другое}}", "ru", ru) == "один", "ru apply 1");
            ru["n"] = "2";
            Check(LangFormat.Apply("{n, plural, one {один} few {несколько} many {много} other {другое}}", "ru", ru) == "несколько", "ru apply 2");
            ru["n"] = "5";
            Check(LangFormat.Apply("{n, plural, one {один} few {несколько} many {много} other {другое}}", "ru", ru) == "много", "ru apply 5");
            ru["n"] = "1.5";
            Check(LangFormat.Apply("{n, plural, one {один} few {несколько} many {много} other {другое}}", "ru", ru) == "другое", "ru apply 1.5");
        }

        static void EnglishMatchesLegacy(Dictionary<string, string> english)
        {
            CompareSample(english, Defaults(), "defaults");
            var hops0 = Defaults(); hops0["hops"] = "0"; CompareSample(english, hops0, "hops 0");
            var hops1 = Defaults(); hops1["hops"] = "1"; CompareSample(english, hops1, "hops 1");
            var stock = Defaults(); stock["spearStock"] = "4"; stock["stepStock"] = "1"; CompareSample(english, stock, "stocks");
            var bare = Defaults(); bare["armorOn"] = "no"; bare["death"] = "off"; bare["jolt"] = "off"; CompareSample(english, bare, "clauses off");
            var full = Defaults(); full["death"] = "full"; full["stun"] = "1"; full["decay"] = "1"; full["shocked"] = "1"; full["immune"] = "1"; full["targets"] = "1";
            CompareSample(english, full, "singular seconds");
            var oneSecondSkill = Defaults(); oneSecondSkill["circuitSeconds"] = "1"; oneSecondSkill["gazeSeconds"] = "1";
            CompareSample(english, oneSecondSkill, "skill seconds stay plural in English");
        }

        static void CompareSample(Dictionary<string, string> english, Dictionary<string, string> args, string label)
        {
            var legacy = Legacy(args);
            foreach (string token in Dynamic)
            {
                string formatted = LangFormat.Apply(english[token], "en", args);
                Check(formatted == legacy[token], label + " " + token + "\n--- formatted ---\n" + formatted + "\n--- legacy ---\n" + legacy[token]);
            }
        }

        static void EveryLanguageFormats(Dictionary<string, Dictionary<string, string>> doc)
        {
            var samples = new List<Dictionary<string, string>>();
            samples.Add(Defaults());
            string[] hops = { "0", "1", "2", "5", "11", "21" };
            string[] stocks = { "0", "1", "2", "5", "11", "21" };
            string[] seconds = { "0.5", "1", "2", "5", "11", "21" };
            foreach (string hop in hops) { var s = Defaults(); s["hops"] = hop; s["orbs"] = hop; s["targets"] = hop; samples.Add(s); }
            foreach (string stock in stocks) { var s = Defaults(); s["spearStock"] = stock; s["stepStock"] = stock; samples.Add(s); }
            foreach (string second in seconds)
            {
                var s = Defaults();
                s["stun"] = second; s["decay"] = second; s["shocked"] = second; s["immune"] = second;
                s["circuitSeconds"] = second; s["gazeSeconds"] = second;
                samples.Add(s);
            }
            var off = Defaults(); off["armorOn"] = "no"; off["death"] = "off"; off["jolt"] = "off"; samples.Add(off);
            var on = Defaults(); on["armorOn"] = "yes"; on["death"] = "full"; on["jolt"] = "on"; samples.Add(on);
            var partial = Defaults(); partial["death"] = "partial"; samples.Add(partial);

            foreach (var lang in doc)
            {
                string rules = lang.Key.Equals("strings", StringComparison.OrdinalIgnoreCase) ? "en" : lang.Key;
                foreach (var args in samples)
                    foreach (string token in Dynamic)
                        LangFormat.Apply(lang.Value[token], rules, args);
            }
        }

        static Dictionary<string, string> Defaults()
        {
            var a = new Dictionary<string, string>();
            a["boltDamage"] = "100%"; a["hops"] = "3";
            a["tap"] = "400%"; a["full"] = "1600%"; a["burst"] = "50%"; a["burstFull"] = "100%"; a["spearStock"] = "1";
            a["conductorSeconds"] = "3"; a["conductorTargets"] = "2"; a["conductorInterval"] = "0.75"; a["conductorTap"] = "20%"; a["conductorFull"] = "35%";
            a["stepStock"] = "2";
            a["circuitSeconds"] = "10"; a["radius"] = "8"; a["pulse"] = "60%"; a["interval"] = "0.5"; a["mult"] = "2.5";
            a["gazeSeconds"] = "4"; a["gazeMaxSeconds"] = "6"; a["pulseInterval"] = "0.25"; a["dps"] = "500%"; a["armorOn"] = "yes"; a["armor"] = "30";
            a["orbs"] = "5"; a["thunder"] = "1000%";
            a["decay"] = "2"; a["death"] = "partial"; a["deathPct"] = "50%"; a["jolt"] = "on"; a["stun"] = "0.5";
            a["bonus"] = "15%"; a["shocked"] = "3"; a["targets"] = "2"; a["pop"] = "150%"; a["immune"] = "4"; a["range"] = "30";
            return a;
        }

        static string Seconds(string num)
        {
            double v = double.Parse(num, CultureInfo.InvariantCulture);
            return num + (Math.Abs(v - 1.0) < 0.001 ? " second" : " seconds");
        }

        static string Enemies(string num)
        {
            int n = int.Parse(num, CultureInfo.InvariantCulture);
            return num + (n == 1 ? " enemy" : " enemies");
        }

        static string Dmg(string text) { return "<style=cIsDamage>" + text + "</style>"; }
        static string Util(string text) { return "<style=cIsUtility>" + text + "</style>"; }

        /// <summary>The sentences KitDescriptions used to build in code, before they moved into templates.</summary>
        static Dictionary<string, string> Legacy(Dictionary<string, string> a)
        {
            var t = new Dictionary<string, string>();
            int hops = int.Parse(a["hops"], CultureInfo.InvariantCulture);
            t["HS_SKILL_ARCBOLT_DESC"] =
                "Snap an arc bolt for " + Dmg(a["boltDamage"] + " damage") +
                (hops > 0 ? " that " + Dmg("chains") + " to up to " + Dmg(a["hops"]) + " more " + (hops == 1 ? "enemy" : "enemies") + ". " : ". ") +
                "Each hit builds " + Dmg("Static") + ".";
            int spearStock = int.Parse(a["spearStock"], CultureInfo.InvariantCulture);
            t["HS_SKILL_SPEAR_DESC"] =
                Util("Agile.") + " Charge a spear of lightning for " + Dmg(a["tap"] + "-" + a["full"] + " damage") +
                ". It " + Util("sticks") + ", then bursts for " + Dmg(a["burst"] + "-" + a["burstFull"] + " of its damage") + " around it." +
                (spearStock > 1 ? " Holds " + Util(a["spearStock"] + " charges") + "." : "") +
                " Enemies struck conduct lightning for " + a["conductorSeconds"] + "s, arcing to up to " + a["conductorTargets"] + " nearby enemies for " +
                Dmg(a["conductorTap"] + "-" + a["conductorFull"] + " damage") + " every " + a["conductorInterval"] + "s. Recharge starts when thrown. With a full Static Charge bank, a successful throw spends it for one Thunderbolt on impact. Partial banks are kept.";
            int stepStock = int.Parse(a["stepStock"], CultureInfo.InvariantCulture);
            t["HS_SKILL_ARCSTEP_DESC"] =
                Util("Blink") + " a short distance in any direction, even in the air. Jump out of it to keep the momentum. Holds " +
                Util(a["stepStock"] + (stepStock == 1 ? " charge" : " charges")) + ".";
            t["HS_SKILL_CIRCUIT_DESC"] =
                Util("Agile.") + " For " + Util(a["circuitSeconds"] + " seconds") + ", your crown strikes enemies within " +
                Util(a["radius"] + "m") + " for " + Dmg(a["pulse"] + " damage") + " every " +
                Util(a["interval"] + "s") + " and " + Dmg("Stormspear") + " charges " + Util(a["mult"] + "x faster") + ".";
            t["HS_SKILL_GAZE_DESC"] =
                "Rise and fire a beam for " + Util(a["gazeSeconds"] + "-" + a["gazeMaxSeconds"] + " seconds") + ", dealing " + Dmg(a["dps"] + " damage per second") +
                (a["armorOn"] == "yes" ? ", with " + Util(a["armor"] + " bonus armor") : "") + ". Its " + Dmg("forks") + " reach further over time." +
                " Once the beam fires, press Primary to spend one entry orb on a pulse (minimum " + a["pulseInterval"] + "s between presses). Holding does not repeat; release Primary first if already held. New orbs are saved for later. Other combat skills are unavailable until the channel ends. Duration grows with level. Each launched pulse adds 2 seconds, up to 14 seconds of beam time.";
            t["HS_PASSIVE_STORM_DESC"] =
                "Hits build " + Dmg("Static") + ". Full Static " + Dmg("Electrocutes") + " an enemy and stores a Static Charge (up to " + a["orbs"] + "). A full bank empowers your next successful Stormspear throw with one " +
                Dmg("Thunderbolt") + " for " + Dmg(a["thunder"] + " damage") + " on impact. Partial banks are kept. Gaze uses stored charges for manual pulses. Charges never discharge automatically.";

            string jolt = a["jolt"] == "on" ? " is jolted for " + Seconds(a["stun"]) + " (not bosses) and" : "";
            string death = "";
            if (a["death"] == "full") death = " An enemy that dies at full Static Electrocutes as it falls.";
            else if (a["death"] == "partial") death = " An enemy that dies with " + a["deathPct"] + " Static or more Electrocutes as it falls.";
            string staticLine = Dmg("Static") + ": every hit charges the enemy. Bigger hits and critical strikes charge it faster. It fades after " +
                Seconds(a["decay"]) + " without a hit." + death;
            string shockLine = Dmg("Electrocute") + ": at full Static the enemy" + jolt + " is " + Dmg("Shocked") + ", taking " +
                Dmg(a["bonus"] + " more damage") + " for " + Seconds(a["shocked"]) + ". The arc jumps to " +
                Enemies(a["targets"]) + " nearby for " + Dmg(a["pop"] + " damage") +
                " and charges them too. That enemy can't build Static again for " + Seconds(a["immune"]) + ".";
            string boltLine = Dmg("Static Charges") + ": each Electrocute stores one charge, up to " + a["orbs"] +
                ". A full bank is spent by the next successful Stormspear throw for one Thunderbolt on impact, dealing " +
                Dmg(a["thunder"] + " damage") + ". Partial banks stay stored; Gaze claims entry charges for manual pulses. No automatic discharge.";
            t["HS_KEYWORD_STORM"] = "<style=cKeywordName>The Storm</style><style=cSub>" + staticLine + "\n" + shockLine + "\n" + boltLine + "</style>";
            t["HS_KEYWORD_STATIC"] = "<style=cKeywordName>Static</style><style=cSub>" + staticLine.Substring(staticLine.IndexOf(':') + 2) + " At full Static the enemy is " + Dmg("Electrocuted") + ".</style>";
            t["HS_KEYWORD_ELECTROCUTE"] = "<style=cKeywordName>Electrocute</style><style=cSub>" + shockLine.Substring(shockLine.IndexOf(':') + 2) + "</style>";
            t["HS_KEYWORD_SHOCKED"] = "<style=cKeywordName>Shocked</style><style=cSub>The enemy takes " + Dmg(a["bonus"] + " more damage") + " from all sources for " + Seconds(a["shocked"]) + ".</style>";
            return t;
        }

        static string Join(List<string> items) { return string.Join(", ", items.ToArray()); }

        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    }
}
