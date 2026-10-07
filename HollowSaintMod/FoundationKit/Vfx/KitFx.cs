using System;
using System.Collections;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Vfx
{

    /// <summary>Entry point for raising beats.</summary>
    public static class KitFx
    {
        public static GameObject NetworkedPrefab { get; private set; }
        private static bool warnedStuckOwner;

        internal static void Register()
        {
            if (NetworkedPrefab != null) return;
            VfxAssets.Load();
            ChainLightningFx.Load();
            KitSfx.LoadBanks();
            var prefab = VfxAssets.NewPrefab("HollowSaintBeatEffect");
            var effect = prefab.AddComponent<EffectComponent>();
            effect.applyScale = false;
            effect.parentToReferencedTransform = false;
            effect.positionAtReferencedTransform = false;
            var vfx = prefab.AddComponent<VFXAttributes>();
            vfx.DoNotPool = true; // our component initialises in Start; pooled reuse would skip it
            vfx.vfxPriority = VFXAttributes.VFXPriority.Always;
            prefab.AddComponent<BeatEffect>();
            prefab.AddComponent<DestroyOnTimer>().duration = 2f;
            NetworkedPrefab = prefab;
            KitContent.AddEffect(prefab);
        }

        /// <summary>Server: send a beat to everyone (including the host).</summary>
        public static void Server(Beat beat, Vector3 origin, Vector3 start = default(Vector3), float scale = 1f, bool sound = true, float delay = 0f, CharacterBody owner = null)
        {
            if (!NetworkServer.active || NetworkedPrefab == null) return;
            var data = new EffectData
            {
                origin = origin,
                start = start,
                scale = scale,
                genericUInt = (uint)beat,
                genericBool = sound,
                genericFloat = Mathf.Clamp(delay, 0f, 1.2f),
                color = SkinFxPalette.ForBody(owner).NetworkColor
            };
            // Crown tendrils carry their owner so each client starts them from its own
            // live halo ring (start stays the server's ring point as the fallback).
            if ((beat == Beat.CircuitArc || beat == Beat.SpearRecall || beat == Beat.CircuitDwellZap) && owner) data.SetNetworkedObjectReference(owner.gameObject);
            EffectManager.SpawnEffect(NetworkedPrefab, data, true);
        }

        /// <summary>v0.9.10 server: the thrown spear lodged at <paramref name="point"/>, in <paramref name="victim"/>
        /// (rides it on every machine) or in the ground when victim is null.</summary>
        public static void ServerStuck(Vector3 point, Vector3 direction, float charge, float seconds, GameObject victim, CharacterBody owner)
        {
            if (!NetworkServer.active || NetworkedPrefab == null) return;
            var identity = owner ? owner.GetComponent<NetworkIdentity>() : null;
            uint ownerId = identity ? identity.netId.Value : 0u;
            if (ownerId > Stormspear.SpearStuckEvent.MaxOwnerId)
            {
                seconds = Mathf.Min(seconds, Stormspear.StormspearTuning.StickSeconds);
                if (!warnedStuckOwner)
                {
                    warnedStuckOwner = true;
                    Plugin.Log.LogWarning("HOLLOW_SAINT_STUCK_OWNER_ID: using short spear FX for an oversized network ID.");
                }
            }
            var data = new EffectData
            {
                origin = point,
                start = direction,
                scale = charge,
                genericUInt = Stormspear.SpearStuckEvent.Encode(ownerId),
                genericBool = true,
                genericFloat = Mathf.Clamp(seconds, 0f, 3f),
                color = SkinFxPalette.ForBody(owner).NetworkColor
            };
            if (victim) data.SetNetworkedObjectReference(victim);
            EffectManager.SpawnEffect(NetworkedPrefab, data, true);
        }

        /// <summary>Server: route a sequenced Storm presentation beat to its owning body.</summary>
        internal static void ServerOnBody(Beat beat, CharacterBody body, Vector3 target, float duration, int revision)
        {
            if (!NetworkServer.active || NetworkedPrefab == null || !body) return;
            var data = new EffectData
            {
                origin = target,
                start = Socket(body, "Halo"),
                genericUInt = (uint)beat,
                genericFloat = duration,
                scale = revision,
                color = SkinFxPalette.ForBody(body).NetworkColor
            };
            data.SetNetworkedObjectReference(body.gameObject);
            EffectManager.SpawnEffect(NetworkedPrefab, data, true);
        }

        /// <summary>Local: play a beat on this machine only (the caller runs on every machine).</summary>
        public static void Local(Beat beat, CharacterBody body, Vector3 origin, Vector3 start = default(Vector3), float scale = 1f)
        {
            try
            {
                if (beat == Beat.ChargeTick || beat == Beat.MeterFull || beat == Beat.SpearRecall)
                    BodyCurrentFx.PulseCore(body, 0.18f);
                BeatVisuals.Play(beat, origin, start, scale, body);
                if (body) KitSfx.Play(beat, body.gameObject, true);
            }
            catch (Exception error)
            {
                Plugin.Log.LogError("HOLLOW_SAINT_FX_ERROR " + beat + ": " + error);
            }
        }

        /// <summary>Socket position on the body's model, falling back to the core.</summary>
        public static Vector3 Socket(CharacterBody body, string alias)
        {
            var t = KitUtil.ResolveSocket(body, alias);
            if (t) return t.position;
            return body ? body.corePosition : Vector3.zero;
        }
    }
}
