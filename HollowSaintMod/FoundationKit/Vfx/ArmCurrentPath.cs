using UnityEngine;

namespace HollowSaint.FoundationKit.Vfx
{
    // Surface route: posterior upper arm, outside elbow, underside forearm, hand.
    // Inputs are posed anatomical normals, so turning/pronation move the route with the limb.
    internal static class ArmCurrentPath
    {
        internal const int PointCount = 9;
        internal const int SegmentCount = PointCount - 1;

        internal static void Build(Vector3[] route, Vector3 shoulder, Vector3 elbow,
            Vector3 wrist, Vector3 outlet, Vector3 upperBack, Vector3 forearmUnder,
            Vector3 handUnder, float unit)
        {
            Vector3 rear = Radial(upperBack, elbow - shoulder, forearmUnder);
            Vector3 under = Radial(forearmUnder, wrist - elbow, rear);
            Vector3 palm = Radial(handUnder, outlet - wrist, under);
            route[1] = shoulder + rear * (0.095f * unit);
            route[2] = Vector3.Lerp(shoulder, elbow, 0.55f) + rear * (0.085f * unit);
            route[3] = elbow + rear * (0.09f * unit);
            Vector3 wrap = rear + under;
            if (wrap.sqrMagnitude < 0.001f) wrap = Vector3.Cross(wrist - elbow, rear);
            route[4] = elbow + wrap.normalized * (0.11f * unit);
            route[5] = Vector3.Lerp(elbow, wrist, 0.6f) + under * (0.08f * unit);
            route[6] = wrist + under * (0.055f * unit);
            route[7] = Vector3.Lerp(wrist, outlet, 0.45f) + palm * (0.025f * unit);
            route[8] = outlet;
            // The live ring supplies route[0] after the shoulder pickup is known.
        }

        private static Vector3 Radial(Vector3 preferred, Vector3 axis, Vector3 fallback)
        {
            Vector3 normal = Vector3.ProjectOnPlane(preferred, axis);
            if (normal.sqrMagnitude < 0.0001f) normal = Vector3.ProjectOnPlane(fallback, axis);
            if (normal.sqrMagnitude < 0.0001f)
                normal = Vector3.Cross(axis, Mathf.Abs(axis.normalized.y) < 0.9f ? Vector3.up : Vector3.right);
            return normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.back;
        }
    }
}
