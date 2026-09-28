"""Render copies of the matrix specs: frames go to transitions/temp/<name> (git-ignored **/temp/), hero + chase.
python gen_render.py   (then stitch.py over specs/render/*.json, sheets via sheet.py, publish.py copies GIFs)"""
import json
from pathlib import Path

WIP = Path(__file__).resolve().parents[1]
SRC, OUT = WIP/'transitions/specs/matrix', WIP/'transitions/specs/render'
OUT.mkdir(parents=True, exist_ok=True)
n = 0
for f in sorted(SRC.glob('*.json')):
    if f.name.startswith('_'):
        continue
    spec = json.loads(f.read_text(encoding='utf-8'))
    spec['out'] = f"transitions/temp/{spec['name']}"
    spec['views'] = ['hero', 'chase']
    (OUT/f.name).write_text(json.dumps(spec, indent=1), encoding='utf-8')
    n += 1
print(n, 'render specs ->', OUT)
