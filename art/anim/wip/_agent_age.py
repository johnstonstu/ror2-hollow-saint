import subprocess, datetime
ps = r"""
$ids = 37616,34824,31644,40564
Get-CimInstance Win32_Process | Where-Object { $ids -contains $_.ProcessId } | ForEach-Object {
  $ageMin = ((Get-Date) - $_.CreationDate).TotalMinutes
  "$($_.ProcessId)|$($_.Name)|created=$($_.CreationDate)|age_min={0:N1}|cmd=$($_.CommandLine.Substring(0,[Math]::Min(120,$_.CommandLine.Length)))" -f $ageMin
}
"""
r = subprocess.run(["powershell","-NoProfile","-Command", ps], capture_output=True, text=True, errors="ignore")
print(r.stdout)
print("ERR", r.stderr[:300] if r.stderr else "")
