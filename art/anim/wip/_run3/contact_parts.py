"""Per hand-part inside test at one frame: normal test vs ray parity against the full body mesh.

blender --background --factory-startup --python art/anim/wip/_run3/contact_parts.py -- <module> <clip-slug> <frame>
"""
import bpy
import importlib
import sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

TOOLS = Path(__file__).resolve().parents[4]/'tools/blender/anim'
sys.path.insert(0, str(TOOLS))
sys.path.insert(0, str(TOOLS/'clips'))
import hs_anim as H
import clearance

args = sys.argv[sys.argv.index('--')+1:]
module, want, frame = args[0], args[1], int(args[2])
p = H.open_start()
slug = lambda t: ''.join(ch if ch.isalnum() else '-' for ch in t.lower()).strip('-')
built = importlib.import_module(module).build(p)
if want == 'rest':
    p.rig.animation_data.action = None
    p.reset()
else:
    p.rig.animation_data.action = next(a for a, i in built if slug(i['title']) == want)
    bpy.context.scene.frame_set(frame)
p.update()
body = bpy.data.objects[H.BODY]
dom = clearance.dominant_groups(body)
dg = bpy.context.evaluated_depsgraph_get()
ev = body.evaluated_get(dg)
me = ev.to_mesh()
bv = [body.matrix_world @ v.co for v in me.vertices]
keep = [tuple(q.vertices) for q in me.polygons
        if not any(dom[v] in ('L forearm', 'L hand', 'R forearm', 'R hand', 'L upperarm', 'R upperarm') for v in q.vertices)]
thigh = {s: [tuple(q.vertices) for q in me.polygons if all(dom[v] == f'{s} thigh' for v in q.vertices)] for s in H.SIDES}
ev.to_mesh_clear()
full = BVHTree.FromPolygons(bv, keep)
th = {s: BVHTree.FromPolygons(bv, thigh[s]) for s in H.SIDES}
DIRS = [Vector(d).normalized() for d in ((1, 0.2, 0.1), (-0.3, 1, 0.2), (0.1, -0.2, 1), (-1, -0.4, -0.3), (0.2, 0.3, -1))]


def parity(q):
    votes = 0
    for d in DIRS:
        n, o = 0, q.copy()
        for _ in range(40):
            hit = full.ray_cast(o, d)
            if hit[0] is None:
                break
            n += 1
            o = hit[0]+d*1e-5
        votes += n % 2
    return votes


for s in H.SIDES:
    for o in bpy.data.objects:
        if o.type != 'MESH' or not o.name.startswith(f'{s} HAND |') or o.parent_type != 'BONE':
            continue
        mw = o.matrix_world
        pts = [mw @ v.co for v in o.data.vertices]
        inside = [q for q in pts if parity(q) >= 4]
        nd = 0.0
        for q in pts:
            loc, nrm, idx, dist = th[s].find_nearest(q, 0.035)
            if loc is not None and (q-loc).dot(nrm) < 0:
                nd = max(nd, dist)
        tdist = min((th[s].find_nearest(q)[3] for q in pts), default=9)
        if inside or nd > 0.002:
            print(f'PART {s} {o.name[:40]:40} verts={len(pts)} parity_inside={len(inside)} normal_depth={nd*1000:.1f}mm'
                  f' thigh_dist_min={tdist*1000:.1f}mm', flush=True)
print('DONE', flush=True)
