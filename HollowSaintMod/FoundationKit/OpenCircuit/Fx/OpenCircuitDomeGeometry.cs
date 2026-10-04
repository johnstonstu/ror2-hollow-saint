using System;

namespace HollowSaint.FoundationKit.OpenCircuit.Fx
{
    /// <summary>Unit sphere edge samples; no cosmetic radius clamp or floor projection.</summary>
    internal static class OpenCircuitDomeGeometry
    {
        internal const int PathCount = 11;
        internal const int RingSegments = 48;
        internal const int MeridianSegments = 24;

        internal readonly struct Point
        {
            internal readonly float X, Y, Z;
            internal Point(float x, float y, float z) { X = x; Y = y; Z = z; }
        }

        internal static bool IsValidRadius(float radius) => radius > 0f && !float.IsNaN(radius) && !float.IsInfinity(radius);

        internal static bool ShouldShow(bool alive, bool buff, bool visible, bool enabled)
            => alive && buff && visible && enabled;

        internal static bool IsLower(int path) => path >= 7;

        internal static Point ScaleTranslate(Point unit, Point center, float radius)
            => new Point(center.X + unit.X * radius, center.Y + unit.Y * radius, center.Z + unit.Z * radius);

        internal static Point[] CreateUnitPath(int path)
        {
            if (path < 0 || path >= PathCount) throw new ArgumentOutOfRangeException(nameof(path));
            bool ring = path < 3;
            int segments = ring ? RingSegments : MeridianSegments;
            var result = new Point[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                double angle = (ring ? 2d : 1d) * Math.PI * i / segments;
                if (ring)
                {
                    double y = path == 0 ? 0d : path == 1 ? 0.5d : 0.82d;
                    double horizontal = Math.Sqrt(1d - y * y);
                    result[i] = new Point((float)(horizontal * Math.Cos(angle)), (float)y,
                        (float)(horizontal * Math.Sin(angle)));
                }
                else
                {
                    double heading = ((path - 3) % 4) * Math.PI / 4d;
                    double horizontal = Math.Cos(angle);
                    result[i] = new Point((float)(horizontal * Math.Cos(heading)),
                        (float)(Math.Sin(angle) * (IsLower(path) ? -1d : 1d)),
                        (float)(horizontal * Math.Sin(heading)));
                }
            }
            // Exact closure avoids a floating point seam on rings.
            if (ring) result[segments] = result[0];
            return result;
        }
    }
}
