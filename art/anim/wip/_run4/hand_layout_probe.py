"""Read-only probe of the hand layout on fresh v18 (+rigfix/vfx), rest pose. Prints JSON."""
import bpy, sys, json
from pathlib import Path
sys.path.insert(0, str(Path(bpy.path.abspath('//')).parent) if False else r'C:\Users\stuwj\Documents\Coding\ror2-lightning\tools\blender\anim')
import hs_anim as H
from mathutils import Vector

p = H.open_start()
p.reset(); p.update()
rig = p.rig
out = {'objects': {}, 'bones': {}}
for o in bpy.data.objects:
    if 'HAND |' in o.name:
        c = o.matrix_world @ (sum((Vector(v) for v in o.bound_box), Vector())/8)
        out['objects'][o.name] = {'parent_type': o.parent_type, 'bone': o.parent_bone, 'centre': [round(x, 4) for x in c],
                                  'verts': len(o.data.vertices) if o.type == 'MESH' else 0}
for s in 'LR':
    for n in ['upperarm', 'forearm', 'hand', 'muzzle'] + [f'{d}.{i}' for d in ('thumb', 'index', 'middle', 'ring', 'little') for i in (1, 2, 3)]:
        b = rig.data.bones.get(f'{s} {n}')
        if b is None:
            continue
        m = b.matrix_local
        out['bones'][b.name] = {'head': [round(x, 4) for x in b.head_local], 'tail': [round(x, 4) for x in b.tail_local],
                                'x': [round(x, 3) for x in m.col[0][:3]], 'y': [round(x, 3) for x in m.col[1][:3]],
                                'z': [round(x, 3) for x in m.col[2][:3]], 'roll_parent': b.parent.name if b.parent else ''}
# Curl direction: rotate index.1 by +30 about X and see where the tip goes.
for s in 'LR':
    for d in ('index', 'little', 'thumb'):
        n = f'{s} {d}.1'
        p.reset(); p.update()
        t0 = p.pb[f'{s} {d}.3'].tail.copy()
        from mathutils import Euler
        import math
        for i in (1, 2, 3):
            p.pb[f'{s} {d}.{i}'].rotation_quaternion = Euler((math.radians(30), 0, 0)).to_quaternion()
        p.update()
        t1 = p.pb[f'{s} {d}.3'].tail.copy()
        out[f'curl_{s}_{d}'] = [round(x, 4) for x in (t1 - t0)]
p.reset(); p.update()
print('PROBE', json.dumps(out))
Path(r'C:\Users\stuwj\Documents\Coding\ror2-lightning\art\anim\wip\_run4\hand_layout.json').write_text(json.dumps(out, indent=1))
