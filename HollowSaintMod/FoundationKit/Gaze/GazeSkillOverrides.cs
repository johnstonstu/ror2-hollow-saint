using RoR2;
using RoR2.Skills;
using HollowSaint.FoundationKit.Stormspear;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>Own-source contextual overrides use the native HUD/activation path.
    /// Vanilla keeps separate override banks, but reassigning an original SkillDef may
    /// refill it. Teardown restores its actual bank after that native assignment.</summary>
    [DisallowMultipleComponent]
    internal sealed class GazeSkillOverrides : MonoBehaviour
    {
        private const GenericSkill.SkillOverridePriority Priority = GenericSkill.SkillOverridePriority.Contextual;
        private readonly Slot[] slots = new Slot[4];
        private readonly GazePrimaryTapGate taps = new GazePrimaryTapGate();
        private CharacterBody body;
        private GazeState state;
        private GazeFuelController fuel;
        private bool secondaryDeferred;
        internal bool Active { get; private set; }
        internal int Capacity => fuel ? fuel.EntryCapacity : 2;
        internal bool PulseReady => Active && body && body.healthComponent && body.healthComponent.alive &&
            state != null && state.PrimaryPulseReady && fuel && fuel.AvailableEntry > 0 && fuel.PulseRequestReady;
        internal bool CanExecutePrimary
        {
            get
            {
                if (!Active || !body || !body.inputBank) return false;
                // Native HandleSkill retries held, unclaimed mustKeyPress inputs.
                // Observe even when unavailable so that hold cannot become a later tap.
                return taps.Observe(body.inputBank.skill1.down, PulseReady && body.hasEffectiveAuthority);
            }
        }

        private sealed class Slot
        {
            internal GenericSkill skill;
            internal SkillDef original, replacement;
            internal SkillDef.BaseSkillInstanceData data;
            internal EntityStateMachine machine;
            internal int stock;
            internal float stopwatch;
            internal bool wasBase;
            internal bool selectedAtInstall;
        }

        internal static GazeSkillOverrides For(CharacterBody body)
        {
            if (!body) return null;
            return body.GetComponent<GazeSkillOverrides>() ?? body.gameObject.AddComponent<GazeSkillOverrides>();
        }
        private void Awake() { body = GetComponent<CharacterBody>(); }

        internal void Begin(GazeState activeState, GazeFuelController resource)
        {
            End();
            if (!body || !body.skillLocator) return;
            state = activeState; fuel = resource; Active = true;
            taps.Begin(body.inputBank && body.inputBank.skill1.down);
            var locator = body.skillLocator;
            try
            {
                Install(0, locator.primary, GazeChannelSkillDefs.Pulse);
                secondaryDeferred = SpearFinishing(locator.secondary);
                if (!secondaryDeferred) Install(1, locator.secondary, GazeChannelSkillDefs.Locked);
                Install(2, locator.utility, GazeChannelSkillDefs.Locked);
                Install(3, locator.special, GazeChannelSkillDefs.Locked);
            }
            catch (System.Exception error)
            {
                End();
                Plugin.Log.LogError("HOLLOW_SAINT_GAZE_SKILL_OVERRIDE_FAILED " + error);
                throw;
            }
        }
        private static bool SpearFinishing(GenericSkill skill)
        {
            var current = skill && skill.stateMachine ? skill.stateMachine.state : null;
            return current is StormspearChargeState || current is StormspearThrowState;
        }
        private void Install(int index, GenericSkill skill, SkillDef replacement)
        {
            if (!skill || !replacement) return;
            var slot = new Slot
            {
                skill = skill, original = skill.skillDef, data = skill.skillInstanceData,
                machine = skill.stateMachine, stock = skill.stock, stopwatch = skill.rechargeStopwatch,
                wasBase = skill.skillDef == skill.baseSkill, replacement = replacement
            };
            slots[index] = slot;
            skill.SetSkillOverride(this, replacement, Priority);
            slot.selectedAtInstall = skill.skillDef == replacement;
            if (slot.selectedAtInstall) RefreshStock(slot);
        }
        internal void ObservePrimary()
        {
            if (Active && body && body.inputBank)
                taps.Observe(body.inputBank.skill1.down, PulseReady && body.hasEffectiveAuthority);
        }
        internal void ExecutePrimary()
        {
            if (!Active || !body || !body.inputBank ||
                !taps.Take(body.inputBank.skill1.down, PulseReady && body.hasEffectiveAuthority)) return;
            fuel.RequestPulse();
        }
        internal bool OwnsPrimary => slots[0]?.skill && slots[0].skill.skillDef == GazeChannelSkillDefs.Pulse;

        internal void TickSlot(GenericSkill skill, float dt)
        {
            if (!Active) return;
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.skill != skill || skill.skillDef != slot.replacement) continue;
                // Preserve custom external override banks as-is. Their bespoke recharge
                // callbacks cannot safely run against another SkillDef's instance data.
                if (slot.wasBase && KnownKitDefinition(slot.original) && !CooldownHeld(slot)) skill.RechargeBaseSkill(dt);
                RefreshStock(slot);
                return;
            }
        }
        private static bool KnownKitDefinition(SkillDef def) => def &&
            (def.GetType() == typeof(SkillDef) || def.GetType() == typeof(StormspearSkillDef));
        private static bool CooldownHeld(Slot slot)
        {
            var original = slot.original;
            if (!original) return true;
            if (original.isCooldownBlockedUntilManuallyReset && slot.skill.isCooldownBlocked) return true;
            if (original is StormspearSkillDef)
            {
                var current = slot.machine ? slot.machine.state : null;
                var throwing = current as StormspearThrowState;
                if (StormspearCooldownPolicy.Pause(current is StormspearChargeState, throwing != null,
                    throwing != null && throwing.CooldownReleased)) return true;
            }
            return original.beginSkillCooldownOnSkillEnd && slot.machine && slot.machine.state != null &&
                slot.machine.state.GetType() == original.activationState.stateType;
        }
        private void RefreshStock(Slot slot)
        {
            bool pulse = slot.replacement == GazeChannelSkillDefs.Pulse;
            slot.skill.OverrideMaxStock(pulse ? Mathf.Max(2, Capacity) : 1);
            slot.skill.stock = pulse && fuel ? fuel.AvailableEntry : 0;
            slot.skill.rechargeStopwatch = 0f;
        }
        internal Sprite OriginalIcon(GenericSkill skill)
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null && slots[i].skill == skill) return slots[i].original ? slots[i].original.icon : null;
            return null;
        }

        internal void End()
        {
            Active = false;
            secondaryDeferred = false;
            state = null; fuel = null;
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i]; slots[i] = null;
                if (slot == null || !slot.skill) continue;
                var skill = slot.skill;
                int baseStock = skill.baseStock;
                float baseStopwatch = skill.baseRechargeStopwatch;
                skill.UnsetSkillOverride(this, slot.replacement, Priority);
                // A higher-priority external override remains entirely untouched.
                if (!slot.selectedAtInstall || skill.skillDef != slot.original) continue;
                skill.baseStock = slot.wasBase ? Mathf.Min(baseStock, skill.maxStock) : baseStock;
                skill.baseRechargeStopwatch = baseStopwatch;
                // Known kit definitions have benign assignment hooks. An unknown
                // mod may dispose its prior data in OnUnassigned; retain its freshly
                // assigned native data rather than resurrecting that old instance.
                if (KnownKitDefinition(slot.original)) skill.skillInstanceData = slot.data;
                if (!slot.wasBase)
                {
                    skill.stock = Mathf.Min(slot.stock, skill.maxStock);
                    skill.rechargeStopwatch = slot.stopwatch;
                }
            }
        }
        private void FixedUpdate()
        {
            if (!Active) return;
            var machine = EntityStateMachine.FindByCustomName(gameObject, KitRegistration.CrownMachineName);
            if (!body || !body.healthComponent || !body.healthComponent.alive || !machine || machine.state != state)
            {
                End();
                return;
            }
            // The native charge/failed-throw OnExit refunds via the live secondary
            // slot. Keep that original bank until the actual state has exited.
            if (secondaryDeferred && body.skillLocator && !SpearFinishing(body.skillLocator.secondary))
            {
                secondaryDeferred = false;
                Install(1, body.skillLocator.secondary, GazeChannelSkillDefs.Locked);
            }
        }
        private void OnDisable() { End(); }
        private void OnDestroy() { End(); }
    }
}
