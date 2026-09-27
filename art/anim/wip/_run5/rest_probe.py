"""Which baked bones differ from rest on a clip's first/last frame (armature space).
Run: blender -b --factory-startup --python art/anim/wip/_run5/rest_probe.py -- <module> "<Title>:<frame>;..." """
import sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT/'tools/blender/anim'))
sys.path.insert(0, str(ROOT/'tools/blender/anim/clips'))
import bpy
import importlib
import hs_anim as H

args = sys.argv[sys.argv.index('--')+1:]
p = H.open_start()
built = {i['title']: a for a, i in importlib.import_module(args[0]).build(p)}
p.ik(0, 0)
p.followers(False)
for job in args[1].split(';'):
    title, f = job.rsplit(':', 1)
    p.rig.animation_data.action = built[title]
    bpy.context.scene.frame_set(int(f))
    p.update()
    rows = []
    for n in p.fk:
        m = p.pb[n].matrix
        d = (m.translation-p.rest[n].translation).length
        q = m.to_quaternion().rotation_difference(p.rest[n].to_quaternion())
        rows.append((round(d*1000, 1), round(q.angle*57.2958, 1), n))
    rows.sort(reverse=True)
    print('REST', title, f, rows[:12], flush=True)
