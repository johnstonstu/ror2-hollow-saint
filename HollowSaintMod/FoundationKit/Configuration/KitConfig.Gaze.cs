using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace HollowSaint.FoundationKit
{
    public static partial class KitConfig
    {
        private static void BindGaze(ConfigFile c)
        {
            F(c, gaze, "Armor while channeling", Gaze.GazeTuning.Armor, v => Gaze.GazeTuning.Armor = v, 0f, 100f, 5f, "Bonus armor from the wind-up to the end of the beam (100 armor = half damage taken).");
            F(c, gaze, "Beam seconds", Gaze.GazeTuning.BeamSeconds, v => Gaze.GazeTuning.BeamSeconds = v, 1f, 6f, 0.5f,
                "Base duration. Each level above 1 adds 0.1 seconds, up to 2 extra; total duration is capped at 6 seconds and fixed at cast entry.");
            F(c, gaze, "Damage per second", Gaze.GazeTuning.DamagePerSecond, v => Gaze.GazeTuning.DamagePerSecond = v, 1f, 20f, 0.5f, "Damage coefficient per second of the beam core at base attack speed. Attack speed adds ticks.");
            F(c, gaze, "Tick seconds", Gaze.GazeTuning.TickSeconds, v => Gaze.GazeTuning.TickSeconds = v, 0.1f, 0.5f, 0.05f, "Seconds between core damage ticks at base attack speed.");
            F(c, gaze, "Proc coefficient", Gaze.GazeTuning.ProcCoefficient, v => Gaze.GazeTuning.ProcCoefficient = v, 0f, 1f, 0.05f, "Item proc coefficient of each core tick (and so how much Static it builds).");
            F(c, gaze, "Range", Gaze.GazeTuning.Range, v => Gaze.GazeTuning.Range = v, 20f, 120f, 5f, "Beam length in metres. World geometry stops it.");
            F(c, gaze, "Beam radius", Gaze.GazeTuning.Radius, v => Gaze.GazeTuning.Radius = v, 0.5f, 4f, 0.1f, "Half the beam's hit width in metres.");
            F(c, gaze, "Turn rate", Gaze.GazeTuning.TurnDegreesPerSecond, v => Gaze.GazeTuning.TurnDegreesPerSecond = v, 30f, 360f, 10f, "Degrees per second the beam can turn to follow your aim.");
            F(c, gaze, "Splash radius", Gaze.GazeTuning.SplashRadius, v => Gaze.GazeTuning.SplashRadius = v, 0f, 8f, 0.5f, "Metres around the impact.");
            F(c, gaze, "Splash damage", Gaze.GazeTuning.SplashFraction, v => Gaze.GazeTuning.SplashFraction = v, 0f, 2f, 0.05f, "Fraction of one core tick dealt to enemies at the impact the core missed.");
            F(c, gaze, "Fork interval", Gaze.GazeTuning.ForkInterval, v => Gaze.GazeTuning.ForkInterval = v, 0.2f, 2f, 0.05f, "Seconds between ground fork volleys.");
            I(c, gaze, "Forks per volley", Gaze.GazeTuning.ForkCount, v => Gaze.GazeTuning.ForkCount = v, 0, 6, "Enemies each volley forks into.");
            F(c, gaze, "Reach at start", Gaze.GazeTuning.ReachStart, v => Gaze.GazeTuning.ReachStart = v, 0.1f, 2f, 0.05f, "Splash, fork and chain reach multiplier when the beam starts.");
            F(c, gaze, "Reach at end", Gaze.GazeTuning.ReachEnd, v => Gaze.GazeTuning.ReachEnd = v, 0.5f, 3f, 0.05f, "Splash, fork and chain reach multiplier at the end of a full channel.");
            F(c, gaze, "Fork range", Gaze.GazeTuning.ForkRange, v => Gaze.GazeTuning.ForkRange = v, 2f, 20f, 0.5f, "Metres from the impact.");
            F(c, gaze, "Fork damage", Gaze.GazeTuning.ForkDamage, v => Gaze.GazeTuning.ForkDamage = v, 0f, 5f, 0.1f, "Damage coefficient of each fork.");
            F(c, gaze, "Chain range", Gaze.GazeTuning.ChainRange, v => Gaze.GazeTuning.ChainRange = v, 0f, 15f, 0.5f, "Metres a fork chains to one more enemy.");
            F(c, gaze, "Chain damage", Gaze.GazeTuning.ChainFraction, v => Gaze.GazeTuning.ChainFraction = v, 0f, 1.5f, 0.05f, "Fraction of the fork's damage dealt by the chain hop.");
            F(c, gaze, "Launch height", Gaze.GazeTuning.LaunchHeight, v => Gaze.GazeTuning.LaunchHeight = v, 0f, 15f, 0.5f, "Metres above the ground the cast lifts you to.");
            F(c, gaze, "Drift speed", Gaze.GazeTuning.DriftSpeedMultiplier, v => Gaze.GazeTuning.DriftSpeedMultiplier = v, 0f, 1f, 0.05f, "Move speed multiplier while hovering.");
            // Dev A/B for the 1.2 hold/release trial; read once (restart). Remove before release.
            Gaze.GazeReleaseTuning.UseLegacyTapPulses(c.Bind(gaze, "Legacy tap pulses (A/B, restart)", false,
                "Dev comparison: use the 1.1-style tap-to-spend Gaze pulses instead of hold/release. Restart the game after changing.").Value);
            F(c, gaze, "Charge seconds", Gaze.GazeReleaseTuning.SecondsPerExtraCharge, v => Gaze.GazeReleaseTuning.SecondsPerExtraCharge = v, 0.1f, 1f, 0.02f, "Hold Primary while beaming: seconds to load each extra charge (up to three).");
            F(c, gaze, "Surge damage per charge", Gaze.GazeReleaseTuning.DamagePerCharge, v => Gaze.GazeReleaseTuning.DamagePerCharge = v, 0.5f, 10f, 0.25f, "Damage coefficient of a released surge, per charge spent.");
            F(c, gaze, "Surge proc per charge", Gaze.GazeReleaseTuning.ProcPerCharge, v => Gaze.GazeReleaseTuning.ProcPerCharge = v, 0f, 1f, 0.05f, "Item proc coefficient of a released surge, per charge spent (capped at 1).");
            F(c, gaze, "Cooldown", Gaze.GazeTuning.Cooldown, v => Gaze.GazeTuning.Cooldown = v, 4f, 40f, 0.5f, "Seconds, counted from the end of the beam (restart).", restart: true);
        }

    }
}
