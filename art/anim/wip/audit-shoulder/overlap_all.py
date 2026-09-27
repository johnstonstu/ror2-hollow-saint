import bpy, json
from mathutils.bvhtree import BVHTree
OUT = bpy.path.abspath('//'); sc = bpy.context.scene
RIG = bpy.data.objects['Hollow Saint | v8 rig']; ad = RIG.animation_data
for t in ad.nla_tracks: t.mute = True
pads = {s: bpy.data.objects[f'{s} SHOULDER | V17 pauldron upper'] for s in 'LR'}
others = [o for o in bpy.data.objects if o.type=='MESH' and 'pauldron' not in o.name and not o.name.startswith(('Warm','V11 bust','VFX')) and not o.hide_render]
def tree(o, dg):
    ev = o.evaluated_get(dg); me = ev.to_mesh(); mw = o.matrix_world
    t = BVHTree.FromPolygons([mw @ v.co for v in me.vertices], [tuple(p.vertices) for p in me.polygons]); ev.to_mesh_clear(); return t
res = {}
for a in [a for a in bpy.data.actions if a.name.startswith('HS_anim | ')]:
    ad.action = a
    if ad.action_slot is None and len(a.slots): ad.action_slot = a.slots[0]
    r = {'L': {}, 'R': {}}
    for f in range(int(a.frame_range[0]), int(a.frame_range[1])+1):
        sc.frame_set(f); dg = bpy.context.evaluated_depsgraph_get()
        for s in 'LR':
            pt = tree(pads[s], dg)
            for o in others:
                n = len(pt.overlap(tree(o, dg)))
                if n:
                    e = r[s].setdefault(o.name, [0, 0, 0]); e[1] += 1
                    if n > e[0]: e[0], e[2] = n, f
    res[a.name[10:]] = r
json.dump(res, open(OUT+'shoulder-overlap.json','w'), indent=1); print('DONE')
