"""Scratch (hands2): hand-pose lab. Rest arms (air.STAND_ARMS), one library level per variant, close-ups.

blender --background --factory-startup --python-exit-code 1 --python art/anim/wip/hands2/lab.py -- <out dir> "<py dict of variants>"
variant: {'name': {'curl': 21, 'thumb': 12, 'set': {'handpose.THUMB': {...}}}}
Renders <out>/<name>-{L-profile,L-back,L-palm,pair}.png and prints bends/thumb-tip distances.
"""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Matrix, Vector

TOOLS = Path(__file__).resolve().parents[4]/'tools/blender/anim'
sys.path.insert(0, str(TOOLS))
sys.path.insert(0, str(TOOLS/'clips'))
import hs_anim as H
import handorient
import handpose
import handpass

args = sys.argv[sys.argv.index('--')+1:]
out = Path(args[0])
out = out if out.is_absolute() else Path(__file__).resolve().parents[4]/out
out.mkdir(parents=True, exist_ok=True)
variants = eval(args[1])
p = H.open_start()
import air
scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
sh = scene.display.shading
sh.light, sh.color_type, sh.show_shadows, sh.show_cavity = 'STUDIO', 'OBJECT', False, True
COL = {'thumb': (0.95, 0.12, 0.1, 1), 'index': (0.15, 0.8, 0.2, 1), 'middle': (0.95, 0.95, 0.95, 1),
       'ring': (0.55, 0.55, 0.58, 1), 'little': (0.6, 0.25, 0.85, 1)}
for o in bpy.data.objects:
    if o.type not in ('MESH', 'CURVE'):
        continue
    c = (0.42, 0.42, 0.45, 1)
    if 'HAND |' in o.name:
        tail = o.name.split('|', 1)[1]
        c = (0.2, 0.45, 1.0, 1)
        for dg, col in COL.items():
            if f' {dg} ' in f' {tail} ':
                c = col
    if o.name.startswith('VFX'):
        o.hide_render = True
    o.color = c
cam = bpy.data.objects.new('lab', bpy.data.cameras.new('lab'))
cam.data.type = 'ORTHO'
scene.collection.objects.link(cam)


def look(c, off, up, scale, slab):
    z = off.normalized()
    x = up.cross(z).normalized()
    y = z.cross(x)
    cam.data.ortho_scale = scale
    cam.data.clip_start, cam.data.clip_end = (1.2-slab, 1.2+slab) if slab else (0.05, 20)
    cam.matrix_world = Matrix.Translation(c+1.2*z) @ Matrix((x, y, z)).transposed().to_4x4()


hp = handpass.HandPass(p)
A = air.STAND_ARMS
for name, v in variants.items():
    for k, val in v.get('set', {}).items():
        mod, attr = k.rsplit('.', 1)
        setattr(sys.modules[mod], attr, val)
    p.reset()
    p.ik(0, 0)
    for s in H.SIDES:
        p.arm(s, swing=A['swing'], adduct=A['adduct'], elbow=A['elbow'], twist=A['twist'], wrist=(A['wrist'], 0, 0))
        p.curl(s, v.get('curl', 21), v.get('thumb', 12))
    hp.apply()
    p.update()
    mw = p.rig.matrix_world
    pb = p.pb
    s = 'L'
    d, n, r = handorient.hand_axes(p.rig, s)
    c = 0.5*(mw @ pb[f'{s} hand'].head+mw @ pb[f'{s} middle.2'].head)
    for view, off in (('profile', r), ('back', -n), ('palm', n)):
        look(c, off, -d, 0.30, 0.11)
        H.render_still(scene, cam.name, out/f'{name}-L-{view}.png', (260, 260))
    cc = 0.5*(mw @ pb['L hand'].head+mw @ pb['R hand'].head)
    cc.z -= 0.04
    look(cc, Vector((0, -1, 0)), Vector((0, 0, 1)), 1.1, 0)
    H.render_still(scene, cam.name, out/f'{name}-pair.png', (520, 260))
    bends = {dg: [[round(b, 1), round(sd, 1)] for b, sd in hp.geo.measure(pb, s, dg)] for dg in handpose.DIGITS}
    bends['thumb'] = [round(hp.geo.bend(f'{s} thumb.{i}', pb[f'{s} thumb.{i}'].rotation_quaternion), 1) for i in (1, 2, 3)]
    fa = (mw.to_3x3() @ pb[f'{s} forearm'].matrix.to_3x3()).col[1]
    bends['tip_vs_forearm'] = {dg: round(math.degrees(fa.angle((mw.to_3x3() @ pb[f'{s} {dg}.3'].matrix.to_3x3()).col[1])))
                               for dg in handpose.DIGITS}
    tip = mw @ pb[f'{s} thumb.3'].tail
    i2 = mw @ pb[f'{s} index.2'].head
    rel = tip-i2
    print('LAB', name, json.dumps(bends), 'thumbtip-index2: dist %.3f along n %+.3f along r %+.3f along d %+.3f'
          % (rel.length, rel.dot(n), rel.dot(r), rel.dot(d)), flush=True)
print('LAB DONE', flush=True)
