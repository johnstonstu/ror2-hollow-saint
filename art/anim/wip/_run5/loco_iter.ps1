param([string]$Tag = 'x', [string]$Mods = 'run_dirs loco8', [string]$Frames = '1', [string]$Views = 'front')
# Preview-QA the given modules (fresh v18 build, never saved) and summarise IK, gaps and full-QA fails.
$bl = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
Start-Sleep 7
foreach ($m in $Mods -split ' ') {
    $log = "art/anim/wip/_run5/logs/prev-$m-$Tag.txt"
    & $bl --background --factory-startup --python-exit-code 1 --python tools/blender/anim/preview.py -- $m --frames $Frames --views $Views *> $log
    "$m exit $LASTEXITCODE"
    Select-String -Path $log -Pattern '^BAKED HS_anim \| ' | ForEach-Object {
        $l = $_.Line; $t = ($l -split ' \{')[0]; $j = $l.Substring($l.IndexOf('{')) | ConvertFrom-Json
        "  $t ik_miss=$($j.ik_miss_m) ext=$($j.max_leg_extension)"
    }
    Select-String -Path $log -Pattern 'CHECK \{' | ForEach-Object { '  ' + $_.Line }
}
python art/anim/wip/_run5/fq_mods.py @($Mods -split ' ')
