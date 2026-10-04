using System;

namespace HollowSaint.FoundationKit.OpenCircuit.Fx
{
    /// <summary>Sparse open lightning ribbons on the true unit sphere. No intersecting
    /// latitude/meridian cage, cosmetic radius clamp, filled surface or floor projection.</summary>
    internal static class OpenCircuitDomeGeometry
    {
        internal const int PathCount = 8;
        internal const int RingSegments = 24;
        internal const int MeridianSegments = 20;

        internal readonly struct Point
        {
            internal readonly float X, Y, Z;
            internal Point(float x, float y, float z) { X = x; Y = y; Z = z; }
        }

        internal static bool IsValidRadius(float radius) => radius > 0f && !float.IsNaN(radius) && !float.IsInfinity(radius);

        internal static bool ShouldShow(bool alive, bool buff, bool visible, bool enabled)
            => alive && buff && visible && enabled;

        internal static bool IsLower(int path) => path >= 6;

        internal static Point ScaleTranslate(Point unit, Point center, float radius)
            => new Point(center.X + unit.X * radius, center.Y + unit.Y * radius, center.Z + unit.Z * radius);

        internal static Point[] CreateUnitPath(int path)
        {
            if (path < 0 || path >= PathCount) throw new ArgumentOutOfRangeException(nameof(path));
            int segments = path < 4 ? RingSegments : MeridianSegments;
            var result = new Point[segments + 1];
            WriteUnitPath(path, 0f, result);
            return result;
        }

        /// <summary>Updates preallocated samples. Every vertex stays at exactly radius one;
        /// slow tangential motion conveys current without pulsing the damage boundary.</summary>
        internal static void WriteUnitPath(int path, float time, Point[] result)
        {
            if (path < 0 || path >= PathCount) throw new ArgumentOutOfRangeException(nameof(path));
            int segments = path < 4 ? RingSegments : MeridianSegments;
            if (result == null || result.Length != segments + 1) throw new ArgumentException("sample count", nameof(result));
            for (int i = 0; i <= segments; i++)
            {
                double t = i / (double)segments;
                double y = path < 4 ? 0d : IsLower(path) ? -0.18d - 0.55d * Math.Sin(t * Math.PI) :
                    0.22d + 0.62d * Math.Sin(t * Math.PI);
                double heading = path < 4 ? path * Math.PI / 2d + time * 0.08d :
                    (path % 2) * Math.PI + (IsLower(path) ? Math.PI / 2d : 0d) - time * 0.055d;
                double angle = heading + (t - 0.5d) * (path < 4 ? 0.96d : 1.22d);
                angle += Math.Sin(t * Math.PI) * (i % 2 == 0 ? 0.012d : -0.012d);
                double horizontal = Math.Sqrt(1d - y * y);
                result[i] = new Point((float)(horizontal * Math.Cos(angle)), (float)y,
                    (float)(horizontal * Math.Sin(angle)));
            }
        }
    }
}
