using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// v0.8 aim layer. The torso, neck and head follow the crosshair pitch and the yaw the body has
    /// not yet turned to, using the authored aim-pose spread (66 degrees across spine 14, chest 10,
    /// neck 18, head 24; the authored pelvis share moves to the spine so the legs stay planted).
    /// Full strength in combat, a relaxed look-at out of combat. Restore-each-frame like the other
    /// pose passes; runs first (99) so lean and arm life layer on top.
    /// </summary>
    [DefaultExecutionOrder(99)]
    [DisallowMultipleComponent]
    public sealed class FoundationAimPose : MonoBehaviour
    {
        public static bool Enabled = true;
        private const float MaxPitch = 66f, MaxYaw = 60f;
        private static readonly float[] PitchShare = { 14f / 66f, 10f / 66f, 18f / 66f, 24f / 66f };
        private static readonly float[] YawShare = { 0.15f, 0.2f, 0.3f, 0.35f };
        private CharacterBody body;
        private FoundationPresentation presentation;
        private readonly Transform[] bones = new Transform[4]; // spine, chest, neck, head
        private readonly Quaternion[] saved = new Quaternion[4];
        private readonly Quaternion[] applied = new Quaternion[4];
        private bool hasSaved, hasApplied;
        private float pitch, yaw, torsoWeight;

        private void Start()
        {
            body = GetComponentInParent<CharacterBody>();
            presentation = GetComponent<FoundationPresentation>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "spine") bones[0] = t;
                else if (t.name == "chest") bones[1] = t;
                else if (t.name == "neck") bones[2] = t;
                else if (t.name == "head") bones[3] = t;
            }
            if (!body || !bones[0] || !bones[1] || !bones[2] || !bones[3]) enabled = false;
        }

        private void Update()
        {
            if (!hasSaved) return;
            for (int i = 0; i < 4; i++) bones[i].localRotation = saved[i];
            hasSaved = false;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                if (!hasApplied) return;
                for (int i = 0; i < 4; i++) { saved[i] = bones[i].localRotation; bones[i].localRotation = applied[i]; }
                hasSaved = true;
                return;
            }
            if (!Enabled || !presentation || !presentation.IsPresentingAlive || !body.inputBank) { pitch = yaw = 0f; hasApplied = false; return; }
            Vector3 aim = body.inputBank.aimDirection;
            if (aim.sqrMagnitude < 0.0001f || float.IsNaN(aim.sqrMagnitude)) return;
            aim.Normalize();
            Vector3 facing = body.characterDirection ? body.characterDirection.forward : transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) return;
            facing.Normalize();
            float targetPitch = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(aim.y, -1f, 1f)) * Mathf.Rad2Deg, -MaxPitch, MaxPitch);
            Vector3 flat = new Vector3(aim.x, 0f, aim.z);
            float targetYaw = flat.sqrMagnitude > 0.0001f ? Mathf.Clamp(Vector3.SignedAngle(facing, flat, Vector3.up), -MaxYaw, MaxYaw) : 0f;
            float follow = 1f - Mathf.Exp(-14f * Mathf.Min(dt, 0.1f));
            pitch = Mathf.Lerp(pitch, targetPitch, follow);
            yaw = Mathf.Lerp(yaw, targetYaw, follow);
            bool engaged = !body.outOfCombat || (body.inputBank.skill1.down || body.inputBank.skill2.down || body.inputBank.skill4.down);
            float wanted = engaged ? 1f : 0.35f;
            if (body.isSprinting) wanted *= 0.5f;
            torsoWeight = Mathf.MoveTowards(torsoWeight, wanted, dt / 0.25f);

            for (int i = 0; i < 4; i++) saved[i] = bones[i].localRotation;
            hasSaved = true;
            Vector3 side = Vector3.Cross(Vector3.up, facing);
            for (int i = 0; i < 4; i++)
            {
                // The neck and head always look; the spine and chest follow with the combat weight.
                float w = i < 2 ? torsoWeight : Mathf.Max(torsoWeight, 0.8f);
                // Positive pitch looks up: rotate about the body's left axis.
                Quaternion turn = Quaternion.AngleAxis(-pitch * PitchShare[i] * w, side) * Quaternion.AngleAxis(yaw * YawShare[i] * w, Vector3.up);
                bones[i].rotation = turn * bones[i].rotation;
            }
            for (int i = 0; i < 4; i++) applied[i] = bones[i].localRotation;
            hasApplied = true;
        }

        private void OnDisable() { Update(); hasApplied = false; }
    }
}
