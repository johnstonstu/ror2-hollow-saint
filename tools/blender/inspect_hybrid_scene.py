"""Read-only saved hybrid integrity report; pair with actual render review.

Success criteria: finite nonempty editable geometry, five named review cameras,
packed source textures, no linked external libraries, and unchanged originals.
Counts do not establish concept fidelity or animation readiness.
"""
import bpy
import hashlib
import json
import math
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree

if not bpy.app.background:
    raise RuntimeError('Use isolated background Blender to inspect saved scene')
root = Path(__file__).resolve().parents[2]
scene_path = Path(bpy.data.filepath)
assert scene_path.parent == root/'art'/'hybrid', 'Expected a saved hybrid scene'
originals = {
    'output/higgsfield-hollow-saint/hollow-saint-sam3d.glb':
        '66D30C21AE76185148AC62EB63E602FE30E19727D658AEEC72BC61D87DB5925A',
    'art/refinement/hollow-saint-refinement-v6.blend':
        'CB90845A0C912E86673BA676F153E73F60A4A7EA64C489721A2D58A00094D15D'
}
for relative, expected in originals.items():
    digest = hashlib.sha256((root/relative).read_bytes()).hexdigest().upper()
    assert digest == expected, 'Original changed: '+relative
assert not bpy.data.libraries, 'Unexpected linked external Blender data'
cameras = ['Front orthographic', 'Hero three quarter', 'Side orthographic',
           'Back orthographic', 'Simulated gameplay distance']
for camera in cameras:
    assert camera in bpy.data.objects and bpy.data.objects[camera].type == 'CAMERA', camera
parts = [o for o in bpy.context.scene.objects if o.type in ['MESH','CURVE']
         and not o.name.startswith('Warm gray studio ground')]
assert parts, 'Empty hybrid'
assert not any(o.type == 'ARMATURE' for o in bpy.context.scene.objects), 'Unexpected armature'
halo = [o for o in parts if o.name.startswith('HALO | independent copper arc ')]
assert len(halo) == 4 and len({o.data.as_pointer() for o in halo}) == 4, 'Four independently editable halo arcs required'
for side in ['L','R']:
    for digit in ['index','middle','ring','little','thumb']:
        segments = [o for o in parts if o.name.startswith(f'{side} HAND | {digit} segment ')]
        assert len(segments) == 3, f'{side} {digit}: three articulated segments required'
# Regression guard from hybrid v1: a hand cut without a lower Z limit removed feet.
# Compare unique world-space vertices so legitimate UV seam welding is allowed.
bodies = [o for o in parts if o.name.startswith('HF BODY |')]
assert len(bodies) == 1, 'Expected one preserved HF body'
with bpy.data.libraries.load(str(root/'art'/'comparison'/'higgsfield-sam3d-review.blend'), link=False) as (available, loaded):
    assert 'geometry_0' in available.objects
    loaded.objects = ['geometry_0']
source_body = loaded.objects[0]
def lower_vertices(obj):
    coords = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
    # Source fingertips reach Z=.644; .62 isolates lower legs/feet from hands.
    return {tuple(co) for co in coords if co.z < .62}
expected_lower = lower_vertices(source_body)
actual_lower = lower_vertices(bodies[0])
tree = KDTree(len(actual_lower))
for index, co in enumerate(actual_lower):
    tree.insert(Vector(co), index)
tree.balance()
lower_pairs = [(co, tree.find(Vector(co))[2]) for co in expected_lower]
distances = [distance for co, distance in lower_pairs]
# Derived mesh welds vertices within 2e-6 m. A 3e-6 allowance accounts for
# that operation without a decimal-rounding boundary creating a false failure.
missing_lower = sum(distance > .000003 for distance in distances)
assert not missing_lower, ('Lower-leg/foot regression: '+str(missing_lower)+
                          ' source vertices missing '+str([p for p in lower_pairs if p[1]>.000003][:8]))
bpy.data.objects.remove(source_body)
depsgraph = bpy.context.evaluated_depsgraph_get()
records = []
for obj in parts:
    evaluated = obj.evaluated_get(depsgraph)
    data = evaluated.to_mesh()
    try:
        assert data is not None and data.vertices, 'Empty part: '+obj.name
        assert all(math.isfinite(c) for v in data.vertices for c in v.co), obj.name
        data.calc_loop_triangles()
        records.append({'name': obj.name, 'type': obj.type,
                        'evaluated_triangles': len(data.loop_triangles)})
    finally:
        evaluated.to_mesh_clear()
used_images = set()
for obj in parts:
    for slot in obj.material_slots:
        material = slot.material
        if material and material.node_tree:
            for node in material.node_tree.nodes:
                if node.type == 'TEX_IMAGE' and node.image:
                    used_images.add(node.image)
for image in used_images:
    assert image.packed_file, 'Unpacked texture dependency: '+image.name
report = {'saved_scene': str(scene_path), 'result': 'PASS',
          'preserved_lower_leg_and_foot_unique_vertices': len(expected_lower),
          'maximum_lower_vertex_displacement_m': max(distances),
          'independent_halo_arcs': len(halo), 'fingers_per_hand': 5,
          'unchanged_originals': list(originals), 'review_cameras': cameras,
          'packed_textures': [i.name for i in used_images], 'parts': records,
          'evaluated_triangles': sum(r['evaluated_triangles'] for r in records),
          'scope': 'Saved-scene integrity only; visual audit, rigging and game tests separate.'}
scene_path.with_suffix('.qa.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('HYBRID_SAVED_FILE_QA '+json.dumps({k:v for k,v in report.items() if k!='parts'}, indent=2))
