"""Body skin weights near world points (rest, after open_start fixes).
blender -b --factory-startup --python skin_weights.py -- "x,y,z;x,y,z" [radius] [group filter]"""
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
import hs_anim as H

a = sys.argv[sys.argv.index('--')+1:]
pts = [Vector([float(x) for x in s.split(',')]) for s in a[0].split(';')]
rad = float(a[1]) if len(a) > 1 else 0.03
flt = a[2] if len(a) > 2 else ''
p = H.open_start()
body = bpy.data.objects[H.BODY]
names = {g.index: g.name for g in body.vertex_groups}
mw = body.matrix_world
for q in pts:
    print('POINT', tuple(round(c, 3) for c in q))
    rows = []
    for v in body.data.vertices:
        c = mw @ v.co
        d = (c-q).length
        if d < rad:
            w = {names[g.group]: round(g.weight, 2) for g in v.groups if g.weight > 0.01}
            if not flt or any(flt in k for k in w):
                rows.append((round(d, 3), v.index, tuple(round(x, 3) for x in c), w))
    for r in sorted(rows)[:25]:
        print('  ', r)
    print('  count', len(rows))
