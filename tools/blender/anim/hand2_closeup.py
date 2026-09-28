"""Hand close-ups and full-body frames straight from a saved checkpoint (item 12; background Blender, nothing saved).

Run (on a copy of the checkpoint):
  blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/hand2_closeup.py --
      --blend art/anim/wip/hands2/_work/v25-copy.blend --out art/anim/wip/hands2/v25
      --clips "REST:1;Idle:1,40;Run forward:all/2" [--res 260x260] [--body hero] [--body-res 400x500]
Per hand, Workbench colour-coded (thumb red, index green, middle white, ring grey, little purple, palm blue),
framed on the hand with a thin clip slab so nothing in front occludes it:
  profile - from the thumb edge, wrist at the top: the curl profile of every finger (claw vs relaxed arc)
  back    - the back of the hand, wrist at the top: spread and cascade
  palm    - the palm side, wrist at the top
plus `pair`: a front view of both hands and the thighs (world up, unclipped), for hanging-arm orientation.
--body <camera views, comma list> also renders EEVEE full-body frames (Hero three quarter / Side / chase).
Output: <out>/<clip-slug>/<L|R>-<view>-NNN.png, pair-NNN.png, body-<view>-NNN.png and clip.json.
Tile with hand2_sheet.py.
"""
import bpy
import json
import sys
from pathlib import Path
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).parent))
import hs_anim as H
import handorient

args = sys.argv[sys.argv.index('--')+1:]


def opt(name, default=None):
    return args[args.index(name)+1] if name in args else default


res = tuple(int(v) for v in opt('--res', '260x260').split('x'))
body_res = tuple(int(v) for v in opt('--body-res', '400x500').split('x'))
body_views = [v for v in opt('--body', '').split(',') if v]
SCALE, DIST, SLAB = 0.30, 1.2, 0.11
VIEWS = ('profile', 'back', 'palm')
BODY_CAMS = {'hero': 'Hero three quarter', 'side': 'Side orthographic', 'front': 'Front orthographic'}
H.require_background()
blend = Path(opt('--blend'))
bpy.ops.wm.open_mainfile(filepath=str(blend if blend.is_absolute() else H.ROOT/blend))
rig = bpy.data.objects[H.RIG]
actions = {a.name[len(H.PREFIX):]: a for a in bpy.data.actions if a.name.startswith(H.PREFIX)}
scene = bpy.context.scene
scene.render.fps = H.FPS
COL = {'thumb': (0.95, 0.12, 0.1, 1), 'index': (0.15, 0.8, 0.2, 1), 'middle': (0.95, 0.95, 0.95, 1),
       'ring': (0.55, 0.55, 0.58, 1), 'little': (0.6, 0.25, 0.85, 1)}
hidden = []
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
        if not o.hide_render:
            hidden.append(o)
    o.color = c
if 'chase' in body_views:
    cam = bpy.data.objects.new('Preview chase camera', bpy.data.cameras.new('Preview chase camera'))
    cam.data.lens = 32
    cam.location = (0.35, 5.2, 2.9)
    cam.rotation_euler = (Vector((0, -2.0, 1.2))-cam.location).to_track_quat('-Z', 'Y').to_euler()
    scene.collection.objects.link(cam)
    BODY_CAMS['chase'] = cam.name
cam = bpy.data.objects.new('Hand2 closeup', bpy.data.cameras.new('Hand2 closeup'))
cam.data.type = 'ORTHO'
scene.collection.objects.link(cam)


def workbench():
    scene.render.engine = 'BLENDER_WORKBENCH'
    sh = scene.display.shading
    sh.light, sh.color_type, sh.show_shadows, sh.show_cavity = 'STUDIO', 'OBJECT', False, True
    for o in hidden:
        o.hide_render = True


def look(c, off, up, scale, slab):
    z = off.normalized()
    x = up.cross(z).normalized()
    y = z.cross(x)
    cam.data.ortho_scale = scale
    cam.data.clip_start, cam.data.clip_end = (DIST-slab, DIST+slab) if slab else (0.05, 20.0)
    cam.matrix_world = Matrix.Translation(c+DIST*z) @ Matrix((x, y, z)).transposed().to_4x4()


def place(side, view):
    pb, mw = rig.pose.bones, rig.matrix_world
    c = 0.5*(mw @ pb[f'{side} hand'].head+mw @ pb[f'{side} middle.2'].head)
    d, n, r = handorient.hand_axes(rig, side)
    if view == 'profile':
        look(c, r, -d, SCALE, SLAB)
    elif view == 'back':
        look(c, -n, -d, SCALE, SLAB)
    else:
        look(c, n, -d, SCALE, SLAB)


def place_pair():
    pb, mw = rig.pose.bones, rig.matrix_world
    c = 0.5*(mw @ pb['L hand'].head+mw @ pb['R hand'].head)
    c.z -= 0.04
    span = (mw @ pb['L hand'].head-mw @ pb['R hand'].head).length
    look(c, Vector((0, -1, 0)), Vector((0, 0, 1)), max(0.75, span+0.35), 0)


def slug(title):
    return ''.join(ch if ch.isalnum() else '-' for ch in title.lower()).strip('-')


out = Path(opt('--out'))
out = out if out.is_absolute() else H.ROOT/out
oq = handorient.OrientQA(rig)
for job in opt('--clips', 'REST:1').split(';'):
    title, frames = job.rsplit(':', 1)
    if title == 'REST':
        rig.animation_data.action = None
        for p in rig.pose.bones:
            p.matrix_basis.identity()
        frames = [1]
    else:
        act = actions[title]
        rig.animation_data.action = act
        if getattr(rig.animation_data, 'action_slot', None) is None and len(getattr(act, 'slots', [])):
            rig.animation_data.action_slot = act.slots[0]
        lo, hi = int(act.frame_range[0]), int(act.frame_range[1])
        if frames.startswith('all'):
            step = int(frames.split('/')[1]) if '/' in frames else 1
            loop = json.loads(act.get('clip_json', '{}')).get('loop', False)
            frames = list(range(lo, hi+(0 if loop else 1)))[::step]
        else:
            frames = [int(v) for v in frames.split(',')]
    d = out/slug(title)
    d.mkdir(parents=True, exist_ok=True)
    oq.reset()
    workbench()
    for i, f in enumerate(frames):
        if title != 'REST':
            scene.frame_set(f)
        bpy.context.view_layer.update()
        oq.frame(f)
        for s in H.SIDES:
            for v in VIEWS:
                place(s, v)
                H.render_still(scene, cam.name, d/f'{s}-{v}-{i:03}.png', res)
        place_pair()
        H.render_still(scene, cam.name, d/f'pair-{i:03}.png', (res[0]*2, res[1]))
    if body_views:
        H.eevee(scene, 8)
        for o in hidden:
            o.hide_render = False
        for i, f in enumerate(frames):
            if title != 'REST':
                scene.frame_set(f)
            for v in body_views:
                H.render_still(scene, BODY_CAMS[v], d/f'body-{v}-{i:03}.png', body_res)
    rows = [{'f': r['f'], **{s: {k: (round(x, 3) if isinstance(x, float) else x) for k, x in r[s].items()}
                             for s in H.SIDES}} for r in oq.rows]
    (d/'clip.json').write_text(json.dumps({'title': title, 'blend': str(blend), 'rendered_frames': frames,
                                           'body_views': body_views, 'rows': rows}, indent=1), encoding='utf-8')
    print('HAND2', title, len(frames), flush=True)
print('HAND2 DONE', out, flush=True)
