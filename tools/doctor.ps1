# Read-only prerequisite inventory. Missing optional/native inputs are not PASS.
param([string]$Managed, [string]$ProfileBepInEx)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not $Managed) { $Managed = 'C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed' }
if (-not $ProfileBepInEx) { $ProfileBepInEx = Join-Path $env:APPDATA 'r2modmanPlus-local\RiskOfRain2\profiles\Hollow Saint Dev\BepInEx' }
$rows = @()
foreach ($name in @('dotnet', 'python', 'powershell')) {
    $command = Get-Command $name -ErrorAction SilentlyContinue
    $rows += [pscustomobject]@{ Scope = 'Quick'; Input = $name; Available = [bool]$command; Location = $command.Source }
}
$paths = @(
    @('Build', 'Risk of Options reference', (Join-Path $repo 'HollowSaintMod\lib\RiskOfOptions.dll')),
    @('Build', 'Soundbank (Git LFS)', (Join-Path $repo 'art\audio\HollowSaintAudio\GeneratedSoundBanks\Windows\HollowSaint.bnk')),
    @('Native', 'Installed game assembly', (Join-Path $Managed 'RoR2.dll')),
    @('Native', 'BepInEx/profile references', $ProfileBepInEx),
    @('Package', 'Pinned bundle15', (Join-Path $repo 'artifacts\foundation\bundle15\hollowsaintassets'))
)
foreach ($row in $paths) {
    $available = Test-Path -LiteralPath $row[2]
    if ($available -and (Test-Path -LiteralPath $row[2] -PathType Leaf)) {
        $stream = [IO.File]::OpenRead($row[2])
        try {
            $prefix = New-Object byte[] 64
            $count = $stream.Read($prefix, 0, $prefix.Length)
            if ([Text.Encoding]::ASCII.GetString($prefix, 0, $count).StartsWith('version https://git-lfs.github.com/spec/')) { $available = $false }
        } finally { $stream.Dispose() }
    }
    $rows += [pscustomobject]@{ Scope = $row[0]; Input = $row[1]; Available = $available; Location = $row[2] }
}
$rows | Format-Table -AutoSize
& dotnet --version
if ($LASTEXITCODE -ne 0) { throw 'The SDK selected by global.json is unavailable.' }
'Native checks read installed assemblies; they do not validate gameplay or launch the game.'
'Legacy Unity previews: UNAVAILABLE until fixtures are ported to the 1.2 pose API.'
'Alternate RiskOfOptions.dll paths are supported by the project RiskOfOptionsDll MSBuild property.'
