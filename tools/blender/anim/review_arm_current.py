"""Render the throw study with illustrative, bone-following lightning.

This is a Blender concept visualization, not a capture of the game VFX.
"""
import bpy
import json
import math
import random
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/blender/anim'))
import hs_anim as H

OUT = ROOT / 'artifacts/arm-current-route02/renders'
H.require_background()
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'art/anim/hollow-saint-anim-v36.blend'))
scene = bpy.context.scene
rig = bpy.data.objects[H.RIG]
pb = rig.pose.bones
for track in rig.animation_data.nla_tracks:
    track.mute = True
rig.animation_data.action = None
H.eevee(scene, 8)

FINGERS = ('index', 'middle', 'ring', 'little', 'thumb')
ARM = {f'{s} {b}' for s in 'LR' for b in (
    'shoulder', 'scapula', 'pauldron', 'upperarm', 'forearm',
    'forearm twist', 'hand', 'muzzle')}
ARM |= {b.name for b in rig.data.bones if
        any(f' {finger}.' in b.name for finger in FINGERS)}
UPPER = ARM | {'spine', 'chest', 'neck', 'head', 'head socket', 'core socket'}


def values(title, frame):
    action = bpy.data.actions[H.PREFIX + title]
    return {(fc.data_path, fc.array_index): fc.evaluate(frame)
            for layer in action.layers for strip in layer.strips
            for bag in strip.channelbags for fc in bag.fcurves}


def apply(values, mask=None):
    for (path, index), value in values.items():
        if not path.startswith('pose.bones["'):
            continue
        name = path.split('"')[1]
        if mask is not None and name not in mask:
            continue
        prop = path.rsplit('.', 1)[1]
        if name in pb and hasattr(pb[name], prop):
            getattr(pb[name], prop)[index] = value


def compose(base, frame, side):
    for bone in pb:
        bone.matrix_basis.identity()
    count = int(bpy.data.actions[H.PREFIX + base].frame_range[1]) - 1
    apply(values(base, 1 if base == 'Idle combat' else 1 + (frame - 1) % count))
    apply(values('Arc Bolt ' + side, frame), UPPER if base == 'Idle combat' else ARM)
    bpy.context.view_layer.update()


def material(name, color, strength):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    emission = nodes.new('ShaderNodeEmission')
    emission.inputs['Color'].default_value = (*color, 1)
    emission.inputs['Strength'].default_value = strength
    output = nodes.new('ShaderNodeOutputMaterial')
    mat.node_tree.links.new(emission.outputs[0], output.inputs['Surface'])
    return mat


ARC = material('Study current cyan', (0.02, 0.60, 1.0), 5)
CORE = material('Study current white core', (0.65, 0.96, 1.0), 8)
fx = []


def clear_fx():
    for obj in fx:
        data = obj.data
        bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.curves.remove(data)
    fx.clear()


def line(name, points, width, mat, jag=0.0, seed=0):
    if width <= 0.00001 or len(points) < 2:
        return
    rng = random.Random(seed)
    coords = []
    for a, b in zip(points, points[1:]):
        for step in range(5):
            u = step / 5
            noise = Vector(tuple(rng.uniform(-jag, jag) for _ in range(3)))
            coords.append(a.lerp(b, u) + noise * math.sin(math.pi * u))
    coords.append(points[-1])
    curve = bpy.data.curves.new(name, 'CURVE')
    curve.dimensions = '3D'
    curve.bevel_depth = width
    curve.bevel_resolution = 1
    spline = curve.splines.new('POLY')
    spline.points.add(len(coords) - 1)
    for point, coord in zip(spline.points, coords):
        point.co = (*coord, 1)
    obj = bpy.data.objects.new(name, curve)
    scene.collection.objects.link(obj)
    obj.data.materials.append(mat)
    fx.append(obj)


def world(coord):
    return rig.matrix_world @ coord






def camera(name, position, target, scale):
    cam = bpy.data.objects.new(name, bpy.data.cameras.new(name))
    scene.collection.objects.link(cam)
    cam.location = position
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = scale
    return cam


VIEWS = {
    'side': camera('Study side', (3.6, -0.30, 1.8), (0, -0.30, 1.3), 2.9),
    'rear': camera('Direct rear', (0, 3.7, 1.75), (0, -0.15, 1.3), 2.5),
    'front': camera('Study front three quarter', (-2.5, -3.5, 2.0), (0, -0.30, 1.25), 2.9),
}

def radial(preferred, axis, fallback):
    axis = axis.normalized()
    normal = preferred - axis * preferred.dot(axis)
    if normal.length_squared < 0.0001:
        normal = fallback - axis * fallback.dot(axis)
    if normal.length_squared < 0.0001:
        normal = axis.cross(Vector((0, 0, 1)) if abs(axis.z) < 0.9 else Vector((1, 0, 0)))
    return normal.normalized() if normal.length_squared > 0.0001 else Vector((0, 1, 0))

def path_span(points, begin, end):
    lengths = [(b-a).length for a, b in zip(points, points[1:])]
    total = sum(lengths)
    travelled = 0.0
    result = []
    for a, b, length in zip(points, points[1:], lengths):
        lo = max(0, min(1, (begin * total - travelled) / max(length, 1e-6)))
        hi = max(0, min(1, (end * total - travelled) / max(length, 1e-6)))
        if hi > lo:
            if not result:
                result.append(a.lerp(b, lo))
            result.append(a.lerp(b, hi))
        travelled += length
    return result


def crackle(points, frame, scale):
    rng = random.Random(1300 + frame)
    for i, point in enumerate(points):
        for fork in range(2):
            delta = Vector(tuple(rng.uniform(-1, 1) for _ in range(3)))
            delta.normalize()
            delta *= rng.uniform(0.06, 0.13) * scale
            bend = point + delta * 0.55 + Vector((0, 0.03, 0.025))
            line('Shoulder and limb crackle', [point, bend, point + delta],
                 0.0035 * scale, CORE, 0.009, frame * 33 + i * 5 + fork)


def current(frame):
    clear_fx()
    if not 2 <= frame <= 9:
        return
    weight = {2: 0.35, 3: 0.62, 4: 0.85, 5: 1.0,
              6: 0.80, 7: 0.52, 8: 0.25, 9: 0.06}[frame]
    upper, forearm, hand = (pb['R ' + name] for name in ('upperarm', 'forearm', 'hand'))
    unit = max(0.3, min(4.0, (pb['chest'].head - pb['halo root'].head).length / 0.61))
    def anatomical(bone, direction):
        return bone.matrix.to_3x3() @ rig.data.bones[bone.name].matrix_local.to_3x3().inverted() @ direction
    rear = radial(anatomical(upper, Vector((0, 1, 0))), forearm.head-upper.head, Vector((0, 0, -1)))
    under = radial(anatomical(forearm, Vector((0, 0, -1))), hand.head-forearm.head, rear)
    palm = radial(anatomical(hand, Vector((0, 0, -1))), pb['R muzzle'].head-hand.head, under)
    wrap = rear + under
    if wrap.length_squared < 0.001:
        wrap = (hand.head-forearm.head).cross(rear)
    shoulder = upper.head + rear * 0.095 * unit
    nodes = [shoulder,
             upper.head.lerp(forearm.head, 0.55) + rear * 0.085 * unit,
             forearm.head + rear * 0.09 * unit,
             forearm.head + wrap.normalized() * 0.11 * unit,
             forearm.head.lerp(hand.head, 0.6) + under * 0.08 * unit,
             hand.head + under * 0.055 * unit,
             hand.head.lerp(pb['R muzzle'].head, 0.45) + palm * 0.025 * unit,
             pb['R muzzle'].head]
    heads = [pb[f'halo {i}'].head.copy() for i in range(1, 5)]
    center = sum(heads, Vector()) / 4
    dock = min(heads, key=lambda point: (point - shoulder).length_squared)
    path = [world(p) for p in [dock] + nodes]
    core = pb['core socket'].head
    chest_front = core + Vector((-0.13, -0.04, 0.045))
    shoulder_front = upper.head + Vector((-0.04, -0.11, 0.12))
    feed = [world(p) for p in (core, chest_front, shoulder_front, shoulder, dock)]
    line('Connected core and ring', feed, 0.012 * weight, ARC, 0.027, frame + 500)
    line('Connected core filament', feed, 0.0045 * weight, CORE, 0.015, frame + 500)
    # A short hot sweep on the copper at the source dock.
    radius = (dock - center).length
    angle = math.atan2(dock.z - center.z, dock.x - center.x)
    ring_points = [world(center + Vector((math.cos(angle + u) * radius, 0.018,
                                         math.sin(angle + u) * radius)))
                   for u in (-0.40, -0.20, 0.0, 0.20, 0.40)]
    line('Ring pickup flare', ring_points, 0.014 * weight, ARC, 0.018, frame + 700)
    line('Ring pickup core', ring_points, 0.005 * weight, CORE, 0.011, frame + 700)
    reach = {2: 0.20, 3: 0.52, 4: 0.82}.get(frame, 1.0)
    flow = path_span(path, 0, reach)
    line('Connected arm glow', flow, 0.018 * weight, ARC, 0.030, frame)
    line('Connected arm core', flow, 0.006 * weight, CORE, 0.017, frame)
    pulse = path_span(path, max(0, reach - 0.29), reach)
    line('Travelling pulse sheath', pulse, 0.030 * weight, ARC, 0.020, frame + 30)
    line('Travelling pulse core', pulse, 0.010 * weight, CORE, 0.011, frame + 30)
    crackle(path[1:4] if frame >= 4 else path[:2], frame, weight)
    if frame in (5, 6):
        end = path[-1] + Vector((0, -0.67, 0.04))
        line('Release sheath', [path[-1], end], 0.027 * weight, ARC, 0.045, frame + 200)
        line('Release core', [path[-1], end], 0.012 * weight, CORE, 0.025, frame + 200)
        crackle([path[-1]], frame + 20, weight * 1.15)


def bolt(frame, origin):
    if not 5 <= frame <= 11:
        return
    weight = 1 if frame <= 8 else 0.6 if frame == 9 else 0.25
    tip = origin + Vector((0, -(frame - 5) * 0.23 - 0.30, 0.035))
    path = [tip + Vector((0, 0.24, 0)), tip]
    line('Detached bolt sheath', path, 0.025 * weight, ARC, 0.04, frame + 400)
    line('Detached bolt core', path, 0.010 * weight, CORE, 0.025, frame + 400)


mode = sys.argv[sys.argv.index('--') + 1] if '--' in sys.argv else 'keys'
frames = (1, 3, 5, 8, 12, 20) if mode == 'keys' else range(1, 21)
for name, base in (('standing', 'Idle combat'), ('running', 'Run forward')):
    launch = None
    for frame in frames:
        compose(base, frame, 'right')
        current(frame)
        if frame == 5:
            launch = world(pb['R muzzle'].head)
        bolt(frame, launch if launch is not None else world(pb['R muzzle'].head))
        for view, cam in VIEWS.items():
            folder = OUT / name
            folder.mkdir(parents=True, exist_ok=True)
            H.render_still(scene, cam.name, folder / f'{view}-{frame:03}.png', (320, 400))
        print('SURFACE_ROUTE_RENDER', name, frame, flush=True)
print('SURFACE_ROUTE_DONE', flush=True)
