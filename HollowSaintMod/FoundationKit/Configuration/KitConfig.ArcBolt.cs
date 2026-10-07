using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace HollowSaint.FoundationKit
{
    public static partial class KitConfig
    {
        private static void BindArcBolt(ConfigFile c)
        {
            F(c, bolt, "Damage", KitTuning.ArcBoltDamageCoefficient, v => KitTuning.ArcBoltDamageCoefficient = v, 0.2f, 4f, 0.05f, "Damage coefficient of the bolt. Stored raw; effective native damage is 90% of this value. Inherited splash/chain fractions are applied afterward.");
            F(c, bolt, "Fire interval", KitTuning.ArcBoltInterval, v => KitTuning.ArcBoltInterval = v, 0.2f, 1.5f, 0.05f, "Seconds per shot at 1x attack speed.");
            F(c, bolt, "Aim assistance angle", KitTuning.ArcBoltAssistConeDegrees, v => KitTuning.ArcBoltAssistConeDegrees = v, 0f, 6f, 0.5f, "Degrees either side of the launch direction that can acquire one visible enemy for gentle homing. 0 disables assistance; applies to newly fired bolts.");
            F(c, bolt, "Proc coefficient", KitTuning.ArcBoltProcCoefficient, v => KitTuning.ArcBoltProcCoefficient = v, 0f, 1f, 0.05f, "Item proc coefficient of the direct hit (restart).", restart: true);
            F(c, bolt, "Chain proc coefficient", KitTuning.ArcBoltChainProc, v => KitTuning.ArcBoltChainProc = v, 0f, 1f, 0.05f, "Proc coefficient of the first chain hop; each further hop halves it.");
            I(c, bolt, "Chain targets", KitTuning.ArcBoltMaxChainTargets, v => KitTuning.ArcBoltMaxChainTargets = v, 1, 10, "Enemies hit per bolt including the first.");
            F(c, bolt, "Chain range", KitTuning.ArcBoltChainRange, v => KitTuning.ArcBoltChainRange = v, 4f, 30f, 0.5f, "Metres between chain hops.");
            F(c, bolt, "Chain falloff", KitTuning.ArcBoltChainFalloff, v => KitTuning.ArcBoltChainFalloff = v, 0.3f, 1f, 0.05f, "Damage multiplier per hop.");
            F(c, bolt, "Projectile speed", KitTuning.ArcBoltProjectileSpeed, v => KitTuning.ArcBoltProjectileSpeed = v, 30f, 200f, 5f, "Metres per second (restart). Speeds above 80 shorten flight lifetime to preserve the previous maximum range.", restart: true);
        }

    }
}
