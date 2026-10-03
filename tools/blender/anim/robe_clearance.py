"""Measured Run forward robe clearance over saved FK, preserving non-cloth curves.

Usage after --: SOURCE.blend CANDIDATE.blend
The accepted v32 pass changes only Run forward. Other poses need separate art
work: applying this pass broadly caused regressions. Sources are never overwritten.
Run fullqa_audit.py and robe_verify.py on any newly generated candidate.
"""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Quaternion, Vector
from mathutils.bvhtree import BVHTree

sys.path.insert(0, str(Path(__file__).parent))
import hs_anim as H
import fullqa
from padpass import smooth_series

MAX_SWING = 0.1  # radians (5.73 degrees), a bounded additional outward swing
TARGET_DEPTH = 0.003
BONE_SEGMENT = '.1'


def evaluate(qa, group):
    body, _, groups = qa._geo()
    tree = BVHTree.FromPolygons([Vector(v) for v in body], qa.bpolys)
    baseline = qa.rest['contact'][group]
    worst = 0.0
    for i, point in enumerate(groups[group]['co'][::fullqa.STEP]):
        if baseline[i] > fullqa.BURIED:
            continue
        depth, _ = fullqa.depth(tree, Vector(point))
        worst = max(worst, depth - baseline[i])
    return worst


H.require_background()
args = sys.argv[sys.argv.index('--') + 1:]
source, output = (H.ROOT / path for path in args[:2])
assert not output.exists(), 'Never overwrite a candidate'
source_stamp = source.stat().st_mtime_ns
bpy.ops.wm.open_mainfile(filepath=str(source))
bpy.context.preferences.filepaths.save_version = 0
rig = bpy.data.objects[H.RIG]
for track in rig.animation_data.nla_tracks:
    track.mute = True
rig.animation_data.action = None
rig.data.pose_position = 'REST'
bpy.context.view_layer.update()
poser = type('SavedRig', (), {'rig': rig, 'pb': rig.pose.bones,
                            'rest': {b.name: b.matrix_local.copy() for b in rig.data.bones}})()
poser.r3 = {n: m.to_3x3() for n, m in poser.rest.items()}
qa = fullqa.FullQA(poser)
qa.calibrate_rest()
rig.data.pose_position = 'POSE'
report = {}
for action in list(bpy.data.actions):
    if not action.name.startswith(H.PREFIX):
        continue
    title = action.name[len(H.PREFIX):]
    if title != 'Run forward':
        continue
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    frames = range(int(action.frame_range[0]), int(action.frame_range[1]) + 1)
    corrections = {}
    for frame in frames:
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        corrections[frame] = {}
        for side, direction in (('front', -1.0), ('back', 1.0)):
            group = 'tabard ' + side
            bone = rig.pose.bones[group + BONE_SEGMENT]
            original = bone.rotation_quaternion.copy()
            initial = evaluate(qa, group)
            best, shift = initial, 0.0
            if initial > TARGET_DEPTH:
                for step in range(1, 15):
                    test = MAX_SWING * step / 14
                    bone.rotation_quaternion = original @ Quaternion((1, 0, 0), direction * test)
                    bpy.context.view_layer.update()
                    depth = evaluate(qa, group)
                    if depth < best - 0.0001:
                        best, shift = depth, test
                    if depth <= TARGET_DEPTH:
                        break
            bone.rotation_quaternion = original
            bpy.context.view_layer.update()
            corrections[frame][side] = {'base': list(original), 'swing_rad': shift,
                                       'before_mm': initial * 1000, 'raw_solve_mm': best * 1000}
    info = json.loads(action['clip_json'])
    loop = bool(info.get('loop'))
    count = len(frames) - 1 if loop else len(frames)
    anchors = set(info.get('seam_anchors', [frames[0]]) if loop else [frames[0], frames[-1]])
    for side in ('front', 'back'):
        raw = [0.0 if frame in anchors else corrections[frame][side]['swing_rad'] for frame in frames]
        filtered = smooth_series(raw, count, max, [frame - frames[0] for frame in anchors])
        for i, frame in enumerate(frames):
            corrections[frame][side]['swing_rad'] = filtered[i]
    for frame, sides in corrections.items():
        bpy.context.scene.frame_set(frame)
        for side, item in sides.items():
            bone = rig.pose.bones['tabard ' + side + BONE_SEGMENT]
            direction = -1.0 if side == 'front' else 1.0
            bone.rotation_quaternion = Quaternion(item['base']) @ Quaternion((1, 0, 0), direction * item['swing_rad'])
            bone.keyframe_insert('rotation_quaternion', frame=frame, group=bone.name)
    report[title] = corrections
    peak = max(v['swing_rad'] for r in corrections.values() for v in r.values())
    print('ROBE', title, 'max swing deg', round(math.degrees(peak), 2), flush=True)
output.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(output))
assert source.stat().st_mtime_ns == source_stamp, 'Source changed'
output.with_suffix('.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
