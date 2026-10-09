"""Character-select portrait candidates: head-and-shoulders renders of the v37 model.

blender --background --factory-startup --python tools/icons/render_charselect_candidates.py -- <out_dir>
Writes raw_<name>.png (transparent, 768 px) per candidate; tools/icons/finish_charselect_candidates.py
grades them onto backgrounds at 256 px. Never saves the .blend.
"""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'art/anim/hollow-saint-anim-v37.blend'
RIG = 'Hollow Saint | v8 rig'
PREFIX = 'HS_anim | '
RES = 768

if not bpy.app.background:
    raise SystemExit('Run with blender --background')
out = (Path(sys.argv[sys.argv.index('--') + 1]) if '--' in sys.argv else ROOT / 'art/icons/charselect-candidates/_work').resolve()
out.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
rig = bpy.data.objects[RIG]
pb = rig.pose.bones

for ob in scene.objects:
    if ob.type in ('LIGHT', 'CURVE') or ob.name == 'Warm gray studio ground':
        ob.hide_render = True
socket = bpy.data.objects['Spear grip socket — hand locked']
for ob in socket.children_recursive:
    ob.hide_render = True

rig.animation_data.action = None
for t in rig.animation_data.nla_tracks:
    t.mute = True

scene.render.engine = 'BLENDER_EEVEE'
scene.eevee.taa_render_samples = 64
scene.render.film_transparent = True
scene.render.resolution_x = scene.render.resolution_y = RES
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.view_settings.view_transform = 'AgX'
scene.view_settings.look = 'AgX - Medium High Contrast'

world = bpy.data.worlds.new('Portrait world')
world.use_nodes = True
bg = world.node_tree.nodes['Background']
scene.world = world


def pose(title, frame):
    for b in pb:
        b.matrix_basis.identity()
    action = bpy.data.actions[PREFIX + title]
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for fc in bag.fcurves:
                    if not fc.data_path.startswith('pose.bones["'):
                        continue
                    name = fc.data_path.split('"')[1]
                    prop = fc.data_path.rsplit('.', 1)[1]
                    if name in pb and hasattr(pb[name], prop):
                        getattr(pb[name], prop)[fc.array_index] = fc.evaluate(frame)
    bpy.context.view_layer.update()


def bounds(prefix):
    deps = bpy.context.evaluated_depsgraph_get()
    pts = []
    for ob in scene.objects:
        if ob.type == 'MESH' and not ob.hide_render and ob.name.startswith(prefix):
            ev = ob.evaluated_get(deps)
            pts += [ev.matrix_world @ v.co for v in ev.data.vertices]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return lo, hi


lights = []


def light(name, kind, energy, color, pos, target, size=1.0):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    data.color = color
    if kind == 'AREA':
        data.size = size
    ob = bpy.data.objects.new(name, data)
    scene.collection.objects.link(ob)
    ob.location = pos
    ob.rotation_euler = (Vector(target) - Vector(pos)).to_track_quat('-Z', 'Y').to_euler()
    lights.append(ob)


def clear_lights():
    for ob in lights:
        data = ob.data
        bpy.data.objects.remove(ob, do_unlink=True)
        bpy.data.lights.remove(data)
    lights.clear()


def around(center, yaw, pitch, dist):
    """Point at yaw degrees from the model's front (-Y), positive toward +X, pitch up."""
    y, p = math.radians(yaw), math.radians(pitch)
    return center + Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p))) * dist


CANDIDATES = [
    # top: metres above the head tip (None = include the whole halo); bottom: metres below the neck base;
    # shift: horizontal frame offset in frame heights (positive moves the model left in frame).
    dict(name='a_slate_threequarter', clip='Select idle', frame=1, yaw=32, pitch=6, lens=70, top=0.10, bottom=0.20, shift=0.0,
         key=(40, 'warm', 70), rim=('cyan', 420, -140), fill=30, world=(0.010, 0.012, 0.018)),
    dict(name='b_storm_lowangle', clip='Idle combat', frame=1, yaw=-36, pitch=-5, lens=60, top=0.12, bottom=0.22, shift=-0.06,
         key=(-60, 'cool', 60), rim=('cyan', 600, 150), fill=20, world=(0.012, 0.008, 0.022)),
    dict(name='c_reliquary_closeup', clip='Idle', frame=1, yaw=16, pitch=3, lens=85, top=None, bottom=0.16, shift=0.0,
         key=(-30, 'warm', 80), rim=('cyan', 380, 160), fill=25, world=(0.018, 0.011, 0.008)),
]
TINTS = {'warm': (1.0, 0.88, 0.74), 'cool': (0.80, 0.88, 1.0), 'cyan': (0.35, 0.85, 1.0)}

report = []
for c in CANDIDATES:
    clear_lights()
    pose(c['clip'], c['frame'])
    neck = rig.matrix_world @ pb['neck'].head
    mask_lo, mask_hi = bounds('MASK')
    mid = (mask_lo + mask_hi) / 2
    top = bounds('HALO')[1].z + 0.02 if c['top'] is None else mask_hi.z + c['top']
    bottom = neck.z - c['bottom']
    center = Vector((mid.x, mid.y, (top + bottom) / 2))
    extent = top - bottom
    print('CHARSELECT_FRAME', c['name'], 'mask', tuple(round(v, 3) for v in mask_lo), tuple(round(v, 3) for v in mask_hi),
          'neck', round(neck.z, 3), 'top', round(top, 3), 'bottom', round(bottom, 3), flush=True)
    cam_data = bpy.data.cameras.new(c['name'])
    cam_data.lens = c['lens']
    cam_data.sensor_fit = 'VERTICAL'
    cam = bpy.data.objects.new(c['name'], cam_data)
    scene.collection.objects.link(cam)
    dist = extent / (cam_data.sensor_height / c['lens']) * 1.02
    cam.location = around(center, c['yaw'], c['pitch'], dist)
    cam.rotation_euler = (center - cam.location).to_track_quat('-Z', 'Y').to_euler()
    cam_data.shift_x = c['shift']
    scene.camera = cam

    kyaw, ktint, kpow = c['key']
    light('Key', 'AREA', kpow, TINTS[ktint], around(center, kyaw, 35, 2.2), center, 1.2)
    rtint, rpow, ryaw = c['rim']
    light('Rim', 'AREA', rpow, TINTS[rtint], around(center, ryaw, 25, 2.0), center, 0.6)
    light('Rim 2', 'AREA', rpow * 0.45, TINTS[rtint], around(center, -ryaw, 10, 2.0), center, 0.5)
    light('Fill', 'AREA', c['fill'], (0.75, 0.80, 1.0), around(center, c['yaw'], -25, 2.5), center, 2.0)
    bg.inputs['Color'].default_value = (*c['world'], 1)
    bg.inputs['Strength'].default_value = 1.0

    path = out / f"raw_{c['name']}.png"
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    report.append(dict(name=c['name'], clip=c['clip'], frame=c['frame'], yaw=c['yaw'], pitch=c['pitch'],
                       lens=c['lens'], extent=round(extent, 3), dist=round(dist, 3)))
    print('CHARSELECT_RENDER', c['name'], path, flush=True)

(out / 'render-report.json').write_text(json.dumps(report, indent=1), encoding='utf-8')
print('CHARSELECT_RENDER_DONE', len(report), flush=True)
