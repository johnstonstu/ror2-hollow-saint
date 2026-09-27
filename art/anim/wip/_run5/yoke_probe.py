"""Rest extents (armature space, after the load fixes) of the yoke parts, pads and lower halo arcs."""
import sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT/'tools/blender/anim'))
import bpy
import json
import hs_anim as H

p = H.open_start()
rig = p.rig
rig.data.pose_position = 'REST'
bpy.context.view_layer.update()
out = {}
for o in bpy.data.objects:
    if o.type != 'MESH' or not any(k in o.name for k in ('yoke', 'pauldron', 'copper arc 2', 'copper arc 3')):
        continue
    dg = bpy.context.evaluated_depsgraph_get()
    ev = o.evaluated_get(dg)
    me = ev.to_mesh()
    pts = [o.matrix_world @ v.co for v in me.vertices]
    ev.to_mesh_clear()
    lo = [round(min(q[i] for q in pts), 4) for i in range(3)]
    hi = [round(max(q[i] for q in pts), 4) for i in range(3)]
    out[o.name] = {'min': lo, 'max': hi, 'parent': o.parent_bone or (o.parent.name if o.parent else None),
                   'type': o.parent_type, 'mods': [m.type for m in o.modifiers], 'verts': len(pts)}
    if 'yoke bar' in o.name:
        # x-profile: points near each end
        xs = sorted(pts, key=lambda q: q.x)
        out[o.name]['ends'] = [[round(c, 4) for c in xs[0]], [round(c, 4) for c in xs[-1]]]
print('YOKE', json.dumps(out, indent=1), flush=True)
