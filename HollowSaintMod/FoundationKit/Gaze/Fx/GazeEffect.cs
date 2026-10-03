using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using HollowSaint.FoundationKit.Vfx;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    /// <summary>
    /// Networked Gaze moments (fork hits and chain hops), raised on the server and played on
    /// every machine. Own effect prefab, so the shared Beat list stays untouched.
    /// EffectData: origin = target, start = where the lightning leaves, genericUInt = Kind,
    /// genericFloat = delay, genericBool = sound, color = skin palette.
    /// </summary>
    public sealed class GazeEffect : MonoBehaviour
    {
        public enum Kind : uint { Fork = 1, Chain = 2 }

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
            prefab.AddComponent<DestroyOnTimer>().duration = 2f;
            Prefab = prefab;
            KitContent.AddEffect(prefab);
        }

        public static void Server(Kind kind, Vector3 target, Vector3 from, CharacterBody owner, bool sound, float delay)
        {
            if (!NetworkServer.active || !Prefab) return;
            var data = new EffectData
            {
                origin = target,
                start = from,
                genericUInt = (uint)kind,
                genericBool = sound,
                genericFloat = Mathf.Clamp(delay, 0f, 1f),
                color = SkinFxPalette.ForBody(owner).NetworkColor
            };
            EffectManager.SpawnEffect(Prefab, data, true);
        }

        private void Start()
        {
            var component = GetComponent<EffectComponent>();
            var data = component ? component.effectData : null;
            if (data == null) return;
            try
            {
                var palette = SkinFxPalette.FromNetwork(data.color);
                switch ((Kind)data.genericUInt)
                {
                    case Kind.Fork:
                        GazeForkFx.ToTarget(this, data.start, data.origin, palette, data.genericFloat);
                        if (data.genericBool) Util.PlaySound(GazeSfx.ForkHit, gameObject);
                        break;
                    case Kind.Chain:
                        StartCoroutine(Chain(data.start, data.origin, palette, data.genericFloat));
                        break;
                }
            }
            catch (System.Exception error)
            {
                Plugin.Log.LogError("HOLLOW_SAINT_GAZE_FX_ERROR " + error);
            }
        }

        private static System.Collections.IEnumerator Chain(Vector3 from, Vector3 to, SkinFxPalette palette, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            LightningLine.Spawn(from, to, 0.3f, 1.2f, 2, 0.16f, palette: palette);
            LightningLine.Spawn(from, to, 0.5f, 0.4f, 0, 0.03f, 10f, palette: palette);
            GazeForkFx.Strike(to, palette, 0.8f);
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
