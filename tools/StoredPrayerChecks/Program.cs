using System;
using HollowSaint.FoundationKit.Storm;
using HollowSaint.FoundationKit.Stormspear;

// Success: production spend policy preserves every partial bank, commits only at
// created server projectile, and one funded landing uses immutable strike parameters.
static class Program
{
    static int checks;
    static void Check(bool valid, string name) { checks++; if(!valid)throw new Exception("FAIL "+name); }
    static int Main()
    {
        try { Run(); return 0; }
        catch(Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    static void Run()
    {
        foreach(int capacity in new[]{2,5,6,20})
        {
            for(int initial=0;initial<capacity;initial++)
            {
                int bank=initial;
                Check(!StoredPrayerPolicy.TrySpend(ref bank,capacity,true,true,true,false,out int spent),"partial bank cannot fund spear");
                Check(bank==initial&&spent==0,"all partial charges retained");
            }
            foreach(string blocked in new[]{"remote authority optimism","failed projectile creation","owner death","Gaze reserve"})
            {
                int bank=capacity;
                Check(!StoredPrayerPolicy.TrySpend(ref bank,capacity,blocked!="remote authority optimism",blocked!="failed projectile creation",
                    blocked!="owner death",blocked=="Gaze reserve",out int spent),blocked+" cannot spend full bank");
                Check(bank==capacity&&spent==0,"failed admission preserves entire bank");
            }
            int full=capacity;
            Check(StoredPrayerPolicy.TrySpend(ref full,capacity,true,true,true,false,out int committed)&&committed==capacity&&full==0,"created server projectile spends full configured bank");
            Check(!StoredPrayerPolicy.TrySpend(ref full,capacity,true,true,true,false,out _)&&full==0,"spent bank cannot empower again");
            Check(capacity==committed+full,"claim conservation");
            int retained=20;
            Check(StoredPrayerPolicy.TrySpend(ref retained,capacity,true,true,true,false,out int all)&&all==20&&retained==0,"config lowering retains then explicitly consumes whole prior bank");
        }
        Check(StoredPrayerPolicy.Capacity(-5)==2&&StoredPrayerPolicy.Capacity(5)==5&&StoredPrayerPolicy.Capacity(999)==20,"configured capacity clamped two to twenty, default five");
        float ownerDamage=17,coefficient=10,fraction=.5f,radius=3;
        var launch=new PrayerStrikeSnapshot(ownerDamage,coefficient,fraction,radius,true,funded:true);
        ownerDamage=1000;coefficient=1;fraction=0;radius=20;
        Check(launch.Empowered&&Math.Abs(launch.Damage-130.05f)<.0001f&&Math.Abs(launch.SplashDamage-65.025f)<.0001f&&launch.SplashRadius==3&&launch.Crit,"funded launch damage crit splash snapshot unaffected by later stats/tuning");
        var unfundedCrown=new PrayerStrikeSnapshot(17,10,.5f,3,true);
        Check(Math.Abs(unfundedCrown.Damage-153)<.0001f&&Math.Abs(unfundedCrown.SplashDamage-76.5f)<.0001f,"unfunded Crown strike is exempt from targeted funded reduction");
        Check(!default(PrayerStrikeSnapshot).Empowered,"unfunded projectile has no Prayer bonus");
        foreach(string landing in new[]{"enemy","world terrain"})
        {
            var impact=new PrayerImpactClaim();
            Check(impact.TryResolve(true,true,true,true),landing+" first valid funded landing strikes");
            for(int collider=0;collider<8;collider++)Check(!impact.TryResolve(true,true,true,true),"duplicate impact cannot strike twice");
            impact.Cancel();Check(!impact.TryResolve(true,true,true,true),"cleanup after strike cannot refund or strike");
        }
        foreach(string loss in new[]{"miss/friendly/nonworld","owner dead/replaced/disabled","stage changed","unfunded"})
        {
            var impact=new PrayerImpactClaim();
            Check(!impact.TryResolve(loss!="unfunded",loss!="miss/friendly/nonworld",loss!="owner dead/replaced/disabled",loss!="stage changed"),loss+" rejects Prayer bonus");
            Check(!impact.TryResolve(true,true,true,true),"closed miss/loss cannot revive strike");
        }
        var expired=new PrayerImpactClaim();expired.Cancel();Check(!expired.TryResolve(true,true,true,true),"projectile lifetime expiry closes funded miss");
        var crown=new StormspearShot(StormspearShot.ForceForCharge(1),StormspearShot.CrownCombo);
        Check(crown.CallsThunderbolt(true,1)&&!crown.CallsThunderbolt(true,1,true),"ordinary Crown bonus retained, funded Crown never doubles");
        var hand=new StormspearShot(StormspearShot.ForceForCharge(1),0);
        Check(!hand.CallsThunderbolt(true,1)&&!hand.CallsThunderbolt(true,1,true),"ordinary hand spear gains no free strike");
        Console.WriteLine("PASS "+checks+" production stored-Prayer assertions (bank, launch snapshot, impact idempotency and cleanup)");
    }
}
