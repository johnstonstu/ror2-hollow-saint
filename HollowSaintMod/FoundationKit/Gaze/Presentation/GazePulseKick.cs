using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze
{
    internal sealed class GazePulseKick
    {
        private float last = float.NegativeInfinity;
        internal bool Play(CharacterBody body, int spent)
        {
            if (!ImpactFeelSettings.Enabled || !body || !body.healthComponent || !body.healthComponent.alive ||
                LocalUserManager.readOnlyLocalUsersList.Count != 1 || float.IsNaN(Time.unscaledTime) ||
                float.IsInfinity(Time.unscaledTime) || Time.unscaledTime - last < .24f) return false;
            var crown = EntityStateMachine.FindByCustomName(body.gameObject, KitRegistration.CrownMachineName);
            var state = crown ? crown.state as GazeState : null;
            if (state == null || !state.TimerVisible || !state.FuelAdmissionOpen) return false;
            var local = LocalUserManager.readOnlyLocalUsersList[0];
            var camera = local.cameraRigController;
            if (local.cachedBody != body || !camera || camera.targetBody != body) return false;
            last = Time.unscaledTime;
            // Native emitter feeds camera displacement through the game's shake scale.
            // Never touches inputBank, aim rays, camera rotation or persistent overrides.
            float amplitude = .10f + .025f * Mathf.Clamp(spent, 1, 5);
            ShakeEmitter.CreateSimpleShakeEmitter(camera.transform.position,
                new Wave { amplitude = amplitude, frequency = 20f, cycleOffset = 0f }, .12f, 1f, true);
            return true;
        }
    }
}
