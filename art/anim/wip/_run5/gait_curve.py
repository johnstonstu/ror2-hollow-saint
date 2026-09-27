"""Print a gait's per-frame (y, lift, pitch, toe) and pitch second differences, raw and blurred, without Blender.
python gait_curve.py run.G|run_dirs.GB|run_dirs.GS [blur ...]"""
import math
import sys
import types
from pathlib import Path

m = types.ModuleType('hs_anim')
m.FPS = 24


def clamp(x, a=0.0, b=1.0):
    return max(a, min(b, x))


def smooth(t):
    t = clamp(t)
    return t*t*(3-2*t)


m.clamp, m.smooth, m.lerp = clamp, smooth, (lambda a, b, t: a+(b-a)*t)
m.ramp = lambda x, a, b: smooth((x-a)/(b-a)) if b != a else float(x >= a)
sys.modules['hs_anim'] = m
sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
import gait

src = open(Path(gait.__file__).parent/'clips'/(sys.argv[1].split('.')[0]+'.py')).read()
name = sys.argv[1].split('.')[1]
start = src.index(f'{name} = gait.params(')
depth, i = 0, src.index('(', start)
for j in range(i, len(src)):
    depth += {'(': 1, ')': -1}.get(src[j], 0)
    if depth == 0:
        break
ns = {'gait': gait, 'N': 16}
exec(src[start:j+1], ns)
G = dict(ns[name])
N = G['cycle']
for b in [float(x) for x in sys.argv[2:]] or [0.0]:
    G['angle_blur'] = b
    rows = [gait.foot_cycle(((f-1) % N)/N, G) for f in range(1, N+1)]
    p = [r[2] for r in rows]
    t = [r[3] for r in rows]
    acc = [p[(i+1) % N]-2*p[i]+p[i-1] for i in range(N)]
    print(f'blur {b}')
    print('  y    ', [round(r[0], 3) for r in rows])
    print('  lift ', [round(r[1], 3) for r in rows])
    print('  pitch', [round(x, 1) for x in p])
    print('  acc  ', [round(x, 1) for x in acc])
    print('  toe  ', [round(x, 1) for x in t])
