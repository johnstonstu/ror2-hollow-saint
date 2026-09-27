"""Render low-cost review frames for the v8 rig actions without changing the saved scene."""
import bpy
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FILE = ROOT / 'art/hybrid/hollow-saint-hybrid-v8.blend'
OUT = ROOT / 'art/hybrid/v8-review'
OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(FILE))
scene = bpy.context.scene
rig = bpy.data.objects['Hollow Saint | v8 rig']
scene.camera = bpy.data.objects['Hero three quarter']
scene.render.resolution_x = 720
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.filepath = str(OUT / 'unused.png')
scene.render.engine = 'BLENDER_EEVEE'
scene.eevee.taa_render_samples = 16
for action_name, frame, filename in [
    ('HS_v8 | Idle contained storm | 2s loop', 13, 'idle.png'),
    ('HS_v8 | Arc Bolt point and snap', 14, 'arc-bolt.png'),
    ('HS_v8 | Overcharge brace and release', 34, 'overcharge.png'),
]:
    rig.animation_data.action = bpy.data.actions[action_name]
    scene.frame_set(frame)
    scene.render.filepath = str(OUT / filename)
    bpy.ops.render.render(write_still=True)
    deps = bpy.context.evaluated_depsgraph_get()
    body = bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior'].evaluated_get(deps)
    mesh = body.to_mesh()
    world = [body.matrix_world @ v.co for v in mesh.vertices]
    print(filename, 'z-bounds', round(min(p.z for p in world),3), round(max(p.z for p in world),3))
    body.to_mesh_clear()
print('Rendered v8 review frames to', OUT)
