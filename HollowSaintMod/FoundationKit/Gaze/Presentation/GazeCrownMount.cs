using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    /// <summary>
    /// Lifts the whole halo ("halo root", with its arc bones and socket) off the head and floats
    /// it at the beam origin, its ring plane facing along the beam and spinning. Written after the
    /// Animator and pose passes and before HaloRing refits (GazeBeam runs at 140), so the ring
    /// shape and everything that reads it (charge orbs, crown arcs) follow the floating crown.
    /// The weight blends between the animated pose and the floating pose, so the dismount and the
    /// return are both smooth.
    /// </summary>
    public sealed class GazeCrownMount
    {
        private static readonly string[] ArcBoneNames = { "halo 1", "halo 2", "halo 3", "halo 4" };

        private CharacterBody body;
        private Transform model, haloRoot, socket;
        private readonly Transform[] arcs = new Transform[4];
        private Vector3 centerLocal;       // ring centre in halo-root space (rest pose)
        private float normalSign = -1f;    // which way the ring normal faces along the beam
        private float spin;
        private bool captured, dirty;
        private Vector3 savedLocalPosition;
        private Quaternion savedLocalRotation;
        private Vector3 savedLocalScale;
        private readonly Vector3[] savedArcPositions = new Vector3[4], arcWorldPositions = new Vector3[4];
        private bool dirtyArcs;

        public Vector3 Center { get; private set; }
        public Vector3 Normal { get; private set; }
        public float Weight { get; private set; }

        /// <summary>World position of arc bone i (0..3), or the centre when missing.</summary>
        public Vector3 ArcPoint(int i)
        {
            var t = i >= 0 && i < arcs.Length ? arcs[i] : null;
            return t ? t.position : Center;
        }

        public void Begin(CharacterBody owner)
        {
            Restore();
            body = owner;
            captured = false;
            Resolve();
        }

        public void Release()
        {
            Restore();
            Weight = 0f;
            captured = false;
        }

        /// <summary>Puts back the pose the halo root had before this frame's override. Called
        /// before the Animator runs: if a clip drives the bone this is overwritten anyway, and if
        /// none does the override must not accumulate frame to frame.</summary>
        public void Restore()
        {
            if (!dirty) return;
            dirty = false;
            if (dirtyArcs)
                for (int i = 0; i < arcs.Length; i++)
                    if (arcs[i]) arcs[i].localPosition = savedArcPositions[i];
            dirtyArcs = false;
            if (!haloRoot) return;
            haloRoot.localPosition = savedLocalPosition;
            haloRoot.localRotation = savedLocalRotation;
            haloRoot.localScale = savedLocalScale;
        }

        private bool Resolve()
        {
            var current = body && body.modelLocator ? body.modelLocator.modelTransform : null;
            if (!current) return false;
            if (current == model && haloRoot && socket) return true;
            Restore(); // Restore the previous model before resolving a replacement.
            model = current;
            captured = false;
            haloRoot = null;
            socket = KitUtil.ResolveSocket(body, "Halo");
            for (int i = 0; i < arcs.Length; i++) arcs[i] = null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "halo root") { haloRoot = t; continue; }
                for (int i = 0; i < ArcBoneNames.Length; i++)
                    if (t.name == ArcBoneNames[i]) { arcs[i] = t; break; }
            }
            return haloRoot && socket;
        }

        /// <summary>Called every frame while the skill is visible. weight 0 = animated pose.</summary>
        public void Apply(Vector3 crownPoint, Vector3 beamDirection, float weight, bool firing, float dt, float pulseExpansion = 0f)
        {
            Restore(); // Also safe if called twice before the next pre-Animator Update.
            Weight = weight;
            Center = crownPoint;
            Normal = beamDirection;
            if (!Resolve() || weight <= 0.001f) return;

            Quaternion animRotation = haloRoot.rotation;
            Vector3 animPosition = haloRoot.position;
            Quaternion socketInRoot = Quaternion.Inverse(animRotation) * socket.rotation;
            if (!captured)
            {
                // Rest pose: where the ring centre sits relative to the root, and which side of
                // the ring faces forward, so the crown turns the short way.
                captured = true;
                var ring = HaloRing.For(body);
                Vector3 center = ring && ring.Valid ? ring.Shape.Center : animPosition;
                centerLocal = Quaternion.Inverse(animRotation) * (center - animPosition);
                normalSign = Vector3.Dot(socket.up, beamDirection) >= 0f ? 1f : -1f;
                spin = 0f;
            }

            spin += dt * (firing ? 260f : 120f + 200f * weight);
            Vector3 up = beamDirection * normalSign;
            Vector3 reference = Vector3.ProjectOnPlane(Vector3.up, up);
            if (reference.sqrMagnitude < 1e-3f) reference = Vector3.ProjectOnPlane(((Component)body).transform.forward, up);
            Quaternion socketTarget = Quaternion.AngleAxis(spin, up) * Quaternion.LookRotation(reference.normalized, up);
            Quaternion rootTarget = socketTarget * Quaternion.Inverse(socketInRoot);
            float w = weight * weight * (3f - 2f * weight);
            // Pure presentation: move arc docks radially, preserving copper thickness and root scale.
            float expansion = firing && !float.IsNaN(pulseExpansion) && !float.IsInfinity(pulseExpansion)
                ? (pulseExpansion < 0f ? 0f : pulseExpansion > 3f ? 3f : pulseExpansion) : 0f;
            Vector3 rootPosition = crownPoint - rootTarget * centerLocal;
            Vector3 arc = Vector3.up * Mathf.Sin(w * Mathf.PI) * 0.35f;
            savedLocalPosition = haloRoot.localPosition;
            savedLocalRotation = haloRoot.localRotation;
            savedLocalScale = haloRoot.localScale;
            dirty = true;
            haloRoot.SetPositionAndRotation(Vector3.Lerp(animPosition, rootPosition, w) + arc,
                Quaternion.Slerp(animRotation, rootTarget, w));
            if (firing && w > 0.999f && arcs[0] && arcs[1] && arcs[2] && arcs[3])
            {
                // Recenter from live animated arc heads, including nonuniform rig scaling.
                Vector3 center = (arcs[0].position + arcs[1].position + arcs[2].position + arcs[3].position) * 0.25f;
                haloRoot.position += crownPoint - center;
                if (expansion > 0f)
                {
                    for (int i = 0; i < arcs.Length; i++)
                    {
                        savedArcPositions[i] = arcs[i].localPosition;
                        arcWorldPositions[i] = arcs[i].position;
                    }
                    dirtyArcs = true;
                    for (int i = 0; i < arcs.Length; i++)
                        arcs[i].position = arcWorldPositions[i] + Vector3.ProjectOnPlane(
                            arcWorldPositions[i] - crownPoint, beamDirection.normalized) * (0.6f * expansion);
                }
            }
        }
    }
}
