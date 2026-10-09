using RoR2;
using UnityEngine;
using HollowSaint.FoundationKit.ChargedStorm;

namespace HollowSaint.FoundationKit.HollowedOrb
{
    /// <summary>Additive two-bone casting pose; restores before Animator and after every exit.</summary>
    [DefaultExecutionOrder(135)]
    public sealed class HollowedOrbPose : MonoBehaviour
    {
        private CharacterBody body;
        private readonly Transform[] bones = new Transform[6];
        private readonly Quaternion[] saved = new Quaternion[6];
        private bool dirty, gathering, pushing, fired;
        private int charges;
        private float weight, releasedAt;
        internal static HollowedOrbPose For(CharacterBody body)
        {
            var model = body && body.modelLocator ? body.modelLocator.modelTransform : null;
            if (!model) return null;
            var pose = model.GetComponent<HollowedOrbPose>() ?? model.gameObject.AddComponent<HollowedOrbPose>();
            pose.body = body; return pose;
        }
        private void Awake()
        {
            string[] names = { "L upperarm", "L forearm", "L hand", "R upperarm", "R forearm", "R hand" };
            foreach (var t in GetComponentsInChildren<Transform>(true))
                for (int i = 0; i < names.Length; i++) if (t.name == names[i]) bones[i] = t;
        }
        internal void Gather(int count) { gathering = true; pushing = fired = false; charges = count; }
        internal void Release(bool launched) { gathering = false; pushing = fired = launched; releasedAt = Time.time; }
        private void Restore()
        {
            if (!dirty) return; dirty = false;
            for (int i = 0; i < bones.Length; i++) if (bones[i]) bones[i].localRotation = saved[i];
        }
        private void Update() { Restore(); }
        private void LateUpdate()
        {
            if (!body || !body.healthComponent || !body.healthComponent.alive) { gathering = pushing = false; }
            if (body && Stormspear.StormspearCharge.InCrown(body)) { gathering = pushing = false; }
            if (pushing && Time.time - releasedAt > .25f) pushing = false;
            weight = Mathf.MoveTowards(weight, gathering || pushing ? 1f : 0f, Time.deltaTime * 4f);
            if (weight <= 0f || !body) return;
            for (int i = 0; i < bones.Length; i++) if (!bones[i])
            {
                Plugin.Log.LogWarning("HOLLOW_SAINT_HOLLOWED_ORB_POSE missing required arm bone index=" + i); enabled = false; return;
            }
            for (int i = 0; i < bones.Length; i++) saved[i] = bones[i].localRotation;
            dirty = true;
            var aim = body.inputBank ? body.inputBank.aimDirection.normalized : body.gameObject.transform.forward;
            float reach = fired ? Mathf.SmoothStep(0f, 1f, (Time.time - releasedAt) / .25f) * Mathf.SmoothStep(0f, 1f, weight) : 0f;
            Vector3 center = Vector3.Lerp(OrbCastGeometry.Point(body, aim, charges), OrbCastGeometry.Point(body, aim, charges, true), reach);
            float half = ChargedStormTuning.Diameter(charges) * .5f;
            Solve(0, center - body.gameObject.transform.right * (half + .06f), -body.gameObject.transform.right);
            Solve(3, center + body.gameObject.transform.right * (half + .06f), body.gameObject.transform.right);
        }
        private void Solve(int i, Vector3 target, Vector3 outward)
        {
            Transform upper = bones[i], lower = bones[i + 1], hand = bones[i + 2];
            Vector3 start = upper.position, axis = target - start;
            float a = Vector3.Distance(start, lower.position), b = Vector3.Distance(lower.position, hand.position);
            float length = Mathf.Clamp(axis.magnitude, Mathf.Abs(a - b) + .01f, a + b - .01f);
            if (a < .02f || b < .02f || axis.sqrMagnitude < .001f) return;
            Vector3 forward = axis.normalized;
            float projection = (a * a - b * b + length * length) / (2f * length);
            Vector3 bend = Vector3.ProjectOnPlane(outward + Vector3.down * .65f, forward).normalized;
            Vector3 elbow = start + forward * projection + bend * Mathf.Sqrt(Mathf.Max(0f, a * a - projection * projection));
            Quaternion upperTarget = Quaternion.FromToRotation(lower.position - start, elbow - start) * upper.rotation;
            float blend = Mathf.SmoothStep(0f, 1f, weight);
            upper.rotation = Quaternion.Slerp(upper.rotation, upperTarget, blend);
            Vector3 end = start + forward * length;
            Quaternion lowerTarget = Quaternion.FromToRotation(hand.position - lower.position, end - lower.position) * lower.rotation;
            lower.rotation = Quaternion.Slerp(lower.rotation, lowerTarget, blend);
        }
        private void OnDisable() { Restore(); gathering = pushing = false; weight = 0f; }
        private void OnDestroy() { Restore(); }
    }
}
