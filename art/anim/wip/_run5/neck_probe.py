"""Neck weight gradient: body verts binned by z (front/back halves), mean chest/neck/head/spine weight.
blender -b --factory-startup --python neck_probe.py -- <blend>"""
import bpy
import sys
import json
bpy.ops.wm.open_mainfile(filepath=sys.argv[sys.argv.index('--')+1])
rig = bpy.data.objects['Hollow Saint | v8 rig']
rig.data.pose_position = 'REST'
bpy.context.view_layer.update()
body = bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
names = {g.index: g.name for g in body.vertex_groups}
bins = {}
for v in body.data.vertices:
    c = body.matrix_world @ v.co
    if abs(c.x+0.0375) > 0.16 or not (1.35 < c.z < 1.95):
        continue
    k = (round(c.z, 2)//0.03*0.03, 'back' if c.y > 0.0 else 'front')
    w = {names[g.group]: g.weight for g in v.groups}
    b = bins.setdefault(k, {'n': 0, 'y': [9, -9]})
    b['n'] += 1
    b['y'] = [min(b['y'][0], c.y), max(b['y'][1], c.y)]
    for n in ('chest', 'neck', 'head', 'spine', 'L upperarm', 'R upperarm', 'L shoulder', 'R shoulder'):
        b[n] = b.get(n, 0)+w.get(n, 0)
out = {}
for k in sorted(bins):
    b = bins[k]
    out[f'{k[0]:.2f} {k[1]}'] = {n: round(v/b['n'], 2) for n, v in b.items() if n not in ('n', 'y') and v > 0.01} | {'n': b['n'], 'y': [round(x, 3) for x in b['y']]}
bones = {n: [[round(x, 3) for x in rig.data.bones[n].head_local], [round(x, 3) for x in rig.data.bones[n].tail_local]]
         for n in ('spine', 'chest', 'neck', 'head', 'L scapula', 'R scapula')}
print('NECK', json.dumps({'bins': out, 'bones': bones}, indent=0))
