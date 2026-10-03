# Makes the locally staged Hollow Saint plugin visible in r2modman's "Hollow Saint Dev" profile.
# Writes manifest.json + icon.png into the plugin folder and adds/updates a local (non-online)
# entry in that profile's mods.yml. Backs up mods.yml first. Close r2modman before running.
# Touches ONLY the Hollow Saint Dev profile.
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$profileDir = Join-Path $env:APPDATA 'r2modmanPlus-local\RiskOfRain2\profiles\Hollow Saint Dev'
$pluginDir = Join-Path $profileDir 'BepInEx\plugins\JohnstonStu-HollowSaint'
$modsYml = Join-Path $profileDir 'mods.yml'
if (Get-Process r2modman -ErrorAction SilentlyContinue) { throw 'Close r2modman first; it rewrites mods.yml while open.' }

$manifestSrc = Join-Path $repo 'HollowSaintMod\Package\manifest.json'
$manifest = Get-Content $manifestSrc -Raw | ConvertFrom-Json
Copy-Item $manifestSrc (Join-Path $pluginDir 'manifest.json') -Force
$icon = Join-Path $repo 'HollowSaintMod\Package\icon.png'
if (Test-Path $icon) { Copy-Item $icon (Join-Path $pluginDir 'icon.png') -Force }

$v = $manifest.version_number.Split('.')
$stamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$entry = @"
- manifestVersion: 1
  name: JohnstonStu-HollowSaint
  authorName: JohnstonStu
  websiteUrl: ''
  displayName: HollowSaint
  description: '$($manifest.description.Replace("'", "''"))'
  gameVersion: '0'
  networkMode: both
  packageType: other
  installMode: managed
  installedAtTime: $stamp
  loaders: []
  dependencies: []
  incompatibilities: []
  optionalDependencies: []
  versionNumber:
    major: $($v[0])
    minor: $($v[1])
    patch: $($v[2])
  enabled: true
"@

$text = Get-Content $modsYml -Raw
Copy-Item $modsYml ($modsYml + '.bak-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
# Drop any previous HollowSaint entry (entries start with "- manifestVersion").
$blocks = [regex]::Split($text, '(?m)^(?=- manifestVersion)') | Where-Object { $_.Trim() -ne '' }
$kept = $blocks | Where-Object { $_ -notmatch '(?m)^  name: JohnstonStu-HollowSaint\s*$' }
$out = (($kept | ForEach-Object { $_.TrimEnd() }) -join "`n") + "`n" + $entry.TrimEnd() + "`n"
[IO.File]::WriteAllText($modsYml, $out.Replace("`r`n", "`n"))
"registered JohnstonStu-HollowSaint $($manifest.version_number) in Hollow Saint Dev (entries: $($kept.Count + 1))"
