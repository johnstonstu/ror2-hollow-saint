# Builds the Thunderstore package zip for Hollow Saint from the Release DLL and the tested asset bundle.
# Usage: powershell -ExecutionPolicy Bypass -File tools\release\Make-Package.ps1 [-Bundle <path to hollowsaintassets>]
# Default bundle: the one staged in the "Hollow Saint Dev" profile (the build Stu playtested).
# Output: artifacts\release\JohnstonStu-HollowSaint-<version>.zip (+ an unpacked folder for inspection).
param([string]$Bundle)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$pkg = Join-Path $repo 'HollowSaintMod\Package'
$manifest = Get-Content (Join-Path $pkg 'manifest.json') -Raw | ConvertFrom-Json
$version = $manifest.version_number
$dll = Join-Path $repo 'HollowSaintMod\bin\Release\netstandard2.1\HollowSaint.dll'
if (-not $Bundle) { $Bundle = Join-Path $env:APPDATA 'r2modmanPlus-local\RiskOfRain2\profiles\Hollow Saint Dev\BepInEx\plugins\JohnstonStu-HollowSaint\hollowsaintassets' }
foreach ($f in @($dll, $Bundle, (Join-Path $pkg 'icon.png'), (Join-Path $pkg 'README.md'), (Join-Path $pkg 'CHANGELOG.md'))) { if (-not (Test-Path $f)) { throw "Missing $f" } }
$plugin = (Get-Content (Join-Path $repo 'HollowSaintMod\Plugin.cs') -Raw)
if ($plugin -notmatch ('Version = "' + [regex]::Escape($version) + '"')) { throw "Plugin.cs Version does not match manifest $version" }

$out = Join-Path $repo 'artifacts\release'
$stage = Join-Path $out ("JohnstonStu-HollowSaint-" + $version)
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force (Join-Path $stage 'plugins\HollowSaint') | Out-Null
Copy-Item (Join-Path $pkg 'manifest.json'), (Join-Path $pkg 'README.md'), (Join-Path $pkg 'CHANGELOG.md'), (Join-Path $pkg 'icon.png') $stage
Copy-Item $dll (Join-Path $stage 'plugins\HollowSaint')
Copy-Item $Bundle (Join-Path $stage 'plugins\HollowSaint\hollowsaintassets')
$zip = $stage + '.zip'
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
Get-ChildItem $stage -Recurse -File | ForEach-Object { $_.FullName.Substring($stage.Length + 1) + "  " + $_.Length }
"zip=" + $zip + " bytes=" + (Get-Item $zip).Length
