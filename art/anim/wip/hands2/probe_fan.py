"""Scratch (hands2): rest spread per digit and the spread a +fan local-Z turn gives (handpass.side's fan sign)."""
import bpy
import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[4]/'tools/blender/anim'
sys.path.insert(0, str(TOOLS))
import hs_anim as H
import handpose
from handfix import ZSIGN

bpy.ops.wm.open_mainfile(filepath=str(H.ROOT/sys.argv[sys.argv.index('--')+1]))
rig = bpy.data.objects[H.RIG]


class Rig:
    def __init__(self, rig):
        self.rig = rig
        self.pb = rig.pose.bones
        self.rest = {b.name: b.matrix_local.copy() for b in rig.data.bones}
        self.r3 = {n: m.to_3x3() for n, m in self.rest.items()}


geo = handpose.HandGeo(Rig(rig))
for s in H.SIDES:
    k = (1.0 if s == 'L' else -1.0)*ZSIGN
    print('FAN', s, {d: (round(geo.spread0[(s, d)], 1), round(geo.splay_of(s, d, handpose.about(handpose.Z, k*5.0)), 2))
                     for d in handpose.DIGITS}, flush=True)
    ref = geo.rel[f'{s} middle.1'] @ handpose.Y
    print('DIR', s, {d: round(handpose.signed(ref, geo.rel[f'{s} {d}.1'] @ handpose.Y, geo.normal[s]), 1)
                     for d in handpose.DIGITS}, flush=True)
