# Success: Stormspear damage and burst radius interpolate tap to full, ChargeFromCoefficient
# inverts DamageAt, and the end points match the locked design (400% to 1400%).
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$tuning = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\Stormspear\StormspearTuning.cs'))
$stub = @'
namespace UnityEngine { public static class Mathf { public static float Clamp01(float v) { return v < 0f ? 0f : (v > 1f ? 1f : v); } } }
'@
$checks = @'
namespace HollowSaint.FoundationKit.Stormspear {
 public static class StormspearChecks {
  private static void Check(bool ok, string why) { if (!ok) throw new System.Exception(why); }
  private static bool Near(float a, float b) { return System.Math.Abs(a - b) < 0.001f; }
  public static string Run() {
   Check(Near(StormspearTuning.DamageAt(0f), StormspearTuning.TapDamage), "Tap damage");
   Check(Near(StormspearTuning.DamageAt(1f), StormspearTuning.FullDamage), "Full damage");
   Check(Near(StormspearTuning.DamageAt(2f), StormspearTuning.FullDamage), "Charge not clamped");
   Check(Near(StormspearTuning.BurstRadiusAt(0f), StormspearTuning.BurstRadiusTap), "Tap radius");
   Check(Near(StormspearTuning.BurstRadiusAt(1f), StormspearTuning.BurstRadiusFull), "Full radius");
   for (float c = 0f; c <= 1.001f; c += 0.125f)
    Check(Near(StormspearTuning.ChargeFromCoefficient(StormspearTuning.DamageAt(c)), c), "Inverse at " + c);
   Check(Near(StormspearTuning.TapDamage, 4f) && Near(StormspearTuning.FullDamage, 14f), "Defaults drifted from 400% to 1400%");
   return "STORMSPEAR_PASS: damage and radius ramps, inverse charge recovery, locked end points.";
  }
 }
}
'@
Add-Type -TypeDefinition ($stub + [Environment]::NewLine + $tuning + [Environment]::NewLine + $checks)
[HollowSaint.FoundationKit.Stormspear.StormspearChecks]::Run()
