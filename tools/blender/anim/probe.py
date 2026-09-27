"""Print world positions of chosen bones across a clip module's frames (debugging aid).

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/probe.py -- run "L hand,L toe" 1,5,9
A name starting with ^ prints the bone's direction (head -> tail) instead; @<object> prints an object's
-Z axis in rig space, like the bone directions (e.g. "@VFX | L heel jet outer").
"""
import bpy
import importlib
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
sys.path.insert(0, str(Path(__file__).parent/'clips'))
import hs_anim as H

args = sys.argv[sys.argv.index('--')+1:]
p = H.open_start()
print('PROBE pole angles', p.pole_angles, 'rest pole shift', p.rest_pole_shift, flush=True)
print('PROBE rest', {b.name: tuple(round(c, 3) for c in b.vector.normalized()) for b in p.rig.data.bones if b.name in ('L toe', 'L foot', 'R toe')}, flush=True)
built = importlib.import_module(args[0]).build(p)
names = args[1].split(',')
frames = [int(v) for v in args[2].split(',')]
def value(n):
    if n.startswith('^'):
        b = p.pb[n[1:]]
        return (b.tail-b.head).normalized()
    if n.startswith('@'):
        o = bpy.data.objects[n[1:]].evaluated_get(bpy.context.evaluated_depsgraph_get())
        return -(p.rig.matrix_world.inverted().to_3x3() @ o.matrix_world.to_3x3()).normalized().col[2]
    return p.pb[n].matrix.translation


for act, info in built:
    p.rig.animation_data.action = act
    for f in frames:
        bpy.context.scene.frame_set(f)
        p.update()
        bpy.context.view_layer.update()
        print('PROBE', info['title'], f, ' | '.join(f'{n}: ' + ', '.join(f'{c:+.3f}' for c in value(n)) for n in names), flush=True)
