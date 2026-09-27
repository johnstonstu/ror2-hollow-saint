"""Body flap verts (dominant tabard bone) with neighbours that are not on the flap: where the flap joins the skin.
blender -b --factory-startup --python flap_links.py -- <blend> <front|back>"""
import sys

import bmesh
import bpy

a = sys.argv[sys.argv.index('--')+1:]
bpy.ops.wm.open_mainfile(filepath=a[0])
rig = bpy.data.objects['Hollow Saint | v8 rig']
rig.data.pose_position = 'REST'
body = bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
names = {g.index: g.name for g in body.vertex_groups}
mw = body.matrix_world


def dom(v):
    g = max(v.groups, key=lambda g: g.weight, default=None)
    return names[g.group] if g else ''


bm = bmesh.new()
bm.from_mesh(body.data)
bm.verts.ensure_lookup_table()
flap = {v.index for v in body.data.vertices if dom(v).startswith(f'tabard {a[1]}')}
links = {}
for i in sorted(flap):
    for e in bm.verts[i].link_edges:
        o = e.other_vert(bm.verts[i])
        if o.index not in flap:
            links.setdefault(i, []).append(o.index)
print('FLAP', a[1], len(flap), 'verts;', len(links), 'on the border')
rows = []
for i, nb in links.items():
    c = mw @ body.data.vertices[i].co
    for j in nb:
        d = mw @ body.data.vertices[j].co
        v = body.data.vertices[j]
        rows.append((round(c.z, 3), i, dom(body.data.vertices[i]), [round(x, 3) for x in c], j, dom(v),
                     [round(x, 3) for x in d], round((c-d).length, 3)))
rows.sort()
for r in rows:
    print('  LINK', r)
