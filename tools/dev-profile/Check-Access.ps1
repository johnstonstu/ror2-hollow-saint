# Scans HollowSaint.dll for field/method accesses that are non-public in the REAL game assemblies.
# The GameLibs reference package is publicized, so the compiler accepts private members that then
# throw FieldAccessException / MethodAccessException at runtime. Exit code 1 if any are found.
param([string]$Dll, [string]$Managed, [string]$ProfileBepInEx)
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
if (-not $Dll) { $Dll = Join-Path $repo 'HollowSaintMod\bin\Release\netstandard2.1\HollowSaint.dll' }
$cecil = Join-Path $env:USERPROFILE '.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll'
if (-not $Managed) { $Managed = 'C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed' }
if (-not $ProfileBepInEx) { $ProfileBepInEx = Join-Path $env:APPDATA 'r2modmanPlus-local\RiskOfRain2\profiles\Hollow Saint Dev\BepInEx' }
foreach ($inputPath in @($Dll, $cecil, (Join-Path $Managed 'RoR2.dll'), $ProfileBepInEx)) {
    if (-not (Test-Path -LiteralPath $inputPath)) {
        Write-Output "ACCESS_CHECK_UNAVAILABLE: missing $inputPath"
        exit 2
    }
}
Add-Type -Path $cecil
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory($Managed)
$resolver.AddSearchDirectory((Split-Path -Parent $Dll))
Get-ChildItem -LiteralPath $ProfileBepInEx -Recurse -Filter *.dll | ForEach-Object DirectoryName | Sort-Object -Unique | ForEach-Object { $resolver.AddSearchDirectory($_) }
$params = New-Object Mono.Cecil.ReaderParameters
$params.AssemblyResolver = $resolver
$bytes = [IO.File]::ReadAllBytes($Dll)
$module = [Mono.Cecil.ModuleDefinition]::ReadModule((New-Object IO.MemoryStream(,$bytes)), $params)
$unresolved = New-Object System.Collections.Generic.HashSet[string]

function IsSubclass($type, $baseFullName) {
    $t = $type
    while ($t -ne $null) {
        if ($t.FullName -eq $baseFullName) { return $true }
        if ($t.BaseType -eq $null) { return $false }
        try { $t = $t.BaseType.Resolve() }
        catch { [void]$unresolved.Add("base type $($t.BaseType.FullName): $($_.Exception.Message)"); return $false }
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
            # Rectangular array methods are CLR intrinsics, not members on the
            # game's element type. Cecil cannot resolve them to definitions.
            if ($op.DeclaringType -is [Mono.Cecil.ArrayType] -and
                $op -is [Mono.Cecil.MethodReference] -and $op.Name -in @('.ctor', 'Get', 'Set', 'Address')) { continue }
            $scope = $op.DeclaringType.Scope.Name
            if ($scope -eq $module.Name -or $scope -eq $module.Assembly.Name.Name) { continue }
            try { $def = $op.Resolve() }
            catch {
                [void]$unresolved.Add("$($op.FullName) <- $($method.FullName): $($_.Exception.Message)")
                continue
            }
            if ($def -eq $null) {
                [void]$unresolved.Add("$($op.FullName) <- $($method.FullName): resolution returned null")
                continue
            }
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
$module.Dispose()
$resolver.Dispose()
if ($problems.Count -gt 0) { 'ACCESS_CHECK_FAIL ' + $problems.Count; $problems | Sort-Object }
if ($unresolved.Count -gt 0) { 'ACCESS_CHECK_UNAVAILABLE: unresolved references ' + $unresolved.Count; $unresolved | Sort-Object }
if ($problems.Count -gt 0) { exit 1 }
if ($unresolved.Count -gt 0) { exit 2 }
'ACCESS_CHECK_PASS'
exit 0
