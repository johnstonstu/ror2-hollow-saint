$ErrorActionPreference = 'Stop'
$game = 'C:/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2'
$preloader = Join-Path $env:APPDATA 'r2modmanPlus-local/RiskOfRain2/profiles/Hollow Saint Dev/BepInEx/core/BepInEx.Preloader.dll'
if (!(Test-Path -LiteralPath $preloader)) { throw 'Install the isolated foundation profile first' }
if (Get-Process -Name 'Risk of Rain 2' -ErrorAction SilentlyContinue) { throw 'A game is already running; close it normally first' }
# Visible interactive game for controller review; no global Steam/loader settings changed.
$arguments = '--doorstop-enabled true --doorstop-target-assembly "' + $preloader + '" --r2profile "Hollow Saint Dev"'
Start-Process -FilePath (Join-Path $game 'Risk of Rain 2.exe') -WorkingDirectory $game -ArgumentList $arguments -PassThru
