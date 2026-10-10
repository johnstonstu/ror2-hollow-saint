using System.Collections.Generic;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ArcStep;
using HollowSaint.FoundationKit.SpearDischarge;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// Procedural arm life layered after the Animator: breathing on chest and shoulders,
    /// calm idle sway, per-finger curl noise, wrist sway, and a spring chain that makes the
    /// arms react to the body's movement (acceleration lag, turn drag, sprint/glide trailing,
    /// airborne float, landing dip) with follow-through from shoulder to fingertips.
    /// Same restore-each-frame pattern as FoundationMotionPose: every edit is undone in Update
    /// before the Animator runs, so nothing accumulates; the springs live in
    /// FoundationArmLifeMath (tested offline by tools/tests/Check-ArmLife.ps1).
    /// While a skill gesture plays, the casting arm keeps only 25% of the layer so throws stay
    /// on their authored line; it blends back over 0.2 s. Off on death; the select-screen
    /// mannequin never gets this component (FoundationPresentation adds it only with a body).
    /// </summary>
    [DefaultExecutionOrder(101)]
    [DisallowMultipleComponent]
    public sealed class FoundationArmPose : MonoBehaviour
    {
        /// <summary>Config: master toggle (KitConfig "0. Movement" / "Arm life").</summary>
        public static bool LifeEnabled = true;
        /// <summary>Left arm is playing a gesture (Arc Bolt); the javelin guide arm yields to it.</summary>
        internal bool LeftCasting { get; private set; }
        /// <summary>Right arm is playing a gesture (the guide arm when the spear is in the left hand).</summary>
        internal bool RightCasting { get; private set; }
        /// <summary>Config: overall amplitude scale 0-2 (KitConfig "Arm life intensity").</summary>
        public static float LifeIntensity = 1f;
        /// <summary>Config: acceleration lag, turn drag and sprint/glide trailing, 0-2.</summary>
        public static float ReactionIntensity = 1f;
        /// <summary>Config: airborne float, jump and landing reaction, 0-2.</summary>
        public static float AirIntensity = 1f;
        /// <summary>Config: calm idle sway of the whole arm, 0-2.</summary>
        public static float IdleSwayIntensity = 1f;
        /// <summary>Config: delayed follow-through of elbow, wrist and fingers, 0-2.</summary>
        public static float FollowThrough = 1f;

        private const float FadeSeconds = 0.15f;
        private const float BreathHz = 0.25f;
        private const float FingerCurlDegrees = 6f;
        private const float WristSwayDegrees = 3.6f;
        private const float DashReaction = 0.5f;
        private const float TeleportSpeed = 80f;
        private static readonly string[] FingerNames = { "index", "middle", "ring", "little" };
        private static readonly float[] SegmentShare = { 0.45f, 0.35f, 0.2f };
        private static readonly int EmptyHash = Animator.StringToHash("Empty");
        private static Dictionary<int, int> oneHanded;

        private sealed class Arm
        {
            public Transform shoulder, upperarm, forearm, hand;
            public readonly Transform[,] fingers = new Transform[4, 3];
            public readonly float[] fingerSeed = new float[4];
            public readonly Quaternion[,] fingerLife = new Quaternion[4, 3];
            public float wristSeed, swaySeed, curlSign = 1f, fallbackSide;
            public readonly ArmChain chain = new ArmChain();
            public float cast = 1f;
            public float fingersWeight = 1f;
            // 1.3.2: remembered crown-hold pose (animator output) and how much of it is pinned.
            public Transform[] pinBones;
            public Quaternion[] pinPose;
            public float pin;
        }

        private FoundationPresentation presentation;
        private CharacterBody body;
        private Animator animator;
        private Transform chest, pelvis;
        private readonly Arm left = new Arm(), right = new Arm();
        private Transform[] saved;
        private Quaternion[] savedRotations;
        private bool hasSaved;
        private int upperBodyLayer = -1, upperArmsLayer = -1, overlayLayer = -1;
        private float weight, breathPhase;
        // Motion sampling (world space).
        private bool sampled, wasAirborne;
        private Vector3 lastPosition, filteredVelocity, previousFiltered, accel, previousForward;
        private float yawRate, airTime, fallSpeed;

        private void Start()
        {
            presentation = GetComponent<FoundationPresentation>();
            body = GetComponentInParent<CharacterBody>();
            animator = GetComponent<Animator>();
            foreach (var bone in GetComponentsInChildren<Transform>(true))
            {
                string n = bone.name;
                if (n == "chest") { chest = bone; continue; }
                if (n == "pelvis") { pelvis = bone; continue; }
                if (n.Length < 3 || n[1] != ' ' || (n[0] != 'L' && n[0] != 'R')) continue;
                Arm arm = n[0] == 'L' ? left : right;
                string part = n.Substring(2);
                switch (part)
                {
                    case "shoulder": arm.shoulder = bone; continue;
                    case "upperarm": arm.upperarm = bone; continue;
                    case "forearm": arm.forearm = bone; continue;
                    case "hand": arm.hand = bone; continue;
                }
                int dot = part.IndexOf('.');
                if (dot <= 0 || dot + 2 != part.Length) continue;
                int finger = System.Array.IndexOf(FingerNames, part.Substring(0, dot));
                int segment = part[dot + 1] - '1';
                if (finger >= 0 && segment >= 0 && segment < 3) arm.fingers[finger, segment] = bone;
            }
            if (!body || !Valid(left) || !Valid(right))
            {
                if (body) Plugin.Log.LogWarning("HOLLOW_SAINT_ARM_POSE missing a required arm bone; arm life disabled.");
                enabled = false;
                return;
            }
            // Desync every finger, wrist and arm; seeds differ per instance so two Saints never mirror.
            float baseSeed = (GetInstanceID() & 1023) * 0.37f;
            for (int i = 0; i < 4; i++) { left.fingerSeed[i] = baseSeed + 11.3f * i; right.fingerSeed[i] = baseSeed + 57.9f + 13.7f * i; }
            left.wristSeed = baseSeed + 101.1f; right.wristSeed = baseSeed + 203.7f;
            left.swaySeed = baseSeed * 0.11f; right.swaySeed = baseSeed * 0.11f + 2.4f;
            breathPhase = baseSeed;
            left.curlSign = 1f; right.curlSign = -1f;
            left.fallbackSide = -1f; right.fallbackSide = 1f;
            ClearFingerLife(left); ClearFingerLife(right);

            var list = new List<Transform>();
            if (chest) list.Add(chest);
            Collect(left, list); Collect(right, list);
            var pinList = new List<Transform>();
            Collect(left, pinList); left.pinBones = pinList.ToArray(); pinList.Clear();
            Collect(right, pinList); right.pinBones = pinList.ToArray();
            left.pinPose = new Quaternion[left.pinBones.Length];
            right.pinPose = new Quaternion[right.pinBones.Length];
            saved = list.ToArray();
            savedRotations = new Quaternion[saved.Length];
            if (animator)
            {
                upperBodyLayer = animator.GetLayerIndex(KitAnim.UpperBodyLayer);
                upperArmsLayer = animator.GetLayerIndex(KitAnim.UpperArmsLayer);
                overlayLayer = animator.GetLayerIndex(KitAnim.OverlayLayer);
            }
            if (oneHanded == null)
            {
                oneHanded = new Dictionary<int, int>();
                foreach (string state in FoundationArmLifeMath.OneHandedGestures)
                    oneHanded[Animator.StringToHash(state)] = FoundationArmLifeMath.CastingArm(state);
            }
        }

        private static bool Valid(Arm a) { return a.shoulder && a.upperarm && a.forearm && a.hand; }

        private static void Collect(Arm a, List<Transform> list)
        {
            list.Add(a.shoulder); list.Add(a.upperarm); list.Add(a.forearm); list.Add(a.hand);
            for (int f = 0; f < 4; f++) for (int s = 0; s < 3; s++) if (a.fingers[f, s]) list.Add(a.fingers[f, s]);
        }

        private static void ClearFingerLife(Arm a)
        {
            for (int f = 0; f < 4; f++) for (int s = 0; s < 3; s++) a.fingerLife[f, s] = Quaternion.identity;
        }

        // Restore our last edits before the Animator evaluates (prevents drift when a
        // state omits a curve or the animator is paused, e.g. frozen or hit-stopped).
        private void Update() { Restore(); }

        private void LateUpdate() { TickPose(Time.deltaTime, Time.time); }

        internal void TickPose(float rawDt, float time)
        {
            if (rawDt <= 0f || float.IsNaN(rawDt) || float.IsInfinity(rawDt)) { HoldLastPose(); return; }
            float dt = FoundationArmLifeMath.ClampDt(rawDt);
            bool alive = body && presentation && presentation.IsPresentingAlive;
            if (!alive) { ResetMotion(); weight = 0f; hasApplied = false; return; }

            Vector3 up = Vector3.up;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, up);
            if (forward.sqrMagnitude < 0.0001f) forward = previousForward.sqrMagnitude > 0.5f ? previousForward : Vector3.forward;
            forward.Normalize();
            Vector3 side = Vector3.Cross(up, forward); // body right
            ArmMotion motion = SampleMotion(rawDt, dt, forward, side);

            float target = LifeEnabled && LifeIntensity > 0.001f ? 1f : 0f;
            weight = Mathf.MoveTowards(weight, target, dt / FadeSeconds);
            if (weight < 0.001f)
            {
                left.chain.Reset(); right.chain.Reset();
                ClearFingerLife(left); ClearFingerLife(right);
                for (int i = 0; i < saved.Length; i++) savedRotations[i] = saved[i].localRotation;
                hasSaved = true;
                PinHeldArm(dt);
                SpearCarry.ApplyAim(body, time, rawDt);
                RecordApplied();
                return;
            }
            float w = Mathf.SmoothStep(0f, 1f, weight);

            // Casting arm keeps 25% of the layer while its gesture plays, 0.2 s blend back.
            int casting = CastingMask();
            LeftCasting = (casting & 1) != 0;
            RightCasting = (casting & 2) != 0;
            left.cast = FoundationArmLifeMath.CastWeight(left.cast, (casting & 1) != 0, dt);
            right.cast = FoundationArmLifeMath.CastWeight(right.cast, (casting & 2) != 0, dt);

            Vector3 root = pelvis ? pelvis.position : transform.position;
            float reaction = LifeIntensity * ReactionIntensity * (ArcStepState.IsBodyDashing(body) ? DashReaction : 1f);
            float air = LifeIntensity * AirIntensity;
            float sway = LifeIntensity * IdleSwayIntensity;
            float t = time;
            StepArm(left, motion, root, side, reaction, air, sway, t, dt);
            StepArm(right, motion, root, side, reaction, air, sway, t, dt);

            for (int i = 0; i < saved.Length; i++) savedRotations[i] = saved[i].localRotation;
            hasSaved = true;
            // After the save (so Restore returns the bones to the Animator pose) and before the
            // life layer (so breathing and follow-through still ride on the pinned arm).
            PinHeldArm(dt);

            breathPhase += dt * BreathHz * Mathf.PI * 2f;
            float breath = Mathf.Sin(breathPhase); // -1..1, inhale positive
            float noise = w * LifeIntensity;

            // Breathing: chest lifts/opens a touch, clavicles rise, upperarms counter so the
            // hands barely move.
            if (chest) chest.rotation = Quaternion.AngleAxis(-0.9f * breath * noise * Mathf.Min(left.cast, right.cast), side) * chest.rotation;
            Breathe(left, breath, noise * left.cast);
            Breathe(right, breath, noise * right.cast);

            ApplyArm(left, t, w * left.cast, noise * left.cast, root, forward, side, dt);
            ApplyArm(right, t, w * right.cast, noise * right.cast, root, forward, side, dt);
            SpearCarry.ApplyAim(body, time, rawDt);
            RecordApplied();
        }

        /// <summary>Body-frame motion. The local player reads its motor; remote bodies (and a
        /// host's view of clients) differentiate the interpolated body position instead.</summary>
        private ArmMotion SampleMotion(float rawDt, float dt, Vector3 forward, Vector3 side)
        {
            Vector3 position = ((Component)body).transform.position;
            Vector3 velocity;
            if (body.hasEffectiveAuthority && body.characterMotor) velocity = body.characterMotor.velocity;
            else velocity = sampled ? (position - lastPosition) / rawDt : Vector3.zero;
            lastPosition = position;
            bool airborne = !presentation.GroundedForPresentation;
            if (float.IsNaN(velocity.sqrMagnitude) || float.IsInfinity(velocity.sqrMagnitude) || velocity.sqrMagnitude > TeleportSpeed * TeleportSpeed)
            {
                // Teleport or respawn snap: start over rather than fling the arms.
                sampled = false;
                velocity = Vector3.zero;
                left.chain.Reset(); right.chain.Reset();
            }

            if (!sampled)
            {
                filteredVelocity = previousFiltered = velocity;
                accel = Vector3.zero;
                yawRate = 0f;
                previousForward = forward;
                wasAirborne = airborne;
                airTime = fallSpeed = 0f;
                sampled = true;
            }
            else
            {
                filteredVelocity = Vector3.Lerp(filteredVelocity, velocity, 1f - Mathf.Exp(-14f * dt));
                Vector3 rawAccel = (filteredVelocity - previousFiltered) / rawDt;
                previousFiltered = filteredVelocity;
                float follow = 1f - Mathf.Exp(-10f * dt);
                accel = Vector3.Lerp(accel, Vector3.ClampMagnitude(rawAccel, FoundationArmLifeMath.AccelClamp), follow);
                float turn = Mathf.Clamp(Vector3.SignedAngle(previousForward, forward, Vector3.up) / rawDt, -720f, 720f);
                yawRate = Mathf.Lerp(yawRate, turn, follow);
                previousForward = forward;

                // Jump and landing impulses go straight into the springs (frame-rate independent).
                float airAmount = LifeIntensity * AirIntensity;
                if (airborne)
                {
                    if (!wasAirborne && velocity.y > 4f)
                    {
                        float kick = FoundationArmLifeMath.TakeoffKick(velocity.y, airAmount);
                        FoundationArmLifeMath.Kick(left.chain, 0f, kick);
                        FoundationArmLifeMath.Kick(right.chain, 0f, kick);
                    }
                    airTime += rawDt;
                    fallSpeed = Mathf.Max(fallSpeed - 40f * dt, -velocity.y);
                }
                else
                {
                    if (wasAirborne && airTime > 0.12f && fallSpeed > 1f)
                    {
                        float kick = FoundationArmLifeMath.LandingKick(fallSpeed, airAmount);
                        FoundationArmLifeMath.Kick(left.chain, kick * 0.3f, kick);
                        FoundationArmLifeMath.Kick(right.chain, kick * 0.3f, kick);
                    }
                    airTime = fallSpeed = 0f;
                }
                wasAirborne = airborne;
            }

            ArmMotion m = new ArmMotion();
            m.ForwardAccel = Vector3.Dot(accel, forward);
            m.RightAccel = Vector3.Dot(accel, side);
            m.YawRate = yawRate;
            m.Speed = new Vector2(filteredVelocity.x, filteredVelocity.z).magnitude;
            m.Glide = presentation.GlideWeight;
            m.Airborne = airborne;
            m.VerticalSpeed = filteredVelocity.y;
            return m;
        }

        private void StepArm(Arm a, ArmMotion m, Vector3 root, Vector3 side, float reaction, float air,
            float sway, float t, float dt)
        {
            float lateral = Vector3.Dot(a.upperarm.position - root, side);
            float armSide = Mathf.Abs(lateral) > 0.02f ? Mathf.Sign(lateral) : a.fallbackSide;
            float swing, spread, swaySwing, swaySpread;
            FoundationArmLifeMath.Targets(m, armSide, reaction, air, out swing, out spread);
            FoundationArmLifeMath.Sway(t, a.swaySeed, sway, out swaySwing, out swaySpread);
            FoundationArmLifeMath.StepChain(a.chain, swing + swaySwing, spread + swaySpread, dt);
            a.fallbackSide = armSide;
        }

        // ---- 1.3.2: Arc Bolt over a held crown pose (see FoundationLayerWeights) ----
        private const float PinInFallback = 0.1f, PinOut = 0.12f;
        private FoundationLayerWeights layerWeights;
        private bool weightsResolved, pinCaptured;
        // The crown clips, sampled for the off arm at the time the hold has reached, and the model
        // hierarchy saved around each sample (the clip writes every bone it animates).
        private AnimationClip castClip, holdClip;
        private bool clipsResolved;
        private Transform[] sampleBones;
        private Vector3[] samplePos, sampleScale;
        private Quaternion[] sampleRot;

        /// <summary>The bolt replaces the crown hold on its layer, so the Animator no longer
        /// produces the hold for the non-throwing arm, and the bolt clip's own off-arm pose hangs at
        /// the side. While a bolt stands in for the hold (and while the arms blend back into it)
        /// this puts the other arm on the hold clip's pose at the time the hold has reached (it
        /// keeps rising through a Gaze charge). A remembered steady pose is the fallback when the
        /// clip cannot be sampled. After the save (so Restore returns the Animator pose) and before
        /// the life layer, so breathing and follow-through still ride on the held arm.</summary>
        private void PinHeldArm(float dt)
        {
            DebugPinArm = 0; DebugPinSampled = false; DebugSampleError = -1f;
            if (!weightsResolved) { layerWeights = GetComponent<FoundationLayerWeights>(); weightsResolved = true; }
            if (!layerWeights) return;
            int holdHash; float holdTime;
            int bolt = layerWeights.PinTarget(out holdHash, out holdTime);
            DebugPinArm = bolt;
            if (bolt == 0)
            {
                if (layerWeights.HoldSteady(out holdHash, out holdTime))
                {
                    if (DebugCompare) DebugSampleError = SampleError(holdHash, holdTime);
                    Capture(left); Capture(right); pinCaptured = true;
                }
                else if (!layerWeights.HoldPresent()) pinCaptured = false;
                if (left.pin > 0f) Pin(left, false, false, dt);
                if (right.pin > 0f) Pin(right, false, false, dt);
                return;
            }
            Arm held = bolt > 0 ? left : right, throwing = bolt > 0 ? right : left;
            bool sampled = SampleHold(holdHash, holdTime, held);
            DebugPinSampled = sampled;
            // The sample is the pose the Animator showed just before the cut, so it applies at
            // once; easing it in let the arm dip toward the bolt clip's off-arm pose for a few frames.
            Pin(held, sampled || pinCaptured, sampled, dt);
            if (throwing.pin > 0f) Pin(throwing, false, false, dt);
        }

        private static void Capture(Arm a)
        {
            for (int i = 0; i < a.pinBones.Length; i++) a.pinPose[i] = a.pinBones[i].localRotation;
        }

        private void ResolveClips()
        {
            clipsResolved = true;
            var controller = animator ? animator.runtimeAnimatorController : null;
            if (!controller || animator.isHuman) return;
            foreach (var clip in controller.animationClips)
            {
                if (!clip) continue;
                string n = clip.name.Replace('_', ' ');
                if (n == FoundationKit.OpenCircuit.OpenCircuitTuning.CastArmsState) castClip = clip;
                else if (n == FoundationKit.OpenCircuit.OpenCircuitTuning.HoldArmsState) holdClip = clip;
            }
            sampleBones = GetComponentsInChildren<Transform>(true);
            samplePos = new Vector3[sampleBones.Length];
            sampleScale = new Vector3[sampleBones.Length];
            sampleRot = new Quaternion[sampleBones.Length];
        }

        /// <summary>Writes the crown clip's pose at holdTime (normalized) for one arm into its pinPose.</summary>
        private bool SampleHold(int holdHash, float holdTime, Arm a)
        {
            if (!clipsResolved) ResolveClips();
            var clip = holdHash == HoldArmsHash ? holdClip : holdHash == CastArmsHash ? castClip : null;
            if (!clip) return false;
            for (int i = 0; i < sampleBones.Length; i++)
            {
                var t = sampleBones[i];
                if (!t) continue;
                samplePos[i] = t.localPosition; sampleRot[i] = t.localRotation; sampleScale[i] = t.localScale;
            }
            clip.SampleAnimation(gameObject, Mathf.Clamp01(holdTime) * clip.length);
            for (int i = 0; i < a.pinBones.Length; i++) a.pinPose[i] = a.pinBones[i].localRotation;
            for (int i = 0; i < sampleBones.Length; i++)
            {
                var t = sampleBones[i];
                if (!t) continue;
                t.localPosition = samplePos[i]; t.localRotation = sampleRot[i]; t.localScale = sampleScale[i];
            }
            return true;
        }

        private static readonly int CastArmsHash = Animator.StringToHash(FoundationKit.OpenCircuit.OpenCircuitTuning.CastArmsState);
        private static readonly int HoldArmsHash = Animator.StringToHash(FoundationKit.OpenCircuit.OpenCircuitTuning.HoldArmsState);

        // ---- dev diagnostics (DevAutopilot) ----
        internal static bool DebugCompare;
        internal float DebugSampleError = -1f;
        internal int DebugPinArm;
        internal bool DebugPinSampled;
        internal float DebugPinLeft { get { return left.pin; } }
        internal float DebugPinRight { get { return right.pin; } }

        /// <summary>Dev: largest angle (degrees) between the Animator's left-arm pose on a steady
        /// hold and the sampled clip at the same time. Near zero when sampling matches the Animator.</summary>
        private float SampleError(int holdHash, float holdTime)
        {
            if (!SampleHold(holdHash, holdTime, left)) return -1f;
            float worst = 0f;
            for (int i = 0; i < left.pinBones.Length; i++)
                worst = Mathf.Max(worst, Quaternion.Angle(left.pinBones[i].localRotation, left.pinPose[i]));
            return worst;
        }

        private static void Pin(Arm a, bool held, bool instant, float dt)
        {
            a.pin = held && instant ? 1f : Mathf.MoveTowards(a.pin, held ? 1f : 0f, dt / (held ? PinInFallback : PinOut));
            if (a.pin <= 0f) return;
            float w = Mathf.SmoothStep(0f, 1f, a.pin);
            for (int i = 0; i < a.pinBones.Length; i++)
                a.pinBones[i].localRotation = Quaternion.Slerp(a.pinBones[i].localRotation, a.pinPose[i], w);
        }

        private SpearCarry carryCache;
        private SpearCarry Carry()
        {
            if (!carryCache && body) carryCache = body.GetComponent<SpearCarry>();
            return carryCache;
        }

        /// <summary>Bit 1: left arm casting, bit 2: right arm. Known one-handed gestures
        /// (Arc Bolt left/right, Conduit Spear) mark only their arm; anything else both.</summary>
        private int CastingMask()
        {
            if (!animator) return 0;
            var carry = Carry();
            return LayerMask(upperBodyLayer) | LayerMask(upperArmsLayer) | LayerMask(overlayLayer) | (carry && carry.Casting ? (carry.Left ? 1 : 2) : 0);
        }

        private int LayerMask(int layer)
        {
            if (KitAnim.LayerIdle(animator, layer)) return 0;
            int mask = HashMask(animator.GetCurrentAnimatorStateInfo(layer).shortNameHash);
            if (animator.IsInTransition(layer)) mask |= HashMask(animator.GetNextAnimatorStateInfo(layer).shortNameHash);
            return mask;
        }

        private static int HashMask(int hash)
        {
            if (hash == EmptyHash) return 0;
            int arm;
            if (oneHanded != null && oneHanded.TryGetValue(hash, out arm) && arm != 0) return arm < 0 ? 1 : 2;
            return 3;
        }

        private static void Breathe(Arm a, float breath, float w)
        {
            Vector3 lateral = Vector3.ProjectOnPlane(a.upperarm.position - a.shoulder.position, Vector3.up);
            if (lateral.sqrMagnitude < 0.000001f) return;
            // AngleAxis(angle, Cross(a, b)) turns a toward b: lift the shoulder outward-up.
            Vector3 axis = Vector3.Cross(lateral.normalized, Vector3.up);
            float lift = 1.2f * breath * w;
            a.shoulder.rotation = Quaternion.AngleAxis(lift, axis) * a.shoulder.rotation;
            a.upperarm.rotation = Quaternion.AngleAxis(-lift * 0.8f, axis) * a.upperarm.rotation;
        }

        /// <summary>Turns a limb by swing degrees toward the body's forward and by spread degrees
        /// toward outward-up. Axes come from the limb's current direction, so the same numbers
        /// read correctly on any authored pose; they fade out as the limb nears the target
        /// direction instead of flipping.</summary>
        private static void Bend(Transform bone, Vector3 dir, float swing, float spread, Vector3 forward, Vector3 outwardUp)
        {
            if (dir.sqrMagnitude < 0.000001f) return;
            dir.Normalize();
            Quaternion q = Quaternion.identity;
            if (Mathf.Abs(swing) > 0.01f)
            {
                Vector3 axis = Vector3.Cross(dir, forward);
                float m = axis.magnitude;
                if (m > 0.05f) q = Quaternion.AngleAxis(swing * m, axis / m) * q;
            }
            if (Mathf.Abs(spread) > 0.01f)
            {
                Vector3 axis = Vector3.Cross(dir, outwardUp);
                float m = axis.magnitude;
                if (m > 0.05f) q = Quaternion.AngleAxis(spread * m, axis / m) * q;
            }
            bone.rotation = q * bone.rotation;
        }

        private void ApplyArm(Arm a, float t, float react, float noise, Vector3 root, Vector3 forward, Vector3 side, float dt)
        {
            ArmChain c = a.chain;
            float ft = FollowThrough;
            float armSide = a.fallbackSide;
            Vector3 outwardUp = (side * armSide + Vector3.up * 0.6f).normalized;

            // Shoulder -> elbow: each joint adds its own late share of the trailing.
            Bend(a.upperarm, a.forearm.position - a.upperarm.position, c.LocalSwing(0, ft) * react, c.LocalSpread(0, ft) * react, forward, outwardUp);
            Bend(a.forearm, a.hand.position - a.forearm.position, c.LocalSwing(1, ft) * react, c.LocalSpread(1, ft) * react, forward, outwardUp);

            // Wrist: slow Perlin sway plus the hand's follow-through.
            Transform finger0 = a.fingers[1, 0] ? a.fingers[1, 0] : a.fingers[0, 0];
            Vector3 handDir = finger0 ? finger0.position - a.hand.position : a.hand.position - a.forearm.position;
            if (handDir.sqrMagnitude < 0.000001f) return;
            handDir.Normalize();
            Vector3 across = AcrossKnuckles(a, handDir);
            float swayA = (Mathf.PerlinNoise(a.wristSeed, t * 0.23f) - 0.5f) * 2f * WristSwayDegrees;
            float swayB = (Mathf.PerlinNoise(a.wristSeed + 7.7f, t * 0.19f) - 0.5f) * 2f * WristSwayDegrees * 0.6f;
            a.hand.rotation = Quaternion.AngleAxis(swayA * noise, across) *
                Quaternion.AngleAxis(swayB * noise, Vector3.Cross(handDir, across)) * a.hand.rotation;
            Bend(a.hand, handDir, c.LocalSwing(2, ft) * react, c.LocalSpread(2, ft) * react, forward, outwardUp);
            var carry = Carry();
            if (carry && carry.Gripping && a == (carry.Left ? left : right)) { a.fingersWeight = 0f; ClearFingerLife(a); return; }
            // Preserve the exact molded grasp while held. When it opens, introduce
            // finger life gradually instead of switching noise/follow-through on at release.
            a.fingersWeight = Mathf.MoveTowards(a.fingersWeight, 1f, dt / 0.2f);
            float fingers = Mathf.SmoothStep(0f, 1f, a.fingersWeight);
            react *= fingers; noise *= fingers;

            // Fingers: desynced slow curl noise around the animated pose plus trailing, split
            // over the three segments. Trailing is projected onto the curl hinge so fingers
            // never bend sideways, and curling in is halved so the hands stay open.
            handDir = finger0 ? finger0.position - a.hand.position : handDir;
            if (handDir.sqrMagnitude < 0.000001f) return;
            handDir.Normalize();
            across = AcrossKnuckles(a, handDir);
            UpdateCurlSign(a, across);
            Vector3 curlAxis = across * a.curlSign;
            Vector3 trail = Vector3.Cross(handDir, forward) * (c.LocalSwing(3, ft) * react) +
                Vector3.Cross(handDir, outwardUp) * (c.LocalSpread(3, ft) * react);
            float trailCurl = Vector3.Dot(trail, curlAxis);
            if (trailCurl > 0f) trailCurl *= 0.5f;
            float fingerFollow = 1f - Mathf.Exp(-32f * dt);
            for (int f = 0; f < 4; f++)
            {
                float n = Mathf.PerlinNoise(a.fingerSeed[f], t * (0.3f + 0.05f * f)) - 0.5f;
                float curl = (n * 2f * FingerCurlDegrees + 1f) * noise + trailCurl; // slight bias toward curling in
                for (int s = 0; s < 3; s++)
                {
                    Transform seg = a.fingers[f, s];
                    if (!seg) continue;
                    // Follow the hinge in joint-local space so a new authored hand
                    // pose doesn't redirect the additive curl in a single frame.
                    // Only this small life offset is filtered; animation stays exact.
                    Quaternion target = Quaternion.AngleAxis(curl * SegmentShare[s] * 2f, seg.InverseTransformDirection(curlAxis));
                    a.fingerLife[f, s] = Quaternion.Slerp(a.fingerLife[f, s], target, fingerFollow);
                    seg.localRotation *= a.fingerLife[f, s];
                }
            }
        }

        private static Vector3 AcrossKnuckles(Arm a, Vector3 handDir)
        {
            Transform i = a.fingers[0, 0], l = a.fingers[3, 0];
            Vector3 across = i && l ? i.position - l.position : Vector3.Cross(handDir, Vector3.up);
            across = Vector3.ProjectOnPlane(across, handDir);
            return across.sqrMagnitude > 0.0000001f ? across.normalized : Vector3.Cross(handDir, Vector3.forward).normalized;
        }

        private static void UpdateCurlSign(Arm a, Vector3 across)
        {
            // Sum the animated bend hinge of each finger; when it is clearly bent, its
            // sign relative to the knuckle axis tells which way "curl in" is.
            float sum = 0f;
            for (int f = 0; f < 4; f++)
            {
                Transform s0 = a.fingers[f, 0], s1 = a.fingers[f, 1], s2 = a.fingers[f, 2];
                if (!s0 || !s1 || !s2) continue;
                Vector3 d1 = s1.position - s0.position, d2 = s2.position - s1.position;
                sum += Vector3.Dot(Vector3.Cross(d1.normalized, d2.normalized), across);
            }
            if (Mathf.Abs(sum) > 0.15f) a.curlSign = Mathf.Sign(sum);
        }

        private void ResetMotion()
        {
            sampled = false;
            left.chain.Reset(); right.chain.Reset();
            ClearFingerLife(left); ClearFingerLife(right);
            left.cast = right.cast = 1f;
            left.pin = right.pin = 0f;
            pinCaptured = false;
            accel = Vector3.zero;
            yawRate = airTime = fallSpeed = 0f;
        }

        // v0.8: a zero-delta frame (solo pause, timeScale 0) used to return before re-applying,
        // so the arm life, grip aim and breathing vanished while paused. Hold the last result.
        private Quaternion[] appliedRotations;
        private bool hasApplied;
        private void RecordApplied()
        {
            if (appliedRotations == null || appliedRotations.Length != saved.Length) appliedRotations = new Quaternion[saved.Length];
            for (int i = 0; i < saved.Length; i++) if (saved[i]) appliedRotations[i] = saved[i].localRotation;
            hasApplied = true;
        }
        private void HoldLastPose()
        {
            if (!hasApplied || saved == null) return;
            for (int i = 0; i < saved.Length; i++)
                if (saved[i]) { savedRotations[i] = saved[i].localRotation; saved[i].localRotation = appliedRotations[i]; }
            hasSaved = true;
        }

        private void Restore()
        {
            if (!hasSaved) return;
            for (int i = 0; i < saved.Length; i++) if (saved[i]) saved[i].localRotation = savedRotations[i];
            hasSaved = false;
        }

        private void OnDisable() { Restore(); weight = 0f; ResetMotion(); }
    }
}
