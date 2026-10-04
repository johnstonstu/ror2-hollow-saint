using System;
using System.Collections.Generic;
using EntityStates;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit
{
    // Shared foundation for the kit. docs/kit-architecture.md explains the networking
    // model every skill follows. Short version:
    //
    //   * EntityStates run on EVERY machine. The owning client (isAuthority) decides
    //     timing and aim. The server runs its own copy of the same state
    //     (NetworkServer.active; isAuthority is false there for a remote player), and that
    //     copy is where buffs get applied.
    //   * Projectiles are fired from the AUTHORITY via ProjectileManager.FireProjectile,
    //     which forwards to the server when called on a client. Never gate a projectile
    //     on NetworkServer.active, or clients can never shoot.
    //   * Direct damage (TakeDamage, BlastAttack, chain hops) and meter changes happen on
    //     the server only (NetworkServer.active). Never gate server work on
    //     hasEffectiveAuthority: on the host, a remote player's body is not "ours".
    //   * State that clients must see (Storm charge, Open Circuit window) lives in
    //     buffs, because CharacterBody already replicates buffs.

    /// <summary>Central tuning. Approved numbers unless marked PROPOSAL.</summary>
    /// <summary>Central tuning. Approved numbers unless marked PROPOSAL. Fields, not
    /// constants: KitConfig binds most of them to the BepInEx config and the in-game
    /// Risk of Options menu. Cooldowns and stocks are baked into SkillDefs at load, so
    /// those need a restart; everything read per use (damage, ranges, counts) is live.</summary>
    public static partial class KitTuning
    {
        // Arc Bolt: approved. One shot every 0.5 s at 1x attack speed.
        public static float ArcBoltInterval = 0.5f;
        public static float ArcBoltDamageCoefficient = 1.2f;
        public static int ArcBoltMaxChainTargets = 4;
        public static float ArcBoltChainRange = 12f;
        public static float ArcBoltChainFalloff = 0.75f;
        // v0.9.13 (Stu: primary procs felt high): direct hit 0.8 (was 1.0); chain hops 0.4, 0.2, 0.1 (were 0.5 each).
        public static float ArcBoltProcCoefficient = 0.8f;
        public static float ArcBoltChainProc = 0.4f;
        public static float ArcBoltProjectileSpeed = 120f;
        public static float ArcBoltRadius = 0.75f;
        public static float ArcBoltAssistConeDegrees = 3f;
        // Anim spec section 4: "Bolt release" marker.
        public static float ArcBoltReleaseNormalizedTime = 0.2105f;
        // v0.9.10: shortest time an Arc Bolt arm gesture plays in (high attack speed overlaps gestures).
        public static float ArcBoltMinGestureSeconds = 0.25f;

        // Stormspear (secondary): see Stormspear/StormspearTuning.cs.

        // Arc Step: approved.
        public static int ArcStepMaxStock = 2;
        public static float ArcStepRecharge = 5f;
        public static float ArcStepDuration = 0.55f;
        public static float ArcStepLookLift = 0.35f; // v0.9.16: share of the dash speed that follows the aim pitch
        public static float ArcStepSpeed = 20f; // was 16; playtest: 'needs to move a bit further'
        // PROPOSAL: no i-frames pending Stuart's decision.
        public static bool ArcStepGrantsIFrames = false;

        // Open Circuit: approved.
        // v0.9.15: crown 10 s (was 8), cooldown 8 s counted from when the crown closes (was 12 s from the cast).
        public static float OpenCircuitCooldown = 8f;
        public static float OpenCircuitBuffSeconds = 10f;
        public static float OpenCircuitPulseInterval = 0.5f;
        public static float OpenCircuitPulseDamageCoefficient = 0.6f;
        public static float OpenCircuitRadius = 8f;
        // Storm passive ("Answered Prayer"): Static -> Electrocute -> Thunderbolt.
        // See docs/storm-passive.md. All live-tunable (section "5. Storm").
        public static float StaticThreshold = 0.25f;          // fraction of target max health that fills Static 0 -> 100%
        public static float StaticMinGain = 0.06f;             // was 0.08: minimum Static per full-proc hit
        public static float StaticCritMultiplier = 1.5f;
        public static float StaticOpenCircuitWeight = 0.3f;    // proc-equivalent for Open Circuit pulses (they carry proc 0)
        public static float StaticDecayDelay = 2f;             // seconds after the last hit before decay starts
        public static float StaticDecayPerSecond = 0.5f;       // fraction per second
        public static float ElectrocuteStunSeconds = 0.5f;     // v0.9.13: was 1.5 (a jolt, not a long stun)
        public static float ShockedSeconds = 3f;
        public static float ShockedDamageMultiplier = 1.15f;   // was 1.2
        public static float ElectrocutePopDamageCoefficient = 1.5f; // was 2.5
        public static float ElectrocutePopRadius = 6f;
        public static int ElectrocutePopTargets = 2;           // was 3
        public static float ElectrocutePopProc = 0.3f;         // was 0.5
        public static float ElectrocutePopStatic = 0.15f;      // was 0.4 (cascade)
        public static float ElectrocuteImmuneSeconds = 4f;
        public static float DeathDischargeStatic = 0.5f;       // v0.9.16: dying with this much Static Electrocutes (0 = off)
        public static int ElectrocutesPerSecondCap = 4;
        public static int StormChargeMax = 5;                  // Electrocutes per Thunderbolt (v0.9.16 Stu: was 6)
        public static float ThunderboltDamageCoefficient = 10f;
        public static float ThunderboltSplashFraction = 0.5f;
        public static float ThunderboltSplashRadius = 3f;
        public static float ThunderboltRange = 30f;
        public static float ThunderboltTelegraphSeconds = 0.6f;    // v0.9.15: was 0.35 (the charges combine visibly)
        public static float ThunderboltFlightSeconds = 1.0f;       // v0.9.15: launch to strike, was a fixed 0.25
        public static float ThunderboltCooldown = 4f;
    }

    /// <summary>Which Hollow Saint skill produced a hit. Only our own code passes these.</summary>
    public enum HsDamageSource
    {
        None = 0,
        ArcBolt = 1,
        Stormspear = 2,
        OpenCircuit = 3
    }

    /// <summary>
    /// Everything the kit contributes to the game catalogs. Modules add to this during
    /// FoundationContent.LoadStaticContentAsync; GenerateContentPackAsync copies it into
    /// the mod's own ContentPack. One pack, one path. Nothing goes through R2API
    /// ContentAddition, so there is no second pack registered under the same plugin.
    /// </summary>
    public static class KitContent
    {
        private static readonly List<Type> states = new List<Type>();
        private static readonly List<SkillDef> skillDefs = new List<SkillDef>();
        private static readonly List<SkillFamily> skillFamilies = new List<SkillFamily>();
        private static readonly List<BuffDef> buffDefs = new List<BuffDef>();
        private static readonly List<GameObject> projectiles = new List<GameObject>();
        private static readonly List<EffectDef> effects = new List<EffectDef>();

        public static SerializableEntityStateType AddState(Type stateType)
        {
            if (!states.Contains(stateType)) states.Add(stateType);
            return new SerializableEntityStateType(stateType);
        }

        public static SkillDef AddSkillDef(SkillDef def)
        {
            if (def != null && !skillDefs.Contains(def)) skillDefs.Add(def);
            return def;
        }

        public static SkillFamily AddSkillFamily(SkillFamily family)
        {
            if (family != null && !skillFamilies.Contains(family)) skillFamilies.Add(family);
            return family;
        }

        public static BuffDef AddBuff(BuffDef buff)
        {
            if (buff != null && !buffDefs.Contains(buff)) buffDefs.Add(buff);
            return buff;
        }

        public static GameObject AddProjectile(GameObject prefab)
        {
            if (prefab != null && !projectiles.Contains(prefab)) projectiles.Add(prefab);
            return prefab;
        }

        public static void AddEffect(GameObject effectPrefab)
        {
            if (effectPrefab != null) effects.Add(new EffectDef(effectPrefab));
        }

        internal static void PopulateInto(ContentPack pack)
        {
            pack.entityStateTypes.Add(states.ToArray());
            pack.skillDefs.Add(skillDefs.ToArray());
            pack.skillFamilies.Add(skillFamilies.ToArray());
            pack.buffDefs.Add(buffDefs.ToArray());
            pack.projectilePrefabs.Add(projectiles.ToArray());
            pack.effectDefs.Add(effects.ToArray());
            Plugin.Log.LogInfo("HOLLOW_SAINT_KIT_CONTENT states=" + states.Count +
                " skillDefs=" + skillDefs.Count + " families=" + skillFamilies.Count +
                " buffs=" + buffDefs.Count + " projectiles=" + projectiles.Count +
                " effects=" + effects.Count);
        }

        /// <summary>Builds and registers a buff. BuffDef.name must be unique.</summary>
        public static BuffDef MakeBuff(string name, Color color, bool canStack, bool isDebuff, bool hidden, string icon = null)
        {
            var buff = ScriptableObject.CreateInstance<BuffDef>();
            buff.name = name;
            buff.buffColor = color;
            buff.canStack = canStack;
            buff.isDebuff = isDebuff;
            buff.isHidden = hidden;
            buff.iconSprite = icon != null ? KitIcons.Sprite(icon) : null;
            if (buff.iconSprite == null) buff.isHidden = true; // never show an empty HUD slot
            return AddBuff(buff);
        }
    }

    /// <summary>
    /// The Storm charge meter (Answered Prayer). The charge IS the stack count of a visible
    /// buff on the Saint (one stack per Electrocute, up to StormChargeMax), so it replicates
    /// to every client with no custom networking. Only the server mutates it. The component
    /// runs on every machine and plays the local presentation (charge tick, meter-full
    /// flourish) off stack-count changes. The name is historical: it used to be the
    /// Discharge meter, and BodyFx reads Normalized for the chest core.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DischargeMeter : MonoBehaviour
    {
        public static BuffDef ChargeBuff { get; private set; }

        private CharacterBody owner;
        private int lastSeen;
        private Gaze.GazeFuelLedger gazeFuel;
        // A full living merge is held for a future explicit claim/consume. Merely
        // rejecting a new charge at full does not re-arm the automatic passive.
        public bool AutoHeldFromMerge { get; private set; }
        public bool AutomaticHeld { get { return (gazeFuel != null && gazeFuel.Active) || AutoHeldFromMerge; } }

        internal static void RegisterBuff()
        {
            if (ChargeBuff != null) return;
            // Visible with its stack count: this is the player-facing Storm charge (0 to 6).
            ChargeBuff = KitContent.MakeBuff("bdHsStormCharge",
                new Color(0.3f, 0.92f, 1f), canStack: true, isDebuff: false, hidden: false, icon: "buff_discharge_charge");
        }

        public int Charge { get { return owner != null && ChargeBuff != null ? owner.GetBuffCount(ChargeBuff) : 0; } }
        public float Normalized { get { return Mathf.Clamp01(Charge / (float)Mathf.Max(1, KitTuning.StormChargeMax)); } }
        public bool IsFull { get { return Charge >= KitTuning.StormChargeMax; } }

        private void Awake()
        {
            owner = GetComponent<CharacterBody>();
        }

        /// <summary>Server only. Adds one charge. Returns true if this filled the meter.</summary>
        public bool AddCharge()
        {
            if (!NetworkServer.active || owner == null || ChargeBuff == null) return false;
            if (gazeFuel != null && gazeFuel.Active)
            {
                bool accepted = gazeFuel.TryGain();
                if (accepted)
                {
                    owner.SetBuffCount(ChargeBuff.buffIndex, gazeFuel.Reserve);
                    var driver = owner.GetComponent<Gaze.GazeFuelController>();
                    if (driver) driver.ReserveChanged();
                }
                else KitLog.Event("GAZE_FUEL_GAIN_REJECTED", "cast bank at capacity=" + gazeFuel.Capacity);
                return accepted && gazeFuel.Unspent + gazeFuel.Reserve == gazeFuel.Capacity;
            }
            if (IsFull) return false;
            owner.AddBuff(ChargeBuff);
            return IsFull;
        }

        internal void ClaimGazeFuel(Gaze.GazeFuelLedger ledger)
        {
            if (!NetworkServer.active || !owner || !ChargeBuff) return;
            ledger.Begin(Charge, KitTuning.StormChargeMax);
            gazeFuel = ledger;
            AutoHeldFromMerge = false;
            owner.SetBuffCount(ChargeBuff.buffIndex, 0);
        }

        internal int ReleaseGazeFuel(bool alive)
        {
            if (!NetworkServer.active || gazeFuel == null) return 0;
            int retained = gazeFuel.End(alive);
            AutoHeldFromMerge = gazeFuel.HoldAfterMerge;
            gazeFuel = null;
            if (owner && ChargeBuff) owner.SetBuffCount(ChargeBuff.buffIndex, retained);
            return retained;
        }

        /// <summary>Server only. Empties the meter.</summary>
        public void Consume()
        {
            if (!NetworkServer.active || owner == null || ChargeBuff == null) return;
            if (gazeFuel != null && gazeFuel.Active) return;
            AutoHeldFromMerge = false;
            owner.SetBuffCount(ChargeBuff.buffIndex, 0);
        }

        private void FixedUpdate()
        {
            if (owner == null) return;
            int now = Charge;
            if (Gaze.GazeFuelController.OwnsPresentation(owner)) { lastSeen = now; return; }
            if (now == lastSeen) return;
            int max = KitTuning.StormChargeMax;
            if (now >= max && lastSeen < max)
            {
                // Full: the "call the storm" gesture, only when no skill gesture is playing.
                if (KitAnim.UpperBodyIdle(owner))
                    KitAnim.PlayOnBody(owner, KitAnim.OverlayLayer, "Meter full flourish", 1f);
                Vfx.KitFx.Local(Vfx.Beat.MeterFull, owner, Vfx.KitFx.Socket(owner, "Core"));
            }
            else if (now > lastSeen && now < max)
            {
                Vfx.KitFx.Local(Vfx.Beat.ChargeTick, owner, Vfx.KitFx.Socket(owner, "Core"));
            }
            lastSeen = now;
        }
    }

    /// <summary>
    /// Animation helper that never warns. The game bundle currently ships a single-layer
    /// controller, so most kit states do not exist yet; EntityState.PlayAnimation on a
    /// missing layer logs "Invalid Layer Index -1" every shot. This checks first and
    /// logs each missing state once.
    /// </summary>
    public static class KitAnim
    {
        public const string BodyLayer = "Body";
        public const string UpperBodyLayer = "UpperBody";
        public const string OverlayLayer = "Overlay";
        public const string HaloLayer = "Halo";
        /// <summary>bundle06: arms-only gesture layer (no spine/chest) used while moving.</summary>
        public const string UpperArmsLayer = "UpperArms";
        public const string SpearCarryLayer = "SpearCarry";
        public const string PlaybackRateParam = "attackSpeed";

        private static readonly HashSet<string> reported = new HashSet<string>();

        public const string OverlayRateParam = "overlaySpeed";
        public const string HaloRateParam = "haloSpeed";

        // Per-Animator cache of float parameter name hashes, so the per-layer rate params can be
        // detected at runtime (old controller: only attackSpeed; new: overlaySpeed/haloSpeed).
        private static readonly Dictionary<Animator, HashSet<int>> paramCache = new Dictionary<Animator, HashSet<int>>();
        private static readonly int AttackSpeedHash = Animator.StringToHash(PlaybackRateParam);
        private static readonly int OverlaySpeedHash = Animator.StringToHash(OverlayRateParam);
        private static readonly int HaloSpeedHash = Animator.StringToHash(HaloRateParam);
        private const float PlayFade = 0.09f;
        private const float OtherLayerFade = 0.12f;

        private static HashSet<int> ParamsOf(Animator animator)
        {
            HashSet<int> set;
            if (paramCache.TryGetValue(animator, out set)) return set;
            // Drop entries for destroyed animators (Unity null) so the cache cannot grow forever.
            if (paramCache.Count > 16)
            {
                var dead = new List<Animator>();
                foreach (var kv in paramCache) if (kv.Key == null) dead.Add(kv.Key);
                for (int i = 0; i < dead.Count; i++) paramCache.Remove(dead[i]);
            }
            set = new HashSet<int>();
            var ps = animator.parameters;
            for (int i = 0; i < ps.Length; i++)
                if (ps[i].type == AnimatorControllerParameterType.Float) set.Add(ps[i].nameHash);
            paramCache[animator] = set;
            return set;
        }

        private static int RateParamFor(Animator animator, string layer)
        {
            int wanted = layer == SpearCarryLayer ? Animator.StringToHash("spearSpeed") : layer == OverlayLayer ? OverlaySpeedHash : layer == HaloLayer ? HaloSpeedHash : AttackSpeedHash;
            if (wanted != AttackSpeedHash && !ParamsOf(animator).Contains(wanted)) return AttackSpeedHash;
            return wanted;
        }

        private static readonly int EmptyHash = Animator.StringToHash("Empty");

        private static void FadeToEmpty(Animator animator, string layer, float fade)
        {
            int idx = animator.GetLayerIndex(layer);
            if (idx < 0 || !animator.HasState(idx, EmptyHash)) return;
            if (animator.GetCurrentAnimatorStateInfo(idx).shortNameHash == EmptyHash && !animator.IsInTransition(idx) &&
                PendingState(animator, idx) == 0) return;
            animator.CrossFadeInFixedTime(EmptyHash, fade, idx);
            MarkPending(animator, idx, EmptyHash);
        }

        /// <summary>True when no skill gesture is playing on the UpperBody or UpperArms layer.</summary>
        public static bool UpperBodyIdle(CharacterBody body)
        {
            if (body == null || body.modelLocator == null || body.modelLocator.modelTransform == null) return true;
            var animator = body.modelLocator.modelTransform.GetComponent<Animator>();
            if (animator == null) return true;
            return LayerIdle(animator, animator.GetLayerIndex(UpperBodyLayer)) &&
                LayerIdle(animator, animator.GetLayerIndex(UpperArmsLayer));
        }

        /// <summary>True when the layer (by index) is resting in "Empty" and not blending.
        /// A missing layer (index below 0) counts as idle.</summary>
        public static bool LayerIdle(Animator animator, int layerIndex)
        {
            if (animator == null || layerIndex < 0 || layerIndex >= animator.layerCount) return true;
            int requested = PendingState(animator, layerIndex);
            if (requested != 0) return requested == EmptyHash;
            return animator.GetCurrentAnimatorStateInfo(layerIndex).shortNameHash == EmptyHash && !animator.IsInTransition(layerIndex);
        }

        /// <summary>The arm layer a skill gesture should use right now: UpperArms while moving
        /// (ground speed above the walk threshold) or airborne, so the run torso and lean
        /// survive; UpperBody when standing or when the controller has no UpperArms layer.</summary>
        public static string GestureLayerFor(CharacterBody body, Animator animator)
        {
            if (animator == null || animator.GetLayerIndex(UpperArmsLayer) < 0) return UpperBodyLayer;
            if (body == null || body.characterMotor == null) return UpperBodyLayer;
            var motor = body.characterMotor;
            Vector3 v = motor.velocity;
            float planar = new Vector2(v.x, v.z).magnitude;
            return FoundationAnimRules.UseArmsLayer(planar, motor.isGrounded) ? UpperArmsLayer : UpperBodyLayer;
        }

        /// <summary>Plays a skill gesture on the arm layer chosen by GestureLayerFor and fades
        /// the other arm layer to Empty. Falls back to UpperBody when the state is missing on
        /// UpperArms (older controllers). Returns the layer used, or null if nothing played.</summary>
        public static string PlayGesture(CharacterBody body, Animator animator, string state, float duration)
        {
            if (animator == null) return null;
            string layer = GestureLayerFor(body, animator);
            if (layer == UpperArmsLayer && !animator.HasState(animator.GetLayerIndex(UpperArmsLayer), Animator.StringToHash(state)))
                layer = UpperBodyLayer;
            if (!Play(animator, layer, state, duration)) return null;
            FadeToEmpty(animator, layer == UpperArmsLayer ? UpperBodyLayer : UpperArmsLayer, OtherLayerFade);
            return layer;
        }

        /// <summary>True when the state exists on the named layer of this animator.</summary>
        public static bool HasState(Animator animator, string layer, string state)
        {
            if (animator == null) return false;
            int idx = animator.GetLayerIndex(layer);
            return idx >= 0 && animator.HasState(idx, Animator.StringToHash(state));
        }

        public static bool Play(Animator animator, string layer, string state, float duration)
        {
            if (animator == null) return false;
            int layerIndex = animator.GetLayerIndex(layer);
            int stateHash = Animator.StringToHash(state);
            if (layerIndex < 0 || !animator.HasState(layerIndex, stateHash))
            {
                if (reported.Add(layer + "/" + state))
                    Plugin.Log.LogInfo("HOLLOW_SAINT_ANIM_PENDING " + layer + "/" + state +
                        " (not in the current controller; skipped)");
                return false;
            }
            int rate = RateParamFor(animator, layer);
            // Skill gestures own the arms: the Overlay layer shares the upper-body mask and sits
            // above UpperBody, so a Discharge snap / flourish would hide every bolt and spear.
            if (layer == UpperBodyLayer || layer == UpperArmsLayer) FadeToEmpty(animator, OverlayLayer, 0.08f);
            // v0.8: never force an animator evaluation here. The vanilla helper calls
            // animator.Update(0) twice to read the clip length; doing that from LateUpdate (or
            // after the pose passes) re-evaluated the whole rig mid-frame and erased every
            // procedural accent for a frame. The length comes from the controller's clips instead,
            // and the request takes effect at the animator's normal evaluation.
            float length = ClipLength(animator, state);
            float speed = length > 0.001f ? length / Mathf.Max(0.01f, duration) : 1f;
            animator.SetFloat(rate, speed);
            // A weight-0 gesture layer holds a stale pose; start the clip outright and let
            // FoundationLayerWeights fade the layer in from the body pose instead.
            if (FoundationLayerWeights.Managed(layer) && animator.GetLayerWeight(layerIndex) < 0.05f)
                animator.PlayInFixedTime(stateHash, layerIndex, 0f);
            else animator.CrossFadeInFixedTime(stateHash, PlayFade, layerIndex, 0f);
            MarkPending(animator, layerIndex, stateHash);
            return true;
        }

        // ---- v0.8 request bookkeeping (no mid-frame evaluation) ----
        private static readonly Dictionary<RuntimeAnimatorController, Dictionary<string, float>> clipLengths =
            new Dictionary<RuntimeAnimatorController, Dictionary<string, float>>();
        private static readonly Dictionary<long, KeyValuePair<int, int>> pending = new Dictionary<long, KeyValuePair<int, int>>();

        /// <summary>Clip length for a state named after its clip ("Conduit Spear" = Conduit_Spear),
        /// read from the active (override) controller. 0 when unknown.</summary>
        public static float ClipLength(Animator animator, string state)
        {
            var controller = animator ? animator.runtimeAnimatorController : null;
            if (!controller) return 0f;
            Dictionary<string, float> map;
            if (!clipLengths.TryGetValue(controller, out map))
            {
                map = new Dictionary<string, float>();
                foreach (var clip in controller.animationClips)
                    if (clip) map[clip.name.Replace('_', ' ')] = clip.length;
                clipLengths[controller] = map;
            }
            float length;
            if (map.TryGetValue(state, out length)) return length;
            if (reported.Add("length/" + state))
                Plugin.Log.LogWarning("HOLLOW_SAINT_ANIM_LENGTH unknown clip for state '" + state + "'; playing at 1x");
            return 0f;
        }

        private static long PendingKey(Animator animator, int layer) => ((long)animator.GetInstanceID() << 8) | (uint)(layer & 0xff);

        private static void MarkPending(Animator animator, int layer, int stateHash)
        {
            pending[PendingKey(animator, layer)] = new KeyValuePair<int, int>(Time.frameCount, stateHash);
        }

        /// <summary>A request made this frame (or last, from LateUpdate) that the animator has not
        /// evaluated yet. Returns the requested state hash, or 0.</summary>
        public static int PendingState(Animator animator, int layer)
        {
            KeyValuePair<int, int> entry;
            if (!animator || !pending.TryGetValue(PendingKey(animator, layer), out entry)) return 0;
            if (Time.frameCount - entry.Key > 1) return 0;
            return entry.Value;
        }

        /// <summary>Returns a gesture layer to its "Empty" state with a cross-fade. Never use
        /// Play for this: EntityState.PlayAnimation sets the shared rate parameter to
        /// clipLength / duration, which is 0 for an empty state and freezes every gesture.</summary>
        public static void Stop(CharacterBody body, string layer, float fade = 0.2f)
        {
            if (body == null || body.modelLocator == null || body.modelLocator.modelTransform == null) return;
            var animator = body.modelLocator.modelTransform.GetComponent<Animator>();
            if (animator == null) return;
            int layerIndex = animator.GetLayerIndex(layer);
            int empty = Animator.StringToHash("Empty");
            if (layerIndex < 0 || !animator.HasState(layerIndex, empty)) return;
            animator.CrossFadeInFixedTime(empty, fade, layerIndex);
            MarkPending(animator, layerIndex, empty);
        }

        public static bool PlayOnBody(CharacterBody body, string layer, string state, float duration)
        {
            if (body == null || body.modelLocator == null || body.modelLocator.modelTransform == null) return false;
            return Play(body.modelLocator.modelTransform.GetComponent<Animator>(), layer, state, duration);
        }

        public static string PlayGestureOnBody(CharacterBody body, string state, float duration)
        {
            if (body == null || body.modelLocator == null || body.modelLocator.modelTransform == null) return null;
            return PlayGesture(body, body.modelLocator.modelTransform.GetComponent<Animator>(), state, duration);
        }

        /// <summary>Plays a skill gesture (arm layer by movement, like PlayGesture) whose exit hands
        /// off to a "recover" tail. bundle06 bakes the gesture -> recover chain into the controller
        /// (recover runs at 1x after the gesture), so this only checks the tail exists and logs once
        /// when it does not; without it the gesture returns to Empty as before.</summary>
        public static string PlayGestureWithRecover(CharacterBody body, Animator animator, string state, string recoverState, float duration)
        {
            string layer = PlayGesture(body, animator, state, duration);
            if (layer != null && !string.IsNullOrEmpty(recoverState) && !HasState(animator, layer, recoverState) &&
                reported.Add(layer + "/" + recoverState))
                Plugin.Log.LogInfo("HOLLOW_SAINT_ANIM_PENDING " + layer + "/" + recoverState +
                    " (no recover tail in the current controller; gesture returns to Empty)");
            return layer;
        }

        /// <summary>Moves a running gesture from one arm layer to the other at the same normalized
        /// time (used when movement changes mid-gesture). No-op when either layer is missing, the
        /// source is resting or blending, or the target lacks the state.</summary>
        public static bool MoveGesture(Animator animator, int fromLayer, int toLayer, float fade)
        {
            if (animator == null || fromLayer < 0 || toLayer < 0 || fromLayer == toLayer) return false;
            if (fromLayer >= animator.layerCount || toLayer >= animator.layerCount) return false;
            // FixedUpdate may have queued a fresh cast before presentation migrates
            // the previous pose. Wait for that request to evaluate instead of moving
            // stale state over the new cast (or its pending fade to Empty).
            if (PendingState(animator, fromLayer) != 0 || PendingState(animator, toLayer) != 0) return false;
            if (animator.IsInTransition(fromLayer) || animator.IsInTransition(toLayer)) return false;
            var info = animator.GetCurrentAnimatorStateInfo(fromLayer);
            if (info.shortNameHash == EmptyHash || !animator.HasState(toLayer, info.shortNameHash)) return false;
            if (animator.GetCurrentAnimatorStateInfo(toLayer).shortNameHash != EmptyHash) return false;
            float at = info.loop ? Mathf.Repeat(info.normalizedTime, 1f) : Mathf.Min(info.normalizedTime, 0.99f);
            // Same clip, same time on the other arm layer: start it outright (its weight fades in)
            // while the source layer fades out; no blend from a stale retained pose.
            if (animator.GetLayerWeight(toLayer) < 0.05f) animator.Play(info.shortNameHash, toLayer, at);
            else animator.CrossFade(info.shortNameHash, fade, toLayer, at);
            animator.CrossFadeInFixedTime(EmptyHash, fade, fromLayer);
            MarkPending(animator, toLayer, info.shortNameHash);
            MarkPending(animator, fromLayer, EmptyHash);
            return true;
        }

        public static Animator AnimatorOf(CharacterBody body)
        {
            if (body == null || body.modelLocator == null || body.modelLocator.modelTransform == null) return null;
            return body.modelLocator.modelTransform.GetComponent<Animator>();
        }
    }

    /// <summary>Shared helpers for sockets, aiming, attribution and capped AoE.</summary>
    public static class KitUtil
    {
        /// <summary>v0.9.9: the item events a native hit sends, for our manual DamageInfo hits.
        /// OnHitEnemy rolls on-hit items; OnHitAll is the event Brilliant Behemoth listens to.
        /// Vanilla BlastAttack and projectile impacts send both, and only for hits that were not
        /// rejected (blocked or immune), so this mirrors that. Proc 0 sends nothing.</summary>
        public static void ReportHit(DamageInfo info, GameObject victim)
        {
            if (info == null || victim == null || info.rejected || info.procCoefficient <= 0f || GlobalEventManager.instance == null) return;
            GlobalEventManager.instance.OnHitEnemy(info, victim);
            GlobalEventManager.instance.OnHitAll(info, victim);
        }

        /// <summary>Resolves a ChildLocator child by name, falling back to a transform
        /// search. Returns null rather than throwing.</summary>
        public static Transform ResolveSocket(CharacterBody body, string childName)
        {
            if (body == null) return null;
            var model = body.modelLocator != null ? body.modelLocator.modelTransform : null;
            if (model == null) return null;
            var locator = model.GetComponent<ChildLocator>();
            if (locator != null)
            {
                var found = locator.FindChild(childName);
                if (found != null) return found;
            }
            var nested = model.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nested.Length; i++)
                if (nested[i].name == childName) return nested[i];
            return null;
        }

        public static Vector3 EyePosition(CharacterBody body)
        {
            if (body != null && body.aimOriginTransform != null) return body.aimOriginTransform.position;
            return body != null ? ((Component)body).transform.position : Vector3.zero;
        }

        public static bool IsHollowSaint(CharacterBody body)
        {
            return body != null && body.baseNameToken == KitTokens.Name;
        }

        /// <summary>True when the attacker is a Hollow Saint body and the source is one
        /// of its skills.</summary>
        public static bool IsHollowSaintSkillDamage(GameObject attacker, HsDamageSource source)
        {
            if (source == HsDamageSource.None || attacker == null) return false;
            return IsHollowSaint(attacker.GetComponent<CharacterBody>());
        }

        /// <summary>Maps the vanilla skill damage source on a hit back to our skill. Our
        /// skills tag every hit Primary/Secondary/Special; item procs arrive as
        /// NoneSpecified. Single copy (it used to be duplicated in two files).</summary>
        public static HsDamageSource SourceOf(DamageInfo damageInfo)
        {
            if (damageInfo == null) return HsDamageSource.None;
            switch (damageInfo.damageType.damageSource)
            {
                case DamageSource.Primary: return HsDamageSource.ArcBolt;
                case DamageSource.Secondary: return HsDamageSource.Stormspear;
                case DamageSource.Special: return HsDamageSource.OpenCircuit;
                default: return HsDamageSource.None;
            }
        }

        /// <summary>Server only. Damages up to maxTargets distinct enemies within radius,
        /// nearest first. BlastAttack has no target cap, which is why this exists.
        /// Returns the hit positions (empty when nothing was struck).</summary>
        public static List<Vector3> CappedBlast(CharacterBody attacker, Vector3 origin, float radius, int maxTargets,
            float damage, bool crit, float procCoefficient, DamageTypeCombo damageType,
            DamageColorIndex color, bool linearFalloff, HealthComponent exclude = null, ProcChainMask procChainMask = default(ProcChainMask))
        {
            var hits = new List<Vector3>();
            if (!NetworkServer.active || attacker == null) return hits;
            var team = attacker.teamComponent != null ? attacker.teamComponent.teamIndex : TeamIndex.None;
            var search = new BullseyeSearch
            {
                searchOrigin = origin,
                searchDirection = Vector3.up,
                minAngleFilter = 0f,
                maxAngleFilter = 180f,
                minDistanceFilter = 0f,
                maxDistanceFilter = radius,
                teamMaskFilter = TeamMask.GetEnemyTeams(team),
                filterByLoS = false,
                filterByDistinctEntity = true,
                sortMode = BullseyeSearch.SortMode.Distance
            };
            search.RefreshCandidates();
            search.FilterOutGameObject(attacker.gameObject);

            foreach (var hurtBox in search.GetResults())
            {
                if (hits.Count >= maxTargets) break;
                var health = hurtBox != null ? hurtBox.healthComponent : null;
                if (health == null || !health.alive || health == exclude) continue;
                float scale = 1f;
                if (linearFalloff)
                {
                    float distance = Vector3.Distance(origin, hurtBox.transform.position);
                    scale = Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(distance / Mathf.Max(0.01f, radius)));
                }
                var info = new DamageInfo
                {
                    damage = damage * scale,
                    crit = crit,
                    attacker = attacker.gameObject,
                    inflictor = attacker.gameObject,
                    position = hurtBox.transform.position,
                    force = Vector3.zero,
                    procCoefficient = procCoefficient,
                    damageType = damageType,
                    damageColorIndex = color,
                    procChainMask = procChainMask,
                    inflictedHurtbox = hurtBox
                };
                health.TakeDamage(info);
                ReportHit(info, health.gameObject);
                hits.Add(hurtBox.transform.position);
            }
            return hits;
        }
    }

    /// <summary>Playtest evidence: logs the first few occurrences of each event, tagged
    /// with which machine it ran on, so a log shows every skill actually firing without
    /// flooding it at 2 shots per second.</summary>
    public static class KitLog
    {
        private const int PerEvent = 3;
        private static readonly Dictionary<string, int> counts = new Dictionary<string, int>();

        public static void Event(string name, string detail = null)
        {
            if (KitConfig.EventLog != null && !KitConfig.EventLog.Value && !DevAutopilot.Active) return;
            int n;
            counts.TryGetValue(name, out n);
            if (n >= PerEvent) return;
            counts[name] = n + 1;
            Plugin.Log.LogInfo("HOLLOW_SAINT_EVENT " + name + " #" + (n + 1) +
                " server=" + NetworkServer.active + (detail != null ? " " + detail : ""));
        }
    }

    /// <summary>Language token keys. The text is in Language/HollowSaint.language.
    /// Description tokens are templates; KitDescriptions fills the numbers.</summary>
    public static class KitTokens
    {
        public const string Name = "HS_NAME";
        public const string Subtitle = "HS_SUBTITLE";

        public const string ArcBoltName = "HS_SKILL_ARCBOLT_NAME";
        public const string ArcBoltDesc = "HS_SKILL_ARCBOLT_DESC";
        public const string ConduitSpearName = "HS_SKILL_SPEAR_NAME";
        public const string ConduitSpearDesc = "HS_SKILL_SPEAR_DESC";
        public const string ArcStepName = "HS_SKILL_ARCSTEP_NAME";
        public const string ArcStepDesc = "HS_SKILL_ARCSTEP_DESC";
        public const string OpenCircuitName = "HS_SKILL_CIRCUIT_NAME";
        public const string OpenCircuitDesc = "HS_SKILL_CIRCUIT_DESC";
        public const string StormName = "HS_PASSIVE_STORM_NAME";
        public const string StormDesc = "HS_PASSIVE_STORM_DESC";
        public const string KeywordStatic = "HS_KEYWORD_STATIC";
        public const string KeywordStorm = "HS_KEYWORD_STORM";
        public const string KeywordElectrocute = "HS_KEYWORD_ELECTROCUTE";
        public const string KeywordShocked = "HS_KEYWORD_SHOCKED";
    }
}
