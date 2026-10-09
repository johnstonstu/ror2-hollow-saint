using System;
using System.Linq;
using RoR2;
using UnityEngine;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.HollowedOrb;
using HollowSaint.FoundationKit.Storm;
using HollowSaint.FoundationKit.Thundercloud;

// Storm flow: charged hits prime Static below full (follow-up hits finish the
// Electrocute), cloud strikes Shock, and the gather cadence is 0.25 s.
// Adapters record priming calls; they do not simulate StormServer decay.
static partial class Program
{
    static void StormFlow()
    {
        Near(StaticPrimePolicy.Apply(0f,.35f,.95f),.35f,"priming adds Static");
        Near(StaticPrimePolicy.Apply(.8f,.35f,.95f),.95f,"priming stops below full Static");
        Near(StaticPrimePolicy.Apply(.97f,.35f,.95f),.97f,"priming never lowers existing Static");
        Near(StaticPrimePolicy.Apply(.2f,.35f,1.5f),.55f,"configured cap is bounded");
        Near(StaticPrimePolicy.Apply(.9f,.35f,2f),.95f,"cap above full is clamped below full");
        Check(StaticPrimePolicy.Apply(0f,1f,1f)<1f,"priming alone can never Electrocute");
        Near(StaticPrimePolicy.Amount(.35f,.65f),.2275f,"revisit priming follows damage falloff");
        Near(StaticPrimePolicy.Amount(float.NaN,1f),0f,"invalid priming is ignored");
        Near(StaticPrimePolicy.Amount(.35f,-1f),0f,"negative scale primes nothing");

        Near(StoredChargeCastLedger.Gathered(.369f,5),1,"special cadence second-charge threshold");
        Near(StoredChargeCastLedger.Gathered(.37f,5),2,"special second charge at 0.37 s");
        Near(StoredChargeCastLedger.Gathered(1.12f,5),5,"five special charges by 1.12 s");
        Near(StoredChargeCastLedger.Gathered(1.5f,5,OrbCastFlow.FirstChargeAt),5,"five Orb charges by 1.5 s");
        Near(StoredChargeCastLedger.Gathered(.749f,5,OrbCastFlow.FirstChargeAt),1,"Orb second-charge threshold");

        // Cloud: every pulse primes and Shocks each living victim beneath it once; dead victims get neither.
        Reset();StormServer.primes.Clear();StormServer.shocks.Clear();
        var owner=Body();var a=Body(10,TeamIndex.Monster);var b=Body(20,TeamIndex.Monster);
        Physics.AimHit=new(0,0,15);BullseyeSearch.candidates.AddRange(new[]{a.mainHurtBox,b.mainHurtBox});
        var cloud=ServerThundercloud.Prepare(owner,3,Vector3.forward);Check(cloud!=null,"flow cloud prepared");cloud.Begin();
        b.healthComponent.alive=false;
        for(int i=0;i<200 && cloud.Tick(.05f);i++){}
        int flowPulses=ThundercloudSchedule.Pulses(3);
        Check(a.healthComponent.received.Count==flowPulses && b.healthComponent.received.Count==0,"flow cloud damages the living victim once per pulse");
        Check(StormServer.primes.Count==flowPulses && StormServer.primes.All(p=>p.victim==a.healthComponent),"cloud primes each struck victim once per pulse");
        Near(StormServer.primes[0].amount,ChargedStormTuning.CloudStaticPrime,"cloud priming amount");
        Check(StormServer.shocks.Count==flowPulses && StormServer.shocks.All(s=>s==a),"each cloud strike Shocks its victim");
        ChargedStormTuning.CloudShocks=false;
        Reset();StormServer.primes.Clear();StormServer.shocks.Clear();
        owner=Body();a=Body(10,TeamIndex.Monster);Physics.AimHit=new(0,0,10);BullseyeSearch.candidates.Add(a.mainHurtBox);
        cloud=ServerThundercloud.Prepare(owner,1,Vector3.forward);cloud.Begin();for(int i=0;i<200 && cloud.Tick(.05f);i++){}
        Check(StormServer.shocks.Count==0 && StormServer.primes.Count==ThundercloudSchedule.Pulses(1),"Shock option disables only the Shock");
        ChargedStormTuning.CloudShocks=true;

        // Orb: one prime per enemy hit, decreasing with revisits; the final burst primes each caught enemy at half strength.
        Reset();StormServer.primes.Clear();
        owner=Body();a=Body(5,TeamIndex.Monster);b=Body(10,TeamIndex.Monster);
        BullseyeSearch.candidates.AddRange(new[]{a.mainHurtBox,b.mainHurtBox});
        var orb=new ServerHollowedOrb(owner,3,Vector3.forward);orb.Begin();
        for(int i=0;i<600 && orb.Tick(.02f);i++){}
        int hits=a.healthComponent.received.Count+b.healthComponent.received.Count;
        int orbHits=OrbHits(a)+OrbHits(b);
        Check(orbHits==ChargedStormTuning.HitBudget(3) && StormServer.primes.Count==hits,"each enemy Orb hit and burst hit primes once");
        Check(StormServer.primes.All(p=>p.victim!=owner.healthComponent),"the owner is never primed");
        Check(hits>orbHits && StormServer.primes.Skip(orbHits).All(p=>Math.Abs(p.amount-ChargedStormTuning.OrbStaticPrime*.5f)<.001f),"burst primes at half strength");
        var aPrimes=StormServer.primes.Take(orbHits).Where(p=>p.victim==a.healthComponent).Select(p=>p.amount).ToList();
        Near(aPrimes[0],ChargedStormTuning.OrbStaticPrime,"fresh Orb hit primes fully");
        for(int i=1;i<aPrimes.Count;i++) Check(aPrimes[i]<aPrimes[i-1],"revisit primes less");
        Check(StormServer.Depth==0,"storm damage scope balanced after flow casts");
    }
}
