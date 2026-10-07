using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HollowSaint.FoundationKit
{
    /// <summary>
    /// Parser for an R2API .language file: a JSON object whose values are objects of strings.
    /// Kept free of game references so tools/tests/Check-Language.ps1 can compile it with Add-Type
    /// (C# 5, which Windows PowerShell 5.1 still uses).
    /// </summary>
    internal static class LanguageJson
    {
        internal static Dictionary<string, Dictionary<string, string>> Parse(string text)
        {
            if (text == null) throw new FormatException("Language file is empty.");
            int i = 0;
            if (text.Length > 0 && text[0] == '\uFEFF') i = 1;
            Skip(text, ref i);
            Dictionary<string, Dictionary<string, string>> root = ParseLanguages(text, ref i);
            Skip(text, ref i);
            if (i != text.Length) throw new FormatException("Unexpected trailing data in language file at " + i + ".");
            return root;
        }

        private static Dictionary<string, Dictionary<string, string>> ParseLanguages(string text, ref int i)
        {
            Expect(text, ref i, '{');
            var root = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Skip(text, ref i);
            if (Peek(text, i) == '}') { i++; return root; }
            while (true)
            {
                string language = ParseString(text, ref i);
                Skip(text, ref i);
                Expect(text, ref i, ':');
                Skip(text, ref i);
                if (root.ContainsKey(language)) throw new FormatException("Duplicate language '" + language + "'.");
                root.Add(language, ParseTokens(text, ref i));
                Skip(text, ref i);
                char c = Peek(text, i);
                if (c == ',') { i++; Skip(text, ref i); continue; }
                if (c == '}') { i++; break; }
                throw new FormatException("Expected ',' or '}' in language object at " + i + ".");
            }
            return root;
        }

        private static Dictionary<string, string> ParseTokens(string text, ref int i)
        {
            if (Peek(text, i) != '{') throw new FormatException("Language section must be an object at " + i + ".");
            i++;
            var tokens = new Dictionary<string, string>(StringComparer.Ordinal);
            Skip(text, ref i);
            if (Peek(text, i) == '}') { i++; return tokens; }
            while (true)
            {
                string key = ParseString(text, ref i);
                Skip(text, ref i);
                Expect(text, ref i, ':');
                Skip(text, ref i);
                if (Peek(text, i) != '"') throw new FormatException("Token '" + key + "' must be a string.");
                string value = ParseString(text, ref i);
                if (tokens.ContainsKey(key)) throw new FormatException("Duplicate token '" + key + "'.");
                tokens.Add(key, value);
                Skip(text, ref i);
                char c = Peek(text, i);
                if (c == ',') { i++; Skip(text, ref i); continue; }
                if (c == '}') { i++; break; }
                throw new FormatException("Expected ',' or '}' in token object at " + i + ".");
            }
            return tokens;
        }

        private static string ParseString(string text, ref int i)
        {
            Expect(text, ref i, '"');
            var sb = new StringBuilder();
            while (i < text.Length)
            {
                char c = text[i++];
                if (c == '"') return sb.ToString();
                if (c == '\\')
                {
                    if (i >= text.Length) throw new FormatException("Truncated escape.");
                    char e = text[i++];
                    if (e == '"' || e == '\\' || e == '/') sb.Append(e);
                    else if (e == 'b') sb.Append('\b');
                    else if (e == 'f') sb.Append('\f');
                    else if (e == 'n') sb.Append('\n');
                    else if (e == 'r') sb.Append('\r');
                    else if (e == 't') sb.Append('\t');
                    else if (e == 'u')
                    {
                        if (i + 4 > text.Length) throw new FormatException("Truncated \\u escape.");
                        int code;
                        if (!int.TryParse(text.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                            throw new FormatException("Bad \\u escape.");
                        sb.Append((char)code);
                        i += 4;
                    }
                    else throw new FormatException("Unknown escape '\\" + e + "'.");
                }
                else if (c < ' ') throw new FormatException("Raw control character in string.");
                else sb.Append(c);
            }
            throw new FormatException("Unterminated string.");
        }

        private static void Skip(string text, ref int i)
        {
            while (i < text.Length && (text[i] == ' ' || text[i] == '\t' || text[i] == '\r' || text[i] == '\n')) i++;
        }

        private static char Peek(string text, int i)
        {
            if (i >= text.Length) throw new FormatException("Unexpected end of language file.");
            return text[i];
        }

        private static void Expect(string text, ref int i, char expected)
        {
            if (Peek(text, i) != expected) throw new FormatException("Expected '" + expected + "' at " + i + ".");
            i++;
        }
    }
}
