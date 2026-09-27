"""Body verts near given rest points: count, bbox and weight mix, plus body-wide tabard weight summary.
blender -b --factory-startup --python body_near.py -- <blend> "x,y,z;x,y,z" [radius]"""
import bpy
import sys
from mathutils import Vector

a = sys.argv[sys.argv.index('--')+1:]
bpy.ops.wm.open_mainfile(filepath=a[0])
sys.path.insert(0, bpy.path.abspath('//../../tools/blender/anim'))
rig = bpy.data.objects['Hollow Saint | v8 rig']
rig.data.pose_position = 'REST'
bpy.context.view_layer.update()
body = bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
print('BODY', body.name, len(body.data.vertices))
names = {g.index: g.name for g in body.vertex_groups}
rad = float(a[2]) if len(a) > 2 else 0.05
co = [body.matrix_world @ v.co for v in body.data.vertices]
for spec in a[1].split(';'):
    q = Vector([float(x) for x in spec.split(',')])
    ids = [i for i, c in enumerate(co) if (c-q).length < rad]
    w = {}
    for i in ids:
        for g in body.data.vertices[i].groups:
            w[names[g.group]] = w.get(names[g.group], 0)+g.weight
    tot = sum(w.values()) or 1
    print('NEAR', spec, len(ids), {k: round(v/tot, 2) for k, v in sorted(w.items(), key=lambda x: -x[1])[:6]})
tab = [i for i, v in enumerate(body.data.vertices)
       if any(names[g.group].startswith('tabard') and g.weight > 0.3 for g in v.groups)]
if tab:
    zs = [co[i].z for i in tab]
    ys = [co[i].y for i in tab]
    print('TABARD-WEIGHTED BODY VERTS', len(tab), 'z', round(min(zs), 3), round(max(zs), 3), 'y', round(min(ys), 3), round(max(ys), 3))
    import collections
    cl = collections.defaultdict(list)
    for i in tab:
        v = body.data.vertices[i]
        top = max(v.groups, key=lambda g: g.weight)
        cl[names[top.group]].append(co[i])
    for k, pts in sorted(cl.items()):
        lo = [round(min(p[j] for p in pts), 3) for j in range(3)]
        hi = [round(max(p[j] for p in pts), 3) for j in range(3)]
        print('  TAB', k, len(pts), lo, hi)
    polys = [p for p in body.data.polygons if all(i in set(tab) for i in p.vertices)]
    print('  polys fully tabard-weighted', len(polys))
    # connected islands of the body mesh
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(body.data)
    seen, isl = set(), []
    for v in bm.verts:
        if v.index in seen:
            continue
        stack, comp = [v], []
        seen.add(v.index)
        while stack:
            x = stack.pop()
            comp.append(x.index)
            for e in x.link_edges:
                o = e.other_vert(x)
                if o.index not in seen:
                    seen.add(o.index)
                    stack.append(o)
        isl.append(comp)
    print('  ISLANDS', len(isl), sorted((len(c), sum(1 for i in c if i in set(tab))) for c in isl)[-8:])
