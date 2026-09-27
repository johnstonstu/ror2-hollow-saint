import subprocess, pathlib, datetime, sys, os
root = pathlib.Path(r"C:\Users\stuwj\Documents\Coding\ror2-lightning")
os.chdir(root)
wip = root / "art" / "anim" / "wip"
log = wip / "refine-run-log.txt"
prompt = (wip / "refine-prompt.txt").read_text(encoding="utf-8")
agent = r"C:\Users\stuwj\AppData\Local\cursor-agent\agent.cmd"
stamp = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
cmd = [
    agent, "-p", "--trust", "--force",
    "--output-format", "stream-json",
    "--workspace", str(root),
    prompt,
]
with log.open("a", encoding="utf-8", newline="\n") as f:
    f.write(f"\n===== refine launch {stamp} items 9b + 9 REVISED + 9c (+ optional 8) =====\n")
    f.write("mode: agent -p --trust --force --output-format stream-json\n")
    f.flush()
    try:
        p = subprocess.run(cmd, cwd=str(root), stdout=f, stderr=subprocess.STDOUT, text=True)
        code = p.returncode
    except Exception as e:
        f.write(f"LAUNCH_ERROR: {e!r}\n")
        code = 99
    f.write(f"===== refine exit code={code} {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')} =====\n")
sys.exit(code)
