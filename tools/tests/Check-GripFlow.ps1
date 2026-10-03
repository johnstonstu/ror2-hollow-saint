# Success: all 240 baked built-bundle poses have zero hand/grip triangle
# intersections, and sampled grip vertices stay within the fitted clearance.
param([ValidateSet('11','12','13')][string]$Bundle = '13')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
& (Join-Path $PSScriptRoot 'Prepare-FxValidation.ps1')
$unity = 'C:\Program Files\Unity 2021.3.33f1\Editor\Unity.exe'
$blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$folder = if ($Bundle -eq '13') { 'grip-flow03' } elseif ($Bundle -eq '12') { 'grip-flow02' } else { 'grip-flow01' }
$output = Join-Path $repo "artifacts\$folder"
New-Item -ItemType Directory -Force $output | Out-Null
$project = Join-Path $repo 'HollowSaintUnityProject'
$log = Join-Path $output 'unity-export.log'
$taskArgs = @('-batchmode', '-projectPath', ('"' + $project + '"'), '-executeMethod', 'HollowSaint.PreviewValidation.GripFlowValidation.RunBatch', '-quit', '-logFile', ('"' + $log + '"'))
$oldBundle = $env:HS_GRIP_BUNDLE
$env:HS_GRIP_BUNDLE = $Bundle
try { $process = Start-Process -FilePath $unity -WindowStyle Hidden -PassThru -ArgumentList $taskArgs }
finally { $env:HS_GRIP_BUNDLE = $oldBundle }
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity grip geometry export failed ($($process.ExitCode)); see $log" }
$export = Get-Content (Join-Path $output 'export.txt') -Raw
if (-not $export.StartsWith('EXPORTED')) { throw $export }
$checker = Join-Path $repo 'tools\blender\spear\check_grip_flow.py'
& $blender --background --factory-startup --python-exit-code 1 --python $checker -- $folder
if ($LASTEXITCODE -ne 0) { throw "Baked grip surface check failed; see artifacts/$folder/surface-check.json" }
Get-Content (Join-Path $output 'verification.txt')
