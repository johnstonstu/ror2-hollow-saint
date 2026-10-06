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
        private uint hitCast;
        private int hitPhase = -1;

        internal void Begin() { lastLoaded = 0; hitPhase = -1; }

        internal void Loaded(GameObject source, int count)
        {
            int previous = lastLoaded;
            lastLoaded = count;
            if (!source || count <= previous || count < 1) return;
            int tier = Mathf.Clamp(count, 1, 3);
            Util.PlaySound(CustomSoundBank.Ready ? "Play_HS_GazeLoad" + tier : "Play_HS_ChargeTick", source);
        }

        internal bool Hit(CharacterBody body, uint cast, int phase, int group)
        {
            if (!body || (cast == hitCast && phase == hitPhase)) return false;
            hitCast = cast; hitPhase = phase;
            KitLog.Event("GAZE_SURGE_HIT", "tier=" + group + " phase=" + phase);
            int tier = Mathf.Clamp(group, 1, 3);
            Util.PlaySound(CustomSoundBank.Ready ? "Play_HS_GazeSurgeHit" + tier : "Play_HS_SpearBurst", body.gameObject);
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
