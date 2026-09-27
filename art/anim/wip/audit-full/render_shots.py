"""Close-up diagnostic stills (workbench, part highlighting). Read-only on the copy; never saves.
blender --background --factory-startup audit-full-v16-copy.blend --python render_shots.py -- shots.json"""
import bpy, sys, json, math
import numpy as np
from mathutils import Vector
args = sys.argv[sys.argv.index('--')+1:]
OUT = bpy.path.abspath('//')
jobs = json.load(open(OUT + args[0]))
sc = bpy.context.scene
RIG = bpy.data.objects['Hollow Saint | v8 rig']; ad = RIG.animation_data
BODY = bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
exec(open(OUT + 'parts_lib.py').read())
if len(args) > 1 and args[1] == 'noshrink':
    for o in bpy.data.objects:
        for m in getattr(o, 'modifiers', []):
            if m.type == 'SHRINKWRAP': m.show_viewport = False; m.show_render = False
bdom = dominant(BODY); breg = [region_of(g) for g in bdom]
GRAY = (0.62, 0.62, 0.64, 1)
def paint(o, cols):
    me = o.data
    if 'audit_col' in me.color_attributes: me.color_attributes.remove(me.color_attributes['audit_col'])
    ca = me.color_attributes.new('audit_col', 'FLOAT_COLOR', 'POINT')
    flat = np.array(cols, dtype=np.float32).reshape(-1)
    ca.data.foreach_set('color', flat)
    me.color_attributes.active_color = ca
    try: me.color_attributes.render_color_index = me.color_attributes.find('audit_col')
    except Exception: pass
sc.render.engine = 'BLENDER_WORKBENCH'
sh = sc.display.shading
sh.light = 'STUDIO'; sh.color_type = 'VERTEX' if hasattr(sh, 'color_type') else 'VERTEX'
try: sh.color_type = 'VERTEX'
except Exception: sh.color_type = 'ATTRIBUTE'
sh.show_cavity = True; sh.show_object_outline = True; sh.show_specular_highlight = True
sc.render.film_transparent = False
for o in bpy.data.objects:
    if o.name.startswith(('VFX', 'V11 bust')): o.hide_render = True
    if o.type == 'CURVE': o.hide_render = True
cd = bpy.data.cameras.new('auditcam'); cam = bpy.data.objects.new('auditcam', cd); sc.collection.objects.link(cam); sc.camera = cam
for j in jobs:
    hl = j.get('highlight', {})
    for o in bpy.data.objects:
        if o.type != 'MESH' or o.hide_render: continue
        if o == BODY:
            cols = [hl.get('body:' + r, GRAY) for r in breg]
        else:
            g = group_of(o)
            c = hl.get(g, GRAY) if g else (0.5, 0.48, 0.45, 1)
            cols = [c] * len(o.data.vertices)
        paint(o, cols)
    for o in j.get('hide', []):
        for ob in bpy.data.objects:
            if group_of(ob) == o: ob.hide_render = True
    a = bpy.data.actions['HS_anim | ' + j['clip']]; ad.action = a
    if ad.action_slot is None and len(a.slots): ad.action_slot = a.slots[0]
    sc.frame_set(j['frame'])
    t = j['target']
    if t.startswith('bone:'):
        pb = RIG.pose.bones[t[5:]]; tgt = RIG.matrix_world @ (pb.head.lerp(pb.tail, j.get('along', 0.0)))
    else:
        tgt = Vector(j['point'])
    tgt = tgt + Vector(j.get('offset', (0, 0, 0)))
    d = Vector(j['dir']).normalized()
    cd.lens = j.get('lens', 50); cd.clip_start = 0.01
    cam.location = tgt + d * j.get('dist', 0.9)
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    sc.render.resolution_x, sc.render.resolution_y, sc.render.resolution_percentage = 1000, 1000, 100
    sc.render.filepath = OUT + j['name'] + '.png'
    bpy.ops.render.render(write_still=True)
    print('WROTE', sc.render.filepath, flush=True)
    for o in j.get('hide', []):
        for ob in bpy.data.objects:
            if group_of(ob) == o: ob.hide_render = False
