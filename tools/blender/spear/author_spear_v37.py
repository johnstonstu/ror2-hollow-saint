"""Held spear, catch, throw and channel transitions from the actual fitted hand.

Isolated background Blender only. Original actions/checkpoints stay immutable.
"""
import ast
import json
import math
import sys
from pathlib import Path
from mathutils import Matrix, Quaternion, Vector

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'tools/blender/anim/armpass_v35.py'
OUT = ROOT / 'art/anim/hollow-saint-anim-v37.blend'
assert not OUT.exists(), 'Use a new numbered authoring checkpoint'
sys.path.insert(0, str(SOURCE.parent))
sys.argv = [str(SOURCE), '--', str(OUT)]
tree = ast.parse(SOURCE.read_text(encoding='utf-8'))
nodes = []
for node in tree.body:
    if isinstance(node, ast.Expr) and isinstance(node.value, ast.Call) and isinstance(node.value.func, ast.Name) and node.value.func.id == 'arm_ik_off':
        break
    if isinstance(node, ast.Assign) and any(isinstance(t, ast.Name) and t.id == 'SRC' for t in node.targets):
        node.value = ast.parse("H.ROOT/'art/concepts/spear-hand-fit01/hand-fit-study.blend'", mode='eval').body
    nodes.append(node)
ns = {'__file__': str(SOURCE), '__name__': 'spear_helpers'}
exec(compile(ast.fix_missing_locations(ast.Module(body=nodes, type_ignores=[])), str(SOURCE), 'exec'), ns)
bpy, rig, pb, H, HP = (ns[k] for k in ('bpy', 'rig', 'pb', 'H', 'HP'))
ns['arm_ik_off']()
rig.animation_data.action = None
HELD = {b.name: (b.location.copy(), b.rotation_quaternion.copy(), b.scale.copy()) for b in pb}
held_hand_world = pb['R hand'].matrix.to_3x3().normalized()
held_shoulder = pb['R upperarm'].head.copy()
held_wrist_offset = pb['R hand'].head - held_shoulder
socket = bpy.data.objects['Spear grip socket — hand locked']
socket_local = pb['R hand'].matrix.inverted() @ rig.matrix_world.inverted() @ socket.matrix_world
shaft_up = socket.matrix_world.to_3x3() @ Vector((0, 0, 1))
shaft_up.normalize()
base = ns['Clip']('Idle combat')
IDLE = dict(base.frames[0])
baseline = {act.name: { (fc.data_path, fc.array_index): [(tuple(k.co), k.interpolation) for k in fc.keyframe_points]
                      for fc in ns['fcurves'](act) } for act in bpy.data.actions if act.name.startswith(H.PREFIX)}
report = {'clips': {}, 'retained_actions': [], 'grip_socket_hand_local': [list(r) for r in socket_local]}


def mix(a, b, u):
    u = max(0, min(1, u))
    t = u * u * (3 - 2 * u)
    return a * (1 - t) + b * t


def new_clip(title, n, loop=False, markers=None):
    name = H.PREFIX + title
    if name not in bpy.data.actions:
        action = base.act.copy()
        action.name = name
        action.use_fake_user = True
    c = ns['Clip'](title)
    c.f0, c.f1 = 1, n
    c.info = {'title': title, 'frames': [1, n], 'loop': loop, 'markers': markers or {},
              'kind': 'spear gesture', 'pass': 'v37 fitted grip and soft transitions'}
    c.frames = [dict(IDLE) for _ in range(n)]
    return c


def place(frame, wrist, direction, finger=64, thumb=46, guide=0):
    ns['pose_from'](frame, base.bones)
    ns['P'].update()
    ns['solve_arm']('R', pb['R upperarm'].head + wrist, Vector((-1, 0.1, -0.6)))
    rotation = shaft_up.rotation_difference(direction.normalized()).to_matrix() @ held_hand_world
    pb['R hand'].rotation_quaternion = ns['basis_for']('R hand', rotation)
    for name, q in ns['hand_quats']('R', finger, thumb).items():
        pb[name].rotation_quaternion = q
    ns['P'].update()
    if guide > 0:
        ns['solve_arm']('L', pb['L upperarm'].head + Vector((-0.13, -0.30, -0.27)), Vector((1, 0.12, -0.7)))
        hand_q = ns['basis_for']('L hand', ns['frame_of'](Vector((-0.1, -1, 0.28)), Vector((-1, 0, -0.25)), 'L'))
        pb['L hand'].rotation_quaternion = pb['L hand'].rotation_quaternion.slerp(hand_q, guide)
        for name, q in ns['hand_quats']('L', 25 + 8 * guide, 24, 0.06).items():
            pb[name].rotation_quaternion = pb[name].rotation_quaternion.slerp(q, guide)
    ns['P'].update()
    ns['solve_followers']()
    ns['P'].update()
    ns['read_basis'](frame, ns['ARMS'])


def save(c):
    c.save()
    report['clips'][c.title] = c.info


hold = new_clip('Spear held', 49, True)
for i, frame in enumerate(hold.frames):
    u = i / 48
    breath = math.sin(2 * math.pi * u)
    wrist = held_wrist_offset + Vector((0.004 * breath, 0.005 * math.sin(4 * math.pi * u), 0.007 * breath))
    place(frame, wrist, shaft_up + Vector((0.012 * breath, 0.01 * breath, 0)))
hold.frames[-1] = dict(hold.frames[0])
save(hold)

# Same grasp, raised out of the ring silhouette while the crown is active.
crown = new_clip('Spear crown held', 49, True)
for i, frame in enumerate(crown.frames):
    u = i / 48
    place(frame, Vector((-0.27, -0.22, -0.16 + 0.007 * math.sin(2 * math.pi * u))), Vector((0.3, -0.15, 1)))
crown.frames[-1] = dict(crown.frames[0])
save(crown)

fan_wrist = Vector((-0.16, -0.43, -0.23))
fan_direction = Vector((0, -1, 0.035)).normalized()
fan = new_clip('Spear fan loop', 25, True)
for i, frame in enumerate(fan.frames):
    u = i / 24
    breath = math.sin(2 * math.pi * u)
    place(frame, fan_wrist + Vector((0.004 * breath, 0.006 * math.sin(4 * math.pi * u), 0.004 * breath)),
          fan_direction + Vector((0.009 * breath, 0, 0.006 * breath)), guide=1)
fan.frames[-1] = dict(fan.frames[0])
save(fan)


def frame_blend(a, b, u):
    result = dict(a)
    u = max(0, min(1, u))
    for bone in base.bones:
        ns['setq'](result, bone, ns['getq'](a, bone).slerp(ns['getq'](b, bone), u))
        for prop in ('location', 'scale'):
            ns['setv'](result, bone, ns['getv'](a, bone, prop).lerp(ns['getv'](b, bone, prop), u), prop)
    return result


for title, n, start, finish in (
    ('Spear fan start', 9, hold.frames[0], fan.frames[0]),
    ('Spear fan end', 13, fan.frames[0], hold.frames[0]),
):
    c = new_clip(title, n)
    for i in range(n):
        u = i / (n - 1)
        c.frames[i] = frame_blend(start, finish, H.smoother(u))
    save(c)

catch = new_clip('Spear catch', 20, markers={'Spear caught': 14})
free_wrist = pb['R hand'].head - pb['R upperarm'].head
ns['pose_from'](IDLE, base.bones)
ns['P'].update()
free_wrist = pb['R hand'].head - pb['R upperarm'].head
reach = held_wrist_offset + Vector((-0.035, -0.11, 0.08))
for i, frame in enumerate(catch.frames):
    f = i + 1
    if f <= 8:
        wrist = mix(free_wrist, reach, (f - 1) / 7)
    else:
        wrist = mix(reach, held_wrist_offset, (f - 8) / 11)
    grip_level = mix(8, 64, (f - 10) / 4)
    place(frame, wrist, shaft_up, finger=grip_level, thumb=mix(14, 46, (f - 10) / 4))
catch.frames[0] = dict(IDLE)
catch.frames[-1] = dict(hold.frames[0])
save(catch)

throw = new_clip('Conduit Spear', 20, markers={'Spear release': 7})
windup = Vector((-0.32, 0.04, -0.18))
release = Vector((-0.18, -0.49, -0.15))
for i, frame in enumerate(throw.frames):
    f = i + 1
    if f <= 4:
        wrist = mix(held_wrist_offset, windup, (f - 1) / 3)
        direction = mix(shaft_up, Vector((0.12, -0.62, 0.78)), (f - 1) / 3)
    elif f <= 8:
        wrist = mix(windup, release, (f - 4) / 3)
        direction = mix(Vector((0.12, -0.62, 0.78)), fan_direction, (f - 4) / 3)
    else:
        wrist = mix(release, free_wrist, (f - 8) / 12)
        direction = mix(fan_direction, shaft_up, (f - 8) / 12)
    fingers = 64 if f < 7 else mix(47, 8, (f - 7) / 2) if f <= 10 else mix(8, 21, (f - 10) / 10)
    thumb = 46 if f < 7 else mix(32, 14, (f - 7) / 3)
    place(frame, wrist, direction, finger=fingers, thumb=thumb)
throw.frames[0] = dict(hold.frames[0])
throw.frames[-1] = dict(IDLE)
save(throw)
recover = new_clip('Conduit Spear recover', 13)
recover.frames = [dict(IDLE) for _ in range(13)]
save(recover)

# Contract: fixed grasp and exact transition/loop seams; old actions stay exact.
for c in (hold, crown, fan):
    assert c.frames[0] == c.frames[-1], c.title + ' loop seam'
for a, b in ((throw.frames[0], hold.frames[0]), (catch.frames[-1], hold.frames[0]),
             (throw.frames[-1], recover.frames[0])):
    assert a == b, 'Spear boundary mismatch'
for act in bpy.data.actions:
    if act.name not in baseline or act.name in (H.PREFIX + 'Conduit Spear', H.PREFIX + 'Conduit Spear recover'):
        continue
    now = {(fc.data_path, fc.array_index): [(tuple(k.co), k.interpolation) for k in fc.keyframe_points] for fc in ns['fcurves'](act)}
    assert now == baseline[act.name], 'Unrelated action changed: ' + act.name
    report['retained_actions'].append(act.name)
for frame in [hold.frames[0], fan.frames[0], crown.frames[0]] + throw.frames[:6] + catch.frames[13:]:
    for name in ns['FING']['R']:
        assert ns['getq'](frame, name).rotation_difference(ns['getq'](hold.frames[0], name)).angle < 0.00001, 'Grasp changed: ' + name
rig.animation_data.action = hold.act
scene = bpy.context.scene
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT))
directory = ROOT / 'art/anim/v37'
directory.mkdir(parents=True, exist_ok=True)
(directory / 'author-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('SPEAR_V37_SAVED', len(report['clips']), 'retained', len(report['retained_actions']), flush=True)
