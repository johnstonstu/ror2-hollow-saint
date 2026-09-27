"""Reopen v10, validate every animation frame, and render review images."""
import bpy
import json
import math
import hashlib
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'art/hybrid/v10-animation'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'art/hybrid/hollow-saint-hybrid-v10.blend'))
scene = bpy.context.scene
rig = bpy.data.objects['Hollow Saint | v8 rig']
body = bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
catalog = json.loads((OUT/'catalog.json').read_text())
studio = [o for o in scene.objects if o.type in {'CAMERA', 'LIGHT'}]
studio.append(bpy.data.objects['Warm gray studio ground'])
baseline = {o.name: o.matrix_world.copy() for o in studio}
report = {'actions': [], 'source_hashes_unchanged': False, 'weight_max_error': 0}
deform = {g.index for g in body.vertex_groups if g.name in rig.data.bones
          and rig.data.bones[g.name].use_deform}
for v in body.data.vertices:
    total = sum(g.weight for g in v.groups if g.group in deform)
    report['weight_max_error'] = max(report['weight_max_error'], abs(total-1))
assert report['weight_max_error'] < 1e-5


def snapshot():
    return [v for p in rig.pose.bones for row in p.matrix_basis for v in row]


for spec in catalog:
    rig.animation_data.action = bpy.data.actions[spec['name']]
    scene.frame_set(1)
    start = snapshot()
    max_motion = 0
    lower_drift = 0
    for frame in range(1, spec['end']+1):
        scene.frame_set(frame)
        deps = bpy.context.evaluated_depsgraph_get()
        evaluated = body.evaluated_get(deps)
        mesh = evaluated.to_mesh()
        assert all(math.isfinite(c) for v in mesh.vertices for c in v.co)
        # These clips keep the pelvis and legs planted. Any lower-body movement
        # would expose accidental arm/hand influence, even if coordinates are finite.
        for vertex in mesh.vertices:
            original = body.data.vertices[vertex.index]
            if original.co.z < .94:
                lower_drift = max(lower_drift, (vertex.co-original.co).length)
        evaluated.to_mesh_clear()
        for o in studio:
            assert max(abs(o.matrix_world[i][j]-baseline[o.name][i][j])
                       for i in range(4) for j in range(4)) < 1e-6, o.name
        max_motion = max(max_motion, max(abs(a-b) for a, b in zip(start, snapshot())))
    endpoint_error = max(abs(a-b) for a, b in zip(start, snapshot()))
    assert endpoint_error < 1e-5, (spec['name'], endpoint_error)
    assert max_motion > .005, spec['name']
    assert lower_drift < 1e-5, (spec['name'], lower_drift)
    report['actions'].append({'name': spec['name'], 'frames_checked': spec['end'],
                              'endpoint_error': endpoint_error, 'motion': max_motion,
                              'lower_body_drift': lower_drift})
hashes = json.loads((OUT/'source-hashes.json').read_text())
assert all(hashlib.sha256((ROOT/'art/hybrid'/name).read_bytes()).hexdigest() == digest
           for name, digest in hashes.items())
report['source_hashes_unchanged'] = True
report['status'] = 'PASS numerical checks; visual inspection separate'
(OUT/'qa.json').write_text(json.dumps(report, indent=2))
print('QA PASS', flush=True)

scene.render.engine = 'BLENDER_EEVEE'
scene.eevee.taa_render_samples = 12
scene.render.image_settings.file_format = 'PNG'
scene.render.resolution_percentage = 100
scene.render.resolution_x = scene.render.resolution_y = 512
for i, spec in enumerate(catalog):
    rig.animation_data.action = bpy.data.actions[spec['name']]
    scene.frame_set(spec['preview'])
    for view, camera in [('hero', 'Hero three quarter'), ('side', 'Side orthographic')]:
        scene.camera = bpy.data.objects[camera]
        scene.render.filepath = str(OUT/f'pose-{i}-{view}.png')
        bpy.ops.render.render(write_still=True)
        print('POSE', i, view, flush=True)

if '--motion' in sys.argv:
    scene.camera = bpy.data.objects['Hero three quarter']
    scene.render.resolution_x = scene.render.resolution_y = 384
    scene.eevee.taa_render_samples = 8
    for i, spec in enumerate(catalog):
        rig.animation_data.action = bpy.data.actions[spec['name']]
        folder = OUT/f'clip-{i}'
        folder.mkdir(exist_ok=True)
        for n, frame in enumerate(range(1, spec['end']+1, 2)):
            scene.frame_set(frame)
            scene.render.filepath = str(folder/f'{n:03}.png')
            bpy.ops.render.render(write_still=True)
        print('MOTION', i, flush=True)
