"""Which limb-skin vertices sit inside the rest of the body on a frame (fullqa 'body <limb>' probe, minus rest depth).
blender -b --factory-startup --python limb_probe.py -- <module> "<title>" <frame> "<limb, e.g. L arm>" """
import importlib
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim/clips'))
import hs_anim as H
import fullqa as FQ

a = sys.argv[sys.argv.index('--')+1:]
mod, title, frame, limb = a[0], a[1], int(a[2]), a[3]
p = H.open_start()
acts = {i['title']: act for act, i in importlib.import_module(mod).build(p)}
p.ik(0, 0)
p.followers(False)
q = FQ.FullQA(p)
body = q.body
names = {g.index: g.name for g in body.vertex_groups}
regs = FQ.LIMBS[limb]
ids = np.concatenate([q.reg_idx[r] for r in regs if r in q.reg_idx])
keep = [i for i, pp in enumerate(q.bpolys) if q.poly_reg[i] not in regs]


def measure():
    dg = bpy.context.evaluated_depsgraph_get()
    bco, _, _ = FQ.ev_mesh(body, dg)
    bv = [Vector(v) for v in bco]
    tree = BVHTree.FromPolygons(bv, [q.bpolys[i] for i in keep])
    out = {}
    for i in ids:
        d, idx = FQ.depth(tree, bv[i])
        out[i] = (d, q.poly_reg[keep[idx]] if d else None, bco[i])
    return out


p.reset()
p.rig.animation_data.action = None
bpy.context.view_layer.update()
rest = measure()
p.rig.animation_data.action = acts[title]
bpy.context.scene.frame_set(frame)
cur = measure()
mw = body.matrix_world
rows = []
for i, (d, reg, co) in cur.items():
    dd = d-rest[i][0]
    if dd > 0.004:
        w = {names[g.group]: round(g.weight, 2) for g in body.data.vertices[i].groups if g.weight > 0.02}
        rows.append((round(dd*1000, 1), i, reg, q.breg[i], tuple(round(x, 3) for x in co),
                     tuple(round(x, 3) for x in mw @ body.data.vertices[i].co), w))
rows.sort(key=lambda r: -r[0])
print('HITS', len(rows))
for r in rows[:30]:
    print('  ', r)
