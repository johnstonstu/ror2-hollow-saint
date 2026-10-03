# Success: no early impact; one launch/impact; no overlapping flight; cooldown begins
# at impact; cancel cannot impact later. Uses the actual production deadline class.
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$source = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\Storm\ThunderboltFlight.cs'))
$checks = @'
namespace HollowSaint.FoundationKit.Storm
{
    public static class ThunderboltFlightChecks
    {
        private static void Check(bool ok, string message)
        {
            if (!ok) throw new System.Exception(message);
        }

        public static string Run()
        {
            var flight = new ThunderboltFlight();
            int consumes = 0, releases = 0, impacts = 0;
            if (flight.Launch(10f, 4f)) { consumes++; releases++; }
            Check(!flight.Launch(10.1f, 4f), "Overlapping launch accepted");
            Check(!flight.Impact(10.249f), "Impact occurred before flight deadline");
            if (flight.Impact(10.25f)) impacts++;
            Check(!flight.Impact(10.26f), "Duplicate impact accepted");
            Check(consumes == 1 && releases == 1 && impacts == 1, "Flight was not exactly once");
            Check(!flight.Ready(14.249f, 4f), "Cooldown measured from launch instead of impact");
            Check(flight.Ready(14.25f, 4f), "Cooldown did not expire at impact + 4 seconds");
            Check(flight.Launch(14.25f, 4f), "Next ready launch refused");
            Check(!flight.Impact(14.499f), "Second flight impacted early");
            Check(flight.Impact(14.5f), "Second flight did not impact on deadline");
            Check(flight.Launch(14.5f, 0f), "Zero cooldown launch refused");
            Check(flight.Cancel(), "Destroyed owner flight did not cancel");
            Check(!flight.Impact(99f), "Cancelled flight impacted later");
            Check(!flight.Cancel(), "Cancellation accepted twice");
            return "THUNDERBOLT_FLIGHT_PASS: deadline, exactly-once transitions, overlap guard, impact cooldown, cancellation.";
        }
    }
}
'@
Add-Type -TypeDefinition ($source + [Environment]::NewLine + $checks)
[HollowSaint.FoundationKit.Storm.ThunderboltFlightChecks]::Run()
