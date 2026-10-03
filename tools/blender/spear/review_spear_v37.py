"""Fitted spear animation/VFX review; Blender visualization, not game footage."""
import bpy
import json
import math
import random
import sys
from pathlib import Path
from mathutils import Matrix, Quaternion, Vector

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/blender/anim'))
import hs_anim as H
H.require_background()
mode = sys.argv[sys.argv.index('--') + 1] if '--' in sys.argv else 'keys'
OUT = ROOT / 'artifacts/spear-v37-review01' / mode
assert not OUT.exists(), 'Preserve previous reviews; use another numbered folder'
OUT.mkdir(parents=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'art/anim/hollow-saint-anim-v37.blend'))
scene = bpy.context.scene
H.eevee(scene, 16)
rig = bpy.data.objects[H.RIG]
pb = rig.pose.bones
rig.animation_data.action = None
for t in rig.animation_data.nla_tracks:
    t.mute = True
socket = bpy.data.objects['Spear grip socket — hand locked']
weapon = list(socket.children)
flight = bpy.data.objects.new('Review detached spear', None)
scene.collection.objects.link(flight)
flight_parts = []
for original in weapon:
    copy = original.copy()
    scene.collection.objects.link(copy)
    copy.parent = flight
    copy.matrix_parent_inverse = Matrix.Identity(4)
    copy.matrix_basis = original.matrix_basis.copy()
    copy.hide_render = True
    flight_parts.append(copy)
for ob in scene.objects:
    if ob.type == 'CURVE' and ob not in weapon:
        ob.hide_render = True
cyan = bpy.data.materials['Study connected current']
white = bpy.data.materials['Study electric core']
fx = []
finger_names = ('index', 'middle', 'ring', 'little', 'thumb')
arms = {b.name for b in pb if b.name.startswith(('R ', 'L ')) and
        any(p in b.name for p in ('shoulder', 'scapula', 'pauldron', 'upperarm', 'forearm', 'hand', 'muzzle') + finger_names)}
right = {b for b in arms if b.startswith('R ')}
upper = arms | {'spine', 'chest', 'neck', 'head'}


def values(title, frame):
    action = bpy.data.actions[H.PREFIX + title]
    return {(fc.data_path, fc.array_index): fc.evaluate(frame) for layer in action.layers
            for strip in layer.strips for bag in strip.channelbags for fc in bag.fcurves}


def apply(data, mask=None):
    for (path, index), value in data.items():
        if path.startswith('pose.bones["'):
            name = path.split('"')[1]
            if mask is None or name in mask:
                prop = path.rsplit('.', 1)[1]
                if hasattr(pb[name], prop):
                    getattr(pb[name], prop)[index] = value


def compose(base, age, title, frame):
    for b in pb:
        b.matrix_basis.identity()
    count = int(bpy.data.actions[H.PREFIX + base].frame_range[1]) - 1
    apply(values(base, 1 if base == 'Idle combat' else 1 + age % count))
    mask = right if title in ('Spear held', 'Spear crown held', 'Spear catch', 'Conduit Spear') else upper if base == 'Idle combat' else arms
    apply(values(title, frame), mask)
    bpy.context.view_layer.update()


def line(name, points, width, material, jitter=0, seed=0):
    rng = random.Random(seed)
    coords = []
    for a, b in zip(points, points[1:]):
        for k in range(5):
            u = k / 5
            noise = Vector(tuple(rng.uniform(-jitter, jitter) for _ in range(3)))
            coords.append(a.lerp(b, u) + noise * math.sin(u * math.pi))
    coords.append(points[-1])
    curve = bpy.data.curves.new(name, 'CURVE')
    curve.dimensions = '3D'
    curve.bevel_depth = width
    curve.bevel_resolution = 1
    sp = curve.splines.new('POLY')
    sp.points.add(len(coords) - 1)
    for p, co in zip(sp.points, coords):
        p.co = (*co, 1)
    obj = bpy.data.objects.new(name, curve)
    scene.collection.objects.link(obj)
    obj.data.materials.append(material)
    fx.append(obj)


def clear_fx():
    for ob in fx:
        data = ob.data
        bpy.data.objects.remove(ob, do_unlink=True)
        bpy.data.curves.remove(data)
    fx.clear()


def circuit(frame, held, strength):
    def w(p):
        return rig.matrix_world @ p
    def anatomical(b, direction, axis):
        v = b.matrix.to_3x3() @ rig.data.bones[b.name].matrix_local.to_3x3().inverted() @ direction
        return (v - axis.normalized() * v.dot(axis.normalized())).normalized()
    ua, fa, hand = (pb['R ' + n] for n in ('upperarm', 'forearm', 'hand'))
    back = anatomical(ua, Vector((0, 1, 0)), fa.head - ua.head)
    under = anatomical(fa, Vector((0, 0, -1)), hand.head - fa.head)
    shoulder = ua.head + back * 0.095
    dock = min((pb[f'halo {i}'].head.copy() for i in range(1, 5)), key=lambda p: (p - shoulder).length_squared)
    contact = socket.matrix_world @ Vector((0.025, -0.02, 0.04)) if held else w(pb['R muzzle'].head)
    route = [w(dock), w(shoulder), w(ua.head.lerp(fa.head, 0.55) + back * 0.085),
             w(fa.head + back * 0.09), w(fa.head + (back + under).normalized() * 0.11),
             w(fa.head.lerp(hand.head, 0.6) + under * 0.08), w(hand.head + under * 0.055), contact]
    line('Body current', route, 0.012 * strength, cyan, 0.025, frame)
    line('Body current core', route, 0.005 * strength, white, 0.012, frame)
    if held:
        tip = socket.matrix_world @ Vector((0, 0, 1.30))
        spine = [contact, socket.matrix_world @ Vector((0, 0, 0.26)), tip]
        line('Hand into spear current', spine, 0.012 * strength, cyan, 0.015, frame + 30)
        for sign in (-1, 1):
            line('Copper gap arcs', [socket.matrix_world @ Vector((sign * 0.13, 0, 0.90)), tip], 0.004, cyan, 0.02, frame + sign)


def cone(frame):
    tip = socket.matrix_world @ Vector((0, 0, 1.30))
    forward = (socket.matrix_world.to_3x3() @ Vector((0, 0, 1))).normalized()
    side = forward.cross(Vector((0, 0, 1))).normalized()
    for k in range(7):
        angle = math.radians(-28 + 56 * k / 6 + math.sin(frame * 0.4 + k) * 1.5)
        direction = (forward * math.cos(angle) + side * math.sin(angle) + Vector((0, 0, 0.04 * math.sin(k * 2.1)))).normalized()
        end = tip + direction * (7.2 + 0.5 * math.sin(frame * 0.7 + k))
        points = [tip, tip.lerp(end, 0.25), tip.lerp(end, 0.62), end]
        line('Fan lightning', points, 0.012, cyan, 0.08, frame * 17 + k)
        line('Fan lightning core', points, 0.005, white, 0.06, frame * 17 + k)
        fork = tip.lerp(end, 0.55)
        line('Fan branch', [fork, fork + direction * 0.8 + side * (0.15 if k % 2 else -0.15), end + side * 0.30], 0.004, cyan, 0.09, frame + k)


def camera(name, pos, target, scale):
    ob = bpy.data.objects.new(name, bpy.data.cameras.new(name))
    scene.collection.objects.link(ob)
    ob.location = pos
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()
    ob.data.type = 'ORTHO'
    ob.data.ortho_scale = scale
    return ob


rear = camera('Spear review rear', (-2.3, 3.7, 2.0), (0, -0.20, 1.3), 3.2)
side_view = camera('Spear review side', (3.8, -1.2, 2.1), (0, -0.35, 1.3), 3.7)
wide = camera('Fan wide', (6, 3, 6), (0, -4, 1.1), 11.5)
sequence = [('Spear held', range(1, 17)), ('Spear fan start', range(1, 10)),
            ('Spear fan loop', range(1, 26)), ('Spear fan end', range(1, 14)),
            ('Conduit Spear', range(1, 21)), ('Arc Bolt left', range(1, 21)),
            ('Spear catch', range(1, 21)), ('Spear held', range(1, 17))]
shots = [(title, frame) for title, frames in sequence for frame in frames]
if mode == 'keys':
    shots = [('Spear held', 1), ('Spear fan start', 5), ('Spear fan loop', 7),
             ('Spear fan end', 7), ('Conduit Spear', 4), ('Conduit Spear', 7),
             ('Spear catch', 8), ('Spear catch', 14), ('Spear crown held', 1)]
for locomotion in ('Idle combat', 'Run forward'):
    folder = OUT / ('standing' if locomotion == 'Idle combat' else 'running')
    folder.mkdir()
    spear_out = False
    for age, (title, frame) in enumerate(shots):
        if mode != 'keys' and age % 2:
            continue
        clear_fx()
        compose(locomotion, age, title, frame)
        if title == 'Conduit Spear' and frame >= 7:
            spear_out = True
        if title == 'Spear catch':
            spear_out = frame < 14
        if title in ('Spear held', 'Spear crown held', 'Spear fan start', 'Spear fan loop', 'Spear fan end'):
            spear_out = False
        for ob in weapon:
            ob.hide_render = spear_out
        flying = title == 'Conduit Spear' and frame >= 7 or title == 'Spear catch' and frame < 14
        for ob in flight_parts:
            ob.hide_render = not flying
        if flying:
            hand = socket.matrix_world.translation.copy()
            release_axis = Vector((0, -1, 0.12)).normalized()
            if title == 'Conduit Spear':
                position = hand + release_axis * max(0, frame - 7) * 0.5
                rotation = release_axis.to_track_quat('Z', 'Y')
            else:
                t = min(1, (frame - 1) / 13)
                eased = t * t * (3 - 2 * t)
                position = hand + Vector((0, -8, 0.5)) * (1 - eased)
                rotation = release_axis.to_track_quat('Z', 'Y').slerp(socket.matrix_world.to_quaternion(), max(0, (t - 0.45) / 0.55))
            flight.matrix_world = Matrix.Translation(position) @ rotation.to_matrix().to_4x4()
        active = title.startswith('Spear fan') or title == 'Conduit Spear' and frame <= 10 or title == 'Spear catch'
        if active:
            circuit(age, not spear_out, 1 if title == 'Spear fan loop' else 0.75)
        if title == 'Spear fan loop':
            cone(age)
        for label, cam in [('rear', rear), ('side', wide if title == 'Spear fan loop' else side_view)]:
            H.render_still(scene, cam.name, folder / f'{label}-{age:03}.png', (420, 480))
        print('SPEAR_REVIEW', locomotion, title, frame, flush=True)
(OUT / 'sequence.json').write_text(json.dumps(shots), encoding='utf-8')
