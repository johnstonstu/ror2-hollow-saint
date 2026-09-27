"""Read-only saved-scene checks; visual fidelity still requires image review.

Success criteria: reopen editable original parts with finite evaluated geometry,
four independent halo arcs, five articulated digits per hand, five review cameras,
and no linked external assets. Run using Blender --background FILE --python SCRIPT.
"""
import json
import math
import bpy

if not bpy.app.background:
    raise RuntimeError('Saved-file verification must run in isolated background Blender')

collections = [c for c in bpy.data.collections if c.name.startswith('HOLLOW SAINT |')]
assert len(collections) == 1, 'Expected one editable survivor collection'
parts = list(collections[0].objects)
assert parts, 'Survivor collection is empty'
assert not bpy.data.libraries, 'Model unexpectedly depends on externally linked assets'
assert not any(o.type == 'ARMATURE' for o in parts), 'Review study unexpectedly contains a rig'
halo = [o for o in parts if o.name.startswith('Halo | copper quadrant ')]
assert len(halo) == 4, 'Expected four separate halo arcs'
assert len({o.data.as_pointer() for o in halo}) == 4, 'Halo arcs must be independently editable'
expected_cameras = ['Front orthographic', 'Hero three quarter', 'Side orthographic',
                    'Back orthographic', 'Simulated gameplay distance']
for name in expected_cameras:
    assert name in bpy.data.objects and bpy.data.objects[name].type == 'CAMERA', name
for side in ['L', 'R']:
    for digit in ['index', 'middle', 'ring', 'little', 'thumb']:
        segments = [o for o in parts if o.name.startswith(f'{side} {digit} | phalanx ')]
        assert len(segments) >= 2, f'{side} {digit}: missing articulated finger segments'

depsgraph = bpy.context.evaluated_depsgraph_get()
vertices = 0
triangles = 0
for obj in parts:
    assert obj.type in ['MESH', 'CURVE'], f'Unexpected part type: {obj.name}'
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    try:
        assert mesh is not None and mesh.vertices, f'Empty evaluated geometry: {obj.name}'
        for vertex in mesh.vertices:
            assert all(math.isfinite(v) for v in vertex.co), f'Invalid coordinate: {obj.name}'
        mesh.calc_loop_triangles()
        vertices += len(mesh.vertices)
        triangles += len(mesh.loop_triangles)
    finally:
        evaluated.to_mesh_clear()

print('REFINEMENT_SAVED_FILE_QA ' + json.dumps({
    'saved_file': bpy.data.filepath,
    'editable_parts': len(parts),
    'evaluated_vertices': vertices,
    'evaluated_triangles': triangles,
    'halo_arcs': len(halo),
    'cameras': len(expected_cameras),
    'result': 'PASS',
    'scope': 'Static saved-scene integrity only. Fidelity, deformation, export, and game behavior are separate.'
}, indent=2))
