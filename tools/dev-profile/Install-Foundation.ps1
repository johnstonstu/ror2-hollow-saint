$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$profiles = Join-Path $env:APPDATA 'r2modmanPlus-local/RiskOfRain2/profiles'
$sourceProfile = Join-Path $profiles 'demo time new'
$targetProfile = Join-Path $profiles 'Hollow Saint Dev'
$targetBep = Join-Path $targetProfile 'BepInEx'
if (!(Test-Path $targetProfile)) { throw 'Expected existing isolated profile is absent' }
if (Test-Path $targetBep) { throw 'Profile already provisioned; inspect it before updating' }
$targetList = Join-Path $targetProfile 'mods.yml'
if ((Get-Content -LiteralPath $targetList -Raw).Trim() -ne '[]') { throw 'Expected empty development profile' }
$names = @('bbepis-BepInExPack', 'RiskofThunder-RoR2BepInExPack',
    'RiskofThunder-HookGenPatcher', 'RiskofThunder-FixPluginTypesSerialization',
    'RiskofThunder-R2API_Core', 'RiskofThunder-R2API_ContentManagement',
    'RiskofThunder-R2API_Prefab', 'RiskofThunder-R2API_Language')
$sourceList = Join-Path $sourceProfile 'mods.yml'
$sourceHash = (Get-FileHash -LiteralPath $sourceList).Hash
$blocks = [regex]::Split((Get-Content -LiteralPath $sourceList -Raw), '(?m)(?=^- manifestVersion:)')
$selected = @($blocks | Where-Object {
    $match = [regex]::Match($_, '(?m)^  name: (.+)\r?$')
    $match.Success -and ($match.Groups[1].Value.Trim() -in $names)
})
if ($selected.Count -ne $names.Count) { throw 'Dependency list incomplete' }
$dll = Join-Path $workspace 'HollowSaintMod/bin/Release/netstandard2.1/HollowSaint.dll'
$bundle = Join-Path $workspace 'artifacts/foundation/bundle01/hollowsaintassets'
foreach ($file in @($dll, $bundle)) { if (!(Test-Path $file)) { throw "Missing build: $file" } }
New-Item -ItemType Directory -Path $targetBep | Out-Null
foreach ($loaderFile in @('.doorstop_version', 'doorstop_config.ini', 'winhttp.dll')) {
    $source = Join-Path $sourceProfile $loaderFile
    if (!(Test-Path -LiteralPath $source)) { throw "Missing reference loader file: $loaderFile" }
    Copy-Item -LiteralPath $source -Destination (Join-Path $targetProfile $loaderFile)
}
Copy-Item -LiteralPath (Join-Path $sourceProfile 'BepInEx/core') -Destination (Join-Path $targetBep 'core') -Recurse
foreach ($category in @('plugins', 'patchers')) {
    $destination = Join-Path $targetBep $category
    New-Item -ItemType Directory -Path $destination | Out-Null
    foreach ($name in $names) {
        $source = Join-Path $sourceProfile "BepInEx/$category/$name"
        if (Test-Path $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $destination $name) -Recurse }
    }
}
$plugin = Join-Path $targetBep 'plugins/JohnstonStu-HollowSaint'
New-Item -ItemType Directory -Path $plugin | Out-Null
Copy-Item -LiteralPath $dll -Destination $plugin
Copy-Item -LiteralPath $bundle -Destination $plugin
Copy-Item -LiteralPath $targetList -Destination (Join-Path $targetProfile 'mods.before-foundation.yml')
Set-Content -LiteralPath $targetList -Value ($selected -join '')
if ((Get-FileHash -LiteralPath $sourceList).Hash -ne $sourceHash) { throw 'Reference profile list changed during provisioning' }
$report = @("Target: $targetProfile", 'Reference profile manifest unchanged.', 'Manual local plugin: JohnstonStu-HollowSaint', 'Dependencies:') + $names
$report | Set-Content -LiteralPath (Join-Path $workspace 'artifacts/foundation/profile-install.txt')
Write-Output "Installed foundation only into $targetProfile"
