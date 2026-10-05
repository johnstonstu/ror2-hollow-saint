namespace UnityEngine {
 public enum QueryTriggerInteraction { Ignore }
 public struct RaycastHit { public Vector3 point,normal; }
 public static class Physics {
  public static int Rays,Capsules; public static bool Ground,Blocked; public static float GroundY;
  public static bool Raycast(Vector3 origin,Vector3 direction,out RaycastHit hit,float distance,int mask,QueryTriggerInteraction trigger){Rays++;hit=new RaycastHit{point=new Vector3(origin.x,GroundY,origin.z),normal=Vector3.up};return Ground&&origin.y>=GroundY&&origin.y-distance<=GroundY;}
  public static bool CheckCapsule(Vector3 a,Vector3 b,float radius,int mask,QueryTriggerInteraction trigger){Capsules++;return Blocked;}
 }
}
namespace RoR2 {public struct LayerIndex {public int mask;public static LayerIndex world=new LayerIndex{mask=1};} }
