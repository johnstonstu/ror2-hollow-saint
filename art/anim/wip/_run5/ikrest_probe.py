"""IK leg at the rest foot: thigh/shin local rotation and knee error vs rest, per pole option, lock on/off.
blender -b --factory-startup --python ikrest_probe.py"""
import math
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim/clips'))
import hs_anim as H
import arcstep

p = H.open_start()
arcstep.solve_rest_poles(p)
print('POLES', p.pole_angles, 'rest_pole_shift', {s: (tuple(round(x, 3) for x in v), e) for s, (v, e) in p.rest_pole_shift.items()})


def ang(q):
    return round(math.degrees(q.angle), 1)


for lock in (True, False):
    for s in H.SIDES:
        p.pb[f'{s} shin'].lock_ik_y = p.pb[f'{s} shin'].lock_ik_z = lock
    for tag, kw in (('plain', {}), ('rest_match', {'rest_match': 1.0}),
                    ('arcstep shift', {'pole_shift': None})):
        row = []
        for s in H.SIDES:
            p.reset()
            p.ik(1.0, 0.0)
            k = dict(kw)
            if 'pole_shift' in k:
                k['pole_shift'] = arcstep.REST_POLE_SHIFT[s]
            p.foot(s, p.foot_rest[s]['ball'], 0, 0, **k)
            p.update()
            knee = p.pb[f'{s} shin'].matrix.translation
            err = (knee-p.rest[f'{s} shin'].translation).length
            row.append(f"{s}: thigh {ang(p.pb[f'{s} thigh'].matrix_basis.to_quaternion()):5.1f} "
                       f"shin {ang(p.pb[f'{s} shin'].matrix_basis.to_quaternion()):5.1f} "
                       f"(visual thigh dq {ang((p.rest[f'{s} thigh'].to_3x3().inverted() @ p.pb[f'{s} thigh'].matrix.to_3x3()).to_quaternion()):5.1f}) knee err {1000*err:5.1f}mm")
        print(f'LOCK {lock} {tag:14}', ' | '.join(row))
