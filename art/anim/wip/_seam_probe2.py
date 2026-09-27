"""Baked L shin at Land end / Jump f1 / Idle f1 (air + presentation only). Background Blender."""
import sys
sys.path[:0] = ['tools/blender/anim', 'tools/blender/anim/clips']
import bpy
import hs_anim as H
import air
import presentation as P

p = H.open_start()
clips = {}
for m in (air, P):
    for act, info in m.build(p):
        clips[info['title']] = act
for title, f in (('Land', air.N_LAND), ('Jump', 1), ('Idle', 1)):
    p.rig.animation_data.action = clips[title]
    bpy.context.scene.frame_set(f)
    p.update()
    print('SEAMPROBE2', title, f, {b: tuple(round(v, 5) for v in p.pb[b].matrix.translation) for b in ('L shin', 'L foot', 'L thigh')},
          flush=True)
