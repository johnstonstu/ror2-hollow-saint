param([string]$Tag, [string[]]$Modules, [string]$Extra = "--no-render")
# Parallel background-Blender previews (QA only by default); prints per-clip status and arm clearance.
$B = "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe"
$root = "C:\Users\stuwj\Documents\Coding\ror2-lightning"
Set-Location $root
New-Item -ItemType Directory -Force art\anim\wip\logs | Out-Null
$procs = foreach ($m in $Modules) {
    $argv = @("--background", "--factory-startup", "--python-exit-code", "1", "--python", "tools/blender/anim/preview.py", "--", $m) + ($Extra -split ' ' | Where-Object { $_ })
    Start-Process -FilePath $B -ArgumentList $argv -RedirectStandardOutput "art\anim\wip\logs\$Tag-$m.log" -RedirectStandardError "art\anim\wip\logs\$Tag-$m.err" -PassThru -NoNewWindow
}
$procs | Wait-Process
foreach ($m in $Modules) {
    "== $m"
    Select-String -Path "art\anim\wip\logs\$Tag-$m.log" -Pattern '^QA ' | ForEach-Object {
        $j = $_.Line.Substring(3) | ConvertFrom-Json
        "{0}: {1} ik={2} ext={3} clear={4} L={5} R={6} hand={7}" -f $j.title, $j.status, $j.ik_miss_m, $j.max_leg_extension, $j.arm_clear_min_m,
            ($j.arm_clearance.L | ConvertTo-Json -Compress), ($j.arm_clearance.R | ConvertTo-Json -Compress), ($j.hand_qa_summary | ConvertTo-Json -Compress)
    }
    Get-Content "art\anim\wip\logs\$Tag-$m.err" -Tail 4
    Select-String -Path "art\anim\wip\logs\$Tag-$m.log" -Pattern 'Traceback|Error:' | Select-Object -First 5
}
