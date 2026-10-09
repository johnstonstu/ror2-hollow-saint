using System;
using System.Collections.Generic;
using System.Linq;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.Thundercloud;
using RoR2;
using UnityEngine;

// Lingering Thundercloud: castable free (no charges) with an empty bank, pulses on a
// fixed cadence for a charge-scaled window, and every pulse strikes each visible enemy
// beneath it once. Stage change, owner death or body replacement stop later pulses.
static partial class Program
{
    static void CloudStorm()
    {
        // Free cast admission: an empty bank casts, a quick tap spends nothing.
        Check(ChargedStormTuning.CloudFreeCast && new ThundercloudState().AllowsEmpty,"Thundercloud allows an empty-bank free cast by default");
        Near(StoredChargeCastLedger.Gathered(ThundercloudSchedule.FreeCastGrace-.001f,5,ThundercloudSchedule.FreeCastGrace),0,"free-cast grace holds the first charge");
        Near(StoredChargeCastLedger.Gathered(ThundercloudSchedule.FreeCastGrace,5,ThundercloudSchedule.FreeCastGrace),1,"first cloud charge gathers at the grace");
        foreach(int entry in new[]{0,3})
        {
            Reset();var body=Body();var meter=body.GetComponent<DischargeMeter>();meter.RegisterForTest(entry);
            var driver=body.GetComponent<StoredChargeDriver>();var conn=body.master.playerCharacterMasterController.networkUser.connectionToClient;
            var state=State(body,0,false);state.Age(ThundercloudSchedule.FreeCastGrace-.01f);
            state.ServerRequest(Request(0,8,2),new UnityEngine.Networking.NetworkConnection(),false);
            Check(!state.Released && driver.launches==0,"foreign free storm release rejected: "+entry);
            state.ServerRequest(Request(0,8,2),conn,false);
            Check(state.Released && driver.launches==1 && driver.spent==0 && meter.Charge==entry,"free Thundercloud admitted and spends nothing: "+entry);
            state.ServerRequest(Request(0,8,2),conn,false);
            Check(driver.launches==1 && meter.Charge==entry,"free storm retry cannot duplicate");
            state.OnExit();Check(!meter.StoredCastGathering,"free storm clears its lease: "+entry);
        }
        {
            Reset();var body=Body();var meter=body.GetComponent<DischargeMeter>();meter.RegisterForTest(5);
            var state=State(body,0,false);state.Age(ThundercloudSchedule.FreeCastGrace);
            state.ServerRequest(Request(0,8,2),body.master.playerCharacterMasterController.networkUser.connectionToClient,false);
            Check(state.Released && body.GetComponent<StoredChargeDriver>().spent==1 && meter.Charge==4,"holding past the grace gathers and spends one charge");state.OnExit();
        }
        ChargedStormTuning.CloudFreeCast=false;
        try
        {
            Reset();var body=Body();var meter=body.GetComponent<DischargeMeter>();meter.RegisterForTest(0);
            var state=State(body,0,false);state.Age(1);
            state.ServerRequest(Request(0,8,0),body.master.playerCharacterMasterController.networkUser.connectionToClient,false);
            Check(!state.AllowsEmpty && !state.Released && body.GetComponent<StoredChargeDriver>().launches==0,"disabled free cast restores the charge requirement");state.OnExit();
            Reset();body=Body();meter=body.GetComponent<DischargeMeter>();meter.RegisterForTest(5);
            state=State(body,0,false);state.Age(StoredChargeCastLedger.FirstChargeAt);
            state.ServerRequest(Request(0,8,1),body.master.playerCharacterMasterController.networkUser.connectionToClient,false);
            Check(state.Released && meter.Charge==4,"disabled free cast restores the quick first charge");state.OnExit();
        }
        finally { ChargedStormTuning.CloudFreeCast=true; }

        // Pulse count and timing: FirstStrike, then every Interval through the charge-scaled window.
        Near(ChargedStormTuning.CloudDuration(0),3,"free storm window");Near(ChargedStormTuning.CloudDuration(1),4,"one-charge window");
        Near(ChargedStormTuning.CloudDuration(5),8,"five-charge window");Near(ThundercloudSchedule.Interval,.75f,"pulse interval");
        Check(ThundercloudSchedule.Pulses(0)==5 && ThundercloudSchedule.Pulses(1)==6 && ThundercloudSchedule.Pulses(5)==11,"pulse counts for 0/1/5 charges");
        foreach(int charges in new[]{0,1,5})
        {
            Reset();var owner=Body();var a=Body(10,TeamIndex.Monster);
            Physics.AimHit=new(0,0,10);BullseyeSearch.candidates.Add(a.mainHurtBox);
            var cloud=ServerThundercloud.Prepare(owner,charges,Vector3.forward);cloud.Begin();
            Near(cloud.Duration,ThundercloudSchedule.CompleteAt(charges),"cloud duration matches its schedule "+charges);
            var times=new List<float>();float age=0;bool live=true;
            for(int i=0;i<3000 && live;i++)
            {
                live=cloud.Tick(.01f);age+=.01f;
                if(a.healthComponent.received.Count>times.Count) times.Add(age);
            }
            int pulses=ThundercloudSchedule.Pulses(charges);
            Check(!live && times.Count==pulses && a.healthComponent.received.Count==pulses,"one strike per pulse for the whole window: "+charges);
            for(int i=0;i<pulses;i++)
                Check(times[i]>=ThundercloudSchedule.PulseAt(i)-.0001f && times[i]<ThundercloudSchedule.PulseAt(i)+.0101f,"pulse "+i+" lands on schedule: "+charges);
            Check(Math.Abs(age-ThundercloudSchedule.CompleteAt(charges))<.0201f,"storm ends at its completion time: "+charges);
        }

        // Each pulse hits each enemy in radius once; a late arrival is struck by later pulses.
        {
            Reset();var owner=Body();var a=Body(14,TeamIndex.Monster);var b=Body(16,TeamIndex.Monster);var late=Body(40,TeamIndex.Monster);
            Physics.AimHit=new(0,0,15);BullseyeSearch.candidates.AddRange(new[]{a.mainHurtBox,b.mainHurtBox,b.mainHurtBox,late.mainHurtBox});
            var cloud=ServerThundercloud.Prepare(owner,1,Vector3.forward);cloud.Begin();
            cloud.Tick(ThundercloudSchedule.FirstStrike);
            Check(a.healthComponent.received.Count==1 && b.healthComponent.received.Count==1 && late.healthComponent.received.Count==0,"first pulse strikes each enemy in radius once");
            Check(ChargedStormEffects.strikeStrokes.Count==2 && ChargedStormEffects.strikeStrokes.All(s=>s==ThundercloudSchedule.ReturnStrokeCount),"first pulse keeps the full return strokes");
            cloud.Tick(ThundercloudSchedule.Interval+.001f);
            Check(a.healthComponent.received.Count==2 && b.healthComponent.received.Count==2 && late.healthComponent.received.Count==0,"second pulse strikes each enemy once more");
            Check(ChargedStormEffects.strikeStrokes.Count==4 && ChargedStormEffects.strikeStrokes.Skip(2).All(s=>s==1),"repeating pulses use a single stroke");
            late.transform.position=new(0,0,20);
            RunCloud(cloud);int pulses=ThundercloudSchedule.Pulses(1);
            Check(a.healthComponent.received.Count==pulses && b.healthComponent.received.Count==pulses,"every pulse strikes each enemy in radius once");
            Check(late.healthComponent.received.Count==pulses-2,"enemy entering the storm is struck by every later pulse");
            Check(ChargedStormEffects.strikes.Count==3*pulses-2,"visual strikes match committed damage");
        }

        // Stage change, owner death and body replacement stop later pulses; a dead enemy takes no more.
        foreach(string stop in new[]{"stage","owner","replaced","victim"})
        {
            Reset();var owner=Body();var a=Body(10,TeamIndex.Monster);
            Physics.AimHit=new(0,0,10);BullseyeSearch.candidates.Add(a.mainHurtBox);
            var cloud=ServerThundercloud.Prepare(owner,5,Vector3.forward);cloud.Begin();
            cloud.Tick(ThundercloudSchedule.FirstStrike);Check(a.healthComponent.received.Count==1,"storm struck before interruption: "+stop);
            if(stop=="stage") Stage.instance=new();
            if(stop=="owner") owner.healthComponent.alive=false;
            if(stop=="replaced") owner.master.body=Body();
            if(stop=="victim") a.healthComponent.alive=false;
            bool live=cloud.Tick(1);
            Check(live==(stop=="victim") && a.healthComponent.received.Count==1,"interruption stops later pulses: "+stop);
            if(live) { RunCloud(cloud);Check(a.healthComponent.received.Count==1,"dead enemy receives no further pulses"); }
        }

        // Per-strike damage at body damage 15: effective (.9 + .18 x charges) x .9.
        Near(ChargedStormTuning.CloudCoefficient(0),.9f,"free cloud raw coefficient");Near(ChargedStormTuning.CloudCoefficient(5),1.8f,"five-charge raw coefficient");
        foreach(var (charges,expected) in new[]{(0,12.15f),(5,24.3f)})
        {
            Reset();var owner=Body();owner.damage=15;var a=Body(10,TeamIndex.Monster);
            Physics.AimHit=new(0,0,10);BullseyeSearch.candidates.Add(a.mainHurtBox);
            var cloud=ServerThundercloud.Prepare(owner,charges,Vector3.forward);cloud.Begin();RunCloud(cloud);
            Check(a.healthComponent.received.Count==ThundercloudSchedule.Pulses(charges),"damage storm completes: "+charges);
            Check(a.healthComponent.received.All(d=>Math.Abs(d.damage-expected)<.001f && Math.Abs(d.procCoefficient-.4f)<.001f),"per-strike damage at body damage 15: "+charges);
        }
    }
}
