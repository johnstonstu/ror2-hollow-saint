"""Run the 9i full-body QA (fullqa.FullQA) over the HS_anim actions in a saved checkpoint.

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/fullqa_audit.py --
     art/anim/hollow-saint-anim-vNN.blend art/anim/wip/audit-full/fixes/qa-vNN.json ["Title A;Title B"]
Nothing is saved to the .blend.
"""
import bpy
import json
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import hs_anim as H
import fullqa

args = sys.argv[sys.argv.index('--')+1:]
H.require_background()
bpy.ops.wm.open_mainfile(filepath=str(H.ROOT/args[0]))
rig = bpy.data.objects[H.RIG]
for t in rig.animation_data.nla_tracks:
    t.mute = True


class Rig:
    def __init__(self, rig):
        self.rig = rig
        self.pb = rig.pose.bones


rig.data.pose_position = 'REST'
rig.animation_data.action = None
bpy.context.view_layer.update()
t0 = time.time()
fq = fullqa.FullQA(Rig(rig))
report = {'rest': fq.calibrate_rest(), 'symmetry': fullqa.symmetry(rig), 'clips': {}}
print('FULLQA REST', round(time.time()-t0, 1), json.dumps(report['rest'])[:2000], flush=True)
rig.data.pose_position = 'POSE'
scene = bpy.context.scene
wanted = set(args[2].split(';')) if len(args) > 2 and args[2] else None
for act in sorted((a for a in bpy.data.actions if a.name.startswith(H.PREFIX)), key=lambda a: a.name):
    title = act.name[len(H.PREFIX):]
    if wanted and title not in wanted:
        continue
    info = json.loads(act.get('clip_json', '{}'))
    rig.animation_data.action = act
    if getattr(rig.animation_data, 'action_slot', None) is None and len(getattr(act, 'slots', [])):
        rig.animation_data.action_slot = act.slots[0]
    fq.reset()
    t1 = time.time()
    for f in range(int(act.frame_range[0]), int(act.frame_range[1])+1):
        scene.frame_set(f)
        bpy.context.view_layer.update()
        fq.frame(f)
    report['clips'][title] = fq.summarize(info.get('loop', bool(act.use_cyclic)), info.get('accents', ()),
                                          info.get('contact_exempt', ()))
    print('FULLQA', title, round(time.time()-t1, 1), json.dumps(report['clips'][title]['full_qa']['fails']),
          flush=True)
out = H.ROOT/args[1]
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(report, indent=1), encoding='utf-8')
print('FULLQA AUDIT DONE', round(time.time()-t0, 1), flush=True)
