# Success: slow walks change cadence (not stride); authored speeds map to 1x;
# diagonals/reversals stay finite/continuous; contacts follow actual clip phase.
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$source = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationLocomotionMath.cs'))
$checks = @'
namespace HollowSaint
{
    public static class LocomotionChecks
    {
        private static void Check(bool ok, string message) { if (!ok) throw new System.Exception(message); }
        private static void Near(float value, float expected, string message)
        { Check(System.Math.Abs(value - expected) < 0.0001f, message + ": " + value); }
        public static string Run()
        {
            var slow = FoundationLocomotionMath.Evaluate(0f, 1f, 0.3f);
            Near(slow.Gait, 0f, "Slow movement must remain a full-stride walk");
            Near(slow.Rate, 0.2f, "0.3m/s forward walk cadence");
            Near(FoundationLocomotionMath.Evaluate(0f, 1f, 1.5f).Rate, 1f, "Authored forward walk");
            Near(FoundationLocomotionMath.Evaluate(1f, 0f, 0.6f).Rate, 1f, "Authored strafe walk");
            Near(FoundationLocomotionMath.Evaluate(1f, 0f, 3.4f).Rate, 1f, "Authored strafe run");
            Near(FoundationLocomotionMath.Evaluate(1f, 1f, 4.7f).Rate, 1f, "Authored diagonal run");
            Near(FoundationLocomotionMath.Evaluate(0f, -1f, 4.25f).Rate, 1f, "Authored backward run");
            Near(FoundationLocomotionMath.Evaluate(0f, 1f, 12f).Rate, 2f, "Fast forward cadence");
            Near(FoundationLocomotionMath.Evaluate(1f, 0f, 6.8f).Rate, 2f, "Fast strafe cadence");
            Near(FoundationLocomotionMath.Evaluate(0f, 0f, 0f).Rate, 0f, "Stationary cadence");
            for (int angle = -180; angle <= 180; angle++)
            {
                double a = angle * System.Math.PI / 180.0;
                var s = FoundationLocomotionMath.Evaluate((float)System.Math.Sin(a), (float)System.Math.Cos(a), 2.7f);
                Check(!float.IsNaN(s.Rate) && s.Rate > 0f && s.Gait >= 0f && s.Gait <= 1f, "Invalid direction sample");
                Near(s.ReferenceSpeed * s.Rate, 2.7f, "Projected stride must match actual ground travel");
            }
            Near(FoundationLocomotionMath.MovingEntryPhase("Run start", 0.3f), 0.5f, "Run-start R contact");
            Near(FoundationLocomotionMath.MovingEntryPhase("Glide exit", 0.3f), 0f, "Glide-exit L contact");
            Near(FoundationLocomotionMath.MovingEntryPhase("Land", 2.37f), 0.37f, "Landing retains gait phase");
            var clock = new FoundationFootContactClock();
            Check(!clock.Advance(0.1f), "No fabricated entry contact");
            Check(!clock.Advance(0.49f), "No early contact");
            Check(clock.Advance(0.5f), "R contact missing");
            Check(!clock.Advance(0.5f), "Duplicate R contact");
            Check(clock.Advance(1f), "L contact missing on wrap");
            Check(!clock.Advance(0.2f), "Animator rewind fabricated contact");
            Check(!clock.Advance(8f), "Hitch must not burst missed contacts");
            clock.Reset();
            Check(!clock.Advance(9f), "Reset state leaked a contact");
            return "LOCOMOTION_PASS: authored directions, slow/fast cadence, continuous directional speeds, phase entries, contact clock.";
        }
    }
}
'@
Add-Type -TypeDefinition ($source + [Environment]::NewLine + $checks)
[HollowSaint.LocomotionChecks]::Run()
