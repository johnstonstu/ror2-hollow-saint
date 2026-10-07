# Success: refactored methods outside the explicitly extracted configuration type
# retain the release IL, fields, constants and signatures. Not native execution.
param([Parameter(Mandatory)][string]$Before, [Parameter(Mandatory)][string]$After)
$ErrorActionPreference = 'Stop'
$cecil = Join-Path $env:USERPROFILE '.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll'
Add-Type -Path $cecil
function Read-Contract([string]$path) {
    $module = [Mono.Cecil.ModuleDefinition]::ReadModule((Resolve-Path -LiteralPath $path).Path)
    try {
        $result = @{}
        foreach ($type in $module.GetTypes()) {
            # Bind was split into feature methods; its behavior is covered by config fixtures.
            if ($type.FullName -like 'HollowSaint.FoundationKit.KitConfig*') { continue }
            $result['type:' + $type.FullName] = "$($type.Attributes)|$($type.BaseType)"
            foreach ($field in $type.Fields) {
                $result['field:' + $field.FullName] = "$($field.Attributes)|$($field.Constant)"
            }
            foreach ($method in $type.Methods) {
                $body = @($method.Attributes.ToString())
                if ($method.HasBody) {
                    $body += $method.Body.Variables | ForEach-Object { $_.VariableType.FullName }
                    $body += $method.Body.Instructions | ForEach-Object { "$($_.OpCode) $($_.Operand)" }
                    $body += $method.Body.ExceptionHandlers | ForEach-Object { "$($_.HandlerType)|$($_.TryStart)|$($_.TryEnd)|$($_.HandlerStart)|$($_.HandlerEnd)|$($_.CatchType)" }
                }
                $result['method:' + $method.FullName] = $body -join "`n"
            }
        }
        return $result
    } finally { $module.Dispose() }
}
$old = Read-Contract $Before
$new = Read-Contract $After
$differences = @(@($old.Keys) + @($new.Keys) | Sort-Object -Unique | Where-Object { $old[$_] -cne $new[$_] })
if ($differences.Count) { $differences; throw "Assembly comparison: $($differences.Count) differences" }
"ASSEMBLY_COMPARISON_PASS: $($old.Count) type, field and method contracts unchanged (configuration extraction excluded)."
