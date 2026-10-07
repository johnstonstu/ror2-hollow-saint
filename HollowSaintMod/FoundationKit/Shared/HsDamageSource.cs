using System;
using System.Collections.Generic;
using EntityStates;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit
{

    /// <summary>Which Hollow Saint skill produced a hit. Only our own code passes these.</summary>
    public enum HsDamageSource
    {
        None = 0,
        ArcBolt = 1,
        Stormspear = 2,
        OpenCircuit = 3
    }
}
