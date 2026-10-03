"""Small deterministic spring response for the tabard, applied before collision correction.

Waist acceleration, turn velocity and travel drag drive the existing cloth bones.
The waist is firm; the hem responds more slowly. Seam anchors have zero added
motion so existing authored handoffs remain exact. No Blender cloth modifier or
runtime simulation is required: hs_anim.bake captures the result as ordinary FK.
"""
import math

from mathutils import Vector

import hs_anim as H

ENABLED = True
GAIN = 2.0
LIMIT = Vector((3.0, 3.0, 1.5))
WEIGHTS = (0.35, 0.65, 0.85)
STIFFNESS = (110.0, 85.0, 65.0)
DAMPING = (21.0, 18.5, 16.5)
# Narrow knee/hem clearances need less secondary motion than open running poses.
# These are animation amplitudes, not collision-QA exemptions.
POSE_GAIN = {'Descend': 0.0, 'Run forward right': 0.15, 'Run backward right': 0.0,
             'Plant turn 90 right': 0.15, 'Arc Step end': 0.15, 'Spawn': 0.0,
             'Glide exit': 0.15, 'Run pivot 180 left': 0.0, 'Run pivot 180 right': 0.15,
             'Run lean left': 0.15, 'Run backward': 0.15}


def soft_bound(value):
    return Vector(tuple(limit * math.tanh(v / limit) for v, limit in zip(value, LIMIT)))


def difference(values, i, loop):
    n = len(values)
    a, b = ((i - 1) % n, (i + 1) % n) if loop else (max(0, i - 1), min(n - 1, i + 1))
    return (values[b] - values[a]) * H.FPS / (2 if loop else max(1, b - a))


def envelope(i, count, loop, anchors):
    distances = [min(abs(i - p), count - abs(i - p)) if loop else abs(i - p) for p in anchors]
    distance = min(distances, default=count)
    return math.sin(0.5 * math.pi * min(distance / 3.0, 1.0)) ** 2


def prepare(poser, frames, pose_fn, loop, meta, legs_ik, arms_ik, title=''):
    if not ENABLED or POSE_GAIN.get(title, 1.0) == 0.0:
        return {}
    meta = meta or {}
    count = len(frames) - 1 if loop else len(frames)
    positions, rotations = [], []
    for frame in frames[:count]:
        poser.reset()
        poser.ik(legs_ik, arms_ik)
        pose_fn(poser, frame)
        poser.update()
        matrix = poser.pb['pelvis'].matrix
        positions.append(matrix.translation.copy())
        delta = matrix.to_3x3() @ poser.r3['pelvis'].inverted()
        rotations.append(Vector(tuple(math.degrees(a) for a in delta.to_euler('XYZ'))))
    targets = targets_for(positions, rotations, meta, loop)
    anchors = [frames.index(f) for f in meta.get('seam_anchors', [frames[0]]) if f in frames] if loop else [0, count - 1]
    result = {f: {} for f in frames}
    for chain in (H.TABARD_FRONT, H.TABARD_BACK):
        for depth, bone in enumerate(chain):
            followed = H.spring_follow(targets, loop, stiffness=STIFFNESS[depth], damping=DAMPING[depth], passes=8)
            for i, frame in enumerate(frames):
                index = i % count
                response = soft_bound(followed[index] * WEIGHTS[depth] * GAIN * POSE_GAIN.get(title, 1.0))
                result[frame][bone] = response * envelope(index, count, loop, anchors)
    poser.reset()
    return result


def targets_for(positions, rotations, meta, loop):
    count = len(positions)
    velocity = [difference(positions, i, loop) for i in range(count)]
    move = H.move_vector(meta)
    speeds = meta.get('speed_curve', [meta.get('speed_mps', 0.0) or 0.0] * count)
    moves = meta.get('move_vectors', [move] * count)
    root = [Vector((-moves[min(i, len(moves)-1)][0], -moves[min(i, len(moves)-1)][1], 0))
            * speeds[min(i, len(speeds) - 1)] for i in range(count)]
    headings = meta.get('turn_curve', [0.0] * count)
    targets = []
    for i in range(count):
        acceleration = difference(velocity, i, loop) + difference(root, i, loop)
        angular = difference(rotations, i, loop)
        a, b = ((i-1) % count, (i+1) % count) if loop else (max(0, i-1), min(count-1, i+1))
        span = 2 if loop else max(1, b-a)
        # Shortest-angle differences avoid artificial impulses at +/-180 degrees.
        angular.z = ((rotations[b].z-rotations[a].z+180) % 360-180) * H.FPS / span
        turn_rate = (headings[min(b, len(headings)-1)]-headings[min(a, len(headings)-1)]) * H.FPS / span
        acceleration += Vector((0, 0, math.radians(turn_rate))).cross(root[i])
        angular.z += turn_rate
        target = Vector((-0.10 * acceleration.y - 0.12 * root[i].y,
                         0.10 * acceleration.x - 0.025 * angular.y,
                         -0.015 * angular.z))
        targets.append(soft_bound(target))
    return targets


def apply(poser, offsets):
    for bone, angles in offsets.items():
        basis = poser.r3[bone]
        offset = (basis.inverted() @ H.R(*angles) @ basis).to_quaternion()
        poser.pb[bone].rotation_quaternion = offset @ poser.pb[bone].rotation_quaternion


def report(motion):
    return {'cloth_motion_max_deg': {bone: round(max(v[bone].length for v in motion.values()), 3)
                                   for bone in next(iter(motion.values()), {})}}
