# Creates only an empty, isolated r2modman profile. Never selects or launches it.
# Official r2modman sources checked 2026-09-26:
# src/r2mm/model_implementation/ProfileImpl.ts
# src/r2mm/mods/ProfileModList.ts
# src/store/modules/ProfilesModule.ts
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$profileRoot = Join-Path $env:APPDATA 'r2modmanPlus-local\RiskOfRain2\profiles'
$profilePath = Join-Path $profileRoot 'Hollow Saint Dev'

try {
    if (-not (Test-Path -LiteralPath $profileRoot -PathType Container)) {
        throw "Expected existing r2modman profile directory is missing: $profileRoot"
    }
    if (Test-Path -LiteralPath $profilePath) {
        throw "Profile already exists; refusing to overwrite anything: $profilePath"
    }

    # Snapshot only manifest hashes; no profile contents are copied.
    $existing = @(Get-ChildItem -LiteralPath $profileRoot -Directory | ForEach-Object {
        $manifest = Join-Path $_.FullName 'mods.yml'
        if (Test-Path -LiteralPath $manifest -PathType Leaf) {
            Get-FileHash -LiteralPath $manifest -Algorithm SHA256
        }
    })

    $null = New-Item -ItemType Directory -Path $profilePath
    $manifestPath = Join-Path $profilePath 'mods.yml'
    $stream = [System.IO.File]::Open($manifestPath, [System.IO.FileMode]::CreateNew)
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes("[]`n")
        $stream.Write($bytes, 0, $bytes.Length)
    } finally {
        $stream.Dispose()
    }

    foreach ($snapshot in $existing) {
        if ((Get-FileHash -LiteralPath $snapshot.Path -Algorithm SHA256).Hash -ne $snapshot.Hash) {
            throw "An existing profile manifest changed during setup: $($snapshot.Path)"
        }
    }
    if ((Get-Content -LiteralPath $manifestPath -Raw).Trim() -ne '[]') {
        throw 'New profile mod list did not verify as empty.'
    }
    Write-Output "Created empty profile: $profilePath"
    Write-Output "Verified $($existing.Count) existing profile manifest hashes unchanged."
    Write-Output 'No loader or mods installed; no profile selected; no game launched.'
} catch {
    Write-Error -Message "Failed to prepare Hollow Saint development profile: $($_.Exception.Message)" -ErrorAction Continue
    throw
}
