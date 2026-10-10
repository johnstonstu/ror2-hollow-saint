using System;
using System.Linq;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.HollowedOrb;
using HollowSaint.FoundationKit.Thundercloud;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

static partial class Program
{
    static void StormReach()
    {
        // Success: wider actual server selection only for empowered casts,
        // unchanged finite hits/attenuation, and a lingering per-pulse cloud.
        Near(ChargedStormTuning.BounceRange(0),18,"free bounce reach unchanged");
        Near(ChargedStormTuning.BounceRange(1),18,"one-charge bounce reach unchanged");
        Near(ChargedStormTuning.BounceRange(3),27,"three-charge bounce reach");
        Near(ChargedStormTuning.BounceRange(5),36,"five-charge reach doubles");
        float oldBase=ChargedStormTuning.OrbBounceRange, oldGrowth=ChargedStormTuning.OrbBounceRangePerCharge;
        try
        {
            ChargedStormTuning.OrbBounceRange=10;Near(ChargedStormTuning.BounceRange(5),28,"custom base participates");
            ChargedStormTuning.OrbBounceRangePerCharge=0;Near(ChargedStormTuning.BounceRange(5),10,"zero growth restores constant reach");
            ChargedStormTuning.OrbBounceRangePerCharge=10;Near(ChargedStormTuning.BounceRange(20),60,"reach has absolute cap");
            ChargedStormTuning.OrbBounceRangePerCharge=float.NaN;Near(ChargedStormTuning.BounceRange(5),10,"invalid growth cannot poison targeting");
        }
        finally { ChargedStormTuning.OrbBounceRange=oldBase;ChargedStormTuning.OrbBounceRangePerCharge=oldGrowth; }
        foreach(int charge in new[]{0,1,3,5})
        {
            Reset();var owner=Body();var a=Body(20,TeamIndex.Monster);var b=Body(45,TeamIndex.Monster);
            BullseyeSearch.candidates.AddRange(new[]{a.mainHurtBox,b.mainHurtBox});
            var orb=new ServerHollowedOrb(owner,charge,(a.corePosition-KitUtil.EyePosition(owner)).normalized);
            bool live=orb.Tick(.7f);
            Check(a.healthComponent.received.Count==1,"first aim-selected enemy receives one hit");
            if(charge<3)
                Check(live && ChargedStormEffects.lastOrbTarget==a.healthComponent && b.healthComponent.received.Count==0,"short casts cannot bridge a 25m gap and latch instead");
            else
                Check(live && ChargedStormEffects.lastOrbTarget==b.healthComponent,"empowered server selects fresh distant enemy");
            for(int i=0;i<15 && live;i++)live=orb.Tick(.8f);
            Check(!live && OrbHits(a)+OrbHits(b)==ChargedStormTuning.HitBudget(charge),"wider reach cannot add enemy hits");
            if(charge<3) Check(b.healthComponent.received.Count==0,"latched short cast never reaches the distant enemy");
            else Near(OrbDamage(b)[1].damage/OrbDamage(b)[0].damage,.75f,"long repeat attenuation preserved");
        }
        Reset();var blockedOwner=Body();var first=Body(20,TeamIndex.Monster);var beyond=Body(45,TeamIndex.Monster);
        BullseyeSearch.candidates.AddRange(new[]{first.mainHurtBox,beyond.mainHurtBox});
        Physics.Obstructed=(from,to)=>from.z>10 && to.z>30;
        var blocked=new ServerHollowedOrb(blockedOwner,5,(first.corePosition-KitUtil.EyePosition(blockedOwner)).normalized);
        Check(blocked.Tick(.7f) && ChargedStormEffects.lastOrbTarget==first.healthComponent && beyond.healthComponent.received.Count==0,"empowered reach respects a wall and latches on the near enemy");
        for(int i=0;i<15 && blocked.Tick(.8f);i++){}
        Check(beyond.healthComponent.received.Count==0 && OrbHits(first)==ChargedStormTuning.HitBudget(5),"walled enemy never struck by bounce, latch or burst");
        Reset();var edgeOwner=Body();first=Body(20,TeamIndex.Monster);beyond=Body(56.01f,TeamIndex.Monster);
        BullseyeSearch.candidates.AddRange(new[]{first.mainHurtBox,beyond.mainHurtBox});
        var edge=new ServerHollowedOrb(edgeOwner,5,(first.corePosition-KitUtil.EyePosition(edgeOwner)).normalized);
        Check(edge.Tick(.7f) && ChargedStormEffects.lastOrbTarget==first.healthComponent,"full charge cannot exceed 36m bounce boundary");
        for(int i=0;i<15 && edge.Tick(.8f);i++){}
        Check(beyond.healthComponent.received.Count==0,"36m boundary holds through the latch and burst");

        Near(ThundercloudSchedule.CompleteAt(0),5.442f,"free storm ends after its sixth pulse, last bolt and fade");
        Near(ThundercloudSchedule.CompleteAt(1),6.192f,"one-charge storm ends after its seventh pulse");
        Near(ThundercloudSchedule.CompleteAt(5),10.692f,"five-charge storm ends after its thirteenth pulse");
        foreach(int n in new[]{0,1,2,5,20,64})
        {
            int pulses=ThundercloudSchedule.Pulses(n);float last=ThundercloudSchedule.PulseAt(pulses-1);
            Check(pulses>=1 && pulses<=21,"pulse count bounded");
            Near(ThundercloudSchedule.PulseAt(0),ThundercloudSchedule.FirstStrike,"first pulse at FirstStrike");
            for(int i=1;i<pulses;i++) Near(ThundercloudSchedule.PulseAt(i)-ThundercloudSchedule.PulseAt(i-1),ThundercloudSchedule.Interval,"pulses are evenly spaced");
            Check(last<=ThundercloudSchedule.FirstStrike+ChargedStormTuning.CloudDuration(n)+.001f,"pulses stay inside the storm window");
            Check(last+ThundercloudSchedule.BoltLifetime*1.4f<=ThundercloudSchedule.CompleteAt(n)-ThundercloudSchedule.Fade+.001f,"last pulse's bolt finishes before fade");
            Check(ThundercloudSchedule.FirstStrike+ThundercloudSchedule.StrokeAt(ThundercloudSchedule.ReturnStrokeCount-1)+ThundercloudSchedule.BoltLifetime*1.4f<=
                ThundercloudSchedule.CompleteAt(n)-ThundercloudSchedule.Fade+.001f,"first pulse's return strokes finish before fade");
        }
        Reset();var stormOwner=Body();var near=Body(10,TeamIndex.Monster);var far=Body(20,TeamIndex.Monster);
        Physics.AimHit=new(0,0,15);BullseyeSearch.candidates.AddRange(new[]{near.mainHurtBox,far.mainHurtBox});
        var cloud=ServerThundercloud.Prepare(stormOwner,5,Vector3.forward);cloud.Begin();
        cloud.Tick(ThundercloudSchedule.FirstStrike-.01f);Check(near.healthComponent.received.Count==0,"no damage during formation");
        cloud.Tick(.02f);Check(near.healthComponent.received.Count==1 && far.healthComponent.received.Count==1,"first pulse strikes every enemy beneath the storm");
        cloud.Tick(ThundercloudSchedule.Interval-.03f);Check(near.healthComponent.received.Count==1 && far.healthComponent.received.Count==1,"next pulse waits for the interval");
        cloud.Tick(.04f);Check(near.healthComponent.received.Count==2 && far.healthComponent.received.Count==2,"second pulse strikes both again");
        int stormPulses=ThundercloudSchedule.Pulses(5);
        Check(!cloud.Tick(20) && near.healthComponent.received.Count==stormPulses && far.healthComponent.received.Count==stormPulses,"storm delivers every pulse and ends");
        cloud.Tick(2);Check(near.healthComponent.received.Count==stormPulses && ChargedStormEffects.strikes.Count==2*stormPulses,"fade tail adds neither damage nor network strikes");
        Vector3 sky=new(0,10,15), center=new(0,0,15);
        Check(ChargedStormTargeting.CloudVisible(sky,center,new(0,0,31),16),"return flashes include exact area boundary");
        Check(!ChargedStormTargeting.CloudVisible(sky,center,new(0,0,31.01f),16),"return flashes stop when victim leaves area");
        Check(!ChargedStormTargeting.CloudVisible(sky,center,new(0,17,15),16),"column stops a little above the cloud");
        Check(ChargedStormTargeting.CloudVisible(sky,center,new(0,15,22),16),"flyers up near the cloud are inside the column");
        Check(ChargedStormTargeting.CloudVisible(sky,center,new(0,-7,15),16) && !ChargedStormTargeting.CloudVisible(sky,center,new(0,-9,15),16),"column reaches 8 m below the aim point");
        Check(ChargedStormTargeting.ColumnSearchRange(sky,center,16)>=Mathf.Sqrt(16*16+16*16)-.01f,"search covers the column corners");
        Physics.Obstructed=(from,to)=>true;
        Check(!ChargedStormTargeting.CloudVisible(sky,center,new(0,0,15),16),"later flashes stop behind cover");
        Reset();var caster=Body();caster.GetComponent<DischargeMeter>().RegisterForTest(5);
        var state=State(caster,0,true);var writer=new NetworkWriter();state.OnSerialize(writer);
        uint token=new NetworkReader(writer.Stream.ToArray()).ReadUInt32();
        state.ReceiveReply(new StoredChargeTransport.Packet { cast=token,kind=0,count=5,duration=ThundercloudSchedule.CompleteAt(5) });
        state.Age(10.6f);state.FixedUpdate();
        Check(!state.outer.ended && !StoredChargeState.BlocksPrimary(caster),"cloud recovery covers the full storm without blocking Primary");
        state.Age(10.8f);state.FixedUpdate();Check(state.outer.ended,"cloud recovery ends after final fade");state.OnExit();
        // 1.3.1: pressing Special again ends the storm early with a partial cooldown refund.
        {
            Reset();var dc=Body();dc.GetComponent<DischargeMeter>().RegisterForTest(5);
            var ds=State(dc,0,true);var dw=new NetworkWriter();ds.OnSerialize(dw);
            uint dt=new NetworkReader(dw.Stream.ToArray()).ReadUInt32();
            dc.inputBank.skill4.down=true; // still held from the cast
            ds.ReceiveReply(new StoredChargeTransport.Packet { cast=dt,kind=0,count=3,duration=8f });
            var slot=dc.skillLocator.special;slot.stock=0;slot.maxStock=1;slot.rechargeStopwatch=0;slot.finalRechargeInterval=10;
            ds.Age(2f);ds.FixedUpdate();Check(!ds.outer.ended,"storm still running before the dismiss");
            ds.Update();Check(!dc.inputBank.skill4.hasPressBeenClaimed && !ds.outer.ended,"a button still held from the cast does not dismiss");
            dc.inputBank.skill4.down=false;ds.Update();dc.inputBank.skill4.down=true;ds.Update();
            Check(dc.inputBank.skill4.hasPressBeenClaimed,"dismiss press is claimed");
            ds.FixedUpdate();Check(dc.GetComponent<StoredChargeDriver>().dismissals>=1,"server dismisses the storm");
            ds.Age(2.25f);ds.FixedUpdate();Check(ds.outer.ended,"storm cast ends right after the dismiss");
            ds.OnExit();Near(slot.rechargeStopwatch,3.75f,"refund: half the 10 s cooldown x 6 of 8 s unused");
        }
        {
            Reset();var co=Body();var cv=Body(10,TeamIndex.Monster);Physics.AimHit=new(0,0,10);BullseyeSearch.candidates.Add(cv.mainHurtBox);
            ChargedStormEffects.cloudDismissals=0;
            var dcloud=ServerThundercloud.Prepare(co,3,Vector3.forward);dcloud.Begin();
            dcloud.Tick(ThundercloudSchedule.FirstStrike+.01f);int struck=cv.healthComponent.received.Count;
            Check(struck==1,"first strike lands before dismissal");
            dcloud.Dismiss();dcloud.Dismiss();
            Check(!dcloud.Tick(5f) && cv.healthComponent.received.Count==struck,"dismissed storm stops striking");
            Check(ChargedStormEffects.cloudDismissals==1,"one dismiss effect for every client");
        }
    }
}
