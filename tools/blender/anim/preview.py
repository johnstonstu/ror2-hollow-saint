"""Build one clip module on a fresh v18 (never saved), run locomotion QA and render review frames.

Run:
  blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/preview.py -- run
Options after the module name:
  --views hero,side     cameras (hero, side, front, back, gameplay)
  --step 1              render every Nth frame
  --res 480x600         motion frame size
  --no-render           QA only
  --stills              one Cycles hero+side still at each clip's first marker
  --ortho 2.75          camera ortho scale override (glide needs more room)
  --frames 1,5          render only these frames (inspection)
  --clips a,b           render only these clip slugs (QA still covers the whole module)
A module may set PREVIEW_ORTHO to change its default ortho scale.
Output: art/anim/wip/<module>/<clip-slug>/  (owned by whoever owns the module)
"""
import bpy
import importlib
import json
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
sys.path.insert(0, str(Path(__file__).parent/'clips'))
import hs_anim as H

args = sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
module = args[0]


def opt(name, default):
    return args[args.index(name)+1] if name in args else default


VIEWS = {'hero': 'Hero three quarter', 'side': 'Side orthographic', 'front': 'Front orthographic',
         'back': 'Back orthographic', 'gameplay': 'Simulated gameplay distance'}
VIEWS['chase'] = 'Preview chase camera'
views = opt('--views', 'hero,side').split(',')
step = int(opt('--step', '1'))
res = tuple(int(v) for v in opt('--res', '480x600').split('x'))
ortho = float(opt('--ortho', '0'))

p = H.open_start()
import clearance
import contact
import handorient
import handqa
clr = clearance.Clearance()
con = contact.Contact()
hq = handqa.HandQA(p)
oq = handorient.OrientQA(p.rig)
p.reset()
p.update()
print('HANDQA REST', json.dumps(hq.calibrate_rest()), flush=True)
rest_hand = oq.calibrate_rest()
print('HAND ORIENT REST handedness', json.dumps(rest_hand), flush=True)
if not (rest_hand['L'] < 0 < rest_hand['R']):
    raise RuntimeError(f'hands are mirror-handed at rest: {rest_hand}')
print('CONTACT REST', json.dumps(con.calibrate_rest()), flush=True)
scene = bpy.context.scene
if 'chase' in views:
    # RoR2-like third-person view: behind and above, looking past the character.
    cam = bpy.data.objects.new(VIEWS['chase'], bpy.data.cameras.new(VIEWS['chase']))
    cam.data.lens = 32
    cam.location = (0.35, 5.2, 2.9)
    cam.rotation_euler = (H.Vector((0, -2.0, 1.2))-cam.location).to_track_quat('-Z', 'Y').to_euler()
    scene.collection.objects.link(cam)
scene.render.fps = H.FPS
mod = importlib.import_module(module)
built = mod.build(p)
rig = p.rig
body = bpy.data.objects[H.BODY]
out_root = H.ROOT/'art/anim/wip'/module
out_root.mkdir(parents=True, exist_ok=True)


def slug(title):
    return ''.join(c if c.isalnum() else '-' for c in title.lower()).strip('-')


report = []
for act, info in built:
    rig.animation_data.action = act
    frames = list(range(int(act.frame_range[0]), int(act.frame_range[1])+1))
    balls = {s: [] for s in H.SIDES}
    tips = {s: [] for s in H.SIDES}
    min_body_z = 9.0
    finite = True
    arm_frames = []
    contact_frames = []
    hq.reset()
    oq.reset()
    for f in frames:
        scene.frame_set(f)
        p.update()
        for s in H.SIDES:
            m = p.pb[f'{s} toe'].matrix
            balls[s].append(m.translation.copy())
            tips[s].append((m @ H.Vector((0, p.pb[f'{s} toe'].length, 0))).z)
        dg = bpy.context.evaluated_depsgraph_get()
        ev = body.evaluated_get(dg)
        mesh = ev.to_mesh()
        zs = [(body.matrix_world @ v.co).z for v in mesh.vertices]
        finite &= all(math.isfinite(z) for z in zs)
        min_body_z = min(min_body_z, min(zs))
        ev.to_mesh_clear()
        arm_frames.append((f, clr.frame()))
        contact_frames.append((f, con.frame()))
        hq.frame(f)
        oq.frame(f)
    speed = info.get('speed_mps')
    # Travel direction in world XY (character faces -Y; its left is +X). Planted feet move opposite.
    travel = {'forward': (0, -1), 'backward': (0, 1), 'left': (1, 0), 'right': (-1, 0)}.get(
        info.get('direction', 'forward'), (0, -1))
    travel = info.get('travel', travel)
    slide = 0.0
    planted_frames = 0
    for s in H.SIDES:
        b = balls[s]
        for i in range(len(b)-1):
            if b[i].z < 0.06 and b[i+1].z < 0.06:
                planted_frames += 1
                k = (speed or 0)/H.FPS
                d = b[i+1]-b[i]
                slide = max(slide, math.hypot(d.x+travel[0]*k, d.y+travel[1]*k))
    info.update({'module': module, 'action': act.name, 'planted_frame_pairs': planted_frames,
                 'max_planted_slide_m_per_frame': round(slide, 4), 'min_body_z': round(min_body_z, 4),
                 'min_toe_tip_z': round(min(min(v) for v in tips.values()), 4), 'finite': finite})
    info.update(clearance.summarize(arm_frames, info.get('kind')))
    info.update(hq.summarize(info['loop'], info.get('finger_accents', ())))
    info.update(contact.summarize(contact_frames, con.rest, info.get('hand_contacts', ())))
    info.update(oq.summarize(info['loop']))
    info['status'] = 'PASS' if (finite and info['bake_error_m'] < 1e-3 and info['ik_miss_m'] < 0.01
                                and min_body_z > H.GROUND_Z-0.012 and info['arm_clear_ok'] is not False
                                and info['hand_qa_summary']['ok'] and info['hand_contact_ok']
                                and info['hand_orient_ok']) else 'CHECK'
    report.append(info)
    print('QA', json.dumps(info), flush=True)

(out_root/'qa.json').write_text(json.dumps(report, indent=2), encoding='utf-8')

if '--clips' in args:
    wanted = set(opt('--clips', '').split(','))
    built = [(a, i) for a, i in built if slug(i['title']) in wanted]
ortho = ortho or getattr(mod, 'PREVIEW_ORTHO', 0)
if '--no-render' not in args:
    H.eevee(scene, 12)
    for cam in VIEWS.values():
        c = bpy.data.objects.get(cam)
        if c and ortho:
            c.data.ortho_scale = ortho
    for act, info in built:
        rig.animation_data.action = act
        d = out_root/slug(info['title'])
        d.mkdir(parents=True, exist_ok=True)
        frames = list(range(int(act.frame_range[0]), int(act.frame_range[1])+1))
        if info['loop']:
            frames = frames[:-1]
        if '--frames' in args:
            frames = [int(v) for v in opt('--frames', '').split(',')]
            step = 1
            d = d/'inspect'
            d.mkdir(exist_ok=True)
        for view in views:
            for n, f in enumerate(frames[::step]):
                scene.frame_set(f)
                H.render_still(scene, VIEWS[view], d/f'{view}-{n:03}.png', res)
            print('RENDERED', info['title'], view, flush=True)
        # Frames left over from a longer earlier render move aside (nothing is deleted), so the
        # sheets and GIFs only pick up this render.
        fresh = {f'{v}-{n:03}.png' for v in views for n in range(len(frames[::step]))}
        for old in d.glob('*-[0-9][0-9][0-9].png'):
            if old.name not in fresh:
                (d/'stale').mkdir(exist_ok=True)
                old.replace(d/'stale'/old.name)
        (d/'clip.json').write_text(json.dumps({**info, 'step': step, 'views': views,
                                                'rendered_frames': frames[::step]}, indent=2), encoding='utf-8')
    if '--stills' in args:
        H.gpu_cycles(scene, 64)
        for act, info in built:
            rig.animation_data.action = act
            d = out_root/slug(info['title'])
            f = min(info['markers'].values()) if info['markers'] else int(act.frame_range[0])
            scene.frame_set(f)
            for view in ('hero', 'side'):
                H.render_still(scene, VIEWS[view], d/f'still-{view}.png', (1100, 1400))
print('PREVIEW DONE', module, flush=True)
