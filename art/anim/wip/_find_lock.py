import subprocess, os, time
path = r"C:\Users\stuwj\Documents\Coding\ror2-lightning\art\anim\wip\refine-run-log.txt"
print("exists", os.path.exists(path), "size", os.path.getsize(path) if os.path.exists(path) else None)
# try rename
try:
    os.rename(path, path + ".oldlock")
    print("renamed ok")
except Exception as e:
    print("rename fail", repr(e))
# list cmd/python with command lines via tasklist /V is weak; use powershell CIM from a file
ps = r"""
$target = 'refine-run-log'
Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -and $_.CommandLine -like '*refine*' } | ForEach-Object {
  "$($_.ProcessId)|$($_.Name)|$($_.CommandLine)"
}
"""
r = subprocess.run(["powershell","-NoProfile","-Command", ps], capture_output=True, text=True, errors="ignore")
print("PS_OUT")
print(r.stdout)
print("PS_ERR", r.stderr[:500] if r.stderr else "")
