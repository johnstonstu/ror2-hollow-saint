$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (Get-Process 'Risk of Rain 2' -ErrorAction SilentlyContinue) { throw 'Close the game before updating the development profile.' }
$profiles = Join-Path $env:APPDATA 'r2modmanPlus-local/RiskOfRain2/profiles'
$source = Join-Path $profiles 'demo time new'
$target = Join-Path $profiles 'Hollow Saint Dev'
$sourceList = Join-Path $source 'mods.yml'
$targetList = Join-Path $target 'mods.yml'
$sourceHash = (Get-FileHash -LiteralPath $sourceList).Hash
$oldList = Get-Content -LiteralPath $targetList -Raw
$blocks = [regex]::Split((Get-Content -LiteralPath $sourceList -Raw), '(?m)(?=^- manifestVersion:)')
$selected = @($blocks | Where-Object {
    $match = [regex]::Match($_, '(?m)^  name: (.+)\r?$')
    $match.Success -and $match.Groups[1].Value.Trim() -ne 'JohnstonStu-AH64'
})
$names = @($selected | ForEach-Object { [regex]::Match($_, '(?m)^  name: (.+)\r?$').Groups[1].Value.Trim() })
if ($names.Count -ne 34) { throw 'Reference profile changed; review its package selection first.' }
$backup = Join-Path $workspace ('artifacts/foundation/profile-backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
Copy-Item -LiteralPath $targetList -Destination (Join-Path $backup 'mods.yml')
$verified = 0
foreach ($category in @('plugins', 'patchers')) {
    foreach ($name in $names) {
        $package = Join-Path $source "BepInEx/$category/$name"
        if (!(Test-Path -LiteralPath $package)) { continue }
        foreach ($file in Get-ChildItem -LiteralPath $package -Recurse -File) {
            $relative = $file.FullName.Substring($source.Length + 1)
            $destination = Join-Path $target $relative
            if (Test-Path -LiteralPath $destination) {
                if ((Get-FileHash -LiteralPath $destination).Hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash) {
                    throw "Existing support file differs; inspect before replacing: $relative"
                }
            } else {
                New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
                Copy-Item -LiteralPath $file.FullName -Destination $destination
            }
            if ((Get-FileHash -LiteralPath $destination).Hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash) { throw "Copy verification failed: $relative" }
            $verified++
        }
    }
}
$plugin = Join-Path $target 'BepInEx/plugins/JohnstonStu-HollowSaint/HollowSaint.dll'
Get-FileHash -LiteralPath $plugin | Out-Null
Copy-Item -LiteralPath $plugin -Destination (Join-Path $backup 'HollowSaint.dll')
$build = Join-Path $workspace 'HollowSaintMod/bin/Release/netstandard2.1/HollowSaint.dll'
Copy-Item -LiteralPath $build -Destination $plugin -Force
if ((Get-FileHash -LiteralPath $build).Hash -ne (Get-FileHash -LiteralPath $plugin).Hash) { throw 'Plugin verification failed' }
Set-Content -LiteralPath $targetList -Value ($selected -join '')
if ((Get-FileHash -LiteralPath $sourceList).Hash -ne $sourceHash) { throw 'Reference manifest changed during sync' }
$report = @('Support packages: 34 (demo time new excluding AH64)', "Verified support files: $verified", "Backup: $backup", "Plugin SHA256: $((Get-FileHash -LiteralPath $plugin).Hash)", 'Game was not launched. Runtime validation pending.')
$report | Set-Content -LiteralPath (Join-Path $workspace 'artifacts/foundation/profile-support-sync.txt')
$report
