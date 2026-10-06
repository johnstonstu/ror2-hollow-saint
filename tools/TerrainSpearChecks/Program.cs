using System.Reflection;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ArcBolt;
using HollowSaint.FoundationKit.Stormspear;
using RoR2;
using UnityEngine;

// Success: proposed correction cannot turn a clear physical path into a world hit;
// muzzle relocation cannot cross a wall; spear burst excludes primary/occluded/out-of-range
// victims, falls off without changing proc/crit/mask; native mapped input rejects before
// stock/cooldown use and requires release after blocked holds, with Circuit exception.
int checks=0;
void Check(bool yes,string why) {checks++;if(!yes)throw new Exception(why);}
void Near(float a,float b,string why)=>Check(Math.Abs(a-b)<0.0002f,why+$" ({a} vs {b})");
foreach(float radius in new[]{.35f,.75f})
foreach(float grade in new[]{-.4f,0f,.4f})
{
    var normal=new Vector3(-grade,1,0).normalized;
    Physics.Planes.Clear(); Physics.Planes.Add((normal,0));
    Vector3 start=normal*(radius+.2f), clear=new Vector3(1,grade,0).normalized;
    Vector3 blocked=(clear-normal*.35f).normalized;
    Check(ProjectileWorldClearance.Clear(start,clear,3,radius),"flat/up/down original clears sphere");
    Check(!ProjectileWorldClearance.TryCorrection(start,clear,blocked,3,radius,out var kept),"reject steering into terrain");
    Near(Vector3.Distance(kept,clear),0,"preserve original heading");
    Check(ProjectileWorldClearance.TryCorrection(start,clear,clear,3,radius,out kept),"accept clear correction");
    // A center ray stays free at the end while the sphere penetrates the surface.
    var edge=(clear-normal*.09f).normalized;
    Check(!Physics.Linecast(start,start+edge*3,1,QueryTriggerInteraction.Ignore),"center ray alone clears");
    Check(!ProjectileWorldClearance.Clear(start,edge,3,radius),"radius catches terrain edge");
    Check(!ProjectileWorldClearance.Clear(normal*(radius*.5f),clear,3,radius),"embedded start cannot be assisted");
    Physics.Planes.Clear(); Physics.Planes.Add((new Vector3(-1,0,0),-2));
    Vector3 safe=ProjectileWorldClearance.LaunchOrigin(Vector3.zero,new Vector3(3,0,0),radius);
    Check(safe.x<2-radius && safe.x>=0,"muzzle clamps to player's side of wall");
    Check(ProjectileWorldClearance.Clear(safe,new Vector3(-1,0,0),1,radius),"clamped start is not embedded");
    Near(ProjectileWorldClearance.LaunchOrigin(Vector3.zero,new Vector3(1,0,0),radius).x,1,"clear muzzle retained");
    Near(ProjectileWorldClearance.LaunchOrigin(new Vector3(3,0,0),new Vector3(3.5f,0,0),radius).x,3.5f,"blocked anchor retains native muzzle");
}
Physics.Planes.Clear();
// Execute the real steering component: line-clear target, clear current path,
// but the proposed radius-aware next step grazes the floor.
foreach(float radius in new[]{.35f,.75f})
{
    var projectile=new GameObject();var controller=projectile.AddComponent<RoR2.Projectile.ProjectileController>();
    controller.teamFilter=projectile.AddComponent<TeamComponent>();
    var sphere=projectile.AddComponent<SphereCollider>();sphere.radius=radius;
    var flight=projectile.AddComponent<RoR2.Projectile.ProjectileSimple>();flight.desiredForwardSpeed=radius<.5f?150:120;
    var steering=projectile.AddComponent<ProjectileAimForgiveness>();
    projectile.GetComponent<Transform>().position=new Vector3(0,radius+.005f,0);
    var go=new GameObject();var victim=go.AddComponent<HealthComponent>();var box=go.AddComponent<HurtBox>();
    box.healthComponent=victim;box.collider=go.AddComponent<Collider>();
    box.transform.position=new Vector3(40,.1f,0);box.collider.bounds=new Bounds{center=box.transform.position};
    BullseyeSearch.Candidates=new(){box};Physics.Planes.Add((Vector3.up,0));
    typeof(ProjectileAimForgiveness).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(steering,null);
    Check(steering.LockedTarget==box,"native component acquires center-LOS target");
    typeof(ProjectileAimForgiveness).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(steering,null);
    Check(!steering.enabled,"native component retires blocked proposed correction");
    Near(steering.TurnUsed,0,"blocked correction does not spend angular budget");
    Near(Vector3.Distance(steering.transform.forward,new Vector3(1,0,0)),0,"native component preserves unassisted clear heading");
    Physics.Planes.Clear();
}
CharacterBody Body()
{
    var go=new GameObject();var b=go.AddComponent<CharacterBody>();b.healthComponent=go.AddComponent<HealthComponent>();
    b.teamComponent=go.AddComponent<TeamComponent>();b.skillLocator=new SkillLocator();
    return b;
}
HurtBox Target(float x,bool alive=true)
{
    var go=new GameObject();var h=go.AddComponent<HealthComponent>();h.alive=alive;
    var box=go.AddComponent<HurtBox>();box.healthComponent=h;box.collider=go.AddComponent<Collider>();
    box.transform.position=new Vector3(x,0,0);box.collider.bounds=new Bounds {center=box.transform.position};return box;
}
var attacker=Body();var primary=Target(0);var near=Target(1);var mid=Target(5);var edgeVictim=Target(10);var outside=Target(10.01f);var dead=Target(2,false);
BullseyeSearch.Candidates=new(){primary,near,mid,edgeVictim,outside,dead};KitUtil.Reports.Clear();
var hits=SpearBurstDamage.Apply(attacker,Vector3.zero,Vector3.zero,10,1134,true,.5f,
    new DamageTypeCombo {value=3},primary.healthComponent,new ProcChainMask {value=9});
Check(hits.Count==3 && primary.healthComponent.damages.Count==0,"only live other enemies in radius hit");
Near(near.healthComponent.damages.Single().damage,1077.3f,"near falloff");
Near(mid.healthComponent.damages.Single().damage,850.5f,"half-radius falloff");
Near(edgeVictim.healthComponent.damages.Single().damage,567,"edge falloff");
Check(KitUtil.Reports.Count==3,"one native proc report per accepted enemy");
foreach(var info in KitUtil.Reports) { Near(info.procCoefficient,.5f,"proc unchanged by falloff");Check(info.crit && info.procChainMask.value==9 && info.damageType.value==3,"crit/mask/type preserved"); }
Physics.Planes.Add((new Vector3(-1,0,0),-2));KitUtil.Reports.Clear();
Check(SpearBurstDamage.Apply(attacker,Vector3.zero,Vector3.zero,10,1134,false,.5f,new(),primary.healthComponent,new()).Count==1,"wall excludes farther targets");
Check(KitUtil.Reports.Count==1,"no blocked-target proc hooks");Physics.Planes.Clear();
for(int i=0;i<=100;i++) {Near(SpearBurstPolicy.Scale(i*.1f,10),1-i*.005f,"monotonic conservative falloff");}
foreach(float d in new[]{-1,float.NaN,float.PositiveInfinity,10.001f})Near(SpearBurstPolicy.Scale(d,10),0,"invalid/outside excluded");
Near(SpearBurstPolicy.Scale(5,5),.5f,"ground edge fraction");
Near(SpearBurstPolicy.Scale(0,0),0,"zero radius excluded");
BullseyeSearch.Candidates=Enumerable.Range(0,70).Select(i=>Target(1+i*.01f)).ToList();
Check(SpearBurstDamage.Apply(attacker,Vector3.zero,Vector3.zero,10,1,false,.5f,new(),null,new()).Count==64,"target cap preserved");

foreach(bool heldBefore in new[]{false,true})
foreach(bool throwing in new[]{false,true})
{
    var b=Body();var machine=b.gameObject.AddComponent<EntityStateMachine>();machine.customName="Spear";
    var slot=new GenericSkill {characterBody=b,skillDef=new ArcBoltInputSkillDef(),stock=1,rechargeStopwatch=2};
    var secondary=new GenericSkill {characterBody=b,skillDef=new StormspearSkillDef(),stock=0};b.skillLocator.secondary=secondary;
    b.inputBank.skill1.down=heldBefore;
    machine.state=throwing?new StormspearThrowState():new StormspearChargeState();
    b.inputBank.skill1.down=true;
    for(int i=0;i<8;i++)Check(!slot.ExecuteIfReady(),"normal charge/throw blocks mapped held/pressed primary");
    Check(slot.stock==1 && slot.rechargeStopwatch==2 && slot.executions==0,"blocked input consumes nothing");
    machine.state=new EntityStates.EntityState();
    Check(!slot.ExecuteIfReady(),"no queued primary on action exit/cancel");
    b.inputBank.skill1.down=false;Check(slot.skillDef.IsReady(slot),"release rearms outside action");
    b.inputBank.skill1.down=true;Check(slot.ExecuteIfReady(),"fresh repress fires normally");
    slot.stock=1;Check(slot.ExecuteIfReady(),"normal held-primary cadence resumes");
}
var circuit=Body();var spearMachine=circuit.gameObject.AddComponent<EntityStateMachine>();spearMachine.customName="Spear";spearMachine.state=new StormspearChargeState();
var primarySlot=new GenericSkill {characterBody=circuit,skillDef=new ArcBoltInputSkillDef()};circuit.circuit=true;circuit.inputBank.skill1.down=true;
for(int i=0;i<6;i++){primarySlot.stock=1;Check(primarySlot.ExecuteIfReady(),"Circuit permits parallel primary");}
circuit.circuit=false;primarySlot.stock=1;Check(!primarySlot.ExecuteIfReady(),"Circuit ending during charge blocks pending primary");
spearMachine.state=new EntityStates.EntityState();Check(!primarySlot.ExecuteIfReady(),"closing Circuit cannot queue release shot");
circuit.inputBank.skill1.down=false;Check(primarySlot.skillDef.IsReady(primarySlot),"release clears closing-Circuit latch");
circuit.inputBank.skill1.down=true;circuit.skillLocator.secondary=new GenericSkill{characterBody=circuit,skillDef=new StormspearSkillDef()};circuit.inputBank.skill2.down=true;
Check(!primarySlot.ExecuteIfReady() && circuit.skillLocator.secondary.stock==1,"simultaneous ready spear blocks before primary stock use");
circuit.inputBank.skill2.down=false;circuit.inputBank.skill1.down=false;primarySlot.skillDef.IsReady(primarySlot);
circuit.inputBank.skill1.down=true;Check(primarySlot.ExecuteIfReady(),"repeated action restores normal native input");
primarySlot.stock=1;spearMachine.state=new StormspearThrowState();Check(!primarySlot.ExecuteIfReady(),"throw gate established");
circuit.healthComponent.alive=false;Check(!primarySlot.ExecuteIfReady(),"death blocks and resets latch");
circuit.healthComponent.alive=true;spearMachine.state=new EntityStates.EntityState();Check(primarySlot.ExecuteIfReady(),"new living idle action is not permanently locked");
var gate=circuit.GetComponent<SpearPrimaryGate>();typeof(SpearPrimaryGate).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gate,null);
primarySlot.stock=1;Check(primarySlot.ExecuteIfReady(),"disable reset leaves no stale latch");
spearMachine.state=new StormspearChargeState();var gazeOverride=new GenericSkill {characterBody=circuit,skillDef=new RoR2.Skills.SkillDef()};
Check(gazeOverride.ExecuteIfReady(),"restriction never mutates Gaze/native override input or stock");
foreach(var action in new EntityStates.EntityState[]{new StormspearChargeState(),new StormspearThrowState()})
{
    var b=Body();var m=b.gameObject.AddComponent<EntityStateMachine>();m.customName="Spear";m.state=action;
    var p=new GenericSkill{characterBody=b,skillDef=new ArcBoltInputSkillDef()};b.circuit=true;b.inputBank.skill1.down=true;
    Check(p.ExecuteIfReady(),"Circuit exception applies to charge and release states");
    p.stock=1;b.circuit=false;Check(!p.ExecuteIfReady(),"closing Circuit during either phase suppresses primary");
    m.state=new EntityStates.EntityState();Check(!p.ExecuteIfReady(),"closed-Circuit hold cannot queue a shot");
    b.inputBank.skill1.down=false;Check(p.skillDef.IsReady(p),"release restores readiness after either phase");
}
var late=Body();var lateSlot=new GenericSkill{characterBody=late,skillDef=new ArcBoltInputSkillDef()};
Check(lateSlot.skillDef.IsReady(lateSlot),"initial body without spear machine remains usable");
var lateMachine=late.gameObject.AddComponent<EntityStateMachine>();lateMachine.customName="Spear";lateMachine.state=new StormspearChargeState();
late.inputBank.skill1.down=true;Check(!lateSlot.ExecuteIfReady(),"late native machine initialization cannot bypass gate");
Console.WriteLine($"Terrain/spear/source-input checks: {checks} assertions passed (synthetic world/native input adapters; no runtime claim).");
