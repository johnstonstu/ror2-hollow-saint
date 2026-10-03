# Keys in HollowSaint.language match English, placeholders and style/color tags match,
# JSON parses, Russian plural blocks have one/few/many, and the English templates still
# produce the sentences the mod used to build in code.
# Usage: powershell -ExecutionPolicy Bypass -File tools\tests\Check-Language.ps1
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$src = @()
foreach ($name in @('LanguageJson.cs', 'LangFormat.cs')) {
    $src += [IO.File]::ReadAllText((Join-Path $repo "HollowSaintMod\FoundationKit\$name"))
}
$src += [IO.File]::ReadAllText((Join-Path $repo 'tools\tests\LanguageChecks.cs'))
Add-Type -TypeDefinition ($src -join "`n") -Language CSharp
[HollowSaint.FoundationKit.LanguageChecks]::Run((Join-Path $repo 'HollowSaintMod\Language\HollowSaint.language'))
