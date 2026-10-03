# Builds a throwaway r2modman profile, "Hollow Saint Clean", that holds ONLY what the Thunderstore
# manifest declares (BepInExPack, HookGenPatcher, RoR2BepInExPack, R2API Core / ContentManagement /
# Prefab / Language, Risk Of Options) plus the packaged mod, with no config. It is the closest local
# stand-in for a fresh install: a missing dependency or a default-config problem shows up here, not in
# the dev profile. -NoRiskOfOptions leaves it out to test the soft-dependency path (manual installs).
# Copies from the "Hollow Saint Dev" profile.
# Usage: powershell -ExecutionPolicy Bypass -File tools\release\New-CleanProfile.ps1 -Package artifacts\release\JohnstonStu-HollowSaint-<v>
param([Parameter(Mandatory=$true)][string]$Package, [switch]$NoRiskOfOptions)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$profiles = Join-Path $env:APPDATA 'r2modmanPlus-local\RiskOfRain2\profiles'
$dev = Join-Path $profiles 'Hollow Saint Dev'
$clean = Join-Path $profiles 'Hollow Saint Clean'
if (Get-Process 'Risk of Rain 2' -ErrorAction SilentlyContinue) { throw 'Close Risk of Rain 2 first.' }
if (Test-Path $clean) { Remove-Item $clean -Recurse -Force }
New-Item -ItemType Directory -Force (Join-Path $clean 'BepInEx\plugins'), (Join-Path $clean 'BepInEx\patchers') | Out-Null
foreach ($f in 'winhttp.dll', 'doorstop_config.ini', '.doorstop_version') { Copy-Item (Join-Path $dev $f) $clean }
Copy-Item (Join-Path $dev 'BepInEx\core') (Join-Path $clean 'BepInEx\core') -Recurse
Copy-Item (Join-Path $dev 'BepInEx\patchers\RiskofThunder-HookGenPatcher') (Join-Path $clean 'BepInEx\patchers') -Recurse
$deps = @('RiskofThunder-RoR2BepInExPack', 'RiskofThunder-HookGenPatcher', 'RiskofThunder-R2API_Core', 'RiskofThunder-R2API_ContentManagement', 'RiskofThunder-R2API_Prefab', 'RiskofThunder-R2API_Language')
if (-not $NoRiskOfOptions) { $deps += 'Rune580-Risk_Of_Options' }
foreach ($p in $deps) {
    $src = Join-Path $dev ('BepInEx\plugins\' + $p)
    if (Test-Path $src) { Copy-Item $src (Join-Path $clean 'BepInEx\plugins') -Recurse }
}
$pkgDir = (Resolve-Path (Join-Path $repo $Package)).Path
$dest = Join-Path $clean 'BepInEx\plugins\JohnstonStu-HollowSaint'
New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item (Join-Path $pkgDir 'plugins\HollowSaint\*') $dest
Copy-Item (Join-Path $pkgDir 'manifest.json'), (Join-Path $pkgDir 'icon.png') $dest
Get-ChildItem (Join-Path $clean 'BepInEx\plugins') | ForEach-Object { "plugin: " + $_.Name }
"clean profile ready: " + $clean
