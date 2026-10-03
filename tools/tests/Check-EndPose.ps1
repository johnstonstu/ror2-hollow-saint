# Success: separate native editor-frame rendering completes 120 posed frames;
# the four ring arcs return within 5 mm of authored rest after the closing clip.
# Baseline intentionally skips that clip and must fail the same rest check.
param([switch]$Baseline)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
& (Join-Path $PSScriptRoot 'Prepare-FxValidation.ps1')
$output = Join-Path $repo $(if ($Baseline) { 'artifacts\end-pose-baseline' } else { 'artifacts\end-pose02' })
New-Item -ItemType Directory -Force $output | Out-Null
$previous = $env:HS_END_POSE_BASELINE
try {
    $env:HS_END_POSE_BASELINE = $(if ($Baseline) { '1' } else { '0' })
    $taskArgs = @('-batchmode', '-projectPath', ('"' + (Join-Path $repo 'HollowSaintUnityProject') + '"'), '-executeMethod', 'HollowSaint.PreviewValidation.EndPoseFrameCapture.RunBatch', '-logFile', ('"' + (Join-Path $output 'unity.log') + '"'))
    # No -quit: the runner schedules captures across EditorApplication.update and exits itself.
    $taskProcess = Start-Process 'C:\Program Files\Unity 2021.3.33f1\Editor\Unity.exe' -WindowStyle Hidden -ArgumentList $taskArgs -PassThru
    $taskProcess.WaitForExit()
    $report = Get-Content (Join-Path $output 'verification.txt') -Raw
    if ($Baseline) {
        if ($taskProcess.ExitCode -eq 0 -or -not $report.Contains('Ring did not close')) { throw 'Missing-closing-clip baseline did not reproduce the expected failure' }
        'EXPECTED_BASELINE_FAILURE: ring fails to return after the missing closing clip.'
    } else {
        if ($taskProcess.ExitCode -ne 0 -or -not $report.StartsWith('ALL PASS')) { throw $report }
        $report.TrimEnd()
    }
} finally { $env:HS_END_POSE_BASELINE = $previous }
