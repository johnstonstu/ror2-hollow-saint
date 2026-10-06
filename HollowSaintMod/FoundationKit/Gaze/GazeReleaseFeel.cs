using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>1.2 hold/release feel: one ascending chime per loaded charge, and the
    /// surge's weight delivered when it actually connects (sound + camera kick scaled
    /// by tier), once per surge rather than once per victim. Client-side only.</summary>
    internal sealed class GazeReleaseFeel
    {
        private int lastLoaded;
        private GameObject emitter;

        /// <summary>1.2 audio pass: the body's sound object carries Gaze's whole loop stack, and
        /// in-game captures showed short cues posted there being dropped intermittently. A
        /// dedicated child emitter keeps the feedback cues on their own Wwise game object.</summary>
        private GameObject Emitter(GameObject body)
        {
            if (emitter) return emitter;
            emitter = new GameObject("HS_GazeFeelEmitter");
            emitter.transform.SetParent(body.transform, false);
            return emitter;
        }
        private uint hitCast;
        private int hitPhase = -1;

        internal void Begin() { lastLoaded = 0; hitPhase = -1; }

        // 1.2 mix: the beam's borrowed vanilla loops sit ~10 dB over everything while Gaze runs,
        // so loading ducks the body's own voices (the loops live there; our cues use the child
        // emitter) and releasing restores them: a gather, then the surge lands over full beam.
        private const float GatherDuck = 0.55f; // about -5 dB
        private GameObject ducked;
        private static bool warned;
        private void Duck(GameObject body, bool on)
        {
            try
            {
                if (on && body && ducked != body)
                {
                    // Per-connection volume: apply to every listener (split screen, extra cameras).
                    foreach (var listener in Object.FindObjectsOfType<AkAudioListener>())
                        if (listener) AkSoundEngine.SetGameObjectOutputBusVolume(body, listener.gameObject, GatherDuck);
                    ducked = body;
                }
                else if (!on && ducked)
                {
                    foreach (var listener in Object.FindObjectsOfType<AkAudioListener>())
                        if (listener) AkSoundEngine.SetGameObjectOutputBusVolume(ducked, listener.gameObject, 1f);
                    ducked = null;
                }
            }
            catch (System.Exception error)
            {
                ducked = null;
                if (!warned) { warned = true; Plugin.Log.LogWarning("HOLLOW_SAINT_GAZE_DUCK " + error.Message); }
            }
        }
        internal void End() => Duck(null, false);

        internal void Loaded(GameObject source, int count)
        {
            int previous = lastLoaded;
            lastLoaded = count;
            Duck(source, count > 0);
            if (!source || count <= previous || count < 1) return;
            int tier = Mathf.Clamp(count, 1, 3);
            Util.PlaySound(CustomSoundBank.Ready ? "Play_HS_GazeLoad" + tier : "Play_HS_ChargeTick", Emitter(source));
        }

        internal bool Hit(CharacterBody body, uint cast, int phase, int group)
        {
            if (!body || (cast == hitCast && phase == hitPhase)) return false;
            hitCast = cast; hitPhase = phase;
            KitLog.Event("GAZE_SURGE_HIT", "tier=" + group + " phase=" + phase);
            int tier = Mathf.Clamp(group, 1, 3);
            Util.PlaySound(CustomSoundBank.Ready ? "Play_HS_GazeSurgeHit" + tier : "Play_HS_SpearBurst", Emitter(body.gameObject));
            Kick(body, tier);
            return true;
        }

        private static void Kick(CharacterBody body, int tier)
        {
            if (!ImpactFeelSettings.Enabled || LocalUserManager.readOnlyLocalUsersList.Count != 1) return;
            var local = LocalUserManager.readOnlyLocalUsersList[0];
            var camera = local.cameraRigController;
            if (local.cachedBody != body || !camera || camera.targetBody != body) return;
            float amplitude = tier == 1 ? .30f : tier == 2 ? .55f : .90f;
            float duration = tier == 1 ? .16f : tier == 2 ? .22f : .32f;
            ShakeEmitter.CreateSimpleShakeEmitter(camera.transform.position,
                new Wave { amplitude = amplitude, frequency = 18f, cycleOffset = 0f }, duration, 1f, true);
        }
    }
}
