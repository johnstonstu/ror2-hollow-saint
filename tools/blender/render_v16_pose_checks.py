"""Render worst-frame pauldron poses for a checkpoint (read-only).

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/render_v16_pose_checks.py -- v16
"""
import bpy
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
VERSION=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'v16'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/f'art/hybrid/hollow-saint-hybrid-{VERSION}.blend'))
scene=bpy.context.scene
rig=bpy.data.objects['Hollow Saint | v8 rig']
prefs=bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type='OPTIX'
prefs.get_devices()
for d in prefs.devices:d.use=d.type!='CPU'
scene.cycles.device='GPU'
scene.cycles.samples=96
out=ROOT/'art/hybrid/v16'
for action,frame,label in (('HS_v10 | Arc Bolt - two finger snap',9,'arc-bolt-f9'),('HS_v10 | Discharge - break the seal',22,'discharge-f22')):
    rig.animation_data.action=bpy.data.actions[action]
    scene.frame_set(frame)
    for cam,view in (('Hero three quarter','hero'),('Front orthographic','front')):
        scene.camera=bpy.data.objects[cam]
        scene.render.resolution_x,scene.render.resolution_y=1100,1400
        scene.render.filepath=str(out/f'pose-{VERSION}-{label}-{view}.png')
        bpy.ops.render.render(write_still=True)
print('DONE')
