"""Add a first-pass game-style deformation rig and three animation actions to hybrid v7.

This preserves v7 and saves a new v8 review file. The supplied HF body is a fused
triangular mesh, so this deliberately uses simple nearest-segment weights rather
than claiming production skinning quality.
"""
import bpy
import math
from mathutils import Vector
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'art/hybrid/hollow-saint-hybrid-v7.blend'
OUT = ROOT / 'art/hybrid/hollow-saint-hybrid-v8.blend'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
scene.render.fps = 24
scene.frame_start = 1
scene.frame_end = 48

body = bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
arm_data = bpy.data.armatures.new('Hollow Saint | deformation skeleton')
rig = bpy.data.objects.new('Hollow Saint | v8 rig', arm_data)
scene.collection.objects.link(rig)
rig.show_in_front = True
rig['status'] = 'FIRST PASS RIG STUDY | review deformation before game use'
rig['source'] = 'hybrid v7 preserved; source HF body retained'

bones = {
    'root': ((0, 0, 0.02), (0, 0, 0.95), None, False),
    'pelvis': ((0, 0, 0.88), (0, 0, 1.08), 'root', True),
    'spine': ((0, 0, 1.02), (0, 0, 1.40), 'pelvis', True),
    'chest': ((0, 0, 1.36), (0, 0, 1.52), 'spine', True),
    'neck': ((0, 0, 1.49), (0, 0, 1.59), 'chest', True),
    'head': ((0, 0, 1.57), (0, 0, 1.80), 'neck', True),
}
for side, s in [('L', 1), ('R', -1)]:
    bones.update({
        side+' thigh': ((s*.13, 0, 1.00), (s*.16, 0, .58), 'pelvis', True),
        side+' shin': ((s*.16, 0, .58), (s*.18, 0, .14), side+' thigh', True),
        side+' foot': ((s*.18, 0, .14), (s*.18, .16, .055), side+' shin', True),
        side+' upperarm': ((s*.17, 0, 1.42), (s*.34, 0, 1.20), 'chest', True),
        side+' forearm': ((s*.34, 0, 1.20), (s*.47, 0, .98), side+' upperarm', True),
        side+' hand': ((s*.47, 0, .98), (s*.50, 0, .86), side+' forearm', True),
    })
    for finger, spread in [('index', .012), ('middle', .004), ('ring', -.006), ('little', -.018), ('thumb', .035)]:
        parent = side+' hand'
        for i in range(1, 4):
            name = f'{side} {finger}.{i}'
            x = s*(.50 + spread + (i-1)*.002)
            z = .87 - (i-1)*.035
            bones[name] = ((x, -.015, z), (x, -.015, z-.034), parent, True)
            parent = name
    bones[side+' scapula'] = ((s*.14, 0, 1.48), (s*.22, 0, 1.42), 'chest', False)
for i, x in enumerate([-.17, .17, .17, -.17], 1):
    bones[f'halo {i}'] = ((x, .09, 1.55), (x, .09, 1.72), 'chest', False)

bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
for name, (head, tail, parent, deform) in bones.items():
    eb = arm_data.edit_bones.new(name)
    eb.head, eb.tail = head, tail
    eb.use_deform = deform
for name, (_, _, parent, _) in bones.items():
    if parent:
        arm_data.edit_bones[name].parent = arm_data.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')

def segment_distance(p, a, b):
    ab = b-a
    t = max(0.0, min(1.0, (p-a).dot(ab) / max(ab.length_squared, 1e-9)))
    return (p-(a+t*ab)).length

# Vertex groups blend across the closest two deforming bones. Coordinates are
# evaluated in the character's shared world space and written in mesh vertex order.
deform_bones = [(n, Vector(v[0]), Vector(v[1])) for n,v in bones.items() if v[3]]
groups = {name: body.vertex_groups.new(name=name) for name,_,_ in deform_bones}
for v in body.data.vertices:
    p = body.matrix_world @ v.co
    distances = sorted((segment_distance(p,a,b),name) for name,a,b in deform_bones)[:2]
    inv = [1.0 / max(d, .012)**3 for d,_ in distances]
    total = sum(inv)
    for (distance,name), weight in zip(distances, inv):
        groups[name].add([v.index], weight/total, 'REPLACE')
mod = body.modifiers.new('Hollow Saint v8 | armature deformation', 'ARMATURE')
mod.object = rig
body.parent = rig
body['rig_note'] = 'Approximate nearest-segment weights; fused source mesh needs deformation review.'

def assign_bone(obj, bone_name):
    if obj in (body, rig) or obj.name == 'Warm gray studio ground':
        return
    old = obj.matrix_world.copy()
    obj.parent = rig
    obj.parent_type = 'BONE'
    obj.parent_bone = bone_name
    obj.matrix_world = old
    obj['rig_parent_bone'] = bone_name

for obj in list(scene.objects):
    if obj in (body, rig) or obj.name == 'Warm gray studio ground':
        continue
    n = obj.name.lower()
    side = 'L' if n.startswith('l ') else ('R' if n.startswith('r ') else None)
    bone = 'chest'
    if 'halo' in n:
        import re
        m = re.search(r'(?:arc |halo )(\d)', n)
        if m: bone = 'halo '+m.group(1)
    elif 'hand' in n and side:
        finger = next((f for f in ('index','middle','ring','little','thumb') if f in n), None)
        m = __import__('re').search(r'segment (\d)', n)
        bone = f"{side} {finger}.{m.group(1)}" if finger and m else side+' hand'
        if 'knuckle' in n: bone = side+' hand'
    elif ('shoulder' in n or 'scapula' in n) and side:
        bone = side+' scapula'
    elif any(k in n for k in ('tabard','hip')):
        bone = 'pelvis'
    elif 'head' in n or 'mask' in n or 'face' in n:
        bone = 'head'
    elif 'back' in n or 'spine' in n or 'yoke' in n or 'conductor' in n:
        bone = 'chest'
    elif side:
        if 'foot' in n or 'toe' in n: bone = side+' foot'
        elif 'shin' in n or 'calf' in n or 'ankle' in n: bone = side+' shin'
        elif 'knee' in n or 'thigh' in n: bone = side+' thigh'
        elif 'wrist' in n or 'palm' in n: bone = side+' hand'
        elif 'forearm' in n or 'elbow' in n: bone = side+' forearm'
        elif 'arm' in n or 'shoulder' in n: bone = side+' upperarm'
    assign_bone(obj, bone)

def action(name, end):
    scene.frame_end = end
    bpy.ops.object.mode_set(mode='OBJECT') if rig.mode != 'OBJECT' else None
    bpy.context.view_layer.objects.active = rig
    rig.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True  # retain inactive actions in the delivered .blend
    rig.animation_data.action = act
    return act

def key_pose(frame, rotations=None, locations=None):
    scene.frame_set(frame)
    rotations = rotations or {}
    locations = locations or {}
    for name, pb in rig.pose.bones.items():
        pb.rotation_mode = 'XYZ'
        pb.rotation_euler = rotations.get(name, (0,0,0))
        pb.location = locations.get(name, (0,0,0))
        pb.keyframe_insert(data_path='rotation_euler', frame=frame, group=name)
        if name in locations:
            pb.keyframe_insert(data_path='location', frame=frame, group=name)

# Calm readable two-second loop, matching the contained-storm charge sheet.
act = action('HS_v8 | Idle contained storm | 2s loop', 48)
key_pose(1, {'spine':(0,0,0), 'head':(0,0,0), 'L upperarm':(0,0,0), 'R upperarm':(0,0,0)})
key_pose(13, {'spine':(.012,0,0), 'head':(-.015,0,.02), 'L upperarm':(0,0,-.025), 'R upperarm':(0,0,.025)}, {'spine':(0,0,.012)})
key_pose(25, {'spine':(0,0,0), 'head':(0,0,0), 'L upperarm':(0,0,0), 'R upperarm':(0,0,0)})
key_pose(37, {'spine':(-.012,0,0), 'head':(.015,0,-.02), 'L upperarm':(0,0,.025), 'R upperarm':(0,0,-.025)}, {'spine':(0,0,-.012)})
key_pose(49, {'spine':(0,0,0), 'head':(0,0,0), 'L upperarm':(0,0,0), 'R upperarm':(0,0,0)})
act.use_cyclic = True

# Right hand aimed snap: restrained point, short anticipation, strike, recover.
act = action('HS_v8 | Arc Bolt point and snap', 32)
key_pose(1)
key_pose(6, {'spine':(-.025,0,-.03), 'R upperarm':(.22,0,.08), 'R forearm':(.08,0,-.08), 'R hand':(.02,0,0), 'head':(-.025,0,-.025)})
key_pose(10, {'spine':(-.015,0,-.04), 'R upperarm':(1.12,0,.06), 'R forearm':(.72,0,-.04), 'R hand':(.08,0,0), 'head':(-.01,0,-.04)})
key_pose(14, {'spine':(0,0,0), 'R upperarm':(1.18,0,.04), 'R forearm':(.68,0,-.03), 'R hand':(.06,0,0), 'head':(0,0,0)})
key_pose(24, {'spine':(0,0,0), 'R upperarm':(.65,0,.02), 'R forearm':(.34,0,0), 'R hand':(.02,0,0)})
key_pose(32)

# Charge then vent: brace the torso, spread arms and petals, return to rest.
act = action('HS_v8 | Overcharge brace and release', 48)
key_pose(1)
key_pose(10, {'spine':(-.035,0,0), 'L upperarm':(.34,0,.06), 'R upperarm':(.34,0,-.06), 'head':(-.02,0,0)})
key_pose(20, {'spine':(-.06,0,0), 'L upperarm':(.92,0,.18), 'R upperarm':(.92,0,-.18), 'L forearm':(.22,0,0), 'R forearm':(.22,0,0)})
key_pose(28, {'spine':(.04,0,0), 'L upperarm':(.68,0,.08), 'R upperarm':(1.12,0,-.10), 'R forearm':(.68,0,0), 'head':(-.03,0,-.03)}, {'root':(0,0,.012)})
key_pose(34, {'spine':(0,0,0), 'L upperarm':(.24,0,0), 'R upperarm':(.78,0,-.08), 'R forearm':(.42,0,0), 'head':(0,0,0)}, {'root':(0,0,0)})
key_pose(48)

scene.frame_end = 48
scene.frame_set(1)
scene['animation_review'] = 'Three first-pass actions: Idle, Arc Bolt, Overcharge. Inspect weights and poses before game integration.'
scene['rig_status'] = 'Version 8 review rig, not production or in-game validated'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT))
print('SAVED', OUT)
print('BONES', len(arm_data.bones), 'ACTIONS', [a.name for a in bpy.data.actions if a.name.startswith('HS_v8')])
