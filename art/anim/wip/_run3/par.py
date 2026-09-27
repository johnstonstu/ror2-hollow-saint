"""Run several background Blender scripts in parallel and print matching output lines (system Python).

python art/anim/wip/_run3/par.py "PATTERN" "script.py -- args" "script.py -- args" ... [--jobs 3]
Each job: blender --background --factory-startup --python-exit-code 1 --python <script> -- <args>
Full logs go to art/anim/wip/_run3/logs/par-<n>.log.
"""
import re
import shlex
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BLENDER = 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe'
args = sys.argv[1:]
jobs = 3
if '--jobs' in args:
    i = args.index('--jobs')
    jobs = int(args[i+1])
    del args[i:i+2]
pat = re.compile(args[0])
logs = Path(__file__).parent/'logs'
logs.mkdir(exist_ok=True)


def run(item):
    n, spec = item
    parts = shlex.split(spec, posix=True)
    script, rest = parts[0], parts[1:]
    cmd = [BLENDER, '--background', '--factory-startup', '--python-exit-code', '1', '--python', script, *rest]
    log = logs/f'par-{n}.log'
    with open(log, 'w', encoding='utf-8', errors='replace') as f:
        code = subprocess.run(cmd, cwd=ROOT, stdout=f, stderr=subprocess.STDOUT).returncode
    lines = [l.rstrip() for l in log.read_text(encoding='utf-8', errors='replace').splitlines()
             if pat.search(l) or 'Error' in l or l.startswith('Traceback')]
    return spec, code, lines


with ThreadPoolExecutor(jobs) as ex:
    for spec, code, lines in ex.map(run, list(enumerate(args[1:]))):
        print(f'=== {spec} (exit {code})')
        for l in lines:
            print(l)
