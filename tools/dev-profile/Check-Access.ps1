# Scans HollowSaint.dll for field/method accesses that are non-public in the REAL game assemblies.
# The GameLibs reference package is publicized, so the compiler accepts private members that then
# throw FieldAccessException / MethodAccessException at runtime. Exit code 1 if any are found.
param([string]$Dll)
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
if (-not $Dll) { $Dll = Join-Path $repo 'HollowSaintMod\bin\Release\netstandard2.1\HollowSaint.dll' }
$cecil = Get-ChildItem "$env:USERPROFILE\.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll"
Add-Type -Path $cecil.FullName
$managed = 'C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed'
$profilePlugins = Join-Path $env:APPDATA 'r2modmanPlus-local\RiskOfRain2\profiles\Hollow Saint Dev\BepInEx'
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory($managed)
Get-ChildItem $profilePlugins -Recurse -Filter *.dll | % DirectoryName | Sort -Unique | % { $resolver.AddSearchDirectory($_) }
$params = New-Object Mono.Cecil.ReaderParameters
$params.AssemblyResolver = $resolver
$bytes = [IO.File]::ReadAllBytes($Dll)
$module = [Mono.Cecil.ModuleDefinition]::ReadModule((New-Object IO.MemoryStream(,$bytes)), $params)

function IsSubclass($type, $baseFullName) {
    $t = $type
    while ($t -ne $null) {
        if ($t.FullName -eq $baseFullName) { return $true }
        if ($t.BaseType -eq $null) { return $false }
        try { $t = $t.BaseType.Resolve() } catch { return $false }
    }
    return $false
}

$problems = New-Object System.Collections.Generic.HashSet[string]
foreach ($type in $module.GetTypes()) {
    foreach ($method in $type.Methods) {
        if (-not $method.HasBody) { continue }
        foreach ($ins in $method.Body.Instructions) {
            $op = $ins.Operand
            if ($op -isnot [Mono.Cecil.MemberReference]) { continue }
            if ($op -is [Mono.Cecil.TypeReference]) { continue }
            $scope = $op.DeclaringType.Scope.Name
            if ($scope -eq $module.Name -or $scope -like 'HollowSaint*') { continue }
            try { $def = $op.Resolve() } catch { $def = $null }
            if ($def -eq $null) { continue }
            $public = $false; $family = $false
            if ($def -is [Mono.Cecil.FieldDefinition]) { $public = $def.IsPublic; $family = $def.IsFamily -or $def.IsFamilyOrAssembly }
            elseif ($def -is [Mono.Cecil.MethodDefinition]) { $public = $def.IsPublic; $family = $def.IsFamily -or $def.IsFamilyOrAssembly }
            else { continue }
            if (-not $def.DeclaringType.IsPublic -and -not $def.DeclaringType.IsNestedPublic) { $public = $false }
            if ($public) { continue }
            if ($family -and (IsSubclass $type $def.DeclaringType.FullName)) { continue }
            [void]$problems.Add("$($def.DeclaringType.FullName)::$($def.Name)  <- $($type.FullName).$($method.Name)")
        }
    }
}
if ($problems.Count -eq 0) { 'ACCESS_CHECK_PASS'; exit 0 }
'ACCESS_CHECK_FAIL ' + $problems.Count
$problems | Sort
exit 1
