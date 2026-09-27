param([string]$Json)
# Runs arm_tune.py on a variants file and prints one compact block per variant.
$B = "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe"
$root = "C:\Users\stuwj\Documents\Coding\ror2-lightning"
Set-Location $root
$full = (Resolve-Path $Json).Path
& $B --background --factory-startup --python-exit-code 1 --python tools/blender/anim/arm_tune.py -- $full 2>&1 | Select-String '^TUNE|Error:|Traceback|File "' | ForEach-Object {
    $l = $_.Line
    if ($l.StartsWith('TUNE')) {
        $j = $l.Substring(5) | ConvertFrom-Json
        "{0,-6} {1,-26} min={2,7} Lunder={3} Runder={4} spread={5}`n   L_mm={6}`n   R_mm={7}`n   Lhits={8}`n   Rhits={9}" -f $j.target, $j.label, $j.min_m, $j.L.frames_under_min, $j.R.frames_under_min, ($j.spread_m | ConvertTo-Json -Compress), ($j.L_mm -join ','), ($j.R_mm -join ','), ($j.L_hits | ConvertTo-Json -Compress), ($j.R_hits | ConvertTo-Json -Compress)
    } else { $l }
}
