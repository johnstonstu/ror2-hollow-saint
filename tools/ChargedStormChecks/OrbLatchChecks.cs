using System;
using System.Collections.Generic;
using System.Linq;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.HollowedOrb;
using HollowSaint.FoundationKit.Storm;
using RoR2;
using UnityEngine;

// Latching storm ball: with no other target the Orb clings to its last victim and
// zaps the rest of its finite budget every latch interval, then bursts exactly once.
// The owner is never a target. Fresh enemies still win; deaths, stage changes and
// owner loss end cleanly without phantom damage.
static partial class Program
{
    static bool IsBurst(DamageInfo d) => Math.Abs(d.procCoefficient-.3f)<.001f;
    static int OrbHits(CharacterBody x) => x.healthComponent.received.Count(d=>!IsBurst(d));
    static int BurstHits(CharacterBody x) => x.healthComponent.received.Count(IsBurst);
    static List<DamageInfo> OrbDamage(CharacterBody x) => x.healthComponent.received.Where(d=>!IsBurst(d)).ToList();
    static void Drain(ServerHollowedOrb orb,float dt=.25f) { int frames=0; while(orb.Tick(dt)) Check(++frames<80,"orb flight cannot loop forever"); }
    static float Launch(int charges) => 10*KitDamagePolicy.Effective(ChargedStormTuning.OrbCoefficient(charges));
    static void OrbLatch()
    {
        Check(ChargedStormTuning.HitBudget(0)==3 && ChargedStormTuning.HitBudget(1)==4 && ChargedStormTuning.HitBudget(5)==8,"hit budget: free 3, plus one per extra charge");
        Near(ChargedStormTuning.OrbCoefficient(0),4.2f,"free Orb raw coefficient");
        Near(ChargedStormTuning.OrbCoefficient(1),5f,"one-charge raw coefficient");
        Near(ChargedStormTuning.OrbCoefficient(5),8.2f,"five-charge raw coefficient");
        Near((float)OrbBouncePolicy<object>.RepeatRetain,.75f,"repeat hits retain three quarters");
        Near(ChargedStormTuning.BurstRadius(0),3,"free burst radius");Near(ChargedStormTuning.BurstRadius(1),3.8f,"one-charge burst radius");
        Near(ChargedStormTuning.BurstRadius(5),7,"five-charge burst radius");
        Check(ChargedStormTuning.BurstRadius(64)<=12 && ChargedStormTuning.BurstRadius(-5)>=1,"burst radius bounded");

        // Free Orb on a lone enemy: 3 hits at 1, .75, .5625, one zap per latch interval, then one burst.
        Reset();var owner=Body();var a=Body(5,TeamIndex.Monster);BullseyeSearch.candidates.Add(a.mainHurtBox);
        var orb=new ServerHollowedOrb(owner,0,Vector3.forward);orb.Begin();
        Check(orb.Tick(.5f) && OrbHits(a)==1 && ChargedStormEffects.lastOrbTarget==a.healthComponent,"free Orb latches onto a lone enemy");
        Check(orb.Tick(.1f) && OrbHits(a)==1,"latched zaps wait for the latch interval");
        Check(orb.Tick(.15f) && OrbHits(a)==2,"second latched zap");
        Check(orb.Tick(.1f) && OrbHits(a)==2,"latched zap cadence holds");
        Check(!orb.Tick(.15f) && OrbHits(a)==3,"third zap spends the free budget and ends the orb");
        var zaps=OrbDamage(a);float free=Launch(0);float[] scales={1f,.75f,.5625f};
        Near(free,37.8f,"free Orb effective damage at body damage 10");
        for(int i=0;i<3;i++)
        {
            Near(zaps[i].damage,free*scales[i],"latched repeat scale "+i);
            Near(zaps[i].procCoefficient,i==0?.8f:.25f,"latched repeat proc "+i);
        }
        Check(BurstHits(a)==1 && ChargedStormEffects.bursts.Count==1,"spent latch bursts exactly once");
        Near(a.healthComponent.received.Last().damage,free*ChargedStormTuning.OrbBurstFraction,"burst deals its fraction of launch damage");
        Near(ChargedStormEffects.bursts[0].radius,3,"free burst uses the small radius");
        Check(StormServer.primes.Count==4 && StormServer.primes.All(p=>p.victim==a.healthComponent),"each zap and the burst prime once");
        Near(StormServer.primes[2].amount,ChargedStormTuning.OrbStaticPrime*.5625f,"latched priming follows damage falloff");
        Check(owner.healthComponent.received.Count==0,"owner is never struck");
        Check(!orb.Tick(1) && a.healthComponent.received.Count==4 && ChargedStormEffects.bursts.Count==1,"a finished orb cannot zap or burst again");

        foreach(int tier in new[]{0,1,3,5})
        {
            Reset();owner=Body();var boss=Body(5,TeamIndex.Monster);BullseyeSearch.candidates.Add(boss.mainHurtBox);
            orb=new ServerHollowedOrb(owner,tier,Vector3.forward);orb.Begin();Drain(orb);
            int budget=ChargedStormTuning.HitBudget(tier);var hits=OrbDamage(boss);
            Check(hits.Count==budget,"lone latch spends exactly the finite budget "+tier);
            for(int i=0;i<budget;i++)
            {
                Near(hits[i].damage,Launch(tier)*MathF.Pow(.75f,i),"latched attenuation "+tier);
                Near(hits[i].procCoefficient,i==0?.8f:.25f,"latched proc "+tier);
            }
            Check(BurstHits(boss)==1 && ChargedStormEffects.bursts.Count==1,"lone latch ends in one burst "+tier);
            Near(ChargedStormEffects.bursts[0].radius,ChargedStormTuning.BurstRadius(tier),"burst radius follows charges "+tier);
            Check(owner.healthComponent.received.Count==0,"owner never damaged "+tier);
        }
        // 1.3.1: Backup Magazine adds hits (also on a lone, latched target), never extra casts.
        Check(ChargedStormTuning.HitBudget(0,1)==ChargedStormTuning.HitBudget(0)+1 && ChargedStormTuning.HitBudget(5,2)==ChargedStormTuning.HitBudget(5)+2,"each magazine adds one hit");
        Check(ChargedStormTuning.HitBudget(5,40)==24 && ChargedStormTuning.HitBudget(1,-3)==ChargedStormTuning.HitBudget(1),"magazine hits are bounded and never negative");
        {
            Reset();var magOwner=Body();var lone=Body(5,TeamIndex.Monster);BullseyeSearch.candidates.Add(lone.mainHurtBox);
            var magOrb=new ServerHollowedOrb(magOwner,1,Vector3.forward,2);magOrb.Begin();Drain(magOrb);
            Check(OrbHits(lone)==ChargedStormTuning.HitBudget(1)+2,"two magazines add two latched zaps on a lone target");
        }

        // Two enemies still alternate A-B-A-B through all eight five-charge hits.
        Reset();owner=Body();a=Body(4,TeamIndex.Monster);var b=Body(6,TeamIndex.Monster);
        BullseyeSearch.candidates.AddRange(new[]{a.mainHurtBox,b.mainHurtBox});
        orb=new ServerHollowedOrb(owner,5,(a.corePosition-KitUtil.EyePosition(owner)).normalized);orb.Begin();
        var order=new List<HealthComponent>();
        for(int frame=0;frame<40;frame++)
        {
            int beforeA=OrbHits(a),beforeB=OrbHits(b);
            bool more=orb.Tick(.25f);
            if(OrbHits(a)>beforeA)order.Add(a.healthComponent);
            if(OrbHits(b)>beforeB)order.Add(b.healthComponent);
            if(!more)break;
        }
        Check(order.Count==8,"two-enemy five-charge flight uses all eight hits");
        for(int i=0;i<order.Count;i++)Check(order[i]==(i%2==0?a.healthComponent:b.healthComponent),"two enemies keep A B A B alternation");
        Check(BurstHits(a)==1 && BurstHits(b)==1 && ChargedStormEffects.bursts.Count==1,"final burst catches both nearby enemies once");
        Check(owner.healthComponent.received.Count==0,"alternation never routes through the owner");

        // A fresh enemy entering reach breaks the latch; the orb flies to it.
        Reset();owner=Body();a=Body(5,TeamIndex.Monster);BullseyeSearch.candidates.Add(a.mainHurtBox);
        orb=new ServerHollowedOrb(owner,5,Vector3.forward);orb.Begin();
        Check(orb.Tick(.25f) && ChargedStormEffects.lastOrbTarget==a.healthComponent,"lone enemy latched");
        Check(orb.Tick(.25f) && OrbHits(a)==2,"latched zap lands");
        var c=Body(9,TeamIndex.Monster);BullseyeSearch.candidates.Add(c.mainHurtBox);
        Check(orb.Tick(.25f) && ChargedStormEffects.lastOrbTarget==c.healthComponent && OrbHits(a)==2,"fresh enemy in reach breaks the latch without zapping");
        Check(orb.Tick(.25f) && OrbHits(c)==1,"orb flies on to the fresh enemy");
        Near(c.healthComponent.received[0].damage,Launch(5),"fresh victim takes a full-strength hit");
        Near(c.healthComponent.received[0].procCoefficient,.8f,"fresh victim takes the fresh proc");
        Drain(orb);
        Check(OrbHits(a)+OrbHits(c)==8 && ChargedStormEffects.bursts.Count==1,"broken latch keeps the finite budget and one burst");

        // Latched victim dies: hop to a remaining candidate, otherwise burst. No phantom damage.
        Reset();owner=Body();a=Body(5,TeamIndex.Monster);BullseyeSearch.candidates.Add(a.mainHurtBox);
        orb=new ServerHollowedOrb(owner,5,Vector3.forward);orb.Begin();orb.Tick(.25f);a.healthComponent.alive=false;
        Check(!orb.Tick(.25f) && a.healthComponent.received.Count==1 && ChargedStormEffects.bursts.Count==1,"latched victim death with nothing in reach bursts");
        Check(owner.healthComponent.received.Count==0,"empty burst harms nobody");
        Reset();owner=Body();a=Body(5,TeamIndex.Monster);BullseyeSearch.candidates.Add(a.mainHurtBox);
        orb=new ServerHollowedOrb(owner,5,Vector3.forward);orb.Begin();orb.Tick(.25f);
        b=Body(9,TeamIndex.Monster);BullseyeSearch.candidates.Add(b.mainHurtBox);a.healthComponent.alive=false;
        Check(orb.Tick(.05f) && ChargedStormEffects.lastOrbTarget==b.healthComponent && b.healthComponent.received.Count==0,"latched victim death hops to a remaining candidate");
        Drain(orb);
        Check(a.healthComponent.received.Count==1 && OrbHits(b)==7 && BurstHits(b)==1,"redirected orb spends the rest on the living enemy without phantom hits");

        // Owner death and stage change cancel a latch outright.
        Reset();owner=Body();a=Body(5,TeamIndex.Monster);BullseyeSearch.candidates.Add(a.mainHurtBox);
        orb=new ServerHollowedOrb(owner,5,Vector3.forward);orb.Begin();orb.Tick(.25f);owner.healthComponent.alive=false;
        Check(!orb.Tick(.25f) && a.healthComponent.received.Count==1 && ChargedStormEffects.bursts.Count==0,"owner death cancels a latch");
        Reset();owner=Body();a=Body(5,TeamIndex.Monster);BullseyeSearch.candidates.Add(a.mainHurtBox);
        orb=new ServerHollowedOrb(owner,5,Vector3.forward);orb.Begin();orb.Tick(.25f);Stage.instance=new();
        Check(!orb.Tick(.25f) && a.healthComponent.received.Count==1 && ChargedStormEffects.bursts.Count==0,"stage change cancels a latch");

        // Burst radius grows with charges: a 5 m neighbour is caught only by the big burst.
        foreach(int tier in new[]{1,5})
        {
            Reset();owner=Body();a=Body(5,TeamIndex.Monster);b=Body(10,TeamIndex.Monster);
            BullseyeSearch.candidates.AddRange(new[]{a.mainHurtBox,b.mainHurtBox});
            orb=new ServerHollowedOrb(owner,tier,(a.corePosition-KitUtil.EyePosition(owner)).normalized);orb.Begin();Drain(orb);
            Check(OrbHits(a)+OrbHits(b)==ChargedStormTuning.HitBudget(tier),"alternating budget "+tier);
            Check(BurstHits(b)==1 && ChargedStormEffects.bursts.Count==1,"final victim takes the burst once "+tier);
            Check(BurstHits(a)==(tier==5?1:0),"burst radius grows with charges to reach a 5m neighbour: "+tier);
            Near(b.healthComponent.received.Last().damage,Launch(tier)*ChargedStormTuning.OrbBurstFraction,"burst damage is unattenuated "+tier);
        }

        // Tall targets: the latch follows the real core path regardless of foot geometry.
        foreach(bool flying in new[]{false,true})
        {
            Reset();owner=Body();var boss=Body(4,TeamIndex.Monster);
            boss.transform.position=flying?new(0,20,4):new(0,12,4);boss.footOffset=flying?new(0,-20,0):new(0,-12,0);
            BullseyeSearch.candidates.Add(boss.mainHurtBox);
            orb=new ServerHollowedOrb(owner,0,(boss.corePosition-KitUtil.EyePosition(owner)).normalized);orb.Begin();
            Check(orb.Tick(1f) && OrbHits(boss)==1 && ChargedStormEffects.lastOrbTarget==boss.healthComponent,"tall target latch uses the real core path: "+flying);
            Drain(orb);
            Check(OrbHits(boss)==3 && owner.healthComponent.received.Count==0,"tall boss receives the full free budget: "+flying);
        }
    }
}
