// Native adapter simulation: independent override banks, latest priority wins,
// OnAssigned instance data recreation and fullRestockOnAssign are the real traps.
// These tests execute production controls/SkillDefs, not Unity/network/HUD rendering.
using System.Reflection;
namespace UnityEngine {
    public class Object { public static implicit operator bool(Object o)=>o!=null; }
    public class Sprite:Object {}
    public class ScriptableObject:Object { public string name; public static T CreateInstance<T>() where T:ScriptableObject,new()=>new T(); }
    public sealed class DisallowMultipleComponent:Attribute {}
    public class MonoBehaviour:Object { public GameObject gameObject; public T GetComponent<T>()=>gameObject.GetComponent<T>(); }
    public class GameObject:Object {
        readonly Dictionary<Type,object> components=new();
        public T GetComponent<T>()=>components.TryGetValue(typeof(T),out var value)?(T)value:default;
        public T AddComponent<T>() where T:MonoBehaviour,new() {
            var value=new T{gameObject=this};components[typeof(T)]=value;
            typeof(T).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance)?.Invoke(value,null);return value;
        }
    }
    public static class Mathf { public static int Max(int a,int b)=>Math.Max(a,b); public static int Min(int a,int b)=>Math.Min(a,b); }
    public static class Time {public static float unscaledTime;}
}
namespace EntityStates { public enum InterruptPriority { Any,Skill,Frozen } }
namespace RoR2.Skills {
    using UnityEngine; using RoR2;
    public class SkillDef:ScriptableObject {
        public class BaseSkillInstanceData {}
        public string skillName,skillNameToken,skillDescriptionToken,activationStateMachineName;
        public string[] keywordTokens; public Sprite icon;
        public SerializableEntityStateType activationState;
        public EntityStates.InterruptPriority interruptPriority;
        public float baseRechargeInterval;
        public int baseMaxStock=1,rechargeStock=1,requiredStock=1,stockToConsume=1;
        public bool hideStockCount,hideCooldown,fullRestockOnAssign=true,dontAllowPastMaxStocks,
            resetCooldownTimerOnUse,cancelSprintingOnActivation,forceSprintDuringState,canceledFromSprinting,
            isCombatSkill,mustKeyPress,suppressSkillActivation,beginSkillCooldownOnSkillEnd,isCooldownBlockedUntilManuallyReset;
        public int nativeExecutions;
        public virtual BaseSkillInstanceData OnAssigned(GenericSkill slot)=>new BaseSkillInstanceData();
        public virtual void OnUnassigned(GenericSkill slot) {}
        public virtual bool IsReady(GenericSkill slot)=>slot.stock>=requiredStock;
        public virtual bool CanExecute(GenericSkill slot)=>IsReady(slot);
        public virtual void OnExecute(GenericSkill slot) { nativeExecutions++;slot.stock-=stockToConsume; }
        public virtual void OnFixedUpdate(GenericSkill slot,float dt) {}
        public virtual int GetMaxStock(GenericSkill slot)=>baseMaxStock;
        public virtual Sprite GetCurrentIcon(GenericSkill slot)=>icon;
    }
}
namespace RoR2 {
    using UnityEngine;using RoR2.Skills;
    public readonly struct SerializableEntityStateType { public Type stateType{get;} public SerializableEntityStateType(Type type)=>stateType=type; }
    public class InputBankTest:Object { public class Button { public bool down; } public Button skill1=new(); }
    public class HealthComponent:Object { public bool alive=true; }
    public class CharacterBody:MonoBehaviour { public bool hasEffectiveAuthority=true; public HealthComponent healthComponent=new();public InputBankTest inputBank=new();public SkillLocator skillLocator=new();public CharacterMaster master=new(); }
    public class CharacterMaster:Object { public PlayerCharacterMasterController playerCharacterMasterController=new(); }
    public class PlayerCharacterMasterController:Object { public NetworkUser networkUser=new(); }
    public class NetworkUser:Object { public LocalUser localUser=new();public CameraRigController cameraRigController=new(); }
    public class LocalUser { public bool isUIFocused;public Rewired.Player inputPlayer=new(); }
    public class CameraRigController:Object { public bool isControlAllowed=true; }
    public class SkillLocator:Object { public GenericSkill primary,secondary,utility,special; }
    public class EntityStateMachine:MonoBehaviour {
        public string customName;public object state;
        public static EntityStateMachine FindByCustomName(GameObject owner,string name)=>owner.GetComponent<EntityStateMachine>();
    }
    public class GenericSkill:Object {
        public enum SkillOverridePriority { Contextual=4,Network=5 }
        sealed class Override { public object source;public SkillDef def;public SkillOverridePriority priority;public int stock;public float timer; }
        readonly List<Override> overrides=new();Override current;
        public CharacterBody characterBody;public EntityStateMachine stateMachine;
        public SkillDef skillDef,baseSkill;
        public SkillDef.BaseSkillInstanceData skillInstanceData;
        public int baseStock,maxStock,bonusStockFromBody;public float baseRechargeStopwatch,cooldownScale=1;
        public bool isCooldownBlocked;
        public int stock { get=>current==null?baseStock:current.stock;set {if(current==null)baseStock=value;else current.stock=value;} }
        public float rechargeStopwatch { get=>current==null?baseRechargeStopwatch:current.timer;set {if(current==null)baseRechargeStopwatch=value;else current.timer=value;} }
        public int OverrideCount=>overrides.Count;
        public GenericSkill(CharacterBody body,SkillDef def,EntityStateMachine machine) {
            characterBody=body;baseSkill=skillDef=def;stateMachine=machine;maxStock=def.baseMaxStock;
            skillInstanceData=def.OnAssigned(this);baseStock=maxStock;
        }
        public void SetSkillOverride(object source,SkillDef def,SkillOverridePriority priority) {
            overrides.Add(new Override{source=source,def=def,priority=priority});Pick();
        }
        public void UnsetSkillOverride(object source,SkillDef def,SkillOverridePriority priority) {
            overrides.RemoveAll(x=>ReferenceEquals(x.source,source)&&x.def==def&&x.priority==priority);Pick();
        }
        void Pick() {
            var next=overrides.OrderBy(x=>x.priority).LastOrDefault();var def=next?.def??baseSkill;
            if(def==skillDef){current=next;return;}
            skillDef.OnUnassigned(this);current=next;skillDef=def;
            skillInstanceData=def.OnAssigned(this);RecalculateMaxStock();
            if(def.fullRestockOnAssign&&stock<maxStock)stock=maxStock;
            if(def.dontAllowPastMaxStocks)stock=Math.Min(stock,maxStock);
        }
        public void OverrideMaxStock(int count)=>maxStock=count;
        public int GetBaseMaxStock()=>baseSkill.GetMaxStock(this)+bonusStockFromBody;
        public void SetBonusStockFromBody(int value){bonusStockFromBody=value;RecalculateMaxStock();}
        public void RecalculateMaxStock()=>maxStock=skillDef.GetMaxStock(this)+(skillDef.dontAllowPastMaxStocks?0:bonusStockFromBody);
        public void RechargeBaseSkill(float dt) {
            float original=baseSkill.baseRechargeInterval;
            float interval=Math.Min(original,Math.Max(.5f,original*cooldownScale));
            if(interval==0){baseStock=GetBaseMaxStock();baseRechargeStopwatch=0;return;}
            baseRechargeStopwatch+=dt;
            int gained=(int)(baseRechargeStopwatch/interval)*baseSkill.rechargeStock;
            baseStock+=gained;
            if(baseStock>=GetBaseMaxStock()){baseStock=GetBaseMaxStock();baseRechargeStopwatch=0;}
            else baseRechargeStopwatch-=gained*interval;
        }
        public bool ExecuteIfReady() {if(!skillDef.CanExecute(this))return false;skillDef.OnExecute(this);return true;}
    }
}
namespace HollowSaint {
    public static class Plugin { public static Logger Log=new(); public class Logger { public void LogError(object error)=>Console.Error.WriteLine(error); } }
}
namespace HollowSaint.FoundationKit {
    public static class KitRegistration { public const string CrownMachineName="Crown"; }
    public static class KitContent { public static RoR2.Skills.SkillDef AddSkillDef(RoR2.Skills.SkillDef def)=>def; }
    public static class KitIcons { public static UnityEngine.Sprite Sprite(string name)=>new(); }
}
namespace HollowSaint.FoundationKit.Gaze {
    public class GazeState { public bool PrimaryPulseReady=true; }
    public class GazeLockState {}
    public class GazeFuelController:UnityEngine.MonoBehaviour {
        public int EntryCapacity=5,AvailableEntry=5,Requests;
        public bool PulseRequestReady=true;
        public void RequestPulse()=>Requests++;
    }
}
namespace HollowSaint.FoundationKit.Stormspear {
    public class StormspearSkillDef:RoR2.Skills.SkillDef {}
    public class StormspearChargeState {}
    public class StormspearThrowState { public bool CooldownReleased; }
}
namespace HollowSaint.FoundationKit.ArcBolt { public class ArcBoltInputSkillDef:RoR2.Skills.SkillDef {} }
public static class RewiredConsts { public static class Action { public const int UICancel=15; } }
namespace Rewired { public class Player { public bool cancel;public int lastAction=-1;public bool GetButton(int action){lastAction=action;return cancel;} } }
