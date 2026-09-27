"""Workbench stills of the torso: body grey, L arm green, R arm yellow, TABARD red/blue (nothing saved).
blender -b --factory-startup --python body_render.py -- <out_dir> "<title>:<frame>;..." <module> [dz] [scale]
dz: camera centre height above the pelvis (default 0.15); scale: ortho size (default 1.2)."""
import importlib
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim/clips'))
import hs_anim as H

a = sys.argv[sys.argv.index('--')+1:]
out = H.ROOT/a[0]
out.mkdir(parents=True, exist_ok=True)
specs = a[1].split(';')
dz = float(a[3]) if len(a) > 3 else 0.15
scale = float(a[4]) if len(a) > 4 else 1.2
p = H.open_start()
acts = {i['title']: act for act, i in importlib.import_module(a[2]).build(p)}
p.ik(0, 0)
p.followers(False)
sc = bpy.context.scene
sc.render.engine = 'BLENDER_WORKBENCH'
sh = sc.display.shading
sh.light, sh.color_type, sh.show_shadows, sh.show_cavity = 'STUDIO', 'OBJECT', False, True
sc.render.resolution_x = sc.render.resolution_y = 520
for o in bpy.data.objects:
    if o.type != 'MESH':
        continue
    n = o.name
    o.color = ((0.9, 0.2, 0.15, 1) if n.startswith('TABARD |') and ' front ' in n else
               (0.2, 0.4, 0.95, 1) if n.startswith('TABARD |') else
               (0.3, 0.8, 0.3, 1) if n.startswith('L ') else
               (0.95, 0.8, 0.2, 1) if n.startswith('R ') else (0.55, 0.55, 0.58, 1))
    if n.startswith(('VFX', 'Warm')):
        o.hide_render = True
cam_d = bpy.data.cameras.new('br cam')
cam_d.type = 'ORTHO'
cam_d.ortho_scale = scale
cam = bpy.data.objects.new('br cam', cam_d)
sc.collection.objects.link(cam)
sc.camera = cam
VIEWS = {'front': Vector((0, -1, 0)), 'side': Vector((1, 0, 0)), 'q': Vector((0.8, -0.8, 0.3)),
         'side-r': Vector((-1, 0, 0))}
for spec in specs:
    t, f = spec.rsplit(':', 1)
    p.rig.animation_data.action = acts[t]
    sc.frame_set(int(f))
    p.update()
    c = (p.rig.matrix_world @ p.pb['pelvis'].matrix).translation.copy()
    c.z += dz
    for v, d in VIEWS.items():
        d = d.normalized()
        cam.location = c+d*3.0
        cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = str(out/f"{spec.replace(':', '_').replace(' ', '-')}-{v}.png")
        bpy.ops.render.render(write_still=True)
print('BODY RENDER DONE', out)
