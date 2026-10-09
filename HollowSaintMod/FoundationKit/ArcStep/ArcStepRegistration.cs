using EntityStates;
using RoR2.Skills;
using UnityEngine;

namespace HollowSaint.FoundationKit.ArcStep
{
    /// <summary>Arc Step (utility) registration. Runs on the body's "Body" state machine,
    /// like Commando's roll, so the dash owns movement for its duration.</summary>
    public static class ArcStepRegistration
    {
        public const string SkillDefName = "HollowSaintArcStep";
        public const string ActivationStateMachineName = "Body";

        public static SkillDef SkillDef { get; private set; }
        public static SerializableEntityStateType ArcStepStateType { get; private set; }

        public static SkillDef RegisterArcStep()
        {
            if (SkillDef != null) return SkillDef;
            ArcStepStateType = KitContent.AddState(typeof(ArcStepState));

            var def = ScriptableObject.CreateInstance<ChargedStorm.StoredChargeCompatibleSkillDef>();
            def.skillName = SkillDefName;
            ((ScriptableObject)def).name = SkillDefName;
            def.skillNameToken = KitTokens.ArcStepName;
            def.skillDescriptionToken = KitTokens.ArcStepDesc;
            def.keywordTokens = System.Array.Empty<string>();
            def.icon = KitIcons.Sprite("skill_arc_step");
            def.activationStateMachineName = ActivationStateMachineName;
            def.activationState = ArcStepStateType;
            def.interruptPriority = InterruptPriority.Skill;
            def.baseRechargeInterval = KitTuning.ArcStepRecharge; // 5 s per charge
            def.baseMaxStock = KitTuning.ArcStepMaxStock;        // 2 charges
            def.rechargeStock = 1;
            def.requiredStock = 1;
            def.stockToConsume = 1;
            def.resetCooldownTimerOnUse = false;
            def.beginSkillCooldownOnSkillEnd = false;
            def.fullRestockOnAssign = true;
            def.cancelSprintingOnActivation = false;
            def.forceSprintDuringState = false;
            def.canceledFromSprinting = false;
            def.isCombatSkill = false;
            def.mustKeyPress = true;
            def.hideStockCount = false;
            def.hideCooldown = false;

            SkillDef = KitContent.AddSkillDef(def);
            Plugin.Log.LogInfo("Arc Step registered.");
            return SkillDef;
        }
    }
}
