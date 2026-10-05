using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using HollowSaint.FoundationKit.Vfx;

namespace HollowSaint.FoundationKit.Stormspear
{
    /// <summary>Charge audio follows the replicated charge contract, independently of materials.
    /// StormspearFx owns geometry only; this is the sole charge/hold/release sound owner.</summary>
    [DisallowMultipleComponent]
    internal sealed class StormspearAudio : MonoBehaviour
    {
        private CharacterBody body;
        private StormspearCharge charge;
        private Stage stage;
        private uint startId, loopId, tierId, releaseId;
        private bool subscribed, active, loopStarted, warned;
        private float loopAt, releaseAt = -1f;
        private int pendingTier;
        private string releaseEvent;

        private void Awake() { body = GetComponent<CharacterBody>(); }
        private void OnEnable() { Bind(); }
        private bool Audible => body && body.healthComponent && body.healthComponent.alive &&
            !(NetworkServer.active && !NetworkClient.active);

        private void Bind()
        {
            if (subscribed) return;
            if (!charge) charge = GetComponent<StormspearCharge>();
            if (!charge) return;
            charge.Begun += Begun; charge.Tick += Tick;
            charge.Released += Released; charge.Cancelled += Cancelled;
            subscribed = true;
            if (charge.Charging) Begun(charge);
        }

        private uint Play(string custom, string fallback)
        {
            string name = CustomSoundBank.Ready ? custom : fallback;
            if (!Audible || string.IsNullOrEmpty(name)) return 0;
            try { return Util.PlaySound(name, gameObject); }
            catch (System.Exception error) { Warn(error); return 0; }
        }

        private void Warn(System.Exception error)
        {
            if (warned) return;
            warned = true;
            Plugin.Log.LogWarning("HOLLOW_SAINT_SPEAR_AUDIO " + error.Message);
        }

        private void Stop(ref uint id)
        {
            uint playing = id; id = 0;
            if (playing == 0) return;
            try { AkSoundEngine.StopPlayingID(playing, 30); }
            catch (System.Exception error) { Warn(error); }
        }

        private void Clear()
        {
            active = loopStarted = false; pendingTier = 0;
            releaseAt = -1f; releaseEvent = null;
            Stop(ref startId); Stop(ref loopId); Stop(ref tierId); Stop(ref releaseId);
        }

        private void Begun(StormspearCharge state)
        {
            // Repeated Begin without a corresponding release/cancel cannot layer loops.
            if (active && state == charge) return;
            Clear();
            if (!Audible) return;
            stage = Stage.instance; active = true;
            startId = Play("Play_HS_SpearChargeStart", "Play_mage_m1_cast_lightning");
            loopAt = Time.time + .20f;
        }

        private void Tick(StormspearCharge state, int tier)
        {
            if (active) pendingTier = Mathf.Max(pendingTier, Mathf.Clamp(tier, 1, 3));
        }

        private void Released(StormspearCharge state)
        {
            bool heardCharge = active;
            Clear();
            if (!heardCharge || !Audible) return;
            stage = Stage.instance;
            bool crown = state.LastReleaseForm == SpearForm.Crown;
            releaseEvent = crown || state.LastReleaseCharge >= .5f ? "Play_HS_SpearThrowHeavy" : "Play_HS_SpearThrow";
            releaseAt = Time.time + (crown ? 0f : Mathf.Max(0f, StormspearTuning.HandReleaseDelay));
        }

        private void Cancelled(StormspearCharge state) { Clear(); }

        private void Update()
        {
            Bind();
            if (!Audible || Stage.instance != stage || !charge || !charge.isActiveAndEnabled)
            { Clear(); return; }
            if (active && !charge.Charging) { Clear(); return; }
            if (active && !loopStarted && Time.time >= loopAt)
            {
                // One emitter-local held voice during charge and at full, with no fallback loop.
                loopStarted = true;
                loopId = Play("Play_HS_SpearChargeLoop", null);
            }
            if (active && pendingTier > 0)
            {
                int tier = pendingTier; pendingTier = 0;
                Stop(ref tierId);
                // Attack-speed jumps crossing several thresholds make only the highest cue.
                if (tier == 3) tierId = Play("Play_HS_MeterFull", "Play_railgunner_R_gun_ready");
                else tierId = Play("Play_HS_ChargeTick", "Play_mage_m1_cast_lightning");
            }
            if (releaseAt >= 0f && Time.time >= releaseAt)
            {
                string name = releaseEvent;
                releaseAt = -1f; releaseEvent = null;
                releaseId = Play(name, "Play_captain_m2_tazer_shoot");
            }
        }

        private void OnDisable()
        {
            Clear();
            if (subscribed && charge)
            {
                charge.Begun -= Begun; charge.Tick -= Tick;
                charge.Released -= Released; charge.Cancelled -= Cancelled;
            }
            subscribed = false;
        }
        private void OnDestroy() { OnDisable(); }
    }
}
