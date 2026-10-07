using System;
using System.Linq;
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

        /// <summary>HS_SEGMENTS=audio-probe: every Play_HS_* event, alone, 1.1 s apart, on the idle
        /// Saint (loops stopped after 0.8 s). Clean in-game level per sound for analyze_capture.</summary>
        private System.Collections.IEnumerator AudioProbe()
        {
            yield return Segment("audio-probe");
            // Clean room: music off for this process only (Wwise RTPC, not the saved settings)
            // and no living monsters, so every cue is measured against near-silence.
            AkSoundEngine.SetRTPCValue("Volume_MSX", 0f);
            foreach (var tc in TeamComponent.GetTeamMembers(TeamIndex.Monster).ToArray())
            {
                var hc = tc ? tc.GetComponent<HealthComponent>() : null;
                if (hc) { hc.godMode = false; hc.Suicide(); }
            }
            yield return Wait(4f);
            AudioMark("SYNC");
            Util.PlaySound("Play_HS_SpearImpact", pilot.gameObject);
            yield return Wait(1.5f);
            string[] names =
            {
                "GazeLoad1", "GazeLoad2", "GazeLoad3", "GazeSurgeHit1", "GazeSurgeHit2", "GazeSurgeHit3",
                "GazeSurge1", "GazeSurge3", "GazeSurge5", "ArcBoltCast", "BoltImpact", "ChainHop", "Electrocute",
                "StaticTier", "ChargeTick", "MeterFull", "SpearChargeStart", "SpearThrow", "SpearThrowHeavy",
                "SpearImpact", "SpearBurst", "SpearStruck", "SpearPulse", "SpearCatch", "ArcStepStart", "ArcStepEnd",
                "AirJump", "Land", "CircuitUnfold", "CircuitOpen", "CircuitPulse", "CircuitClose",
                "ThunderTelegraph", "ThunderRelease", "ThunderStrike",
                "CircuitLoop", "SpearChargeLoop", "GlideLoop", "FanLoop",
            };
            // Gaze gather duck, clean room: beam alone 2 s, hold (3 charges) 3 s, release, beam 1.5 s.
            if (Environment.GetEnvironmentVariable("HS_PROBE_GAZE") == "1" && FoundationKit.Gaze.GazeRegistration.SkillDef)
            {
                var slot = pilot.skillLocator.special;
                slot.SetSkillOverride(this, FoundationKit.Gaze.GazeRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
                pilot.SetBuffCount(FoundationKit.DischargeMeter.ChargeBuff.buffIndex, 5);
                pilot.skillLocator.ResetSkills(); aimTarget = pilot.footPosition + facing * 8f;
                yield return Wait(0.3f);
                yield return Press(4); yield return Wait(1.3f);
                var crown = EntityStateMachine.FindByCustomName(pilot.gameObject, KitRegistration.CrownMachineName);
                trace.AppendLine("PROBE_GAZE_STATE " + (crown ? crown.state.GetType().Name : "none"));
                AudioMark("GAZE_BEAM"); yield return Wait(2.0f);
                AudioMark("GAZE_HOLD"); fire1 = true; yield return Wait(3.0f);
                AudioMark("GAZE_RELEASE"); fire1 = false; yield return Wait(2.5f);
                AudioMark("GAZE_DONE");
                yield return Press(4); yield return Wait(1.5f);
                slot.UnsetSkillOverride(this, FoundationKit.Gaze.GazeRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
            }
            // Thunderbolt impact through its real path (EffectManager prefab sound), then the same
            // event posted on a fresh emitter at the pilot, to separate path from media problems.
            for (int k = 0; k < 3; k++)
            {
                AudioMark("PROBE ThunderImpactFx");
                FoundationKit.Storm.RoyalCapacitorFx.Strike(pilot.footPosition + facing * 4f, pilot);
                yield return Wait(2.6f);
            }
            for (int k = 0; k < 2; k++)
            {
                AudioMark("PROBE ThunderStrikeFresh");
                var emitter = new GameObject("HS_ProbeEmitter");
                emitter.transform.position = pilot.corePosition + facing * 2f;
                Util.PlaySound("Play_HS_ThunderStrike", emitter);
                yield return Wait(2.6f);
                Destroy(emitter, 0.1f);
            }
            // Repeat a few key cues to tell dropped voices from quiet mixes.
            string repeats = Environment.GetEnvironmentVariable("HS_PROBE_REPEAT");
            if (!string.IsNullOrEmpty(repeats))
            {
                var list = new System.Collections.Generic.List<string>();
                foreach (var n in new[] { "GazeLoad1", "GazeSurgeHit3", "MeterFull", "ArcStepStart", "ThunderStrike", "BoltImpact" })
                    for (int k = 0; k < 4; k++) list.Add(n);
                names = list.ToArray();
            }
            foreach (var n in names)
            {
                AudioMark("PROBE " + n);
                Util.PlaySound("Play_HS_" + n, pilot.gameObject);
                yield return Wait(n.EndsWith("Loop") ? 0.8f : 1.1f);
                if (n.EndsWith("Loop")) { Util.PlaySound("Stop_HS_" + n, pilot.gameObject); yield return Wait(0.6f); }
            }
        }

        private void WriteAudioLog()
        {
            try { System.IO.File.WriteAllText(System.IO.Path.Combine(output, "audio-events.txt"), audioLog.ToString()); }
            catch (Exception e) { Plugin.Log.LogError(e); }
        }
    }
}
