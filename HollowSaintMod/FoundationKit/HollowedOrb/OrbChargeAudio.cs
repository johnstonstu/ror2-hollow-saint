using System;
using System.Collections.Generic;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.HollowedOrb
{
    /// <summary>One gather owns one delayed loop and every intake voice until its terminal edge.</summary>
    internal sealed class OrbChargeAudio : IDisposable
    {
        private readonly GameObject source;
        private readonly List<uint> playing = new List<uint>();
        private bool begun, ended, loopAttempted, warned;
        private float loopAt;
        private Stage stage;

        internal OrbChargeAudio(GameObject source) { this.source = source; }
        private bool Audible => source && !(NetworkServer.active && !NetworkClient.active);

        internal void Begin(float now)
        {
            if (begun || ended) return;
            begun = true; stage = Stage.instance; loopAt = now + .20f;
            PlayCue(CustomSoundBank.Ready ? "Play_HS_OrbChargeStart" : "Play_mage_m1_cast_lightning");
        }

        internal void Tick(float now, bool ownerAlive)
        {
            if (ended || !begun) return;
            if (!ownerAlive || !Audible || Stage.instance != stage) { End(false); return; }
            if (loopAttempted || now < loopAt) return;
            loopAttempted = true;
            // A missing custom bank gets finite cues only, never an unowned vanilla loop.
            if (CustomSoundBank.Ready) PlayCue("Play_HS_OrbChargeLoop");
        }

        internal void PlayCue(string name)
        {
            if (ended || !Audible || string.IsNullOrEmpty(name)) return;
            try
            {
                uint id = Util.PlaySound(name, source);
                if (id != 0) playing.Add(id);
            }
            catch (Exception error) { Warn("post " + name, error); }
        }

        internal void End(bool fired)
        {
            if (ended) return;
            ended = true;
            foreach (uint id in playing)
            {
                try { AkSoundEngine.StopPlayingID(id, fired ? 70 : 180); }
                catch (Exception error) { Warn("stop playing ID " + id, error); }
            }
            playing.Clear();
        }

        private void Warn(string operation, Exception error)
        {
            if (warned) return;
            warned = true;
            Plugin.Log.LogWarning("HOLLOW_SAINT_ORB_CHARGE_AUDIO " + operation + ": " + error.Message);
        }

        public void Dispose() { End(false); }
    }

    internal static class OrbThrowAudio
    {
        internal static void Play(bool firstSegment, Vector3 origin)
        {
            if (!firstSegment || (NetworkServer.active && !NetworkClient.active)) return;
            // The flight object can vanish on a point-blank hit. A stationary, finite
            // anchor keeps the discharge tail audible through impact and later bounces.
            var anchor = new GameObject("HS_OrbThrowAudio");
            anchor.transform.position = origin;
            UnityEngine.Object.Destroy(anchor, 2f);
            try
            {
                Util.PlaySound(CustomSoundBank.Ready ? "Play_HS_OrbThrow" : "Play_captain_m2_tazer_shoot", anchor);
            }
            catch (Exception error)
            {
                Plugin.Log.LogWarning("HOLLOW_SAINT_ORB_THROW_AUDIO post: " + error.Message);
                UnityEngine.Object.Destroy(anchor);
            }
        }
    }
}
