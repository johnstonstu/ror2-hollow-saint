"""Hand close-ups (with --context) for every clip of the given modules, then hand_sheet.py per clip.

python art/anim/wip/_run3/hands_all.py [module ...] [--jobs 4] [--sheets-only]
"""
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
MODS = ['run', 'walk', 'glide', 'air', 'run_dirs', 'primary', 'arcstep', 'special', 'presentation']
args = sys.argv[1:]
jobs = args[args.index('--jobs')+1] if '--jobs' in args else '4'
mods = [a for a in args if a in MODS] or MODS
slug = lambda t: ''.join(c if c.isalnum() else '-' for c in t.lower()).strip('-')
slugs = {}
for m in mods:
    d = json.loads((ROOT/f'art/anim/wip/{m}/qa.json').read_text(encoding='utf-8'))
    clips = d['clips'] if isinstance(d, dict) and 'clips' in d else d
    slugs[m] = [slug(c['title']) for c in clips]
if '--sheets-only' not in args:
    cmds = [f"tools/blender/anim/hand_closeup.py -- {m} {','.join(s)} --context" for m, s in slugs.items()]
    subprocess.run([sys.executable, str(Path(__file__).parent/'par.py'), '^(HANDS|Traceback|\\w+Error)', *cmds,
                    '--jobs', jobs], cwd=ROOT, check=False)
for m, ss in slugs.items():
    for s in ss:
        subprocess.run([sys.executable, str(ROOT/'tools/blender/anim/hand_sheet.py'), str(ROOT/f'art/anim/wip/hands/{s}'),
                        '--cols', '4', '--scale', '0.4'], cwd=ROOT, check=False)
