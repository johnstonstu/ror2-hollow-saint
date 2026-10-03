using System;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>One travelling beat for core, ring, limb, weapon and channel. Presentation only.</summary>
    internal static class LightningRhythm
    {
        internal const float Period = 0.48f;
        internal const float Speed = 8f;
        internal static float Pulse(float seconds, float distance = 0f)
        {
            if (float.IsNaN(seconds + distance) || float.IsInfinity(seconds + distance)) return 0f;
            double phase = (seconds - distance / Speed) / Period;
            double wave = 0.5 + 0.5 * Math.Cos(phase * Math.PI * 2);
            return (float)(wave * wave * wave);
        }
        internal static float Gain(float seconds, float distance = 0f) => 0.78f + 0.42f * Pulse(seconds, distance);
    }
}
