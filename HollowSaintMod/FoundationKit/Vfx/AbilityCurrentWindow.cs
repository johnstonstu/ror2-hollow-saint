using System;

namespace HollowSaint.FoundationKit.Vfx
{
    // Pure timing: late release, fast attacks and stale interruption are tested offline.
    internal sealed class AbilityCurrentWindow
    {
        internal uint Generation { get; private set; }
        private float started, releaseAt, ends, cancelAt, cancelWeight, cancelTravel;
        private bool cancelled;

        internal uint Begin(float now, float duration, float releaseFraction)
        {
            duration = Finite(duration) ? Clamp(duration, 0.01f, 30f) : 0.5f;
            releaseFraction = Finite(releaseFraction) ? Clamp(releaseFraction, 0f, 1f) : 0.2f;
            Generation = Generation == uint.MaxValue ? 1 : Generation + 1;
            started = now;
            releaseAt = now + duration * releaseFraction;
            ends = releaseAt + 0.12f;
            cancelled = false;
            return Generation;
        }

        internal void Release(uint token, float now)
        {
            if (token == 0 || token != Generation || cancelled) return;
            releaseAt = now;
            ends = now + 0.12f;
        }

        internal void Cancel(uint token, float now)
        {
            if (token == 0 || token != Generation || cancelled) return;
            float pulse;
            Sample(now, out cancelWeight, out cancelTravel, out pulse);
            cancelled = true;
            cancelAt = now;
            ends = now + 0.04f;
        }

        internal bool Sample(float now, out float weight, out float travel, out float pulse)
        {
            weight = travel = pulse = 0f;
            if (Generation == 0 || now < started || now >= ends) return false;
            if (cancelled)
            {
                weight = cancelWeight * Clamp((ends - now) / 0.04f, 0f, 1f);
                travel = cancelTravel;
                return weight > 0.001f;
            }
            float build = Clamp((now - started) / Math.Max(0.001f, releaseAt - started), 0f, 1f);
            float onset = Clamp((now - started) / Math.Max(0.001f, Math.Min(0.04f, releaseAt - started)), 0f, 1f);
            float tail = now <= releaseAt ? 1f : Clamp((ends - now) / 0.12f, 0f, 1f);
            weight = onset * (0.4f + 0.6f * build) * tail;
            travel = 0.15f + 0.85f * build;
            pulse = Clamp(1f - Math.Abs(now - releaseAt) / 0.055f, 0f, 1f) * tail;
            return weight > 0.001f;
        }

        internal void Clear() { Generation = 0; cancelled = false; }
        private static bool Finite(float x) { return !float.IsNaN(x) && !float.IsInfinity(x); }
        private static float Clamp(float x, float lo, float hi) { return Math.Max(lo, Math.Min(hi, x)); }
    }
}
