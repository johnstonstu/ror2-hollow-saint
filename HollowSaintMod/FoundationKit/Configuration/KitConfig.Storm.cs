using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace HollowSaint.FoundationKit
{
    public static partial class KitConfig
    {
        private static void BindStorm(ConfigFile c)
        {
            F(c, storm, "Static threshold", KitTuning.StaticThreshold, v => KitTuning.StaticThreshold = v, 0.05f, 1f, 0.05f, "Fraction of the target's max health a full-proc hit needs to deal to fill Static from 0 to 100%.");
            F(c, storm, "Static minimum per hit", KitTuning.StaticMinGain, v => KitTuning.StaticMinGain = v, 0.01f, 0.5f, 0.01f, "Minimum Static per full-proc hit, so bosses still build.");
            F(c, storm, "Static crit multiplier", KitTuning.StaticCritMultiplier, v => KitTuning.StaticCritMultiplier = v, 1f, 3f, 0.05f, "Static multiplier on critical hits.");
            F(c, storm, "Static Open Circuit weight", KitTuning.StaticOpenCircuitWeight, v => KitTuning.StaticOpenCircuitWeight = v, 0f, 1f, 0.05f, "Proc-coefficient equivalent used for Open Circuit pulses (they carry no proc).");
            F(c, storm, "Static decay delay", KitTuning.StaticDecayDelay, v => KitTuning.StaticDecayDelay = v, 0f, 10f, 0.25f, "Seconds after the last hit before Static starts to drain.");
            F(c, storm, "Static decay rate", KitTuning.StaticDecayPerSecond, v => KitTuning.StaticDecayPerSecond = v, 0.05f, 3f, 0.05f, "Fraction of full Static drained per second.");
            F(c, storm, "Electrocute stun", KitTuning.ElectrocuteStunSeconds, v => KitTuning.ElectrocuteStunSeconds = v, 0f, 5f, 0.25f, "Seconds of the Electrocute jolt (stun) on enemies that can be stunned (0 = none). Every Electrocuted enemy is also Shocked.");
            F(c, storm, "Shocked duration", KitTuning.ShockedSeconds, v => KitTuning.ShockedSeconds = v, 0.5f, 10f, 0.5f, "Seconds.");
            F(c, storm, "Shocked damage taken", KitTuning.ShockedDamageMultiplier, v => KitTuning.ShockedDamageMultiplier = v, 1f, 2f, 0.05f, "Multiplier on ALL damage a Shocked enemy takes.");
            F(c, storm, "Pop damage", KitTuning.ElectrocutePopDamageCoefficient, v => KitTuning.ElectrocutePopDamageCoefficient = v, 0.5f, 10f, 0.1f, "Damage coefficient of the Electrocute arc burst. Shared passive damage remains unchanged for every triggering skill.");
            F(c, storm, "Pop radius", KitTuning.ElectrocutePopRadius, v => KitTuning.ElectrocutePopRadius = v, 2f, 20f, 0.5f, "Metres.");
            I(c, storm, "Pop targets", KitTuning.ElectrocutePopTargets, v => KitTuning.ElectrocutePopTargets = v, 1, 10, "Enemies hit by one Electrocute burst.");
            F(c, storm, "Pop proc coefficient", KitTuning.ElectrocutePopProc, v => KitTuning.ElectrocutePopProc = v, 0f, 1f, 0.05f, "Proc coefficient of the burst.");
            F(c, storm, "Pop Static", KitTuning.ElectrocutePopStatic, v => KitTuning.ElectrocutePopStatic = v, 0f, 1f, 0.05f, "Static each burst target gains (cascade).");
            F(c, storm, "Electrocute immunity", KitTuning.ElectrocuteImmuneSeconds, v => KitTuning.ElectrocuteImmuneSeconds = v, 0f, 15f, 0.5f, "Seconds an Electrocuted enemy cannot build Static.");
            I(c, storm, "Electrocute cap per second", KitTuning.ElectrocutesPerSecondCap, v => KitTuning.ElectrocutesPerSecondCap = v, 1, 20, "Max Electrocutes per second per Saint (screen and performance guard).");
            F(c, storm, "Death discharge", KitTuning.DeathDischargeStatic, v => KitTuning.DeathDischargeStatic = v, 0f, 1f, 0.05f, "An enemy that dies holding at least this much Static (0.5 = half) Electrocutes as it dies: it lights an orb and arcs to its neighbours. 0 turns it off.");
            I(c, storm, "Charges per Thunderbolt", KitTuning.StormChargeMax, v => KitTuning.StormChargeMax = v, 2, 20, "Stored Static Charge capacity. A full bank empowers the next successful spear throw; Gaze claims entry charges. Charges never discharge automatically.");
            F(c, storm, "Thunderbolt damage", KitTuning.ThunderboltDamageCoefficient, v => KitTuning.ThunderboltDamageCoefficient = v, 1f, 30f, 0.5f, "Raw strike coefficient; can crit. Ordinary full-charge spear Prayer uses 38.25% with no item procs; funded full-bank Prayer uses 76.5% with proc 1.0. Splash inherits once.");
            F(c, storm, "Thunderbolt splash", KitTuning.ThunderboltSplashFraction, v => KitTuning.ThunderboltSplashFraction = v, 0f, 1f, 0.05f, "Fraction of the strike damage dealt to nearby enemies.");
            F(c, storm, "Thunderbolt splash radius", KitTuning.ThunderboltSplashRadius, v => KitTuning.ThunderboltSplashRadius = v, 1f, 10f, 0.5f, "Metres.");
            F(c, storm, "Thunderbolt range", KitTuning.ThunderboltRange, v => KitTuning.ThunderboltRange = v, 10f, 60f, 1f, "Legacy automatic-target range, retained for config compatibility. Stored charges no longer search for targets automatically.");
            F(c, storm, "Thunderbolt telegraph", KitTuning.ThunderboltTelegraphSeconds, v => KitTuning.ThunderboltTelegraphSeconds = v, 0.1f, 1.5f, 0.05f, "Legacy automatic-discharge setting, retained for compatibility. The bank-funded spear strike occurs on landing.");
            F(c, storm, "Thunderbolt flight", KitTuning.ThunderboltFlightSeconds, v => KitTuning.ThunderboltFlightSeconds = v, 0.2f, 2f, 0.05f, "Legacy automatic-discharge flight setting, retained for compatibility. Bank-funded strikes follow the spear's actual landing.");
            F(c, storm, "Thunderbolt cooldown", KitTuning.ThunderboltCooldown, v => KitTuning.ThunderboltCooldown = v, 0f, 20f, 0.5f, "Legacy automatic-discharge cooldown, retained for compatibility. Stored charges require an explicit ability use.");
        }

    }
}
