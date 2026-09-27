"""Hybrid v15: touch-ups (outer forearm cyan conductors, wrist-back dashes, pauldron tone matched to body).

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/build_hybrid_v15.py
"""
import bpy
import json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

if not bpy.app.background:
    raise RuntimeError('Use isolated --background --factory-startup only')
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'art/hybrid/hollow-saint-hybrid-v14.blend'
OUT_BLEND=ROOT/'art/hybrid/hollow-saint-hybrid-v15.blend'
OUT_DIR=ROOT/'art/hybrid/v15'
OUT_DIR.mkdir(parents=True,exist_ok=True)
TAG='v15'

bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
rig=bpy.data.objects['Hollow Saint | v8 rig']
rig.data.pose_position='REST'
bpy.context.view_layer.update()
report={'source':str(SOURCE.relative_to(ROOT)),'changes':[]}

body=bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
bverts=[body.matrix_world@v.co for v in body.data.vertices]
bpolys=[tuple(p.vertices) for p in body.data.polygons]
bvh=BVHTree.FromPolygons(bverts,bpolys)
group_names={g.index:g.name for g in body.vertex_groups}
body_weights=[{group_names[g.group]:g.weight for g in v.groups if g.weight>0} for v in body.data.vertices]
cyan=bpy.data.materials['V11 cyan conductor light']


def skin_to_body(obj,rigid_bone=None):
    obj.parent=rig
    obj.matrix_parent_inverse=rig.matrix_world.inverted()
    if rigid_bone:
        obj.vertex_groups.new(name=rigid_bone).add([v.index for v in obj.data.vertices],1.0,'REPLACE')
        obj.modifiers.new('Armature','ARMATURE').object=rig
        return
    groups={}
    for v in obj.data.vertices:
        _,_,pi,_=bvh.find_nearest(v.co)
        ids=bpolys[pi]
        inv=[1/max((bverts[i]-v.co).length,1e-5)**2 for i in ids]
        total=sum(inv);blended={}
        for i,f in zip(ids,inv):
            for g,w in body_weights[i].items():
                blended[g]=blended.get(g,0)+w*f/total
        norm=sum(blended.values()) or 1
        for g,w in blended.items():
            if g not in groups:groups[g]=obj.vertex_groups.new(name=g)
            groups[g].add([v.index],w/norm,'REPLACE')
    obj.modifiers.new('Armature','ARMATURE').object=rig


def ribbon(name,pts,width,rigid_bone,lift=.0018,max_jump=.02):
    clean=[]
    for p in pts:
        if p is None:continue
        if clean and (p[0]-clean[-1][0]).length>max_jump:continue
        clean.append(p)
    if len(clean)<2:
        report.setdefault('skipped',[]).append(name)
        return None
    verts=[];faces=[]
    for i,(p,n) in enumerate(clean):
        a=clean[max(i-1,0)][0];b=clean[min(i+1,len(clean)-1)][0]
        side=n.cross((b-a).normalized()).normalized()*width/2
        base=p+n*lift
        verts+=[base-side,base+side]
    for i in range(len(clean)-1):
        faces.append((i*2,i*2+1,i*2+3,i*2+2))
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(v) for v in verts],[],faces)
    mesh.materials.append(cyan)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    scene.collection.objects.link(obj)
    skin_to_body(obj,rigid_bone)
    obj[TAG]='Concept forearm conductor; visual study'
    return obj


def surface_along(bone,t0,t1,outward,steps=16,reach=.12):
    head=rig.matrix_world@bone.head_local
    tail=rig.matrix_world@bone.tail_local
    axis=(tail-head).normalized()
    out=(outward-axis*outward.dot(axis)).normalized()
    pts=[]
    for i in range(steps+1):
        c=head.lerp(tail,t0+(t1-t0)*i/steps)
        loc,normal,_,_=bvh.ray_cast(c+out*reach,-out,reach*1.2)
        pts.append((loc,normal) if loc is not None and normal.dot(out)>.2 else None)
    return pts


made=0
for side in ('L','R'):
    sx=1 if side=='L' else -1
    fore=rig.data.bones[f'{side} forearm']
    # Outer-front face of the forearm armour, matching the long conductor on the concept arms.
    # Stops short of the elbow, where forearm armour weights blend into the upper arm.
    if ribbon(f'{side} ARM | V15 outer forearm conductor',surface_along(fore,.40,.82,Vector((sx,-.55,0))),.0068,None):made+=1
report['changes'].append(f'{made} outer forearm cyan conductors')

plate=bpy.data.materials['V12 ivory ceramic plate'].node_tree
ramp=next(n for n in plate.nodes if n.bl_idname=='ShaderNodeValToRGB')
ramp.color_ramp.elements[0].color=(.52,.455,.345,1)
ramp.color_ramp.elements[1].color=(.60,.535,.415,1)
report['changes'].append('Pauldron ceramic toned down about 7% to sit with the tinted body ivory')

try:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    for backend in ('OPTIX','CUDA','HIP','ONEAPI'):
        try:
            prefs.compute_device_type=backend
            prefs.get_devices()
            if any(d.type!='CPU' for d in prefs.devices):
                for d in prefs.devices:d.use=d.type!='CPU'
                scene.cycles.device='GPU'
                report['render_device']=backend
                break
        except TypeError:
            continue
except Exception as exc:
    report['render_device_error']=str(exc)

rig.data.pose_position='POSE'
rig.animation_data.action=bpy.data.actions['HS_v10 | Idle - contained storm']
scene.frame_set(1)
scene[f'{TAG}_fidelity']='; '.join(report['changes'])
bpy.ops.wm.save_as_mainfile(filepath=str(OUT_BLEND))
print('SAVED',OUT_BLEND,flush=True)

backdrop=bpy.data.objects['V11 bust backdrop']
prefix=f'hollow-saint-hybrid-{TAG}'
shots=[('front','Front orthographic',1100,1400),('hero','Hero three quarter',1100,1400),
       ('side','Side orthographic',1100,1400),('back','Back orthographic',1100,1400),
       ('gameplay-distance','Simulated gameplay distance',1500,1000),('bust','V11 bust detail',1100,1100)]
for suffix,cam,w,h in shots:
    scene.camera=bpy.data.objects[cam]
    scene.render.resolution_x,scene.render.resolution_y=w,h
    scene.render.filepath=str(OUT_DIR/f'{prefix}-{suffix}.png')
    backdrop.hide_render=suffix!='bust'
    bpy.ops.render.render(write_still=True)
    print('RENDER',suffix,flush=True)
backdrop.hide_render=True
scene.camera=bpy.data.objects['Hero three quarter']
scene.render.resolution_x,scene.render.resolution_y=1100,1400
for action,frame,label in (('HS_v10 | Arc Bolt - two finger snap',9,'pose-arc-bolt'),('HS_v10 | Arc Step - in place dash',9,'pose-arc-step')):
    rig.animation_data.action=bpy.data.actions[action]
    scene.frame_set(frame)
    scene.render.filepath=str(OUT_DIR/f'{prefix}-{label}.png')
    bpy.ops.render.render(write_still=True)
    print('RENDER',label,flush=True)
report['status']='Visual fidelity study; renders are Blender studio evidence only, no game test'
(OUT_DIR/'metrics.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('DONE',flush=True)
