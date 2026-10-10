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
        // Success: Cloud permits exactly one pending bolt, while hand Orb/Circuit
        // cancel without release FX. Spear cancels only its configured hand,
        // pauses new shots, then resumes held Primary without a fresh press.
        foreach (byte kind in new byte[]{0,1,2}) foreach (bool pending in new[]{true,false})
        {
            Reset(); var body=Body(); body.GetComponent<DischargeMeter>().RegisterForTest(5);
            var bolt=PendingBolt(body); StoredChargeState gather=null;
            if (pending)
            {
                var slot=kind==1?body.skillLocator.secondary:body.skillLocator.special;
                slot.stock=1; slot.skillDef=new StoredChargeSkillDef { allowsUnchargedCast=kind==1, followsCloudFreeCast=kind==0 };
                if(kind==1) body.inputBank.skill2.down=true; else body.inputBank.skill4.down=true;
            }
            else gather=State(body,kind,true);
            bolt.Age(.12f); bolt.FixedUpdate();
            if (kind==0)
            {
                // 1.3.2: Thundercloud's gather leaves Primary firing; the bolt is not cut.
                Check(!bolt.outer.ended && HollowSaint.FoundationKit.Vfx.BodyCurrentFx.cancelled==0,"Thundercloud gather keeps the pending bolt: pending="+pending);
                Check(!StoredChargeState.BlocksPrimary(body),"Thundercloud gather never blocks Primary: pending="+pending);
                Check(RoR2.Projectile.ProjectileManager.instance.shots==1 && HollowSaint.FoundationKit.Vfx.BodyCurrentFx.released==1,"Thundercloud gather permits projectile and release current");
                bolt.Age(.2f); bolt.FixedUpdate();
                Check(RoR2.Projectile.ProjectileManager.instance.shots==1,"Thundercloud overlap cannot fire twice");
                bolt.OnExit(); gather?.OnExit(); continue;
            }
            bolt.OnExit();
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
        foreach (bool authority in new[]{true,false})
        {
            Reset(); var client=Body(); var bolt=PendingBolt(client);
            UnityEngine.Networking.NetworkServer.active=false; bolt.isAuthority=authority;
            bolt.Age(.12f); bolt.FixedUpdate(); bolt.OnExit();
            Check(RoR2.Projectile.ProjectileManager.instance.shots==(authority?1:0),"only the owning client sends a projectile request without server authority");
        }
        SpearPrimaryOverlap();
    }
    static void SpearPrimaryOverlap()
    {
        foreach (bool spearLeft in new[]{true,false}) foreach (bool left in new[]{true,false})
        foreach (bool pending in new[]{true,false}) foreach (bool alive in new[]{true,false})
        {
            Reset(); var body=Body(); body.inputBank.skill1.down=true;
            HollowSaint.FoundationKit.SpearDischarge.SpearCarry.left=spearLeft;
            if (!left) { var first=PendingBolt(body); first.OnExit(); }
            var bolt=PendingBolt(body);
            var machine=body.gameObject.AddComponent<EntityStateMachine>(); machine.customName="Spear";
            body.skillLocator.secondary.stock=1;
            body.skillLocator.secondary.skillDef=new StormspearSkillDef { mustKeyPress=true };
            if (pending) body.inputBank.skill2.down=true;
            else machine.state=new StormspearChargeState();
            body.healthComponent.alive=alive;
            var primary=new ArcBoltInputSkillDef(); var slot=new GenericSkill { characterBody=body,stock=1 };
            Check(!primary.CanExecute(slot),"spear/death closes native new-bolt admission");
            bolt.Age(.12f); bolt.FixedUpdate(); bolt.OnExit();
            bool cancelled=left==spearLeft || !alive;
            Check(bolt.outer.ended==cancelled && RoR2.Projectile.ProjectileManager.instance.shots==(cancelled?0:1),"spear overlap cancels only its hand; death cancels both");
            Check(HollowSaint.FoundationKit.Vfx.BodyCurrentFx.cancelled==(cancelled?1:0),"spear/death cancellation cleans pending arm current");
            if (!alive) continue;
            machine.state=new StormspearThrowState();
            body.inputBank.skill2.down=false;
            Check(!primary.IsReady(slot),"throw keeps new Primary paused");
            machine.SetNextStateToMain();
            Check(primary.CanExecute(slot) && body.inputBank.skill1.down,"held Primary resumes immediately after throw without a new press");
        }
        Reset(); var gaze=Body(); gaze.inputBank.skill2.down=true;
        gaze.skillLocator.secondary.stock=1; gaze.skillLocator.secondary.skillDef=new StormspearSkillDef { mustKeyPress=true };
        var crown=gaze.gameObject.AddComponent<EntityStateMachine>(); crown.customName="Crown"; crown.state=new HollowSaint.FoundationKit.Gaze.GazeState();
        Check(SpearPrimaryGate.Allows(gaze),"Secondary claimed by Gaze cannot falsely pause bolts");
        crown.SetNextStateToMain(); gaze.inputBank.skill2.hasPressBeenClaimed=true;
        Check(SpearPrimaryGate.Allows(gaze),"claimed held mustKeyPress spear cannot falsely pause bolts");
        gaze.inputBank.skill2.hasPressBeenClaimed=false;
        Check(!SpearPrimaryGate.Allows(gaze),"fresh ready spear press pauses new bolts");
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
