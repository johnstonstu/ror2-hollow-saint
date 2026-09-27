"""Build six bespoke animation studies without changing the source scenes."""
import bpy
import math
import json
import sys
import hashlib
from pathlib import Path
from mathutils import Vector, Euler, Matrix

if not bpy.app.background:
    raise RuntimeError('Run in an isolated background Blender process')
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'art/hybrid/v10-animation'
OUT.mkdir(exist_ok=True)
sys.path.insert(0, str(Path(__file__).parent))
from animation_rig_v10 import prepare_rig

sources = [ROOT/f'art/hybrid/hollow-saint-hybrid-v{v}.blend' for v in (7, 8, 9)]
hashes = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in sources}
bpy.ops.wm.open_mainfile(filepath=str(sources[-1]))
bpy.context.preferences.filepaths.save_version = 0
scene = bpy.context.scene
scene.render.fps = 24
rig = bpy.data.objects['Hollow Saint | v8 rig']
prepare_rig(rig)
catalog = []


def aim(name, direction):
    """Swing a bone toward a world direction while retaining its inherited roll."""
    bpy.context.view_layer.update()
    p = rig.pose.bones[name]
    matrix = p.matrix.copy()
    delta = matrix.to_3x3().col[1].normalized().rotation_difference(Vector(direction).normalized())
    result = delta.to_matrix().to_4x4() @ matrix
    result.translation = matrix.translation
    p.matrix = result
    bpy.context.view_layer.update()


def pose(frame, rotations=None, locations=None, arms=None, curl=0, crown=0):
    scene.frame_set(frame)
    for p in rig.pose.bones:
        p.matrix_basis.identity()
    for name, degrees in (rotations or {}).items():
        rig.pose.bones[name].rotation_quaternion = Euler(tuple(math.radians(v) for v in degrees)).to_quaternion()
    for name, offset in (locations or {}).items():
        rig.pose.bones[name].location = offset
    bpy.context.view_layer.update()
    for side, upper, fore in arms or []:
        aim(side+' upperarm', upper)
        aim(side+' forearm', fore)
    if curl:
        for side in ('L', 'R'):
            for digit in ('ring', 'little'):
                for i in range(1, 4):
                    p = rig.pose.bones[f'{side} {digit}.{i}']
                    p.rotation_quaternion = Euler((math.radians(curl), 0, 0)).to_quaternion()
    if 'Arc Bolt' in rig.animation_data.action.name and curl >= 18:
        for digit, dx in [('index', .02), ('middle', -.025)]:
            for i in range(1, 4):
                aim(f'R {digit}.{i}', (dx, -1, .025))
    for i in range(1, 5):
        p = rig.pose.bones[f'halo {i}']
        center = Vector((-.0375, .15, 1.79))
        rest = p.bone.head_local
        turn = Matrix.Rotation(math.radians(-82*crown), 3, 'X')
        target = center + turn @ ((rest-center)*(1+.22*crown))
        target.z += .53*crown
        p.location = p.bone.matrix_local.to_3x3().inverted() @ (target-rest)
        local_turn = p.bone.matrix_local.to_3x3().inverted() @ turn @ p.bone.matrix_local.to_3x3()
        p.rotation_quaternion = local_turn.to_quaternion()
    for p in rig.pose.bones:
        for channel in ('location', 'rotation_quaternion', 'scale'):
            p.keyframe_insert(data_path=channel, frame=frame, group=p.name)


def action(title, end, loop, events, preview):
    act = bpy.data.actions.new('HS_v10 | '+title)
    act.use_fake_user = True
    rig.animation_data.action = act
    act.use_frame_range = True
    act.frame_start, act.frame_end = 1, end
    act.use_cyclic = loop
    act['loop'] = loop
    act['events_json'] = json.dumps(events)
    for label, frame in events.items():
        act.pose_markers.new(label).frame = frame
    catalog.append({'name': act.name, 'title': title, 'end': end, 'loop': loop,
                    'events': events, 'preview': preview})
    return act


action('Idle - contained storm', 49, True, {'Inhale': 13, 'Exhale': 37}, 13)
for f, breath in [(1, 0), (13, 1), (25, 0), (37, -1), (49, 0)]:
    pose(f, {'spine': (breath*.8, 0, 0), 'head': (-breath*.8, 0, breath*1.1)},
         {'chest': (0, breath*.006, 0)}, crown=.025*breath)

action('Arc Bolt - two finger snap', 25, False,
       {'Anticipation': 5, 'Bolt release': 9, 'Recoil': 12, 'Recovered': 25}, 9)
pose(1)
pose(5, {'chest': (0, -5, 0)}, arms=[('R', (-.3, -.3, -.85), (.18, -.4, .8))], curl=12)
pose(9, {'chest': (0, 4, 0), 'head': (0, -4, 0)},
     arms=[('R', (-.12, -.90, -.32), (.04, -1, .08))], curl=25)
pose(12, {'chest': (0, 2, 0)}, arms=[('R', (-.14, -.83, -.38), (.1, -.98, .16))], curl=18)
pose(18, arms=[('R', (-.42, -.48, -.70), (.06, -.8, -.25))], curl=8)
pose(25)

charge_arms = [('L', (.42, -.40, -.78), (-.35, -.8, .48)),
               ('R', (-.42, -.40, -.78), (.35, -.8, .48))]
action('Charge - gathering current', 49, True,
       {'Pulse high': 13, 'Pulse low': 37}, 13)
for f, t in [(1, 0), (13, 1), (25, 0), (37, -1), (49, 0)]:
    pose(f, {'spine': (2+t*.5, 0, 0), 'head': (5+t, 0, 0)},
         arms=charge_arms, curl=12+3*t, crown=.13+.035*t)

action('Discharge - break the seal', 49, False,
       {'Gather': 13, 'Release': 22, 'Settle': 32, 'Recovered': 49}, 22)
pose(1)
pose(8, arms=charge_arms, curl=10, crown=.06)
pose(13, {'head': (6, 0, 0), 'chest': (2, 0, 0)}, arms=charge_arms, curl=20, crown=.18)
pose(20, {'chest': (-3, 0, 0)}, arms=[('L', (.8, -.4, -.3), (.4, -.9, .1)),
         ('R', (-.8, -.4, -.3), (-.4, -.9, .1))], crown=.4)
pose(22, {'head': (-3, 0, 0)}, arms=[('L', (.8, -.4, -.3), (.4, -.9, .1)),
         ('R', (-.8, -.4, -.3), (-.4, -.9, .1))], crown=.44)
pose(32, arms=charge_arms, crown=.12)
pose(49)

action('Arc Step - in place dash', 25, False,
       {'Compress': 6, 'Dash start': 9, 'Dash end': 15, 'Recovered': 25}, 10)
pose(1)
pose(6, {'spine': (8, 0, 0), 'head': (-5, 0, 0)},
     arms=[('L', (.45, .2, -.85), (.1, -.6, -.7)),
           ('R', (-.45, .2, -.85), (-.1, -.6, -.7))])
pose(9, {'spine': (14, 0, 0), 'head': (-11, 0, 0)},
     arms=[('L', (.45, .6, -.65), (.1, .4, -.8)),
           ('R', (-.45, .6, -.65), (-.1, .4, -.8))], crown=.08)
pose(15, {'spine': (12, 0, 0), 'head': (-10, 0, 0)},
     arms=[('L', (.45, .5, -.75), (.1, .35, -.8)),
           ('R', (-.45, .5, -.75), (-.1, .35, -.8))], crown=.06)
pose(20, {'spine': (-3, 0, 0), 'head': (2, 0, 0)})
pose(25)

action('Open Circuit - unfolding crown', 73, False,
       {'Unfold': 18, 'Crown active': 30, 'Recall': 49, 'Recovered': 73}, 30)
pose(1)
pose(10, arms=charge_arms, crown=.08)
for f, spread in [(18, .5), (30, 1), (42, 1.04), (49, 1)]:
    pose(f, {'head': (-4*spread, 0, 0)},
         arms=[('L', (.8, -.12, -.45), (.7, -.2, .3)),
               ('R', (-.8, -.12, -.45), (-.7, -.2, .3))], crown=spread)
pose(60, arms=charge_arms, crown=.3)
pose(73)

# Clamp handles at authored extremes to avoid overshoot around snappy casts.
for spec in catalog:
    act = bpy.data.actions[spec['name']]
    for layer in act.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for curve in bag.fcurves:
                    for key in curve.keyframe_points:
                        key.handle_left_type = key.handle_right_type = 'AUTO_CLAMPED'

rig.animation_data.action = bpy.data.actions[catalog[0]['name']]
scene.frame_start, scene.frame_end = 1, 48
scene.frame_set(1)
scene.camera = bpy.data.objects['Hero three quarter']
for obj in scene.objects:
    if obj.type == 'CAMERA':
        obj.data.ortho_scale = max(obj.data.ortho_scale, 3.25)
        obj.location.z += .23
        direction = Vector((-.035, 0, 1.38)) - obj.location
        obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
scene['animation_review'] = 'v10: six custom actions; see v10-animation/review.html'
scene['rig_status'] = 'Animation study; no Unity/export/game validation'
scene.render.engine = 'BLENDER_EEVEE'
scene.eevee.taa_render_samples = 16
scene.render.resolution_x = scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'art/hybrid/hollow-saint-hybrid-v10.blend'))
(OUT/'catalog.json').write_text(json.dumps(catalog, indent=2))
(OUT/'source-hashes.json').write_text(json.dumps(hashes, indent=2))
assert hashes == {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in sources}
print('V10 SAVED: six actions, source hashes unchanged', flush=True)
