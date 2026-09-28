"""Scratch (hands2): per-frame finger bends/sideways/levels of one digit in a baked clip of a saved blend.

blender --background --factory-startup --python-exit-code 1 --python art/anim/wip/hands2/probe_clip.py --
    <blend> "<clip title>" <side> <digit>[,digit...]
"""
import bpy
import math
import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[4]/'tools/blender/anim'
sys.path.insert(0, str(TOOLS))
import hs_anim as H
import handpose

args = sys.argv[sys.argv.index('--')+1:]
bpy.ops.wm.open_mainfile(filepath=str(H.ROOT/args[0]))
rig = bpy.data.objects[H.RIG]
for t in rig.animation_data.nla_tracks:
    t.mute = True


class Rig:
    def __init__(self, rig):
        self.rig = rig
        self.pb = rig.pose.bones
        self.rest = {b.name: b.matrix_local.copy() for b in rig.data.bones}
        self.r3 = {n: m.to_3x3() for n, m in self.rest.items()}


geo = handpose.HandGeo(Rig(rig))
act = bpy.data.actions[H.PREFIX+args[1]]
rig.animation_data.action = act
if getattr(rig.animation_data, 'action_slot', None) is None and len(getattr(act, 'slots', [])):
    rig.animation_data.action_slot = act.slots[0]
s = args[2]
digits = args[3].split(',')
pb = rig.pose.bones
for f in range(int(act.frame_range[0]), int(act.frame_range[1])+1):
    bpy.context.scene.frame_set(f)
    row = [f'f{f:3}']
    for d in digits:
        if d == 'thumb':
            row.append('thumb ' + ' '.join(f'{geo.bend(f"{s} thumb.{i}", pb[f"{s} thumb.{i}"].rotation_quaternion):6.1f}'
                                           for i in (2, 3)) + f' tw1 {handpose.twist_x(pb[f"{s} thumb.1"].rotation_quaternion):6.1f}')
            continue
        m = geo.measure(pb, s, d)
        q = [pb[f'{s} {d}.{i}'].rotation_quaternion for i in (1, 2, 3)]
        row.append(f'{d} b ' + ' '.join(f'{b:6.1f}' for b, _ in m) + ' | side ' + ' '.join(f'{x:6.1f}' for _, x in m)
                   + ' | twX ' + ' '.join(f'{handpose.twist_x(x):6.1f}' for x in q))
    print('P', '  '.join(row), flush=True)
