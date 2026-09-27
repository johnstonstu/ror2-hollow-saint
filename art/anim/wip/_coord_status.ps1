Set-Location C:\Users\stuwj\Documents\Coding\ror2-lightning
"NOW $(Get-Date -Format 'HH:mm:ss')"
$r3=Get-Process -Id 40464 -ErrorAction SilentlyContinue; "run3 node alive: $([bool]$r3)"
$vf=Get-Process -Id 39240 -ErrorAction SilentlyContinue; "vfx node alive: $([bool]$vf)"
foreach($l in 'art\anim\wip\agent-run3-20260926-2342.log','art\vfx\vfx-run1-20260927-0045.log'){ $f=Get-Item $l; "$l len=$($f.Length) mtime=$($f.LastWriteTime.ToString('HH:mm:ss'))" }
"-- blenders"; Get-CimInstance Win32_Process -Filter "Name='blender.exe'" | % { "$($_.ProcessId) ppid=$($_.ParentProcessId) $($_.CreationDate.ToString('HH:mm:ss')) $($_.CommandLine.Substring(0,[Math]::Min(220,$_.CommandLine.Length)))" }
"-- newest blends"; Get-ChildItem art\anim\*.blend,art\anim\*.blend1,art\anim\*.tmp -ErrorAction SilentlyContinue | Sort LastWriteTime -Desc | Select -First 3 | % { "$($_.Name) $($_.Length) $($_.LastWriteTime.ToString('HH:mm:ss'))" }
"-- STATUS.md mtime $((Get-Item art\anim\STATUS.md).LastWriteTime.ToString('HH:mm:ss'))"
"-- refine-run-log plain tail"; Get-Content art\anim\wip\refine-run-log.txt -Tail 6 | ? { $_ -notmatch '^\{' } | % { $_.Substring(0,[Math]::Min(400,$_.Length)) }
"-- run3 last assistant texts"; $t=Get-Content art\anim\wip\agent-run3-20260926-2342.log -Tail 3000 | ? { $_ -match '^\{"type":"assistant"' } | Select -Last 3; $t | % { try { ($_ | ConvertFrom-Json).message.content[0].text.Substring(0,[Math]::Min(500,($_ | ConvertFrom-Json).message.content[0].text.Length)) } catch {} }
"-- run3 last tool cmds"; Get-Content art\anim\wip\agent-run3-20260926-2342.log -Tail 3000 | ? { $_ -match '"subtype":"started"' } | Select -Last 3 | % { $m=[regex]::Match($_,'"(command|path)":"([^"]{0,250})'); $m.Groups[2].Value }
