"""Per-frame local rotation (euler deg) + fullqa pop (deg/f2) for bones of clips of a module.
blender -b --factory-startup --python bone_probe.py -- <module> "Title A;Title B" "bone a;bone b" """
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
bones = args[2].split(';')
built = importlib.import_module(args[0]).build(p)
p.ik(0, 0)
p.followers(False)
sc = bpy.context.scene
for act, info in built:
    if info['title'] not in titles:
        continue
    p.rig.animation_data.action = act
    print('CLIP', info['title'])
    seq = []
    for f in range(int(act.frame_range[0]), int(act.frame_range[1])+1):
        sc.frame_set(f)
        seq.append({b: p.pb[b].rotation_quaternion.copy() for b in bones})
    for i, q in enumerate(seq):
        row = []
        for b in bones:
            e = q[b].to_euler()
            pop = 0.0
            if 0 < i < len(seq)-1:
                mid = seq[i-1][b].slerp(seq[i+1][b], 0.5)
                d = q[b].rotation_difference(mid)
                pop = 2*math.degrees(min(d.angle, 2*math.pi-d.angle))
            row.append(f'{b}: ({math.degrees(e.x):6.1f},{math.degrees(e.y):6.1f},{math.degrees(e.z):6.1f}) pop {pop:5.1f}')
        print(f'  f{i+1:3d}', ' | '.join(row))
