"""Motion range per clip in saved checkpoints: how far key bones travel over each clip (read-only).

Run: blender -b --factory-startup --python-exit-code 1 --python tools/blender/anim/motion_range.py --
       art/anim/hollow-saint-anim-v7.blend art/anim/hollow-saint-anim-v8.blend --clips "Glide loop,Ascend"
Opens each file in turn (never saves), plays each named action and prints, per bone, the diagonal of the
world-position bounding box (mm) and the peak rotation away from the first frame's orientation (deg).
Writes art/anim/wip/motion-range.json.
"""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Quaternion, Vector

ROOT = Path(__file__).resolve().parents[3]
argv = sys.argv[sys.argv.index('--')+1:]
clips = argv[argv.index('--clips')+1].split(',') if '--clips' in argv else []
files = [a for a in argv if a.endswith('.blend')]
BONES = ('pelvis', 'chest', 'head', 'L hand', 'R hand', 'L index.3', 'R foot IK', 'L foot IK',
         'tabard front.3', 'tabard back.2', 'halo root', 'halo 1')

out = {}
for fp in files:
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/fp))
    rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE' and 'v8 rig' in o.name)
    scene = bpy.context.scene
    res = {}
    for title in clips:
        act = bpy.data.actions.get(f'HS_anim | {title}')
        if act is None:
            continue
        rig.animation_data.action = act
        f0, f1 = (int(v) for v in act.frame_range)
        scene.frame_set(f0+1)
        pos = {b: [] for b in BONES if b in rig.pose.bones}
        rot = {b: [] for b in pos}
        for f in range(f0, f1+1):
            scene.frame_set(f)
            bpy.context.view_layer.update()
            for b in pos:
                m = rig.matrix_world @ rig.pose.bones[b].matrix
                pos[b].append(m.translation.copy())
                rot[b].append(m.to_3x3().normalized().to_quaternion())
        row = {}
        for b in pos:
            ps = pos[b]
            lo = Vector((min(p.x for p in ps), min(p.y for p in ps), min(p.z for p in ps)))
            hi = Vector((max(p.x for p in ps), max(p.y for p in ps), max(p.z for p in ps)))
            q0 = rot[b][0]
            peak = 0.0
            for q in rot[b]:
                d = math.degrees(2*math.acos(min(1.0, abs(q0.dot(q)))))
                peak = max(peak, d)
            row[b] = {'range_mm': round((hi-lo).length*1000, 1), 'rot_deg': round(peak, 2)}
        res[title] = row
    out[Path(fp).stem] = res

(ROOT/'art/anim/wip/motion-range.json').write_text(json.dumps(out, indent=1), encoding='utf-8')
tags = list(out)
for title in clips:
    print('==', title)
    for b in BONES:
        vals = [out[t].get(title, {}).get(b) for t in tags]
        if all(vals):
            print('  ', f'{b:15s}', '  '.join(f"{v['range_mm']:7.1f}mm {v['rot_deg']:5.1f}deg" for v in vals))
