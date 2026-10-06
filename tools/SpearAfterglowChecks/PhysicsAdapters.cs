using System;
namespace UnityEngine {
 public enum QueryTriggerInteraction { Ignore }
 public struct RaycastHit { public Vector3 point,normal; }
 public static class Physics {
  public static int Rays,Capsules,Profile; public static bool Ground,Blocked; public static float GroundY,Slope;
  public static float Height(float x,float z){
   switch(Profile){
    case 1:return GroundY+.3f*x; // true sloping plane
    case 2:return GroundY+.32f*MathF.Exp(-MathF.Pow((x-.8f)/.45f,2)); // convex ridge
    case 3:return GroundY-.32f*MathF.Exp(-MathF.Pow((x-.8f)/.45f,2)); // concave dip
    case 4:return GroundY+(x>.9f?.9f:0); // impassable step
    case 6:return GroundY+.08f*MathF.Sin(x*3)+.06f*MathF.Sin(z*2); // rolling uneven terrain
    default:return GroundY+x*Slope;
   }
  }
  public static bool Exists(float x,float z)=>Profile!=5||x<.9f;
  public static Vector3 Normal(float x,float z){float h=.001f;return new Vector3(-(Height(x+h,z)-Height(x-h,z))/(2*h),1,-(Height(x,z+h)-Height(x,z-h))/(2*h)).normalized;}
  public static bool Raycast(Vector3 origin,Vector3 direction,out RaycastHit hit,float distance,int mask,QueryTriggerInteraction trigger){Rays++;float y=Height(origin.x,origin.z);hit=new RaycastHit{point=new Vector3(origin.x,y,origin.z),normal=Normal(origin.x,origin.z)};return Ground&&Exists(origin.x,origin.z)&&origin.y>=y&&origin.y-distance<=y;}
  public static bool CheckCapsule(Vector3 a,Vector3 b,float radius,int mask,QueryTriggerInteraction trigger){
   Capsules++;if(Blocked)return true;if(!Ground)return false;
   int steps=Math.Max(2,(int)(Vector3.Distance(a,b)/.01f));
   for(int i=0;i<=steps;i++){var p=Vector3.Lerp(a,b,i/(float)steps);if(Exists(p.x,p.z)&&(p.y-Height(p.x,p.z))*Normal(p.x,p.z).y<radius-.001f)return true;}
   return false;
  }
 }
}
namespace RoR2 {public struct LayerIndex {public int mask;public static LayerIndex world=new LayerIndex{mask=1};} }
