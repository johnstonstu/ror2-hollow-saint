"""Scratch probe (hands2): world joint positions and hand axes, plus which side of the palm mesh the knuckle
plates sit on (dorsal check).
blender --background --factory-startup --python art/anim/wip/hands2/probe_points.py -- <blend> "Idle" 1
"""
import bpy
import sys
from pathlib import Path
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
import handorient

args = sys.argv[sys.argv.index('--')+1:]
bpy.ops.wm.open_mainfile(filepath=args[0])
rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE' and 'rig' in o.name)
if args[1] == 'REST':
    rig.animation_data.action = None
    for p in rig.pose.bones:
        p.matrix_basis.identity()
else:
    rig.animation_data.action = bpy.data.actions['HS_anim | '+args[1]]
    bpy.context.scene.frame_set(int(args[2]))
bpy.context.view_layer.update()
mw = rig.matrix_world
pb = rig.pose.bones
f = lambda v: '(' + ', '.join(f'{x:+.3f}' for x in v) + ')'
for s in 'LR':
    d, n, r = handorient.hand_axes(rig, s)
    w = mw @ pb[f'{s} hand'].head
    print(f'{s} d{f(d)} n{f(n)} r{f(r)} wrist{f(w)}')
    for dg in ('index', 'little', 'thumb'):
        pts = [mw @ pb[f'{s} {dg}.{i}'].head for i in (1, 2, 3)]+[mw @ pb[f'{s} {dg}.3'].tail]
        print(f'  {dg:7s}', ' '.join(f((p-w)) for p in pts), ' along n:', ' '.join(f'{(p-w).dot(n):+.3f}' for p in pts))
    for part in ('tapered sculpted palm', 'index knuckle', 'middle knuckle'):
        o = bpy.data.objects.get(f'{s} HAND | {part}')
        if o:
            c = sum((o.matrix_world @ v.co for v in o.data.vertices), Vector())/len(o.data.vertices)
            print(f'  {part:22s} centre along n {(c-w).dot(n):+.4f}  along d {(c-w).dot(d):+.4f}')
