"""Close-up hand renders for the 9c hand pass (fresh v18, never saved).

Run:
  blender --background --factory-startup --python tools/blender/anim/hand_closeup.py -- <module> <clip-slug>[,slug] [--step 1] [--res 320x320] [--context]
Two ortho views per hand, framed in the hand's own frame so the fingers stay upright in the image:
  back  - back of the hand from the thumb side (thumb tuck, knuckle cascade)
  palm  - palm side (curl, interpenetration, thumb against the palm)
--context adds an unclipped, wider world-axis view from the front-outside of each hand, so hand/body
contact (thighs, torso, tabard) is visible too.
Output: art/anim/wip/hands/<clip-slug>/<L|R>-<view>-NNN.png + clip.json; tile with hand_sheet.py.
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
module, wanted = args[0], set(args[1].split(','))


def opt(name, default):
    return args[args.index(name)+1] if name in args else default


step = int(opt('--step', '1'))
res = tuple(int(v) for v in opt('--res', '320x320').split('x'))
SCALE, DIST = 0.30, 1.2


def slug(title):
    return ''.join(c if c.isalnum() else '-' for c in title.lower()).strip('-')


for kv in [args[i+1] for i, a in enumerate(args) if a == '--set']:   # inspection overrides: module.NAME=value
    name, value = kv.split('=')
    mname, attr = name.rsplit('.', 1)
    setattr(importlib.import_module(mname), attr, float(value))
out_name = opt('--out', '')
p = H.open_start()
scene = bpy.context.scene
scene.render.fps = H.FPS
mod = importlib.import_module(module)
built = [(a, i) for a, i in mod.build(p) if slug(i['title']) in wanted]
cam = bpy.data.objects.new('Hand closeup', bpy.data.cameras.new('Hand closeup'))
cam.data.type = 'ORTHO'
cam.data.ortho_scale = SCALE
cam.data.clip_start, cam.data.clip_end = DIST-0.08, DIST+0.08   # slab around the hand: body/tabard never occlude
scene.collection.objects.link(cam)
ctx = bpy.data.objects.new('Hand context', bpy.data.cameras.new('Hand context'))
ctx.data.type = 'ORTHO'
ctx.data.ortho_scale = 0.60
scene.collection.objects.link(ctx)
H.eevee(scene, 12)

# Thumb side of each hand in the hand bone's local frame (-Z is the palm side of the hand bone).
p.rig.animation_data.action = None
p.reset()
p.update()
THUMB_X = {}
for s in H.SIDES:
    local = p.pb[f'{s} hand'].matrix.inverted() @ p.pb[f'{s} thumb.1'].matrix.translation
    THUMB_X[s] = 1.0 if local.x > 0 else -1.0
VIEWS = {'back': lambda s: Vector((0.55*THUMB_X[s], 0.0, 0.85)), 'palm': lambda s: Vector((0.25*THUMB_X[s], 0.0, -0.95))}


def place(side, view):
    m = p.pb[f'{side} hand'].matrix
    tip = p.pb[f'{side} middle.2'].matrix.translation
    c = 0.5*(m.translation+tip)
    rot = m.to_3x3().normalized()
    off = (rot @ VIEWS[view](side)).normalized()
    up = rot @ Vector((0, 1, 0))
    z = off                                   # camera looks down -Z
    x = up.cross(z).normalized()
    y = z.cross(x)
    cam.matrix_world = Matrix.Translation(c+DIST*off) @ Matrix((x, y, z)).transposed().to_4x4()


def place_context(side):
    c = p.pb[f'{side} hand'].matrix.translation
    z = Vector((0.75*H.sign(side), -0.66, 0.0)).normalized()
    x = Vector((0, 0, 1)).cross(z).normalized()
    y = z.cross(x)
    ctx.matrix_world = Matrix.Translation(c+2.5*z) @ Matrix((x, y, z)).transposed().to_4x4()


out = H.ROOT/'art/anim/wip/hands'
for act, info in built:
    p.rig.animation_data.action = act
    d = out/(slug(info['title'])+(f'-{out_name}' if out_name else ''))
    d.mkdir(parents=True, exist_ok=True)
    frames = list(range(int(act.frame_range[0]), int(act.frame_range[1])+1))
    if info['loop']:
        frames = frames[:-1]
    frames = frames[::step]
    for n, f in enumerate(frames):
        scene.frame_set(f)
        p.update()
        for s in H.SIDES:
            for view in VIEWS:
                place(s, view)
                H.render_still(scene, cam.name, d/f'{s}-{view}-{n:03}.png', res)
            if '--context' in args:
                place_context(s)
                H.render_still(scene, ctx.name, d/f'{s}-context-{n:03}.png', res)
    (d/'clip.json').write_text(json.dumps({'title': info['title'], 'step': step, 'rendered_frames': frames,
                                           'markers': info.get('markers', {}),
                                           'finger_accents': info.get('finger_accents', [])}, indent=2),
                               encoding='utf-8')
    print('HANDS', info['title'], len(frames), flush=True)
print('HANDS DONE', module, flush=True)
