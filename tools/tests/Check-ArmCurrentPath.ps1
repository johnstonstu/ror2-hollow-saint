# Success: upper-arm points stay behind the limb; the elbow wrap stays outside;
# forearm points stay underneath; outlet remains exact. Rotation/mirroring/scale
# preserve the route and straight/degenerate limbs produce finite points.
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$source = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\Vfx\ArmCurrentPath.cs'))
$stub = @'
namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float a, float b, float c) { x=a; y=b; z=c; }
        public static Vector3 up => new Vector3(0,1,0);
        public static Vector3 right => new Vector3(1,0,0);
        public static Vector3 back => new Vector3(0,0,-1);
        public float sqrMagnitude => x*x+y*y+z*z;
        public Vector3 normalized => sqrMagnitude > 1e-12f ? this/(float)System.Math.Sqrt(sqrMagnitude) : new Vector3();
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a, float k) => new Vector3(a.x*k,a.y*k,a.z*k);
        public static Vector3 operator /(Vector3 a, float k) => a*(1/k);
        public static float Dot(Vector3 a, Vector3 b) => a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a+(b-a)*t;
        public static Vector3 ProjectOnPlane(Vector3 a, Vector3 n) => n.sqrMagnitude > 1e-12f ? a-n*(Dot(a,n)/n.sqrMagnitude) : a;
    }
    public static class Mathf { public static float Abs(float x) => System.Math.Abs(x); }
}
'@
$checks = @'
namespace HollowSaint.FoundationKit.Vfx
{
    using UnityEngine;
    public static class ArmCurrentPathChecks
    {
        private static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
        private static void Check(bool ok,string detail) { if (!ok) throw new System.Exception(detail); }
        private static void Near(Vector3 a,Vector3 b,string detail) => Check((a-b).sqrMagnitude < 1e-9f,detail);
        private static Vector3 Turn(Vector3 p) => V(p.z,p.y,-p.x);
        private static Vector3 Mirror(Vector3 p) => V(-p.x,p.y,p.z);
        public static string Run()
        {
            var s=V(0,1,0); var e=V(0.3f,0.7f,0); var w=V(0.4f,0.7f,0.3f); var o=V(0.4f,0.7f,0.5f);
            var back=V(0,0,-1); var under=V(0,-1,0);
            var p=new Vector3[ArmCurrentPath.PointCount];
            ArmCurrentPath.Build(p,s,e,w,o,back,under,under,1f);
            Check(Vector3.Dot(p[1]-s,back)>0.09f && Vector3.Dot(p[2]-s,back)>0.08f &&
                Vector3.Dot(p[3]-e,back)>0.08f,"Upper-arm current no longer behind limb");
            Check((p[4]-e).sqrMagnitude>0.01f && Vector3.Dot(p[4]-e,back)>0.06f &&
                Vector3.Dot(p[4]-e,under)>0.06f,"Elbow wrap cuts the joint");
            Check(p[5].y<0.63f && p[6].y<0.65f && p[7].y<0.69f,"Forearm/hand route not underneath");
            Near(p[8],o,"Outlet moved");
            var q=new Vector3[p.Length];
            ArmCurrentPath.Build(q,Turn(s),Turn(e),Turn(w),Turn(o),Turn(back),Turn(under),Turn(under),1f);
            for(int i=1;i<p.Length;i++) Near(q[i],Turn(p[i]),"Body turn changed anatomical route");
            ArmCurrentPath.Build(q,Mirror(s),Mirror(e),Mirror(w),Mirror(o),Mirror(back),Mirror(under),Mirror(under),1f);
            for(int i=1;i<p.Length;i++) Near(q[i],Mirror(p[i]),"Left/right route is asymmetric");
            ArmCurrentPath.Build(q,s*2,e*2,w*2,o*2,back,under,under,2f);
            for(int i=1;i<p.Length;i++) Near(q[i],p[i]*2,"Body scale changed route proportions");
            foreach(var axis in new[]{back,under,V(0,0,0)})
            {
                ArmCurrentPath.Build(q,V(0,0,0),axis,axis*2,axis*3,axis,axis,axis,1f);
                foreach(var point in q) Check(!float.IsNaN(point.x+point.y+point.z) &&
                    !float.IsInfinity(point.x+point.y+point.z),"Straight/degenerate limb lost route");
            }
            return "ARM_CURRENT_PATH_PASS: posterior arm, elbow wrap, underside, outlet, rotation, mirror, scale, straight/degenerate.";
        }
    }
}
'@
Add-Type -TypeDefinition ($source + [Environment]::NewLine + $stub + [Environment]::NewLine + $checks)
[HollowSaint.FoundationKit.Vfx.ArmCurrentPathChecks]::Run()
