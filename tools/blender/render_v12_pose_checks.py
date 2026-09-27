"""Read-only pose check renders for v12 pauldrons at the worst QA frames (does not save the .blend)."""
import bpy
from pathlib import Path

if not bpy.app.background:
    raise RuntimeError('Use isolated --background only')
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/hybrid/v12'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'art/hybrid/hollow-saint-hybrid-v12.blend'))
prefs=bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type='OPTIX'
prefs.get_devices()
for d in prefs.devices:
    d.use=d.type=='OPTIX'
scene=bpy.context.scene
scene.cycles.device='GPU'
scene.cycles.samples=64
rig=bpy.data.objects['Hollow Saint | v8 rig']
cams={c.name:c for c in bpy.data.objects if c.type=='CAMERA'}
CHECKS=[('HS_v10 | Arc Step - in place dash',9),('HS_v10 | Arc Bolt - two finger snap',9)]
for action_name,frame in CHECKS:
    rig.animation_data.action=bpy.data.actions[action_name]
    scene.frame_set(frame)
    tag=action_name.split('|')[1].split('-')[0].strip().lower().replace(' ','-')
    for cam_key,label in (('hero','hero'),('side','side')):
        cam=next(c for n,c in cams.items() if cam_key in n.lower())
        scene.camera=cam
        scene.render.filepath=str(OUT/f'check-{tag}-f{frame}-{label}.png')
        bpy.ops.render.render(write_still=True)
print('DONE')
