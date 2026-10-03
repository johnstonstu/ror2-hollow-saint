# Success: current builds before release, is strongest at release, ends within
# 0.12s, cancels within 0.04s, rejects stale cast tokens and stays finite at high speed.
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$source = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\Vfx\AbilityCurrentWindow.cs'))
$checks = @'
namespace HollowSaint.FoundationKit.Vfx
{
    public static class CurrentChecks
    {
        private static void Check(bool ok, string detail) { if (!ok) throw new System.Exception(detail); }
        public static string Run()
        {
            var window = new AbilityCurrentWindow();
            float weight, travel, pulse;
            Check(!window.Sample(0f, out weight, out travel, out pulse), "Idle current visible");
            uint first = window.Begin(10f, 0.5f, 0.2f);
            window.Sample(10.05f, out weight, out travel, out pulse);
            Check(weight > 0.6f && weight < 0.8f && travel > 0.5f && travel < 0.7f, "Pre-release build/path");
            window.Release(first, 10.1f);
            window.Sample(10.1f, out weight, out travel, out pulse);
            Check(weight > 0.999f && travel > 0.999f && pulse > 0.999f, "Release pulse not at hand");
            Check(!window.Sample(10.23f, out weight, out travel, out pulse), "Current lingers after release");
            first = window.Begin(20f, 0.5f, 0.2f);
            uint second = window.Begin(20.05f, 0.5f, 0.2f);
            window.Cancel(first, 20.1f);
            Check(window.Sample(20.12f, out weight, out travel, out pulse), "Stale exit cancelled next cast");
            window.Cancel(second, 20.12f);
            window.Sample(20.13f, out weight, out travel, out pulse);
            Check(weight > 0 && pulse == 0f, "Interrupt fails to fade or flashes release");
            Check(!window.Sample(20.17f, out weight, out travel, out pulse), "Cancelled current lingers");
            for (int speed = 1; speed <= 100; speed++)
            {
                uint token = window.Begin(30f, 0.5f / speed, 0.2f);
                window.Release(token, 30.001f);
                Check(window.Sample(30.001f, out weight, out travel, out pulse), "Fast release invisible");
                Check(weight <= 1.001f && travel <= 1.001f && pulse <= 1.001f &&
                    !float.IsNaN(weight + travel + pulse), "Fast release unbounded");
            }
            uint late = window.Begin(40f, 0.5f, 0.2f);
            window.Release(late, 40.5f);
            Check(window.Sample(40.5f, out weight, out travel, out pulse) && pulse > 0.999f, "Late fixed-step release lost");
            window.Clear();
            Check(!window.Sample(40.5f, out weight, out travel, out pulse), "Death/reset retained current");
            window.Begin(50f, float.NaN, float.PositiveInfinity);
            window.Sample(50.05f, out weight, out travel, out pulse);
            Check(!float.IsNaN(weight + travel + pulse), "Nonfinite settings poison effect");
            return "ABILITY_CURRENT_PASS: build, release, fade, interruption, stale token, high speed, late release, reset.";
        }
    }
}
'@
Add-Type -TypeDefinition ($source + [Environment]::NewLine + $checks)
[HollowSaint.FoundationKit.Vfx.CurrentChecks]::Run()
