"""Leg/arm landmark probe: seam-loop centroids between dominant regions vs bone joints, and L/R mirror (about MID_X
and about x=0). blender -b --factory-startup --python leg_probe.py -- <blend>"""
import bpy
import sys
import json
import numpy as np
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
import hs_anim as H
import fullqa

bpy.ops.wm.open_mainfile(filepath=sys.argv[sys.argv.index('--')+1])
rig = bpy.data.objects[H.RIG]
rig.data.pose_position = 'REST'
bpy.context.view_layer.update()
body = bpy.data.objects[H.BODY]
deform = {b.name for b in rig.data.bones if b.use_deform}
dom = fullqa.dominant(body, deform)
co = np.array([body.matrix_world @ v.co for v in body.data.vertices])
seam = {}
for e in body.data.edges:
    a, b = e.vertices
    ga, gb = dom[a], dom[b]
    if ga != gb:
        k = tuple(sorted((ga, gb)))
        seam.setdefault(k, set()).update((a, b))
out = {}
want = [('pelvis', '{} thigh'), ('{} thigh', '{} shin'), ('{} shin', '{} foot'), ('{} foot', '{} toe'),
        ('chest', '{} upperarm'), ('{} upperarm', '{} forearm'), ('{} forearm', '{} hand'), ('spine', '{} thigh')]
for s in 'LR':
    for a, b in want:
        k = tuple(sorted((a.format(s), b.format(s))))
        if k in seam:
            ids = sorted(seam[k])
            c = co[ids]
            out[f'{s} {a.format("")}|{b.format("")}'.replace('  ', ' ')] = [np.round(c.mean(0), 4).tolist(), len(ids),
                                                                          np.round(c.min(0), 3).tolist(), np.round(c.max(0), 3).tolist()]
bones = {}
for n in ('thigh', 'shin', 'foot', 'toe', 'upperarm', 'forearm', 'hand'):
    for s in 'LR':
        b = rig.data.bones[f'{s} {n}']
        bones[f'{s} {n}'] = [[round(x, 4) for x in b.head_local], [round(x, 4) for x in b.tail_local]]
# region extents per leg
ext = {}
for g in ('L thigh', 'R thigh', 'L shin', 'R shin', 'L foot', 'R foot', 'L toe', 'R toe'):
    ids = [i for i, x in enumerate(dom) if x == g]
    if ids:
        c = co[ids]
        ext[g] = [len(ids), np.round(c.min(0), 3).tolist(), np.round(c.max(0), 3).tolist(), np.round(c.mean(0), 3).tolist()]
print('PROBE', json.dumps({'seams': out, 'bones': bones, 'ext': ext}, indent=1))
