"""Seam check on a SAVED checkpoint (seams.py rebuilds from modules): loop wraps and declared hand-offs.
Run: blender --background --factory-startup --python tools/blender/anim/seams_saved.py -- BLEND OUT.json
Deform-bone head positions (mm, armature space) for seam_from/seam_to/starts_from/ends_in/run_frame_*;
*_arms keys compare arm/hand local rotations only (deg), for arms-only layer clips."""
import bpy, json, sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))
import hs_anim as H
args = sys.argv[sys.argv.index('--')+1:]
bpy.ops.wm.open_mainfile(filepath=str(H.ROOT/args[0]))
rig = bpy.data.objects[H.RIG]
for t in rig.animation_data.nla_tracks:
    t.mute = True
acts = {a.name[len(H.PREFIX):]: a for a in bpy.data.actions if a.name.startswith(H.PREFIX)}
info = {t: json.loads(a['clip_json']) for t, a in acts.items()}
DEF = [b.name for b in rig.pose.bones if b.bone.use_deform]
ARM = [b for b in DEF if b[:2] in ('L ', 'R ') and not any(k in b for k in ('thigh', 'shin', 'foot', 'toe'))]
cache = {}


def pose(t, f):
    if (t, f) not in cache:
        a = acts[t]
        rig.animation_data.action = a
        rig.animation_data.action_slot = a.slots[0]
        bpy.context.scene.frame_set(f)
        cache[(t, f)] = ({n: rig.pose.bones[n].matrix.translation.copy() for n in DEF},
                         {n: rig.pose.bones[n].matrix_basis.to_quaternion() for n in ARM})
    return cache[(t, f)]


def norm(t, f):
    f0, f1 = info[t]['frames']
    return f0+(f-f0) % (f1-f0) if info[t].get('loop') else f


rows = []
for t, i in info.items():
    f0, f1 = i['frames']
    decl = []
    if i.get('loop'):
        decl.append(('wrap', f0, t, f1, False))
    for key, fr, arms in (('seam_from', f0, False), ('seam_to', f1, False), ('seam_from_arms', f0, True),
                          ('seam_to_arms', f1, True)):
        if i.get(key) and i[key][0] in info:
            decl.append((key, fr, i[key][0], norm(i[key][0], i[key][1]), arms))
    if i.get('starts_from') in info:
        decl.append(('starts_from', f0, i['starts_from'], info[i['starts_from']]['frames'][0], False))
    if i.get('ends_in') in info:
        decl.append(('ends_in', f1, i['ends_in'], info[i['ends_in']]['frames'][0], False))
    for k, fk in (('run_frame_start', f0), ('run_frame_end', f1)):
        if k in i:
            decl.append((k, fk, 'Run forward', norm('Run forward', i[k]), False))
    for key, fr, o, of, arms in decl:
        a, b = pose(t, fr), pose(o, of)
        if arms:
            v = max(a[1][n].rotation_difference(b[1][n]).angle for n in ARM)*57.2958
            rows.append({'clip': t, 'seam': key, 'other': [o, of], 'max_deg_arms': round(v, 3)})
        else:
            v, bn = max(((a[0][n]-b[0][n]).length, n) for n in DEF)
            rows.append({'clip': t, 'seam': key, 'other': [o, of], 'max_mm': round(v*1000, 3), 'bone': bn})
out = H.ROOT/args[1]
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(rows, indent=1), encoding='utf-8')
worst = max(r.get('max_mm', 0) for r in rows)
print('SEAMS', len(rows), 'worst_mm', round(worst, 3), 'arms_worst_deg', max([r.get('max_deg_arms', 0) for r in rows]))
for r in rows:
    if r.get('max_mm', 0) > 0.01 or r.get('max_deg_arms', 0) > 0.05:
        print('SEAM>', r)
