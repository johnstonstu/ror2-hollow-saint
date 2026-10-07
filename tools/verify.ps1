# Offline by default. Never launches Unity, stages a profile, or publishes.
param(
    [ValidateSet('Quick', 'Build', 'Native')][string]$Scope = 'Quick',
    [ValidateRange(1, 8)][int]$Jobs = 3,
    [string]$Managed,
    [string]$ProfileBepInEx
)
$ErrorActionPreference = 'Stop'
$arguments = @((Join-Path $PSScriptRoot 'verification\run.py'), '--scope', $Scope, '--jobs', $Jobs)
if ($Managed) { $arguments += @('--managed', $Managed) }
if ($ProfileBepInEx) { $arguments += @('--profile-bepinex', $ProfileBepInEx) }
& python @arguments
exit $LASTEXITCODE
