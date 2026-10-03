using EntityStates;
using RoR2.Skills;
using UnityEngine;

namespace HollowSaint.FoundationKit.Stormspear
{
    /// <summary>Stormspear (secondary) registration: states, projectile and SkillDef.</summary>
    public static class StormspearRegistration
    {
        public const string SkillName = "HollowSaintStormspear";
        public const string MachineName = "Spear";

        public static SkillDef SkillDef { get; private set; }

        public static SkillDef Register()
        {
            if (SkillDef != null) return SkillDef;

            var chargeType = KitContent.AddState(typeof(StormspearChargeState));
            KitContent.AddState(typeof(StormspearThrowState));
            if (KitContent.AddProjectile(StormspearProjectile.EnsurePrefab()) == null)
                Plugin.Log.LogError("Stormspear: no projectile prefab; the skill will charge but throw nothing.");

            var def = ScriptableObject.CreateInstance<SkillDef>();
            def.skillName = SkillName;
            ((ScriptableObject)def).name = SkillName;
            def.skillNameToken = KitTokens.ConduitSpearName;
            def.skillDescriptionToken = KitTokens.ConduitSpearDesc;
            def.icon = KitIcons.Sprite("skill_conduit_spear");
            def.keywordTokens = new[] { "KEYWORD_AGILE" };
            def.activationStateMachineName = MachineName;
            def.activationState = chargeType;
            def.interruptPriority = InterruptPriority.Skill;
            def.baseRechargeInterval = StormspearTuning.Cooldown;
            def.baseMaxStock = StormspearTuning.BaseStock;
            def.rechargeStock = 1;
            def.requiredStock = 1;
            def.stockToConsume = 1;
            def.mustKeyPress = true;
            def.resetCooldownTimerOnUse = false;
            def.beginSkillCooldownOnSkillEnd = false;
            def.canceledFromSprinting = false;
            def.cancelSprintingOnActivation = false;
            def.forceSprintDuringState = false;
            def.isCombatSkill = true;
            def.hideStockCount = false;
            def.hideCooldown = false;

            SkillDef = KitContent.AddSkillDef(def);
            Plugin.Log.LogInfo("Stormspear registered.");
            return SkillDef;
        }
    }
}
