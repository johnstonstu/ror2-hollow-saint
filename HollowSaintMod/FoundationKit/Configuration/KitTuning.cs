namespace HollowSaint.FoundationKit
{
    // Shared foundation for the kit. docs/kit-architecture.md explains the networking
    // model every skill follows. Short version:
    //
    //   * EntityStates run on EVERY machine. The owning client (isAuthority) decides
    //     timing and aim. The server runs its own copy of the same state
    //     (NetworkServer.active; isAuthority is false there for a remote player), and that
    //     copy is where buffs get applied.
    //   * Projectiles are fired from the AUTHORITY via ProjectileManager.FireProjectile,
    //     which forwards to the server when called on a client. Never gate a projectile
    //     on NetworkServer.active, or clients can never shoot.
    //   * Direct damage (TakeDamage, BlastAttack, chain hops) and meter changes happen on
    //     the server only (NetworkServer.active). Never gate server work on
    //     hasEffectiveAuthority: on the host, a remote player's body is not "ours".
    //   * State that clients must see (Storm charge, Open Circuit window) lives in
    //     buffs, because CharacterBody already replicates buffs.

    /// <summary>Central tuning. Approved numbers unless marked PROPOSAL.</summary>
    /// <summary>Central tuning. Approved numbers unless marked PROPOSAL. Fields, not
    /// constants: KitConfig binds most of them to the BepInEx config and the in-game
    /// Risk of Options menu. Cooldowns and stocks are baked into SkillDefs at load, so
    /// those need a restart; everything read per use (damage, ranges, counts) is live.</summary>
    public static partial class KitTuning
    {
        // Arc Bolt: approved. One shot every 0.5 s at 1x attack speed.
        public static float ArcBoltInterval = 0.5f;
        public static float ArcBoltDamageCoefficient = 1.9f; // 1.3.1: 171% effective (Titan bench 35 -> ~42 DPS; Commando ~67, Huntress ~30)
        public static int ArcBoltMaxChainTargets = 4;
        public static float ArcBoltChainRange = 12f;
        public static float ArcBoltChainFalloff = 0.75f;
        // v0.9.13 (Stu: primary procs felt high): direct hit 0.8 (was 1.0); chain hops 0.4, 0.2, 0.1 (were 0.5 each).
        public static float ArcBoltProcCoefficient = 1f;
        public static float ArcBoltChainProc = 0.4f;
        public static float ArcBoltProjectileSpeed = 120f;
        public static float ArcBoltRadius = 0.75f;
        public static float ArcBoltAssistConeDegrees = 3f;
        // Anim spec section 4: "Bolt release" marker.
        public static float ArcBoltReleaseNormalizedTime = 0.2105f;
        // v0.9.10: shortest time an Arc Bolt arm gesture plays in (high attack speed overlaps gestures).
        public static float ArcBoltMinGestureSeconds = 0.25f;

        // Stormspear (secondary): see Stormspear/StormspearTuning.cs.

        // Arc Step: approved.
        public static int ArcStepMaxStock = 2;
        public static float ArcStepRecharge = 5f;
        public static float ArcStepDuration = 0.55f;
        public static float ArcStepLookLift = 0.35f; // v0.9.16: share of the dash speed that follows the aim pitch
        public static float ArcStepSpeed = 20f; // was 16; playtest: 'needs to move a bit further'
        // PROPOSAL: no i-frames pending Stuart's decision.
        public static bool ArcStepGrantsIFrames = false;

        // Open Circuit: approved.
        // v0.9.15: crown 10 s (was 8), cooldown 8 s counted from when the crown closes (was 12 s from the cast).
        public static float OpenCircuitCooldown = 8f;
        public static float OpenCircuitBuffSeconds = 10f;
        public static float OpenCircuitPulseInterval = 0.5f;
        public static float OpenCircuitPulseDamageCoefficient = 0.8f;
        public static float OpenCircuitRadius = 8f;
        // Storm passive ("Answered Prayer"): Static -> Electrocute -> Thunderbolt.
        // See docs/storm-passive.md. All live-tunable (section "5. Storm").
        public static float StaticThreshold = 0.25f;          // fraction of target max health that fills Static 0 -> 100%
        public static float StaticMinGain = 0.06f;             // was 0.08: minimum Static per full-proc hit
        public static float StaticCritMultiplier = 1.5f;
        public static float StaticOpenCircuitWeight = 0.3f;    // proc-equivalent for Open Circuit pulses (they carry proc 0)
        public static float StaticDecayDelay = 2f;             // seconds after the last hit before decay starts
        public static float StaticDecayPerSecond = 0.5f;       // fraction per second
        public static float ElectrocuteStunSeconds = 0.5f;     // v0.9.13: was 1.5 (a jolt, not a long stun)
        public static float ShockedSeconds = 3f;
        public static float ShockedDamageMultiplier = 1.15f;   // was 1.2
        public static float ElectrocutePopDamageCoefficient = 1.5f; // was 2.5
        public static float ElectrocutePopRadius = 6f;
        public static int ElectrocutePopTargets = 2;           // was 3
        public static float ElectrocutePopProc = 0.3f;         // was 0.5
        public static float ElectrocutePopStatic = 0.15f;      // was 0.4 (cascade)
        public static float ElectrocuteImmuneSeconds = 4f;
        public static float DeathDischargeStatic = 0.3f;        // v0.9.16: dying with this much Static Electrocutes (0 = off)
        public static int ElectrocutesPerSecondCap = 4;
        public static int StormChargeMax = 5;                  // Electrocutes per Thunderbolt (v0.9.16 Stu: was 6)
        public static float ThunderboltDamageCoefficient = 10f;
        public static float ThunderboltSplashFraction = 0.5f;
        public static float ThunderboltSplashRadius = 3f;
        public static float ThunderboltRange = 30f;
        public static float ThunderboltTelegraphSeconds = 0.6f;    // v0.9.15: was 0.35 (the charges combine visibly)
        public static float ThunderboltFlightSeconds = 1.0f;       // v0.9.15: launch to strike, was a fixed 0.25
        public static float ThunderboltCooldown = 4f;
    }
}
