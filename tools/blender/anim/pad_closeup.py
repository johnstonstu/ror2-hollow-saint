"""9g shoulder-pad close-ups (background Blender, nothing saved).

Run:
  blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/pad_closeup.py --
      (--blend art/anim/hollow-saint-anim-vNN.blend | --module <clip module>) --tag before
      --clips "REST:1;Discharge:1,9,15;Idle:all/8" [--res 360x360] [--out art/anim/wip/shoulders]
      [--center x,y,z --scale 0.6]   (fixed world framing instead of the chest-relative one)
Colour-coded Workbench renders framed on the shoulders (chest-relative, so the framing rides with the torso):
  front / back / top, and outL / outR (from the character's left / right side, slightly in front).
Colours: pauldrons orange, halo arcs cyan, yoke parts yellow, scapula shells green, everything else grey.
Output: art/anim/wip/shoulders/<tag>/<clip-slug>/<view>-NNN.png + clip.json. Tile with pad_sheet.py.
"""
import bpy
import importlib
import json
import sys
from pathlib import Path
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).parent))
sys.path.insert(0, str(Path(__file__).parent/'clips'))
import hs_anim as H

args = sys.argv[sys.argv.index('--')+1:]


def opt(name, default=None):
    return args[args.index(name)+1] if name in args else default


tag = opt('--tag', 'check')
res = tuple(int(v) for v in opt('--res', '360x360').split('x'))
SCALE, DIST = 0.95, 3.0
VIEWS = {'front': Vector((0, -1, 0.12)), 'back': Vector((0, 1, 0.2)), 'top': Vector((0, -0.3, 1)),
         'outL': Vector((1, -0.45, 0.25)), 'outR': Vector((-1, -0.45, 0.25))}
H.require_background()
if opt('--blend'):
    bpy.ops.wm.open_mainfile(filepath=str(H.ROOT/opt('--blend')))
    rig = bpy.data.objects[H.RIG]
    actions = {a.name[len(H.PREFIX):]: a for a in bpy.data.actions if a.name.startswith(H.PREFIX)}
else:
    p = H.open_start()
    rig = p.rig
    actions = {i['title']: a for a, i in importlib.import_module(opt('--module')).build(p)}
    p.ik(0, 0)
    p.followers(False)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
sh = scene.display.shading
sh.light, sh.color_type, sh.show_shadows, sh.show_cavity = 'STUDIO', 'OBJECT', False, True
scene.render.film_transparent = False
for o in bpy.data.objects:
    if o.type not in ('MESH', 'CURVE'):
        continue
    n = o.name
    c = (0.42, 0.42, 0.45, 1)
    if 'SHOULDER |' in n:
        c = (1.0, 0.5, 0.1, 1)
    elif 'yoke' in n.lower():
        c = (1.0, 0.85, 0.15, 1)
    elif 'HALO |' in n:
        c = (0.1, 0.85, 0.95, 1)
    elif 'scapula' in n.lower() or (n.startswith('BACK |') and 'node' in n):
        c = (0.3, 0.8, 0.35, 1)
    if n.startswith('VFX') or 'backdrop' in n.lower() or 'ground' in n.lower() or max(o.dimensions) > 2.5:
        o.hide_render = True
    o.color = c
cam = bpy.data.objects.new('Pad closeup', bpy.data.cameras.new('Pad closeup'))
cam.data.type = 'ORTHO'
cam.data.ortho_scale = SCALE
cam.data.clip_start, cam.data.clip_end = 0.1, 2*DIST
scene.collection.objects.link(cam)


CENTER = Vector([float(v) for v in opt('--center').split(',')]) if opt('--center') else None
if opt('--scale'):
    cam.data.ortho_scale = float(opt('--scale'))


def place(view):
    pb, mw = rig.pose.bones, rig.matrix_world
    c = mw @ (0.5*(pb['L upperarm'].head+pb['R upperarm'].head))
    ch = (mw @ pb['chest'].matrix).to_3x3()
    rest = (mw @ rig.data.bones['chest'].matrix_local).to_3x3()
    r = ch @ rest.inverted()
    up = r @ Vector((0, 0, 1))
    c = c+up*0.12
    if CENTER is not None:
        r = Matrix.Identity(3)
        up = Vector((0, 0, 1))
        c = CENTER
    z = (r @ VIEWS[view]).normalized()
    upv = up if view != 'top' else r @ Vector((0, 1, 0))
    x = upv.cross(z).normalized()
    y = z.cross(x)
    cam.matrix_world = Matrix.Translation(c+DIST*z) @ Matrix((x, y, z)).transposed().to_4x4()


def slug(title):
    return ''.join(ch if ch.isalnum() else '-' for ch in title.lower()).strip('-')


out = H.ROOT/opt('--out', 'art/anim/wip/shoulders')/tag
for job in opt('--clips', 'REST:1').split(';'):
    title, frames = job.rsplit(':', 1)
    if title == 'REST':
        rig.data.pose_position = 'REST'
        rig.animation_data.action = None
        frames = [1]
    else:
        rig.data.pose_position = 'POSE'
        act = actions[title]
        rig.animation_data.action = act
        if getattr(rig.animation_data, 'action_slot', None) is None and len(getattr(act, 'slots', [])):
            rig.animation_data.action_slot = act.slots[0]
        lo, hi = int(act.frame_range[0]), int(act.frame_range[1])
        if frames.startswith('all'):
            step = int(frames.split('/')[1]) if '/' in frames else 1
            frames = list(range(lo, hi+1))[::step]
        else:
            frames = [int(v) for v in frames.split(',')]
    d = out/slug(title)
    d.mkdir(parents=True, exist_ok=True)
    for i, f in enumerate(frames):
        scene.frame_set(f)
        bpy.context.view_layer.update()
        for v in VIEWS:
            place(v)
            H.render_still(scene, cam.name, d/f'{v}-{i:03}.png', res)
    (d/'clip.json').write_text(json.dumps({'title': title, 'tag': tag, 'rendered_frames': frames,
                                           'views': list(VIEWS)}, indent=1), encoding='utf-8')
    print('PADCLOSE', title, frames, flush=True)
print('PADCLOSE DONE', tag, flush=True)
