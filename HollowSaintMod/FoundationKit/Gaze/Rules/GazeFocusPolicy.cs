using System;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>
    /// 1.3.1 (Stu playtest): holding the core beam on one enemy ramps its damage. Focus (0..1) builds
    /// with time on target, then bleeds off after a short grace when the beam slips away, so flicking
    /// between targets or a brief miss keeps most of the ramp. Pure math; the server owns the state.
    /// </summary>
    internal static class GazeFocusPolicy
    {
        public static float MaxBonus = 1f;        // +100% core damage at full focus
        public static float RampSeconds = 3f;     // time on target from 0 to full
        public static float GraceSeconds = .5f;   // off target this long before focus starts to fade
        public static float DecaySeconds = 1f;    // full focus fades to zero over this, after the grace
        internal const float MaxStep = .4f;       // one tick never credits more than this much time
        internal const int Tiers = 5; // 1.3.1: five audible steps (rising chime per step)

        /// <summary>Focus after a core hit at <paramref name="now"/>; lastHit &lt; 0 means a fresh target.</summary>
        internal static float Next(float focus, float lastHit, float now)
        {
            if (lastHit < 0f || float.IsNaN(focus)) return 0f;
            float elapsed = Math.Max(0f, now - lastHit);
            if (elapsed > GraceSeconds)
                focus -= (elapsed - GraceSeconds) / Math.Max(.05f, DecaySeconds);
            focus = Math.Max(0f, focus);
            focus += Math.Min(elapsed, MaxStep) / Math.Max(.1f, RampSeconds);
            return Math.Min(1f, focus);
        }
        internal static float Multiplier(float focus) => 1f + Math.Max(0f, MaxBonus) * Math.Max(0f, Math.Min(1f, focus));
        internal static int Tier(float focus) => (int)Math.Floor(Math.Max(0f, Math.Min(1f, focus)) * Tiers + 1e-4);
    }
}
