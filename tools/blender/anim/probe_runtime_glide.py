"""Read-only v32 pose check approximating the runtime forward-glide accent.

Measures saved walk/hover sole clearance and renders before/after full-speed
forward lean. Uses the same bounded angles as FoundationMotionPose, no new clips.
"""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

sys.path.insert(0, str(Path(__file__).parent))
import hs_anim as H
import fullqa

H.require_background()
bpy.ops.wm.open_mainfile(filepath=str(H.ROOT / 'art/anim/hollow-saint-anim-v32.blend'))
rig = bpy.data.objects[H.RIG]
for track in rig.animation_data.nla_tracks:
    track.mute = True
poser = type('Rig', (), {'rig': rig, 'pb': rig.pose.bones,
                        'rest': {b.name: b.matrix_local.copy() for b in rig.data.bones}})()
poser.r3 = {name: matrix.to_3x3() for name, matrix in poser.rest.items()}
qa = fullqa.FullQA(poser)
scene = bpy.context.scene
H.eevee(scene, 12)
camera = bpy.data.objects.new('Runtime motion review', bpy.data.cameras.new('Runtime motion review'))
scene.collection.objects.link(camera)
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 3.1
camera.location = (3.7, -5.0, 2.2)
camera.rotation_euler = (Vector((H.MID_X, 0, 1.2)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
out = H.ROOT / 'artifacts/foundation/v051-motion-preview'
out.mkdir(parents=True, exist_ok=True)
report = {}


def measure(label):
    body, _, _ = qa._geo()
    report[label] = {side + '_sole_m': float(body[qa.reg_idx[side + ' foot'], 2].min()) for side in H.SIDES}
    body_axis = rig.pose.bones['neck'].head - rig.pose.bones['pelvis'].head
    report[label]['torso_forward_degrees'] = math.degrees(math.atan2(-body_axis.y, body_axis.z))
    H.render_still(scene, camera.name, out / (label + '.png'), (480, 600))


def pitch(name, degrees):
    bone = rig.pose.bones[name]
    pivot = bone.head.copy()
    bone.matrix = Matrix.Translation(pivot) @ Quaternion((1, 0, 0), math.radians(degrees)).to_matrix().to_4x4() @ Matrix.Translation(-pivot) @ bone.matrix
    bpy.context.view_layer.update()


for title, frame, label in [('Walk forward', 7, 'slow-walk'), ('Glide loop', 8, 'glide-before')]:
    action = bpy.data.actions[H.PREFIX + title]
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    measure(label)

feet = {side: rig.pose.bones[side + ' foot'].matrix.to_quaternion() for side in H.SIDES}
thrust = min(1.0, max(0.0, (11.165 - 2) / 10))
angle = 9 + 13 * thrust
pitch('pelvis', angle * 0.7)
pitch('spine', angle * 0.3)
pitch('head', -angle * 0.4)
for side, rotation in feet.items():
    foot = rig.pose.bones[side + ' foot']
    target = (Quaternion((1, 0, 0), math.radians(3 + 4 * thrust)) @ rotation).to_matrix().to_4x4()
    target.translation = foot.head.copy()
    foot.matrix = target
bpy.context.view_layer.update()
# Rendering evaluates the active action again. Key the temporary pose in memory
# so the comparison image includes it; this script never saves the checkpoint.
for name in ('pelvis', 'spine', 'head', 'L foot', 'R foot'):
    bone = rig.pose.bones[name]
    bone.keyframe_insert('rotation_quaternion', frame=8, group=name)
    bone.keyframe_insert('location', frame=8, group=name)
measure('glide-forward-drive')
assert report['glide-forward-drive']['torso_forward_degrees'] > report['glide-before']['torso_forward_degrees'] + 12
assert min(report['glide-forward-drive'][s + '_sole_m'] for s in H.SIDES) > 0.05
(out / 'pose-probe.json').write_text(json.dumps(report, indent=2))
print('MOTION POSE', json.dumps(report), flush=True)
