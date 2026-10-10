using System;
using RoR2;
using UnityEngine;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.HollowedOrb;

static partial class Program
{
    static void OrbRefinement()
    {
        // Success: optional fuel retains all bank/stock/authentication guards;
        // aim assistance follows reticle alignment and respects terrain.
        var lease = new StoredChargeCastLedger(); int bank = 0;
        Check(lease.Begin(1,0,true), "free Orb acquires an exclusive empty-bank lease");
        Check(!lease.Begin(2,5,true), "free gather cannot overlap another claim");
        Check(lease.Spend(1,0,.24f,ref bank,out int spent,true,.5f) && spent==0 && bank==0, "empty lease commits zero once");
        Check(!lease.Spend(1,0,.24f,ref bank,out _,true,.5f), "duplicate zero commit rejected");
        Near(StoredChargeCastLedger.Gathered(.499f,5,.5f),0,"Orb short-cast grace threshold");
        Near(StoredChargeCastLedger.Gathered(.5f,5,.5f),1,"first optional charge at half a second");
        Near(StoredChargeCastLedger.Gathered(.8f,5,.5f),2,"subsequent optional charge timing");
        Near(StoredChargeCastLedger.Gathered(.12f,5),1,"cloud intake remains unchanged");
        Near(OrbCastFlow.RequestAt(.02f),.24f,"tap receives minimum windup and short settle");
        Near(OrbCastFlow.RequestAt(.9f),.96f,"long hold settles without extra windup");

        foreach(int entry in new[]{0,1,5})
        {
            Reset(); var body=Body(); var meter=body.GetComponent<DischargeMeter>(); meter.RegisterForTest(entry);
            var slot=body.skillLocator.secondary; slot.stock=1;
            var orbDef=new StoredChargeSkillDef{allowsUnchargedCast=true};slot.skillDef=orbDef;
            Check(orbDef.IsReady(slot) && orbDef.CanExecute(slot),"Orb admits zero/partial/full bank: "+entry);
            if(entry==0) Check(!new StoredChargeSkillDef().CanExecute(slot),"a def without uncharged admission still needs charge");
            slot.stock=0;Check(!orbDef.CanExecute(slot),"free Orb still needs native skill stock");slot.stock=1;
            slot.stock--; // Native activation consumes stock before OnEnter.
            var state=State(body,1,false);state.Age(.3f);
            var driver=body.GetComponent<StoredChargeDriver>();var request=Request(1,8,0);
            state.ServerRequest(request,new UnityEngine.Networking.NetworkConnection(),false);
            Check(!state.Released && driver.launches==0,"foreign zero-cost release rejected");
            state.ServerRequest(request,body.master.playerCharacterMasterController.networkUser.connectionToClient,false);
            Check(state.Released && driver.launches==1 && driver.spent==0 && meter.Charge==entry,"free release preserves exact bank: "+entry);
            state.ServerRequest(request,body.master.playerCharacterMasterController.networkUser.connectionToClient,false);
            Check(driver.launches==1,"free release retry cannot duplicate projectile");state.OnExit();
            Check(!meter.StoredCastGathering && state.activatorSkillSlot.stock==0,"free cast clears lease and retains consumed stock");
        }
        Reset();var b=Body();var m=b.GetComponent<DischargeMeter>();m.RegisterForTest(0);
        var s=State(b,1,false);m.AddCharge();s.Age(1f);
        s.ServerRequest(Request(1,8,3),b.master.playerCharacterMasterController.networkUser.connectionToClient,false);
        Check(s.Released && m.Charge==1 && b.GetComponent<StoredChargeDriver>().spent==0,"newly earned fuel cannot expand empty entry allowance");s.OnExit();
        Reset();b=Body();m=b.GetComponent<DischargeMeter>();m.RegisterForTest(5);s=State(b,1,false);s.Age(.8f);
        s.ServerRequest(Request(1),b.master.playerCharacterMasterController.networkUser.connectionToClient,false);
        Check(s.Released && m.Charge==3,"deliberate two-charge hold spends exactly two");s.OnExit();

        Reset();b=Body();m=b.GetComponent<DischargeMeter>();m.RegisterForTest(0);s=State(b,1,true);
        s.Age(.02f);s.FixedUpdate();s.Age(.23f);s.FixedUpdate();
        Check(!s.Released && b.GetComponent<StoredChargeDriver>().launches==0,"native tap cannot launch before minimum windup");
        s.Age(.26f);s.FixedUpdate();Check(s.Released && b.GetComponent<StoredChargeDriver>().launches==1,"native tap launches without requiring a charge");s.OnExit();

        Reset();b=Body();b.GetComponent<DischargeMeter>().RegisterForTest(5);
        b.skillLocator.secondary.skillDef=new StoredChargeSkillDef{allowsUnchargedCast=true};b.skillLocator.secondary.stock=1;b.inputBank.skill2.down=true;
        HollowSaint.FoundationKit.Stormspear.StormspearCharge.crown=true;
        Check(!StoredChargeState.BlocksPrimary(b),"pending overhead Orb leaves Primary available");
        s=State(b,1,true);s.Update();
        Check(!StoredChargeState.BlocksPrimary(b) && !b.inputBank.skill1.hasPressBeenClaimed,"overhead gather leaves hands/native Primary free");
        HollowSaint.FoundationKit.Stormspear.StormspearCharge.crown=false;s.Update();
        Check(StoredChargeState.BlocksPrimary(b) && b.inputBank.skill1.hasPressBeenClaimed,"Circuit expiry restores normal two-hand ownership");s.OnExit();

        Reset();var owner=Body();var near=Body(5,TeamIndex.Monster);near.transform.position=new(.6f,1,5);
        var centered=Body(20,TeamIndex.Monster);centered.transform.position=new(0,1,20);
        BullseyeSearch.candidates.AddRange(new[]{near.mainHurtBox,centered.mainHurtBox});
        Check(OrbAimTargeting.Select(owner,owner.corePosition,Vector3.forward,70)==centered.healthComponent,"reticle alignment wins over nearest off-center enemy");
        centered.transform.position=new(3.8f,1,20);BullseyeSearch.candidates.Clear();BullseyeSearch.candidates.Add(centered.mainHurtBox);
        Check(OrbAimTargeting.Select(owner,owner.corePosition,Vector3.forward,70)==centered.healthComponent,"about eleven degrees receives aim assistance");
        centered.transform.position=new(4.7f,1,20);
        Check(OrbAimTargeting.Select(owner,owner.corePosition,Vector3.forward,70)==null,"outside twelve-degree cone does not steal aim");
        centered.transform.position=new(0,1,20);Physics.Obstructed=(from,to)=>true;
        Check(OrbAimTargeting.Select(owner,owner.corePosition,Vector3.forward,70)==null,"muzzle obstruction excludes homing target");
        Reset();owner=Body();near=Body(5,TeamIndex.Monster);centered=Body(10,TeamIndex.Monster);
        BullseyeSearch.candidates.AddRange(new[]{near.mainHurtBox,centered.mainHurtBox});
        var flight=new ServerHollowedOrb(owner,0,(near.corePosition-KitUtil.EyePosition(owner)).normalized);
        Check(flight.Tick(.5f) && flight.Tick(.5f) && !flight.Tick(.5f),"free Orb has three finite native impacts");
        Check(OrbHits(near)==2 && OrbHits(centered)==1,"free Orb bounces to the fresh victim, then revisits");
        Near(near.healthComponent.received[0].damage,37.8f,"free Orb base damage scaled once");
        Near(near.healthComponent.received[1].damage,37.8f*.75f,"free Orb revisit keeps three quarters");
        Near(ChargedStormTuning.Diameter(0),.9f,"free Orb is bigger (1.3.1)");
        Near(ChargedStormTuning.OrbCoefficient(1)*.9f,4.5f,"one-charge effective coefficient");
        Check(ChargedStormTuning.HitBudget(0)==3 && ChargedStormTuning.HitBudget(1)==4 && ChargedStormTuning.HitBudget(5)==8,"budgets: free 3, plus one per extra charge");
    }
}
