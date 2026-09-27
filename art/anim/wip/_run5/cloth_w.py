"""Front cloth vertex weights after open_start vs the body flap weights under them.
blender -b --factory-startup --python cloth_w.py"""
import sys
from pathlib import Path

import bpy
from mathutils.bvhtree import BVHTree

sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
import hs_anim as H

p = H.open_start()
print('FIX', p.rig.get('hs_body_fix'))
body = bpy.data.objects[H.BODY]
cloth = bpy.data.objects['TABARD | V11 front cloth']
bn = {g.index: g.name for g in body.vertex_groups}
cn = {g.index: g.name for g in cloth.vertex_groups}
bv = [body.matrix_world @ v.co for v in body.data.vertices]
tree = BVHTree.FromPolygons(bv, [tuple(q.vertices) for q in body.data.polygons])
for i in (41, 50, 60, 62, 85, 95, 104):
    v = cloth.data.vertices[i]
    c = cloth.matrix_world @ v.co
    loc, nrm, idx, d = tree.find_nearest(c)
    poly = body.data.polygons[idx]
    bw = {}
    for j in poly.vertices:
        for g in body.data.vertices[j].groups:
            bw[bn[g.group]] = bw.get(bn[g.group], 0)+g.weight/len(poly.vertices)
    print('V', i, [round(x, 3) for x in c], 'cloth', {cn[g.group]: round(g.weight, 2) for g in v.groups},
          'body@', round(d*1000, 1), 'mm', {k: round(x, 2) for k, x in bw.items() if x > 0.01},
          'nrm', [round(x, 2) for x in nrm])
