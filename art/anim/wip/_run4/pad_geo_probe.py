"""Read-only: rest geometry of pads, yoke, halo arcs and the halo/pauldron bones on v19."""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT/'tools/blender/anim'))
import bpy
from mathutils import Vector

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
out = {}


def pts(o):
    ev = o.evaluated_get(dg)
    me = ev.to_mesh()
    v = [o.matrix_world @ x.co for x in me.vertices]
    ev.to_mesh_clear()
    return v


def bbox(v):
    return [[round(min(q[i] for q in v), 4) for i in range(3)], [round(max(q[i] for q in v), 4) for i in range(3)]]


for o in bpy.data.objects:
    if o.type == 'MESH' and (o.name.startswith('HALO |') or 'pauldron' in o.name or 'SHOULDER' in o.name or 'scapula' in o.name.lower()):
        v = pts(o)
        out[o.name] = {'parent': o.parent.name if o.parent else None, 'ptype': o.parent_type, 'pbone': o.parent_bone,
                       'groups': [g.name for g in o.vertex_groups][:6],
                       'mods': [m.type for m in o.modifiers], 'bbox': bbox(v), 'n': len(v)}
for n in ('halo root', 'halo 1', 'halo 2', 'halo 3', 'halo 4', 'L pauldron', 'R pauldron', 'L scapula', 'R scapula', 'chest', 'L shoulder', 'R shoulder', 'L upperarm', 'R upperarm'):
    b = rig.data.bones.get(n)
    if b:
        out['BONE '+n] = {'head': [round(x, 4) for x in b.head_local], 'tail': [round(x, 4) for x in b.tail_local],
                          'parent': b.parent.name if b.parent else None,
                          'cons': [(c.type, c.name, round(c.influence, 2), c.mute) for c in rig.pose.bones[n].constraints]}
# pad vs yoke: yoke verts inside pad bbox
yoke = pts(bpy.data.objects['HALO | V17 yoke bar'])
for s in 'LR':
    pv = pts(bpy.data.objects[f'{s} SHOULDER | V17 pauldron upper'])
    lo, hi = bbox(pv)
    ins = [q for q in yoke if all(lo[i] <= q[i] <= hi[i] for i in range(3))]
    out[f'yoke in {s} pad bbox'] = [len(ins), bbox(ins) if ins else None]
    for k in (2, 3):
        av = pts(bpy.data.objects[f'HALO | independent copper arc {k}'])
        low = min(av, key=lambda q: q.z)
        out[f'arc{k} lowest'] = [round(x, 4) for x in low]
        out[f'arc{k} min dist to {s} pad'] = round(min((a-p).length for a in av[::2] for p in pv), 4)
print('PADGEO', json.dumps(out), flush=True)
(ROOT/'art/anim/wip/_run4/pad_geo.json').write_text(json.dumps(out, indent=1))
