using EntityStates;
using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze
{
    internal static class GazeChannelSkillDefs
    {
        internal static SkillDef Pulse { get; private set; }
        internal static SkillDef Locked { get; private set; }
        internal static void Register()
        {
            if (Pulse) return;
            var pulse = ScriptableObject.CreateInstance<GazePulseSkillDef>();
            Configure(pulse, "HollowSaintGazePrimaryPulse", "HS_SKILL_GAZE_PULSE_NAME", "HS_SKILL_GAZE_PULSE_DESC");
            pulse.baseMaxStock = 20;
            pulse.hideStockCount = false;
            pulse.icon = KitIcons.Sprite("skill_gaze");
            Pulse = KitContent.AddSkillDef(pulse);
            var locked = ScriptableObject.CreateInstance<GazeChannelLockedSkillDef>();
            Configure(locked, "HollowSaintGazeChannelLocked", "HS_SKILL_GAZE_LOCK_NAME", "HS_SKILL_GAZE_LOCK_DESC");
            locked.baseMaxStock = 1;
            locked.hideStockCount = true;
            Locked = KitContent.AddSkillDef(locked);
        }
        private static void Configure(SkillDef def, string name, string nameToken, string descToken)
        {
            def.skillName = name;
            ((ScriptableObject)def).name = name;
            def.skillNameToken = nameToken; def.skillDescriptionToken = descToken;
            def.keywordTokens = System.Array.Empty<string>();
            def.activationStateMachineName = KitRegistration.CrownMachineName;
            def.activationState = new SerializableEntityStateType(typeof(GazeLockState));
            def.interruptPriority = InterruptPriority.Any;
            def.baseRechargeInterval = 0f; def.rechargeStock = 0;
            def.requiredStock = 1; def.stockToConsume = 0;
            def.fullRestockOnAssign = false; def.dontAllowPastMaxStocks = true;
            def.resetCooldownTimerOnUse = false;
            def.cancelSprintingOnActivation = false; def.forceSprintDuringState = false;
            def.canceledFromSprinting = false; def.isCombatSkill = false;
            def.mustKeyPress = true; def.suppressSkillActivation = true;
            // hideCooldown=true forces the vanilla HUD to appear ready despite IsReady.
            def.hideCooldown = false;
        }
    }

    internal sealed class GazeChannelSkillData : SkillDef.BaseSkillInstanceData
    {
        internal GazeSkillOverrides controls;
    }

    internal sealed class GazePulseSkillDef : SkillDef
    {
        public override BaseSkillInstanceData OnAssigned(GenericSkill slot) => new GazeChannelSkillData
            { controls = slot.characterBody ? slot.characterBody.GetComponent<GazeSkillOverrides>() : null };
        private static GazeSkillOverrides Controls(GenericSkill slot) => (slot.skillInstanceData as GazeChannelSkillData)?.controls;
        public override bool IsReady(GenericSkill slot) => Controls(slot)?.PulseReady == true;
        public override bool CanExecute(GenericSkill slot) => Controls(slot)?.CanExecutePrimary == true;
        public override void OnExecute(GenericSkill slot) { Controls(slot)?.ExecutePrimary(); }
        public override void OnFixedUpdate(GenericSkill slot, float dt) { Controls(slot)?.TickSlot(slot, dt); }
        public override int GetMaxStock(GenericSkill slot) => Mathf.Max(2, Controls(slot)?.Capacity ?? 2);
    }

    internal sealed class GazeChannelLockedSkillDef : SkillDef
    {
        public override BaseSkillInstanceData OnAssigned(GenericSkill slot) => new GazeChannelSkillData
            { controls = slot.characterBody ? slot.characterBody.GetComponent<GazeSkillOverrides>() : null };
        private static GazeSkillOverrides Controls(GenericSkill slot) => (slot.skillInstanceData as GazeChannelSkillData)?.controls;
        public override bool IsReady(GenericSkill slot) => false;
        public override bool CanExecute(GenericSkill slot) => false;
        public override void OnExecute(GenericSkill slot) { }
        public override void OnFixedUpdate(GenericSkill slot, float dt) { Controls(slot)?.TickSlot(slot, dt); }
        public override Sprite GetCurrentIcon(GenericSkill slot) => Controls(slot)?.OriginalIcon(slot);
    }
}
