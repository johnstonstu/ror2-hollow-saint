# Success: connected current hands off without torso jumps and follows the native
# controller's final poses with isolated skin palettes and bounded cleanup.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
& (Join-Path $PSScriptRoot 'Prepare-FxValidation.ps1')
$output = Join-Path $repo 'artifacts\current-flow02'
New-Item -ItemType Directory -Force $output | Out-Null
$project = Join-Path $repo 'HollowSaintUnityProject'
$log = Join-Path $output 'unity.log'
$taskArgs = @('-batchmode', '-projectPath', ('"' + $project + '"'), '-executeMethod', 'HollowSaint.PreviewValidation.CurrentFlowValidation.RunBatch', '-quit', '-logFile', ('"' + $log + '"'))
$taskProcess = Start-Process -FilePath 'C:\Program Files\Unity 2021.3.33f1\Editor\Unity.exe' -WindowStyle Hidden -PassThru -ArgumentList $taskArgs
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw "Native current flow failed ($($taskProcess.ExitCode)); see $log" }
$report = Get-Content (Join-Path $output 'verification.txt') -Raw
if (-not $report.StartsWith('ALL PASS')) { throw $report }
$report.TrimEnd()
