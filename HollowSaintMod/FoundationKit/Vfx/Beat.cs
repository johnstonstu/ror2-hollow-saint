using System;
using System.Collections;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>Every audiovisual moment in the kit. Each beat plays its VFX and SFX together.</summary>
    public enum Beat : uint
    {
        // Local: raised on every machine by the EntityState or a replicated buff edge.
        ArcBoltCast = 1,
        SpearThrow = 2,
        ArcStepStart = 3,
        ArcStepEnd = 4,
        CircuitUnfold = 5,
        CircuitOpen = 6,
        CircuitClose = 7,
        MeterFull = 8,
        StaticTier = 9,   // enemy Static crackle arc (StaticFx); also its throttled tick sound
        ChargeTick = 10,  // Saint's Storm charge went up

        // Networked: raised on the server, sent to every client through EffectManager.
        BoltImpact = 20,
        ChainHop = 21,
        SpearImpact = 22,
        CircuitPulse = 23,
        CircuitArc = 24,
        Electrocute = 27,
        ElectrocuteArc = 28,
        ThunderTelegraph = 29,
        ThunderStrike = 30,
        ThunderGather = 31,
        ThunderRelease = 32,
        ThunderCancel = 33,
        SpearPulse = 34,     // planted spear pulse: faint radius ring + crackle at the spear
        SpearPulseArc = 35,  // thin arc spear -> pulse target (also the despawn fizzle)
        SpearConduct = 36,   // Arc Bolt hit -> spear (spread feed)
        SpearSpread = 37,    // spear -> spread target
        SpearRecall = 38,    // spear flies back to the hand
        SpearStruck = 39,    // Arc Bolt struck the planted spear itself
        SpearBurst = 40,     // v0.9 Stormspear impact: AoE lightning spreading from origin; scale = burst radius (m)
        SpearStuck = 41,     // v0.9.10 spear lodged in an enemy or the ground; start = flight direction, scale = charge, float = seconds
        CircuitDwellZap = 42 // confirmed server dwell hit; owner and exact victim point, no independent damage
    }
}
