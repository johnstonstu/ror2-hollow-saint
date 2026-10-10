using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.ContentManagement { public class ContentPack {} }

namespace EntityStates
{
    public class EntityState { }
    public enum InterruptPriority { Skill, PrioritySkill }
    public class BaseSkillState : EntityState
    {
        public RoR2.CharacterBody characterBody;
        public RoR2.GenericSkill activatorSkillSlot;
        public RoR2.EntityStateMachine outer;
        public bool isAuthority;
        protected GameObject gameObject => characterBody.gameObject;
        protected float attackSpeedStat => 1f;
        protected float damageStat => characterBody.damage;
        protected bool RollCrit() => characterBody.RollCrit();
        protected void AddRecoil(float a,float b,float c,float d) {}
        protected float fixedAge;
        protected RoR2.InputBankTest inputBank => characterBody.inputBank;
        protected RoR2.SkillLocator skillLocator => characterBody.skillLocator;
        protected Ray GetAimRay() => new() { direction = inputBank.aimDirection };
        protected object GetModelAnimator() => null;
        public void Age(float age) { fixedAge = age; }
        public virtual void OnEnter() {} public virtual void OnExit() {}
        public virtual void Update() {} public virtual void FixedUpdate() {}
        public virtual void OnSerialize(NetworkWriter w) {} public virtual void OnDeserialize(NetworkReader r) {}
        public virtual InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }
}
namespace RoR2
{
    public class Stage : UnityEngine.Object { public static Stage instance = new(); }
    public class BuffDef : UnityEngine.Object { public int buffIndex; public bool canStack; }
    public enum TeamIndex { Player, Monster, None }
    public class TeamComponent : UnityEngine.Object { public TeamIndex teamIndex; }
    public class CharacterBody : MonoBehaviour
    {
        readonly Dictionary<int,int> buffs = new();
        public HealthComponent healthComponent;
        public InputBankTest inputBank = new();
        public SkillLocator skillLocator;
        public CharacterMaster master;
        public TeamComponent teamComponent = new();
        public HurtBox mainHurtBox;
        public float damage = 10;
        public float attackSpeed = 1;
        public bool hasEffectiveAuthority = true;
        public Vector3 corePosition => transform.position;
        public Vector3 footOffset;
        public Vector3 footPosition => corePosition + footOffset;
        public void SetAimTimer(float seconds) {}
        public int GetBuffCount(BuffDef b) => buffs.GetValueOrDefault(b.buffIndex);
        public void SetBuffCount(int id,int count) { buffs[id] = count; }
        public void AddBuff(BuffDef b) { buffs[b.buffIndex] = GetBuffCount(b) + 1; }
        public void AddTimedBuff(BuffDef b,float seconds) { buffs[b.buffIndex] = b.canStack ? GetBuffCount(b)+1 : 1; }
        public void ClearTimedBuffs(int index) { buffs[index]=0; }
        public bool RollCrit() => false;
    }
    public class HealthComponent : Component
    {
        public CharacterBody body;
        public bool alive = true;
        public List<DamageInfo> received = new();
        public void TakeDamage(DamageInfo info)
        {
            if (!NetworkServer.active || HollowSaint.FoundationKit.Storm.StormServer.Depth <= 0)
                throw new Exception("Unowned damage or direct spender Static feedback.");
            received.Add(info);
        }
    }
    public class HurtBox : Component { public HealthComponent healthComponent; }
    public class CharacterMaster : UnityEngine.Object
    { public CharacterBody body; public PlayerCharacterMasterController playerCharacterMasterController = new(); public CharacterBody GetBody() => body; }
    public class PlayerCharacterMasterController : UnityEngine.Object { public NetworkUser networkUser = new(); }
    public class NetworkUser : UnityEngine.Object { public NetworkConnection connectionToClient = new(); }
    public class InputBankTest : UnityEngine.Object
    {
        public class Button { public bool down, justPressed, hasPressBeenClaimed; }
        public Button skill1 = new(), skill2 = new(), skill3 = new(), skill4 = new();
        public Vector3 aimDirection = Vector3.forward;
    }
    public class SkillLocator : UnityEngine.Object { public GenericSkill special, secondary, utility; }
    public class GenericSkill : UnityEngine.Object
    {
        public CharacterBody characterBody;
        public Skills.SkillDef skillDef;
        public EntityStateMachine stateMachine;
        public int stock, maxStock = 1;
        public float rechargeStopwatch = 2, finalRechargeInterval = 10;
        public int bonusStockFromBody;
        public float CalculateFinalRechargeInterval() => finalRechargeInterval;
        public void AddOneStock() { stock++; rechargeStopwatch = 0; }
        public bool CanExecute() => skillDef != null && skillDef.CanExecute(this);
        public int executions;
        public bool ExecuteIfReady() { if(!CanExecute()) return false;stock--;executions++;return true; }
    }
    public class EntityStateMachine : Component
    {
        public string customName;
        public EntityStates.EntityState state;
        public bool ended;
        public static EntityStateMachine FindByCustomName(GameObject obj,string name) => obj.GetComponents<EntityStateMachine>().FirstOrDefault(s => s.customName == name);
        public void SetNextStateToMain() { ended = true; state = new EntityStates.EntityState(); }
    }
    public enum DamageSource { Primary, Secondary, Special }
    public enum DamageType { Generic } public enum DamageTypeExtended { Generic }
    public enum DamageColorIndex { Default, Electrocution }
    public struct DamageTypeCombo { public DamageTypeCombo(DamageType a,DamageTypeExtended b,DamageSource c) {} }
    public class DamageInfo
    {
        public float damage, procCoefficient;
        public bool crit;
        public GameObject attacker, inflictor;
        public Vector3 position;
        public DamageColorIndex damageColorIndex;
        public DamageTypeCombo damageType;
        public HurtBox inflictedHurtbox;
    }
    public class TeamMask { public TeamIndex team; public bool HasTeam(TeamIndex target) => target!=team && target!=TeamIndex.None; public static TeamMask GetEnemyTeams(TeamIndex team) => new() { team=team }; }
    public class BullseyeSearch
    {
        public static List<HurtBox> candidates = new();
        public Vector3 searchOrigin, searchDirection;
        public float minDistanceFilter, maxDistanceFilter, minAngleFilter, maxAngleFilter;
        public TeamMask teamMaskFilter;
        public bool filterByDistinctEntity, filterByLoS;
        public enum SortMode { Distance } public SortMode sortMode;
        public void RefreshCandidates() {}
        public IEnumerable<HurtBox> GetResults() => candidates.Where(b =>
            b.healthComponent.body.teamComponent.teamIndex != teamMaskFilter.team &&
            Vector3.Distance(searchOrigin,b.transform.position) <= maxDistanceFilter &&
            Vector3.Dot(searchDirection.normalized,(b.transform.position-searchOrigin).normalized) >= MathF.Cos(maxAngleFilter*MathF.PI/180)-.00001f &&
            (!filterByLoS || !Physics.Linecast(searchOrigin,b.transform.position,1,QueryTriggerInteraction.Ignore)))
            .OrderBy(b => Vector3.Distance(searchOrigin,b.transform.position));
    }
    public class LayerIndex { public int mask; public static LayerIndex world = new(){mask=1}, entityPrecise = new(){mask=2}; }
}
namespace RoR2.Skills
{
    public class SkillDef : UnityEngine.Object
    {
        public virtual bool IsReady(RoR2.GenericSkill slot) => slot.stock > 0;
        public virtual bool CanExecute(RoR2.GenericSkill slot) => IsReady(slot);
        public virtual void OnFixedUpdate(RoR2.GenericSkill slot,float dt) {}
    }
}
