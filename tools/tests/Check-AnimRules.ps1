# Success: gesture layer choice, run-stop eligibility and foot, fidget picks and the
# 8-15 s idle fidget timer and consistent rising/falling loop after a dash
# (FoundationAnimRules.cs, no Unity dependency).
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$source = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\Character\Animation\FoundationAnimRules.cs'))
$checks = @'
namespace HollowSaint
{
    public static class AnimRulesChecks
    {
        private static void Check(bool ok, string message) { if (!ok) throw new System.Exception(message); }
        public static string Run()
        {
            Check(!FoundationAnimRules.UseArmsLayer(0f, true), "Standing uses UpperBody");
            Check(!FoundationAnimRules.UseArmsLayer(1.0f, true), "Creeping uses UpperBody");
            Check(FoundationAnimRules.UseArmsLayer(1.5f, true), "Walk uses UpperArms");
            Check(FoundationAnimRules.UseArmsLayer(0f, false), "Airborne uses UpperArms");
            Check(FoundationAnimRules.IsAscending(12f) && FoundationAnimRules.IsAscending(0.5001f), "Rising dash exit preserves ascent");
            Check(!FoundationAnimRules.IsAscending(0.5f) && !FoundationAnimRules.IsAscending(0f), "Apex uses descending loop");
            Check(!FoundationAnimRules.IsAscending(-12f) && !FoundationAnimRules.IsAscending(float.NaN), "Falling/invalid motion never rises");
            Check(FoundationAnimRules.ShouldRunStop(6f, 1f), "Forward run stops");
            Check(!FoundationAnimRules.ShouldRunStop(2f, 1f), "Walk does not stop");
            Check(!FoundationAnimRules.ShouldRunStop(6f, 0f), "Strafe does not stop");
            Check(FoundationAnimRules.TrackRecentSpeed(7f, 0f, 0.05f) > 5.9f, "Recent speed decays slowly");
            Check(FoundationAnimRules.TrackRecentSpeed(7f, 0f, 1f) == 0f, "Recent speed floors at current");
            Check(!FoundationAnimRules.RunStopOnRightFoot(0.1f) && FoundationAnimRules.RunStopOnRightFoot(0.5f) &&
                !FoundationAnimRules.RunStopOnRightFoot(1.9f), "Stop foot follows phase");
            Check(FoundationAnimRules.PickFidget(0, -1, 0.5f) == -1, "No fidgets");
            for (int prev = 0; prev < 3; prev++)
                for (float roll = 0f; roll < 1f; roll += 0.01f)
                {
                    int pick = FoundationAnimRules.PickFidget(3, prev, roll);
                    Check(pick >= 0 && pick < 3 && pick != prev, "Fidget repeat or out of range");
                }
            var timer = new FoundationIdleFidgetTimer((a, b) => 10f);
            Check(!timer.Tick(true, 0.1f) && timer.Armed, "Arms on first eligible tick");
            Check(!timer.Tick(true, 9.5f), "Not due before 10 s");
            Check(timer.Tick(true, 1f), "Due after 10 s");
            Check(!timer.Tick(true, 0.1f), "Rearms after firing");
            Check(!timer.Tick(false, 0.1f) && !timer.Armed, "Movement disarms");
            var wild = new FoundationIdleFidgetTimer((a, b) => 99f);
            wild.Tick(true, 0f);
            Check(wild.Remaining <= 15f, "Wait clamped to 15 s");
            return "ANIM_RULES_PASS: arm layer, airborne/dash loop, run stop, fidget pick, fidget timer.";
        }
    }
}
'@
Add-Type -TypeDefinition ($source + [Environment]::NewLine + $checks)
[HollowSaint.AnimRulesChecks]::Run()
