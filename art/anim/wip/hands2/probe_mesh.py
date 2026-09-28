"""Scratch probe (hands2): hand mesh parts and their principal axes in bone space vs the bone's Y axis.

blender --background --factory-startup --python art/anim/wip/hands2/probe_mesh.py -- <blend> <out.json>
"""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Matrix, Vector

args = sys.argv[sys.argv.index('--')+1:]
bpy.ops.wm.open_mainfile(filepath=args[0])
rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE' and 'rig' in o.name)
if rig.animation_data:
    rig.animation_data.action = None
for p in rig.pose.bones:
    p.matrix_basis.identity()
bpy.context.view_layer.update()


def pca(pts):
    c = sum(pts, Vector())/len(pts)
    m = Matrix(((0,)*3,)*3)
    for p in pts:
        d = p-c
        for i in range(3):
            for j in range(3):
                m[i][j] += d[i]*d[j]
    v = Vector((1, 0.3, 0.2)).normalized()
    for _ in range(60):
        v = (m @ v).normalized()
    ext = [d.dot(v) for d in (p-c for p in pts)]
    return c, v, max(ext)-min(ext)


out = {}
for o in bpy.data.objects:
    if o.type != 'MESH' or not o.name.startswith(('L HAND', 'R HAND')):
        continue
    pb = rig.pose.bones.get(o.parent_bone) if o.parent_type == 'BONE' else None
    pts = [o.matrix_world @ v.co for v in o.data.vertices]
    c, ax, length = pca(pts)
    e = {'parent_bone': o.parent_bone, 'parent_type': o.parent_type, 'verts': len(pts), 'len_m': round(length, 4)}
    if pb is not None:
        y = (rig.matrix_world.to_3x3() @ pb.matrix.to_3x3()).col[1].normalized()
        if ax.dot(y) < 0:
            ax = -ax
        e['pca_vs_boneY_deg'] = round(math.degrees(ax.angle(y)), 1)
        e['bone_len'] = round(pb.length, 4)
        head = rig.matrix_world @ pb.head
        e['centre_from_head_along_Y'] = round((c-head).dot(y), 4)
    out[o.name] = e
Path(args[1]).write_text(json.dumps(out, indent=1), encoding='utf-8')
print('MESH PROBE DONE', len(out), flush=True)
