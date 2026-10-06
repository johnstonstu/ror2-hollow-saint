using RoR2;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>Read the owning player's configured UI-cancel action only while
    /// this Gaze state is active. No hardware key, map mutation or global UI hook.</summary>
    internal sealed class GazeMappedCancel
    {
        private bool held;
        internal void Begin(CharacterBody body)
        {
            held = !TryRead(body, out bool down) || down;
        }
        internal bool Observe(CharacterBody body)
        {
            if (!TryRead(body, out bool down)) { held = true; return false; }
            bool edge = down && !held;
            held = down;
            return edge;
        }
        private static bool TryRead(CharacterBody body, out bool down)
        {
            down = false;
            if (!body || !body.hasEffectiveAuthority || !body.healthComponent || !body.healthComponent.alive || !body.master) return false;
            var controller = body.master.playerCharacterMasterController;
            var networkUser = controller ? controller.networkUser : null;
            var local = networkUser ? networkUser.localUser : null;
            var camera = networkUser ? networkUser.cameraRigController : null;
            if (local == null || local.isUIFocused || !camera || !camera.isControlAllowed || local.inputPlayer == null) return false;
            down = local.inputPlayer.GetButton(RewiredConsts.Action.UICancel);
            return true;
        }
    }
}
