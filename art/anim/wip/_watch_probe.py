import pathlib, json, subprocess, sys

root = pathlib.Path(r"C:/Users/stuwj/Documents/Coding/ror2-lightning/art/anim")
status = (root / "STATUS.md").read_text(encoding="utf-8", errors="replace")
print("STATUS_HEAD")
print("\n".join(status.splitlines()[:45]))
print("---MATCHES---")
for i, l in enumerate(status.splitlines(), 1):
    low = l.lower()
    if any(k in low for k in ("v16", "9d", "9e", "heel", "not started", "item 10", "achilles")):
        print(i, l)

qa = root / "v16" / "qa-summary.json"
print("---QA---")
if qa.exists():
    data = json.loads(qa.read_text(encoding="utf-8"))
    if isinstance(data, dict):
        print("keys", list(data.keys())[:40])
        for k in ("checkpoint", "version", "pass", "fail", "summary", "notes", "clips_pass", "all_pass", "status"):
            if k in data:
                print(k, ":", data[k])
        # print compact pass/fail counts if nested
        clips = data.get("clips") or data.get("by_clip") or data.get("results")
        if isinstance(clips, dict):
            fails = [n for n, v in clips.items() if isinstance(v, dict) and v.get("status") not in (None, "PASS", "pass", True)]
            print("clip_count", len(clips), "nonpass_sample", fails[:20])
        print(json.dumps(data)[:3000])
    else:
        print(str(data)[:3000])
else:
    print("no qa")

log = pathlib.Path(r"C:/Users/stuwj/Documents/Coding/ror2-lightning/art/anim/wip/agent-run3-20260926-2342.log")
print("---AGENT_TAIL---")
print("\n".join(log.read_text(encoding="utf-8", errors="replace").splitlines()[-60:]))

print("---NODE---")
ps = r"""
Get-CimInstance Win32_Process |
  Where-Object { $_.Name -eq 'node.exe' } |
  Select-Object ProcessId, CommandLine |
  ConvertTo-Json -Depth 3
"""
out = subprocess.check_output(["powershell", "-NoProfile", "-Command", ps], text=True, errors="replace")
print(out[:15000])

print("---TRANSITIONS---")
t = root / "wip" / "transitions"
print("exists", t.exists())
if t.exists():
    files = sorted(t.rglob("*"), key=lambda p: p.stat().st_mtime, reverse=True)[:30]
    for p in files:
        if p.is_file():
            print(p, p.stat().st_mtime)

print("---HEELS---")
for pat in ("*heel*", "*thrust*", "*stitch*", "*walk*run*glide*"):
    for p in (root / "wip").rglob(pat):
        if p.is_file():
            print(p)
