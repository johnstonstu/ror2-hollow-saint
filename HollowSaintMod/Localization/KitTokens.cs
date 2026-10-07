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

    /// <summary>Language token keys. The text is in Language/HollowSaint.language.
    /// Description tokens are templates; KitDescriptions fills the numbers.</summary>
    public static class KitTokens
    {
        public const string Name = "HS_NAME";
        public const string Subtitle = "HS_SUBTITLE";

        public const string ArcBoltName = "HS_SKILL_ARCBOLT_NAME";
        public const string ArcBoltDesc = "HS_SKILL_ARCBOLT_DESC";
        public const string ConduitSpearName = "HS_SKILL_SPEAR_NAME";
        public const string ConduitSpearDesc = "HS_SKILL_SPEAR_DESC";
        public const string ArcStepName = "HS_SKILL_ARCSTEP_NAME";
        public const string ArcStepDesc = "HS_SKILL_ARCSTEP_DESC";
        public const string OpenCircuitName = "HS_SKILL_CIRCUIT_NAME";
        public const string OpenCircuitDesc = "HS_SKILL_CIRCUIT_DESC";
        public const string StormName = "HS_PASSIVE_STORM_NAME";
        public const string StormDesc = "HS_PASSIVE_STORM_DESC";
        public const string KeywordStatic = "HS_KEYWORD_STATIC";
        public const string KeywordStorm = "HS_KEYWORD_STORM";
        public const string KeywordElectrocute = "HS_KEYWORD_ELECTROCUTE";
        public const string KeywordShocked = "HS_KEYWORD_SHOCKED";
    }
}
