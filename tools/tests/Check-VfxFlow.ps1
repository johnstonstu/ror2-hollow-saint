# Success: native Unity VFX colors stay isolated, moving endpoints stay pinned,
# poses actually animate, idle/death/invisibility clean up, the fan tail lasts at
# most 90 ms, and source-color snapshots expire independently of gameplay buffs.
# Carry checks additionally require exact throw release, bounded aim recovery,
# rapid restart continuity and unchanged fitted socket/finger transforms.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
& (Join-Path $PSScriptRoot 'Prepare-FxValidation.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Preview preparation failed or is unavailable; Unity was not launched.' }
$unity = 'C:\Program Files\Unity 2021.3.33f1\Editor\Unity.exe'
if (-not (Test-Path -LiteralPath $unity)) { throw 'Unity 2021.3.33f1 not found' }
$project = Join-Path $repo 'HollowSaintUnityProject'
$log = Join-Path $repo 'artifacts\foundation\vfx-flow-check-unity.log'
$taskArgs = @('-batchmode', '-projectPath', ('"' + $project + '"'), '-executeMethod', 'HollowSaint.PreviewValidation.FxValidationRunner.RunBatch', '-quit', '-logFile', ('"' + $log + '"'))
$fxProcess = Start-Process -FilePath $unity -WindowStyle Hidden -PassThru -ArgumentList $taskArgs
$fxProcess.WaitForExit()
if ($fxProcess.ExitCode -ne 0) { throw "Unity VFX check failed ($($fxProcess.ExitCode)); see $log" }
$report = Get-Content (Join-Path $repo 'artifacts\vfx-flow01\verification.txt') -Raw
if (-not $report.StartsWith('ALL PASS')) { throw $report }
$report.TrimEnd()
$carryReport = Get-Content (Join-Path $repo 'artifacts\spear-flow01\verification.txt') -Raw
if (-not $carryReport.StartsWith('ALL PASS')) { throw $carryReport }
$carryReport.TrimEnd()
$motionReport = Get-Content (Join-Path $repo 'artifacts\motion-flow01\verification.txt') -Raw
if (-not $motionReport.StartsWith('ALL PASS')) { throw $motionReport }
$motionReport.TrimEnd()
$armReport = Get-Content (Join-Path $repo 'artifacts\arm-flow01\verification.txt') -Raw
if (-not $armReport.StartsWith('ALL PASS')) { throw $armReport }
$armReport.TrimEnd()
