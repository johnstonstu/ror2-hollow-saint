"""Scratch (hands2): LegClear proxy clearance (mm, at zero extra extension) per frame of a baked clip vs the
clearance.py measurement.  blender ... --python probe_clear.py -- <blend> "<clip title>" <side>
"""
import bpy
import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[4]/'tools/blender/anim'
sys.path.insert(0, str(TOOLS))
import hs_anim as H
import handpose
import clearance

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


p = Rig(rig)
geo = handpose.HandGeo(p)
lc = handpose.LegClear(p)
clr = clearance.Clearance()
act = bpy.data.actions[H.PREFIX+args[1]]
rig.animation_data.action = act
if getattr(rig.animation_data, 'action_slot', None) is None and len(getattr(act, 'slots', [])):
    rig.animation_data.action_slot = act.slots[0]
s = args[2]
pb = rig.pose.bones
for f in range(int(act.frame_range[0]), int(act.frame_range[1])+1):
    bpy.context.scene.frame_set(f)
    bpy.context.view_layer.update()
    m = pb[f'{s} hand'].matrix
    lc._legs = [(pb[b].matrix.inverted(), kd, nrm) for b, (kd, nrm) in lc.legs[s].items()]
    pts = [pb[b].matrix @ c for b, cs in lc.probes[s] for c in cs]
    c = lc.clearance(p, s, pts, m.translation.copy(), (m.to_3x3() @ geo.palm_axis[s]).normalized(), 0.0)
    real = clr.frame()
    print('C', f, 'proxy_mm', round(c*1000, 1), 'clearance', json_s := str(real.get(s) if isinstance(real, dict) else real)[:160], flush=True)
