"""Read-only: rest clearance of halo arcs 2/3 and the yoke bar from the pads under candidate halo offsets / yoke x-scale.
Signed clearance: distance from each arc/yoke vertex to the pad surface, negative when inside the pad (and pad verts
inside the arc/yoke count too)."""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT/'tools/blender/anim'))
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

import hs_anim as H

bpy.ops.wm.open_mainfile(filepath=str(ROOT/'art/anim/hollow-saint-anim-v19.blend'))
rig = bpy.data.objects[H.RIG]
rig.animation_data.action = None
for pb in rig.pose.bones:
    pb.location = (0, 0, 0)
    pb.rotation_quaternion = (1, 0, 0, 0)
    pb.scale = (1, 1, 1)
bpy.context.view_layer.update()
dg = bpy.context.evaluated_depsgraph_get()
DIRS = [Vector(d).normalized() for d in ((1, 0.2, 0.1), (-0.3, 1, 0.2), (0.1, -0.2, 1), (-1, -0.4, -0.3), (0.2, 0.3, -1))]


def mesh(o):
    ev = o.evaluated_get(dg)
    me = ev.to_mesh()
    v = [o.matrix_world @ x.co for x in me.vertices]
    p = [tuple(x.vertices) for x in me.polygons]
    ev.to_mesh_clear()
    return v, p


def inside(tree, q, need=3):
    votes = 0
    for d in DIRS:
        n, o = 0, q
        for _ in range(24):
            hit = tree.ray_cast(o, d)
            if hit[0] is None:
                break
            n += 1
            o = hit[0]+d*1e-5
        votes += n % 2
    return votes >= need


pads = {s: mesh(bpy.data.objects[f'{s} SHOULDER | V17 pauldron upper']) for s in 'LR'}
ptrees = {s: BVHTree.FromPolygons(*pads[s]) for s in 'LR'}
arcs = {s: mesh(bpy.data.objects[f'HALO | independent copper arc {2 if s == "L" else 3}']) for s in 'LR'}
yoke = mesh(bpy.data.objects['HALO | V17 yoke bar'])


def clearance(verts, polys, s):
    t = ptrees[s]
    worst = 9.0
    for q in verts:
        loc, _, _, d = t.find_nearest(q, 0.2)
        if loc is None:
            continue
        worst = min(worst, -d if inside(t, q) else d)
    other = BVHTree.FromPolygons(verts, polys)
    for q in pads[s][0]:
        loc, _, _, d = other.find_nearest(q, 0.05)
        if loc is not None and inside(other, q):
            worst = min(worst, -d)
    return worst


res = {'halo': [], 'yoke': []}
centre = Vector((H.MID_X, 0.15, 1.957))
for dz in (0.0, 0.02, 0.04):
    for dy in (0.03, 0.045, 0.06):
        for sc in (0.86, 0.9, 0.94):
            row = {'dz': dz, 'dy': dy, 'scale': sc}
            for s in 'LR':
                v, p = arcs[s]
                vv = [centre+(q-centre)*sc+Vector((0, dy, dz)) for q in v]
                row[s] = round(clearance(vv, p, s)*1000, 1)
            res['halo'].append(row)
            print('HALO', row, flush=True)
for k in ():
    v, p = yoke
    vv = [Vector((H.MID_X+(q.x-H.MID_X)*k, q.y, q.z)) for q in v]
    row = {'xscale': k, 'L': round(clearance(vv, p, 'L')*1000, 1), 'R': round(clearance(vv, p, 'R')*1000, 1),
           'half_len': round(max(abs(q.x-H.MID_X) for q in vv), 4)}
    res['yoke'].append(row)
    print('YOKE', row, flush=True)
(ROOT/'art/anim/wip/_run4/pad_clear_search2.json').write_text(json.dumps(res, indent=1))

