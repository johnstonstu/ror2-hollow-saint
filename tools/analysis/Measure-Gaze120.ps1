# Analytical estimate for the installed private candidate, not a game simulation.
# Assumes level 1, default capacity, continuous single-target aim, no crit/items,
# and every pulse striking that target. Excludes forks, splash, passive and healing.
# Integrates core damage continuously; actual fixed-step tick boundaries differ.
param([string]$Revision = 'd383879ba5ce2de6a1dcc1bb3c8269ace491a7b4')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
function Read-Source([string]$name) {
    $result = & git -C $repo show "${Revision}:HollowSaintMod/FoundationKit/Gaze/$name"
    if ($LASTEXITCODE -ne 0) { throw "Cannot read Gaze source $name at $Revision" }
    return $result -join "`n"
}
function Number([string]$source, [string]$name) {
    $pattern = '\b' + [regex]::Escape($name) + '\s*=\s*([0-9.]+)f?\s*;'
    $match = [regex]::Match($source, $pattern)
    if (-not $match.Success) { throw "Cannot resolve numeric field $name" }
    return [double]::Parse($match.Groups[1].Value, [cultureinfo]::InvariantCulture)
}
$tuning = Read-Source 'GazeTuning.cs'
$ledger = Read-Source 'GazeFuelLedger.cs'
$policy = Read-Source 'GazeManualInputPolicy.cs'
$ramp = Read-Source 'GazeRampPolicy.cs'
$state = Read-Source 'GazeState.cs'
$controller = Read-Source 'GazeFuelController.cs'
if (-not $ledger.Contains('0.5f * orbs * 5f / Clamp(capacity)') -or
    -not $state.Contains('Mathf.Max(0.05f, GazeTuning.TickSeconds') -or
    -not $controller.Contains('phase, 1, ledger.Capacity')) {
    throw 'Model assumptions no longer match pulse or tick formulas; review before using.'
}
$baseSeconds = Number $tuning 'BeamSeconds'
$dps = Number $tuning 'DamagePerSecond'
$tick = Number $tuning 'TickSeconds'
$proc = Number $tuning 'ProcCoefficient'
$intake = Number $ledger 'IntakeDuration'
$minimumInterval = Number $policy 'MinimumInterval'
$extraSeconds = Number $policy 'ExtraPerLaunch'
$maximumSeconds = Number $policy 'MaximumActualSeconds'
$rampPerStep = Number $ramp 'DamagePerStep'
$rows = foreach ($speed in @(1, 2, 4, 8)) {
    foreach ($pattern in @('no-fuel', 'early', 'spaced')) {
        $presses = @(switch ($pattern) {
            'early' { @(0, $minimumInterval, (2*$minimumInterval), (3*$minimumInterval), (4*$minimumInterval)) }
            'spaced' { @(0, 2, 4, 6, 8) }
            default { @() }
        })
        $launches = @($presses | ForEach-Object { $_ + $intake })
        $seconds = [Math]::Min($maximumSeconds, $baseSeconds + $extraSeconds*$launches.Count)
        $rate = 1 / [Math]::Max(0.05, $tick/$speed)
        $rampArea = 0.0
        foreach ($launch in $launches) { $rampArea += $rampPerStep * [Math]::Max(0.0, [double]($seconds-$launch)) }
        $core = $dps*$tick*$rate*($seconds+$rampArea)
        $pulse = 0.5*$launches.Count # One orb, capacity five, all direct strikes land.
        [pscustomobject]@{
            AttackSpeed = $speed; Pattern = $pattern; BeamSeconds = $seconds
            CorePercent = [Math]::Round(100*$core, 2)
            PulsePercent = 100*$pulse
            PulseSharePercent = [Math]::Round(100*$pulse/($core+$pulse), 3)
            CoreProcWeightPerSecond = $rate*$proc
        }
    }
}
$rows | ConvertTo-Json
