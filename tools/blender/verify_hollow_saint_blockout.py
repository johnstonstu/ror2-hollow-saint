"""Read-only saved-scene QA; run after opening the blockout with headless Blender."""
import bpy
import json
from mathutils import Vector

scene = bpy.context.scene
collection = bpy.data.collections.get('HOLLOW SAINT - editable blockout parts')
assert collection is not None, 'Missing editable model collection'
objects = list(collection.objects)
halo = [o for o in objects if o.name.startswith('Halo quadrant ')]
assert len(halo) == 4, 'Exactly four independent halo quadrants required'
assert not any(o.type == 'ARMATURE' for o in scene.objects), 'Blockout should not claim a rig'
assert len([o for o in scene.objects if o.type == 'CAMERA']) == 4, 'Missing review cameras'
for side in ['L', 'R']:
    digits = [o for o in objects if o.name.startswith(side+' ') and o.name.endswith('proximal')]
    assert len(digits) == 5, f'{side} hand must have five distinct digits'


def bounds(items):
    coordinates = [obj.matrix_world @ Vector(v) for obj in items for v in obj.bound_box]
    low = [min(p[axis] for p in coordinates) for axis in range(3)]
    high = [max(p[axis] for p in coordinates) for axis in range(3)]
    return {'min': low, 'max': high, 'size': [high[i]-low[i] for i in range(3)]}


all_bounds = bounds(objects)
body_bounds = bounds([o for o in objects if not o.name.startswith('Halo ')])
mask_bounds = bounds([bpy.data.objects['Blank ivory mask']])
halo_bounds = bounds(halo)
gap = halo_bounds['min'][1] - mask_bounds['max'][1]
assert gap > .10, 'Halo needs visible rear clearance behind mask'
assert bpy.data.objects['Rear cyan charge node'] is not None
print('BLOCKOUT QA '+json.dumps({
    'saved_scene': bpy.data.filepath,
    'checks_passed': 8,
    'model_bounds_m': all_bounds,
    'body_bounds_m': body_bounds,
    'halo_to_mask_depth_clearance_m': gap,
    'mesh_objects': len(objects),
    'halo_quadrants': len(halo),
    'cameras': 4,
    'armatures': 0,
    'note': 'Static scene validation only; deformation, animation, export and gameplay untested.'
}, indent=2))
