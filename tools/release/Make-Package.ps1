# Builds the Thunderstore package zip for Hollow Saint: Release build, validation, then a spec-conformant zip.
# Usage: powershell -ExecutionPolicy Bypass -File tools\release\Make-Package.ps1 [-Bundle <path to hollowsaintassets>] [-SkipBuild]
# Default bundle: artifacts\foundation\bundle15\hollowsaintassets (the bundle Stu playtested; SHA256 pinned below).
# Passing -Bundle skips the hash pin, so only do that on purpose for a new bundle (then update $BundleSha256).
# Output: artifacts\release\JohnstonStu-Hollow_Saint-<version>.zip (+ an unpacked folder for inspection).
# Nothing is uploaded. Publish to Thunderstore by hand.
param([string]$Bundle, [switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
function Fail([string]$message) { throw "Make-Package: $message" }

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$pkg = Join-Path $repo 'HollowSaintMod\Package'
$csproj = Join-Path $repo 'HollowSaintMod\HollowSaint.csproj'
$dll = Join-Path $repo 'HollowSaintMod\bin\Release\netstandard2.1\HollowSaint.dll'
$license = Join-Path $repo 'LICENSE'
$icon = Join-Path $pkg 'icon.png'
$BundleSha256 = '7238F181B17B011A0BCC0BF13AB8C7D951CAA7930A24DB4F5B83F278BEA111B4'
$pinned = Join-Path $repo 'artifacts\foundation\bundle15\hollowsaintassets'

# --- 1. inputs and version ---------------------------------------------------
$manifest = Get-Content (Join-Path $pkg 'manifest.json') -Raw | ConvertFrom-Json
$version = $manifest.version_number
if ($manifest.name -ne 'Hollow_Saint') { Fail "manifest name is '$($manifest.name)', expected Hollow_Saint" }
if ($version -notmatch '^\d+\.\d+\.\d+$') { Fail "manifest version_number '$version' is not major.minor.patch" }
if ($manifest.description.Length -gt 250) { Fail "manifest description is $($manifest.description.Length) chars (max 250)" }
$plugin = Get-Content (Join-Path $repo 'HollowSaintMod\Plugin.cs') -Raw
if ($plugin -notmatch 'const string Version = "([^"]+)"') { Fail 'Plugin.cs Version constant not found' }
if ($Matches[1] -ne $version) { Fail "Plugin.cs Version $($Matches[1]) does not match manifest $version (NetworkCompatibility rejects mismatched lobbies)" }

if ($Bundle) { Write-Warning "Using -Bundle $Bundle (hash pin skipped)" }
else {
    $Bundle = $pinned
    if (-not (Test-Path -LiteralPath $Bundle -PathType Leaf)) { Fail "Missing pinned bundle $Bundle" }
    $hash = (Get-FileHash -LiteralPath $Bundle -Algorithm SHA256).Hash
    if ($hash -ne $BundleSha256) { Fail "Pinned bundle hash $hash does not match $BundleSha256" }
}
foreach ($f in @($Bundle, $icon, (Join-Path $pkg 'README.md'), (Join-Path $pkg 'CHANGELOG.md'), $license)) {
    if (-not (Test-Path -LiteralPath $f -PathType Leaf)) { Fail "Missing $f" }
}
if ((Get-Content (Join-Path $pkg 'CHANGELOG.md') -Raw) -notmatch ('(?m)^## ' + [regex]::Escape($version) + '\b')) { Fail "CHANGELOG.md has no '## $version' entry" }

Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile($icon)
try { if ($img.Width -ne 256 -or $img.Height -ne 256) { Fail "icon.png is $($img.Width)x$($img.Height), Thunderstore needs exactly 256x256" } }
finally { $img.Dispose() }

# --- 2. Release build --------------------------------------------------------
if (-not $SkipBuild) {
    & dotnet build $csproj -c Release -nologo -v q --no-incremental
    if ($LASTEXITCODE -ne 0) { Fail "dotnet build -c Release failed (exit $LASTEXITCODE)" }
}
if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) { Fail "Missing $dll" }

# --- 3. stage (allowlist only) -----------------------------------------------
$out = Join-Path $repo 'artifacts\release'
$stage = Join-Path $out ("JohnstonStu-Hollow_Saint-" + $version)
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
$files = [ordered]@{
    'manifest.json' = (Join-Path $pkg 'manifest.json')
    'README.md' = (Join-Path $pkg 'README.md')
    'CHANGELOG.md' = (Join-Path $pkg 'CHANGELOG.md')
    'icon.png' = $icon
    'LICENSE' = $license
    'plugins/HollowSaint/HollowSaint.dll' = $dll
    'plugins/HollowSaint/hollowsaintassets' = $Bundle
}
foreach ($e in $files.GetEnumerator()) {
    $target = Join-Path $stage ($e.Key.Replace('/', '\'))
    New-Item -ItemType Directory -Force (Split-Path -Parent $target) | Out-Null
    Copy-Item -LiteralPath $e.Value -Destination $target
}

# --- 4. zip ------------------------------------------------------------------
# Not Compress-Archive / ZipFile.CreateFromDirectory: on Windows PowerShell (.NET Framework) both write
# backslash separators, which breaks extraction on Linux / Steam Deck (the bundle lands as a flat file
# named "plugins\HollowSaint\hollowsaintassets" and the mod cannot load it). Entries are added one by one
# with forward-slash names, at the zip root (no wrapping folder).
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = $stage + '.zip'
if (Test-Path $zip) { Remove-Item $zip -Force }
$archive = [System.IO.Compression.ZipFile]::Open($zip, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($e in $files.GetEnumerator()) {
        $src = Join-Path $stage ($e.Key.Replace('/', '\'))
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $src, $e.Key, [System.IO.Compression.CompressionLevel]::Optimal)
    }
} finally { $archive.Dispose() }

# --- 5. verify the produced zip ----------------------------------------------
$archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
try { $entries = @($archive.Entries | ForEach-Object { $_.FullName }) } finally { $archive.Dispose() }
if (@($entries | Where-Object { $_.Contains([string][char]0x5C) }).Count -gt 0) { Fail "zip contains backslash path separators: $($entries -join ', ')" }
foreach ($k in $files.Keys) { if ($entries -notcontains $k) { Fail "zip is missing $k" } }
if ($entries.Count -ne $files.Count) { Fail "zip has unexpected entries: $($entries -join ', ')" }

# No local user paths in shipped files (e.g. an unmapped PDB path inside the DLL).
foreach ($k in $files.Keys) {
    $bytes = [IO.File]::ReadAllBytes((Join-Path $stage $k.Replace('/', '\')))
    foreach ($text in @([Text.Encoding]::ASCII.GetString($bytes), [Text.Encoding]::Unicode.GetString($bytes))) {
        if ($text -match '[A-Za-z]:\\Users\\') { Fail "$k contains a local user path ($($Matches[0])...)" }
    }
}
$entries | ForEach-Object { $_ + "  " + (Get-Item -LiteralPath (Join-Path $stage $_.Replace('/', '\'))).Length }
"dll sha256=" + (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
"zip=" + $zip + " bytes=" + (Get-Item $zip).Length
"Not uploaded. Publish to Thunderstore by hand."
