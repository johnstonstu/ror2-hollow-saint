using System;
using HollowSaint.FoundationKit.ChargedStorm;

namespace HollowSaint.FoundationKit.Thundercloud
{
    /// <summary>Lingering storm: rises fast, then strikes everything beneath it on a cadence
    /// (faster with attack speed, per instance; the charges-only overloads are the 1x schedule) for a window that grows with gathered charges, then fades.</summary>
    internal static class ThundercloudSchedule
    {
        internal const float Ascent = .5f, FirstStrike = .6f, Fade = .7f;
        internal const float FreeCastGrace = .3f; // release before this casts the free storm
        internal const int ReturnStrokeCount = 4;
        internal const float ReturnStrokeInterval = .55f, BoltLifetime = .28f;
        internal static float StrokeAt(int index) => Math.Max(0, Math.Min(ReturnStrokeCount - 1, index)) * ReturnStrokeInterval;
        internal static float Interval => ChargedStormTuning.Bound(ChargedStormTuning.CloudStrikeInterval, .25f, 3f);
        internal static float Window(int charges) => ChargedStormTuning.CloudDuration(charges);
        /// <summary>Pulse times: FirstStrike, then every Interval while inside the window.</summary>
        internal static int Pulses(int charges) => 1 + (int)Math.Floor(Window(charges) / Interval + 1e-4);
        internal static float PulseAt(int index) => FirstStrike + Math.Max(0, index) * Interval;
        internal static float CompleteAt(int charges) => PulseAt(Pulses(charges) - 1) + BoltLifetime * 1.4f + Fade;

        // 1.3.1: attack speed adds strikes inside the same window (like Flamethrower ticks), so
        // Syringes and friends feed the cloud. Captured once at cast; capped at twice the rate.
        internal static float IntervalFor(float attackSpeed) =>
            Interval / Math.Min(2f, Math.Max(1f, float.IsNaN(attackSpeed) ? 1f : attackSpeed));
        internal static int Pulses(int charges, float interval) => 1 + (int)Math.Floor(Window(charges) / interval + 1e-4);
        internal static float PulseAt(int index, float interval) => FirstStrike + Math.Max(0, index) * interval;
        internal static float CompleteAt(int charges, float interval) => PulseAt(Pulses(charges, interval) - 1, interval) + BoltLifetime * 1.4f + Fade;
    }
}
