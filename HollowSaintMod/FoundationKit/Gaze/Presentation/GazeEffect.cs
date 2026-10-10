using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using HollowSaint.FoundationKit.Vfx;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    /// <summary>
    /// Networked Gaze moments (core/splash contacts, fork hits and chain hops), raised on the server and played on
    /// every machine. Own effect prefab, so the shared Beat list stays untouched.
    /// EffectData: origin = target, start = where the lightning leaves, genericUInt = Kind,
    /// genericBool = sound, networkedObjectReference = channel owner. Hits render immediately.
    /// </summary>
    public sealed partial class GazeEffect : MonoBehaviour
    {
        public enum Kind : uint { Fork = 1, Chain = 2, Contact = 3, Focus = 4 }
        private static bool reportedSendFailure;

        public static GameObject Prefab { get; private set; }

        internal static void Register()
        {
            if (Prefab) return;
            var prefab = VfxAssets.NewPrefab("HollowSaintGazeEffect");
            var effect = prefab.AddComponent<EffectComponent>();
            effect.applyScale = false;
            effect.parentToReferencedTransform = false;
            effect.positionAtReferencedTransform = false;
            var vfx = prefab.AddComponent<VFXAttributes>();
            vfx.DoNotPool = true; // initialises in Start; pooled reuse would skip it
            vfx.vfxPriority = VFXAttributes.VFXPriority.Always;
            prefab.AddComponent<GazeEffect>();
            prefab.AddComponent<DestroyOnTimer>().duration = 0.1f;
            Prefab = prefab;
            KitContent.AddEffect(prefab);
        }

        public static void Server(Kind kind, Vector3 target, Vector3 from, CharacterBody owner, bool sound, float delay)
        {
            if (!NetworkServer.active || !Prefab || !owner) return;
            try
            {
                var data = new EffectData
                {
                    origin = target,
                    start = from,
                    genericUInt = (uint)kind,
                    genericBool = sound,
                    genericFloat = Mathf.Clamp(delay, 0f, 1f),
                    color = SkinFxPalette.ForBody(owner).NetworkColor
                };
                data.SetNetworkedObjectReference(owner.gameObject);
                EffectManager.SpawnEffect(Prefab, data, true);
            }
            catch (System.Exception error)
            {
                // A cosmetic send failure must not stop the rest of the damage tick.
                if (!reportedSendFailure) Plugin.Log.LogWarning("HOLLOW_SAINT_GAZE_FX_SEND_FAILED " + error);
                reportedSendFailure = true;
            }
        }

        private void Start()
        {
            var component = GetComponent<EffectComponent>();
            var data = component ? component.effectData : null;
            if (data == null) return;
            try
            {
                var owner = data.ResolveNetworkedObjectReference();
                if ((Kind)data.genericUInt == Kind.Focus) { FocusCue(data, owner); return; }
                var tendrils = owner ? owner.GetComponent<GazeTendrils>() : null;
                // An ended/destroyed channel never resurrects from a late hit packet.
                if (!tendrils) return;
                bool shown = tendrils.Confirm(data.start, data.origin, (Kind)data.genericUInt == Kind.Fork);
                if (shown && (Kind)data.genericUInt == Kind.Fork && data.genericBool)
                    Util.PlaySound(GazeSfx.ForkHit, gameObject);
            }
            catch (System.Exception error)
            {
                Plugin.Log.LogError("HOLLOW_SAINT_GAZE_FX_ERROR " + error);
            }
        }

    }

    public sealed partial class GazeEffect
    {
        /// <summary>1.3.1: focus step reached on a target (1-5): a small flare and a chime one step higher;
        /// full focus adds a heavier strike so the ramp is heard as it builds.</summary>
        private void FocusCue(EffectData data, GameObject owner)
        {
            if (!owner) return;
            int step = Mathf.Clamp(Mathf.RoundToInt(data.genericFloat * GazeFocusPolicy.Tiers), 1, GazeFocusPolicy.Tiers);
            int tier = Mathf.Clamp(Mathf.CeilToInt(step * 3f / GazeFocusPolicy.Tiers), 1, 3); // visual size 1-3
            var palette = SkinFxPalette.FromNetwork(data.color);
            VfxParticles.Burst(data.origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, .14f + .04f * tier,
                Vector2.zero, Vector2.one * (1.2f + .8f * tier), palette.Core);
            VfxParticles.Burst(data.origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 6 + 6 * tier,
                .22f + .05f * tier, new Vector2(3f, 6f + 2f * tier), new Vector2(.04f, .1f), palette.Arc, stretch: .06f);
            VfxParticles.Ring(data.origin, (data.start - data.origin).normalized, .2f, .8f + .6f * tier, .25f, .08f + .03f * tier,
                palette.Material(VfxAssets.Trail), palette);
            if (data.genericBool)
            {
                Util.PlaySound(CustomSoundBank.Ready ? "Play_HS_GazeLoad" + step : "Play_HS_ChargeTick", gameObject);
                if (step >= GazeFocusPolicy.Tiers) Util.PlaySound(CustomSoundBank.Ready ? "Play_HS_GazeSurgeHit3" : "Play_HS_SpearBurst", gameObject);
            }
        }
    }

    /// <summary>Gaze sounds: vanilla events from banks the kit already loads (KitSfx.Banks).</summary>
    public static class GazeSfx
    {
        public const string Launch = "Play_captain_shift_start";
        public const string Charge = "Play_railgunner_R_gun_chargeUp";
        public const string Ignite = "Play_railgunner_R_fire";
        public const string IgniteBlast = "Play_mage_R_lightningBlast";
        public const string HumStart = "Play_loader_R_active_loop";
        public const string HumStop = "Stop_loader_R_active_loop";
        public const string CrackleStart = "Play_captain_m2_tazed_loop";
        public const string CrackleStop = "Stop_captain_m2_tazed_loop";
        public const string End = "Play_mage_R_end";
        public const string ForkHit = "Play_captain_m2_tazer_bounce";
    }
}
