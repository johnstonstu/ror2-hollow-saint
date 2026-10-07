using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace HollowSaint.FoundationKit
{
    public static partial class KitConfig
    {
        private static void BindStormspear(ConfigFile c)
        {
            F(c, spear, "Charge seconds", Stormspear.StormspearTuning.ChargeSeconds, v => Stormspear.StormspearTuning.ChargeSeconds = v, 0.4f, 6f, 0.1f, "Seconds from tap to full charge at 1x attack speed. Attack speed shortens it.");
            F(c, spear, "Aim assistance angle", Stormspear.StormspearTuning.AssistConeDegrees, v => Stormspear.StormspearTuning.AssistConeDegrees = v, 0f, 6f, 0.5f, "Degrees either side of the launch direction that can acquire one visible enemy for gentle homing. 0 disables assistance; applies to new hand and crown throws.");
            F(c, spear, "Crown charge multiplier", Stormspear.StormspearTuning.CrownChargeMultiplier, v => Stormspear.StormspearTuning.CrownChargeMultiplier = v, 1f, 6f, 0.1f, "How much faster the spear charges while Open Circuit is up.");
            SpearHand = c.Bind(spear, "Spear hand", SpearDischarge.SpearHand.Auto, "Which hand holds and throws the hand spear. Auto follows your input device: left hand on a controller (left trigger), right hand on mouse and keyboard (right-click). Arc Bolt fires from the other hand while the spear charges. The hand only changes between throws.");
            SpearDischarge.SpearCarry.HandMode = SpearHand.Value;
            SpearHand.SettingChanged += (s, e) => SpearDischarge.SpearCarry.HandMode = SpearHand.Value;
            F(c, spear, "Off-hand Arc Bolt rate", Stormspear.StormspearTuning.OffHandRateMultiplier, v => Stormspear.StormspearTuning.OffHandRateMultiplier = v, 0.1f, 1f, 0.05f, "Legacy setting retained for existing configs; no longer changes firing. Arc Bolt pauses during normal spear actions and fires at its normal rate during Open Circuit.");
            F(c, spear, "Minimum throw interval", Stormspear.StormspearTuning.MinThrowInterval, v => Stormspear.StormspearTuning.MinThrowInterval = v, 0.05f, 1f, 0.05f, "Seconds between tap throws at 1x attack speed when dumping stocks.");
            F(c, spear, "Tap damage", Stormspear.StormspearTuning.TapDamage, v => Stormspear.StormspearTuning.TapDamage = v, 0.5f, 12f, 0.1f, "Damage coefficient of an uncharged throw. Stored raw; effective native damage is 90% of this value. Inherited splash/chain fractions are applied afterward.");
            F(c, spear, "Full damage", Stormspear.StormspearTuning.FullDamage, v => Stormspear.StormspearTuning.FullDamage = v, 1f, 30f, 0.5f, "Stored raw full-charge coefficient. Prototype effective damage is 81% of this value: native 90% times a charge-weighted reduction up to another 10%. Burst inherits once.");
            F(c, spear, "Cooldown", Stormspear.StormspearTuning.Cooldown, v => Stormspear.StormspearTuning.Cooldown = v, 1f, 15f, 0.5f, "Raw seconds per stock (restart). Prototype recharge is 120% of this value; default 5 becomes 6 seconds.", restart: true);
            F(c, spear, "Burst radius (tap)", Stormspear.StormspearTuning.BurstRadiusTap, v => Stormspear.StormspearTuning.BurstRadiusTap = v, 1f, 15f, 0.5f, "Metres, uncharged throw.");
            F(c, spear, "Burst radius (full)", Stormspear.StormspearTuning.BurstRadiusFull, v => Stormspear.StormspearTuning.BurstRadiusFull = v, 1f, 25f, 0.5f, "Metres, fully charged throw.");
            F(c, spear, "Burst damage", Stormspear.StormspearTuning.BurstDamageFraction, v => Stormspear.StormspearTuning.BurstDamageFraction = v, 0f, 2f, 0.05f, "Near-impact fraction of direct damage, uncharged. Requires world line of sight and falls to half at the burst edge; excludes the direct victim.");
            F(c, spear, "Burst damage (full)", Stormspear.StormspearTuning.BurstDamageFractionFull, v => Stormspear.StormspearTuning.BurstDamageFractionFull = v, 0f, 2f, 0.05f, "Near-impact fraction of direct damage, fully charged. Requires world line of sight and falls to half at the burst edge; excludes the direct victim.");
            F(c, spear, "Stick seconds", Stormspear.StormspearTuning.StickSeconds, v => Stormspear.StormspearTuning.StickSeconds = v, 0f, 1f, 0.05f, "How long the spear stays lodged in what it hit before it bursts.");
            F(c, spear, "Ground burst", Stormspear.StormspearTuning.GroundBurstScale, v => Stormspear.StormspearTuning.GroundBurstScale = v, 0f, 1f, 0.05f, "Damage and radius of the burst when the spear hits terrain instead of an enemy (1 = same as an enemy hit).");
            F(c, spear, "Burst proc coefficient", Stormspear.StormspearTuning.BurstProcCoefficient, v => Stormspear.StormspearTuning.BurstProcCoefficient = v, 0f, 1f, 0.05f, "Item proc coefficient of the burst (and so how much Static it builds).");
        }

    }
}
