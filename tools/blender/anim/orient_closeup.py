"""9f hand orientation close-ups (background Blender, nothing saved).

Run:
  blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/orient_closeup.py --
      (--blend art/anim/hollow-saint-anim-vNN.blend | --module <clip module>) --tag before
      --clips "REST:1;Discharge:1,9,15;Arc Bolt right:all/2" [--res 300x300]
Colour-coded Workbench renders, four views per hand, framed on the hand with a thin clip slab (nothing
between camera and hand occludes it):
  front / back - world views from in front of / behind the character (up = world up)
  palm         - looking at the side the fingers curl toward (wrist at the top)
  thumb        - looking at the thumb edge of the hand (wrist at the top)
Colours: thumb red, index green, middle white, ring grey, little purple, palm/knuckles blue, tip lights
yellow. 'REST' is the bind pose. Output: art/anim/wip/hands/orientation/<tag>/<clip-slug>/<L|R>-<view>-NNN.png
+ clip.json (per-frame handedness, thumb_up, palm_fwd, gate; see handorient.py). Tile with orient_sheet.py.
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
import handorient

args = sys.argv[sys.argv.index('--')+1:]


def opt(name, default=None):
    return args[args.index(name)+1] if name in args else default


tag = opt('--tag', 'check')
res = tuple(int(v) for v in opt('--res', '300x300').split('x'))
SCALE, DIST, SLAB = 0.34, 1.2, 0.11
VIEWS = ('front', 'back', 'palm', 'thumb')
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
COL = {'thumb': (0.95, 0.12, 0.1, 1), 'index': (0.15, 0.8, 0.2, 1), 'middle': (0.95, 0.95, 0.95, 1),
       'ring': (0.55, 0.55, 0.58, 1), 'little': (0.6, 0.25, 0.85, 1)}
for o in bpy.data.objects:
    if o.type not in ('MESH', 'CURVE'):
        continue
    c = (0.42, 0.42, 0.45, 1)
    if 'HAND |' in o.name:
        tail = o.name.split('|', 1)[1]
        c = (0.2, 0.45, 1.0, 1)
        for dg, col in COL.items():
            if f' {dg} ' in f' {tail} ':
                c = col
        if 'tip light' in tail:
            c = (1.0, 0.85, 0.1, 1)
    if o.name.startswith('VFX') or 'backdrop' in o.name.lower() or 'ground' in o.name.lower():
        o.hide_render = True
    o.color = c
cam = bpy.data.objects.new('Orient closeup', bpy.data.cameras.new('Orient closeup'))
cam.data.type = 'ORTHO'
cam.data.ortho_scale = SCALE
cam.data.clip_start, cam.data.clip_end = DIST-SLAB, DIST+SLAB
scene.collection.objects.link(cam)
oq = handorient.OrientQA(rig)


def look(c, off, up):
    z = off.normalized()
    x = up.cross(z).normalized()
    y = z.cross(x)
    cam.matrix_world = Matrix.Translation(c+DIST*z) @ Matrix((x, y, z)).transposed().to_4x4()


def place(side, view):
    pb, mw = rig.pose.bones, rig.matrix_world
    c = 0.5*(mw @ pb[f'{side} hand'].head+mw @ pb[f'{side} middle.2'].head)
    d, n, r = handorient.hand_axes(rig, side)
    if view == 'front':
        look(c, Vector((0, -1, 0)), Vector((0, 0, 1)))
    elif view == 'back':
        look(c, Vector((0, 1, 0)), Vector((0, 0, 1)))
    elif view == 'palm':
        look(c, n, -d)
    else:
        look(c, r, -d)


def slug(title):
    return ''.join(ch if ch.isalnum() else '-' for ch in title.lower()).strip('-')


out = H.ROOT/'art/anim/wip/hands/orientation'/tag
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
    oq.reset()
    for i, f in enumerate(frames):
        scene.frame_set(f)
        bpy.context.view_layer.update()
        oq.frame(f)
        for s in H.SIDES:
            for v in VIEWS:
                place(s, v)
                H.render_still(scene, cam.name, d/f'{s}-{v}-{i:03}.png', res)
    rows = [{'f': r['f'], **{s: {k: (round(x, 3) if isinstance(x, float) else x) for k, x in r[s].items()}
                             for s in H.SIDES}} for r in oq.rows]
    (d/'clip.json').write_text(json.dumps({'title': title, 'tag': tag, 'rendered_frames': frames, 'rows': rows},
                                          indent=1), encoding='utf-8')
    print('ORIENT', title, json.dumps(rows[:3]), flush=True)
print('ORIENT DONE', tag, flush=True)
