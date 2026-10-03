using EntityStates;
using RoR2.Skills;
using UnityEngine;

namespace HollowSaint.FoundationKit.ArcBolt
{
    /// <summary>Arc Bolt (primary) registration: state, projectile and SkillDef.</summary>
    public static class ArcBoltRegistration
    {
        public const string SkillName = "HollowSaintArcBolt";

        public static SkillDef SkillDef { get; private set; }
        public static SerializableEntityStateType ArcBoltStateType { get; private set; }

        /// <summary>Idempotent. Returns the SkillDef.</summary>
        public static SkillDef RegisterArcBolt()
        {
            if (SkillDef != null) return SkillDef;

            ArcBoltStateType = KitContent.AddState(typeof(ArcBoltState));
            if (KitContent.AddProjectile(ArcBoltProjectile.EnsurePrefab()) == null)
                Plugin.Log.LogError("Arc Bolt: no projectile prefab; the skill will animate but fire nothing.");

            var def = ScriptableObject.CreateInstance<SkillDef>();
            def.skillName = SkillName;
            ((ScriptableObject)def).name = SkillName;
            def.skillNameToken = KitTokens.ArcBoltName;
            def.skillDescriptionToken = KitTokens.ArcBoltDesc;
            def.icon = KitIcons.Sprite("skill_arc_bolt");
            def.keywordTokens = new[] { KitTokens.KeywordStorm };
            def.activationStateMachineName = "Weapon";
            def.activationState = ArcBoltStateType;
            // Vanilla primary pattern (Commando FirePistol2): no cooldown; the state's own
            // duration sets the cadence and the skill only starts from an idle machine.
            // That makes the 0.5 s interval scale with attack speed, which a fixed
            // 0.5 s recharge would not.
            def.interruptPriority = InterruptPriority.Any;
            def.baseRechargeInterval = 0f;
            def.baseMaxStock = 1;
            def.rechargeStock = 1;
            def.requiredStock = 1;
            def.stockToConsume = 1;
            def.resetCooldownTimerOnUse = false;
            def.beginSkillCooldownOnSkillEnd = false;
            def.canceledFromSprinting = false;
            def.cancelSprintingOnActivation = true;
            def.forceSprintDuringState = false;
            def.isCombatSkill = true;
            def.mustKeyPress = false; // hold to auto-fire
            def.hideStockCount = true;
            def.hideCooldown = false;

            SkillDef = KitContent.AddSkillDef(def);
            Plugin.Log.LogInfo("Arc Bolt registered.");
            return SkillDef;
        }
    }
}
