using EntityStates;
using RoR2.Skills;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>Gaze of the Hollow (alternate special) registration: states, effect and SkillDef.</summary>
    public static class GazeRegistration
    {
        public const string SkillName = "HollowSaintGazeOfTheHollow";
        public const string NameToken = "HS_SKILL_GAZE_NAME";
        public const string DescToken = "HS_SKILL_GAZE_DESC";

        public static SkillDef SkillDef { get; private set; }

        public static SkillDef Register()
        {
            if (SkillDef != null) return SkillDef;

            var beamType = KitContent.AddState(typeof(GazeState));
            KitContent.AddState(typeof(GazeEndState));
            KitContent.AddState(typeof(GazeLockState));
            GazeArmor.Register();
            // Presentation must never be able to abort the content load.
            try { Fx.GazeEffect.Register(); }
            catch (System.Exception error) { Plugin.Log.LogError("HOLLOW_SAINT_GAZE_FX_REGISTER_FAILED: " + error); }

            var def = ScriptableObject.CreateInstance<SkillDef>();
            def.skillName = SkillName;
            ((ScriptableObject)def).name = SkillName;
            def.skillNameToken = NameToken;
            def.skillDescriptionToken = DescToken;
            def.icon = KitIcons.Sprite("skill_gaze");
            def.keywordTokens = System.Array.Empty<string>();
            def.activationStateMachineName = KitRegistration.CrownMachineName;
            def.activationState = beamType;
            def.interruptPriority = InterruptPriority.PrioritySkill;
            def.baseRechargeInterval = GazeTuning.Cooldown;
            def.baseMaxStock = 1;
            def.rechargeStock = 1;
            def.requiredStock = 1;
            def.stockToConsume = 1;
            def.resetCooldownTimerOnUse = false;
            def.beginSkillCooldownOnSkillEnd = true;
            def.cancelSprintingOnActivation = true;
            def.forceSprintDuringState = false;
            def.canceledFromSprinting = false;
            def.isCombatSkill = true;
            def.mustKeyPress = true;
            def.hideStockCount = true;
            def.hideCooldown = false;

            SkillDef = KitContent.AddSkillDef(def);
            Plugin.Log.LogInfo("Gaze of the Hollow registered: cooldown=" + GazeTuning.Cooldown + "s beam=" +
                GazeTuning.BeamSeconds + "s dps=" + GazeTuning.DamagePerSecond);
            return SkillDef;
        }
    }
}
