import bpy, sys, os, math
from mathutils import Vector
OUT=os.path.dirname(bpy.data.filepath)
jobs=[j.split(':') for j in sys.argv[sys.argv.index('--')+1].split(';')]
arm=bpy.data.objects["Hollow Saint | v8 rig"]
sc=bpy.context.scene
sc.render.engine='BLENDER_WORKBENCH'
sh=sc.display.shading
sh.light='STUDIO'; sh.color_type='OBJECT'; sh.show_shadows=False; sh.show_cavity=True
sc.render.resolution_x=640; sc.render.resolution_y=480; sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG'
sc.render.film_transparent=False
for o in bpy.data.objects:
    if o.type in ('MESH','CURVE'):
        n=o.name
        c=(0.55,0.55,0.58,1)
        if 'HAND |' in n:
            c=(0.75,0.75,0.78,1)
            if 'thumb' in n: c=(0.95,0.1,0.1,1)
            elif 'palm' in n: c=(0.2,0.45,1.0,1)
            elif 'segment 3' in n or 'tip light' in n: c=(1.0,0.85,0.1,1)
        if n.startswith('VFX') or 'backdrop' in n or 'ground' in n: o.hide_render=True
        o.color=c
cam_data=bpy.data.cameras.new('auditcam'); cam_data.type='ORTHO'
cam=bpy.data.objects.new('auditcam',cam_data); sc.collection.objects.link(cam); sc.camera=cam
P=arm.pose.bones
chest_rest=arm.data.bones["chest"].matrix_local.to_3x3()
def W(o): return o.matrix_world @ (sum((Vector(c) for c in o.bound_box),Vector())/8)
for clip,frame,view in jobs:
    if clip=='REST':
        arm.data.pose_position='REST'; frame=1
    else:
        arm.data.pose_position='POSE'
        a=bpy.data.actions["HS_anim | "+clip]; arm.animation_data.action=a
        try: arm.animation_data.action_slot=a.slots[0]
        except: pass
    sc.frame_set(int(frame))
    cm=arm.matrix_world.to_3x3() @ P["chest"].matrix.to_3x3() @ chest_rest.inverted()
    fwd=cm @ Vector((0,-1,0)); fwd.z=0; fwd.normalize()
    right=fwd.cross(Vector((0,0,1))).normalized()
    pts=[W(o) for o in bpy.data.objects if (('L HAND |' in o.name) if view=='lclose' else ('HAND |' in o.name or 'SHOULDER |' in o.name))]
    lo=Vector([min(p[i] for p in pts) for i in range(3)]); hi=Vector([max(p[i] for p in pts) for i in range(3)])
    c=(lo+hi)/2
    if view=='front': dirv=fwd
    elif view=='q': dirv=(fwd+0.8*right+0.5*Vector((0,0,1))).normalized()
    elif view=='lside': dirv=-right
    elif view=='lclose': dirv=(fwd-0.9*right+0.25*Vector((0,0,1))).normalized()
    elif view=='top': dirv=(fwd*0.6+Vector((0,0,1))).normalized()
    cam.location=c+dirv*6
    cam.rotation_euler=(-dirv).to_track_quat('-Z','Y').to_euler()
    ext=max((hi-lo).length*0.75,0.6) if view!='lclose' else 0.3; cam_data.ortho_scale=ext*1.25
    sc.render.filepath=os.path.join(OUT,f"orient-{clip.replace(' ','-').lower()}-f{int(frame):02d}-{view}.png")
    bpy.ops.render.render(write_still=True)
    print("RENDERED",sc.render.filepath,flush=True)



