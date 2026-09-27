"""Compare two shoulder_diag reports (system Python).

Run: python tools/blender/anim/shoulder_compare.py cbase cfix
"""
import json
import sys
from pathlib import Path

D = Path(__file__).resolve().parents[3]/'art/anim/wip/shoulder'
a, b = (json.loads((D/f'{t}.json').read_text()) for t in sys.argv[1:3])
KEYS = ['stretched_2', 'max_stretch', 'compressed_0.6', 'collapsed_faces', 'flipped_faces']
tot = {k: [0, 0] for k in KEYS}
for section in ('sweep', 'clips'):
    for x, y in zip(a[section], b[section]):
        label = f"{x.get('clip', x.get('kind'))} {x.get('frame', x.get('deg'))}"
        for s in 'LR':
            print(f"{label:22} {s} raise {x[s]['raise_deg']:5} | "
                  + ' | '.join(f'{k} {x[s][k]}->{y[s][k]}' for k in KEYS))
            for k in KEYS:
                tot[k][0] += x[s][k]
                tot[k][1] += y[s][k]
print('TOTAL', {k: (round(v[0], 1), round(v[1], 1)) for k, v in tot.items()})
