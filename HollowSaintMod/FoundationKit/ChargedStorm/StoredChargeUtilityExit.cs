using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    public sealed class StoredChargeUtilityExit : MonoBehaviour
    {
        private CharacterBody body;
        private GenericSkill utility;
        private RoR2.Skills.SkillDef definition;
        private bool pending;
        private float expires;
        internal static void Queue(CharacterBody body)
        {
            if (!body || !body.hasEffectiveAuthority || !body.skillLocator || !body.skillLocator.utility) return;
            var exit = body.GetComponent<StoredChargeUtilityExit>() ?? body.gameObject.AddComponent<StoredChargeUtilityExit>();
            exit.body = body; exit.utility = body.skillLocator.utility; exit.definition = exit.utility.skillDef;
            exit.pending = true; exit.expires = Time.unscaledTime + 4f;
        }
        private void FixedUpdate()
        {
            if (!pending) return;
            if (!body || !body.hasEffectiveAuthority || !body.healthComponent || !body.healthComponent.alive ||
                !utility || utility.skillDef != definition || Time.unscaledTime > expires) { pending = false; return; }
            if (StoredChargeState.IsGathering(body)) return;
            pending = false; utility.ExecuteIfReady();
        }
        private void OnDisable() { pending = false; }
    }
}
