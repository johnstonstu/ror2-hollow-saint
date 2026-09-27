"""Rebuild clip modules on a fresh v18 and diff them against the same actions in a saved checkpoint.

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/action_diff.py --
     art/anim/hollow-saint-anim-v18.blend primary [module ...] [--out path.json]
Per action: worst bone position (mm) and rotation (deg) difference over every frame, the bone/frame where it
happens, and the worst root VFX property difference. Nothing is saved.
"""
import bpy
import importlib
import json
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
sys.path.insert(0, str(Path(__file__).parent/'clips'))
import hs_anim as H

args = sys.argv[sys.argv.index('--')+1:]
out = args[args.index('--out')+1] if '--out' in args else None
args = [a for i, a in enumerate(args) if a != '--out' and (i == 0 or args[i-1] != '--out')]
ref_path, modules = Path(args[0]), args[1:]
H.require_background()


def sample(rig, act):
    rig.animation_data.action = act
    scene = bpy.context.scene
    res = {}
    for f in range(int(act.frame_range[0]), int(act.frame_range[1])+1):
        scene.frame_set(f)
        bpy.context.view_layer.update()
        res[f] = ({b.name: b.matrix.copy() for b in rig.pose.bones if not b.name.endswith((' IK', ' pole'))},
                  {k: float(rig.pose.bones['root'][k]) for k in rig.pose.bones['root'].keys()
                   if k.startswith('hs_')})
    return res


bpy.ops.wm.open_mainfile(filepath=str(ref_path))
rig = bpy.data.objects[H.RIG]
ref_actions = {a.name: a for a in bpy.data.actions if a.name.startswith(H.PREFIX)}
p = H.open_start()
built = []
for m in modules:
    built += [(act, info) for act, info in importlib.import_module(m).build(p)]
new = {act.name: sample(p.rig, act) for act, _ in built}
bpy.ops.wm.open_mainfile(filepath=str(ref_path))
rig = bpy.data.objects[H.RIG]
report = {}
for name, frames in new.items():
    act = bpy.data.actions.get(name)
    if act is None:
        report[name] = 'missing in reference'
        continue
    ref = sample(rig, act)
    worst_p = worst_r = worst_prop = 0.0
    at_p = at_r = at_prop = ''
    for f, (mats, props) in frames.items():
        if f not in ref:
            report.setdefault(name + ' frames', []).append(f)
            continue
        rm, rp = ref[f]
        for b, m in mats.items():
            if b not in rm:
                continue
            d = (m.translation-rm[b].translation).length*1000
            q = m.to_quaternion().rotation_difference(rm[b].to_quaternion())
            a = math.degrees(min(q.angle, 2*math.pi-q.angle))
            if d > worst_p:
                worst_p, at_p = d, f'{b} f{f}'
            if a > worst_r:
                worst_r, at_r = a, f'{b} f{f}'
        for k, v in props.items():
            dv = abs(v-rp.get(k, 0.0))
            if dv > worst_prop:
                worst_prop, at_prop = dv, f'{k} f{f}'
    report[name] = {'frames': len(frames), 'ref_frames': len(ref), 'max_pos_mm': round(worst_p, 3), 'pos_at': at_p,
                    'max_rot_deg': round(worst_r, 3), 'rot_at': at_r, 'max_prop': round(worst_prop, 4),
                    'prop_at': at_prop}
    print('DIFF', name, json.dumps(report[name]), flush=True)
if out:
    Path(out).write_text(json.dumps(report, indent=1), encoding='utf-8')
print('ACTION DIFF DONE', flush=True)
