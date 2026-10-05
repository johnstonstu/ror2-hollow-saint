using System.Reflection;
using HollowSaint.FoundationKit.Gaze;
using HollowSaint.FoundationKit.Stormspear;
using RoR2; using RoR2.Skills; using UnityEngine;

static class Program {
    static int checks;
    sealed class ExternalDef:SkillDef {}
    static void Check(bool pass,string message) {checks++;if(!pass)throw new Exception(message);}
    sealed class Fixture {
        internal CharacterBody body;internal GazeFuelController fuel;internal GazeSkillOverrides controls;
        internal GazeState state=new();internal EntityStateMachine crown;
        internal GenericSkill[] skills;internal SkillDef[] originals;internal SkillDef.BaseSkillInstanceData[] data;
        internal Fixture(bool held=false) {
            var go=new GameObject();body=go.AddComponent<CharacterBody>();fuel=go.AddComponent<GazeFuelController>();
            crown=go.AddComponent<EntityStateMachine>();crown.customName="Crown";crown.state=state;
            controls=GazeSkillOverrides.For(body);body.inputBank.skill1.down=held;
            originals=Enumerable.Range(0,4).Select(_=>new SkillDef{baseMaxStock=3,baseRechargeInterval=10,fullRestockOnAssign=true,icon=new Sprite()}).ToArray();
            originals[0].baseMaxStock=1;originals[0].baseRechargeInterval=0;
            originals[3].beginSkillCooldownOnSkillEnd=true;originals[3].activationState=new(typeof(GazeState));
            skills=originals.Select(d=>new GenericSkill(body,d,crown)).ToArray();
            data=skills.Select(s=>s.skillInstanceData).ToArray();
            for(int i=0;i<4;i++){skills[i].stock=i==0?1:1;skills[i].rechargeStopwatch=i==0?0:2;}
            body.skillLocator.primary=skills[0];body.skillLocator.secondary=skills[1];body.skillLocator.utility=skills[2];body.skillLocator.special=skills[3];
        }
        internal void Begin()=>controls.Begin(state,fuel);
        internal void Tick(float dt) {foreach(var s in skills)s.skillDef.OnFixedUpdate(s,dt);}
        internal void Press(bool down) {body.inputBank.skill1.down=down;controls.ObservePrimary();}
    }
    static void Main() {
        GazeChannelSkillDefs.Register();
        Check(!GazeChannelSkillDefs.Pulse.fullRestockOnAssign&&GazeChannelSkillDefs.Pulse.stockToConsume==0&&GazeChannelSkillDefs.Pulse.mustKeyPress,"pulse definition uses custom stockless execution");
        foreach(bool nativeFirst in new[]{false,true})foreach(string unavailable in new[]{"windup","rate interval","empty bank"}) {
            var f=new Fixture();f.Begin();
            if(unavailable=="windup")f.state.PrimaryPulseReady=false;
            else if(unavailable=="rate interval")f.fuel.PulseRequestReady=false;
            else f.fuel.AvailableEntry=0;
            f.body.inputBank.skill1.down=true;
            if(!nativeFirst)f.controls.ObservePrimary();
            Check(!f.skills[0].ExecuteIfReady()&&f.fuel.Requests==0,unavailable+" press rejected in either polling order");
            if(nativeFirst)f.controls.ObservePrimary();
            f.state.PrimaryPulseReady=true;f.fuel.PulseRequestReady=true;f.fuel.AvailableEntry=5;
            // Model native HandleSkill retrying an unclaimed held mustKeyPress input.
            for(int retry=0;retry<10;retry++) {
                if(!nativeFirst)f.controls.ObservePrimary();
                Check(!f.skills[0].ExecuteIfReady()&&f.fuel.Requests==0,unavailable+" held press never queues across eligibility transition");
                if(nativeFirst)f.controls.ObservePrimary();
            }
            f.Press(false);f.body.inputBank.skill1.down=true;
            if(!nativeFirst)f.controls.ObservePrimary();
            Check(f.skills[0].ExecuteIfReady()&&f.fuel.Requests==1,unavailable+" later release/fresh press executes exactly once");
            if(nativeFirst)f.controls.ObservePrimary();
            Check(!f.skills[0].ExecuteIfReady()&&f.fuel.Requests==1,"fresh eligible press cannot repeat");f.controls.End();
        }
        foreach(int count in new[]{0,1,2,5,6,20}) {
            var f=new Fixture(true);f.fuel.EntryCapacity=Math.Max(2,count);f.fuel.AvailableEntry=count;f.Begin();
            Check(f.skills[0].stock==count&&f.skills[0].maxStock==Math.Max(2,count),"native HUD available entry/max count");
            Check(!f.skills[0].ExecuteIfReady()&&f.fuel.Requests==0,"held entry cannot execute");
            for(int i=1;i<4;i++)Check(!f.skills[i].ExecuteIfReady()&&!f.skills[i].skillDef.IsReady(f.skills[i]),"secondary utility special disabled");
            f.Press(false);f.Press(true);
            Check(f.skills[0].ExecuteIfReady()==(count>0),"fresh mapped Primary obeys available bank");
            Check(!f.skills[0].ExecuteIfReady()&&f.fuel.Requests==(count>0?1:0),"native path emits once per fresh press");
            Check(f.skills[0].stock==count,"unacknowledged request does not decrement HUD stock");
            f.fuel.AvailableEntry=Math.Max(0,count-1);f.Tick(.02f);
            Check(f.skills[0].stock==f.fuel.AvailableEntry,"ACK updates native stock without native spend");
            Check(f.originals.All(d=>d.nativeExecutions==0),"pulse has no baseline skill execution/item hook");
            f.controls.End();
            Check(f.skills[0].maxStock==1,"fuel max stock does not linger after native unset");
        }
        var progress=new Fixture();progress.Begin();progress.Tick(4);
        Check(progress.skills[2].baseStock==1&&progress.skills[2].baseRechargeStopwatch==6,"utility underlying cooldown advances");
        Check(progress.skills[3].baseStock==1&&progress.skills[3].baseRechargeStopwatch==2,"Special end-gated cooldown stays paused");
        progress.controls.End();
        for(int i=0;i<4;i++) {
            Check(progress.skills[i].skillDef==progress.originals[i]&&ReferenceEquals(progress.skills[i].skillInstanceData,progress.data[i]),"native skill and exact instance restored");
            Check(progress.skills[i].stock==1,"fullRestockOnAssign never grants extra stock");
        }
        Check(progress.skills[2].rechargeStopwatch==6&&progress.skills[3].rechargeStopwatch==2,"cooldown progress survives return");
        progress.controls.End();Check(progress.skills.All(s=>s.OverrideCount==0),"double teardown idempotent");
        var external=new Fixture();var prior=new SkillDef{baseMaxStock=3,baseRechargeInterval=2,fullRestockOnAssign=true};var source=new object();
        external.skills[0].SetSkillOverride(source,prior,GenericSkill.SkillOverridePriority.Contextual);
        external.skills[0].stock=2;external.skills[0].rechargeStopwatch=1.1f;var priorData=external.skills[0].skillInstanceData;
        external.Begin();external.Tick(4);external.controls.End();
        Check(external.skills[0].skillDef==prior&&external.skills[0].stock==2&&external.skills[0].rechargeStopwatch==1.1f&&ReferenceEquals(priorData,external.skills[0].skillInstanceData),"preexisting override bank/data retained without bespoke recharge emulation");
        Check(external.skills[0].OverrideCount==1,"only own source removed");
        var custom=new Fixture();var customDef=new ExternalDef{baseMaxStock=3};
        custom.skills[0].SetSkillOverride(source,customDef,GenericSkill.SkillOverridePriority.Contextual);
        custom.skills[0].stock=1;custom.skills[0].rechargeStopwatch=1.4f;var disposedData=custom.skills[0].skillInstanceData;
        custom.Begin();custom.controls.End();
        Check(custom.skills[0].stock==1&&custom.skills[0].rechargeStopwatch==1.4f&&!ReferenceEquals(disposedData,custom.skills[0].skillInstanceData),"unknown custom data follows native fresh assignment while bank preserved");
        var spear=new Fixture();var spearDef=new StormspearSkillDef{baseMaxStock=3,baseRechargeInterval=10};
        spear.skills[1]=new GenericSkill(spear.body,spearDef,spear.crown);spear.body.skillLocator.secondary=spear.skills[1];
        spear.skills[1].stock=1;spear.skills[1].rechargeStopwatch=2;
        var throwing=new StormspearThrowState();spear.skills[1].stateMachine=new EntityStateMachine{state=throwing};spear.Begin();spear.Tick(.2f);
        Check(spear.skills[1].baseRechargeStopwatch==2,"existing Spear pre-release cooldown stays paused");
        Check(spear.skills[1].skillDef==spearDef,"secondary override waits actual throw exit");
        throwing.CooldownReleased=true;spear.skills[1].stateMachine.state=new GazeLockState();Invoke(spear.controls,"FixedUpdate");spear.Tick(.2f);
        Check(Math.Abs(spear.skills[1].baseRechargeStopwatch-2.2f)<.0001f,"existing Spear resumes recharge after exit");spear.controls.End();
        foreach(int originalStock in new[]{0,1,2})foreach(bool failedThrow in new[]{false,true}) {
            var f=new Fixture();var def=new StormspearSkillDef{baseMaxStock=3,baseRechargeInterval=10};
            var machine=new EntityStateMachine{state=failedThrow?(object)new StormspearThrowState():new StormspearChargeState()};
            f.skills[1]=new GenericSkill(f.body,def,machine);f.body.skillLocator.secondary=f.skills[1];
            f.skills[1].stock=originalStock;f.skills[1].rechargeStopwatch=3.25f;var saved=f.skills[1].skillInstanceData;
            f.Begin();Invoke(f.controls,"FixedUpdate");
            Check(f.skills[1].skillDef==def,"queued lock does not switch secondary bank");
            // Native spear OnExit adds one unthrown stock and keeps partial progress.
            f.skills[1].stock++;machine.state=new GazeLockState();Invoke(f.controls,"FixedUpdate");
            Check(f.skills[1].skillDef==GazeChannelSkillDefs.Locked,"lock override installed after actual refund");
            f.controls.End();
            Check(f.skills[1].stock==originalStock+1&&f.skills[1].rechargeStopwatch==3.25f&&ReferenceEquals(saved,f.skills[1].skillInstanceData),"charge/failed throw refund preserved including empty and spare stocks");
        }
        var deferredEnd=new Fixture();deferredEnd.skills[1].stateMachine=new EntityStateMachine{state=new StormspearChargeState()};deferredEnd.Begin();deferredEnd.controls.End();
        deferredEnd.skills[1].stateMachine.state=new GazeLockState();Invoke(deferredEnd.controls,"FixedUpdate");
        Check(deferredEnd.skills[1].skillDef==deferredEnd.originals[1]&&deferredEnd.skills[1].OverrideCount==0,"ended cast never installs deferred secondary");
        var higher=new Fixture();higher.Begin();var other=new SkillDef{baseMaxStock=7};higher.skills[0].SetSkillOverride(source,other,GenericSkill.SkillOverridePriority.Network);
        higher.skills[0].stock=4;higher.skills[0].rechargeStopwatch=3.5f;var otherData=higher.skills[0].skillInstanceData;
        higher.controls.End();Check(higher.skills[0].skillDef==other&&higher.skills[0].stock==4&&higher.skills[0].rechargeStopwatch==3.5f&&ReferenceEquals(otherData,higher.skills[0].skillInstanceData),"new higher override entirely untouched");
        var alreadyHigher=new Fixture();alreadyHigher.skills[0].SetSkillOverride(source,other,GenericSkill.SkillOverridePriority.Network);
        alreadyHigher.skills[0].stock=2;alreadyHigher.skills[0].rechargeStopwatch=1;alreadyHigher.Begin();
        alreadyHigher.skills[0].stock=3;alreadyHigher.skills[0].rechargeStopwatch=2;var runningData=alreadyHigher.skills[0].skillInstanceData;
        alreadyHigher.controls.End();Check(alreadyHigher.skills[0].stock==3&&alreadyHigher.skills[0].rechargeStopwatch==2&&ReferenceEquals(runningData,alreadyHigher.skills[0].skillInstanceData),"preexisting higher bank progress never rolled back");
        foreach(string reason in new[]{"natural end","stun","death","disable","destroy","machine replacement"}) {
            var f=new Fixture();f.Begin();
            if(reason=="death"){f.body.healthComponent.alive=false;Invoke(f.controls,"FixedUpdate");}
            else if(reason=="machine replacement"){f.crown.state=new object();Invoke(f.controls,"FixedUpdate");}
            else if(reason=="disable")Invoke(f.controls,"OnDisable");
            else if(reason=="destroy")Invoke(f.controls,"OnDestroy");
            else f.controls.End(); // GazeState.OnExit delegates for natural/native hurt transitions.
            Check(!f.controls.Active&&f.skills.All(s=>s.OverrideCount==0),reason+" removes all channel overrides");
            Check(f.skills[3].stock==1&&f.skills[3].rechargeStopwatch==2,reason+" restores original Special bank");
        }
        var noAuthority=new Fixture();noAuthority.Begin();noAuthority.body.hasEffectiveAuthority=false;noAuthority.Press(true);
        Check(!noAuthority.skills[0].ExecuteIfReady()&&noAuthority.fuel.Requests==0,"observers never request pulses");
        ExitChecks();
        Console.WriteLine("PASS "+checks+" production controls assertions using native adapter simulation");
    }
    static void ExitChecks()
    {
        var edges=new GazeExitEdges();edges.Begin(true,true);
        for(int i=0;i<100;i++)Check(edges.Observe(true,true)==GazeExitAction.None,"initial held mapped actions never self-cancel");
        Check(edges.Observe(false,false)==GazeExitAction.None,"release has no action");
        Check(edges.Observe(true,false)==GazeExitAction.Special,"fresh mapped Special exits regardless of extended time");
        Check(edges.Observe(true,false)==GazeExitAction.None,"held Special cannot repeat");
        edges.Observe(false,false);Check(edges.Observe(true,true)==GazeExitAction.Utility,"simultaneous mapped actions choose Utility once");
        foreach(string reason in new[]{"ready","empty","replaced","death","authority lost","disable","expiry"})
        {
            var f=new Fixture();f.Begin();f.Tick(1);f.controls.End();f.crown.state=new object();Time.unscaledTime=0;
            int stock=f.skills[2].stock;float timer=f.skills[2].rechargeStopwatch;
            if(reason=="empty")f.skills[2].stock=stock=0;
            GazeUtilityExit.Queue(f.body);var driver=f.body.GetComponent<GazeUtilityExit>();
            if(reason=="replaced")f.skills[2].skillDef=new ExternalDef();
            if(reason=="death")f.body.healthComponent.alive=false;
            if(reason=="authority lost")f.body.hasEffectiveAuthority=false;
            if(reason=="disable")Invoke(driver,"OnDisable");
            if(reason=="expiry")Time.unscaledTime=1;
            Invoke(driver,"FixedUpdate");Invoke(driver,"FixedUpdate");
            Check(f.originals[2].nativeExecutions==(reason=="ready"?1:0),reason+" equipped utility executes at most once through native definition");
            Check(f.skills[2].stock==stock-(reason=="ready"?1:0)&&f.skills[2].rechargeStopwatch==timer,reason+" native stock/cooldown preserved");
        }
        var blocked=new Fixture();blocked.Begin();blocked.controls.End();Time.unscaledTime=0;
        GazeUtilityExit.Queue(blocked.body);var queued=blocked.body.GetComponent<GazeUtilityExit>();
        Invoke(queued,"FixedUpdate");Check(blocked.originals[2].nativeExecutions==0,"queued utility waits for Crown teardown");
        blocked.crown.state=new GazeLockState();Invoke(queued,"FixedUpdate");Check(blocked.originals[2].nativeExecutions==0,"queued utility waits for actual machine unlock");
        blocked.crown.state=new object();Invoke(queued,"FixedUpdate");Check(blocked.originals[2].nativeExecutions==1,"unlocked equipped utility activates once");
    }
    static void Invoke(object target,string method)=>target.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,null);
}
