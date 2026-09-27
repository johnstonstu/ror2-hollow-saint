"""Per-frame knee bend and knee-direction angle (fullqa definition) for clips of a module, plus rest.
blender -b --factory-startup --python knee_probe.py -- <module> "Title A;Title B" """
import bpy
import importlib
import math
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim/clips'))
import hs_anim as H

args = sys.argv[sys.argv.index('--')+1:]
p = H.open_start()
titles = set(args[1].split(';'))


def row(tag):
    out = []
    for s in H.SIDES:
        pb = p.rig.pose.bones
        th, sh, ft, toe = pb[f'{s} thigh'], pb[f'{s} shin'], pb[f'{s} foot'], pb[f'{s} toe']
        vt = (sh.head-th.head).normalized()
        vs = (sh.tail-sh.head).normalized()
        bend = math.degrees(vt.angle(vs, 0.0))
        kd = (sh.tail-th.head).normalized().cross(sh.matrix.to_3x3().col[0])
        kd.z = 0
        fwd = toe.head-ft.head
        fwd.z = 0
        ang = math.degrees(kd.angle(fwd, 0.0)) if kd.length > 1e-4 else -1
        out.append(f'{s}: bend {bend:5.1f} dir {ang:5.1f} kd ({kd.x:+.2f},{kd.y:+.2f}) fwd ({fwd.x:+.2f},{fwd.y:+.2f}) toez {toe.head.z:.3f}')
    print(f'  {tag:>5}', ' | '.join(out))


p.reset()
p.update()
row('rest')
built = importlib.import_module(args[0]).build(p)
p.ik(0, 0)
p.followers(False)
sc = bpy.context.scene
for act, info in built:
    if info['title'] not in titles:
        continue
    p.rig.animation_data.action = act
    print('CLIP', info['title'])
    for f in range(int(act.frame_range[0]), int(act.frame_range[1])+1):
        sc.frame_set(f)
        row(f'f{f}')
