"""Read-only: boundary / non-manifold edge counts and loose parts of the pad, halo arc and yoke meshes (evaluated)."""
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT/'tools/blender/anim'))
import bmesh
import bpy

import hs_anim as H

H.open_start()
dg = bpy.context.evaluated_depsgraph_get()
names = ['L SHOULDER | V17 pauldron upper', 'R SHOULDER | V17 pauldron upper', 'HALO | independent copper arc 2',
         'HALO | independent copper arc 3', 'HALO | V17 yoke bar']
for n in names:
    o = bpy.data.objects.get(n)
    if o is None:
        print('MANIFOLD', n, 'missing', flush=True)
        continue
    ev = o.evaluated_get(dg)
    me = ev.to_mesh()
    bm = bmesh.new()
    bm.from_mesh(me)
    boundary = sum(1 for e in bm.edges if e.is_boundary)
    nonman = sum(1 for e in bm.edges if not e.is_manifold)
    parts, seen = 0, set()
    for v in bm.verts:
        if v.index in seen:
            continue
        parts += 1
        stack = [v]
        while stack:
            x = stack.pop()
            if x.index in seen:
                continue
            seen.add(x.index)
            stack.extend(e.other_vert(x) for e in x.link_edges)
    mods = [m.type for m in o.modifiers]
    print('MANIFOLD', n, {'verts': len(bm.verts), 'faces': len(bm.faces), 'boundary_edges': boundary,
                          'nonmanifold_edges': nonman, 'parts': parts, 'modifiers': mods}, flush=True)
    bm.free()
    ev.to_mesh_clear()
