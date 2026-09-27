"""World bbox / parent / weights summary of objects matching substrings (rest pose).
blender -b --factory-startup --python obj_probe.py -- <blend> "sub1,sub2" """
import bpy
import sys
import json
from mathutils import Vector

a = sys.argv[sys.argv.index('--')+1:]
bpy.ops.wm.open_mainfile(filepath=a[0])
rig = bpy.data.objects['Hollow Saint | v8 rig']
rig.data.pose_position = 'REST'
bpy.context.view_layer.update()
subs = a[1].split(',')
out = {}
for o in bpy.data.objects:
    if o.type != 'MESH' or not any(s in o.name for s in subs):
        continue
    cs = [o.matrix_world @ v.co for v in o.data.vertices]
    lo = [round(min(c[i] for c in cs), 3) for i in range(3)]
    hi = [round(max(c[i] for c in cs), 3) for i in range(3)]
    w = {}
    names = {g.index: g.name for g in o.vertex_groups}
    for v in o.data.vertices:
        for g in v.groups:
            w[names[g.group]] = w.get(names[g.group], 0)+g.weight
    tot = sum(w.values()) or 1
    out[o.name] = {'lo': lo, 'hi': hi, 'nv': len(cs), 'parent': o.parent.name if o.parent else None,
                   'ptype': o.parent_type, 'pbone': o.parent_bone, 'hide_render': o.hide_render,
                   'mods': [m.type for m in o.modifiers],
                   'w': {k: round(v/tot, 2) for k, v in sorted(w.items(), key=lambda x: -x[1])[:5]}}
print('OBJ', json.dumps(out, indent=0))
