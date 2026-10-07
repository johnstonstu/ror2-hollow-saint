# Success: native finger handoffs remain bounded at 30/60/144Hz, restore authored
# transforms, retain correct casting fades and never reverse their curl hinge.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
& (Join-Path $PSScriptRoot 'Prepare-FxValidation.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Preview preparation failed or is unavailable; Unity was not launched.' }
$output = Join-Path $repo 'artifacts\finger-flow01'
New-Item -ItemType Directory -Force $output | Out-Null
$project = Join-Path $repo 'HollowSaintUnityProject'
$log = Join-Path $output 'unity.log'
$taskArgs = @('-batchmode', '-projectPath', ('"' + $project + '"'), '-executeMethod', 'HollowSaint.PreviewValidation.FingerFlowValidation.RunBatch', '-quit', '-logFile', ('"' + $log + '"'))
$taskProcess = Start-Process -FilePath 'C:\Program Files\Unity 2021.3.33f1\Editor\Unity.exe' -WindowStyle Hidden -PassThru -ArgumentList $taskArgs
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw "Native finger audit failed ($($taskProcess.ExitCode)); see $log" }
$report = Get-Content (Join-Path $output 'verification.txt') -Raw
if (-not $report.StartsWith('ALL PASS')) { throw $report }
$report.TrimEnd()
