"""List emissive materials on v18 and the objects using them (read-only, never saves).

Run: blender -b --factory-startup --python-exit-code 1 --python tools/blender/anim/mat_probe.py
"""
import bpy
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import hs_anim as H

bpy.ops.wm.open_mainfile(filepath=str(H.START))
users = {}
for o in bpy.data.objects:
    if o.type == 'MESH':
        for s in o.material_slots:
            if s.material:
                users.setdefault(s.material.name, []).append(o.name)
out = []
for m in bpy.data.materials:
    if not m.use_nodes:
        continue
    em = []
    for n in m.node_tree.nodes:
        if n.type == 'BSDF_PRINCIPLED':
            s = n.inputs['Emission Strength']
            c = n.inputs['Emission Color']
            if (s.default_value > 0 or s.is_linked) and (c.is_linked or max(c.default_value[:3]) > 0):
                em.append({'node': n.name, 'strength': s.default_value, 'strength_linked': s.is_linked,
                           'color': [round(v, 3) for v in c.default_value[:3]], 'color_linked': c.is_linked})
        elif n.type == 'EMISSION':
            em.append({'node': n.name, 'type': 'EMISSION', 'strength': n.inputs['Strength'].default_value,
                       'strength_linked': n.inputs['Strength'].is_linked,
                       'color': [round(v, 3) for v in n.inputs['Color'].default_value[:3]]})
    if em:
        out.append({'material': m.name, 'emission': em, 'objects': users.get(m.name, [])[:8],
                    'n_objects': len(users.get(m.name, []))})
print('MATPROBE', json.dumps(out, indent=1))
rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
print('MATPROBE rig', rig.name, 'toe bones', [b.name for b in rig.data.bones if 'toe' in b.name or 'foot' in b.name])
print('MATPROBE engine', bpy.context.scene.render.engine)
