"""Read-only rig inventory for animation preparation."""
import bpy
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'art/hybrid/hollow-saint-hybrid-v9.blend'))
rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
rig.animation_data.action = None
for p in rig.pose.bones:
    p.matrix_basis.identity()
bpy.context.view_layer.update()
result = {'objects': [], 'bones': []}
for o in bpy.context.scene.objects:
    result['objects'].append({'name': o.name, 'type': o.type,
        'parent': o.parent.name if o.parent else None, 'bone': o.parent_bone,
        'position': list(o.matrix_world.translation), 'dimensions': list(o.dimensions)})
for b in rig.data.bones:
    result['bones'].append({'name': b.name, 'head': list(b.head_local),
        'tail': list(b.tail_local), 'matrix': [list(row) for row in b.matrix_local]})
(ROOT/'art/hybrid/v9-rig-inventory.json').write_text(json.dumps(result, indent=2))
print('INVENTORY SAVED', flush=True)
