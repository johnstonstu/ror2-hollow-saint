param([string]$Tag = 't', [string]$Mod = 'spear')
Start-Sleep 7
$log = "art/anim/wip/_run5/logs/$Mod-$Tag.log"
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --python-exit-code 1 --python tools/blender/anim/preview.py -- $Mod --no-render *> $log
"exit $LASTEXITCODE"
Select-String -Path $log -Pattern "SPEAR solve|Traceback|Error:" | ForEach-Object { $_.Line.Substring(0, [Math]::Min(260, $_.Line.Length)) }
python art/anim/wip/_run5/qa_brief.py $Mod
