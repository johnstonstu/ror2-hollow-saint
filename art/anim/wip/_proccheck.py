import subprocess
out = subprocess.check_output(["tasklist", "/V", "/FO", "CSV"], text=True, errors="ignore")
for line in out.splitlines():
    low = line.lower()
    if "blender" in low or "cursor-agent" in low or "agent.cmd" in low or "ror2-lightning" in low:
        print(line[:320])
print("DONE")
