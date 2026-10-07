using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze
{
    internal enum GazeExitAction { None, Special, Utility }

    internal sealed class GazeExitEdges
    {
        private bool specialHeld, utilityHeld;
        internal void Begin(bool special, bool utility) { specialHeld = special; utilityHeld = utility; }
        internal GazeExitAction Observe(bool special, bool utility)
        {
            bool specialEdge = special && !specialHeld, utilityEdge = utility && !utilityHeld;
            specialHeld = special; utilityHeld = utility;
            return utilityEdge ? GazeExitAction.Utility : specialEdge ? GazeExitAction.Special : GazeExitAction.None;
        }
    }

    /// <summary>Wait for native state/slot teardown before one normal equipped Utility attempt.
    /// No synthetic stock, direct state construction or cooldown edits.</summary>
    [DisallowMultipleComponent]
    internal sealed class GazeUtilityExit : MonoBehaviour
    {
        private CharacterBody body;
        private GenericSkill utility;
        private SkillDef definition;
        private float expires;
        private bool pending;
        internal static void Queue(CharacterBody body)
        {
            if (!body || !body.hasEffectiveAuthority || !body.skillLocator) return;
            var driver = body.GetComponent<GazeUtilityExit>() ?? body.gameObject.AddComponent<GazeUtilityExit>();
            driver.body = body;
            driver.utility = body.skillLocator.utility;
            driver.definition = driver.utility ? driver.utility.skillDef : null;
            driver.expires = Time.unscaledTime + .5f;
            driver.pending = driver.utility && driver.definition;
        }
        private void FixedUpdate()
        {
            if (!pending) return;
            if (!body || !body.hasEffectiveAuthority || !body.healthComponent || !body.healthComponent.alive ||
                !utility || !body.skillLocator || body.skillLocator.utility != utility || utility.skillDef != definition ||
                Time.unscaledTime > expires) { pending = false; return; }
            var crown = EntityStateMachine.FindByCustomName(body.gameObject, KitRegistration.CrownMachineName);
            if (crown && crown.state is GazeState) return;
            if (utility.stateMachine && utility.stateMachine.state is GazeLockState) return;
            pending = false;
            // Native CanExecute/OnExecute owns stock, cooldown, sprint and selected state.
            utility.ExecuteIfReady();
        }
        private void OnDisable() { pending = false; }
        private void OnDestroy() { pending = false; }
    }
}
