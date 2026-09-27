"""Which tabard vertices sit inside the body on a frame, and where (fullqa depth, minus rest).
blender -b --factory-startup --python tabard_probe.py -- <module> "<title>" <frame> [group] """
import sys
from collections import Counter
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim/clips'))
import importlib
import hs_anim as H
import fullqa

a = sys.argv[sys.argv.index('--')+1:]
group = a[3] if len(a) > 3 else 'tabard front'
p = H.open_start()
fq = fullqa.FullQA(p)
deform = {b.name for b in p.rig.data.bones if b.use_deform}
bdom = fullqa.dominant(fq.body, deform)


def snapshot():
    dg = bpy.context.evaluated_depsgraph_get()
    bco, _, _ = fullqa.ev_mesh(fq.body, dg)
    tree = BVHTree.FromPolygons([Vector(v) for v in bco], fq.bpolys)
    rows = []
    for o in fq.groups[group]:
        co, _, _ = fullqa.ev_mesh(o, dg)
        for i, q in enumerate(co):
            d, idx = fullqa.depth(tree, Vector(q))
            rows.append((o.name, i, d, idx, tuple(q)))
    return rows


p.reset()
p.update()
rest = {(r[0], r[1]): r[2] for r in snapshot()}
for bones in (('tabard front.1', 'tabard back.1'), ('tabard front.2', 'tabard back.2'), ('tabard front.3', None)):
    for deg in (-12, -27, 15):
        p.reset()
        p.rot(bones[0], H.R(x=deg))
        if bones[1]:
            p.rot(bones[1], H.R(x=-deg))
        p.update()
        rows = snapshot()
        worst = max(((d-rest.get((n, i), 0.0), n) for n, i, d, idx, q in rows
                     if rest.get((n, i), 0.0) <= fullqa.BURIED), default=(0, ''))
        print('SWING ONLY', bones, deg, 'hits', sum(1 for n, i, d, idx, q in rows if d-rest.get((n, i), 0.0) > 0.005),
              'worst', round(worst[0]*1000, 1), worst[1])
if a[0] == '-':
    sys.exit(0)
built = importlib.import_module(a[0]).build(p)
p.ik(0, 0)
p.followers(False)
for act, info in built:
    if info['title'] != a[1]:
        continue
    p.rig.animation_data.action = act
    bpy.context.scene.frame_set(int(a[2]))
    p.update()
    rows = snapshot()
    hits = []
    for name, i, d, idx, q in rows:
        b = rest.get((name, i), 0.0)
        if d - b > 0.005 and b <= fullqa.BURIED:
            poly = fq.bpolys[idx]
            hits.append((round((d-b)*1000, 1), name, i, [round(x, 3) for x in q],
                         Counter(bdom[v] for v in poly).most_common(2), fq.poly_reg[idx]))
    hits.sort(reverse=True)
    print('HITS', len(hits))
    per = Counter(h[1] for h in hits)
    print('PER OBJECT', dict(per))
    per_bone = Counter(str(h[4][0][0]) for h in hits)
    print('PER BODY BONE', dict(per_bone))
    for h in hits[:25]:
        print('  ', h)
