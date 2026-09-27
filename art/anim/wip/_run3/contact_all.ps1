param([string]$Mods = "run,walk,glide,air,run_dirs,primary,arcstep,special,presentation", [string]$Tag = "v15")
$B = "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe"
$root = "C:\Users\stuwj\Documents\Coding\ror2-lightning"
$out = "$root\art\anim\wip\_run3\contact"
New-Item -ItemType Directory -Force $out | Out-Null
$jobs = @()
foreach ($m in $Mods.Split(',')) {
    while (($jobs | Where-Object { -not $_.HasExited }).Count -ge 3) { Start-Sleep 2 }
    $jobs += Start-Process -FilePath $B -ArgumentList "--background","--factory-startup","--python-exit-code","1","--python","art/anim/wip/_run3/contact_probe.py","--",$m -WorkingDirectory $root -RedirectStandardOutput "$out\$Tag-$m.log" -RedirectStandardError "$out\$Tag-$m.err" -NoNewWindow -PassThru
}
$jobs | ForEach-Object { $_.WaitForExit() }
Get-ChildItem "$out\$Tag-*.log" | ForEach-Object { Select-String -Path $_ -Pattern "^(REST|CLIP)" | ForEach-Object { $_.Line } }
