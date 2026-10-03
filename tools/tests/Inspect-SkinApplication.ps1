$ErrorActionPreference = 'Stop'
Add-Type -Path "$env:USERPROFILE\.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll"
$path = 'C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\RoR2.dll'
$module = [Mono.Cecil.ModuleDefinition]::ReadModule([IO.MemoryStream]::new([IO.File]::ReadAllBytes($path)))
$rows = foreach ($type in $module.GetTypes()) {
    if ($type.FullName -notmatch 'SkinDef|CharacterModel') { continue }
    foreach ($method in $type.Methods) {
        if (-not $method.HasBody) { continue }
        $instructions = @($method.Body.Instructions)
        if (-not ($instructions | Where-Object { $_.Operand -and $_.Operand.ToString() -match 'set_sharedMaterial|defaultMaterial' })) { continue }
        $method.FullName
        $instructions | ForEach-Object ToString
    }
}
$output = Join-Path $PSScriptRoot '..\..\artifacts\skin-light01\skin-application-il.txt'
$rows | Set-Content -LiteralPath $output
$rows | Select-String 'SkinDef|sharedMaterial|defaultMaterial' | ForEach-Object Line
$module.Dispose()
