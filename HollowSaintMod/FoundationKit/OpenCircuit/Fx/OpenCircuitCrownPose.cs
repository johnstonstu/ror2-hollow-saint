using HollowSaint.FoundationKit.Vfx;
using UnityEngine;

namespace HollowSaint.FoundationKit.OpenCircuit.Fx
{
    /// <summary>Reversible post-animation translation of the four actual crown elements.
    /// No scale, rotation, root, Animator parameter or gameplay writes.</summary>
    internal sealed class OpenCircuitCrownPose
    {
        private readonly Transform[] arcs = new Transform[4];
        private readonly Vector3[] saved = new Vector3[4], world = new Vector3[4];
        private bool dirty;
        internal readonly HaloRingShape Shape = new HaloRingShape();
        internal float Weight { get; private set; }
        internal void Bind(Transform[] source)
        {
            Restore();
            for (int i = 0; i < arcs.Length; i++) arcs[i] = source[i];
            Weight = 0f;
        }
        internal bool Fit()
        {
            if (!arcs[0] || !arcs[1] || !arcs[2] || !arcs[3]) { Shape.Valid = false; return false; }
            return Shape.Fit(arcs[0].position, arcs[1].position, arcs[2].position, arcs[3].position);
        }
        internal void Restore()
        {
            if (!dirty) return;
            dirty = false;
            for (int i = 0; i < arcs.Length; i++) if (arcs[i]) arcs[i].localPosition = saved[i];
        }
        internal void Release() { Restore(); Weight = 0f; }
        internal bool Apply(Vector3 damageCenter, float radius, float weight)
        {
            Restore(); Weight = 0f;
            if (!Fit() || !OpenCircuitDomeGeometry.IsValidRadius(radius) || float.IsNaN(weight) || float.IsInfinity(weight)) return false;
            weight = Mathf.Clamp01(weight);
            if (weight <= 0f) return true;
            for (int i = 0; i < arcs.Length; i++) { saved[i] = arcs[i].localPosition; world[i] = arcs[i].position; }
            Vector3 u = Vector3.ProjectOnPlane(world[0] - Shape.Center, Vector3.up).normalized;
            if (u.sqrMagnitude < 0.01f) u = Vector3.forward;
            Vector3 v = Vector3.Cross(Vector3.up, u);
            if (Vector3.Dot(world[1] - Shape.Center, v) < 0f) v = -v;
            dirty = true;
            for (int i = 0; i < arcs.Length; i++)
            {
                Vector3 radial = i == 0 ? u : i == 1 ? v : i == 2 ? -u : -v;
                // Final ring = equator of the actual core-centred damage sphere.
                // Preserve authored copper thickness and rotation; move only metal docks.
                arcs[i].position = Vector3.Lerp(world[i], damageCenter + radial * radius, weight);
            }
            Weight = weight;
            return Fit();
        }
    }
}
