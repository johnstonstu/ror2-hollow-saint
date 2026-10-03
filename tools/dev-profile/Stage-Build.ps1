# Builds HollowSaint.dll and stages it (and optionally an asset bundle) into the
# "Hollow Saint Dev" r2modman profile. Backs up whatever it replaces first.
# Refuses while the game is running.
# Usage:
#   powershell -ExecutionPolicy Bypass -File tools\dev-profile\Stage-Build.ps1 [-SkipBuild] [-Bundle artifacts\foundation\bundle02\hollowsaintassets]
# Roll back: copy HollowSaint.dll / hollowsaintassets from the printed backup folder into the plugin folder.
param([switch]$SkipBuild, [string]$Bundle)
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$profileDir = Join-Path $env:APPDATA 'r2modmanPlus-local\RiskOfRain2\profiles\Hollow Saint Dev'
$pluginDir = Join-Path $profileDir 'BepInEx\plugins\JohnstonStu-HollowSaint'
$built = Join-Path $repo 'HollowSaintMod\bin\Release\netstandard2.1\HollowSaint.dll'

if (Get-Process 'Risk of Rain 2' -ErrorAction SilentlyContinue) { throw 'Risk of Rain 2 is running. Close it before staging.' }
if (-not (Test-Path $pluginDir)) { throw "Plugin folder not found: $pluginDir" }

if (-not $SkipBuild) {
    & dotnet build (Join-Path $repo 'HollowSaintMod\HollowSaint.csproj') -c Release --no-restore -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Build failed; nothing staged.' }
}

& powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Check-Access.ps1') -Dll $built
if ($LASTEXITCODE -ne 0) { throw 'Access check failed (private game member used via publicized reference); nothing staged.' }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupDir = Join-Path $repo "artifacts\foundation\profile-backup-$stamp"
New-Item -ItemType Directory -Force $backupDir | Out-Null
$lines = @()

$target = Join-Path $pluginDir 'HollowSaint.dll'
if (Test-Path $target) { Copy-Item $target $backupDir; $lines += "previousDll=" + (Get-FileHash $target).Hash }
Copy-Item $built $target -Force
$lines += "stagedDll=" + (Get-FileHash $target).Hash

$langSource = Join-Path $repo 'HollowSaintMod\Language\HollowSaint.language'
if (-not (Test-Path -LiteralPath $langSource)) { throw "Missing $langSource" }
$langTarget = Join-Path $pluginDir 'HollowSaint.language'
if (Test-Path -LiteralPath $langTarget) { Copy-Item -LiteralPath $langTarget $backupDir; $lines += "previousLanguage=" + (Get-FileHash -LiteralPath $langTarget).Hash }
Copy-Item -LiteralPath $langSource -Destination $langTarget -Force
$lines += "stagedLanguage=" + (Get-FileHash -LiteralPath $langTarget).Hash

if ($Bundle) {
    $bundleSource = Resolve-Path (Join-Path $repo $Bundle)
    $bundleTarget = Join-Path $pluginDir 'hollowsaintassets'
    if (Test-Path $bundleTarget) { Copy-Item $bundleTarget $backupDir; $lines += "previousBundle=" + (Get-FileHash $bundleTarget).Hash }
    Copy-Item $bundleSource $bundleTarget -Force
    $lines += "stagedBundle=" + (Get-FileHash $bundleTarget).Hash + " from " + $Bundle
}
$lines += "backup=$backupDir"
$lines | Tee-Object (Join-Path $backupDir 'stage.txt')
