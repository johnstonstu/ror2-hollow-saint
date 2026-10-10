using System;
using System.Collections;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Vfx
{

    /// <summary>Receives a networked beat on each machine and plays it.</summary>
    public sealed class BeatEffect : MonoBehaviour
    {
        private void Start()
        {
            var component = GetComponent<EffectComponent>();
            var data = component != null ? component.effectData : null;
            if (data == null) return;
            var beat = Stormspear.SpearStuckEvent.IsStuck(data.genericUInt) ? Beat.SpearStuck : (Beat)data.genericUInt;
            if (beat == Beat.CircuitDwellZap)
            {
                try
                {
                    var owner = data.ResolveNetworkedObjectReference();
                    var visual = owner ? owner.GetComponent<OpenCircuit.Fx.OpenCircuitDomeFx>() : null;
                    if (visual) visual.ShowConfirmedStrike(data.origin);
                    // 1.2 audio pass: the dwell zap (Circuit's strongest hit) was silent. Short,
                    // punchy and spatial at the victim, distinct from the 0.5 s pulse tick.
                    Util.PlaySound(CustomSoundBank.Ready ? "Play_HS_SpearStruck" : "Play_loader_R_shock", gameObject);
                }
                catch (Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_CIRCUIT_ZAP_FX " + error); }
                return;
            }
            if (beat == Beat.ThunderGather || beat == Beat.ThunderRelease || beat == Beat.ThunderCancel)
            {
                var owner = data.ResolveNetworkedObjectReference();
                var halo = owner ? owner.GetComponent<Storm.StormChargeHalo>() : null;
                if (halo) halo.ReceiveThunderBeat(beat, data.origin, data.genericFloat, Mathf.RoundToInt(data.scale), SkinFxPalette.FromNetwork(data.color));
                return;
            }
            if (beat == Beat.SpearStuck)
            {
                try
                {
                    var palette = SkinFxPalette.FromNetwork(data.color);
                    Stormspear.Fx.StuckSpearFx.Spawn(data.origin, data.start, data.scale, data.genericFloat, data.ResolveNetworkedObjectReference(), palette, Stormspear.SpearStuckEvent.OwnerId(data.genericUInt));
                    if (data.genericBool) KitSfx.Play(beat, gameObject, false, data.scale);
                }
                catch (Exception error) { Plugin.Log.LogError("HOLLOW_SAINT_FX_ERROR " + beat + ": " + error); }
                return;
            }
            float delay = data.genericFloat;
            GameObject arcOwner = beat == Beat.CircuitArc || beat == Beat.SpearRecall || beat == Beat.ThunderTelegraph ? data.ResolveNetworkedObjectReference() : null;
            if (delay > 0.001f)
            {
                // Staggered chain: the prefab's DestroyOnTimer (2 s) outlives delay + play.
                StartCoroutine(PlayAfter(beat, delay, data.origin, data.start, data.scale, data.genericBool, SkinFxPalette.FromNetwork(data.color), arcOwner));
                return;
            }
            PlayNow(beat, data.origin, data.start, data.scale, data.genericBool, SkinFxPalette.FromNetwork(data.color), arcOwner);
        }

        private IEnumerator PlayAfter(Beat beat, float delay, Vector3 origin, Vector3 start, float scale, bool sound, SkinFxPalette palette, GameObject owner)
        {
            yield return new WaitForSeconds(delay);
            PlayNow(beat, origin, start, scale, sound, palette, owner);
        }

        private void PlayNow(Beat beat, Vector3 origin, Vector3 start, float scale, bool sound, SkinFxPalette palette, GameObject owner = null)
        {
            try
            {
                var ownerBody = owner ? owner.GetComponent<CharacterBody>() : null;
                if (owner && beat == Beat.CircuitArc)
                {
                    // Leave from this machine's live ring, at the point nearest the target.
                    var ring = ownerBody ? ownerBody.GetComponent<HaloRing>() : null;
                    if (ring && ring.Valid) start = ring.Shape.Nearest(origin);
                    var crown = ownerBody ? ownerBody.GetComponent<CircuitCrownFx>() : null;
                    if (crown) crown.Kick();
                }
                BeatVisuals.Play(beat, origin, start, scale, ownerBody, palette);
                if (sound) KitSfx.Play(beat, gameObject, false, scale);
            }
            catch (Exception error)
            {
                Plugin.Log.LogError("HOLLOW_SAINT_FX_ERROR " + beat + ": " + error);
            }
        }
    }
}
