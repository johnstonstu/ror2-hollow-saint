using EntityStates;
using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace HollowSaint.FoundationKit.OpenCircuit
{
    /// <summary>Open Circuit (special) registration: state, buff and SkillDef.</summary>
    public static class OpenCircuitRegistration
    {
        public const string SkillDefName = "HollowSaintOpenCircuit";

        public static SkillDef SkillDef { get; private set; }
        public static SerializableEntityStateType OpenCircuitStateType { get; private set; }

        public static SkillDef RegisterOpenCircuit()
        {
            if (SkillDef != null) return SkillDef;
            OpenCircuitStateType = KitContent.AddState(typeof(OpenCircuitState));
            OpenCircuitBuff.Register();
            ClosedCircuitDriver.Install();

            var def = ScriptableObject.CreateInstance<ChargedStorm.StoredChargeSkillDef>();
            def.skillName = SkillDefName;
            ((ScriptableObject)def).name = SkillDefName;
            def.skillNameToken = KitTokens.OpenCircuitName;
            def.skillDescriptionToken = KitTokens.OpenCircuitDesc;
            def.icon = KitIcons.Sprite("skill_open_circuit");
            def.keywordTokens = new[] { "KEYWORD_AGILE" };
            def.activationStateMachineName = "Crown"; // its own machine; see KitRegistration.AddCrownMachine
            def.activationState = OpenCircuitStateType;
            def.interruptPriority = InterruptPriority.PrioritySkill;
            def.baseRechargeInterval = KitTuning.OpenCircuitCooldown; // 8 s after the crown closes (OpenCircuitPulseDriver)
            def.baseMaxStock = 1;
            def.rechargeStock = 1;
            def.requiredStock = 1;
            def.stockToConsume = 1;
            def.resetCooldownTimerOnUse = false;
            // Gathering must not refill stock before the paid crown opens,
            // including with heavy cooldown reduction. The buff driver then
            // holds recharge at zero until the crown closes.
            def.beginSkillCooldownOnSkillEnd = true;
            def.cancelSprintingOnActivation = false;
            def.forceSprintDuringState = false;
            def.canceledFromSprinting = false;
            def.isCombatSkill = true;
            def.mustKeyPress = true;
            def.hideStockCount = true;
            def.hideCooldown = false;

            SkillDef = KitContent.AddSkillDef(def);
            Plugin.Log.LogInfo("Open Circuit registered: cooldown=" + KitTuning.OpenCircuitCooldown +
                "s window=" + KitTuning.OpenCircuitBuffSeconds + "s pulse=" + KitTuning.OpenCircuitPulseInterval +
                "s radius=" + KitTuning.OpenCircuitRadius + "m");
            return SkillDef;
        }
    }
}
