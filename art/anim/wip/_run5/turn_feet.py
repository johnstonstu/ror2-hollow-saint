"""Print turns.py engine foot tracks (character space) per frame: blender --background --python turn_feet.py"""
import math
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT/'tools/blender/anim'))
sys.path.insert(0, str(ROOT/'tools/blender/anim/clips'))
import turns

for t in (turns.pivot(1), turns.pivot(-1), turns.plant(1), turns.plant(-1)):
    print('TF', t.title)
    for i in range(t.n):
        row = []
        for s in ('L', 'R'):
            b, pitch, yaw = t.feet[s].at(i)
            row.append(f'{s} ({b.x:+.2f},{b.y:+.2f},{b.z:.2f}) p{pitch:4.0f} y{yaw:5.0f}')
        pos = t.path.pos[i]
        print(f'TF {i:2d} psi {math.degrees(t.path.yaw[i]):6.1f} body ({pos.x:+.2f},{pos.y:+.2f}) | ' + ' | '.join(row))
