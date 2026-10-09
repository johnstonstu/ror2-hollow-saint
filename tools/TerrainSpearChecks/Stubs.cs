using System.Reflection;
// ChargedStorm behavior is exercised by its linked production suite. This legacy
// spear adapter has no charged-storm cast and preserves that baseline condition.
namespace HollowSaint.FoundationKit.ChargedStorm
{ internal static class StoredChargeState { internal static bool BlocksPrimary(RoR2.CharacterBody body) => false; } }
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int value){} }
    public class Object
    {
        public static implicit operator bool(Object obj) => obj != null;
        public static bool operator !(Object obj) => obj == null;
    }
    public class GameObject : Object
    {
        readonly List<Component> components = new();
        public T AddComponent<T>() where T : Component, new()
        {
            var c = new T { gameObject = this }; components.Add(c);
            c.transform = c is Transform t ? t : GetComponent<Transform>() ?? AddComponent<Transform>();
            typeof(T).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(c, null);
            return c;
        }
        public T GetComponent<T>() where T : class => components.OfType<T>().FirstOrDefault();
        public T[] GetComponents<T>() => components.OfType<T>().ToArray();
    }
    public class Component : Object
    {
        public GameObject gameObject; public Transform transform;
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
        public T[] GetComponents<T>() => gameObject.GetComponents<T>();
    }
    public class MonoBehaviour : Component { public bool enabled=true; }
    public struct Quaternion { public Vector3 forward; }
    public class Transform : Component
    {
        public Vector3 position,forward=new(1,0,0),lossyScale=new(1,1,1);
        public Quaternion rotation {get=>new(){forward=forward};set=>forward=value.forward;}
        public Vector3 TransformPoint(Vector3 v)=>position+v;
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x=x; this.y=y; this.z=z; }
        public static Vector3 zero => new(0,0,0); public static Vector3 up => new(0,1,0);
        public float magnitude => MathF.Sqrt(x*x+y*y+z*z);
        public Vector3 normalized => magnitude > 0 ? this/magnitude : zero;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a, float b) => new(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator /(Vector3 a, float b) => new(a.x/b,a.y/b,a.z/b);
        public static float Dot(Vector3 a, Vector3 b) => a.x*b.x+a.y*b.y+a.z*b.z;
        public static float Distance(Vector3 a, Vector3 b) => (a-b).magnitude;
        public static float Angle(Vector3 a,Vector3 b)=>MathF.Acos(Math.Clamp(Dot(a.normalized,b.normalized),-1,1))*180/MathF.PI;
        public static Vector3 RotateTowards(Vector3 a,Vector3 b,float radians,float unused)
        {
            float angle=Angle(a,b)*MathF.PI/180;if(angle<.00001f)return b.normalized;
            float t=Math.Min(1,radians/angle);
            return (a.normalized*MathF.Sin((1-t)*angle)+b.normalized*MathF.Sin(t*angle))/MathF.Sin(angle);
        }
    }
    public struct Bounds { public Vector3 center; }
    public class Collider : Component { public Bounds bounds; }
    public class SphereCollider : Collider { public float radius; public Vector3 center; }
    public class Rigidbody : Component { public Vector3 velocity; }
    public struct RaycastHit { public float distance; }
    public enum QueryTriggerInteraction { Ignore }
    public static class Mathf { public static float Max(float a,float b) => Math.Max(a,b); public static float Abs(float f)=>Math.Abs(f);public static float Min(float a,float b)=>Math.Min(a,b);public const float Deg2Rad=MathF.PI/180; }
    public static class Time { public static float fixedDeltaTime=.02f; }
    public static class Physics
    {
        // Synthetic halfspaces represent flat/sloped terrain and walls. Tests exercise
        // production queries/decisions; these are not a Unity PhysX or CCD simulation.
        public static readonly List<(Vector3 normal,float offset)> Planes = new();
        public static bool CheckSphere(Vector3 p,float radius,int mask,QueryTriggerInteraction triggers)
            => Planes.Any(s => Vector3.Dot(s.normal,p)-s.offset <= radius);
        public static bool SphereCast(Vector3 p,float radius,Vector3 dir,out RaycastHit hit,float length,int mask,QueryTriggerInteraction triggers)
        {
            float nearest=float.PositiveInfinity;
            foreach(var s in Planes)
            {
                float clearance=Vector3.Dot(s.normal,p)-s.offset-radius;
                float rate=Vector3.Dot(s.normal,dir);
                if(clearance<=0) nearest=0;
                else if(rate<0) { float d=-clearance/rate; if(d<=length)nearest=Math.Min(nearest,d); }
            }
            hit=new RaycastHit {distance=nearest}; return !float.IsPositiveInfinity(nearest);
        }
        public static bool Linecast(Vector3 a,Vector3 b,int mask,QueryTriggerInteraction triggers)
            => CheckSphere(a,0,mask,triggers) || CheckSphere(b,0,mask,triggers);
    }
}
namespace EntityStates { public class EntityState {} }
namespace RoR2
{
    using UnityEngine;
    public static class LayerIndex { public static (int mask,int unused) world=(1,0); }
    public enum TeamIndex { None, Player, Monster }
    public class TeamComponent : Component { public TeamIndex teamIndex=TeamIndex.Player; }
    public class InputBankTest : Object { public Button skill1,skill2; public struct Button { public bool down,hasPressBeenClaimed; } }
    public class CharacterBody : Component
    {
        public HealthComponent healthComponent; public SkillLocator skillLocator; public InputBankTest inputBank=new();
        public TeamComponent teamComponent; public bool circuit;
    }
    public class SkillLocator : Object { public GenericSkill secondary; }
    public class EntityStateMachine : Component { public string customName; public EntityStates.EntityState state; }
    public class GenericSkill : Object
    {
        public CharacterBody characterBody; public Skills.SkillDef skillDef; public int stock=1,executions; public float rechargeStopwatch;
        public bool CanExecute()=>skillDef.CanExecute(this);
        public bool ExecuteIfReady() { if(!CanExecute())return false; stock--; executions++;rechargeStopwatch=0;return true; }
    }
    public class HealthComponent : Component
    {
        public bool alive=true; public readonly List<DamageInfo> damages=new();
        public void TakeDamage(DamageInfo info) { damages.Add(info); }
    }
    public class HurtBox : Component { public Collider collider; public HealthComponent healthComponent; }
    public struct ProcChainMask { public int value; }
    public struct DamageTypeCombo { public int value; }
    public enum DamageColorIndex { Default }
    public class DamageInfo
    {
        public float damage,procCoefficient; public bool crit,rejected; public GameObject attacker,inflictor;
        public Vector3 position,force; public DamageTypeCombo damageType; public DamageColorIndex damageColorIndex;
        public ProcChainMask procChainMask; public HurtBox inflictedHurtbox;
    }
    public class TeamMask { public static TeamMask GetEnemyTeams(TeamIndex team)=>new(); }
    public class BullseyeSearch
    {
        public static List<HurtBox> Candidates=new(); public Vector3 searchOrigin,searchDirection;
        public float minAngleFilter,maxAngleFilter,maxDistanceFilter; public TeamMask teamMaskFilter;
        public bool filterByLoS,filterByDistinctEntity; public SortMode sortMode;
        public enum SortMode { Distance, Angle }
        private GameObject excluded;
        public void RefreshCandidates() { }
        public void FilterOutGameObject(GameObject obj)=>excluded=obj;
        public IEnumerable<HurtBox> GetResults()=>Candidates.Where(b=>b.healthComponent.gameObject!=excluded &&
            Vector3.Distance(searchOrigin,b.transform.position)<=maxDistanceFilter)
            .OrderBy(b=>Vector3.Distance(searchOrigin,b.transform.position)).DistinctBy(b=>b.healthComponent);
    }
    public static class FriendlyFireManager {public static bool ShouldDirectHitProceed(HealthComponent victim,TeamIndex team)=>true;}
    public static class Util {public static Quaternion QuaternionSafeLookRotation(Vector3 direction)=>new(){forward=direction};}
}
namespace RoR2.Projectile
{
    public class ProjectileController : UnityEngine.Component {public RoR2.TeamComponent teamFilter;public UnityEngine.GameObject owner;}
    public class ProjectileSimple : UnityEngine.Component {public float desiredForwardSpeed;}
}
namespace RoR2.Skills
{
    public class SkillDef
    {
        public bool mustKeyPress;
        public virtual bool IsReady(RoR2.GenericSkill slot)=>slot.stock>0;
        public virtual bool CanExecute(RoR2.GenericSkill slot)=>IsReady(slot);
    }
}
namespace UnityEngine.Networking { public static class NetworkServer { public static bool active=true; } }
namespace HollowSaint.FoundationKit
{
    public static class KitTuning {public static float ArcBoltAssistConeDegrees=3;}
    public static class KitUtil
    {
        public static List<RoR2.DamageInfo> Reports=new();
        public static void ReportHit(RoR2.DamageInfo info,UnityEngine.GameObject victim) { if(!info.rejected && info.procCoefficient>0)Reports.Add(info); }
    }
}
namespace HollowSaint.FoundationKit.Stormspear
{
    public static class StormspearTuning {public static float AssistConeDegrees=3;}
    public class StormspearChargeState : EntityStates.EntityState { }
    public class StormspearThrowState : EntityStates.EntityState { }
    public class StormspearSkillDef : RoR2.Skills.SkillDef { public StormspearSkillDef(){mustKeyPress=true;} }
    public static class StormspearCharge { public static bool InCrown(RoR2.CharacterBody b)=>b.circuit; }
}
