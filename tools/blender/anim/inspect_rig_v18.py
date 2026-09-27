"""Read-only dump of the v18 rig for locomotion work: bones, axes, constraints, cameras, extents.

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/inspect_rig_v18.py
"""
import bpy
import json
from pathlib import Path
from mathutils import Vector

if not bpy.app.background:
    raise RuntimeError('Use isolated --background only')
ROOT=Path(__file__).resolve().parents[3]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'art/hybrid/hollow-saint-hybrid-v18.blend'))
scene=bpy.context.scene
rig=bpy.data.objects['Hollow Saint | v8 rig']
r3=lambda v:[round(c,4) for c in v]
bones={}
for b in rig.data.bones:
    m=b.matrix_local.to_3x3()
    pb=rig.pose.bones[b.name]
    bones[b.name]={'parent':b.parent.name if b.parent else None,'deform':b.use_deform,
                   'head':r3(b.head_local),'tail':r3(b.tail_local),'length':round(b.length,4),
                   'x':r3(m.col[0]),'y':r3(m.col[1]),'z':r3(m.col[2]),
                   'rotation_mode':pb.rotation_mode,
                   'constraints':[{'type':c.type,'name':c.name,'influence':c.influence,
                                   'subtarget':getattr(c,'subtarget',''),'pole':getattr(c,'pole_subtarget',''),
                                   'pole_angle':round(getattr(c,'pole_angle',0),4)} for c in pb.constraints],
                   'collections':[c.name for c in b.collections]}
objs=[]
for o in scene.objects:
    objs.append({'name':o.name,'type':o.type,'parent':o.parent.name if o.parent else None,
                 'parent_type':o.parent_type,'parent_bone':o.parent_bone,
                 'mods':[m.type for m in o.modifiers],'loc':r3(o.matrix_world.translation),
                 'hide_render':o.hide_render})
cams={o.name:{'loc':r3(o.matrix_world.translation),'rot':r3(o.rotation_euler),'type':o.data.type,
              'ortho':round(o.data.ortho_scale,3),'lens':round(o.data.lens,2)} for o in scene.objects if o.type=='CAMERA'}
actions=[{'name':a.name,'range':list(a.frame_range),'cyclic':a.use_cyclic,'markers':{m.name:m.frame for m in a.pose_markers},
          'users':a.users,'fake':a.use_fake_user} for a in bpy.data.actions]
body=bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
zs=[(body.matrix_world@v.co).z for v in body.data.vertices]
out={'rig':rig.name,'rig_matrix':[r3(r) for r in rig.matrix_world],'fps':scene.render.fps,
     'engine':scene.render.engine,'res':[scene.render.resolution_x,scene.render.resolution_y],
     'body_z':[round(min(zs),4),round(max(zs),4)],'pose_position':rig.data.pose_position,
     'active_action':rig.animation_data.action.name if rig.animation_data and rig.animation_data.action else None,
     'bones':bones,'cameras':cams,'actions':actions,'objects':objs,
     'scene_keys':{k:str(scene[k])[:200] for k in scene.keys()}}
dst=ROOT/'art/anim/inspect-v18.json'
dst.parent.mkdir(parents=True,exist_ok=True)
dst.write_text(json.dumps(out,indent=1),encoding='utf-8')
print('WROTE',dst,len(bones),'bones',len(objs),'objects',flush=True)
