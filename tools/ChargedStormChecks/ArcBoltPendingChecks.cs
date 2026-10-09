using EntityStates;
using RoR2;
using UnityEngine;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ArcBolt;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.Stormspear;

static partial class Program
{
    static ArcBoltState PendingBolt(CharacterBody body)
    {
        RoR2.Projectile.ProjectileManager.instance.shots = 0;
        HollowSaint.FoundationKit.Vfx.BodyCurrentFx.released = 0;
        HollowSaint.FoundationKit.Vfx.BodyCurrentFx.cancelled = 0;
        var machine = body.gameObject.AddComponent<EntityStateMachine>();
        var bolt = new ArcBoltState { characterBody=body, outer=machine, isAuthority=true };
        machine.state=bolt; bolt.OnEnter(); bolt.Age(.04f); bolt.FixedUpdate();
        Check(!machine.ended && RoR2.Projectile.ProjectileManager.instance.shots==0,"Primary enters its pre-fire window");
        return bolt;
    }
    static void PendingPrimary()
    {
        // Success: enter the real Primary, then stagger gather input/state before
        // discharge. No projectile or release FX leaks; Circuit overhead stays usable.
        foreach (byte kind in new byte[]{0,1,2}) foreach (bool pending in new[]{true,false})
        {
            Reset(); var body=Body(); body.GetComponent<DischargeMeter>().RegisterForTest(5);
            var bolt=PendingBolt(body); StoredChargeState gather=null;
            if (pending)
            {
                var slot=kind==1?body.skillLocator.secondary:body.skillLocator.special;
                slot.stock=1; slot.skillDef=new StoredChargeSkillDef { allowsUnchargedCast=kind==1 };
                if(kind==1) body.inputBank.skill2.down=true; else body.inputBank.skill4.down=true;
            }
            else gather=State(body,kind,true);
            bolt.Age(.12f); bolt.FixedUpdate(); bolt.OnExit();
            Check(bolt.outer.ended && RoR2.Projectile.ProjectileManager.instance.shots==0,"staggered gather cancels pending projectile: "+kind+" pending="+pending);
            Check(HollowSaint.FoundationKit.Vfx.BodyCurrentFx.released==0 && HollowSaint.FoundationKit.Vfx.BodyCurrentFx.cancelled==1,"cancelled windup stops its arm current without discharge");
            gather?.OnExit();
        }
        foreach (bool closes in new[]{false,true})
        {
            Reset(); var body=Body(); body.GetComponent<DischargeMeter>().RegisterForTest(5);
            var bolt=PendingBolt(body); StormspearCharge.crown=true;
            var gather=State(body,1,true); if(closes) StormspearCharge.crown=false;
            bolt.Age(.12f); bolt.FixedUpdate();
            Check(RoR2.Projectile.ProjectileManager.instance.shots==(closes?0:1),"Circuit expiry checked at actual Primary discharge");
            Check(bolt.outer.ended==closes,"overhead gather retains entered Primary until normal completion");
            bolt.Age(.2f); bolt.FixedUpdate();
            Check(RoR2.Projectile.ProjectileManager.instance.shots==(closes?0:1),"entered Primary cannot discharge twice");
            bolt.OnExit();gather.OnExit();
        }
        Reset(); var idle=Body();var normal=PendingBolt(idle);
        normal.Age(.12f);normal.FixedUpdate();normal.Age(.5f);normal.FixedUpdate();normal.OnExit();
        Check(RoR2.Projectile.ProjectileManager.instance.shots==1 && normal.outer.ended,"ordinary Primary retains one shot and unchanged cadence");
    }
}
namespace HollowSaint.FoundationKit.ArcBolt { public static class ArcBoltProjectile { public static GameObject Prefab=new(); } }
namespace RoR2
{
    public static class Util
    {
        public static Quaternion QuaternionSafeLookRotation(Vector3 direction)=>new();
        public static bool CharacterRaycast(GameObject obj,Ray ray,out RaycastHit hit,float range,int mask,QueryTriggerInteraction query) { hit=new();return false; }
    }
}
namespace RoR2.Projectile
{
    public struct FireProjectileInfo { public GameObject projectilePrefab,owner; public Vector3 position; public Quaternion rotation; public float damage,force; public bool crit; public DamageColorIndex damageColorIndex; public DamageTypeCombo damageTypeOverride; }
    public class ProjectileManager { public static ProjectileManager instance=new(); public int shots; public void FireProjectile(FireProjectileInfo info) { shots++; } }
}
