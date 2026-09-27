"""Print world positions of the head, pelvis, foot joints and the heel-jet base/tip in the Glide loop.

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/jet_probe.py -- 1,17
"""
import bpy
import sys
from pathlib import Path
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).parent))
sys.path.insert(0, str(Path(__file__).parent/'clips'))
import hs_anim as H
import vfx

frames = [int(v) for v in sys.argv[sys.argv.index('--')+1:][0].split(',')]
p = H.open_start()
import glide
built = glide.build(p)
fmt = lambda v: '(' + ', '.join(f'{c:+.3f}' for c in v) + ')'
for act, info in built:
    if info['title'] != 'Glide loop':
        continue
    p.rig.animation_data.action = act
    for f in frames:
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        dg = bpy.context.evaluated_depsgraph_get()
        mw = p.rig.matrix_world
        out = {n: mw @ p.pb[n].head for n in ('head', 'pelvis', 'L foot', 'L toe')}
        out['L toe tail'] = mw @ p.pb['L toe'].tail
        o = bpy.data.objects['VFX | L heel jet outer'].evaluated_get(dg)
        out['jet base'] = o.matrix_world @ Vector((0, 0, 0))
        out['jet tip'] = o.matrix_world @ Vector((0, 0, -vfx.JET_LEN))
        print('JET', f, ' '.join(f'{k}={fmt(v)}' for k, v in out.items()), flush=True)
        for ob in bpy.data.collections[vfx.COLLECTION].objects:
            if not ob.name.startswith('VFX | L'):
                continue
            ev = ob.evaluated_get(dg)
            ws = [ev.matrix_world @ v.co for v in ev.data.vertices]
            lo = Vector([min(w[i] for w in ws) for i in range(3)])
            hi = Vector([max(w[i] for w in ws) for i in range(3)])
            print('JETOBJ', f, ob.name, 'scale', fmt(ev.matrix_world.to_scale()), 'min', fmt(lo), 'max', fmt(hi),
                  'hide_render', ob.hide_render, flush=True)
