"""Illustrative current flow on the actual fitted weapon, not game footage."""
import bpy
import math
import random
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/blender/anim'))
import hs_anim as H

H.require_background()
STUDY = ROOT / 'artifacts/spear-hand-fit05/study'
OUT = STUDY / 'current-frames'
assert not OUT.exists(), 'Use a new study for another current render'
OUT.mkdir()
bpy.ops.wm.open_mainfile(filepath=str(STUDY / 'hand-fit-study.blend'))
scene = bpy.context.scene
H.eevee(scene, 24)
cam = bpy.data.objects['rear-connected']
cam.location = (-2.2, 3.7, 2)
cam.rotation_euler = (Vector((0, -0.15, 1.35)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
cam.data.ortho_scale = 2.95


def points(obj):
    return [obj.matrix_world @ Vector(p.co[:3]) for p in obj.data.splines[0].points]


route = points(bpy.data.objects['Ring arm hand grip connected core'])
route += points(bpy.data.objects['Contact feeds spear spine'])[1:]
socket = bpy.data.objects['Spear grip socket — hand locked']
route += [socket.matrix_world @ Vector((0, 0, z)) for z in (0.40, 0.60, 0.90, 1.23)]
lengths = [(b - a).length for a, b in zip(route, route[1:])]
total = sum(lengths)


def span(lo, hi):
    travelled = 0.0
    result = []
    for a, b, length in zip(route, route[1:], lengths):
        begin = max(0, min(1, (lo * total - travelled) / max(length, 1e-8)))
        end = max(0, min(1, (hi * total - travelled) / max(length, 1e-8)))
        if end > begin:
            if not result:
                result.append(a.lerp(b, begin))
            result.append(a.lerp(b, end))
        travelled += length
    return result


def draw(name, coords, radius, mat):
    if len(coords) < 2:
        return None
    curve = bpy.data.curves.new(name, 'CURVE')
    curve.dimensions = '3D'
    curve.bevel_depth = radius
    curve.bevel_resolution = 1
    sp = curve.splines.new('POLY')
    sp.points.add(len(coords) - 1)
    for p, co in zip(sp.points, coords):
        p.co = (*co, 1)
    ob = bpy.data.objects.new(name, curve)
    scene.collection.objects.link(ob)
    ob.data.materials.append(mat)
    return ob


effects = [obj for obj in scene.objects if obj.type == 'CURVE' and not obj.name.startswith('Contained lightning')]
cyan = bpy.data.materials['Study connected current']
white = bpy.data.materials['Study electric core']
pulse_objects = []
for frame in range(24):
    for ob in pulse_objects:
        data = ob.data
        bpy.data.objects.remove(ob, do_unlink=True)
        bpy.data.curves.remove(data)
    pulse_objects.clear()
    active = frame < 18
    for ob in effects:
        ob.hide_render = not active
    if active:
        hi = min(1, 0.08 + frame / 15)
        pulse = span(max(0, hi - 0.14), hi)
        for name, width, mat in [('Travelling sheath', 0.022, cyan), ('Travelling filament', 0.008, white)]:
            ob = draw(name, pulse, width, mat)
            if ob:
                pulse_objects.append(ob)
        if pulse:
            rng = random.Random(frame + 753)
            start = pulse[-1]
            for i in range(2):
                delta = Vector(tuple(rng.uniform(-0.09, 0.09) for _ in range(3)))
                ob = draw('Pulse crackle', [start, start + delta * 0.45, start + delta], 0.0025, white)
                pulse_objects.append(ob)
    H.render_still(scene, cam.name, OUT / f'current-{frame:03}.png', (560, 640))
    print('HAND_CURRENT_FRAME', frame, flush=True)
