"""Scratch (hands2): build modules on a fresh v18 and save a scratch blend under art/anim/wip/hands2/_work/.

blender --background --factory-startup --python-exit-code 1 --python art/anim/wip/hands2/scratch_build.py -- <tag> mod1 mod2 [--set module.NAME=value]
Writes _work/<tag>-N.blend (next free N; never overwrites) and prints each clip's hand fields.
"""
import bpy
import importlib
import json
import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[4]/'tools/blender/anim'
sys.path.insert(0, str(TOOLS))
sys.path.insert(0, str(TOOLS/'clips'))
import hs_anim as H

args = sys.argv[sys.argv.index('--')+1:]
tag = args[0]
mods = [a for i, a in enumerate(args[1:], 1) if not a.startswith('--') and args[i-1] != '--set']
for kv in [args[i+1] for i, a in enumerate(args) if a == '--set']:
    name, value = kv.split('=')
    mname, attr = name.rsplit('.', 1)
    setattr(importlib.import_module(mname), attr, float(value) if value.replace('.', '', 1).lstrip('-').isdigit() else eval(value))
work = Path(__file__).parent/'_work'
n = 1
while (work/f'{tag}-{n}.blend').exists():
    n += 1
out = work/f'{tag}-{n}.blend'
p = H.open_start()
for m in mods:
    for act, info in importlib.import_module(m).build(p):
        print('CLIP', info['title'], json.dumps({k: info.get(k) for k in ('hand_settle_max_deg', 'hand_relax_peak_deg',
                                                                            'bake_error_m', 'orient_infeasible')}), flush=True)
p.ik(0, 0)
p.followers(False)
p.reset()
assert not out.exists()
bpy.ops.wm.save_as_mainfile(filepath=str(out))
print('SCRATCH SAVED', out, flush=True)
