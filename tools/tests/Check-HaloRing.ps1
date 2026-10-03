# Success: the halo ring fitted from the four arc bone heads matches the copper mesh in
# the rest pose and in the Open Circuit crown (bone heads and mesh fits sampled from
# art/anim/hollow-saint-anim-v34.blend, "Open Circuit" f1 and f21), nearest points land
# on the ring, and gap angles bound the real gaps. Uses the production HaloRingShape
# with a minimal UnityEngine stub (no Unity assemblies needed).
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$source = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\Vfx\HaloRingShape.cs'))
$stub = @'
namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 up { get { return new Vector3(0, 1, 0); } }
        public static Vector3 right { get { return new Vector3(1, 0, 0); } }
        public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
        public float magnitude { get { return (float)System.Math.Sqrt(x * x + y * y + z * z); } }
        public float sqrMagnitude { get { return x * x + y * y + z * z; } }
        public Vector3 normalized { get { float m = magnitude; return m > 1e-9f ? this / m : new Vector3(); } }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator *(Vector3 a, float d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator /(Vector3 a, float d) { return new Vector3(a.x / d, a.y / d, a.z / d); }
        public static float Dot(Vector3 a, Vector3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
        public static Vector3 Cross(Vector3 a, Vector3 b) { return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x); }
        public static float Distance(Vector3 a, Vector3 b) { return (a - b).magnitude; }
    }
    public static class Mathf
    {
        public const float PI = (float)System.Math.PI;
        public const float Deg2Rad = PI / 180f;
        public static float Sqrt(float f) { return (float)System.Math.Sqrt(f); }
        public static float Atan2(float y, float x) { return (float)System.Math.Atan2(y, x); }
        public static float Cos(float f) { return (float)System.Math.Cos(f); }
        public static float Sin(float f) { return (float)System.Math.Sin(f); }
        public static float Exp(float f) { return (float)System.Math.Exp(f); }
        public static float Abs(float f) { return System.Math.Abs(f); }
        public static float Floor(float f) { return (float)System.Math.Floor(f); }
    }
}
'@
$checks = @'
namespace HollowSaint.FoundationKit.Vfx
{
    using UnityEngine;
    public static class HaloRingChecks
    {
        private static void Check(bool ok, string message) { if (!ok) throw new System.Exception(message); }
        private static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }
        private static void Pose(string name, Vector3[] heads, Vector3 meshCenter, Vector3 meshNormal, float meshRadius)
        {
            var s = new HaloRingShape();
            Check(s.Fit(heads[0], heads[1], heads[2], heads[3]), name + ": fit failed");
            Check(Vector3.Distance(s.Center, meshCenter) < 0.015f, name + ": centre off the mesh centre");
            Check(Mathf.Abs(Vector3.Dot(s.Normal, meshNormal)) > 0.995f, name + ": normal off the mesh plane");
            Check(Mathf.Abs(s.Radius - meshRadius) < 0.06f, name + ": radius " + s.Radius + " vs mesh " + meshRadius);
            // Nearest point lies on the ring plane at ring radius.
            Vector3 probe = s.Center + s.Normal * 0.7f + s.Binormal * 0.9f;
            Vector3 p = s.Nearest(probe);
            Check(Mathf.Abs(Vector3.Dot(p - s.Center, s.Normal)) < 1e-4f, name + ": nearest point off plane");
            Check(Vector3.Dot(p - s.Center, s.Binormal) > 0.25f, name + ": nearest point on the wrong side");
            // Gap 2-3 (lower opening) is the widest, side gaps the narrowest.
            float a, b;
            float[] width = new float[4];
            for (int i = 0; i < 4; i++) { s.Gap(i, 0f, out a, out b); width[i] = (b - a) * s.Direction; }
            Check(width[1] > 0.9f && width[1] < 1.35f, name + ": lower opening " + width[1]);
            Check(width[3] > 0.35f && width[3] < 0.75f, name + ": top gap " + width[3]);
            Check(width[0] > -0.05f && width[0] < 0.2f && width[2] > -0.05f && width[2] < 0.2f, name + ": side gaps " + width[0] + "/" + width[2]);
        }
        public static string Run()
        {
            // Rest (Open Circuit f1): ring upright behind the head, normal +Y (back).
            Pose("rest", new[] { V(0.196f, 0.215f, 2.197f), V(0.291f, 0.215f, 1.823f), V(-0.366f, 0.215f, 1.823f), V(-0.271f, 0.215f, 2.197f) },
                V(-0.038f, 0.215f, 2.015f), V(0f, 1f, 0f), 0.364f);
            // Crown (Open Circuit f21): ring flat above the head, normal -Z (down).
            Pose("crown", new[] { V(0.254f, 0.551f, 2.328f), V(0.363f, 0.09f, 2.316f), V(-0.438f, 0.09f, 2.316f), V(-0.329f, 0.551f, 2.328f) },
                V(-0.038f, 0.326f, 2.326f), V(0f, 0.02f, -1f), 0.437f);
            // Mirrored rig (Unity import flips X): same result with the opposite direction.
            var m = new HaloRingShape();
            Check(m.Fit(V(-0.196f, 0.215f, 2.197f), V(-0.291f, 0.215f, 1.823f), V(0.366f, 0.215f, 1.823f), V(0.271f, 0.215f, 2.197f)), "mirror fit");
            float a, b; m.Gap(1, 0f, out a, out b);
            Check((b - a) * m.Direction > 0.9f, "mirror: lower opening lost");
            Check(!new HaloRingShape().Fit(V(0, 0, 0), V(0, 0, 0), V(0, 0, 0), V(0, 0, 0)), "degenerate accepted");
            return "HALO_RING_PASS: rest and crown fits match the v34 mesh, nearest point, gaps, mirror, degenerate.";
        }
    }
}
'@
Add-Type -TypeDefinition ($source + [Environment]::NewLine + $stub + [Environment]::NewLine + $checks)
[HollowSaint.FoundationKit.Vfx.HaloRingChecks]::Run()
