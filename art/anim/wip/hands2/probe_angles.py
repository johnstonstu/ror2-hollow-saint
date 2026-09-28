"""Scratch probe (hands2): geometric finger joint bends on a checkpoint's baked clips.

blender --background --factory-startup --python art/anim/wip/hands2/probe_angles.py -- <blend> <out.json> ["Title:f,f;Title:f"]
Bend of joint k = signed angle from the parent segment to segment k about bone k's local X (the curl hinge,
+ = toward the palm). Base (.1) is measured from the hand bone. Spread = angle between neighbouring .1 bones
projected on the palm plane.
"""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
import handorient

args = sys.argv[sys.argv.index('--')+1:]
blend, out = args[0], Path(args[1])
spec = args[2] if len(args) > 2 else 'Idle:1,40;Run forward:1,5,9;Walk forward:1,8;Idle combat:1;Arc Bolt right:5,10;Charge full:12;Discharge:9;Conduit Spear:5,7;Glide loop:1;Jump:5'
bpy.ops.wm.open_mainfile(filepath=blend)
rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE' and 'rig' in o.name)
pb = rig.pose.bones
DIG = ('index', 'middle', 'ring', 'little')


def signed(a, b, axis):
    a, b = a.normalized(), b.normalized()
    return math.degrees(math.atan2(a.cross(b).dot(axis), a.dot(b)))


def measure():
    bpy.context.view_layer.update()
    res = {}
    for s in 'LR':
        m = lambda n: rig.matrix_world.to_3x3() @ pb[n].matrix.to_3x3()
        d, n, r = handorient.hand_axes(rig, s)
        row = {}
        wrist = rig.matrix_world @ pb[f'{s} hand'].head
        meta = {}
        for dg in DIG:
            k = rig.matrix_world @ pb[f'{s} {dg}.1'].head
            y1, x1 = m(f'{s} {dg}.1').col[1], m(f'{s} {dg}.1').col[0]
            meta[dg] = round(signed(k-wrist, y1, x1), 1)
        row['base_vs_metacarpal'] = meta
        pal = sum(((rig.matrix_world @ pb[f'{s} {dg}.1'].head)-wrist for dg in DIG), Vector()).normalized()
        row['palm_line_vs_forearm'] = round(signed(m(f'{s} forearm').col[1], pal, m(f'{s} hand').col[0]), 1)
        for dg in DIG+('thumb',):
            prev = m(f'{s} hand').col[1] if dg != 'thumb' else m(f'{s} hand').col[1]
            bends = []
            for i in (1, 2, 3):
                mm = m(f'{s} {dg}.{i}')
                y, x = mm.col[1], mm.col[0]
                bends.append(round(signed(prev, y, x), 1))
                prev = y
            row[dg] = bends
        sp = []
        for a, b in zip(DIG, DIG[1:]):
            ya, yb = m(f'{s} {a}.1').col[1], m(f'{s} {b}.1').col[1]
            ya, yb = ya-n*ya.dot(n), yb-n*yb.dot(n)
            sp.append(round(math.degrees(ya.angle(yb)), 1))
        row['spread'] = sp
        hb = pb[f'{s} hand'].matrix_basis.to_quaternion()
        row['wrist_basis_deg'] = round(math.degrees(hb.angle), 1)
        fa = m(f'{s} forearm').col[1]
        row['wrist_flex_deg'] = round(signed(fa, d, (m(f'{s} hand').col[0])), 1)
        res[s] = row
    return res


report = {}
act0 = rig.animation_data.action if rig.animation_data else None
if rig.animation_data:
    rig.animation_data.action = None
for p in pb:
    p.matrix_basis.identity()
report['REST'] = measure()
for item in spec.split(';'):
    title, fr = item.split(':')
    act = bpy.data.actions.get('HS_anim | '+title)
    if act is None:
        report[title] = 'missing'
        continue
    rig.animation_data.action = act
    for f in fr.split(','):
        bpy.context.scene.frame_set(int(f))
        report[f'{title} f{f}'] = measure()
out.write_text(json.dumps(report, indent=1), encoding='utf-8')
print('PROBE DONE', out, flush=True)
