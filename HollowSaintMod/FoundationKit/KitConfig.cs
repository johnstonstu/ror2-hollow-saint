using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace HollowSaint.FoundationKit
{
    /// <summary>
    /// Binds the balance numbers in KitTuning to BepInEx/config/com.johnstonstu.hollowsaint.cfg
    /// and, when Risk of Options is installed, to sliders in the in-game Mod Options menu.
    /// Values marked (restart) are baked into SkillDefs at load.
    /// </summary>
    public static class KitConfig
    {
        internal struct FloatOption { public ConfigEntry<float> Entry; public float Min, Max, Step; public bool Restart; }
        internal struct IntOption { public ConfigEntry<int> Entry; public int Min, Max; public bool Restart; }

        internal static readonly List<FloatOption> Floats = new List<FloatOption>();
        internal static readonly List<IntOption> Ints = new List<IntOption>();
        internal static readonly List<ConfigEntry<bool>> Bools = new List<ConfigEntry<bool>>();

        /// <summary>Debug logging of the first few gameplay events (HOLLOW_SAINT_EVENT lines).</summary>
        public static ConfigEntry<bool> EventLog;
        /// <summary>Stormspear "Spear hand" (Auto, Left, Right).</summary>
        public static ConfigEntry<SpearDischarge.SpearHand> SpearHand;

        internal static void Bind(ConfigFile c)
        {
            const string move = "0. Movement", bolt = "1. Arc Bolt", spear = "2. Stormspear", step = "3. Arc Step",
                circuit = "4. Open Circuit", storm = "5. Storm", misc = "6. Misc", gaze = "7. Gaze of the Hollow";

            F(c, move, "Move speed", FoundationBody.BaseMoveSpeed, v => { FoundationBody.BaseMoveSpeed = v; FoundationBody.ApplyMovement(); }, 4f, 14f, 0.1f, "Base move speed in m/s (Commando is 7).");
            F(c, move, "Sprint multiplier", FoundationBody.SprintMultiplier, v => { FoundationBody.SprintMultiplier = v; FoundationBody.ApplyMovement(); }, 1f, 3f, 0.05f, "Sprint speed multiplier (Commando is 1.45).");
            F(c, move, "Jump power", FoundationBody.BaseJumpPower, v => { FoundationBody.BaseJumpPower = v; FoundationBody.ApplyMovement(); }, 8f, 30f, 0.5f, "Base jump power (Commando is 15).");
            B(c, move, "Arm life", FoundationArmPose.LifeEnabled, v => FoundationArmPose.LifeEnabled = v, "Procedural arm motion layered over the animation: breathing, idle sway, finger and wrist motion, follow-through and reaction to movement (acceleration, turns, jumps, landings). The casting arm drops to 25% during skill gestures.");
            B(c, move, "Aim follows crosshair", FoundationAimPose.Enabled, v => FoundationAimPose.Enabled = v, "The torso, neck and head follow where you aim (full in combat, a relaxed look out of combat).");
            B(c, misc, "Item displays (restart)", FoundationBody.ItemDisplays, v => FoundationBody.ItemDisplays = v, "Show picked-up items on the Saint (placements borrowed from Commando on matching mounts). Restart to apply.");
            B(c, misc, "Impact feel", Vfx.ImpactFeelSettings.Enabled, v => Vfx.ImpactFeelSettings.Enabled = v, "Brief hit-pause and camera shake on spear impacts and Thunderbolt strikes.");
            B(c, misc, "RoR2 body shader (restart)", FoundationHopoo.Enabled, v => FoundationHopoo.Enabled = v, "Use Risk of Rain 2's own body shader. Adds the elite body colour tint, but the dark skins and night stages look much darker. Takes effect on the next game start.");
            B(c, misc, "Status overlays (restart)", FoundationHopoo.OverlaysOnStandard, v => FoundationHopoo.OverlaysOnStandard = v, "Show cloak, shield, crit, immunity and elite overlays on the Saint with the normal shading. Off hides him completely while cloaked. Takes effect on the next game start.");
            F(c, move, "Arm life intensity", FoundationArmPose.LifeIntensity, v => FoundationArmPose.LifeIntensity = v, 0f, 2f, 0.05f, "Scale of the procedural arm life (0 = off, 1 = default).");

            // Arm life components (v0.7 arm reactions). Each multiplies "Arm life intensity";
            // the "Arm life" toggle still turns everything off.
            F(c, move, "Arm reaction to movement", FoundationArmPose.ReactionIntensity, v => FoundationArmPose.ReactionIntensity = v, 0f, 2f, 0.05f, "Arms trail acceleration, drag outward on turns and trail back while sprinting or gliding (0 = off, 1 = default).");
            F(c, move, "Arm air and landing reaction", FoundationArmPose.AirIntensity, v => FoundationArmPose.AirIntensity = v, 0f, 2f, 0.05f, "Arms float up while airborne and dip on jumps and landings, scaled by landing speed (0 = off, 1 = default).");
            F(c, move, "Arm idle sway", FoundationArmPose.IdleSwayIntensity, v => FoundationArmPose.IdleSwayIntensity = v, 0f, 2f, 0.05f, "Slow calm sway of the whole arm (0 = off, 1 = default).");
            F(c, move, "Arm follow-through", FoundationArmPose.FollowThrough, v => FoundationArmPose.FollowThrough = v, 0f, 2f, 0.05f, "How far elbow, wrist and fingers lag behind the shoulder in the reactions (0 = arm moves as one piece, 1 = default).");

            F(c, bolt, "Damage", KitTuning.ArcBoltDamageCoefficient, v => KitTuning.ArcBoltDamageCoefficient = v, 0.2f, 4f, 0.05f, "Damage coefficient of the bolt.");
            F(c, bolt, "Fire interval", KitTuning.ArcBoltInterval, v => KitTuning.ArcBoltInterval = v, 0.2f, 1.5f, 0.05f, "Seconds per shot at 1x attack speed.");
            F(c, bolt, "Aim assistance angle", KitTuning.ArcBoltAssistConeDegrees, v => KitTuning.ArcBoltAssistConeDegrees = v, 0f, 6f, 0.5f, "Degrees either side of the launch direction that can acquire one visible enemy for gentle homing. 0 disables assistance; applies to newly fired bolts.");
            F(c, bolt, "Proc coefficient", KitTuning.ArcBoltProcCoefficient, v => KitTuning.ArcBoltProcCoefficient = v, 0f, 1f, 0.05f, "Item proc coefficient of the direct hit (restart).", restart: true);
            F(c, bolt, "Chain proc coefficient", KitTuning.ArcBoltChainProc, v => KitTuning.ArcBoltChainProc = v, 0f, 1f, 0.05f, "Proc coefficient of the first chain hop; each further hop halves it.");
            I(c, bolt, "Chain targets", KitTuning.ArcBoltMaxChainTargets, v => KitTuning.ArcBoltMaxChainTargets = v, 1, 10, "Enemies hit per bolt including the first.");
            F(c, bolt, "Chain range", KitTuning.ArcBoltChainRange, v => KitTuning.ArcBoltChainRange = v, 4f, 30f, 0.5f, "Metres between chain hops.");
            F(c, bolt, "Chain falloff", KitTuning.ArcBoltChainFalloff, v => KitTuning.ArcBoltChainFalloff = v, 0.3f, 1f, 0.05f, "Damage multiplier per hop.");
            F(c, bolt, "Projectile speed", KitTuning.ArcBoltProjectileSpeed, v => KitTuning.ArcBoltProjectileSpeed = v, 30f, 200f, 5f, "Metres per second (restart). Speeds above 80 shorten flight lifetime to preserve the previous maximum range.", restart: true);

            F(c, spear, "Charge seconds", Stormspear.StormspearTuning.ChargeSeconds, v => Stormspear.StormspearTuning.ChargeSeconds = v, 0.4f, 6f, 0.1f, "Seconds from tap to full charge at 1x attack speed. Attack speed shortens it.");
            F(c, spear, "Aim assistance angle", Stormspear.StormspearTuning.AssistConeDegrees, v => Stormspear.StormspearTuning.AssistConeDegrees = v, 0f, 6f, 0.5f, "Degrees either side of the launch direction that can acquire one visible enemy for gentle homing. 0 disables assistance; applies to new hand and crown throws.");
            F(c, spear, "Crown charge multiplier", Stormspear.StormspearTuning.CrownChargeMultiplier, v => Stormspear.StormspearTuning.CrownChargeMultiplier = v, 1f, 6f, 0.1f, "How much faster the spear charges while Open Circuit is up.");
            SpearHand = c.Bind(spear, "Spear hand", SpearDischarge.SpearHand.Auto, "Which hand holds and throws the hand spear. Auto follows your input device: left hand on a controller (left trigger), right hand on mouse and keyboard (right-click). Arc Bolt fires from the other hand while the spear charges. The hand only changes between throws.");
            SpearDischarge.SpearCarry.HandMode = SpearHand.Value;
            SpearHand.SettingChanged += (s, e) => SpearDischarge.SpearCarry.HandMode = SpearHand.Value;
            F(c, spear, "Off-hand Arc Bolt rate", Stormspear.StormspearTuning.OffHandRateMultiplier, v => Stormspear.StormspearTuning.OffHandRateMultiplier = v, 0.1f, 1f, 0.05f, "Arc Bolt fire-rate multiplier while charging in the hand (from the free hand only). No penalty in the crown.");
            F(c, spear, "Minimum throw interval", Stormspear.StormspearTuning.MinThrowInterval, v => Stormspear.StormspearTuning.MinThrowInterval = v, 0.05f, 1f, 0.05f, "Seconds between tap throws at 1x attack speed when dumping stocks.");
            F(c, spear, "Tap damage", Stormspear.StormspearTuning.TapDamage, v => Stormspear.StormspearTuning.TapDamage = v, 0.5f, 12f, 0.1f, "Damage coefficient of an uncharged throw.");
            F(c, spear, "Full damage", Stormspear.StormspearTuning.FullDamage, v => Stormspear.StormspearTuning.FullDamage = v, 1f, 30f, 0.5f, "Damage coefficient of a fully charged throw.");
            F(c, spear, "Cooldown", Stormspear.StormspearTuning.Cooldown, v => Stormspear.StormspearTuning.Cooldown = v, 1f, 15f, 0.5f, "Seconds per stock (restart).", restart: true);
            F(c, spear, "Burst radius (tap)", Stormspear.StormspearTuning.BurstRadiusTap, v => Stormspear.StormspearTuning.BurstRadiusTap = v, 1f, 15f, 0.5f, "Metres, uncharged throw.");
            F(c, spear, "Burst radius (full)", Stormspear.StormspearTuning.BurstRadiusFull, v => Stormspear.StormspearTuning.BurstRadiusFull = v, 1f, 25f, 0.5f, "Metres, fully charged throw.");
            F(c, spear, "Burst damage", Stormspear.StormspearTuning.BurstDamageFraction, v => Stormspear.StormspearTuning.BurstDamageFraction = v, 0f, 2f, 0.05f, "Fraction of the direct-hit damage dealt to every other enemy in the burst, uncharged.");
            F(c, spear, "Burst damage (full)", Stormspear.StormspearTuning.BurstDamageFractionFull, v => Stormspear.StormspearTuning.BurstDamageFractionFull = v, 0f, 2f, 0.05f, "Fraction of the direct-hit damage dealt to every other enemy in the burst, fully charged (scales with charge from Burst damage).");
            F(c, spear, "Stick seconds", Stormspear.StormspearTuning.StickSeconds, v => Stormspear.StormspearTuning.StickSeconds = v, 0f, 1f, 0.05f, "How long the spear stays lodged in what it hit before it bursts.");
            F(c, spear, "Ground burst", Stormspear.StormspearTuning.GroundBurstScale, v => Stormspear.StormspearTuning.GroundBurstScale = v, 0f, 1f, 0.05f, "Damage and radius of the burst when the spear hits terrain instead of an enemy (1 = same as an enemy hit).");
            F(c, spear, "Burst proc coefficient", Stormspear.StormspearTuning.BurstProcCoefficient, v => Stormspear.StormspearTuning.BurstProcCoefficient = v, 0f, 1f, 0.05f, "Item proc coefficient of the burst (and so how much Static it builds).");
            B(c, spear, "Crown Thunderbolt", Stormspear.StormspearTuning.CrownThunderbolt, v => Stormspear.StormspearTuning.CrownThunderbolt = v, "A fully charged spear thrown from the crown also calls a Thunderbolt at the impact point, independent of the Storm charge.");
            F(c, spear, "Crown Thunderbolt min charge", Stormspear.StormspearTuning.CrownThunderboltMinCharge, v => Stormspear.StormspearTuning.CrownThunderboltMinCharge = v, 0.1f, 1f, 0.05f, "Charge fraction needed for the crown Thunderbolt (1 = full).");

            F(c, step, "Look lift", KitTuning.ArcStepLookLift, v => KitTuning.ArcStepLookLift = v, 0f, 1f, 0.05f, "How much the step follows where you look, up or down (0 = flat; 0.35 lifts about 2 m looking 45 degrees up).");
            F(c, step, "Speed", KitTuning.ArcStepSpeed, v => KitTuning.ArcStepSpeed = v, 6f, 40f, 0.5f, "Dash speed in m/s at the start of the step.");
            F(c, step, "Duration", KitTuning.ArcStepDuration, v => KitTuning.ArcStepDuration = v, 0.15f, 1.2f, 0.05f, "Seconds per step.");
            F(c, step, "Recharge", KitTuning.ArcStepRecharge, v => KitTuning.ArcStepRecharge = v, 1f, 15f, 0.5f, "Seconds per charge (restart).", restart: true);
            I(c, step, "Charges", KitTuning.ArcStepMaxStock, v => KitTuning.ArcStepMaxStock = v, 1, 5, "Stock (restart).", restart: true);
            B(c, step, "Invulnerable while stepping", KitTuning.ArcStepGrantsIFrames, v => KitTuning.ArcStepGrantsIFrames = v, "Undecided design question; off by default.");

            F(c, circuit, "Pulse damage", KitTuning.OpenCircuitPulseDamageCoefficient, v => KitTuning.OpenCircuitPulseDamageCoefficient = v, 0.1f, 3f, 0.05f, "Damage coefficient per pulse.");
            F(c, circuit, "Pulse interval", KitTuning.OpenCircuitPulseInterval, v => KitTuning.OpenCircuitPulseInterval = v, 0.2f, 2f, 0.05f, "Seconds between pulses.");
            F(c, circuit, "Radius", KitTuning.OpenCircuitRadius, v => KitTuning.OpenCircuitRadius = v, 3f, 25f, 0.5f, "Metres.");
            F(c, circuit, "Duration", KitTuning.OpenCircuitBuffSeconds, v => KitTuning.OpenCircuitBuffSeconds = v, 2f, 20f, 0.5f, "Seconds the crown stays open.");
            F(c, circuit, "Cooldown", KitTuning.OpenCircuitCooldown, v => KitTuning.OpenCircuitCooldown = v, 3f, 30f, 0.5f, "Seconds (restart). Counted from when the crown closes unless Cooldown after crown is off.", restart: true);
            B(c, circuit, "Cooldown after crown", OpenCircuit.OpenCircuitTuning.CooldownAfterCrown, v => OpenCircuit.OpenCircuitTuning.CooldownAfterCrown = v, "The cooldown starts when the crown closes instead of on the cast, so cooldown items can't keep the crown up forever.");

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
            F(c, gaze, "Cooldown", Gaze.GazeTuning.Cooldown, v => Gaze.GazeTuning.Cooldown = v, 4f, 40f, 0.5f, "Seconds, counted from the end of the beam (restart).", restart: true);

            F(c, storm, "Static threshold", KitTuning.StaticThreshold, v => KitTuning.StaticThreshold = v, 0.05f, 1f, 0.05f, "Fraction of the target's max health a full-proc hit needs to deal to fill Static from 0 to 100%.");
            F(c, storm, "Static minimum per hit", KitTuning.StaticMinGain, v => KitTuning.StaticMinGain = v, 0.01f, 0.5f, 0.01f, "Minimum Static per full-proc hit, so bosses still build.");
            F(c, storm, "Static crit multiplier", KitTuning.StaticCritMultiplier, v => KitTuning.StaticCritMultiplier = v, 1f, 3f, 0.05f, "Static multiplier on critical hits.");
            F(c, storm, "Static Open Circuit weight", KitTuning.StaticOpenCircuitWeight, v => KitTuning.StaticOpenCircuitWeight = v, 0f, 1f, 0.05f, "Proc-coefficient equivalent used for Open Circuit pulses (they carry no proc).");
            F(c, storm, "Static decay delay", KitTuning.StaticDecayDelay, v => KitTuning.StaticDecayDelay = v, 0f, 10f, 0.25f, "Seconds after the last hit before Static starts to drain.");
            F(c, storm, "Static decay rate", KitTuning.StaticDecayPerSecond, v => KitTuning.StaticDecayPerSecond = v, 0.05f, 3f, 0.05f, "Fraction of full Static drained per second.");
            F(c, storm, "Electrocute stun", KitTuning.ElectrocuteStunSeconds, v => KitTuning.ElectrocuteStunSeconds = v, 0f, 5f, 0.25f, "Seconds of the Electrocute jolt (stun) on enemies that can be stunned (0 = none). Every Electrocuted enemy is also Shocked.");
            F(c, storm, "Shocked duration", KitTuning.ShockedSeconds, v => KitTuning.ShockedSeconds = v, 0.5f, 10f, 0.5f, "Seconds.");
            F(c, storm, "Shocked damage taken", KitTuning.ShockedDamageMultiplier, v => KitTuning.ShockedDamageMultiplier = v, 1f, 2f, 0.05f, "Multiplier on ALL damage a Shocked enemy takes.");
            F(c, storm, "Pop damage", KitTuning.ElectrocutePopDamageCoefficient, v => KitTuning.ElectrocutePopDamageCoefficient = v, 0.5f, 10f, 0.1f, "Damage coefficient of the Electrocute arc burst.");
            F(c, storm, "Pop radius", KitTuning.ElectrocutePopRadius, v => KitTuning.ElectrocutePopRadius = v, 2f, 20f, 0.5f, "Metres.");
            I(c, storm, "Pop targets", KitTuning.ElectrocutePopTargets, v => KitTuning.ElectrocutePopTargets = v, 1, 10, "Enemies hit by one Electrocute burst.");
            F(c, storm, "Pop proc coefficient", KitTuning.ElectrocutePopProc, v => KitTuning.ElectrocutePopProc = v, 0f, 1f, 0.05f, "Proc coefficient of the burst.");
            F(c, storm, "Pop Static", KitTuning.ElectrocutePopStatic, v => KitTuning.ElectrocutePopStatic = v, 0f, 1f, 0.05f, "Static each burst target gains (cascade).");
            F(c, storm, "Electrocute immunity", KitTuning.ElectrocuteImmuneSeconds, v => KitTuning.ElectrocuteImmuneSeconds = v, 0f, 15f, 0.5f, "Seconds an Electrocuted enemy cannot build Static.");
            I(c, storm, "Electrocute cap per second", KitTuning.ElectrocutesPerSecondCap, v => KitTuning.ElectrocutesPerSecondCap = v, 1, 20, "Max Electrocutes per second per Saint (screen and performance guard).");
            F(c, storm, "Death discharge", KitTuning.DeathDischargeStatic, v => KitTuning.DeathDischargeStatic = v, 0f, 1f, 0.05f, "An enemy that dies holding at least this much Static (0.5 = half) Electrocutes as it dies: it lights an orb and arcs to its neighbours. 0 turns it off.");
            I(c, storm, "Charges per Thunderbolt", KitTuning.StormChargeMax, v => KitTuning.StormChargeMax = v, 2, 20, "Electrocutes needed to call a Thunderbolt.");
            F(c, storm, "Thunderbolt damage", KitTuning.ThunderboltDamageCoefficient, v => KitTuning.ThunderboltDamageCoefficient = v, 1f, 30f, 0.5f, "Damage coefficient of the strike (proc 1.0, can crit).");
            F(c, storm, "Thunderbolt splash", KitTuning.ThunderboltSplashFraction, v => KitTuning.ThunderboltSplashFraction = v, 0f, 1f, 0.05f, "Fraction of the strike damage dealt to nearby enemies.");
            F(c, storm, "Thunderbolt splash radius", KitTuning.ThunderboltSplashRadius, v => KitTuning.ThunderboltSplashRadius = v, 1f, 10f, 0.5f, "Metres.");
            F(c, storm, "Thunderbolt range", KitTuning.ThunderboltRange, v => KitTuning.ThunderboltRange = v, 10f, 60f, 1f, "Metres. Needs line of sight.");
            F(c, storm, "Thunderbolt telegraph", KitTuning.ThunderboltTelegraphSeconds, v => KitTuning.ThunderboltTelegraphSeconds = v, 0.1f, 1.5f, 0.05f, "Seconds the charges take to combine above the crown before the Thunderbolt launches.");
            F(c, storm, "Thunderbolt flight", KitTuning.ThunderboltFlightSeconds, v => KitTuning.ThunderboltFlightSeconds = v, 0.2f, 2f, 0.05f, "Seconds from the charge leaving the crown to the strike landing (rise, hang, streak across the sky, strike).");
            F(c, storm, "Thunderbolt cooldown", KitTuning.ThunderboltCooldown, v => KitTuning.ThunderboltCooldown = v, 0f, 20f, 0.5f, "Minimum seconds between strikes.");

            // Defaults migration: BepInEx keeps saved values, so a changed default only reaches an
            // existing config through here. Each step rewrites an entry only when its saved value
            // still equals the OLD default (the player never touched it); anything the player
            // changed is kept. Bump CurrentDefaultsVersion and add a step whenever a default changes.
            // A fresh config file starts at version 1, runs every step harmlessly (no value matches
            // an old default) and is stamped current.
            var defaultsVersion = c.Bind(misc, "Defaults version", 1, "Internal: tracks default-value migrations. Do not edit.");
            if (defaultsVersion.Value < 2)
            {
                Migrate(step, "Speed", 16f, 20f, v => KitTuning.ArcStepSpeed = v);
            }
            if (defaultsVersion.Value < 3)
            {
                // v0.7 early-game balance pass (tools/balance/dps_model.py).
                Migrate(storm, "Pop damage", 2.5f, 1.5f, v => KitTuning.ElectrocutePopDamageCoefficient = v);
                MigrateInt(storm, "Pop targets", 3, 2, v => KitTuning.ElectrocutePopTargets = v);
                Migrate(storm, "Pop proc coefficient", 0.5f, 0.3f, v => KitTuning.ElectrocutePopProc = v);
                Migrate(storm, "Pop Static", 0.4f, 0.15f, v => KitTuning.ElectrocutePopStatic = v);
                Migrate(storm, "Static minimum per hit", 0.08f, 0.06f, v => KitTuning.StaticMinGain = v);
                Migrate(storm, "Shocked damage taken", 1.2f, 1.15f, v => KitTuning.ShockedDamageMultiplier = v);
                // (v0.9: the spear spread/anchor entries these steps used to migrate are gone.)
            }
            // v0.9: the Conduit Spear / held fan / Conductor entries no longer exist; BepInEx leaves
            // their saved values as unread orphaned lines, which is harmless.
            if (defaultsVersion.Value < 4)
            {
                // v0.9.10: the spear burst no longer hits the struck enemy, so the direct hit goes up;
                // Gaze comes back sooner.
                Migrate(spear, "Full damage", 14f, 16f, v => Stormspear.StormspearTuning.FullDamage = v);
                Migrate(gaze, "Cooldown", 16f, 12f, v => Gaze.GazeTuning.Cooldown = v);
            }
            if (defaultsVersion.Value < 5)
            {
                // v0.9.10: RoR2's body shader on by default so status and elite effects show.
                MigrateBool(misc, "RoR2 body shader (restart)", false, true, v => FoundationHopoo.Enabled = v);
            }
            if (defaultsVersion.Value < 6)
            {
                // v0.9.11: back to the authored shading (overlays now work on it); undo the v5 flip.
                MigrateBool(misc, "RoR2 body shader (restart)", true, false, v => FoundationHopoo.Enabled = v);
            }
            if (defaultsVersion.Value < 7)
            {
                // v0.9.13 (Stu playtest): Electrocute is a short jolt; the full-charge splash is wider.
                Migrate(storm, "Electrocute stun", 1.5f, 0.5f, v => KitTuning.ElectrocuteStunSeconds = v);
                Migrate(spear, "Burst radius (full)", 9f, 10f, v => Stormspear.StormspearTuning.BurstRadiusFull = v);
            }
            if (defaultsVersion.Value < 8)
            {
                // v0.9.15 (Stu playtest): slower, bigger Thunderbolt; Open Circuit crown longer, cooldown after the crown.
                Migrate(storm, "Thunderbolt telegraph", 0.35f, 0.6f, v => KitTuning.ThunderboltTelegraphSeconds = v);
                Migrate(circuit, "Duration", 8f, 10f, v => KitTuning.OpenCircuitBuffSeconds = v);
                Migrate(circuit, "Cooldown", 12f, 8f, v => KitTuning.OpenCircuitCooldown = v);
            }
            if (defaultsVersion.Value < 9)
            {
                // v0.9.16 (Stu playtest): Thunderbolts a little more often.
                MigrateInt(storm, "Charges per Thunderbolt", 6, 5, v => KitTuning.StormChargeMax = v);
            }
            if (defaultsVersion.Value < 10)
            {
                // 1.1.0: "Spear in left hand" (bool, default true) became "Spear hand" (Auto/Left/Right).
                // The old default maps to the new default Auto; a player who turned it off keeps Right.
                var oldHand = new ConfigDefinition(spear, "Spear in left hand");
                var old = c.Bind(oldHand, true);
                if (!old.Value) SpearHand.Value = SpearDischarge.SpearHand.Right;
                c.Remove(oldHand);
                c.Save();
            }
            if (defaultsVersion.Value < 11)
            {
                // Primary damage pass: preserve custom values, including those close to 1.0.
                Migrate(bolt, "Damage", 1.0f, 1.2f, v => KitTuning.ArcBoltDamageCoefficient = v, matchExactly: true);
            }
            if (defaultsVersion.Value < 12)
            {
                // Stormspear damage pass: burst inherits the curve; preserve every custom coefficient.
                Migrate(spear, "Tap damage", 4f, 3.5f, v => Stormspear.StormspearTuning.TapDamage = v, matchExactly: true);
                Migrate(spear, "Full damage", 16f, 14f, v => Stormspear.StormspearTuning.FullDamage = v, matchExactly: true);
            }
            if (defaultsVersion.Value < 13)
            {
                // Reliability pass: migrate only the exact prior default; preserve custom speeds.
                Migrate(bolt, "Projectile speed", 80f, 120f, v => KitTuning.ArcBoltProjectileSpeed = v, matchExactly: true);
            }
            if (defaultsVersion.Value < CurrentDefaultsVersion) defaultsVersion.Value = CurrentDefaultsVersion;
            KitDescriptions.Refresh();

            B(c, misc, "Verbose log", HsLog.Verbose, v => HsLog.Verbose = v, "Write Hollow Saint's load diagnostics to the BepInEx log and show the build tag. For bug reports; off keeps the log clean.");
            EventLog = c.Bind(misc, "Event log", false, "Log the first few occurrences of each gameplay event (HOLLOW_SAINT_EVENT) for playtesting.");
            Bools.Add(EventLog);
            // v0.9.11 (release hygiene): the event log defaults off. Bound after the migrations above, so
            // it is migrated here; an untouched true from an older config moves to false.
            var logVersion = c.Bind(misc, "Event log defaults version", 0, "Internal. Do not edit.");
            if (logVersion.Value < 1) { if (EventLog.Value) EventLog.Value = false; logVersion.Value = 1; }
        }

        private const int CurrentDefaultsVersion = 13;

        private static void MigrateBool(string section, string key, bool oldDefault, bool newDefault, Action<bool> set)
        {
            foreach (var b in Bools)
            {
                if (b.Definition.Section != section || b.Definition.Key != key) continue;
                if (b.Value == oldDefault) { b.Value = newDefault; set(newDefault); }
                return;
            }
        }

        private static void MigrateInt(string section, string key, int oldDefault, int newDefault, Action<int> set)
        {
            foreach (var o in Ints)
            {
                if (o.Entry.Definition.Section != section || o.Entry.Definition.Key != key) continue;
                if (o.Entry.Value == oldDefault) { o.Entry.Value = newDefault; set(newDefault); }
                return;
            }
        }

        private static void Migrate(string section, string key, float oldDefault, float newDefault, Action<float> set, bool matchExactly = false)
        {
            foreach (var o in Floats)
            {
                if (o.Entry.Definition.Section != section || o.Entry.Definition.Key != key) continue;
                if (matchExactly ? o.Entry.Value == oldDefault : Math.Abs(o.Entry.Value - oldDefault) < 0.001f)
                { o.Entry.Value = newDefault; set(newDefault); }
                return;
            }
        }

        private static void F(ConfigFile c, string section, string key, float def, Action<float> set,
            float min, float max, float stepSize, string description, bool restart = false)
        {
            var entry = c.Bind(section, key, def, description);
            set(entry.Value);
            entry.SettingChanged += (s, e) => { set(entry.Value); KitDescriptions.Refresh(); };
            Floats.Add(new FloatOption { Entry = entry, Min = min, Max = max, Step = stepSize, Restart = restart });
        }

        private static void I(ConfigFile c, string section, string key, int def, Action<int> set,
            int min, int max, string description, bool restart = false)
        {
            var entry = c.Bind(section, key, def, description);
            set(entry.Value);
            entry.SettingChanged += (s, e) => { set(entry.Value); KitDescriptions.Refresh(); };
            Ints.Add(new IntOption { Entry = entry, Min = min, Max = max, Restart = restart });
        }

        private static void B(ConfigFile c, string section, string key, bool def, Action<bool> set, string description)
        {
            var entry = c.Bind(section, key, def, description);
            set(entry.Value);
            entry.SettingChanged += (s, e) => set(entry.Value);
            Bools.Add(entry);
        }

        /// <summary>Adds the options page when Risk of Options is loaded. Kept in a separate
        /// method so the RiskOfOptions types are only touched when the mod is present.</summary>
        internal static void TryRegisterOptionsMenu()
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions")) return;
            try { RegisterOptionsMenu(); }
            catch (Exception error) { Plugin.Log.LogWarning("Risk of Options page failed: " + error.Message); }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RegisterOptionsMenu()
        {
            RiskOfOptions.ModSettingsManager.SetModDescription("Hollow Saint balance and debug settings. Values marked restart apply on the next launch.");
            var icon = KitIcons.Sprite("portrait");
            if (icon) RiskOfOptions.ModSettingsManager.SetModIcon(icon);
            foreach (var f in Floats)
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.StepSliderOption(f.Entry,
                    new RiskOfOptions.OptionConfigs.StepSliderConfig { min = f.Min, max = f.Max, increment = f.Step, restartRequired = f.Restart }));
            foreach (var i in Ints)
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.IntSliderOption(i.Entry,
                    new RiskOfOptions.OptionConfigs.IntSliderConfig { min = i.Min, max = i.Max, restartRequired = i.Restart }));
            foreach (var b in Bools)
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(b));
            if (SpearHand != null)
            {
                // Own tokens (HollowSaint.language) so the name, description and choices are translated;
                // Risk of Options would register fixed English tokens for them otherwise.
                var hand = new RiskOfOptions.Options.ChoiceOption(SpearHand);
                RiskOfOptions.ModSettingsManager.AddOption(hand, Plugin.Guid, "Hollow Saint", "HS_OPTION_SPEAR_HAND_NAME", "HS_OPTION_SPEAR_HAND_DESC");
                var tokens = typeof(RiskOfOptions.Options.ChoiceOption).GetField("_nameTokens", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (tokens != null && tokens.GetValue(hand) is string[] names && names.Length == 3)
                    tokens.SetValue(hand, new[] { "HS_OPTION_SPEAR_HAND_AUTO", "HS_OPTION_SPEAR_HAND_LEFT", "HS_OPTION_SPEAR_HAND_RIGHT" });
            }
        }
    }
}
