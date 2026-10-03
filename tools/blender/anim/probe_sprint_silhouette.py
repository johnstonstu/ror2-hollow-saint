"""Read-only source-rig approximation of the runtime sprint silhouette accent.

Never saves a .blend or changes clips/bundles. Compare old/new rear and side
views; measure knees, ankle span, soles, and tabard-to-body penetration on
four glide phases plus partially weighted enter/exit poses.
"""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector
from mathutils.bvhtree import BVHTree

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
camera = bpy.data.objects.new('Sprint silhouette review', bpy.data.cameras.new('Sprint silhouette review'))
scene.collection.objects.link(camera)
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 3.1
out = H.ROOT / 'artifacts/foundation/sprint-silhouette-preview'
out.mkdir(parents=True, exist_ok=True)
report = {'method': 'Source-rig FK approximation; raw penetration, not rest-subtracted FullQA.', 'samples': []}
modified = ['pelvis', 'spine', 'head'] + [s + ' ' + b for s in H.SIDES for b in ('thigh', 'shin', 'foot')]


def rotate(name, axis, degrees):
    bone = rig.pose.bones[name]
    pivot = bone.head.copy()
    bone.matrix = Matrix.Translation(pivot) @ Quaternion(axis, math.radians(degrees)).to_matrix().to_4x4() @ Matrix.Translation(-pivot) @ bone.matrix
    bpy.context.view_layer.update()


def accent(new, weight, bank=0):
    pb = rig.pose.bones
    feet = {s: pb[s + ' foot'].matrix.to_quaternion() for s in H.SIDES}
    thrust = (11.165 - 2) / 10
    angle = (8 + 11 * thrust) if new else (9 + 13 * thrust)
    rotate('pelvis', (0, -1, 0), bank * weight)
    rotate('pelvis', (1, 0, 0), angle * .7 * weight)
    rotate('spine', (1, 0, 0), angle * .3 * weight)
    rotate('head', (1, 0, 0), -angle * .4 * weight)
    if new:
        lateral = pb['L thigh'].head - pb['R thigh'].head
        lateral.z = 0
        lateral.normalize()
        for side, sign in [('L', 1), ('R', -1)]:
            thigh, shin, foot = [pb[side + ' ' + n] for n in ('thigh', 'shin', 'foot')]
            rotate(thigh.name, Vector((0, 0, -1)).cross(lateral * sign).normalized(), 6 * weight)
            upper = (shin.head - thigh.head).normalized()
            lower = (foot.head - shin.head).normalized()
            hinge = upper.cross(lower).normalized()
            current = math.degrees(upper.angle(lower))
            target = 44 + 4 * thrust + (2 if side == 'R' else 0)
            flex = max(0, min(18, target - current)) * weight
            rotate(thigh.name, hinge, -flex * .35)
            rotate(shin.name, hinge, flex)
    for side, original in feet.items():
        foot = pb[side + ' foot']
        target = (Quaternion((1, 0, 0), math.radians((3 + 4 * thrust) * weight)) @ original).to_matrix().to_4x4()
        target.translation = foot.head.copy()
        foot.matrix = target
    bpy.context.view_layer.update()


def measure():
    pb = rig.pose.bones
    body, _, groups = qa._geo()
    tree = BVHTree.FromPolygons([Vector(v) for v in body], qa.bpolys)
    overlaps = {}
    for group in ('tabard front', 'tabard back'):
        for v in groups[group]['co'][::fullqa.STEP]:
            depth, polygon = fullqa.depth(tree, Vector(v))
            if depth:
                key = group + ' > ' + qa.poly_reg[polygon]
                overlaps[key] = max(overlaps.get(key, 0), round(depth * 1000, 3))
    result = {'sole_m': {s: float(body[qa.reg_idx[s + ' foot'], 2].min()) for s in H.SIDES},
              'ankle_span_m': abs(pb['L foot'].head.x - pb['R foot'].head.x),
              'raw_tabard_penetration_mm': overlaps,
              'knee_degrees': {}}
    for s in H.SIDES:
        upper = pb[s + ' shin'].head - pb[s + ' thigh'].head
        lower = pb[s + ' foot'].head - pb[s + ' shin'].head
        result['knee_degrees'][s] = math.degrees(upper.angle(lower))
    axis = pb['neck'].head - pb['pelvis'].head
    result['torso_forward_degrees'] = math.degrees(math.atan2(-axis.y, axis.z))
    return result


for title, frame, weight, bank in [('Glide loop', f, 1, 0) for f in (1, 8, 16, 24)] + [
        ('Glide enter', 4, .4, 0), ('Glide exit', 10, .2, 0), ('Glide loop', 8, 1, 13), ('Glide loop', 8, 1, -13),
        ('Walk forward', 7, 0, 0)]:
    sample = dict(clip=title, frame=frame, glide_weight=weight, bank_degrees=bank)
    for new in (False, True):
        action = bpy.data.actions[H.PREFIX + title].copy()
        rig.animation_data.action = action
        rig.animation_data.action_slot = action.slots[0]
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        accent(new, weight, bank)
        label = 'new' if new else 'old'
        sample[label] = measure()
        if title == 'Glide loop' and frame == 8 and bank == 0:
            # Rendering re-evaluates animation: key only the in-memory copy.
            for name in modified:
                bone = rig.pose.bones[name]
                bone.keyframe_insert('rotation_quaternion', frame=frame, group=name)
                bone.keyframe_insert('location', frame=frame, group=name)
            for view, location in [('rear', (0, 5, 1.9)), ('side', (5, 0, 1.9))]:
                camera.location = location
                camera.rotation_euler = (Vector((H.MID_X, 0, 1.2)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
                H.render_still(scene, camera.name, out / (label + '-' + view + '.png'), (480, 600))
        rig.animation_data.action = None
        bpy.data.actions.remove(action)
    report['samples'].append(sample)
    print('SPRINT SAMPLE', json.dumps(sample), flush=True)

(out / 'pose-probe.json').write_text(json.dumps(report, indent=2))
for row in report['samples']:
    for contact, depth in row['new']['raw_tabard_penetration_mm'].items():
        assert depth <= max(5, row['old']['raw_tabard_penetration_mm'].get(contact, 0) + .5)
    for side in H.SIDES:
        assert row['new']['sole_m'][side] >= row['old']['sole_m'][side] - .005
    if row['glide_weight'] == 0:
        assert abs(row['new']['ankle_span_m'] - row['old']['ankle_span_m']) < .00001
        for side in H.SIDES:
            assert abs(row['new']['sole_m'][side] - row['old']['sole_m'][side]) < .00001
            assert abs(row['new']['knee_degrees'][side] - row['old']['knee_degrees'][side]) < .001
    if row['clip'] == 'Glide loop':
        assert row['new']['ankle_span_m'] > row['old']['ankle_span_m'] + .05
        assert min(row['new']['sole_m'].values()) > .05
        for side in H.SIDES:
            assert row['new']['knee_degrees'][side] > row['old']['knee_degrees'][side] + 8
            assert row['new']['knee_degrees'][side] < 55
print('SPRINT SILHOUETTE CHECKS PASS', flush=True)
