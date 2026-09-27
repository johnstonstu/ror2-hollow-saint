"""Side/front Workbench renders of the hips: body grey, TABARD front red, back blue (nothing saved).
blender -b --factory-startup --python tabard_render.py -- <out_dir> "<spec>;<spec>" [module]
spec: rest | swing:<front deg>:<back deg> | <title>:<frame>"""
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
p = H.open_start()
acts = {}
if len(a) > 2:
    acts = {i['title']: act for act, i in importlib.import_module(a[2]).build(p)}
    p.ik(0, 0)
    p.followers(False)
sc = bpy.context.scene
sc.render.engine = 'BLENDER_WORKBENCH'
sh = sc.display.shading
sh.light, sh.color_type, sh.show_shadows, sh.show_cavity = 'STUDIO', 'OBJECT', False, True
sc.render.resolution_x = sc.render.resolution_y = 480
for o in bpy.data.objects:
    if o.type != 'MESH':
        continue
    o.color = (0.9, 0.2, 0.15, 1) if o.name.startswith('TABARD |') and ' front ' in o.name else \
        (0.2, 0.4, 0.95, 1) if o.name.startswith('TABARD |') else (0.55, 0.55, 0.58, 1)
    if o.name.startswith(('VFX', 'Warm')):
        o.hide_render = True
cam_d = bpy.data.cameras.new('tr cam')
cam_d.type = 'ORTHO'
cam_d.ortho_scale = 1.0
cam = bpy.data.objects.new('tr cam', cam_d)
sc.collection.objects.link(cam)
sc.camera = cam
centre = Vector((H.MID_X, 0.05, 0.80))
VIEWS = {'side': Vector((1, 0, 0)), 'front': Vector((0, -1, 0)), 'q': Vector((0.8, -0.8, 0.15))}
for spec in specs:
    p.rig.animation_data.action = None
    p.reset()
    if spec.startswith('swing:'):
        _, fd, bd = spec.split(':')
        p.rot('tabard front.1', H.R(x=float(fd)))
        p.rot('tabard back.1', H.R(x=float(bd)))
    elif spec != 'rest':
        t, f = spec.rsplit(':', 1)
        p.rig.animation_data.action = acts[t]
        sc.frame_set(int(f))
    p.update()
    c = centre.copy()
    if spec not in ('rest',) and not spec.startswith('swing:'):
        c = p.pb['pelvis'].matrix.translation.copy()
        c.z -= 0.25
    for v, d in VIEWS.items():
        d = d.normalized()
        cam.location = c+d*3.0
        cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = str(out/f"{spec.replace(':', '_').replace(' ', '-')}-{v}.png")
        bpy.ops.render.render(write_still=True)
print('TABARD RENDER DONE', out)
