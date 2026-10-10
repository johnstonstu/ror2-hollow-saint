using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>
    /// Live geometry of the Saint's halo, refitted every LateUpdate from the animated arc
    /// bones, after the Animator (and after FoundationArmPose's chest breathing at 101).
    ///
    /// Why: the "halo socket" and "halo root" bones barely move during Open Circuit (about
    /// 10 deg of tilt). The crown is made by the four arc bones "halo 1..4" each swinging
    /// 70-90 deg on its own dock, which lays the ring flat above the head (normal turns from
    /// pointing back to pointing down, centre rises about 0.3 m, radius grows about 20%).
    /// Anything placed from the socket stays in the old upright plane. Consumers read
    /// Shape (charge orbs, crown arcs, pulse tendrils) and fall back to the socket.
    /// Runs on every machine; plain MonoBehaviour, added lazily with For(body).
    /// </summary>
    [DefaultExecutionOrder(150)]
    [DisallowMultipleComponent]
    public sealed class HaloRing : MonoBehaviour
    {
        private static readonly string[] ArcBoneNames = { "halo 1", "halo 2", "halo 3", "halo 4" };

        public readonly HaloRingShape Shape = new HaloRingShape();

        private CharacterBody body;
        private Transform model;
        private Transform socket;
        private Transform haloRoot;
        private Transform chest;
        private readonly Transform[] arcs = new Transform[4];
        private bool fromBones;
        private float nextResolve;
        private float restRadius;

        /// <summary>0 with the ring upright in its rest plane, about 0.9 in the flat crown.</summary>
        public float Openness { get; private set; }
        /// <summary>World metres per rig unit (1 at the authored scale), from chest to halo root.</summary>
        public float Unit { get; private set; } = 1f;
        /// <summary>Ring radius / its rest radius (1 at rest, about 1.2 in the crown).</summary>
        public float RadiusScale
        {
            get { return restRadius > 1e-4f ? Mathf.Clamp(Shape.Radius / restRadius, 0.7f, 1.6f) : 1f; }
        }
        public Transform Socket { get { return socket; } }
        public bool Valid { get { return Shape.Valid; } }

        public static HaloRing For(CharacterBody body)
        {
            if (!body) return null;
            var ring = body.GetComponent<HaloRing>();
            if (!ring) ring = body.gameObject.AddComponent<HaloRing>();
            return ring;
        }

        /// <summary>Ring centre, or the Halo socket / core when no ring is available.</summary>
        public static Vector3 CenterOf(CharacterBody body)
        {
            var ring = For(body);
            if (ring) ring.EnsureFitted();
            if (ring && ring.Valid) return ring.Shape.Center;
            return KitFx.Socket(body, "Halo");
        }

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
        }

        private void LateUpdate()
        {
            Refit();
        }

        /// <summary>Fit on demand; pose owners can refresh after restoring animated bones.</summary>
        public void EnsureFitted(bool force = false)
        {
            if (force || !Shape.Valid) Refit();
        }

        private void Refit()
        {
            if (!Resolve()) { Shape.Valid = false; return; }
            bool ok = false;
            if (fromBones)
                ok = Shape.Fit(arcs[0].position, arcs[1].position, arcs[2].position, arcs[3].position);
            if (!ok)
            {
                float scale = Mathf.Abs(socket.lossyScale.x);
                Vector3 center = haloRoot ? haloRoot.position : socket.position;
                Shape.SetCircle(center, socket.up, socket.forward, 0.36f * (scale > 1e-4f ? scale : 1f));
            }
            // Socket local Y is the ring normal in the authored rest pose (see StormChargeHalo).
            Openness = 1f - Mathf.Abs(Vector3.Dot(Shape.Normal, socket.up));
            var gaze = body.GetComponent<Gaze.GazeBeam>();
            if (Openness < 0.12f && (!gaze || gaze.Current == Gaze.GazeBeam.Phase.Idle))
            {
                // A floating/pulsing Gaze crown must not redefine the rig's rest radius.
                // Track the rest radius while upright, so RadiusScale follows the unfold.
                restRadius = restRadius <= 0f ? Shape.Radius : Mathf.Lerp(restRadius, Shape.Radius, 1f - Mathf.Exp(-3.1f * Time.deltaTime)); // 0.05 per frame at 60 fps, now frame-rate independent
            }
            if (chest && haloRoot)
                Unit = Mathf.Clamp(Vector3.Distance(chest.position, haloRoot.position) / 0.61f, 0.3f, 4f);
        }

        private bool Resolve()
        {
            var current = body && body.modelLocator ? body.modelLocator.modelTransform : null;
            if (!current) { model = null; return false; }
            if (current == model)
            {
                if (socket) return true;
                if (Time.unscaledTime < nextResolve) return false;
            }
            // One hierarchy walk per model (or per second while bones are missing).
            nextResolve = Time.unscaledTime + 1f;
            model = current;
            socket = KitUtil.ResolveSocket(body, "Halo");
            chest = KitUtil.ResolveSocket(body, "Chest");
            haloRoot = null;
            for (int i = 0; i < arcs.Length; i++) arcs[i] = null;
            var all = model.GetComponentsInChildren<Transform>(true);
            for (int n = 0; n < all.Length; n++)
            {
                var t = all[n];
                if (t.name == "halo root") { haloRoot = t; continue; }
                for (int i = 0; i < ArcBoneNames.Length; i++)
                    if (t.name == ArcBoneNames[i]) { arcs[i] = t; break; }
            }
            fromBones = arcs[0] && arcs[1] && arcs[2] && arcs[3];
            restRadius = 0f;
            if (!socket) return false; // retried once a second
            if (!fromBones)
                Plugin.Log.LogWarning("HOLLOW_SAINT_HALO_RING arc bones missing; ring falls back to the Halo socket");
            return true;
        }
    }
}
