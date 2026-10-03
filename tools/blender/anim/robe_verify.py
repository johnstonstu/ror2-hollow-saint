"""Verify saved robe-only candidates, existing handoffs and positional acceleration."""
import json
import math
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).parent))
import hs_anim as H


def sample(path):
    bpy.ops.wm.open_mainfile(filepath=str(H.ROOT / path))
    rig = bpy.data.objects[H.RIG]
    for track in rig.animation_data.nla_tracks:
        track.mute = True
    result = {}
    for action in bpy.data.actions:
        if not action.name.startswith(H.PREFIX):
            continue
        rig.animation_data.action = action
        rig.animation_data.action_slot = action.slots[0]
        info = json.loads(action['clip_json'])
        frames = {}
        for frame in range(info['frames'][0], info['frames'][1] + 1):
            bpy.context.scene.frame_set(frame)
            bpy.context.view_layer.update()
            frames[frame] = {bone.name: bone.matrix.copy() for bone in rig.pose.bones if bone.bone.use_deform}
        result[info['title']] = (info, frames)
    return result


H.require_background()
args = sys.argv[sys.argv.index('--') + 1:]
before, after = sample(args[0]), sample(args[1])
assert before.keys() == after.keys() and len(after) == 65
changed, max_other = {}, 0.0
for title, (info, frames) in before.items():
    updated, poses = after[title]
    assert info.get('markers') == updated.get('markers') and frames.keys() == poses.keys(), title
    peak = 0.0
    for frame, bones in frames.items():
        for name, old in bones.items():
            new = poses[frame][name]
            distance = (old.translation - new.translation).length * 1000
            angle = old.to_quaternion().rotation_difference(new.to_quaternion()).angle
            angle = math.degrees(min(angle, 2 * math.pi - angle))
            if name.startswith('tabard '):
                peak = max(peak, distance)
            else:
                max_other = max(max_other, distance)
                assert distance < 0.05 and angle < 0.1, (title, frame, name, distance, angle)
    if peak > 0.05:
        changed[title] = round(peak, 3)
assert set(changed) == {'Run forward'}, changed

seams = []
for title, (info, frames) in after.items():
    first, last = info['frames']
    pairs = []
    if info['loop']:
        pairs.append(((title, first), (title, last)))
    src = info.get('seam_from') or (['Run forward', info['run_frame_start']] if 'run_frame_start' in info else None)
    dst = info.get('seam_to') or (['Run forward', info['run_frame_end']] if 'run_frame_end' in info else None)
    if not src and info.get('starts_from') in after:
        src = [info['starts_from'], after[info['starts_from']][0]['frames'][0]]
    if not dst and info.get('ends_in') in after:
        dst = [info['ends_in'], after[info['ends_in']][0]['frames'][0]]
    for ours, other in (((title, first), src), ((title, last), dst)):
        if other and other[0] in after:
            data = after[other[0]][0]
            low, high = data['frames']
            frame = low + (other[1] - low) % (high - low) if data['loop'] else other[1]
            pairs.append((ours, (other[0], frame)))
    for (a, fa), (b, fb) in pairs:
        pa, pb = after[a][1][fa], after[b][1][fb]
        worst = max((pa[n].translation - pb[n].translation).length * 1000 for n in pa)
        assert worst <= 0.005, (a, fa, b, fb, worst)
        seams.append({'from': [a, fa], 'to': [b, fb], 'max_mm': round(worst, 4)})

acceleration = {}
for name in ('tabard front.1', 'tabard front.2', 'tabard front.3'):
    values = []
    for data in (before, after):
        frames = data['Run forward'][1]
        positions = [frames[f][name].translation for f in sorted(frames)[:-1]]
        values.append(max((positions[(i + 1) % len(positions)] - positions[i] * 2 +
                          positions[i - 1]).length * 1000 for i in range(len(positions))))
    acceleration[name] = {'before_mm_f2': round(values[0], 3), 'after_mm_f2': round(values[1], 3)}
    assert values[1] <= values[0] + 1.0, (name, 'Added cloth acceleration exceeds 1mm/frame2', values)
report = {'clips': len(after), 'changed': changed, 'other_bones_max_mm': max_other,
          'seams': seams, 'cloth_acceleration': acceleration}
(H.ROOT / args[2]).write_text(json.dumps(report, indent=2), encoding='utf-8')
print('ROBE VERIFY', json.dumps({k: v for k, v in report.items() if k != 'seams'}), 'seams', len(seams), flush=True)
