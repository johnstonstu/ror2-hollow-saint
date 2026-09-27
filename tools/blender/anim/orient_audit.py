"""Run the 9f orientation QA (handorient.OrientQA) over every HS_anim action in a saved checkpoint.

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/orient_audit.py --
     art/anim/hollow-saint-anim-vNN.blend art/anim/wip/hands/orientation/audit-vNN.json
Nothing is saved to the .blend.
"""
import bpy
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import hs_anim as H
import handorient

args = sys.argv[sys.argv.index('--')+1:]
H.require_background()
bpy.ops.wm.open_mainfile(filepath=str(H.ROOT/args[0]))
rig = bpy.data.objects[H.RIG]
rig.data.pose_position = 'REST'
rig.animation_data.action = None
bpy.context.view_layer.update()
oq = handorient.OrientQA(rig)
report = {'rest_handedness': oq.calibrate_rest(), 'clips': {}}
rig.data.pose_position = 'POSE'
scene = bpy.context.scene
for act in sorted((a for a in bpy.data.actions if a.name.startswith(H.PREFIX)), key=lambda a: a.name):
    rig.animation_data.action = act
    if getattr(rig.animation_data, 'action_slot', None) is None and len(getattr(act, 'slots', [])):
        rig.animation_data.action_slot = act.slots[0]
    oq.reset()
    for f in range(int(act.frame_range[0]), int(act.frame_range[1])+1):
        scene.frame_set(f)
        bpy.context.view_layer.update()
        oq.frame(f)
    report['clips'][act.name[len(H.PREFIX):]] = oq.summarize(bool(act.use_cyclic))
    print('ORIENT', act.name, json.dumps(report['clips'][act.name[len(H.PREFIX):]]), flush=True)
out = H.ROOT/args[1]
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(report, indent=1), encoding='utf-8')
print('ORIENT AUDIT DONE', flush=True)
