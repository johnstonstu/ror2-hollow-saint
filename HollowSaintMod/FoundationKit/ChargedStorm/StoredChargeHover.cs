using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    internal sealed class StoredChargeHover
    {
        private bool held;
        private float targetY;
        private Vector3 back;
        internal void Begin(CharacterBody body, bool authority, Vector3 aim)
        {
            if (!body || !body.characterMotor) return;
            var motor = body.characterMotor;
            var gravity = motor.gravityParameters; gravity.channeledAntiGravityGranterCount++;
            motor.gravityParameters = gravity; held = true;
            Gaze.GazeFallGuard.Hold(body);
            targetY = body.footPosition.y + 3f;
            back = -Vector3.ProjectOnPlane(aim, Vector3.up).normalized;
            if (Physics.Raycast(body.corePosition, Vector3.up, out var roof, 5f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                targetY = Mathf.Min(targetY, body.footPosition.y + Mathf.Max(0f, roof.distance - 2f));
            if (authority && motor.Motor) motor.Motor.ForceUnground(.1f);
        }
        internal void Tick(CharacterBody body, bool authority, float age)
        {
            if (!held || !authority || !body || !body.characterMotor) return;
            var motor = body.characterMotor;
            Vector3 v = age < .4f ? back * 5f : Vector3.zero;
            if (age < .4f && Physics.Raycast(body.corePosition, back, .9f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                v = Vector3.zero;
            v.y = Mathf.Clamp((targetY - body.footPosition.y) * 6f, -4f, 10f);
            motor.velocity = v;
        }
        internal void Freeze(CharacterBody body)
        {
            if (!held || !body) return;
            targetY = body.footPosition.y; back = Vector3.zero;
            if (body.characterMotor) body.characterMotor.velocity = Vector3.zero;
        }
        internal void End(CharacterBody body)
        {
            if (!held) return; held = false;
            if (body && body.characterMotor)
            {
                var gravity = body.characterMotor.gravityParameters;
                gravity.channeledAntiGravityGranterCount = Mathf.Max(0, gravity.channeledAntiGravityGranterCount - 1);
                body.characterMotor.gravityParameters = gravity;
            }
            Gaze.GazeFallGuard.Release(body);
        }
    }
}
