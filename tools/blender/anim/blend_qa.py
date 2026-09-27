"""9h blend-space QA: neighbour clips blended 50/50 at matched normalized time, as a Unity 2D blend tree plays them.

Run:
  blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/blend_qa.py --
      <out.json> <module ...> [--pairs "A|B;C|D"] [--sets run,walk] [--w 0.5]
Per pair and blend weight, every frame of the shared cycle is sampled from both clips (local transforms slerped,
like stitch.py). A foot planted in both source clips (toe below PLANT_Z) must travel at the blended stance
velocity: the weighted sum of each clip's -travel x speed. Reported per pair:
- contact_slide_mm: the worst drift of a planted foot from that velocity accumulated over one contact
  (limit SLIDE_MAX), and the worst single-frame step error;
- phase: each clip's 'L contact' / 'R contact' markers as a fraction of the cycle (must match);
- sink_mm: how far the blended planted ball dips below its ground height (rotation blending shortens the
  leg; Unity's generic blend shows the same, foot IK would remove it);
- gap_mm: the smallest L-minus-R side gap (character left is +X) of the balls, ankles and knees (leg crossing).
Clip travel comes from meta via hs_anim.move_vector; speed from meta speed_mps.
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
import gait

PLANT_Z = 0.06
SLIDE_MAX = 0.010
GAP_MIN = 0.0
RING_RUN = ['Run forward', 'Run forward right', 'Run right', 'Run backward right', 'Run backward',
            'Run backward left', 'Run left', 'Run forward left']
RING_WALK = ['Walk forward', 'Walk forward right', 'Walk right', 'Walk backward right', 'Walk backward',
             'Walk backward left', 'Walk left', 'Walk forward left']

args = sys.argv[sys.argv.index('--')+1:]
out_path = Path(args[0])
flags = {a: args[i+1] for i, a in enumerate(args) if a.startswith('--') and i+1 < len(args)}
modules = [a for i, a in enumerate(args[1:], 1) if not a.startswith('--') and not args[i-1].startswith('--')]
weights = [float(w) for w in flags.get('--w', '0.5').split(',')]

p = H.open_start()
rig = p.rig
scene = bpy.context.scene
clips = {}
for m in modules:
    for act, info in importlib.import_module(m).build(p):
        clips[info['title']] = (act, info)

pairs = []
if '--pairs' in flags:
    pairs = [tuple(s.split('|')) for s in flags['--pairs'].split(';')]
for name in flags.get('--sets', 'run,walk').split(','):
    ring = {'run': RING_RUN, 'walk': RING_WALK}.get(name, [])
    pairs += [(a, b) for a, b in zip(ring, ring[1:]+ring[:1])]
    pairs += [(a, a) for a in ring]
pairs = [(a, b) for a, b in pairs if a in clips and b in clips]


def sample(title, f):
    act, _ = clips[title]
    rig.animation_data.action = act
    scene.frame_set(f)
    return {b.name: (b.location.copy(), b.rotation_quaternion.copy(), b.scale.copy()) for b in p.pb}


def world(pose):
    rig.animation_data.action = None
    for name, (l, q, s) in pose.items():
        b = p.pb[name]
        b.location, b.rotation_quaternion, b.scale = l, q, s
    p.update()
    return {n: (rig.matrix_world @ p.pb[n].head).copy() for n in ('L toe', 'R toe', 'L foot', 'R foot',
                                                                   'L shin', 'R shin')}


def blend(pa, pb, w):
    out = {}
    for name, (la, qa, sa) in pa.items():
        l, q, s = pb[name]
        q = q if qa.dot(q) >= 0 else -q
        out[name] = (la.lerp(l, w), qa.slerp(q, w), sa.lerp(s, w))
    return out


def cycle(title):
    act, info = clips[title]
    a, b = int(act.frame_range[0]), int(act.frame_range[1])
    return a, b-a


def stance_vel(title):
    _, info = clips[title]
    mx, my = H.move_vector(info)
    v = info.get('speed_mps', 0.0)/H.FPS
    return mx*v, my*v                      # planted foot moves opposite travel: +move in Blender XY (see vfx.py)


def phases(title):
    act, info = clips[title]
    a, n = cycle(title)
    return {k: round((f-a)/n, 3) for k, f in info['markers'].items() if 'contact' in k}


report = {'plant_z': PLANT_Z, 'slide_max_mm': SLIDE_MAX*1000, 'pairs': []}
for a, b in pairs:
    (fa, na), (fb, nb) = cycle(a), cycle(b)
    for w in (weights if a != b else [0.0]):
        n = max(na, nb)
        pts, src_z = [], []
        for k in range(n):
            pa = sample(a, fa+round(k*na/n) % na)
            pb = sample(b, fb+round(k*nb/n) % nb)
            wa, wb = world(pa), world(pb)
            src_z.append({s: max(wa[f'{s} toe'].z, wb[f'{s} toe'].z) for s in 'LR'})
            pts.append(world(blend(pa, pb, w)))
        va, vb = stance_vel(a), stance_vel(b)
        vx, vy = va[0]*(1-w)+vb[0]*w, va[1]*(1-w)+vb[1]*w
        if flags.get('--debug') == f'{a}|{b}':
            for k in range(n):
                wa = world(sample(a, fa+round(k*na/n) % na))
                wb = world(sample(b, fb+round(k*nb/n) % nb))
                print('DBG', k, *(f"{s}: A {tuple(round(c, 3) for c in wa[f'{s} toe'])} "
                                  f"B {tuple(round(c, 3) for c in wb[f'{s} toe'])} "
                                  f"blend {tuple(round(c, 3) for c in pts[k][f'{s} toe'])}" for s in 'LR'),
                      flush=True)
            print('DBG v', round(vx, 4), round(vy, 4), flush=True)
        worst, worst_step, contacts, sink = 0.0, 0.0, 0, 0.0
        for s in ('L', 'R'):
            # Planted = planted in both source clips: rotation blending shortens the leg, so the blended foot
            # itself dips (reported as sink_mm) and can't be trusted to say when the foot is down.
            planted = [src_z[k][s] < PLANT_Z for k in range(n)]
            sink = max([sink]+[gait.BALL_Z-pts[k][f'{s} toe'].z for k in range(n) if planted[k]])
            if all(planted):
                runs = [list(range(n))]
            else:
                start = next(k for k in range(n) if not planted[k])
                runs, cur = [], []
                for i in range(1, n+1):
                    k = (start+i) % n
                    if planted[k]:
                        cur.append(k)
                    elif cur:
                        runs.append(cur)
                        cur = []
                if cur:
                    runs.append(cur)
            for r in runs:
                if len(r) < 2:
                    continue
                contacts += 1
                p0 = pts[r[0]][f'{s} toe']
                for i, k in enumerate(r[1:], 1):
                    q = pts[k][f'{s} toe']
                    dx, dy = q.x-p0.x-vx*i, q.y-p0.y-vy*i
                    worst = max(worst, math.hypot(dx, dy))
                    d = pts[k][f'{s} toe']-pts[r[i-1]][f'{s} toe']
                    worst_step = max(worst_step, math.hypot(d.x-vx, d.y-vy))
        gaps = {part: min(pts[k][f'L {part}'].x-pts[k][f'R {part}'].x for k in range(n))
                for part in ('toe', 'foot', 'shin')}
        row = {'a': a, 'b': b, 'w': w, 'contacts': contacts, 'contact_slide_mm': round(worst*1000, 1),
               'step_err_mm': round(worst_step*1000, 1), 'sink_mm': round(sink*1000, 1),
               'gap_mm': {k: round(v*1000, 1) for k, v in gaps.items()},
               'phase': {a: phases(a), b: phases(b)},
               'ok': worst <= SLIDE_MAX and min(gaps.values()) > GAP_MIN and phases(a) == phases(b)}
        report['pairs'].append(row)
        print('BLEND', f'{a} | {b} w{w}', 'slide', row['contact_slide_mm'], 'step', row['step_err_mm'],
              'sink', row['sink_mm'], 'gap', row['gap_mm'], 'ok', row['ok'], flush=True)
report['ok'] = all(r['ok'] for r in report['pairs'])
out_path.parent.mkdir(parents=True, exist_ok=True)
out_path.write_text(json.dumps(report, indent=1), encoding='utf-8')
print('BLEND QA', 'ok' if report['ok'] else 'FAIL', sum(r['ok'] for r in report['pairs']), '/', len(report['pairs']),
      flush=True)
