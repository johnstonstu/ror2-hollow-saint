# Dev-only scripted playtest: launches the Hollow Saint Dev profile with HS_AUTOPILOT set,
# which hosts a solo run, plays a fixed skill script, saves screenshots + trace, then quits.
# Usage: powershell -ExecutionPolicy Bypass -File tools\dev-profile\Run-Autopilot.ps1 -Name autopilot01
param([Parameter(Mandatory=$true)][string]$Name, [int]$TimeoutSeconds = 420, [string]$ProfileName = 'Hollow Saint Dev')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$out = Join-Path $repo ("artifacts\" + $Name)
New-Item -ItemType Directory -Force $out | Out-Null
$game = 'C:/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2'
$profile = Join-Path $env:APPDATA ('r2modmanPlus-local/RiskOfRain2/profiles/' + $ProfileName)
$preloader = Join-Path $profile 'BepInEx/core/BepInEx.Preloader.dll'
if (Get-Process -Name 'Risk of Rain 2' -ErrorAction SilentlyContinue) { throw 'A game is already running; close it first' }
$env:HS_AUTOPILOT = $out
$arguments = '--doorstop-enabled true --doorstop-target-assembly "' + $preloader + '" --r2profile "' + $ProfileName + '"'
$p = Start-Process -FilePath (Join-Path $game 'Risk of Rain 2.exe') -WorkingDirectory $game -ArgumentList $arguments -PassThru
Remove-Item Env:HS_AUTOPILOT
if (-not $p.WaitForExit($TimeoutSeconds * 1000)) { Stop-Process -Id $p.Id -Force; "TIMEOUT: game stopped after $TimeoutSeconds s" }
$log = Join-Path $profile 'BepInEx/LogOutput.log'
if (Test-Path $log) { Copy-Item $log (Join-Path $out 'LogOutput.log') -Force }
"exit=" + $p.ExitCode
if (Test-Path (Join-Path $out 'trace.txt')) { Get-Content (Join-Path $out 'trace.txt') -TotalCount 1 } else { 'no trace written' }
Get-ChildItem $out -Filter *.png | Measure-Object | % { "screenshots=" + $_.Count }
