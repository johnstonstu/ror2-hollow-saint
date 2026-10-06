using System;
using System.Text;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>Dev-only audio timeline: every Util.PlaySound with a wall-clock stamp (UTC ms),
    /// event, emitter and camera distance, plus segment markers. Paired with a WASAPI loopback
    /// recording (tools/audio/Run-AudioCapture.ps1) to measure cues in the real in-game mix.</summary>
    internal sealed partial class DevAutopilot
    {
        private readonly StringBuilder audioLog = new StringBuilder();
        private bool audioHooked;

        private static long WallMs => (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

        private void HookAudio()
        {
            if (audioHooked) return;
            audioHooked = true;
            On.RoR2.Util.PlaySound_string_GameObject += LogSound;
            audioLog.AppendLine("START " + WallMs);
        }

        private void UnhookAudio()
        {
            if (!audioHooked) return;
            audioHooked = false;
            On.RoR2.Util.PlaySound_string_GameObject -= LogSound;
        }

        private void AudioMark(string label) { if (audioHooked) audioLog.AppendLine("MARK " + WallMs + " " + label); }

        private uint LogSound(On.RoR2.Util.orig_PlaySound_string_GameObject orig, string sound, GameObject source)
        {
            uint id = orig(sound, source);
            try
            {
                if (!string.IsNullOrEmpty(sound))
                {
                    var cam = Camera.main;
                    float distance = cam && source ? Vector3.Distance(cam.transform.position, source.transform.position) : -1f;
                    audioLog.Append("SND ").Append(WallMs).Append(' ').Append(sound).Append(' ')
                        .Append(source ? source.name.Replace(' ', '_') : "-").Append(' ').Append(distance.ToString("0.0"))
                        .Append(' ').Append(id == 0 ? "FAILED" : "ok").Append('\n');
                }
            }
            catch { }
            return id;
        }

        private void WriteAudioLog()
        {
            try { System.IO.File.WriteAllText(System.IO.Path.Combine(output, "audio-events.txt"), audioLog.ToString()); }
            catch (Exception e) { Plugin.Log.LogError(e); }
        }
    }
}
