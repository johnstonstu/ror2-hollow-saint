"""Debug one clip frame: contact detail + unclipped front/outside renders around each hand.

blender --background --factory-startup --python art/anim/wip/_run3/contact_debug.py -- <module> <clip-slug> <f1,f2>
Renders to art/anim/wip/_run3/debug/<slug>-f<N>-<side>-<view>.png
"""
import bpy
import importlib
import json
import sys
from pathlib import Path
from mathutils import Matrix, Vector

TOOLS = Path(__file__).resolve().parents[4]/'tools/blender/anim'
sys.path.insert(0, str(TOOLS))
sys.path.insert(0, str(TOOLS/'clips'))
import hs_anim as H
import contact

args = sys.argv[sys.argv.index('--')+1:]
module, want, frames = args[0], args[1], [int(x) for x in args[2].split(',')]
p = H.open_start()
c = contact.Contact()
c.debug = True
slug = lambda t: ''.join(ch if ch.isalnum() else '-' for ch in t.lower()).strip('-')
act = next(a for a, i in importlib.import_module(module).build(p) if slug(i['title']) == want)
p.rig.animation_data.action = act
scene = bpy.context.scene
cam = bpy.data.objects.new('dbg', bpy.data.cameras.new('dbg'))
cam.data.type = 'ORTHO'
cam.data.ortho_scale = float(args[args.index('--scale')+1]) if '--scale' in args else 0.55
scene.collection.objects.link(cam)
H.eevee(scene, 8)
out = Path(__file__).parent/'debug'
out.mkdir(exist_ok=True)
for f in frames:
    scene.frame_set(f)
    p.update()
    c.debug_hits = {}
    print('FRAME', f, json.dumps(c.frame()), json.dumps(c.worst), flush=True)
    if '--norender' in args:
        continue
    for s in H.SIDES:
        k = H.sign(s)
        hc = p.pb[f'{s} hand'].matrix.translation
        for view, d in (('front', Vector((0, -1, 0))), ('out', Vector((k, 0, 0))), ('back', Vector((0, 1, 0)))):
            z = d                                  # camera sits on the d side of the hand, looking back along -d
            x = Vector((0, 0, 1)).cross(z).normalized()
            y = z.cross(x)
            cam.matrix_world = Matrix.Translation(hc+3.0*z) @ Matrix((x, y, z)).transposed().to_4x4()
            H.render_still(scene, cam.name, out/f'{want}-f{f}-{s}-{view}.png', (360, 360))
            if '--xray' in args:
                body = bpy.data.objects[H.BODY]
                body.hide_render = True
                H.render_still(scene, cam.name, out/f'{want}-f{f}-{s}-{view}-nobody.png', (360, 360))
                body.hide_render = False
print('DONE', flush=True)
