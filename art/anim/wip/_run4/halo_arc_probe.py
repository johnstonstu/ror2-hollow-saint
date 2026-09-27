"""Read-only: per frame of a clip, lower-arc-to-pad gap (mm) with the pads chest-rigid (k=0) for the clip's halo
pose, the halo with no own animation (bones reset), and the halo with only the root motion (segments reset).
Run: blender --background --factory-startup --python halo_arc_probe.py -- <module> <title>"""
import importlib
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT/'tools/blender/anim'))
sys.path.insert(0, str(ROOT/'tools/blender/anim/clips'))
from mathutils import Quaternion, Vector

import hs_anim as H
import padpass

argv = sys.argv[sys.argv.index('--')+1:]
module, title = argv[0], argv[1]
p = H.open_start()
mod = importlib.import_module(module)
captured = {}


def fake_bake(poser, t, frames, pose_fn, loop, **kw):
    if t == title:
        captured['args'] = (frames, pose_fn)
    return {'title': t}


H.bake = fake_bake
mod.bake = fake_bake
mod.build(p)
frames, pose_fn = captured['args']
pp = padpass.PadPass(p)
print('HALO PARENT', p.rig.data.bones['halo root'].parent.name, flush=True)
if not hasattr(p, 'handpass'):
    import handpass
    p.handpass = handpass.HandPass(p)
rows = []


def gaps():
    out = {}
    for s in H.SIDES:
        m_ch, w, up = pp.swing(s)
        pad0 = pp.compose(s, m_ch, w, up, 0.0, 0.0)
        pad1 = pp.compose(s, m_ch, w, up, 1.0, 0.0)
        arc = p.pb[padpass.HALO_ARC[s]].matrix.copy()
        out[s] = (round(pp.arc_clearance(s, pad0, arc)*1000, 1), round(pp.arc_clearance(s, pad1, arc)*1000, 1))
    return out


for f in frames:
    p.reset()
    p.ik(0.0, 0.0)
    pose_fn(p, f)
    p.handpass.apply()
    p.update()
    row = {'f': f, 'clip': gaps()}
    root = p.pb['halo root']
    saved = {n: (p.pb[n].location.copy(), p.pb[n].rotation_quaternion.copy()) for n in padpass.HALO_BONES}
    for n in padpass.HALO_BONES[1:]:
        p.pb[n].location = (0, 0, 0)
        p.pb[n].rotation_quaternion = Quaternion()
    p.update()
    row['root_only'] = gaps()
    root.location = (0, 0, 0)
    root.rotation_quaternion = Quaternion()
    p.update()
    row['no_halo'] = gaps()
    ch = p.pb['chest'].matrix
    row['arc_rel_chest'] = {s: [round(v, 3) for v in (ch.inverted() @ p.pb[padpass.HALO_ARC[s]].matrix).translation]
                            for s in H.SIDES}
    for n, (l, q) in saved.items():
        p.pb[n].location, p.pb[n].rotation_quaternion = l, q
    rows.append(row)
    print('ROW', json.dumps(row), flush=True)
out = ROOT/'art/anim/wip/_run4'/f'halo_arc_probe-{title.replace(" ", "_")}.json'
out.write_text(json.dumps(rows, indent=1))
