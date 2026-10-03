using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using RoR2;
using UnityEngine;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;

namespace HollowSaint
{
    /// <summary>
    /// v0.9.11 (HS_SEGMENTS=lobby): stays on character select with Hollow Saint picked, screenshots the
    /// real screen (description, skill list, the display model and its lighting) on every skin, and
    /// writes every HS_ language string as the game resolves it to lobby-text.txt.
    /// </summary>
    internal sealed partial class DevAutopilot
    {
        private IEnumerator LobbySegments()
        {
            yield return Real(4f);
            DumpTokens();
            ModelSkinController skins = null;
            float wait = 0f;
            while (!skins && wait < 10f)
            {
                skins = FindObjectsOfType<ModelSkinController>().FirstOrDefault(s => s && s.gameObject.name.StartsWith("HollowSaint", StringComparison.Ordinal));
                wait += 0.5f;
                yield return Real(0.5f);
            }
            if (!skins) { trace.AppendLine("LOBBY no display model"); yield break; }
            for (int i = 0; i < Mathf.Min(5, skins.skins.Length); i++)
            {
                skins.ApplySkin(i);
                yield return Real(2.5f); // let the select flourish and idle settle
                ScreenCapture.CaptureScreenshot(IOPath.Combine(output, "lobby-skin" + i + ".png"));
                trace.AppendLine("LOBBY shot skin" + i + " " + skins.skins[i].name);
                yield return Real(1f);
            }
        }

        private void DumpTokens()
        {
            var tokens = new System.Collections.Generic.SortedSet<string>();
            foreach (var type in typeof(DevAutopilot).Assembly.GetTypes())
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (field.FieldType != typeof(string)) continue;
                    string value = null;
                    try { value = field.GetValue(null) as string; } catch { }
                    if (value != null && value.StartsWith("HS_", StringComparison.Ordinal) && !value.Contains(" ")) tokens.Add(value);
                }
            foreach (var extra in new[] { "HS_NAME", "HS_DESCRIPTION", "HS_SUBTITLE", "HS_OUTRO", "HS_LORE", "HS_FAILURE" }) tokens.Add(extra);
            var sb = new StringBuilder();
            foreach (var t in tokens)
            {
                string text = Language.GetString(t);
                if (text == t) continue;
                sb.AppendLine("[" + t + "]").AppendLine(text).AppendLine();
            }
            IOFile.WriteAllText(IOPath.Combine(output, "lobby-text.txt"), sb.ToString());
        }
    }
}
