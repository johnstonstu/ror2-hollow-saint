"""Rest-pose L/R mirror symmetry of hand meshes and hand bones, before and after handfix (read-only)."""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
import bpy
from mathutils import Vector
from mathutils.kdtree import KDTree


def measure(tag, rig):
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    pts = {}
    for s in 'LR':
        vs = []
        for o in bpy.data.objects:
            if o.type == 'MESH' and o.name.startswith(f'{s} HAND |'):
                ev = o.evaluated_get(dg)
                me = ev.to_mesh()
                vs += [o.matrix_world @ v.co for v in me.vertices]
                ev.to_mesh_clear()
        pts[s] = vs
    kd = KDTree(len(pts['R']))
    for i, v in enumerate(pts['R']):
        kd.insert(v, i)
    kd.balance()
    d = sorted(kd.find(Vector((2*H.MID_X-v.x, v.y, v.z)))[2] for v in pts['L'])
    bones = {}
    for b in rig.pose.bones:
        if b.name.startswith('L ') and any(k in b.name for k in ('hand', 'index', 'middle', 'ring', 'little', 'thumb')):
            o = rig.pose.bones.get('R '+b.name[2:])
            if o:
                hl = rig.matrix_world @ b.head
                hr = rig.matrix_world @ o.head
                bones[b.name[2:]] = round((Vector((2*H.MID_X-hl.x, hl.y, hl.z))-hr).length*1000, 2)
    print('SYM', tag, 'nL', len(pts['L']), 'nR', len(pts['R']), 'median_mm', round(d[len(d)//2]*1000, 2),
          'p95_mm', round(d[int(len(d)*.95)]*1000, 2), 'max_mm', round(d[-1]*1000, 2), flush=True)
    print('SYMBONES', tag, sorted(bones.items(), key=lambda kv: -kv[1])[:8], flush=True)


import hs_anim as H
import handfix
bpy.ops.wm.open_mainfile(filepath=str(H.START))
rig = bpy.data.objects[H.RIG]
rig.data.pose_position = 'REST'
measure('v18', rig)
handfix.apply(rig)
rig.data.pose_position = 'REST'
measure('fixed', rig)

