# Keys in HollowSaint.language match English, placeholders and style/color tags match,
# JSON parses, Russian plural blocks have one/few/many, and the English templates still
# produce the sentences the mod used to build in code.
# Usage: powershell -ExecutionPolicy Bypass -File tools\tests\Check-Language.ps1
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
# Compiled as separate files: each file has its own using directives, which can't follow
# code from another file in one concatenated source.
Add-Type -Path @(
    (Join-Path $repo 'HollowSaintMod\Localization\LanguageJson.cs'),
    (Join-Path $repo 'HollowSaintMod\Localization\LangFormat.cs'),
    (Join-Path $repo 'tools\tests\LanguageChecks.cs')
)
[HollowSaint.FoundationKit.LanguageChecks]::Run((Join-Path $repo 'HollowSaintMod\Language\HollowSaint.language'))
