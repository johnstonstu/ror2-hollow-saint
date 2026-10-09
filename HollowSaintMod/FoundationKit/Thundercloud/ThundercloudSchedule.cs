using System;
using HollowSaint.FoundationKit.ChargedStorm;

namespace HollowSaint.FoundationKit.Thundercloud
{
    /// <summary>Lingering storm: rises fast, then strikes everything beneath it on a fixed
    /// cadence for a window that grows with gathered charges, then fades.</summary>
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
    }
}
