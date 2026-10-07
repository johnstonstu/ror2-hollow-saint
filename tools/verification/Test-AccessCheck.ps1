# Success: a public native field passes, a private one fails, and an unresolved
# member is unavailable. Synthetic assemblies only; no game/profile modifications.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Add-Type -Path (Join-Path $env:USERPROFILE '.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll')
$scratch = Join-Path $repo ('artifacts\verification\access-fixture-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratch | Out-Null
$gamePath = Join-Path $scratch 'RoR2.dll'
$probePath = Join-Path $scratch 'Probe.dll'
$game = [Mono.Cecil.ModuleDefinition]::CreateModule('RoR2', [Mono.Cecil.ModuleKind]::Dll)
$target = [Mono.Cecil.TypeDefinition]::new('Fixture', 'Native', [Mono.Cecil.TypeAttributes]::Public, $game.TypeSystem.Object)
$game.Types.Add($target)
$field = [Mono.Cecil.FieldDefinition]::new('Value', ([Mono.Cecil.FieldAttributes]::Public -bor [Mono.Cecil.FieldAttributes]::Static), $game.TypeSystem.Int32)
$target.Fields.Add($field)
$probe = [Mono.Cecil.ModuleDefinition]::CreateModule('Probe', [Mono.Cecil.ModuleKind]::Dll)
$caller = [Mono.Cecil.TypeDefinition]::new('Fixture', 'Caller', [Mono.Cecil.TypeAttributes]::Public, $probe.TypeSystem.Object)
$probe.Types.Add($caller)
$method = [Mono.Cecil.MethodDefinition]::new('Read', ([Mono.Cecil.MethodAttributes]::Public -bor [Mono.Cecil.MethodAttributes]::Static), $probe.TypeSystem.Void)
$caller.Methods.Add($method)
$il = $method.Body.GetILProcessor()
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld, $probe.ImportReference($field))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Pop)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$probe.Write($probePath)
$probe.Dispose()
function Assert-Scan([int]$expected, [string]$reason) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo 'tools\dev-profile\Check-Access.ps1') -Dll $probePath -Managed $scratch -ProfileBepInEx $scratch
    if ($LASTEXITCODE -ne $expected) { throw "$reason expected exit $expected, got $LASTEXITCODE" }
}
try {
    $game.Write($gamePath)
    Assert-Scan 0 'public native field'
    [void][IO.File]::ReadAllBytes($gamePath)
    $field.Attributes = [Mono.Cecil.FieldAttributes]::Private -bor [Mono.Cecil.FieldAttributes]::Static
    $game.Write($gamePath)
    Assert-Scan 1 'private native field'
    [void][IO.File]::ReadAllBytes($gamePath)
    $target.Fields.Clear()
    $game.Write($gamePath)
    Assert-Scan 2 'unresolved native field'
} finally { $game.Dispose() }
'ACCESS_SCANNER_FIXTURES_PASS: public/private/unresolved outcomes verified.'
