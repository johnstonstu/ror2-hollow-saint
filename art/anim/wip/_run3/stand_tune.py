"""Score stand-arm variants (air.STAND_ARMS) live: contact depth, clearance, renders. Never saved.

blender --background --factory-startup --python art/anim/wip/_run3/stand_tune.py -- variants.json [--render]
variants.json: [{"name": "a", "arms": {...STAND_ARMS overrides}}, ...]
"""
import bpy
import json
import sys
from pathlib import Path
from mathutils import Matrix, Vector

TOOLS = Path(__file__).resolve().parents[4]/'tools/blender/anim'
sys.path.insert(0, str(TOOLS))
sys.path.insert(0, str(TOOLS/'clips'))
import hs_anim as H
import air
import clearance
import contact
import handpass

args = sys.argv[sys.argv.index('--')+1:]
variants = json.loads(Path(args[0]).read_text())
p = H.open_start()
hp = handpass.HandPass(p)
clr = clearance.Clearance()
con = contact.Contact()
scene = bpy.context.scene
cam = bpy.data.objects.new('tune', bpy.data.cameras.new('tune'))
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 0.42
scene.collection.objects.link(cam)
H.eevee(scene, 8)
out = Path(__file__).parent/'tune'
out.mkdir(exist_ok=True)
base = dict(air.STAND_ARMS)
for v in variants:
    air.STAND_ARMS = {**base, **v['arms']}
    p.reset()
    p.ik(1.0, 0.0)
    air.stand_pose(p)
    hp.apply()
    p.update()
    c = con.frame()
    cl = clr.frame()
    hands = {s: [round(x, 3) for x in p.pb[f'{s} hand'].matrix.translation] for s in H.SIDES}
    print('VARIANT', v['name'], json.dumps({'contact_mm': {s: {k: round(x*1000, 1) for k, x in c[s].items()} for s in c},
                                             'clear_mm': {s: [round(cl[s][0]*1000, 1), cl[s][1], cl[s][2]] for s in cl},
                                             'hand': hands}), flush=True)
    if '--render' in args:
        for s in H.SIDES:
            k = H.sign(s)
            hc = p.pb[f'{s} hand'].matrix.translation
            for view, d in (('front', Vector((0, -1, 0))), ('out', Vector((k, 0, 0)))):
                z = d
                x = Vector((0, 0, 1)).cross(z).normalized()
                y = z.cross(x)
                cam.matrix_world = Matrix.Translation(hc+3.0*z) @ Matrix((x, y, z)).transposed().to_4x4()
                H.render_still(scene, cam.name, out/f"{v['name']}-{s}-{view}.png", (300, 300))
        cam.data.ortho_scale = 2.3
        z = Vector((0, -1, 0))
        x = Vector((0, 0, 1)).cross(z).normalized()
        cam.matrix_world = Matrix.Translation(Vector((-0.04, -3, 1.0))) @ Matrix((x, z.cross(x), z)).transposed().to_4x4()
        H.render_still(scene, cam.name, out/f"{v['name']}-full.png", (300, 400))
        cam.data.ortho_scale = 0.42
print('DONE', flush=True)
