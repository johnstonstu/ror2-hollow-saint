# Success: a 2.9-degree near miss is eligible; 3.1 degrees, occlusion, behind,
# out-of-range and disabled assistance are rejected. Steering never exceeds
# 45 degrees/s or 8 cumulative degrees. At both live default projectile speeds,
# a simulated 2-degree miss at 40 m becomes a hit within the projectile radius.
# This is geometry verification, not a substitute for live collision/network QA.
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$rules = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\AimForgivenessRules.cs'))
$checks = @'
namespace HollowSaint.FoundationKit {
 public static class AimForgivenessChecks {
  private static void Check(bool ok, string why) { if (!ok) throw new System.Exception(why); }
  private static float Cos(float angle) { return (float)System.Math.Cos(angle * System.Math.PI / 180.0); }
  private static bool Near(float a, float b) { return System.Math.Abs(a-b) < 0.0001f; }
  private static double Miss(float speed, bool assisted) {
   double x=0, y=0, heading=0, tx=40, ty=40*System.Math.Tan(2*System.Math.PI/180), closest=100;
   float budget=AimForgivenessRules.TotalTurnDegrees;
   for(int i=0; i<100; i++) {
    double desired=System.Math.Atan2(ty-y,tx-x), error=desired-heading;
    if(assisted && System.Math.Abs(error)<System.Math.PI/2) {
     float step=AimForgivenessRules.TurnStep((float)(System.Math.Abs(error)*180/System.Math.PI),0.02f,budget);
     heading+=System.Math.Sign(error)*step*System.Math.PI/180;
     budget-=step;
    }
    double nx=x+speed*0.02*System.Math.Cos(heading), ny=y+speed*0.02*System.Math.Sin(heading);
    double dx=nx-x, dy=ny-y, fraction=((tx-x)*dx+(ty-y)*dy)/(dx*dx+dy*dy);
    fraction=System.Math.Max(0,System.Math.Min(1,fraction));
    double mx=x+dx*fraction-tx, my=y+dy*fraction-ty;
    closest=System.Math.Min(closest,System.Math.Sqrt(mx*mx+my*my));
    x=nx; y=ny;
    if(x>tx+5) break;
   }
   return closest;
  }
  public static string Run() {
   Check(AimForgivenessRules.CanAcquire(Cos(2.9f),40,3,true),"Near miss not acquired");
   Check(!AimForgivenessRules.CanAcquire(Cos(3.1f),40,3,true),"Outside cone acquired");
   Check(!AimForgivenessRules.CanAcquire(1,40,3,false),"Occluded target acquired");
   Check(!AimForgivenessRules.CanAcquire(-1,40,3,true),"Behind target acquired");
   Check(!AimForgivenessRules.CanAcquire(1,81,3,true),"Out-of-range target acquired");
   Check(!AimForgivenessRules.CanAcquire(1,40,0,true),"Disabled assist acquired");
   Check(!AimForgivenessRules.CanAcquire(1,0,3,true),"Zero offset acquired");
   Check(!AimForgivenessRules.CanAcquire(Cos(7),40,100,true),"Config bypassed 6-degree cone ceiling");
   Check(Near(AimForgivenessRules.TurnStep(30,0.02f,8),0.9f),"Turn rate exceeded");
   Check(Near(AimForgivenessRules.TurnStep(0.1f,0.02f,8),0.1f),"Overshot target heading");
   Check(Near(AimForgivenessRules.TurnStep(30,1,0.2f),0.2f),"Remaining budget exceeded");
   Check(Near(AimForgivenessRules.TurnStep(30,-1,8),0),"Negative time steered");
   float budget=8, used=0;
   for(int i=0;i<200;i++) { float step=AimForgivenessRules.TurnStep(30,0.02f,budget); budget-=step; used+=step; }
   Check(Near(used,8) && Near(budget,0),"Lifetime turn cap failed");
   Check(Miss(80,false)>0.6 && Miss(80,true)<0.6,"Arc Bolt near miss not recovered");
   Check(Miss(150,false)>0.35 && Miss(150,true)<0.35,"Spear near miss not recovered");
   return "AIM_ASSIST_PASS: 15 checks; cone, occlusion/range/disable, turn limits and near-miss flight at 80/150 m/s.";
  }
 }
}
'@
Add-Type -TypeDefinition ($rules + [Environment]::NewLine + $checks)
[HollowSaint.FoundationKit.AimForgivenessChecks]::Run()
