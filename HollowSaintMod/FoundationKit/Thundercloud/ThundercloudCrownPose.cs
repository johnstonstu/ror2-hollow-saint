using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Thundercloud
{
    /// <summary>Feature-local translation of the actual crown; existing pose owners are unchanged.</summary>
    [DefaultExecutionOrder(146)]
    public sealed class ThundercloudCrownPose : MonoBehaviour
    {
        private readonly Transform[] arcs = new Transform[4];
        private readonly Vector3[] saved = new Vector3[4];
        private CharacterBody body;
        private Vector3 origin, sky;
        private float age, duration;
        private bool flying, dirty;
        internal static bool OwnsPresentation(CharacterBody body)
        {
            var model = body && body.modelLocator ? body.modelLocator.modelTransform : null;
            var pose = model ? model.GetComponent<ThundercloudCrownPose>() : null;
            return pose && pose.flying;
        }
        internal static void Begin(CharacterBody body, Vector3 from, Vector3 sky, float duration)
        {
            var model = body && body.modelLocator ? body.modelLocator.modelTransform : null;
            if (!model) return;
            var pose = model.GetComponent<ThundercloudCrownPose>();
            if (!pose) pose = model.gameObject.AddComponent<ThundercloudCrownPose>();
            pose.body = body; pose.origin = from; pose.sky = sky; pose.duration = duration; pose.age = 0; pose.flying = true;
        }
        /// <summary>1.3.1: a dismissed storm recalls the crown now.</summary>
        internal static void Recall(CharacterBody body)
        {
            var model = body && body.modelLocator ? body.modelLocator.modelTransform : null;
            var pose = model ? model.GetComponent<ThundercloudCrownPose>() : null;
            if (pose && pose.flying) pose.duration = Mathf.Min(pose.duration, Mathf.Max(pose.age, ThundercloudSchedule.Ascent) + .4f);
        }
        private void Awake()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                for (int i = 0; i < 4; i++) if (t.name == "halo " + (i + 1)) arcs[i] = t;
        }
        private void Restore()
        {
            if (!dirty) return; dirty = false;
            for (int i = 0; i < 4; i++) if (arcs[i]) arcs[i].localPosition = saved[i];
            var ring = HaloRing.For(body); if (ring) ring.EnsureFitted(true);
        }
        private void Update() { Restore(); }
        private void LateUpdate()
        {
            if (!flying) return;
            age += Time.deltaTime;
            if (!body || !body.healthComponent || !body.healthComponent.alive || age >= duration ||
                StoredCrownTaken())
            { flying = false; Restore(); return; }
            Vector3 center = Vector3.zero;
            for (int i = 0; i < 4; i++)
            {
                if (!arcs[i]) { flying = false; Plugin.Log.LogWarning("HOLLOW_SAINT_CLOUD_CROWN missing halo arc " + (i + 1)); return; }
                center += arcs[i].position * .25f;
            }
            Vector3 target = Vector3.Lerp(origin, sky, Mathf.SmoothStep(0, 1, age / ThundercloudSchedule.Ascent));
            float recall = Mathf.Clamp01((age - (duration - .4f)) / .4f);
            target = Vector3.Lerp(target, center, Mathf.SmoothStep(0, 1, recall));
            for (int i = 0; i < 4; i++) saved[i] = arcs[i].localPosition;
            dirty = true;
            for (int i = 0; i < 4; i++) arcs[i].position += target - center;
        }
        private void OnDisable() { Restore(); flying = false; }
        private bool StoredCrownTaken()
        {
            var beam = body.GetComponent<Gaze.GazeBeam>();
            if (beam && beam.Current != Gaze.GazeBeam.Phase.Idle) return true;
            // 1.3.2: GetComponents allocated a fresh array every frame of the storm; the body's
            // machines are fixed after spawn, so look them up once per body.
            if (machinesOf != body) { machines = body.GetComponents<EntityStateMachine>(); machinesOf = body; }
            foreach (var machine in machines)
                if (machine && machine.state is Gaze.GazeState) return true;
            return false;
        }
        private EntityStateMachine[] machines;
        private CharacterBody machinesOf;
        private void OnDestroy() { Restore(); }
    }
}
