using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HollowSaint.FoundationKit
{
    /// <summary>
    /// Fills a language template with live numbers.
    ///
    /// {name} inserts that argument.
    /// {name, plural, =0 {none} one {one} few {few} many {many} other {rest}}
    /// picks a branch from the number. An exact =N match wins, then the language's plural
    /// category, then other. Russian uses one/few/many; Chinese is always other; English and
    /// Brazilian Portuguese use one only for 1 (0 is other, so "0 charges" stays plural).
    /// {name, select, off {} on {text} other {text}} picks a branch from the argument text.
    ///
    /// C# 5 on purpose: tools/tests/Check-Language.ps1 compiles this file with Add-Type.
    /// </summary>
    internal static class LangFormat
    {
        internal static string Category(string language, double value)
        {
            string lang = language == null ? "" : language.ToLowerInvariant();
            bool whole = Math.Abs(value - Math.Round(value)) < 0.001;
            long n = (long)Math.Round(value);
            if (lang == "ru" || lang.StartsWith("ru-"))
            {
                if (!whole) return "other";
                long a10 = AbsMod(n, 10);
                long a100 = AbsMod(n, 100);
                if (a10 == 1 && a100 != 11) return "one";
                if (a10 >= 2 && a10 <= 4 && (a100 < 12 || a100 > 14)) return "few";
                return "many";
            }
            if (lang == "zh-cn" || lang.StartsWith("zh")) return "other";
            if (whole && n == 1) return "one";
            return "other";
        }

        internal static string Apply(string template, string language, IDictionary<string, string> args)
        {
            int i = 0;
            string text = ParseText(template, ref i, language, args, false, null, null, null, null);
            if (i != template.Length) throw new FormatException("Unbalanced '}' in template.");
            return text;
        }

        /// <summary>Placeholder names, opening style/color tags, and the category keys of each plural or select block.</summary>
        internal static void Inspect(string template, List<string> names, List<string> tags, List<string> pluralKeys, List<string> selectKeys)
        {
            int i = 0;
            ParseText(template, ref i, "en", null, false, names, tags, pluralKeys, selectKeys);
            if (i != template.Length) throw new FormatException("Unbalanced '}' in template.");
        }

        private static string ParseText(string template, ref int i, string language, IDictionary<string, string> args, bool stop,
            List<string> names, List<string> tags, List<string> pluralKeys, List<string> selectKeys)
        {
            var sb = new StringBuilder();
            while (i < template.Length)
            {
                char c = template[i];
                if (c == '}' && stop) return sb.ToString();
                if (c == '{')
                {
                    i++;
                    sb.Append(ParsePlaceholder(template, ref i, language, args, names, tags, pluralKeys, selectKeys));
                    continue;
                }
                if (c == '<')
                {
                    int start = i;
                    int close = template.IndexOf('>', i);
                    if (close < 0) throw new FormatException("Unclosed '<' in template.");
                    string tag = template.Substring(start, close - start + 1);
                    if (tags != null && (tag.StartsWith("<style=", StringComparison.Ordinal) || tag.StartsWith("<color=", StringComparison.Ordinal)))
                        tags.Add(tag);
                    sb.Append(tag);
                    i = close + 1;
                    continue;
                }
                sb.Append(c);
                i++;
            }
            if (stop) throw new FormatException("Unclosed '{' in template.");
            return sb.ToString();
        }

        private static string ParsePlaceholder(string template, ref int i, string language, IDictionary<string, string> args,
            List<string> names, List<string> tags, List<string> pluralKeys, List<string> selectKeys)
        {
            Skip(template, ref i);
            string name = ReadIdent(template, ref i);
            if (names != null) names.Add(name);
            Skip(template, ref i);
            if (i >= template.Length) throw new FormatException("Unclosed placeholder '{" + name + "}'.");
            if (template[i] == '}')
            {
                i++;
                return Arg(args, name);
            }
            if (template[i] != ',') throw new FormatException("Expected '}' or ',' after '{" + name + "'.");
            i++;
            Skip(template, ref i);
            string kind = ReadIdent(template, ref i);
            Skip(template, ref i);
            if (i >= template.Length || template[i] != ',') throw new FormatException("Expected ',' after " + kind + ".");
            i++;
            var keys = new List<string>();
            var bodies = new List<string>();
            while (true)
            {
                Skip(template, ref i);
                if (i < template.Length && template[i] == '}')
                {
                    i++;
                    break;
                }
                string key;
                if (i < template.Length && template[i] == '=')
                {
                    i++;
                    key = "=" + ReadNumber(template, ref i);
                }
                else key = ReadIdent(template, ref i);
                keys.Add(key);
                Skip(template, ref i);
                if (i >= template.Length || template[i] != '{') throw new FormatException("Expected '{' after branch '" + key + "'.");
                i++;
                bodies.Add(ParseText(template, ref i, language, args, true, names, tags, pluralKeys, selectKeys));
                if (i >= template.Length || template[i] != '}') throw new FormatException("Unclosed branch '" + key + "'.");
                i++;
            }
            if (keys.Count == 0) throw new FormatException("{" + name + ", " + kind + "} has no branches.");
            string joined = string.Join(",", keys.ToArray());
            if (kind == "plural")
            {
                if (pluralKeys != null) pluralKeys.Add(name + "=" + joined);
                if (args == null) return "";
                return bodies[PickPlural(keys, Arg(args, name), language)];
            }
            if (kind == "select")
            {
                if (selectKeys != null) selectKeys.Add(name + "=" + joined);
                if (args == null) return "";
                return bodies[PickSelect(keys, Arg(args, name))];
            }
            throw new FormatException("Unknown placeholder kind '" + kind + "'.");
        }

        private static int PickPlural(List<string> keys, string raw, string language)
        {
            double value;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                throw new FormatException("Plural value '" + raw + "' is not a number.");
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i].Length > 0 && keys[i][0] == '=')
                {
                    double exact;
                    if (double.TryParse(keys[i].Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out exact)
                        && Math.Abs(exact - value) < 0.001)
                        return i;
                }
            }
            string category = Category(language, value);
            int other = -1;
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i] == category) return i;
                if (keys[i] == "other") other = i;
            }
            if (other >= 0) return other;
            throw new FormatException("No plural branch for " + category + " (" + raw + ").");
        }

        private static int PickSelect(List<string> keys, string raw)
        {
            int other = -1;
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i] == raw) return i;
                if (keys[i] == "other") other = i;
            }
            if (other >= 0) return other;
            throw new FormatException("No select branch for '" + raw + "'.");
        }

        private static string Arg(IDictionary<string, string> args, string name)
        {
            if (args == null) return "";
            string value;
            if (!args.TryGetValue(name, out value)) throw new FormatException("Missing argument '{" + name + "}'.");
            return value;
        }

        private static void Skip(string template, ref int i)
        {
            while (i < template.Length && (template[i] == ' ' || template[i] == '\t' || template[i] == '\n' || template[i] == '\r')) i++;
        }

        private static string ReadIdent(string template, ref int i)
        {
            int start = i;
            while (i < template.Length && (char.IsLetterOrDigit(template[i]) || template[i] == '_')) i++;
            if (i == start) throw new FormatException("Expected a name at " + i + ".");
            return template.Substring(start, i - start);
        }

        private static string ReadNumber(string template, ref int i)
        {
            int start = i;
            while (i < template.Length && (char.IsDigit(template[i]) || template[i] == '.')) i++;
            if (i == start) throw new FormatException("Expected a number after '='.");
            return template.Substring(start, i - start);
        }

        private static long AbsMod(long n, long m)
        {
            long r = n % m;
            if (r < 0) r += m;
            return r;
        }
    }
}
