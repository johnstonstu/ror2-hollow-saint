using System.Collections.Generic;
using HollowSaint.FoundationKit;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>Velocity-driven pose accents applied after animation, before heel emitters.</summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class FoundationMotionPose : MonoBehaviour
    {
        private FoundationPresentation presentation;
        private CharacterBody body;
        private Animator animator;
        private FoundationKit.SpearDischarge.SpearCarry carry;
        private int upperLayer = -1, armsLayer = -1, overlayLayer = -1;
        private Transform pelvis, spine, head, thighL, thighR, shinL, shinR, footL, footR;
        private readonly Dictionary<Transform, Quaternion> restore = new Dictionary<Transform, Quaternion>();
        private Vector3 previousVelocity, previousForward, previousFacing;
        private Vector3 leanVector;
        private Vector3 torsoLean;
        private float pitch, bank, acceleration, torsoBank, gestureWeight = 1f, glidePoseWeight;
        private bool sampled;

        private void Start()
        {
            presentation = GetComponent<FoundationPresentation>();
            body = GetComponentInParent<CharacterBody>();
            animator = GetComponent<Animator>();
            if (animator)
            {
                upperLayer = animator.GetLayerIndex(KitAnim.UpperBodyLayer);
                armsLayer = animator.GetLayerIndex(KitAnim.UpperArmsLayer);
                overlayLayer = animator.GetLayerIndex(KitAnim.OverlayLayer);
            }
            foreach (var bone in GetComponentsInChildren<Transform>(true))
            {
                switch (bone.name)
                {
                    case "pelvis": pelvis = bone; break;
                    case "spine": spine = bone; break;
                    case "head": head = bone; break;
                    case "L thigh": thighL = bone; break;
                    case "R thigh": thighR = bone; break;
                    case "L shin": shinL = bone; break;
                    case "R shin": shinR = bone; break;
                    case "L foot": footL = bone; break;
                    case "R foot": footR = bone; break;
                }
            }
            if (!body || !pelvis || !spine || !head || !thighL || !thighR || !shinL || !shinR || !footL || !footR)
            {
                if (body) Plugin.Log.LogWarning("HOLLOW_SAINT_MOTION_POSE missing a required posture bone; accents disabled.");
                enabled = false;
            }
        }

        // Restore only our last additive edits before Animator evaluates this frame.
        // This also prevents drift when animation is paused or a state omits a curve.
        private void Update() { Restore(); }

        private void LateUpdate() { TickPose(Time.deltaTime); }

        internal void TickPose(float rawDt)
        {
            if (!body || !presentation || !presentation.IsPresentingAlive) { Reset(); return; }
            if (rawDt <= 0f || float.IsNaN(rawDt) || float.IsInfinity(rawDt)) { HoldLastPose(); return; }
            float dt = Mathf.Min(rawDt, 0.1f);
            applied.Clear();
            Vector3 velocity = body.characterMotor.velocity;
            velocity.y = 0f;
            if (!Finite(velocity) || velocity.sqrMagnitude > 80f * 80f) { Reset(); return; }
            Vector3 forward = body.characterDirection ? body.characterDirection.forward : transform.forward;
            forward.y = 0f;
            if (!Finite(forward) || forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();
            float speed = velocity.magnitude;
            Vector3 travel = speed > 0.1f ? velocity / speed : forward;
            float turn = sampled ? Vector3.SignedAngle(previousForward, travel, Vector3.up) / rawDt : 0f;
            float facingTurn = sampled ? Vector3.SignedAngle(previousFacing, forward, Vector3.up) / rawDt : 0f;
            Vector3 change = sampled ? Vector3.ClampMagnitude((velocity - previousVelocity) / rawDt, 30f) : Vector3.zero;
            float surge = Vector3.Dot(change, travel);
            previousForward = travel;
            previousFacing = forward;
            previousVelocity = velocity;
            sampled = true;
            float follow = 1f - Mathf.Exp(-9f * dt);
            acceleration = Mathf.Lerp(acceleration, Mathf.Clamp(surge, -15f, 15f), follow);
            float thrust = Mathf.InverseLerp(2f, 12f, speed);
            float targetPitch = Mathf.Clamp(8f + 11f * thrust + acceleration * 0.3f, 3f, 22f);
            float targetBank = Mathf.Clamp(-turn * 0.035f, -13f, 13f);
            // Smooth the whole lean vector, not just its magnitude: a hard
            // reversal must pass through upright instead of flipping the axis
            // under a full-strength lean in one frame.
            leanVector = Vector3.Lerp(leanVector, travel * targetPitch, follow);
            pitch = leanVector.magnitude;
            Vector3 poseTravel = pitch > 0.001f ? leanVector / pitch : travel;
            Vector3 leanAxis = Vector3.Cross(Vector3.up, poseTravel);
            float directionalStrength = Mathf.Clamp01(pitch / 9f);
            bank = Mathf.Lerp(bank, targetBank, follow);
            // A dash can drop presentation GlideWeight in one frame. Let the additive
            // posture hand back to its authored dash pose over a short, bounded tail.
            glidePoseWeight = Mathf.MoveTowards(glidePoseWeight, presentation.GlideWeight, dt / 0.08f);
            TorsoAccent(change, facingTurn, speed, forward, dt);
            Impact(forward, dt);
            float weight = glidePoseWeight;
            if (weight < 0.001f) { RecordApplied(); return; }

            Quaternion feetLeft = footL.rotation, feetRight = footR.rotation;
            Save(pelvis); Save(spine); Save(head); Save(footL); Save(footR);
            Save(thighL); Save(thighR); Save(shinL); Save(shinR);
            // Pelvis lean carries the legs behind the hips; the torso adds a little
            // more drive while the gaze and ankles counter-rotate independently.
            pelvis.rotation = Quaternion.AngleAxis(pitch * 0.7f * weight, leanAxis) *
                Quaternion.AngleAxis(bank * weight * directionalStrength, poseTravel) * pelvis.rotation;
            spine.rotation = Quaternion.AngleAxis(pitch * 0.3f * weight, leanAxis) * spine.rotation;
            head.rotation = Quaternion.AngleAxis(-pitch * 0.4f * weight, leanAxis) * head.rotation;
            // Open the rear silhouette without stretching the rig or moving its
            // root. Existing entry/exit poses can already have deeply bent knees;
            // only add flex when below the glide target, and never straighten them.
            Vector3 lateral = Vector3.ProjectOnPlane(thighL.position - thighR.position, Vector3.up).normalized;
            ShapeGlideLeg(thighL, shinL, footL, lateral, 44f + 4f * thrust, weight);
            ShapeGlideLeg(thighR, shinR, footR, -lateral, 46f + 4f * thrust, weight);
            float ankle = (3f + 4f * thrust + acceleration * 0.12f) * weight * directionalStrength;
            footL.rotation = Quaternion.AngleAxis(ankle, leanAxis) * feetLeft;
            footR.rotation = Quaternion.AngleAxis(ankle, leanAxis) * feetRight;
            RecordApplied();
        }

        // v0.8: hold the last accent on zero-delta frames (pause) instead of dropping it.
        private readonly Dictionary<Transform, Quaternion> applied = new Dictionary<Transform, Quaternion>();
        private void RecordApplied()
        {
            applied.Clear();
            foreach (var entry in restore) if (entry.Key) applied[entry.Key] = entry.Key.localRotation;
        }
        private void HoldLastPose()
        {
            foreach (var entry in applied)
            {
                if (!entry.Key) continue;
                Save(entry.Key);
                entry.Key.localRotation = entry.Value;
            }
        }

        private void TorsoAccent(Vector3 change, float facingTurn, float speed, Vector3 forward, float dt)
        {
            float follow = 1f - Mathf.Exp(-10f * dt);
            float air = presentation.GroundedForPresentation ? 1f : 0.6f;
            torsoLean = Vector3.Lerp(torsoLean, Vector3.ClampMagnitude(change * (0.08f * air), 2.4f), follow);
            float targetBank = Mathf.Clamp(-facingTurn * 0.008f, -2.4f, 2.4f) * Mathf.InverseLerp(0.5f, 4f, speed) * air;
            torsoBank = Mathf.Lerp(torsoBank, targetBank, follow);
            bool casting = animator && (!KitAnim.LayerIdle(animator, upperLayer) ||
                !KitAnim.LayerIdle(animator, armsLayer) || !KitAnim.LayerIdle(animator, overlayLayer));
            if (!carry) carry = body.GetComponent<FoundationKit.SpearDischarge.SpearCarry>();
            casting |= carry && carry.Casting;
            gestureWeight = Mathf.MoveTowards(gestureWeight, casting ? 0.45f : 1f, dt / 0.16f);
            float weight = (1f - glidePoseWeight) * gestureWeight;
            if (weight < 0.001f || torsoLean.sqrMagnitude + torsoBank * torsoBank < 0.000001f) return;
            Save(spine); Save(head);
            Quaternion accent = Quaternion.AngleAxis(torsoLean.magnitude * weight, Vector3.Cross(Vector3.up, torsoLean)) *
                Quaternion.AngleAxis(torsoBank * weight, forward);
            spine.rotation = accent * spine.rotation;
            head.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Inverse(accent), 0.65f) * head.rotation;
        }

        // ---- v0.8 impacts: a damped spring that rocks the torso back (hit flinch, spear catch) ----
        private float kick, kickVelocity, lastHealth = -1f;
        private const float KickStiffness = 170f, KickDamping = 22f;

        /// <summary>Adds a backward torso impulse (degrees per second scale) on this body's model.</summary>
        internal static void Kick(CharacterBody who, float strength)
        {
            var model = who && who.modelLocator ? who.modelLocator.modelTransform : null;
            var pose = model ? model.GetComponent<FoundationMotionPose>() : null;
            if (pose) pose.kickVelocity += strength;
        }

        private void Impact(Vector3 forward, float dt)
        {
            var health = body.healthComponent;
            if (health)
            {
                float now = health.combinedHealth, full = Mathf.Max(1f, health.fullCombinedHealth);
                if (lastHealth >= 0f && now < lastHealth)
                {
                    float lost = (lastHealth - now) / full;
                    if (lost > 0.04f) kickVelocity += Mathf.Lerp(60f, 160f, Mathf.InverseLerp(0.04f, 0.3f, lost));
                }
                lastHealth = now;
            }
            float accel = -KickStiffness * kick - KickDamping * kickVelocity;
            kickVelocity += accel * dt;
            kick = Mathf.Clamp(kick + kickVelocity * dt, -12f, 12f);
            if (Mathf.Abs(kick) < 0.01f && Mathf.Abs(kickVelocity) < 0.1f) { kick = kickVelocity = 0f; return; }
            Vector3 axis = Vector3.Cross(Vector3.up, forward);
            Save(spine); Save(head);
            // Positive kick rocks the chest back; the head lags a little behind.
            spine.rotation = Quaternion.AngleAxis(-kick, axis) * spine.rotation;
            head.rotation = Quaternion.AngleAxis(kick * 0.35f, axis) * head.rotation;
        }

        private static bool Finite(Vector3 v) => !float.IsNaN(v.sqrMagnitude) && !float.IsInfinity(v.sqrMagnitude);

        private static void ShapeGlideLeg(Transform thigh, Transform shin, Transform foot,
            Vector3 outward, float targetBend, float weight)
        {
            thigh.rotation = Quaternion.AngleAxis(6f * weight, Vector3.Cross(Vector3.down, outward)) * thigh.rotation;
            Vector3 upper = (shin.position - thigh.position).normalized;
            Vector3 lower = (foot.position - shin.position).normalized;
            Vector3 hinge = Vector3.Cross(upper, lower);
            if (hinge.sqrMagnitude < 0.0001f) return;
            hinge.Normalize();
            float flex = Mathf.Clamp(targetBend - Vector3.Angle(upper, lower), 0f, 18f) * weight;
            // Derive the existing bend plane from joint positions: FBX bone-axis
            // conversion cannot reverse the knee, and each leg keeps its own plane.
            thigh.rotation = Quaternion.AngleAxis(-flex * 0.35f, hinge) * thigh.rotation;
            shin.rotation = Quaternion.AngleAxis(flex, hinge) * shin.rotation;
        }

        private void Save(Transform bone) { if (!restore.ContainsKey(bone)) restore.Add(bone, bone.localRotation); }
        private void Restore()
        {
            foreach (var entry in restore) if (entry.Key) entry.Key.localRotation = entry.Value;
            restore.Clear();
        }
        private void Reset()
        {
            Restore(); applied.Clear(); sampled = false; leanVector = torsoLean = Vector3.zero;
            acceleration = pitch = bank = torsoBank = glidePoseWeight = 0f; gestureWeight = 1f;
            kick = kickVelocity = 0f; lastHealth = -1f;
        }
        private void OnDisable() { Reset(); }
    }
}
