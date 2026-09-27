"""Hybrid v16: animation-ready rig upgrade on the v15 visual checkpoint.

- Refit the leg chain to the digitigrade leg mesh (hip, knee, ankle, ball) and add toe bones.
- Leg IK and arm IK controls with pole targets; constraints ship at influence 0 so FK clips are unchanged.
- Dedicated pauldron bones (40% of upper-arm rotation), tabard front/back bone chains,
  halo root (for lag), and non-deforming sockets for effects.

Legs, pelvis, scapulae and tabard are never animated by the HS_v10 clips, so the body must
deform identically in every existing frame; review_rig_v16.py verifies that.

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/build_rig_v16.py
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

if not bpy.app.background:
    raise RuntimeError('Use isolated --background --factory-startup only')
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'art/hybrid/hollow-saint-hybrid-v15.blend'
OUT_BLEND=ROOT/'art/hybrid/hollow-saint-hybrid-v16.blend'
OUT_DIR=ROOT/'art/hybrid/v16'
OUT_DIR.mkdir(parents=True,exist_ok=True)

bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
rig=bpy.data.objects['Hollow Saint | v8 rig']
body=bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
rig.data.pose_position='REST'
bpy.context.view_layer.update()
report={'source':str(SOURCE.relative_to(ROOT)),'changes':[],'joints':{}}
mw=body.matrix_world
names={g.index:g.name for g in body.vertex_groups}

# ---------------------------------------------------------------- measure leg joints
KNEE_Z,ANKLE_Z=0.70,0.16


def leg_points(side):
    wanted={f'{side} thigh',f'{side} shin',f'{side} foot'}
    return [mw@v.co for v in body.data.vertices if any(names[g.group] in wanted and g.weight>.3 for g in v.groups)]


def centroid(points,z,band=.03):
    sl=[p for p in points if abs(p.z-z)<band]
    return Vector((sum(p.x for p in sl)/len(sl),sum(p.y for p in sl)/len(sl),z))


joints={}
for side in ('L','R'):
    pts=leg_points(side)
    feet=[p for p in pts if p.z<.10]
    foot_x=sum(p.x for p in feet)/len(feet)
    toe_tip=min(feet,key=lambda p:p.y)
    heel_tip=max(feet,key=lambda p:p.y)
    joints[side]={'knee':centroid(pts,KNEE_Z,.04),'ankle':centroid(pts,ANKLE_Z,.03),
                  'ball':Vector((foot_x,-.030,.055)),'toe_tip':Vector((toe_tip.x,toe_tip.y+.01,.02)),
                  'heel_tip':Vector((heel_tip.x,heel_tip.y-.01,.04))}
    report['joints'][side]={k:[round(c,3) for c in v] for k,v in joints[side].items()}

# ---------------------------------------------------------------- edit bones
bpy.context.view_layer.objects.active=rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
eb=rig.data.edit_bones


def new_bone(name,head,tail,parent,deform,roll=0.0):
    b=eb.new(name)
    b.head,b.tail,b.roll=Vector(head),Vector(tail),roll
    b.parent=eb[parent] if parent else None
    b.use_connect=False
    b.use_deform=deform
    return b


for side in ('L','R'):
    j=joints[side]
    eb[f'{side} thigh'].tail=j['knee']
    eb[f'{side} shin'].head=j['knee'];eb[f'{side} shin'].tail=j['ankle']
    foot=eb[f'{side} foot'];foot.head=j['ankle'];foot.tail=j['ball']
    new_bone(f'{side} toe',j['ball'],j['toe_tip'],f'{side} foot',True)
    new_bone(f'{side} heel socket',j['ankle'],j['heel_tip'],f'{side} foot',False)
    for name in (f'{side} thigh',f'{side} shin',f'{side} foot'):
        eb[name].roll=0
    # IK controls share the foot orientation so a world-space rotation copy is identity at rest.
    ik=new_bone(f'{side} foot IK',j['ankle'],j['ankle']+(j['ball']-j['ankle']),'root',False,foot.roll)
    new_bone(f'{side} knee pole',j['knee']+Vector((0,-.45,0)),j['knee']+Vector((0,-.55,0)),'root',False)
    fore=eb[f'{side} forearm'];up=eb[f'{side} upperarm'];hand=eb[f'{side} hand']
    new_bone(f'{side} hand IK',hand.head,hand.tail,'root',False,hand.roll)
    new_bone(f'{side} elbow pole',fore.head+Vector((0,.45,0)),fore.head+Vector((0,.55,0)),'root',False)
    # Pauldron pivots at the shoulder joint and shares the upper-arm axes so local rotation copies line up.
    direction=(up.tail-up.head).normalized()
    new_bone(f'{side} pauldron',up.head,up.head+direction*.12,f'{side} scapula',True,up.roll)
    tip=eb[f'{side} index.3']
    tip_dir=(tip.tail-tip.head).normalized()
    new_bone(f'{side} muzzle',tip.tail,tip.tail+tip_dir*.04,f'{side} index.3',False)

TAB_FRONT=(-0.021,-0.095);TAB_BACK=(-0.038,0.200)
chains={'tabard front':(TAB_FRONT,(1.12,0.86,0.60,0.31)),'tabard back':(TAB_BACK,(1.12,0.84,0.44))}
for prefix,((x,y),zs) in chains.items():
    parent='pelvis'
    for i in range(len(zs)-1):
        name=f'{prefix}.{i+1}'
        new_bone(name,(x,y,zs[i]),(x,y,zs[i+1]),parent,True)
        parent=name
HALO_C=Vector((-0.0375,0.15,1.79))
new_bone('halo root',HALO_C,HALO_C+Vector((0,0,.12)),'chest',False)
for i in (1,2,3,4):
    eb[f'halo {i}'].parent=eb['halo root']
new_bone('halo socket',HALO_C,HALO_C+Vector((0,.08,0)),'halo root',False)
new_bone('core socket',(-0.04,-0.115,1.51),(-0.04,-0.195,1.51),'chest',False)
new_bone('head socket',(0,-0.02,1.84),(0,-0.02,1.92),'head',False)
bpy.ops.object.mode_set(mode='OBJECT')
report['changes'].append('Leg chain refit to the digitigrade mesh (knee/ankle measured from cross-sections), toe bones, heel sockets')

colls={}
for cname,members in (('IK controls',[n for n in rig.data.bones.keys() if n.endswith((' IK',' pole'))]),
                      ('Sockets',[n for n in rig.data.bones.keys() if 'socket' in n or 'muzzle' in n]),
                      ('Secondary',[n for n in rig.data.bones.keys() if n.startswith(('tabard','halo')) or 'pauldron' in n])):
    c=rig.data.collections.get(cname) or rig.data.collections.new(cname)
    for n in members:
        c.assign(rig.data.bones[n])
    colls[cname]=members
report['bone_collections']={k:len(v) for k,v in colls.items()}

# ---------------------------------------------------------------- constraints (influence 0 = FK clips unchanged)
pb=rig.pose.bones


def solve_pole(chain_bone,constraint):
    """Pick the pole angle that leaves the chain at rest when IK is fully on; return (angle, residual)."""
    ref_mid=(rig.matrix_world@pb[chain_bone].matrix).translation.copy()
    ref_tail=(rig.matrix_world@pb[chain_bone].matrix@Vector((0,pb[chain_bone].length,0)))
    constraint.influence=1
    best=(1e9,0)
    def err(a):
        constraint.pole_angle=math.radians(a)
        bpy.context.view_layer.update()
        return ((rig.matrix_world@pb[chain_bone].matrix).translation-ref_mid).length
    for a in range(-180,180,5):
        e=err(a)
        if e<best[0]:best=(e,a)
    lo=best[1]
    for a in [lo+d*.25 for d in range(-20,21)]:
        e=err(a)
        if e<best[0]:best=(e,a)
    constraint.pole_angle=math.radians(best[1])
    bpy.context.view_layer.update()
    tail=(rig.matrix_world@pb[chain_bone].matrix@Vector((0,pb[chain_bone].length,0)))
    residual=max(best[0],(tail-ref_tail).length)
    constraint.influence=0
    bpy.context.view_layer.update()
    return best[1],residual


rig.data.pose_position='POSE'
saved_action=rig.animation_data.action
rig.animation_data.action=None
for p in pb:
    p.location=(0,0,0);p.rotation_quaternion=(1,0,0,0);p.rotation_euler=(0,0,0);p.scale=(1,1,1)
bpy.context.view_layer.update()
ik_report={}
for side in ('L','R'):
    for chain,target,pole in ((f'{side} shin',f'{side} foot IK',f'{side} knee pole'),(f'{side} forearm',f'{side} hand IK',f'{side} elbow pole')):
        c=pb[chain].constraints.new('IK')
        c.name='IK (enable for IK posing)'
        c.target=rig;c.subtarget=target
        c.pole_target=rig;c.pole_subtarget=pole
        c.chain_count=2
        angle,residual=solve_pole(chain,c)
        ik_report[chain]={'pole_angle_deg':angle,'rest_residual_m':round(residual,5)}
    for owner,target in ((f'{side} foot',f'{side} foot IK'),(f'{side} hand',f'{side} hand IK')):
        c=pb[owner].constraints.new('COPY_ROTATION')
        c.name='IK rotation (enable with IK)'
        c.target=rig;c.subtarget=target
        c.influence=0
    c=pb[f'{side} pauldron'].constraints.new('COPY_ROTATION')
    c.name='Follow upper arm 40%'
    c.target=rig;c.subtarget=f'{side} upperarm'
    c.owner_space='LOCAL';c.target_space='LOCAL';c.mix_mode='REPLACE'
    c.influence=.40
report['ik']=ik_report
rig.animation_data.action=saved_action
report['changes'].append('Leg/arm IK with solved pole angles, IK rotation copies (all influence 0), pauldron follows 40% of upper-arm rotation')

# ---------------------------------------------------------------- body weights
new_groups=[f'{s} toe' for s in 'LR']+[f'{p}.{i+1}' for p,(_,zs) in chains.items() for i in range(len(zs)-1)]
for n in new_groups:
    if n not in body.vertex_groups:body.vertex_groups.new(name=n)
vg={g.name:g for g in body.vertex_groups}


def smooth(x,a,b):
    t=min(1,max(0,(x-a)/(b-a)))
    return t*t*(3-2*t)


leg_changed=0
for v in body.data.vertices:
    w={body.vertex_groups[g.group].name:g.weight for g in v.groups}
    for side in ('L','R'):
        keys=[f'{side} thigh',f'{side} shin',f'{side} foot']
        share=sum(w.get(k,0) for k in keys)
        if share<=0:continue
        j=joints[side]
        p=mw@v.co
        up=smooth(p.z,KNEE_Z-.04,KNEE_Z+.04)
        low=smooth(p.z,ANKLE_Z-.03,ANKLE_Z+.03)
        toe=1-smooth(p.y,j['ball'].y-.025,j['ball'].y+.010) if p.z<ANKLE_Z else 0
        new={f'{side} thigh':up,f'{side} shin':(1-up)*low,f'{side} foot':(1-up)*(1-low)*(1-toe),f'{side} toe':(1-up)*(1-low)*toe}
        for k,val in new.items():
            if val*share>1e-4:
                vg[k].add([v.index],val*share,'REPLACE')
            elif k in vg:
                vg[k].remove([v.index])
        leg_changed+=1
tab_counts={}
# The tabard is welded into the body mesh; its rear face (y ~ -0.053) was skinned to the legs, so select by
# slab bounds and replace all weights. Thigh inner surfaces start ~5 mm outside the front slab's x range.
for v in body.data.vertices:
    p=mw@v.co
    if -0.123<p.x<0.078 and 0.30<p.z<1.10 and p.y<-0.045:
        prefix,zs='tabard front',chains['tabard front'][1]
    elif -0.132<p.x<0.055 and 0.44<p.z<1.10 and p.y>0.14:
        prefix,zs='tabard back',chains['tabard back'][1]
    else:
        continue
    for g in list(v.groups):
        body.vertex_groups[g.group].remove([v.index])
    # Stay on the pelvis at the waistband, then hand off down the chain.
    pelvis=smooth(p.z,zs[0]-.10,zs[0]-.02)
    blend={'pelvis':pelvis}
    n=len(zs)-1
    for i in range(n):
        above=smooth(p.z,zs[i+1]-.04,zs[i+1]+.04) if i<n-1 else 1.0
        below=1-smooth(p.z,zs[i]-.04,zs[i]+.04) if i>0 else 1.0
        blend[f'{prefix}.{i+1}']=(1-pelvis)*above*below
    total=sum(blend.values())
    for k,val in blend.items():
        if val>1e-4:vg[k].add([v.index],val/total,'REPLACE')
    tab_counts[prefix]=tab_counts.get(prefix,0)+1
report['body_reweight']={'leg_vertices':leg_changed,'tabard_vertices':tab_counts}
report['changes'].append(f'Body re-weighted: {leg_changed} leg vertices to refit chain + toes; tabard vertices {tab_counts} to chain bones')

# ---------------------------------------------------------------- overlays follow new body weights
bverts=[mw@v.co for v in body.data.vertices]
bpolys=[tuple(p.vertices) for p in body.data.polygons]
bvh=BVHTree.FromPolygons(bverts,bpolys)
body_weights=[{body.vertex_groups[g.group].name:g.weight for g in v.groups if g.weight>0} for v in body.data.vertices]
retarget=set(k for k in vg if k.startswith('tabard')) | {f'{s} {b}' for s in 'LR' for b in ('thigh','shin','foot','toe')}
reskinned=[]
for obj in scene.objects:
    if obj==body or obj.type!='MESH' or not any(m.type=='ARMATURE' for m in obj.modifiers):continue
    if 'SHOULDER' in obj.name:continue
    uses=obj.name.startswith('TABARD') or any(g.name in retarget for g in obj.vertex_groups)
    if not uses:continue
    rest_world=obj.matrix_world
    for g in list(obj.vertex_groups):obj.vertex_groups.remove(g)
    groups={}
    for v in obj.data.vertices:
        co=rest_world@v.co
        _,_,pi,_=bvh.find_nearest(co)
        ids=bpolys[pi]
        inv=[1/max((bverts[i]-co).length,1e-5)**2 for i in ids]
        total=sum(inv);blended={}
        for i,f in zip(ids,inv):
            for g,wt in body_weights[i].items():
                blended[g]=blended.get(g,0)+wt*f/total
        norm=sum(blended.values()) or 1
        for g,wt in blended.items():
            if g not in groups:groups[g]=obj.vertex_groups.new(name=g)
            groups[g].add([v.index],wt/norm,'REPLACE')
    reskinned.append(obj.name)
for obj in [o for o in scene.objects if 'SHOULDER' in o.name]:
    side=obj.name[0]
    for g in list(obj.vertex_groups):obj.vertex_groups.remove(g)
    obj.vertex_groups.new(name=f'{side} pauldron').add([v.index for v in obj.data.vertices],1.0,'REPLACE')
    reskinned.append(obj.name)
report['reskinned_objects']=len(reskinned)

# Dense tabard overlays sink into the sparse body tabard under blended bone rotation (Surface Deform
# spiked on the thin slab). Keep armature weights and push only buried vertices back outside the body.
pushed=[]
for obj in [o for o in scene.objects if o.name.startswith('TABARD')]:
    sw=obj.modifiers.new('Stay outside body','SHRINKWRAP')
    sw.target=body
    sw.wrap_method='NEAREST_SURFACEPOINT'
    sw.wrap_mode='OUTSIDE'
    sw.offset=.0015
    pushed.append(obj.name)
report['shrinkwrap_outside']=len(pushed)
report['changes'].append(f'{len(reskinned)} overlays re-skinned (tabard cloth/trim from new body weights, pauldrons rigid to pauldron bones)')

rig['v16_rig']='IK/pole controls and IK rotation copies ship at influence 0; raise to 1 to pose with IK. Bake to deform bones before export.'
rig.data.pose_position='POSE'
rig.animation_data.action=bpy.data.actions['HS_v10 | Idle - contained storm']
scene.frame_set(1)
scene['v16_rig']='; '.join(report['changes'])
bpy.ops.wm.save_as_mainfile(filepath=str(OUT_BLEND))
print('SAVED',OUT_BLEND,flush=True)
(OUT_DIR/'rig-build.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('DONE',flush=True)
