using System;
using System.Linq;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.HollowedOrb;
using HollowSaint.FoundationKit.Thundercloud;

// Success: exact shared-bank accounting; authenticated native release; finite
// fresh-before-revisit flights that latch then burst; per-pulse area strikes. Explicit native
// adapters do not certify Unity visuals, collisions or actual multiplayer.
static partial class Program
{
    static int checks;
    static void Check(bool condition,string reason) { checks++; if (!condition) throw new Exception(reason); }
    static void Near(float value,float expected,string reason) => Check(Math.Abs(value-expected)<.001f,reason);
    static CharacterBody Body(float z=0,TeamIndex team=TeamIndex.Player)
    {
        var obj = new GameObject(); obj.transform.position = new(0,0,z);
        var b = obj.AddComponent<CharacterBody>(); b.teamComponent.teamIndex=team;
        b.healthComponent = obj.AddComponent<HealthComponent>(); b.healthComponent.body=b;
        b.mainHurtBox = obj.AddComponent<HurtBox>(); b.mainHurtBox.healthComponent=b.healthComponent;
        b.master = new() { body=b };
        b.skillLocator = new() { special=new(){characterBody=b}, secondary=new(){characterBody=b}, utility=new(){characterBody=b} };
        obj.AddComponent<NetworkIdentity>(); obj.AddComponent<DischargeMeter>(); obj.AddComponent<StoredChargeDriver>();
        return b;
    }
    static void Reset()
    {
        NetworkServer.active=true; Physics.AimHit=null; Physics.Obstructed=(a,b)=>false;
        Physics.Collisions=Array.Empty<RaycastHit>(); Physics.WorldOverlap=false; BullseyeSearch.candidates.Clear();
        ChargedStormEffects.strikes.Clear(); ChargedStormEffects.strikeStrokes.Clear(); ChargedStormEffects.bursts.Clear();
        ChargedStormEffects.lastOrbTarget=null; HollowSaint.FoundationKit.Storm.StormServer.primes.Clear(); Stage.instance=new();
        Physics.SphereObstruction=null; HollowSaint.FoundationKit.Stormspear.StormspearCharge.crown=false;
    }
    static void Ledger()
    {
        var l = new StoredChargeCastLedger();
        Check(!l.Begin(0,5) && !l.Begin(1,0),"zero token/bank rejects");
        Check(l.Begin(1,3) && !l.Begin(2,5),"one active gathering lease");
        Near(StoredChargeCastLedger.Gathered(.119f,5),0,"first-charge threshold");
        Near(StoredChargeCastLedger.Gathered(.12f,5),1,"first charge");
        Near(StoredChargeCastLedger.Gathered(.42f,5),2,"second charge");
        Check(StoredChargeCastLedger.Gathered(float.NaN,5)==0,"NaN timing rejects");
        int bank=5;
        Check(!l.Spend(2,1,1f,ref bank,out _) && bank==5,"stale token cannot debit");
        Check(!l.Spend(1,4,2f,ref bank,out _) && bank==5,"new gains cannot expand frozen entry");
        Check(l.Spend(1,3,.42f,ref bank,out int spent) && spent==2 && bank==3,"server timing clamps spend and preserves gains");
        Check(!l.Spend(1,1,2f,ref bank,out _) && bank==3,"duplicate release cannot debit");
        Check(!l.Begin(1,5) && l.Begin(2,5),"old cast cannot reopen");
        l.Cancel(1); Check(l.Active,"stale cancellation cannot cancel current cast");
        l.Cancel(2); Check(!l.Active && bank==3,"cancellation preserves bank");
        for(int entry=1;entry<=20;entry++) for(int ask=1;ask<=entry;ask++)
        {
            var fresh=new StoredChargeCastLedger(); fresh.Begin(1,entry); int actual=entry+2;
            Check(fresh.Spend(1,ask,10,ref actual,out spent) && spent==ask && actual==entry+2-ask,"partial bank conservation across capacities");
        }
    }
    static StoredChargeState State(CharacterBody b,byte kind,bool authority,uint token=8)
    {
        var machine=b.gameObject.AddComponent<EntityStateMachine>(); machine.customName=kind==1?"Spear":"Crown";
        StoredChargeState s=kind==0?new ThundercloudState():kind==1?new HollowedOrbState():new HollowSaint.FoundationKit.OpenCircuit.OpenCircuitState();
        s.characterBody=b; s.isAuthority=authority; s.outer=machine;
        s.activatorSkillSlot=kind==1?b.skillLocator.secondary:b.skillLocator.special; machine.state=s;
        s.activatorSkillSlot.stateMachine=machine;
        if(!authority)
        { var w=new NetworkWriter(); w.Write(token);w.Write((byte)5);w.Write(false);w.Write(0f); s.OnDeserialize(new(w.Stream.ToArray())); }
        s.OnEnter(); return s;
    }
    static StoredChargeTransport.Packet Request(byte kind,uint token=8,byte count=2) =>
        new(){cast=token,kind=kind,count=count,direction=Vector3.forward};
    static void Transport()
    {
        Reset();var client=new NetworkClient();NetworkClient.allClients.Add(client);
        StoredChargeTransport.Install(); StoredChargeTransport.Install();
        RoR2.Networking.NetworkManagerSystem.StartServer();RoR2.Networking.NetworkManagerSystem.StartClient(client);
        Check(NetworkServer.handlers.ContainsKey(29040) && client.handlers.ContainsKey(29041),"reconnect registers handlers idempotently");
        var packet=Request(1,98,4);packet.owner=new(73);packet.direction=new(1,2,3);packet.duration=1.7f;packet.reply=true;
        var writer=new NetworkWriter();packet.Serialize(writer);var copy=new StoredChargeTransport.Packet();copy.Deserialize(new(writer.Stream.ToArray()));
        Check(copy.owner.Value==73 && copy.cast==98 && copy.kind==1 && copy.count==4 && !copy.cancel && copy.reply,"packet identity/flags round trip");
        Near(copy.direction.y,2,"packet aim round trip");Near(copy.duration,1.7f,"packet duration round trip");
        var b=Body();b.GetComponent<DischargeMeter>().RegisterForTest(5);var state=State(b,1,false);state.Age(.8f);
        NetworkServer.objects[1]=b.gameObject;
        NetworkServer.handlers[29040](new(){packet=Request(1),conn=new NetworkConnection()});
        Check(!state.Released,"handler rejects foreign owner");
        var request=Request(1);request.owner=new(1);
        NetworkServer.handlers[29040](new(){packet=request,conn=b.master.playerCharacterMasterController.networkUser.connectionToClient});
        Check(state.Released && b.GetComponent<DischargeMeter>().Charge==3,"handler routes authenticated active native state");state.OnExit();
        StoredChargeTransport.Uninstall();
        Check(!NetworkServer.handlers.ContainsKey(29040) && !client.handlers.ContainsKey(29041),"teardown removes only installed handlers");
        NetworkServer.handlers[29040]=_=>{};
        StoredChargeTransport.Install();
        Check(!StoredChargeTransport.Ready,"foreign message ID collision disables new admission without replacing handler");StoredChargeTransport.Uninstall();
        Check(NetworkServer.handlers.ContainsKey(29040),"teardown preserves foreign handler");NetworkServer.handlers.Clear();NetworkClient.allClients.Clear();StoredChargeTransport.Install();
    }
    static void Runtime()
    {
        foreach(byte kind in new byte[]{0,1,2})
        {
            Reset(); var b=Body();var m=b.GetComponent<DischargeMeter>();m.RegisterForTest(5);
            var def=new StoredChargeSkillDef();var slot=b.skillLocator.special;slot.stock=1;
            Check(def.CanExecute(slot),"charged native slot admits");
            slot.skillDef=def;b.inputBank.skill4.down=true;
            var primary=new HollowSaint.FoundationKit.ArcBolt.ArcBoltInputSkillDef();
            var primarySlot=new GenericSkill(){characterBody=b,stock=1};
            Check(!primary.CanExecute(primarySlot),"same-tick pending Special blocks Primary before native activation");
            // Thundercloud's first charge waits for the free-cast grace (.3 s); .6 s gathers two.
            var s=State(b,kind,false);s.Age(kind==1 ? .8f : kind==0 ? .6f : .42f);
            Check(!primary.IsReady(primarySlot),"gathering blocks Primary native readiness");
            var compatible=new StoredChargeCompatibleSkillDef();var otherSlot=new GenericSkill(){characterBody=b,stock=1,skillDef=compatible};
            Check(!compatible.CanExecute(otherSlot) && !new HollowSaint.FoundationKit.Stormspear.StormspearSkillDef().IsReady(otherSlot),"gather blocks native Utility, other Special and Spear before stock use");
            Check(!def.CanExecute(slot) && m.Charge==5,"gather blocks competing activation without spending");
            s.ServerRequest(Request(kind),new NetworkConnection(),false);
            Check(m.Charge==5 && !s.Released,"foreign connection cannot spend");
            var conn=b.master.playerCharacterMasterController.networkUser.connectionToClient;
            s.ServerRequest(Request(kind,7),conn,false);
            Check(m.Charge==5,"stale release cannot spend");
            s.ServerRequest(Request(kind),conn,false);
            Check(s.Released && m.Charge==3 && b.GetComponent<StoredChargeDriver>().launches==1,"authenticated remote release spends once");
            Check(StoredChargeChargeFx.confirmed==2,"remote release presentation uses confirmed spend count rather than predicted gathered tier");
            s.ServerRequest(Request(kind),conn,false);
            Check(m.Charge==3 && b.GetComponent<StoredChargeDriver>().launches==1,"duplicate remote release does not launch");
            s.OnExit();Check(!m.StoredCastGathering,"release exit clears ownership");
            b.inputBank.skill4.down=false;Check(primary.CanExecute(primarySlot),"primary resumes after release");

            Reset();b=Body();m=b.GetComponent<DischargeMeter>();m.RegisterForTest(3);s=State(b,kind,true);s.Age(1);
            b.skillLocator.utility.stock=1;b.skillLocator.utility.skillDef=compatible;
            StoredChargeUtilityExit.Queue(b);var exit=b.GetComponent<StoredChargeUtilityExit>();
            Invoke(exit,"FixedUpdate");Check(b.skillLocator.utility.executions==0,"Utility queue waits for gather cancellation");
            var w=new NetworkWriter();s.OnSerialize(w);var r=new NetworkReader(w.Stream.ToArray());uint id=r.ReadUInt32();
            var cancel=Request(kind,id);cancel.cancel=true;s.ServerRequest(cancel,null,true);s.OnExit();
            s.outer.SetNextStateToMain();Invoke(exit,"FixedUpdate");Invoke(exit,"FixedUpdate");
            Check(b.skillLocator.utility.executions==1 && b.skillLocator.utility.stock==0,"queued native Utility executes exactly once after movement ownership returns");
            Check(m.Charge==3 && s.activatorSkillSlot.stock==1,"host cancellation returns native stock without consuming bank");
            Near(s.activatorSkillSlot.rechargeStopwatch,2,"refund preserves recharge queue");

            Reset();b=Body();m=b.GetComponent<DischargeMeter>();m.RegisterForTest(5);s=State(b,kind,false);
            Check(!m.TryClaimSpearPrayer(out _) && m.Charge==5,"pending gather blocks full-bank spear claim");
            m.Consume();Check(m.Charge==5,"external bulk consume cannot steal pending bank");
            s.Age(1);var invalid=Request(kind);invalid.direction=new(float.NaN,0,1);
            s.ServerRequest(invalid,b.master.playerCharacterMasterController.networkUser.connectionToClient,false);
            Check(!s.Released && m.Charge==5 && !m.StoredCastGathering,"invalid aim cancels without spending");s.OnExit();

            Reset();b=Body();m=b.GetComponent<DischargeMeter>();m.RegisterForTest(0);slot=b.skillLocator.special;slot.stock=1;
            Check(!def.IsReady(slot) && !def.CanExecute(slot) && slot.stock==1,"empty bank cannot consume stock");

            Reset();b=Body();m=b.GetComponent<DischargeMeter>();m.RegisterForTest(3);s=State(b,kind,true);s.Age(1);
            b.GetComponent<StoredChargeDriver>().failLaunch=true;w=new NetworkWriter();s.OnSerialize(w);r=new(w.Stream.ToArray());id=r.ReadUInt32();
            bool failed=false;try{s.ServerRequest(Request(kind,id),null,true);}catch(Exception){failed=true;}
            s.OnExit();Check(failed && s.Released && m.Charge==1 && s.activatorSkillSlot.stock==0,"committed launch failure acknowledges cost without stock refund");

            Reset();b=Body();m=b.GetComponent<DischargeMeter>();m.RegisterForTest(3);s=State(b,kind,true);
            float releaseAt=kind==1 ? .82f : kind==0 ? .6f : .5f;
            NetworkServer.active=false;s.Age(releaseAt);s.FixedUpdate();s.Age(releaseAt+.02f);s.FixedUpdate();s.Age(releaseAt+.3f);s.FixedUpdate();
            Check(((StoredChargeTransport.Packet)ClientScene.readyConnection.Last).count==2,"release freezes charge count through settle/ack delay");
            s.Age(3.7f);s.FixedUpdate();s.OnExit();
            Check(s.activatorSkillSlot.stock==0,"unacknowledged remote throw cannot optimistically refund stock");NetworkServer.active=true;
        }
        Reset();var held=Body();held.GetComponent<DischargeMeter>().RegisterForTest(3);
        held.gameObject.AddComponent<EntityStateMachine>().state=new HollowSaint.FoundationKit.Gaze.GazeState();
        var ready=new StoredChargeSkillDef();held.skillLocator.secondary.stock=1;
        Check(!ready.IsReady(held.skillLocator.secondary),"native Gaze state blocks new consumers on clients too");
        Check(StoredChargeHover.held==0,"all tested hover exit paths balance gravity holds");
        Reset();var owner=Body();var victim=Body(5,TeamIndex.Monster);
        NetworkServer.active=false;ChargedStormDamage.Hit(owner,victim.healthComponent,100,false,1,DamageSource.Special);
        Check(victim.healthComponent.received.Count==0,"client cannot directly damage");NetworkServer.active=true;
    }
    static void Flights()
    {
        // Success: Open Circuit raises the actual flight muzzle above the eye;
        // overhead clearance never relocates the Orb through a ceiling.
        Reset();var geometryBody=Body();
        Near(OrbCastGeometry.Point(geometryBody,Vector3.forward,1).z,.9f,"normal orb launches from the visible gather point");
        HollowSaint.FoundationKit.Stormspear.StormspearCharge.crown=true;
        Near(OrbCastGeometry.Point(geometryBody,Vector3.forward,5).y,1.55f,"Circuit orb gathers and launches above the head");
        Physics.SphereObstruction=.4f;
        Near(OrbCastGeometry.Point(geometryBody,Vector3.forward,5).y,.38f,"ceiling obstruction clips overhead muzzle safely");
        Reset();var replaced=Body();var replacementVictim=Body(5,TeamIndex.Monster);BullseyeSearch.candidates.Add(replacementVictim.mainHurtBox);
        var replacedFlight=new ServerHollowedOrb(replaced,5,Vector3.forward);replaced.master.body=Body();
        Check(!replacedFlight.Tick(1) && replacementVictim.healthComponent.received.Count==0,"body replacement cancels old Orb damage");
        Reset();var owner=Body();var a=Body(5,TeamIndex.Monster);var b=Body(10,TeamIndex.Monster);var c=Body(15,TeamIndex.Monster);
        BullseyeSearch.candidates.AddRange(new[]{a.mainHurtBox,b.mainHurtBox,c.mainHurtBox});
        var orb=new ServerHollowedOrb(owner,5,(a.corePosition-KitUtil.EyePosition(owner)).normalized);orb.Begin();
        Near(ChargedStormEffects.lastOrbSpeed,32,"visual segment carries authoritative speed, not a capped arrival estimate");
        Check(orb.Tick(.5f) && a.healthComponent.received.Count==1,"orb first hits A");
        Check(orb.Tick(.5f) && b.healthComponent.received.Count==1,"orb next hits B");
        Physics.Collisions=new[]{new RaycastHit{distance=.1f,collider=a.gameObject.AddComponent<Collider>()}};
        Check(orb.Tick(.5f) && c.healthComponent.received.Count==1 && a.healthComponent.received.Count==1,"orb prioritizes fresh C over revisit A");
        int total=3;while(orb.Tick(.5f)) { total++;Check(total<=8,"finite five-charge flight"); }
        Check(ChargedStormTuning.HitBudget(5)==8 && new[]{a,b,c}.Sum(x=>OrbHits(x))==8,"five charges yield eight total orb hits");
        Check(ChargedStormEffects.bursts.Count==1 && new[]{a,b,c}.All(x=>BurstHits(x)<=1),"spent flight bursts once, at most once per enemy");
        Near(a.healthComponent.received[0].damage,73.8f,"damage scaled once at launch");
        Near(b.healthComponent.received[1].damage,73.8f*.75f,"revisit attenuation per victim");
        Near(b.healthComponent.received[1].procCoefficient,.25f,"revisit proc bounded");
        Reset();owner=Body();a=Body(5,TeamIndex.Monster);b=Body(10,TeamIndex.Monster);
        BullseyeSearch.candidates.AddRange(new[]{a.mainHurtBox,b.mainHurtBox});orb=new(owner,1,(a.corePosition-KitUtil.EyePosition(owner)).normalized);
        Check(orb.Tick(.5f) && orb.Tick(.5f) && orb.Tick(.5f) && !orb.Tick(.5f),"one-charge flight ends at its fourth hit");
        Check(OrbHits(a)==2 && OrbHits(b)==2,"A-B-A-B fallback");
        Check(BurstHits(b)==1 && BurstHits(a)==0 && ChargedStormEffects.bursts.Count==1,"small one-charge burst catches only the final victim");
        Reset();owner=Body();a=Body(5,TeamIndex.Monster);BullseyeSearch.candidates.Add(a.mainHurtBox);
        orb=new(owner,5,Vector3.forward);Check(orb.Tick(.5f) && a.healthComponent.received.Count==1 && ChargedStormEffects.lastOrbTarget==a.healthComponent,"solo boss latches on rather than returning through the player");
        Reset();owner=Body();a=Body(5,TeamIndex.Monster);BullseyeSearch.candidates.Add(a.mainHurtBox);orb=new(owner,1,Vector3.forward);
        Physics.Collisions=new[]{new RaycastHit{distance=.1f,collider=new GameObject().AddComponent<Collider>()}};
        Check(!orb.Tick(.1f) && a.healthComponent.received.Count==0,"terrain stops orb without hitting target beyond it");
        Check(ChargedStormEffects.bursts.Count==1,"terrain stop bursts once where the orb ends");
        Reset();owner=Body();a=Body(5,TeamIndex.Monster);BullseyeSearch.candidates.Add(a.mainHurtBox);orb=new(owner,1,Vector3.forward);
        Physics.WorldOverlap=true;Check(!orb.Tick(.1f) && a.healthComponent.received.Count==0,"overlapping launch wall cannot be bypassed");
        Reset();owner=Body();a=Body(60,TeamIndex.Monster);BullseyeSearch.candidates.Add(a.mainHurtBox);orb=new(owner,1,Vector3.forward);
        a.transform.position=new(0,0,100);Check(orb.Tick(3) && !orb.Tick(.02f) && a.healthComponent.received.Count==0,"moving target cannot extend finite launch distance");
        Reset();owner=Body();a=Body(5,TeamIndex.Monster);b=Body(10,TeamIndex.Monster);BullseyeSearch.candidates.AddRange(new[]{a.mainHurtBox,b.mainHurtBox});
        orb=new(owner,1,Vector3.forward);a.healthComponent.alive=false;
        Check(orb.Tick(.5f) && a.healthComponent.received.Count==0 && b.healthComponent.received.Count==1 && ChargedStormEffects.lastOrbTarget==b.healthComponent,"dead target redirects to living target without phantom hit");
        for(int i=0;i<40 && orb.Tick(.5f);i++){}
        Check(a.healthComponent.received.Count==0 && OrbHits(b)==ChargedStormTuning.HitBudget(1),"redirected orb latches and spends its budget on the survivor");
    }
    static void Clouds()
    {
        Reset();var replaced=Body();var replacementVictim=Body(5,TeamIndex.Monster);
        Physics.AimHit=new(0,0,5);BullseyeSearch.candidates.Add(replacementVictim.mainHurtBox);
        var replacedCloud=ServerThundercloud.Prepare(replaced,5,Vector3.forward);replaced.master.body=Body();
        Check(!replacedCloud.Tick(2) && replacementVictim.healthComponent.received.Count==0,"body replacement cancels old cloud damage");
        Reset();var owner=Body();var a=Body(10,TeamIndex.Monster);var b=Body(20,TeamIndex.Monster);
        Physics.AimHit=new(0,0,15);BullseyeSearch.candidates.AddRange(new[]{b.mainHurtBox,a.mainHurtBox,a.mainHurtBox});
        var cloud=ServerThundercloud.Prepare(owner,1,Vector3.forward);Check(cloud!=null,"broad aimed cloud includes group");cloud.Begin();
        cloud.Tick(ThundercloudSchedule.FirstStrike);Check(a.healthComponent.received.Count==1 && b.healthComponent.received.Count==1,"first pulse strikes the whole group at once");
        Check(ChargedStormEffects.strikes.Count==2,"duplicate hurtboxes are struck once per pulse");
        RunCloud(cloud);int groupPulses=ThundercloudSchedule.Pulses(1);
        Check(a.healthComponent.received.Count==groupPulses && b.healthComponent.received.Count==groupPulses,"one cloud hit per entity per pulse, duplicates excluded");
        Near(a.healthComponent.received[0].damage,10*ChargedStormTuning.CloudCoefficient(1)*.9f,"cloud effective coefficient once per strike");
        Near(a.healthComponent.received[groupPulses-1].damage,10*ChargedStormTuning.CloudCoefficient(1)*.9f,"later pulses keep the full per-strike damage");
        Near(a.healthComponent.received[0].procCoefficient,.5f,"cloud strike proc");
        Check(ChargedStormEffects.strikes.Count==2*groupPulses,"visual strikes correspond to committed damage");
        Reset();owner=Body();a=Body(38,TeamIndex.Monster);Physics.AimHit=new(0,0,15);BullseyeSearch.candidates.Add(a.mainHurtBox);
        cloud=ServerThundercloud.Prepare(owner,1,Vector3.forward);Check(cloud!=null,"lingering cloud forms over an empty area (no refusal)");
        RunCloud(cloud);Check(a.healthComponent.received.Count==0,"one-charge radius excludes an enemy 23m from center");
        RunCloud(ServerThundercloud.Prepare(owner,5,Vector3.forward));
        Check(a.healthComponent.received.Count==ThundercloudSchedule.Pulses(5),"charge growth expands actual eligibility");
        Physics.Obstructed=(from,to)=>true;int unoccluded=a.healthComponent.received.Count;
        RunCloud(ServerThundercloud.Prepare(owner,5,Vector3.forward));
        Check(a.healthComponent.received.Count==unoccluded,"occluded cloud targets excluded");
        Reset();owner=Body();a=Body(10,TeamIndex.Monster);Physics.AimHit=new(0,0,10);BullseyeSearch.candidates.Add(a.mainHurtBox);
        cloud=ServerThundercloud.Prepare(owner,5,Vector3.forward);Stage.instance=new();
        Check(!cloud.Tick(2) && a.healthComponent.received.Count==0,"stage change cancels pending strikes");
        Reset();owner=Body();a=Body(10,TeamIndex.Monster);Physics.AimHit=new(0,0,10);BullseyeSearch.candidates.Add(a.mainHurtBox);
        cloud=ServerThundercloud.Prepare(owner,5,Vector3.forward);a.healthComponent.alive=false;cloud.Tick(2);
        Check(a.healthComponent.received.Count==0,"dead scheduled enemy receives no cloud damage");
        Near(ChargedStormTuning.Radius(0),12,"free cloud radius is three quarters of one charge");Near(ChargedStormTuning.Radius(1),16,"large baseline radius");Near(ChargedStormTuning.Radius(5),30,"full-cloud radius");
        Near(ChargedStormTuning.Diameter(1),.9f,"base orb size");Near(ChargedStormTuning.Diameter(5),1.5f,"full orb size");
        KitTuning.StormChargeMax=2;Check(ChargedStormTuning.DescriptionChargeLimit==2,"descriptions respect smaller configured charge bank");KitTuning.StormChargeMax=5;
        for(int i=0;i<64;i++)Check(ChargedStormTuning.Radius(i)<=40 && ChargedStormTuning.HitBudget(i)<=16 && ThundercloudSchedule.Pulses(i)<=21 &&
            ThundercloudSchedule.CompleteAt(i)<=20 && ChargedStormTuning.BurstRadius(i)<=12,"configured capacities remain bounded");
    }
    static void RunCloud(ServerThundercloud cloud)
    {
        int ticks=0;while(cloud.Tick(.05f)) Check(++ticks<1000,"cloud storm is finite");
    }
    static void Motion()
    {
        // A retreating target must not pull the visual instantly to itself at
        // the original arrival deadline; motion keeps the native speed bound.
        Vector3 point=Vector3.zero, target=Vector3.forward*10;
        for(int i=0;i<10;i++)
        {
            target+=Vector3.forward;
            var next=OrbFlightMotion.Advance(point,target,10,.1f);
            Near(Vector3.Distance(next,point),1,"moving-target visual advances exactly one speed-bounded step");
            point=next;
        }
        Near(point.z,10,"retreating target cannot teleport visual at original arrival time");
        Near(Vector3.Distance(point,target),10,"target remains ahead when matching Orb speed");
        Near(OrbFlightMotion.Travel(100,10,100,.1f),1,"long-range low-speed flight is not accelerated by a four-second cap");
        Near(OrbFlightMotion.Travel(100,80,.2f,1),.2f,"authoritative range budget still caps movement");
        Near(OrbFlightMotion.Advance(Vector3.zero,Vector3.forward*.1f,32,1).z,.1f,"visual cannot overshoot nearby target");
    }
    public static void Main()
    { DischargeMeter.RegisterBuff(); HollowSaint.FoundationKit.OpenCircuit.OpenCircuitBuff.Register(); Ledger();Transport();Runtime();Flights();Clouds();Motion();OrbRefinement();CircuitRefinement();OrbLatch();CloudStorm();PendingPrimary();StormReach();StormFlow();ClosedCircuit();CloudAudit();Console.WriteLine($"PASS {checks} charge accounting, native release and server flight/area assertions"); }
    static void Invoke(object obj,string method) => obj.GetType().GetMethod(method,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(obj,null);
}
static class MeterFixture
{
    internal static void RegisterForTest(this DischargeMeter meter,int count)
    { meter.GetComponent<CharacterBody>().SetBuffCount(DischargeMeter.ChargeBuff.buffIndex,count); }
}
