"""Signed hand/forearm-into-body depth probe (background Blender, fresh v18, never saved).

blender --background --factory-startup --python art/anim/wip/_run3/contact_probe.py -- <module> [clip-slug,...]
Prints rest-pose clearance and, per clip frame, the deepest hand/forearm vertex inside the torso/thigh surface
or the tabard.
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
import clearance
import contact

args = sys.argv[sys.argv.index('--')+1:]
p = H.open_start()
c = contact.Contact()
p.rig.animation_data.action = None
p.reset()
p.update()
print('REST', json.dumps(c.frame()), flush=True)
if args:
    mod = importlib.import_module(args[0])
    wanted = set(args[1].split(',')) if len(args) > 1 else None
    slug = lambda t: ''.join(ch if ch.isalnum() else '-' for ch in t.lower()).strip('-')
    for act, info in mod.build(p):
        if wanted and slug(info['title']) not in wanted:
            continue
        p.rig.animation_data.action = act
        rows = []
        for f in range(int(act.frame_range[0]), int(act.frame_range[1])+1):
            bpy.context.scene.frame_set(f)
            p.update()
            rows.append((f, c.frame()))
        print('CLIP', info['title'], json.dumps(contact.summarize(rows, c.rest)), flush=True)
        if '--frames' in args:
            for f, r in rows:
                w = {f'{s} {g}': round(v*1000, 1) for s in r for g, v in r[s].items() if v > 0.0005}
                if w:
                    print('  F', f, w, flush=True)
