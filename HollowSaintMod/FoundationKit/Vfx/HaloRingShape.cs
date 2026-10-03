using UnityEngine;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>
    /// Pure geometry of the copper halo, fitted from the four arc bone heads ("halo 1..4").
    /// Measured in the v34 source rig: each arc's bone head sits at the centre of its arc
    /// (same angle and radius as the mesh centroid), the heads' mean is the ring centre, and
    /// (h1 - h3) x (h2 - h4) is the ring normal. That holds in the rest pose (ring upright
    /// behind the head, normal pointing back) and in the Open Circuit crown (the four arcs
    /// swing flat above the head, normal pointing down), so one fit covers every clip.
    /// Arc order around the ring is 1, 2, 3, 4; the gaps are 1-2 and 3-4 (narrow side
    /// gaps), 2-3 (wide lower opening over the yoke) and 4-1 (top gap).
    /// No Unity object access, so tools/tests can run it with a stub Vector3.
    /// </summary>
    public sealed class HaloRingShape
    {
        public Vector3 Center;
        public Vector3 Normal = Vector3.up;
        /// <summary>In-plane unit vector toward arc 1; Binormal = Normal x Axis.</summary>
        public Vector3 Axis = Vector3.forward;
        public Vector3 Binormal = Vector3.right;
        /// <summary>Mean in-plane radius of the four arc centres.</summary>
        public float Radius = 0.35f;
        /// <summary>+1 or -1: the angular direction that runs arc 1 -> 2 -> 3 -> 4.</summary>
        public float Direction = 1f;
        public bool Valid;

        /// <summary>Arc centre angles (radians, Axis/Binormal basis) and in-plane radii.</summary>
        public readonly float[] Angle = new float[4];
        public readonly float[] ArcRadius = new float[4];

        /// <summary>Half the angular length of each copper arc (v34 mesh: upper arcs 1 and 4
        /// span about 71 deg, lower arcs 2 and 3 about 57 deg).</summary>
        public static readonly float[] HalfSpan =
        {
            35.5f * Mathf.Deg2Rad, 28.5f * Mathf.Deg2Rad, 28.5f * Mathf.Deg2Rad, 35.5f * Mathf.Deg2Rad
        };

        /// <summary>Fits the ring from the four arc bone heads. False when degenerate.</summary>
        public bool Fit(Vector3 h1, Vector3 h2, Vector3 h3, Vector3 h4)
        {
            Vector3 n = Vector3.Cross(h1 - h3, h2 - h4);
            float nLen = n.magnitude;
            if (nLen < 1e-6f) { Valid = false; return false; }
            Vector3 center = (h1 + h2 + h3 + h4) * 0.25f;
            Vector3 normal = n / nLen;
            Vector3 d1 = h1 - center;
            Vector3 axis = d1 - normal * Vector3.Dot(d1, normal);
            float aLen = axis.magnitude;
            if (aLen < 1e-6f) { Valid = false; return false; }
            Center = center;
            Normal = normal;
            Axis = axis / aLen;
            Binormal = Vector3.Cross(Normal, Axis);
            SetArc(0, h1); SetArc(1, h2); SetArc(2, h3); SetArc(3, h4);
            Radius = (ArcRadius[0] + ArcRadius[1] + ArcRadius[2] + ArcRadius[3]) * 0.25f;
            Direction = DeltaAngle(Angle[0], Angle[1]) >= 0f ? 1f : -1f;
            Valid = true;
            return true;
        }

        /// <summary>Fallback when the arc bones are missing: a plain circle, arcs evenly spread.</summary>
        public void SetCircle(Vector3 center, Vector3 normal, Vector3 axis, float radius)
        {
            Center = center;
            Normal = normal.sqrMagnitude > 1e-8f ? normal.normalized : Vector3.up;
            Vector3 a = axis - Normal * Vector3.Dot(axis, Normal);
            Axis = a.sqrMagnitude > 1e-8f ? a.normalized : Perpendicular(Normal);
            Binormal = Vector3.Cross(Normal, Axis);
            Radius = radius;
            Direction = 1f;
            for (int i = 0; i < 4; i++)
            {
                Angle[i] = -0.66f + i * Mathf.PI * 0.5f;
                ArcRadius[i] = radius;
            }
            Valid = true;
        }

        private void SetArc(int i, Vector3 head)
        {
            Vector3 d = head - Center;
            float x = Vector3.Dot(d, Axis);
            float y = Vector3.Dot(d, Binormal);
            Angle[i] = Mathf.Atan2(y, x);
            ArcRadius[i] = Mathf.Sqrt(x * x + y * y);
        }

        /// <summary>Ring radius at an angle, blended between the arc radii (the ring is
        /// slightly egg-shaped: lower arcs sit further out than the upper ones).</summary>
        public float RadiusAt(float angle)
        {
            float sum = 0f, weight = 0f;
            for (int i = 0; i < 4; i++)
            {
                float w = Mathf.Exp(3f * Mathf.Cos(angle - Angle[i]));
                sum += w * ArcRadius[i];
                weight += w;
            }
            return weight > 0f ? sum / weight : Radius;
        }

        public Vector3 Direction3(float angle)
        {
            return Axis * Mathf.Cos(angle) + Binormal * Mathf.Sin(angle);
        }

        /// <summary>World point on the ring at an angle; scale 1 = on the copper.</summary>
        public Vector3 PointAt(float angle, float scale = 1f)
        {
            return Center + Direction3(angle) * (RadiusAt(angle) * scale);
        }

        /// <summary>In-plane angle of a world point (any angle if it sits on the axis).</summary>
        public float AngleOf(Vector3 point)
        {
            Vector3 d = point - Center;
            float x = Vector3.Dot(d, Axis);
            float y = Vector3.Dot(d, Binormal);
            if (x * x + y * y < 1e-10f) return 0f;
            return Mathf.Atan2(y, x);
        }

        /// <summary>The point on the ring closest to a world point.</summary>
        public Vector3 Nearest(Vector3 point)
        {
            return PointAt(AngleOf(point));
        }

        /// <summary>Angles bounding gap i (between arc i and arc i+1), widened by overlap
        /// radians onto each copper tip so a bridging arc visibly lands on the metal.</summary>
        public void Gap(int i, float overlap, out float from, out float to)
        {
            int j = (i + 1) & 3;
            float span = Repeat((Angle[j] - Angle[i]) * Direction, Mathf.PI * 2f);
            from = Angle[i] + Direction * (HalfSpan[i] - overlap);
            to = Angle[i] + Direction * (span - HalfSpan[j] + overlap);
        }

        /// <summary>Signed smallest difference b - a in radians.</summary>
        public static float DeltaAngle(float a, float b)
        {
            float d = Repeat(b - a, Mathf.PI * 2f);
            return d > Mathf.PI ? d - Mathf.PI * 2f : d;
        }

        private static float Repeat(float t, float length)
        {
            return t - Mathf.Floor(t / length) * length;
        }

        private static Vector3 Perpendicular(Vector3 n)
        {
            Vector3 p = Vector3.Cross(n, Mathf.Abs(n.y) > 0.9f ? Vector3.right : Vector3.up);
            return p.normalized;
        }
    }
}
