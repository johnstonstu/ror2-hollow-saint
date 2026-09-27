import bpy, sys, json
from mathutils import Vector
from mathutils.bvhtree import BVHTree
args = sys.argv[sys.argv.index('--')+1:]
OUT = bpy.path.abspath('//')
sc = bpy.context.scene
RIG = bpy.data.objects['Hollow Saint | v8 rig']
ad = RIG.animation_data
for t in ad.nla_tracks: t.mute = True
print('ENGINE', sc.render.engine, sc.render.resolution_x, sc.render.resolution_y, sc.render.resolution_percentage)
def use(name):
    a = bpy.data.actions['HS_anim | '+name]; ad.action = a
    if ad.action_slot is None and len(a.slots): ad.action_slot = a.slots[0]
# overlap metric for idle frames against all non-pad meshes
BODY = bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
def tree(o, dg):
    ev = o.evaluated_get(dg); me = ev.to_mesh(); mw = o.matrix_world
    t = BVHTree.FromPolygons([mw @ v.co for v in me.vertices], [tuple(p.vertices) for p in me.polygons]); ev.to_mesh_clear(); return t
if args[0] == 'overlap':
    use(args[1])
    others = [o for o in bpy.data.objects if o.type=='MESH' and 'pauldron' not in o.name and o.visible_get() and not o.name.startswith(('Warm','V11 bust','VFX'))]
    for f in [int(x) for x in args[2].split(',')]:
        sc.frame_set(f); dg = bpy.context.evaluated_depsgraph_get()
        for s in 'LR':
            pt = tree(bpy.data.objects[f'{s} SHOULDER | V17 pauldron upper'], dg)
            hits = {}
            for o in others:
                n = len(pt.overlap(tree(o, dg)))
                if n: hits[o.name] = n
            print('OVL', args[1], f, s, json.dumps(hits))
else:
    # render: name clip frame cam [target side dist]
    name, clip, f, cam = args[0], args[1], int(args[2]), args[3]
    use(clip); sc.frame_set(f)
    if cam == 'close':
        s, azim = args[4], args[5]
        pad = bpy.data.objects[f'{s} SHOULDER | V17 pauldron upper']
        dg = bpy.context.evaluated_depsgraph_get(); ev = pad.evaluated_get(dg)
        c = sum((pad.matrix_world @ v.co for v in ev.data.vertices), Vector()) / len(ev.data.vertices)
        c.z -= 0.03
        dirs = {'front': Vector((0.35 if s=='L' else -0.35, -1, 0.25)), 'side': Vector((1 if s=='L' else -1, -0.35, 0.2)), 'top': Vector((0.3 if s=='L' else -0.3, -0.6, 1))}
        d = dirs[azim].normalized()
        cd = bpy.data.cameras.new('auditcam'); cd.lens = 50
        co = bpy.data.objects.new('auditcam', cd); sc.collection.objects.link(co)
        co.location = c + d*0.75
        co.rotation_euler = (-d).to_track_quat('-Z','Y').to_euler()
        sc.camera = co
    else:
        sc.camera = bpy.data.objects[cam]
    sc.render.resolution_x, sc.render.resolution_y, sc.render.resolution_percentage = int(args[-2]), int(args[-1]), 100
    sc.render.filepath = OUT + name + '.png'
    bpy.ops.render.render(write_still=True)
    print('WROTE', sc.render.filepath)
