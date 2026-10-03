param([switch]$AuditOnly)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
& (Join-Path $PSScriptRoot 'Prepare-FxValidation.ps1')
$output = Join-Path $repo 'artifacts\skin-light01'
New-Item -ItemType Directory -Force $output | Out-Null
$project = Join-Path $repo 'HollowSaintUnityProject'
$log = Join-Path $output 'unity.log'
$method = if ($AuditOnly) { 'AuditBatch' } else { 'RunBatch' }
$taskArgs = @('-batchmode', '-projectPath', ('"' + $project + '"'), '-executeMethod', "HollowSaint.PreviewValidation.SkinLightValidation.$method", '-quit', '-logFile', ('"' + $log + '"'))
$taskProcess = Start-Process -FilePath 'C:\Program Files\Unity 2021.3.33f1\Editor\Unity.exe' -WindowStyle Hidden -PassThru -ArgumentList $taskArgs
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw "Native skin-light check failed ($($taskProcess.ExitCode)); see $log" }
if ($AuditOnly) { Get-Content (Join-Path $output 'emissive-materials.tsv'); return }
$report = Get-Content (Join-Path $output 'verification.txt') -Raw
if (-not $report.StartsWith('ALL PASS')) { throw $report }
$report.TrimEnd()
