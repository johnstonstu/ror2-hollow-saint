param([string]$Tag, [string[]]$Lane1, [string[]]$Lane2)
# Scratch (hands2): preview.py --no-render per module in two sequential lanes (at most 2 Blenders).
$B = "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe"
Set-Location "C:\Users\stuwj\Documents\Coding\ror2-lightning"
$logs = "art\anim\wip\hands2\_work\logs"
New-Item -ItemType Directory -Force $logs | Out-Null
$lane = {
    param($B, $mods, $logs, $tag)
    Set-Location "C:\Users\stuwj\Documents\Coding\ror2-lightning"
    foreach ($m in $mods) {
        & $B --background --factory-startup --python-exit-code 1 --python tools/blender/anim/preview.py -- $m --no-render *> "$logs\$tag-$m.log"
        "$m exit $LASTEXITCODE"
    }
}
$j1 = Start-Job $lane -ArgumentList $B, $Lane1, $logs, $Tag
$j2 = Start-Job $lane -ArgumentList $B, $Lane2, $logs, $Tag
Wait-Job $j1, $j2 | Out-Null
Receive-Job $j1, $j2
"LANES DONE"
