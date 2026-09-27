"""Stitched transition previews: clips played back to back (hard cut or cross-faded, as Unity would) on a
fresh v18 (never saved), rendered, with handoff metrics. For judging how clips flow, not single clips.

Run:
  blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/stitch.py -- <spec.json> [--views hero,side,chase] [--res 400x500] [--no-render]
spec.json:
  {"name": "walk-run-glide", "modules": ["walk", "run", "glide"], "ortho": 3.4,
   "segments": [{"clip": "Walk forward", "start": 1, "frames": 52},
                {"clip": "Run forward", "frames": 25, "blend": 6, "sync": true},
                {"clip": "Glide enter", "frames": 13}]}
  start   first clip frame to play (default 1). Loops wrap over their unique frames; one-shots hold the end.
  blend   cross-fade frames from the previous segment (default 0 = hard cut, so the handoff itself must
          be seamless). The outgoing clip keeps playing through the fade.
  sync    the incoming loop picks up the outgoing loop's normalized phase (blend-tree style).
  mask    "upper": the segment only drives the upper body (spine up, arms, hands, halo; `UPPER` below)
          over the previous lower-body segment, like a Unity avatar-mask layer. "lower_from": title of the
          locomotion clip to keep playing underneath (with its own start/sync rules via "lower_start").
Writes art/anim/wip/transitions/<name>/<view>-NNN.png, clip.json (labels for sheet.py) and stitch.json:
per-frame sources/weights, and per handoff the worst world-space jumps of key bones: position step
(m/frame) and acceleration (m/frame^2) within +-2 frames of the handoff vs the worst inside the clips.
"""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).parent))
sys.path.insert(0, str(Path(__file__).parent/'clips'))
import hs_anim as H
import importlib

args = sys.argv[sys.argv.index('--')+1:]
spec = json.loads(Path(args[0]).read_text(encoding='utf-8'))


def opt(name, default):
    return args[args.index(name)+1] if name in args else default


VIEWS = {'hero': 'Hero three quarter', 'side': 'Side orthographic', 'front': 'Front orthographic',
         'back': 'Back orthographic', 'chase': 'Preview chase camera'}
views = opt('--views', 'hero,side,chase').split(',')
res = tuple(int(v) for v in opt('--res', '400x500').split('x'))
TRACK = ('pelvis', 'head', 'L hand', 'R hand', 'L toe', 'R toe', 'halo root', 'tabard front.3', 'tabard back.2',
         'L middle.3', 'R middle.3')
# Upper-body avatar mask (Unity): spine and everything parented under it (neck/head, halo, scapulae,
# pauldrons, arms, hands, fingers). Pelvis, legs and tabard stay with the locomotion layer.
UPPER_ROOTS = ('spine',)

p = H.open_start()
rig = p.rig
scene = bpy.context.scene
scene.render.fps = H.FPS
if 'chase' in views:
    cam = bpy.data.objects.new(VIEWS['chase'], bpy.data.cameras.new(VIEWS['chase']))
    cam.data.lens = 32
    cam.location = (0.35, 5.2, 2.9)
    cam.rotation_euler = (Vector((0, -2.0, 1.2))-cam.location).to_track_quat('-Z', 'Y').to_euler()
    scene.collection.objects.link(cam)
clips = {}
for m in spec['modules']:
    for act, info in importlib.import_module(m).build(p):
        clips[info['title']] = (act, info)


def upper_set():
    out = set()
    for b in rig.pose.bones:
        q = b
        while q is not None:
            if q.name in UPPER_ROOTS:
                out.add(b.name)
                break
            q = q.parent
    return out


UPPER = upper_set()
root = p.pb['root']
from vfx import PROPS
cache = {}


def sample(title, f):
    key = (title, f)
    if key not in cache:
        act, _ = clips[title]
        rig.animation_data.action = act
        scene.frame_set(f)
        pose = {b.name: (b.location.copy(), b.rotation_quaternion.copy(), b.scale.copy()) for b in p.pb}
        props = {k: float(root.get(k, 0.0)) for k in PROPS}
        cache[key] = (pose, props)
    return cache[key]


def clip_len(title):
    act, info = clips[title]
    a, b = int(act.frame_range[0]), int(act.frame_range[1])
    return (b-a) if info['loop'] else (b-a+1), a, info['loop']


def clip_frame(title, start, k):
    n, a, loop = clip_len(title)
    if loop:
        return a+((start-a+k) % n)
    return min(start+k, a+n-1)


def blend(pa, pb_, w, bones=None):
    out = dict(pa[0])
    for name, (l, q, s) in pb_[0].items():
        if bones is not None and name not in bones:
            continue
        la, qa, sa = pa[0][name]
        qb = q if qa.dot(q) >= 0 else -q
        out[name] = (la.lerp(l, w), qa.slerp(qb, w), sa.lerp(s, w))
    props = {k: pa[1][k]+(pb_[1][k]-pa[1][k])*w for k in PROPS}
    return out, props


def smooth(x):
    x = min(max(x, 0.0), 1.0)
    return x*x*(3-2*x)


# ---- timeline: per output frame, a list of (title, clip_frame) layers with weights
timeline = []
prev = None   # (title, start, k0) of the previous full-body segment, and its mask
for i, seg in enumerate(spec['segments']):
    title = seg['clip']
    nfr = seg['frames']
    bl = seg.get('blend', 0) if prev else 0
    start = seg.get('start', clip_len(title)[1])
    if seg.get('sync') and prev:
        pn, pa_, ploop = clip_len(prev['clip'])
        n, a, loop = clip_len(title)
        pf = clip_frame(prev['clip'], prev['start'], len(timeline)-prev['t0'])
        start = a+round((pf-pa_)/pn*n) % n
    lower = seg.get('lower_from')
    lstart = seg.get('lower_start', 1)
    for k in range(nfr):
        t = len(timeline)
        entry = {'seg': i, 'clip': title, 'f': clip_frame(title, start, k)}
        if seg.get('mask') == 'upper' and lower:
            entry['lower'] = (lower, clip_frame(lower, lstart, t-seg.get('lower_t0', t-k)))
        if k < bl:
            w = smooth((k+1)/(bl+1))
            entry['from'] = (prev['clip'], clip_frame(prev['clip'], prev['start'], t-prev['t0']), w)
            if prev.get('lower'):
                entry['from_lower'] = prev['lower']
        timeline.append(entry)
    prev = {'clip': title, 'start': start, 't0': len(timeline)-nfr, 'lower': lower}


def pose_at(e):
    cur = sample(e['clip'], e['f'])
    if 'lower' in e:
        base = sample(*e['lower'])
        cur = blend(base, cur, 1.0, UPPER)
    if 'from' in e:
        title, f, w = e['from']
        src = sample(title, f)
        cur = blend(src, cur, w)
    return cur


poses = [pose_at(e) for e in timeline]
rig.animation_data.action = None


def apply(pose):
    for name, (l, q, s) in pose[0].items():
        b = p.pb[name]
        b.location, b.rotation_quaternion, b.scale = l, q, s
    for k, v in pose[1].items():
        root[k] = v


# ---- metrics (world positions of TRACK bones)
pos = []
for n, pose in enumerate(poses):
    apply(pose)
    scene.frame_set(n+1)
    p.update()
    pos.append({b: (rig.matrix_world @ p.pb[b].head).copy() for b in TRACK if b in p.pb})
bounds = [n for n in range(1, len(timeline)) if timeline[n]['seg'] != timeline[n-1]['seg']]


def acc(n, b):
    if n < 1 or n >= len(pos)-1:
        return 0.0
    return (pos[n+1][b]-2*pos[n][b]+pos[n-1][b]).length


def stepv(n, b):
    return (pos[n][b]-pos[n-1][b]).length if n >= 1 else 0.0


near = {n+d for n in bounds for d in range(-2, 3)}
inside = [n for n in range(1, len(pos)-1) if n not in near]
base_acc = {b: max([acc(n, b) for n in inside] or [0.0]) for b in pos[0]}
handoffs = []
for n in bounds:
    e0, e1 = timeline[n-1], timeline[n]
    rows = {}
    for b in pos[0]:
        a = max(acc(k, b) for k in range(max(1, n-2), min(len(pos)-1, n+3)))
        rows[b] = {'acc': round(a, 4), 'clip_max_acc': round(base_acc[b], 4), 'step': round(stepv(n, b), 4),
                   'ratio': round(a/base_acc[b], 2) if base_acc[b] > 1e-6 else None}
    worst = max(rows.items(), key=lambda kv: kv[1]['acc']-kv[1]['clip_max_acc'])
    handoffs.append({'frame': n+1, 'from': f"{e0['clip']} f{e0['f']}", 'to': f"{e1['clip']} f{e1['f']}",
                     'blend': spec['segments'][e1['seg']].get('blend', 0), 'worst_bone': worst[0], **worst[1],
                     'bones': rows})
out = H.ROOT/'art/anim/wip/transitions'/spec['name']
out.mkdir(parents=True, exist_ok=True)
labels = [f"{n+1} {e['clip']} f{e['f']}"+(f" <{e['from'][0]} {e['from'][2]:.2f}" if 'from' in e else '')
          for n, e in enumerate(timeline)]
summary = {'name': spec['name'], 'frames': len(timeline), 'handoffs': handoffs,
           'acc_series_mm': {b: [round(1000*acc(n, b), 1) for n in range(len(pos))] for b in pos[0]},
           'timeline': [{k: v for k, v in e.items()} for e in timeline]}
(out/'stitch.json').write_text(json.dumps(summary, indent=1, default=str), encoding='utf-8')
for h in handoffs:
    print('HANDOFF', spec['name'], h['frame'], h['from'], '->', h['to'], 'blend', h['blend'], 'worst', h['worst_bone'],
          'acc', h['acc'], 'clip_max', h['clip_max_acc'], 'ratio', h['ratio'], flush=True)

if '--no-render' not in args:
    H.eevee(scene, 12)
    if spec.get('ortho'):
        for v in VIEWS.values():
            c = bpy.data.objects.get(v)
            if c and c.data.type == 'ORTHO':
                c.data.ortho_scale = spec['ortho']
    for view in views:
        for n, pose in enumerate(poses):
            apply(pose)
            scene.frame_set(n+1)
            p.update()
            H.render_still(scene, VIEWS[view], out/f'{view}-{n:03}.png', res)
        print('RENDERED', spec['name'], view, flush=True)
    fresh = {f'{v}-{n:03}.png' for v in views for n in range(len(poses))}
    for old in out.glob('*-[0-9][0-9][0-9].png'):
        if old.name not in fresh:
            (out/'stale').mkdir(exist_ok=True)
            old.replace(out/'stale'/old.name)
    (out/'clip.json').write_text(json.dumps({'title': spec['name'], 'step': 1, 'views': views,
                                             'rendered_frames': labels}, indent=2), encoding='utf-8')
print('STITCH DONE', spec['name'], flush=True)
