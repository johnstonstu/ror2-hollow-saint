using System;
using System.Collections;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>Every audiovisual moment in the kit. Each beat plays its VFX and SFX together.</summary>
    public enum Beat : uint
    {
        // Local: raised on every machine by the EntityState or a replicated buff edge.
        ArcBoltCast = 1,
        SpearThrow = 2,
        ArcStepStart = 3,
        ArcStepEnd = 4,
        CircuitUnfold = 5,
        CircuitOpen = 6,
        CircuitClose = 7,
        MeterFull = 8,
        StaticTier = 9,   // enemy Static crackle arc (StaticFx); also its throttled tick sound
        ChargeTick = 10,  // Saint's Storm charge went up

        // Networked: raised on the server, sent to every client through EffectManager.
        BoltImpact = 20,
        ChainHop = 21,
        SpearImpact = 22,
        CircuitPulse = 23,
        CircuitArc = 24,
        Electrocute = 27,
        ElectrocuteArc = 28,
        ThunderTelegraph = 29,
        ThunderStrike = 30,
        ThunderGather = 31,
        ThunderRelease = 32,
        ThunderCancel = 33,
        SpearPulse = 34,     // planted spear pulse: faint radius ring + crackle at the spear
        SpearPulseArc = 35,  // thin arc spear -> pulse target (also the despawn fizzle)
        SpearConduct = 36,   // Arc Bolt hit -> spear (spread feed)
        SpearSpread = 37,    // spear -> spread target
        SpearRecall = 38,    // spear flies back to the hand
        SpearStruck = 39,    // Arc Bolt struck the planted spear itself
        SpearBurst = 40,     // v0.9 Stormspear impact: AoE lightning spreading from origin; scale = burst radius (m)
        SpearStuck = 41,     // v0.9.10 spear lodged in an enemy or the ground; start = flight direction, scale = charge, float = seconds
        CircuitDwellZap = 42 // confirmed server dwell hit; owner and exact victim point, no independent damage
    }

    /// <summary>v0.8 impact feel: a brief attacker hit-pause (visual only) and positional camera
    /// shake on the heavy hits. Runs where the beat renders, so every machine sees the same.</summary>
    internal static class ImpactFeelSettings
    {
        public static bool Enabled = true;
    }
    /// <summary>
    /// Custom Wwise events with vanilla fallbacks if the embedded bank cannot load.
    /// </summary>
    public static class KitSfx
    {
        public static string For(Beat beat)
        {
            // The Ukulele prefab may supply its own audio. Avoid layering another hit.
            if (beat == Beat.ChainHop && ChainLightningFx.HasSound) return null;
            if (CustomSoundBank.Ready)
            {
                // Dedicated impact media reuses ChainHop's crackle at 0.82 source gain.
                // Other ChainHop voices and the global SFX bus retain their original levels.
                if (beat == Beat.BoltImpact) return "Play_HS_BoltImpact";
                switch (beat)
                {
                    case Beat.ArcBoltCast: case Beat.BoltImpact: case Beat.ChainHop:
                    case Beat.SpearThrow: case Beat.SpearImpact:
                    case Beat.ArcStepStart: case Beat.ArcStepEnd:
                    case Beat.CircuitUnfold: case Beat.CircuitOpen:
                    case Beat.CircuitPulse: case Beat.CircuitClose:
                    case Beat.MeterFull: case Beat.StaticTier:
                    case Beat.ChargeTick: case Beat.Electrocute:
                    case Beat.ThunderTelegraph: case Beat.ThunderRelease: case Beat.ThunderStrike:
                    case Beat.SpearRecall: case Beat.SpearPulse: case Beat.SpearStruck: // v0.8 bank
                        return "Play_HS_" + beat;
                }
            }
            switch (beat)
            {
                case Beat.ArcBoltCast: return "Play_mage_m1_cast_lightning";
                case Beat.BoltImpact: return "Play_captain_m2_tazer_bounce";
                case Beat.ChainHop: return "Play_captain_m2_tazer_bounce";
                case Beat.SpearThrow: return "Play_captain_m2_tazer_shoot";
                case Beat.SpearImpact: return "Play_captain_m2_tazer_impact";
                case Beat.ArcStepStart: return "Play_huntress_shift_mini_blink";
                case Beat.ArcStepEnd: return "Play_loader_shift_release";
                case Beat.CircuitUnfold: return "Play_mage_R_start";
                case Beat.CircuitOpen: return "Play_loader_R_activate";
                case Beat.CircuitPulse: return "Play_loader_R_shock";
                case Beat.CircuitClose: return "Play_loader_R_expire";
                case Beat.MeterFull: return "Play_railgunner_R_gun_ready";
                case Beat.StaticTier: return "Play_captain_m2_tazer_bounce";
                case Beat.ChargeTick: return "Play_mage_m1_cast_lightning";
                case Beat.Electrocute: return "Play_captain_m2_tazer_impact";
                case Beat.ThunderTelegraph: return "Play_captain_shift_preImpact";
                case Beat.ThunderStrike: return "Play_captain_shift_impact";
                case Beat.SpearPulse: return "Play_captain_m2_tazer_bounce";
                case Beat.SpearSpread: return "Play_captain_m2_tazer_impact";
                case Beat.SpearRecall: return "Play_mage_m2_zap";
                case Beat.SpearStruck: return "Play_loader_R_shock"; // pylon zap: short and punchy
                case Beat.SpearBurst: return "Play_captain_m2_tazer_impact"; // v0.9 Stormspear impact
                case Beat.SpearStuck: return "Play_captain_m2_tazer_impact"; // v0.9.10 the spear lodges
                default: return null; // arcs ride on their parent beat's sound
            }
        }

        /// <summary>Short existing-bank layers; fallback beats already use these sounds.</summary>
        private static string ExtraFor(Beat beat)
        {
            if (CustomSoundBank.Ready)
            {
                switch (beat)
                {
                    case Beat.ArcBoltCast: return "Play_mage_m1_cast_lightning";
                    case Beat.SpearStuck: return "Play_captain_m2_tazer_impact";
                }
            }
            return beat == Beat.ThunderStrike && !CustomSoundBank.Ready ? "Play_mage_R_lightningBlast" : null;
        }

        private static readonly Dictionary<long, float> lastExtra = new Dictionary<long, float>();
        private static bool AllowExtra(Beat beat, GameObject source, bool persistentSource)
        {
            // Leave the established Thunderbolt sequence alone. New layers cannot
            // stack with attack speed or simultaneous transient hit effects.
            if (beat == Beat.ThunderStrike) return true;
            long key = (long)(uint)beat;
            if (persistentSource) key |= ((long)source.GetInstanceID()) << 8;
            float now = Time.unscaledTime;
            bool tracked = lastExtra.TryGetValue(key, out float last);
            if (tracked && now >= last && now - last < 0.25f) return false;
            if (!tracked && lastExtra.Count >= 64)
            {
                long expired = 0;
                foreach (var entry in lastExtra)
                    if (now < entry.Value || now - entry.Value >= 0.25f) { expired = entry.Key; break; }
                if (expired == 0) return false;
                lastExtra.Remove(expired);
            }
            lastExtra[key] = now;
            return true;
        }

        /// <summary>Banks our placeholder events live in. Loading an already-loaded bank is harmless.</summary>
        internal static readonly string[] Banks = { "char_Mage", "char_captain", "char_loader", "char_Huntress", "char_railgunner", "Char_DroneTech" };

        // Loops and movement sounds (vanilla events, see the bank .txt indexes).
        public static string CrownLoopStart => Sound("Play_HS_CircuitLoop", "Play_loader_R_active_loop");
        public static string CrownLoopStop => Sound("Stop_HS_CircuitLoop", "Stop_loader_R_active_loop");
        public static string GlideLoopStart => Sound("Play_HS_GlideLoop", "Play_DroneTech_Utility_Glide_Loop");
        public static string GlideLoopStop => Sound("Stop_HS_GlideLoop", "Stop_DroneTech_Utility_Glide_Loop");
        public static string GlideEnter => Sound("Play_HS_GlideEnter", "Play_loader_sprint_start");
        public static string GlideExit => Sound("Play_HS_GlideExit", "Play_loader_sprint_end");
        public static string Footstep => Sound("Play_HS_Footstep", "Play_loader_step");
        public static string AirJump => Sound("Play_HS_AirJump", "Play_mage_m2_zap");
        public static string FootstepRun => Sound("Play_HS_FootstepRun", "Play_loader_step_sprint");
        public static string Land => Sound("Play_HS_Land", "Play_loader_step_sprint");
        // v0.8 bank: spear catch and the held fan (no vanilla fallback: silent without the bank).
        public static string SpearCatch => Sound("Play_HS_SpearCatch", null);
        public static string FanStart => Sound("Play_HS_FanStart", null);
        public static string FanLoopStart => Sound("Play_HS_FanLoop", null);
        public static string FanLoopStop => Sound("Stop_HS_FanLoop", null);
        public static string FanEnd => Sound("Play_HS_FanEnd", null);

        internal static void StopThunderGather(GameObject source)
        {
            if (CustomSoundBank.Ready && source) Util.PlaySound("Stop_HS_ThunderTelegraph", source);
        }

        private static string Sound(string custom, string fallback) => CustomSoundBank.Ready ? custom : fallback;

        internal static void LoadBanks()
        {
            CustomSoundBank.Load();
            foreach (var bank in Banks)
            {
                try
                {
                    uint id;
                    var result = AkSoundEngine.LoadBank(bank, out id);
                    Plugin.Log.LogInfo("HOLLOW_SAINT_SFX_BANK " + bank + " result=" + result);
                }
                catch (Exception error)
                {
                    Plugin.Log.LogWarning("HOLLOW_SAINT_SFX_BANK " + bank + " failed: " + error.Message);
                }
            }
        }

        // Per-beat minimum gaps (seconds) so chains and multi-hit bursts do not stack into noise.
        private static float MinGap(Beat beat)
        {
            switch (beat)
            {
                case Beat.ArcBoltCast: return 0.06f;
                case Beat.BoltImpact: return 0.22f;
                case Beat.ChainHop: return 0.05f;
                case Beat.CircuitPulse: return 0.9f;
                case Beat.StaticTier: return 0.25f;
                case Beat.ChargeTick: return 0.12f;
                case Beat.Electrocute: return 0.08f;
                // Accepted gather revisions already deduplicate; a quick re-pick must
                // start its new buildup after stopping the previous one.
                case Beat.ThunderTelegraph: return 0f;
                case Beat.ThunderStrike: return 0.2f;
                case Beat.SpearPulse: return 1.0f;
                case Beat.SpearSpread: return 0.15f;
                case Beat.SpearRecall: return 0.2f;
                case Beat.SpearStruck: return 1f / 6f; // at most 6 per second
                case Beat.SpearBurst: return 0.08f;
                case Beat.SpearStuck: return 0.08f;
                default: return 0f;
            }
        }

        private static readonly Dictionary<long, float> lastPlayed = new Dictionary<long, float>();

        /// <summary>v0.9.1: Stormspear impact sound by burst radius. A charged throw (radius 5 m and up,
        /// about a third charge) gets the sampled lightning strike; a tap keeps the short synth impact.</summary>
        public const float SpearBurstHeavyRadius = 5f;
        public const float SpearHeavyCharge = 0.33f; // the charge at which the burst radius reaches 5 m
        private static string ForScaled(Beat beat, float scale)
        {
            // v0.9.10: the lodge thunks (SpearImpact); the burst after it is the sampled strike when
            // charged and a light crackle for a tap, so a tap does not play the impact twice.
            // v0.9.13 (Stu: impact sound off): one sequence. A tap gets a single punchy impact when it
            // sticks and a silent burst; a charged spear (scale = charge here) gets a light crackle as it
            // sticks and the sampled strike exactly on the burst.
            if (beat == Beat.SpearStuck)
                // v0.9.16 (Stu: light, no zap): every spear hits with the new punchy HS_SpearImpact as it sticks.
                return CustomSoundBank.Ready ? "Play_HS_SpearImpact" : "Play_captain_m2_tazer_impact";
            if (beat == Beat.SpearBurst)
                return scale >= SpearBurstHeavyRadius ? (CustomSoundBank.Ready ? "Play_HS_SpearBurst" : "Play_captain_m2_tazer_impact") : null;
            return For(beat);
        }

        /// <summary>Heavy layer on a charged or crown throw (sampled crackle); null without the bank.</summary>
        public static string SpearThrowHeavy => Sound("Play_HS_SpearThrowHeavy", null);

        /// <summary>Plays a beat's sound, throttled per beat. Pass persistentSource for a
        /// long-lived body so each body throttles independently; transient effect objects
        /// throttle on the beat alone. scale is the beat's scale (Stormspear burst radius).</summary>
        public static void Play(Beat beat, GameObject source, bool persistentSource = false, float scale = 1f)
        {
            string name = ForScaled(beat, scale);
            if (name == null || source == null) return;
            float gap = MinGap(beat);
            if (gap > 0f)
            {
                long key = (long)(uint)beat;
                if (persistentSource) key |= ((long)source.GetInstanceID()) << 8;
                float now = Time.unscaledTime;
                float last;
                if (lastPlayed.TryGetValue(key, out last) && now - last < gap && now >= last) return;
                if (lastPlayed.Count > 64) lastPlayed.Clear();
                lastPlayed[key] = now;
            }
            Util.PlaySound(name, source);
            string extra = ExtraFor(beat);
            if (extra != null && AllowExtra(beat, source, persistentSource)) Util.PlaySound(extra, source);
        }
    }

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
            GameObject arcOwner = beat == Beat.CircuitArc || beat == Beat.SpearRecall ? data.ResolveNetworkedObjectReference() : null;
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

    /// <summary>What each beat looks like. All visuals are independent GameObjects that
    /// clean themselves up, so nothing here depends on the effect prefab's lifetime.</summary>
    public static class BeatVisuals
    {
        /// <summary>Drops a point to the ground below it (within 4 m) so rings lie flat on the floor.</summary>
        private static Vector3 GroundUnder(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out hit, 4.5f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.08f;
            return p;
        }

        private static void ImpactFeel(Beat beat, Vector3 origin, CharacterBody body)
        {
            if (!ImpactFeelSettings.Enabled) return;
            float pause = 0f, amplitude = 0f, frequency = 0f, duration = 0f, radius = 0f;
            switch (beat)
            {
                case Beat.SpearImpact: pause = 0.06f; amplitude = 0.7f; frequency = 18f; duration = 0.22f; radius = 22f; break;
                case Beat.ThunderStrike: pause = 0.07f; amplitude = 1.6f; frequency = 13f; duration = 0.4f; radius = 40f; break;
                case Beat.SpearStruck: pause = 0.04f; amplitude = 0.4f; frequency = 20f; duration = 0.15f; radius = 16f; break;
                default: return;
            }
            var model = body && body.modelLocator ? body.modelLocator.modelTransform : null;
            var presentation = model ? model.GetComponent<HollowSaint.FoundationPresentation>() : null;
            if (presentation) presentation.HitPause(pause);
            ShakeEmitter.CreateSimpleShakeEmitter(origin, new Wave { amplitude = amplitude, frequency = frequency, cycleOffset = 0f }, duration, radius, true);
        }

        public static void Play(Beat beat, Vector3 origin, Vector3 start, float scale, CharacterBody body, SkinFxPalette palette = null)
        {
            palette = palette ?? SkinFxPalette.ForBody(body);
            ImpactFeel(beat, origin, body);
            switch (beat)
            {
                case Beat.ArcBoltCast:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.1f, Vector2.zero, new Vector2(0.4f, 0.5f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 6, 0.18f, new Vector2(4f, 9f), new Vector2(0.08f, 0.16f), palette.Core, stretch: 0.09f);
                    {
                        Vector3 aim = Vector3.zero;
                        if (body && body.inputBank) aim = body.inputBank.aimDirection;
                        for (int i = 0; i < 2; i++)
                        {
                            Vector3 offset = UnityEngine.Random.onUnitSphere * 0.45f;
                            if (aim.sqrMagnitude > 0.01f) offset = aim * 0.4f + UnityEngine.Random.onUnitSphere * 0.22f;
                            LightningLine.Spawn(origin, origin + offset, 0.13f, 0.9f, 1, palette: palette);
                        }
                    }
                    break;

                case Beat.BoltImpact:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.09f, Vector2.zero, new Vector2(0.35f, 0.5f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 6, 0.2f, new Vector2(4f, 9f), new Vector2(0.06f, 0.12f), palette.Core, stretch: 0.06f);
                    LightningLine.Spawn(origin, origin + UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(0.35f, 0.7f), 0.09f, 0.4f, 0, palette: palette);
                    VfxParticles.FlashLight(origin, palette.Arc, 0.6f, 2f, 0.09f);
                    break;

                case Beat.ChainHop:
                    if (ChainLightningFx.TryPlay(start, origin, scale, palette)) break;
                    // Playtest: hops were hard to see. Thicker, longer-lived main arc and a
                    // brighter lingering burn so the path between enemies reads.
                    LightningLine.Spawn(start, origin, 0.32f, 1.6f * scale, 2, 0.14f, palette: palette);
                    LightningLine.Spawn(start, origin, 0.7f, 0.5f * scale, 0, 0.03f, 10f, palette: palette);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 12, 0.25f, new Vector2(6f, 16f), new Vector2(0.08f, 0.18f), palette.Core, stretch: 0.09f);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.1f, Vector2.zero, new Vector2(0.45f, 0.55f), palette.Arc);
                    VfxParticles.FlashLight(origin, palette.Arc, 1.2f, 3.5f, 0.09f);
                    for (int i = 0; i < 2; i++)
                        LightningLine.Spawn(origin, origin + UnityEngine.Random.onUnitSphere * 0.35f, 0.08f, 0.5f, 0, palette: palette);
                    break;

                case Beat.SpearThrow:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.12f, Vector2.zero, new Vector2(0.5f, 0.6f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 10, 0.25f, new Vector2(5f, 12f), new Vector2(0.08f, 0.16f), palette.Arc, stretch: 0.09f);
                    break;

                case Beat.SpearImpact:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.18f, Vector2.zero, new Vector2(1.0f, 1.2f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 26, 0.45f, new Vector2(8f, 20f), new Vector2(0.1f, 0.18f), palette.Arc, stretch: 0.09f);
                    VfxParticles.Ring(origin, Vector3.up, 0.3f, 2.2f, 0.3f, 0.08f, palette.Material(VfxAssets.Trail), palette: palette);
                    for (int i = 0; i < 5; i++)
                        LightningLine.Spawn(origin, origin + UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(1.2f, 2.4f), 0.2f, 0.8f, 1, palette: palette);
                    VfxParticles.FlashLight(origin, palette.Arc, 3f, 6f, 0.25f);
                    break;

                case Beat.ArcStepStart:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 16, 0.3f, new Vector2(5f, 12f), new Vector2(0.08f, 0.18f), palette.Arc, stretch: 0.09f);
                    VfxParticles.Ring(origin + Vector3.up * 0.05f, Vector3.up, 0.2f, 1.2f, 0.22f, 0.05f, palette.Material(VfxAssets.Trail), palette: palette);
                    break;

                case Beat.ArcStepEnd:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 8, 0.22f, new Vector2(3f, 7f), new Vector2(0.08f, 0.14f), palette.Arc, stretch: 0.09f);
                    break;

                case Beat.CircuitUnfold:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 20, 0.6f, new Vector2(1f, 3f), new Vector2(0.08f, 0.16f), palette.Arc);
                    break;

                case Beat.CircuitOpen:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.25f, Vector2.zero, new Vector2(1.2f, 1.4f), palette.Arc);
                    VfxParticles.Ring(GroundUnder(origin), Vector3.up, 0.5f, KitTuning.OpenCircuitRadius, 0.45f, 0.1f, palette.Material(VfxAssets.Trail), palette: palette);
                    VfxParticles.FlashLight(origin, palette.Arc, 4f, 10f, 0.35f);
                    break;

                case Beat.CircuitClose:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 12, 0.4f, new Vector2(1f, 4f), new Vector2(0.08f, 0.14f), palette.Outer);
                    break;

                case Beat.CircuitPulse:
                    VfxParticles.Ring(GroundUnder(origin), Vector3.up, 0.6f, Mathf.Max(1f, scale), 0.3f, 0.04f, palette.Material(VfxAssets.Trail), palette: palette);
                    break;

                case Beat.SpearPulse:
                    // origin = anchor centre, start = spear butt, scale = radius. Deliberately faint.
                    VfxParticles.Ring(GroundUnder(origin), Vector3.up, Mathf.Max(1f, scale) * 0.82f, Mathf.Max(1f, scale), 0.4f, 0.03f, palette.Material(VfxAssets.Trail), palette: palette);
                    for (int i = 0; i < 2; i++)
                        LightningLine.Spawn(start, start + UnityEngine.Random.onUnitSphere * 0.7f, 0.1f, 0.25f, 0, palette: palette);
                    VfxParticles.FlashLight(start, palette.Arc, 1f, 4f, 0.1f);
                    break;

                case Beat.SpearPulseArc:
                    LightningLine.Spawn(start, origin, 0.12f, 0.3f, 0, 0.12f, palette: palette);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 3, 0.15f, new Vector2(2f, 5f), new Vector2(0.05f, 0.1f), palette.Arc, stretch: 0.06f);
                    break;

                case Beat.SpearConduct:
                {
                    // start = Arc Bolt hit, origin = spear butt. The spear flares as it takes the current.
                    // scale 1 = spread followed, 0.6 = link only (nothing else in range).
                    float s = Mathf.Clamp(scale, 0.3f, 1f);
                    LightningLine.Spawn(start, origin, 0.22f, 0.9f * s, s >= 1f ? 1 : 0, 0.14f, palette: palette);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.16f, Vector2.zero, new Vector2(0.9f, 1.1f) * s, palette.Arc);
                    LightningLine.Spawn(origin, origin + UnityEngine.Random.onUnitSphere * 0.5f, 0.12f, 0.3f, 0, 0.2f, palette: palette);
                    VfxParticles.FlashLight(origin, palette.Arc, 2.5f * s, 6f, 0.18f);
                    LanceGhost.FlareNear(origin, 2.5f, 0.5f * s);
                    break;
                }

                case Beat.SpearStruck:
                {
                    // origin = shaft point (in the ground or enemy), start = exposed butt.
                    // White-hot pop at the butt, crackle running the shaft, small fast ring:
                    // brighter and tighter than the faint radius-wide pulse ring.
                    Vector3 butt = start, tip = origin;
                    Vector3 axis = butt - tip;
                    VfxParticles.Burst(butt, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.14f, Vector2.zero, new Vector2(1.3f, 1.5f), palette.Core);
                    VfxParticles.Burst(butt, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.2f, Vector2.zero, new Vector2(0.8f, 0.9f), palette.Arc);
                    VfxParticles.Burst(butt, Quaternion.identity, palette.Material(VfxAssets.Spark), 16, 0.3f, new Vector2(6f, 14f), new Vector2(0.08f, 0.16f), palette.Core, stretch: 0.09f);
                    LightningLine.Spawn(tip, butt, 0.16f, 0.55f, 1, 0.22f, 0.03f, palette: palette);
                    LightningLine.Spawn(butt, tip, 0.1f, 0.35f, 0, 0.3f, 0.03f, palette: palette);
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 a = tip + axis * UnityEngine.Random.Range(0.1f, 0.9f);
                        LightningLine.Spawn(a, a + UnityEngine.Random.onUnitSphere * 0.45f, 0.1f, 0.25f, 0, 0.25f, palette: palette);
                    }
                    VfxParticles.Ring(GroundUnder(tip), Vector3.up, 0.25f, 2.4f, 0.22f, 0.09f, palette.Material(VfxAssets.Trail), palette: palette);
                    VfxParticles.FlashLight(butt, palette.Core, 3f, 7f, 0.14f);
                    LanceGhost.FlareNear(Vector3.Lerp(tip, butt, 0.5f), 3f, 1f);
                    break;
                }

                case Beat.SpearBurst:
                    // v0.9: origin = impact, start = surface normal, scale = burst radius (m).
                    Stormspear.Fx.SpearBurstFx.Play(origin, start, scale, palette);
                    break;

                case Beat.SpearSpread:
                    // start = spear butt, origin = spread target. Thick, branched, lingering.
                    LightningLine.Spawn(start, origin, 0.28f, 1.3f, 2, 0.16f, palette: palette);
                    LightningLine.Spawn(start, origin, 0.5f, 0.45f, 0, 0.03f, 10f, palette: palette);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 10, 0.25f, new Vector2(5f, 12f), new Vector2(0.08f, 0.16f), palette.Core, stretch: 0.09f);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.1f, Vector2.zero, new Vector2(0.45f, 0.55f), palette.Arc);
                    break;

                case Beat.SpearRecall:
                    // start = spear butt (where it leaves), origin = the hand it returns to.
                    var recall = LightningLine.Spawn(start, origin, 0.25f, 0.6f, 1, 0.1f, palette: palette);
                    if (body) recall.endAnchor = SpearDischarge.SpearCarry.GripSocketOf(body);
                    VfxParticles.Burst(start, Quaternion.identity, palette.Material(VfxAssets.Spark), 12, 0.3f, new Vector2(3f, 8f), new Vector2(0.08f, 0.14f), palette.Arc, stretch: 0.09f);
                    break;

                case Beat.CircuitArc:
                    // start = the point on the owner's halo ring nearest the target.
                    VfxParticles.Burst(start, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.1f, Vector2.zero, new Vector2(0.22f, 0.3f), palette.Core);
                    LightningLine.Spawn(start, origin, 0.2f, 1.3f, 2, 0.16f, palette: palette);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 6, 0.2f, new Vector2(3f, 8f), new Vector2(0.08f, 0.14f), palette.Core, stretch: 0.09f);
                    break;

                case Beat.MeterFull:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.3f, Vector2.zero, new Vector2(0.8f, 0.9f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 18, 0.4f, new Vector2(2f, 6f), new Vector2(0.08f, 0.16f), palette.Arc);
                    break;

                case Beat.StaticTier:
                    // origin/start are the two ends of one small crackle arc; scale is the tier (1..4).
                    LightningLine.Spawn(origin, start, 0.12f, 0.12f + 0.03f * scale, 0, 0.2f, palette: palette);
                    if (scale >= 3f)
                        VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 3, 0.18f, new Vector2(1f, 3f), new Vector2(0.05f, 0.1f), palette.Arc, stretch: 0.06f);
                    break;

                case Beat.ChargeTick:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.12f, Vector2.zero, new Vector2(0.3f, 0.4f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 6, 0.25f, new Vector2(2f, 5f), new Vector2(0.05f, 0.1f), palette.Arc, stretch: 0.06f);
                    break;

                case Beat.Electrocute:
                {
                    // scale = victim body radius. Small flash (max 1 m), arcs crawling over the body, sparks.
                    float r = Mathf.Clamp(scale, 0.3f, 3f);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.15f, Vector2.zero, new Vector2(0.7f, 1.0f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 16, 0.35f, new Vector2(4f, 10f), new Vector2(0.08f, 0.16f), palette.Core, stretch: 0.09f);
                    int arcs = UnityEngine.Random.Range(3, 5);
                    for (int i = 0; i < arcs; i++)
                    {
                        Vector3 a = origin + UnityEngine.Random.onUnitSphere * r;
                        Vector3 b = origin + UnityEngine.Random.onUnitSphere * r;
                        LightningLine.Spawn(a, b, 0.2f, 0.32f, 0, 0.2f, palette: palette);
                    }
                    VfxParticles.FlashLight(origin, palette.Arc, 1.5f, 5f, 0.12f);
                    break;
                }

                case Beat.ElectrocuteArc:
                    // start = Electrocuted enemy, origin = pop target.
                    LightningLine.Spawn(start, origin, 0.2f, 0.5f, 2, 0.14f, palette: palette);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.1f, Vector2.zero, new Vector2(0.5f, 0.7f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 10, 0.3f, new Vector2(5f, 12f), new Vector2(0.08f, 0.16f), palette.Core, stretch: 0.09f);
                    break;

                case Beat.ThunderTelegraph:
                {
                    // origin = target's feet, scale = seconds until the strike (v0.9.15). Ring shrinking
                    // inward on the ground until the bolt lands, flicker overhead.
                    Vector3 ground = GroundUnder(origin);
                    float until = scale > 0.05f && scale < 5f ? scale : KitTuning.ThunderboltTelegraphSeconds;
                    VfxParticles.Ring(ground, Vector3.up, 2.6f, 0.4f, until, 0.07f, palette.Material(VfxAssets.Trail), palette: palette);
                    VfxParticles.Ring(ground, Vector3.up, 1.6f, 0.2f, until, 0.04f, palette.Material(VfxAssets.Trail), palette: palette);
                    Vector3 sky = origin + Vector3.up * 25f;
                    LightningLine.Spawn(sky + UnityEngine.Random.insideUnitSphere * 3f, sky + Vector3.down * 8f + UnityEngine.Random.insideUnitSphere * 2f, 0.18f, 0.5f, 1, 0.15f, palette: palette);
                    VfxParticles.FlashLight(origin + Vector3.up * 12f, palette.Arc, 1.5f, 12f, 0.25f);
                    break;
                }

                case Beat.ThunderStrike:
                {
                    // origin = struck enemy's feet. Thick bolt from 25 m up, afterglow, ring, sparks, one light.
                    Vector3 ground = GroundUnder(origin);
                    Vector3 top = origin + Vector3.up * 25f;
                    LightningLine.Spawn(top, origin, 0.25f, 3f, 3, 0.05f, 0.05f, palette: palette);
                    LightningLine.Spawn(top, origin, 0.45f, 1.4f, 0, 0.04f, 0.09f, palette: palette);
                    VfxParticles.Ring(ground, Vector3.up, 0.4f, 3.5f, 0.35f, 0.12f, palette.Material(VfxAssets.Trail), palette: palette);
                    VfxParticles.Burst(origin + Vector3.up * 0.5f, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.2f, Vector2.zero, new Vector2(1.1f, 1.4f), palette.Arc);
                    VfxParticles.Burst(origin + Vector3.up * 0.3f, Quaternion.identity, palette.Material(VfxAssets.Spark), 32, 0.6f, new Vector2(6f, 16f), new Vector2(0.08f, 0.18f), palette.Core, stretch: 0.09f);
                    VfxParticles.FlashLight(origin + Vector3.up * 1.5f, palette.Arc, 3f, 12f, 0.3f);
                    break;
                }
            }
        }
    }
}
