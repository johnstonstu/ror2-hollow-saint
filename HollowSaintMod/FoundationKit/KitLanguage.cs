using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using R2API;
using HollowSaint.FoundationKit.Gaze;

namespace HollowSaint.FoundationKit
{
    /// <summary>
    /// Loads HollowSaint.language (next to the DLL, or the copy embedded in it) and registers the
    /// static tokens. Description tokens stay as templates in the file; KitDescriptions fills them
    /// and pushes the result through an overlay so a config change can replace the numbers without
    /// touching the other languages.
    ///
    /// R2API loads every *.language file under BepInEx/plugins the first time LanguageAPI.Add runs,
    /// and Add refuses to replace a token that is already there. Shipping the file therefore wins
    /// over the English fallback below. LanguageAPI.Add(key, value) with no language writes the
    /// generic "strings" fallback, which would cover a translation, so formatted descriptions must
    /// not go through that overload.
    /// </summary>
    internal static class KitLanguage
    {
        internal const string FileName = "HollowSaint.language";
        internal const string EmbeddedName = "HollowSaint.Language.HollowSaint.language";

        internal static readonly string[] DynamicTokens =
        {
            KitTokens.ArcBoltDesc,
            KitTokens.ConduitSpearDesc,
            KitTokens.ArcStepDesc,
            KitTokens.OpenCircuitDesc,
            GazeRegistration.DescToken,
            KitTokens.StormDesc,
            KitTokens.KeywordStorm,
            KitTokens.KeywordStatic,
            KitTokens.KeywordElectrocute,
            KitTokens.KeywordShocked,
        };

        internal static Dictionary<string, Dictionary<string, string>> Document;
        private static LanguageAPI.LanguageOverlay overlay;
        private static bool staticInstalled;

        internal static bool IsDynamic(string token)
        {
            for (int i = 0; i < DynamicTokens.Length; i++)
                if (DynamicTokens[i] == token) return true;
            return false;
        }

        internal static void Install()
        {
            if (Document == null) Load();
            if (staticInstalled) return;
            staticInstalled = true;
            Dictionary<string, string> english;
            if (!Document.TryGetValue("strings", out english))
                throw new InvalidOperationException("HollowSaint.language has no strings section.");
            foreach (var pair in english)
            {
                if (IsDynamic(pair.Key)) continue;
                // No-op when the file beside the DLL already supplied this token, including translations.
                LanguageAPI.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>Replaces the previous formatted-description overlay. Specific languages win over generic.</summary>
        internal static void PushOverlay(Dictionary<string, Dictionary<string, string>> formatted)
        {
            if (overlay != null) overlay.Remove();
            overlay = LanguageAPI.AddOverlay(formatted);
        }

        private static void Load()
        {
            string path = null;
            if (!string.IsNullOrEmpty(Plugin.DirectoryPath))
                path = Path.Combine(Plugin.DirectoryPath, FileName);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                Document = LanguageJson.Parse(File.ReadAllText(path));
                return;
            }
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedName))
            {
                if (stream == null) throw new InvalidOperationException("Embedded " + FileName + " is missing from the DLL.");
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                    Document = LanguageJson.Parse(reader.ReadToEnd());
            }
            if (Plugin.Log != null)
                Plugin.Log.LogWarning("HOLLOW_SAINT_LANGUAGE_FILE_MISSING " + FileName +
                    " was not next to the DLL, so only the built-in English text is available.");
        }
    }
}
