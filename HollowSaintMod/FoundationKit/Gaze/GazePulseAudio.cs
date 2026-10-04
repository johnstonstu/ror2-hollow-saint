using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>Only the added pulse cue; original Gaze startup/loops/end remain owned by its Fx.</summary>
    internal sealed class GazePulseAudio
    {
        private float lastPlayed = float.NegativeInfinity;
        internal bool Play(GameObject source)
        {
            float now = Time.unscaledTime;
            // The embedded discharge is 0.24 s. Coalesce clustered delayed acknowledgements
            // instead of stacking several voices over the still-playing surge.
            if (!source || float.IsNaN(now) || float.IsInfinity(now) ||
                (now >= lastPlayed && now - lastPlayed < .24f)) return false;
            lastPlayed = now;
            Util.PlaySound(CustomSoundBank.Ready ? "Play_HS_ThunderRelease" : "Play_captain_m2_tazer_shoot", source);
            return true;
        }
    }
}
