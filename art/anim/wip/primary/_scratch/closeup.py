"""Scratch: extra review cameras for primary.py (top, sides, over-the-shoulder, hand close-up).
blender --background --factory-startup --python-exit-code 1 --python closeup.py -- "Arc Bolt right" 3,5,6 top,ots,rside,hand
"""
import bpy
import sys
from pathlib import Path
from mathutils import Vector

TOOLS = Path(r"C:\Users\stuwj\Documents\Coding\ror2-lightning\tools\blender\anim")
sys.path.insert(0, str(TOOLS))
sys.path.insert(0, str(TOOLS/'clips'))
import hs_anim as H
import primary

args = sys.argv[sys.argv.index('--')+1:]
title, frames, views = args[0], [int(v) for v in args[1].split(',')], args[2].split(',')
p = H.open_start()
scene = bpy.context.scene
built = primary.build(p)
act = next(a for a, i in built if i['title'] == title)
p.rig.animation_data.action = act
H.eevee(scene, 16)
side = 'L' if 'left' in title else 'R'
out = H.ROOT/'art/anim/wip/primary/_closeup'/title.lower().replace(' ', '-')
out.mkdir(parents=True, exist_ok=True)


def cam(name, loc, target, ortho=None, lens=50):
    c = bpy.data.cameras.new(name)
    if ortho:
        c.type = 'ORTHO'
        c.ortho_scale = ortho
    else:
        c.lens = lens
    o = bpy.data.objects.new(name, c)
    scene.collection.objects.link(o)
    o.location = loc
    o.rotation_euler = (Vector(target)-Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    return o.name


for f in frames:
    scene.frame_set(f)
    p.update()
    hand = p.world(f'{side} hand').translation
    specs = {
        'top': ((0.0, -0.35, 4.0), (0.0, -0.35, 0.0), 1.9, 50),
        'rside': ((-3.0, -0.3, 1.35), (0.0, -0.3, 1.35), 1.5, 50),
        'lside': ((3.0, -0.3, 1.35), (0.0, -0.3, 1.35), 1.5, 50),
        'ots': ((0.0, 5.0, 3.0), (0.0, -5.0, 1.0), None, 35),
        'hand': (tuple(hand+Vector((-0.55 if side == 'R' else 0.55, -0.25, 0.18))), tuple(hand), None, 70),
    }
    for v in views:
        loc, tgt, ortho, lens = specs[v]
        name = cam(f'CU {v} {f}', loc, tgt, ortho, lens)
        H.render_still(scene, name, out/f'{v}-f{f:02}.png', (420, 420) if v != 'ots' else (640, 400))
print('CLOSEUP DONE', out, flush=True)
