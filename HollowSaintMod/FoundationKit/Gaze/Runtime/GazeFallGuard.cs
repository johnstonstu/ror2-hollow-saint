using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>
    /// No fall damage from the Gaze hover until the Saint lands. Held on every machine (the
    /// server applies fall damage, the authority reports it). Released after the beam once the
    /// motor has been grounded briefly, or after a timeout. A body that already ignored fall
    /// damage keeps the flag.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GazeFallGuard : MonoBehaviour
    {
        private const float GroundedSeconds = 0.25f;
        private const float TimeoutSeconds = 8f;

        private CharacterBody body;
        private bool holding, releasing, hadFlag;
        private float releaseAge, groundedAge;

        public static void Hold(CharacterBody body)
        {
            if (!body) return;
            var guard = body.GetComponent<GazeFallGuard>();
            if (!guard) guard = body.gameObject.AddComponent<GazeFallGuard>();
            guard.body = body;
            if (!guard.holding)
            {
                guard.hadFlag = (body.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage) != 0;
                body.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            }
            guard.holding = true;
            guard.releasing = false;
        }

        public static void Release(CharacterBody body)
        {
            var guard = body ? body.GetComponent<GazeFallGuard>() : null;
            if (!guard || !guard.holding) return;
            guard.releasing = true;
            guard.releaseAge = 0f;
            guard.groundedAge = 0f;
        }

        private void FixedUpdate()
        {
            if (!holding || !releasing || !body) return;
            releaseAge += Time.fixedDeltaTime;
            bool grounded = body.characterMotor && body.characterMotor.isGrounded;
            groundedAge = grounded ? groundedAge + Time.fixedDeltaTime : 0f;
            if (groundedAge < GroundedSeconds && releaseAge < TimeoutSeconds) return;
            holding = releasing = false;
            if (!hadFlag) body.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
        }
    }
}
