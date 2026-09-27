"""Assemble clip modules onto v18 and save the next animation checkpoint (never overwrites).

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/build_anim.py -- run walk glide
Writes art/anim/hollow-saint-anim-vN.blend (next free N) and art/anim/vN/catalog.json.
"""
import bpy
import importlib
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
sys.path.insert(0, str(Path(__file__).parent/'clips'))
import hs_anim as H

modules = sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
if not modules:
    raise SystemExit('name at least one clip module')
out_dir = H.ROOT/'art/anim'
n = 1
while (out_dir/f'hollow-saint-anim-v{n}.blend').exists():
    n += 1
blend = out_dir/f'hollow-saint-anim-v{n}.blend'
start_hash = H.START.stat().st_mtime_ns

p = H.open_start()
scene = bpy.context.scene
scene.render.fps = H.FPS
catalog = []
for m in modules:
    for act, info in importlib.import_module(m).build(p):
        if info.get('status', 'PASS') != 'PASS' and info['bake_error_m'] > 1e-3:
            raise RuntimeError(f'{act.name} failed bake')
        catalog.append({**info, 'module': m, 'action': act.name})
p.ik(0, 0)
p.reset()
p.rig.animation_data.action = bpy.data.actions[catalog[0]['action']]
p.rig['anim_toolkit'] = ('HS_anim clips are baked FK on deform bones; IK ships at influence 0. '
                         f'Knee pole angles re-solved on a bent leg: {p.pole_angles}.')
scene['anim_checkpoint'] = f'v{n}: ' + ', '.join(c['action'] for c in catalog)
scene.frame_start, scene.frame_end = catalog[0]['frames']
scene.frame_set(catalog[0]['frames'][0])
assert not blend.exists()
bpy.ops.wm.save_as_mainfile(filepath=str(blend))
assert H.START.stat().st_mtime_ns == start_hash, 'v18 changed'
(out_dir/f'v{n}').mkdir(exist_ok=True)
(out_dir/f'v{n}'/'catalog.json').write_text(json.dumps(catalog, indent=2), encoding='utf-8')
print('SAVED', blend, len(catalog), 'clips', flush=True)
