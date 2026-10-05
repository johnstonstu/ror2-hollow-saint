namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object value)=>value!=null; }
    public class GameObject:Object {public bool activeInHierarchy=true;}
    public class MonoBehaviour:Object {public static RoR2.CharacterBody Body;public GameObject gameObject=new();protected T GetComponent<T>()where T:class=>Body as T;}
    public class DisallowMultipleComponent:Attribute {}
    public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);}
    public class Transform {public Vector3 position;}
    public class Collider:Object {public bool enabled=true;public Vector3 Point;public Vector3 ClosestPoint(Vector3 origin)=>Point;}
    public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);public static int FloorToInt(float v)=>(int)Math.Floor(v);}
    public static class Time {public static float fixedDeltaTime=.02f;}
}
namespace UnityEngine.Networking {public static class NetworkServer {public static bool active=true;}}
namespace RoR2
{
    using UnityEngine;
    public class BuffDef:Object {}
    public class Stage:Object {public static Stage instance=new();}
    public class TeamComponent:Object {public int teamIndex;}
    public class HurtBoxGroup:Object {public HurtBox[] hurtBoxes;}
    public class CharacterBody:Object {public GameObject gameObject=new();public HealthComponent healthComponent=new();public TeamComponent teamComponent=new();public HurtBoxGroup hurtBoxGroup=new();public Vector3 corePosition;public float damage=17;public bool Open=true,Dash;public bool HasBuff(BuffDef def)=>Open;public bool RollCrit()=>false;}
    public class HurtBox:Object {public HealthComponent healthComponent;public Collider collider=new();public Transform transform=new();public GameObject gameObject=new();}
    public class DamageInfo {public float damage,procCoefficient;public bool rejected;public Vector3 position;}
    public class HealthComponent:Object {public bool alive=true,Reject;public CharacterBody body;public GameObject gameObject=new();public int Hits,StaticHits;public float Damage,Proc;public void TakeDamage(DamageInfo info){Hits++;Damage+=info.damage;Proc=info.procCoefficient;info.rejected=Reject;if(HollowSaint.FoundationKit.Storm.StormServer.Depth==0)StaticHits++;}}
}
namespace HollowSaint {public static class Plugin {public static Logger Log=new();public class Logger {public void LogWarning(object error)=>Console.Error.WriteLine(error);}}}
namespace HollowSaint.FoundationKit
{
    public static class KitTuning {public static float OpenCircuitRadius=8;}
    public static class KitUtil {public static int Reports;public static void ReportHit(RoR2.DamageInfo info,UnityEngine.GameObject target){Reports++;}}
}
namespace HollowSaint.FoundationKit.OpenCircuit
{
    public static class OpenCircuitBuff {public static RoR2.BuffDef Def=new();}
    public static class OpenCircuitTuning {public static bool AllowPulsesDuringGlideAndArcStep;}
    public class OpenCircuitPulseDriver:UnityEngine.Object {public bool isActiveAndEnabled=true;}
}
namespace HollowSaint.FoundationKit.ArcStep {public static class ArcStepState {public static bool IsBodyDashing(RoR2.CharacterBody body)=>body.Dash;}}
namespace HollowSaint.FoundationKit.Storm
{
    public static class StormServer {public static int Depth;public static void BeginStormDamage()=>Depth++;public static void EndStormDamage()=>Depth--;public static RoR2.DamageInfo MakeInfo(RoR2.CharacterBody attacker,RoR2.HurtBox box,float damage,bool crit,float proc)=>new(){damage=damage,procCoefficient=proc,position=box.transform.position};}
}
namespace HollowSaint.FoundationKit.Vfx
{
    public enum Beat {CircuitDwellZap}
    public static class KitFx {public static int Sends;public static UnityEngine.Vector3 LastPoint;public static void Server(Beat beat,UnityEngine.Vector3 origin,RoR2.CharacterBody owner=null,bool sound=true){Sends++;LastPoint=origin;}}
}
