using System;
using System.Collections;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Vfx
{

    /// <summary>v0.8 impact feel: a brief attacker hit-pause (visual only) and positional camera
    /// shake on the heavy hits. Runs where the beat renders, so every machine sees the same.</summary>
    internal static class ImpactFeelSettings
    {
        public static bool Enabled = true;
    }
}
