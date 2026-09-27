"""Body mesh mirror test per region: nearest-vertex distance of each vertex's mirror (about x=m) to the body.
blender -b --factory-startup --python mirror_probe.py -- <blend>"""
import bpy
import sys
import json
import numpy as np
from mathutils.kdtree import KDTree
from mathutils import Vector
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
reg = [fullqa.region_of(g) for g in fullqa.dominant(body, deform)]
co = [body.matrix_world @ v.co for v in body.data.vertices]
kd = KDTree(len(co))
for i, c in enumerate(co):
    kd.insert(c, i)
kd.balance()
out = {}
for m in (0.0, H.MID_X, -0.02, -0.05):
    d = {}
    for i, c in enumerate(co):
        q = Vector((2*m-c.x, c.y, c.z))
        _, _, dist = kd.find(q)
        d.setdefault(reg[i], []).append(dist)
    out[str(m)] = {r: [round(float(np.median(v))*1000, 1), round(float(np.percentile(v, 90))*1000, 1)] for r, v in sorted(d.items())}
print('MIRROR', json.dumps(out, indent=0))
