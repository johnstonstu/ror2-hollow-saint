namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>
    /// Gaze of the Hollow (alternate special): the halo leaves the head, floats in front of the
    /// Saint and fires a continuous lightning beam while the Saint hovers above the
    /// fight. Fields, not constants: KitConfig binds them (section "7. Gaze of the Hollow").
    /// Cooldown is baked into the SkillDef at load (restart).
    /// </summary>
    public static class GazeTuning
    {
        // Timing.
        public static float Cooldown = 12f;                 // starts when the skill ends (v0.9.10: was 16)
        public static float WindupSeconds = 1.0f;           // launch + crown dismount, no damage
        public static float BeamSeconds = 4f;              // base; level baseline caps at six, launched pulses extend to fourteen
        // v0.9.13 (Stu): splash/fork/chain reach grows over the channel, from ReachStart to ReachEnd x the base ranges.
        public static float ReachStart = 0.4f;
        public static float ReachEnd = 1.6f;
        // v0.9.15 (Stu): small armor bonus while channeling (wind-up and beam).
        public static float Armor = 30f;
        public const float EndSeconds = 0.4f;               // crown returns to the head

        // Movement.
        public static float LaunchHeight = 6f;              // metres above the ground under the Saint
        public static float LaunchRiseSeconds = 0.4f;
        public const float CeilingHeadroom = 1.5f;
        public static float DriftSpeedMultiplier = 0.3f;    // walk speed while beaming
        public static float SinkPerSecond = 0.25f;          // hover height slowly settles during the beam
        public static float TurnDegreesPerSecond = 90f;

        // Core beam.
        public static float DamagePerSecond = 5f;           // coefficient per second at 1x attack speed
        public static float TickSeconds = 0.2f;             // attack speed shortens it (more ticks)
        public static float ProcCoefficient = 0.3f;         // 1.2: was 0.5 (7 s beam = 75% more ticks)
        public static float Range = 60f;
        public static float Radius = 1.5f;                  // half the beam's hit width

        // Impact splash (enemies near the impact the core did not hit this tick).
        public static float SplashRadius = 3f;
        public static float SplashFraction = 0.5f;          // of one core tick
        public const float SplashProc = 0.2f;               // 1.2: was 0.3

        // Ground forks.
        public static float ForkInterval = 0.5f;
        public static int ForkCount = 2;
        public static float ForkRange = 8f;
        public static float ForkDamage = 0.7f;              // 1.2: was 1.0 (14 volleys per cast, not 8)
        public const float ForkProc = 0.3f;
        public static float ChainRange = 6f;
        public static float ChainFraction = 0.6f;           // of the fork's damage
        public const float ChainProc = 0.2f;

        // Crown placement relative to the body's core, along the beam.
        public const float CrownUp = 0.75f;
        public const float CrownForward = 1.25f;
    }
}
