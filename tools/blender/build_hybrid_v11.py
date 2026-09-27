"""Hybrid v11 fidelity pass toward selected LEFT A, built from v10 (rig/actions kept).

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/build_hybrid_v11.py
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree

if not bpy.app.background:
    raise RuntimeError('Use isolated --background --factory-startup only')
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'art/hybrid/hollow-saint-hybrid-v10.blend'
OUT_BLEND=ROOT/'art/hybrid/hollow-saint-hybrid-v11.blend'
OUT_DIR=ROOT/'art/hybrid/v11'
OUT_DIR.mkdir(parents=True,exist_ok=True)

bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
rig=bpy.data.objects['Hollow Saint | v8 rig']
body=bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
rig.data.pose_position='REST'
bpy.context.view_layer.update()

CX=-0.0375
HALO_Z=1.79
HALO_INNER,HALO_OUTER=.320,.433
NEW=[]
report={'source':str(SOURCE.relative_to(ROOT)),'changes':[]}


def principled(name,base,metal=0.0,rough=0.5,emit=None,strength=0.0):
    mat=bpy.data.materials.new(name)
    mat.use_nodes=True
    p=mat.node_tree.nodes['Principled BSDF']
    p.inputs['Base Color'].default_value=(*base,1)
    p.inputs['Metallic'].default_value=metal
    p.inputs['Roughness'].default_value=rough
    if emit:
        p.inputs['Emission Color'].default_value=(*emit,1)
        p.inputs['Emission Strength'].default_value=strength
    return mat


cyan_line=principled('V11 cyan conductor light',(.35,.9,1.0),0,.3,(.10,.82,1.0),6.0)
cyan_hot=principled('V11 cyan core hot',(.8,1,1),0,.2,(.55,.95,1.0),14.0)
cyan_gap=principled('V11 halo gap light',(.2,.8,1),0,.25,(.05,.72,1.0),5.0)
cyan_gap_dim=principled('V11 halo top gap light',(.2,.8,1),0,.25,(.05,.72,1.0),2.2)
cloth=principled('V11 tabard cloth',(.030,.028,.027),0,.86)
trim=principled('V11 tabard copper trim',(.40,.17,.055),.65,.38)

# Existing cyan accents (wrist, spine, v9 bands) were dim at strength 2.
old_cyan=bpy.data.materials['HYBRID cyan conductor']
old_cyan.node_tree.nodes['Principled BSDF'].inputs['Emission Strength'].default_value=5.0
report['changes'].append('Existing cyan conductor emission 2 -> 5')


def aged_copper():
    mat=bpy.data.materials.new('V11 aged copper blocks')
    mat.use_nodes=True
    nt=mat.node_tree
    n=nt.nodes
    l=nt.links
    p=n['Principled BSDF']
    uv=n.new('ShaderNodeUVMap');uv.uv_map='HaloUV'
    sep=n.new('ShaderNodeSeparateXYZ');l.new(uv.outputs['UV'],sep.inputs[0])
    fract=n.new('ShaderNodeMath');fract.operation='FRACT';l.new(sep.outputs['X'],fract.inputs[0])
    inv=n.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1
    l.new(fract.outputs[0],inv.inputs[1])
    near=n.new('ShaderNodeMath');near.operation='MINIMUM'
    l.new(fract.outputs[0],near.inputs[0]);l.new(inv.outputs[0],near.inputs[1])
    groove=n.new('ShaderNodeMapRange');groove.inputs['From Min'].default_value=0
    groove.inputs['From Max'].default_value=.045;groove.inputs['To Min'].default_value=1
    groove.inputs['To Max'].default_value=0;l.new(near.outputs[0],groove.inputs['Value'])
    vinv=n.new('ShaderNodeMath');vinv.operation='SUBTRACT';vinv.inputs[0].default_value=1
    l.new(sep.outputs['Y'],vinv.inputs[1])
    vnear=n.new('ShaderNodeMath');vnear.operation='MINIMUM'
    l.new(sep.outputs['Y'],vnear.inputs[0]);l.new(vinv.outputs[0],vnear.inputs[1])
    edge=n.new('ShaderNodeMapRange');edge.inputs['From Min'].default_value=0
    edge.inputs['From Max'].default_value=.10;edge.inputs['To Min'].default_value=.75
    edge.inputs['To Max'].default_value=0;l.new(vnear.outputs[0],edge.inputs['Value'])
    tex=n.new('ShaderNodeTexCoord')
    noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=38
    noise.inputs['Detail'].default_value=6;l.new(tex.outputs['Object'],noise.inputs['Vector'])
    mott=n.new('ShaderNodeMapRange');mott.inputs['From Min'].default_value=.38
    mott.inputs['From Max'].default_value=.68;mott.inputs['To Min'].default_value=0
    mott.inputs['To Max'].default_value=.55;l.new(noise.outputs['Fac'],mott.inputs['Value'])
    dirt=n.new('ShaderNodeMath');dirt.operation='MAXIMUM'
    l.new(groove.outputs[0],dirt.inputs[0]);l.new(edge.outputs[0],dirt.inputs[1])
    dirt2=n.new('ShaderNodeMath');dirt2.operation='MAXIMUM';dirt2.use_clamp=True
    l.new(dirt.outputs[0],dirt2.inputs[0]);l.new(mott.outputs[0],dirt2.inputs[1])
    mix=n.new('ShaderNodeMix');mix.data_type='RGBA'
    mix.inputs[6].default_value=(.38,.15,.05,1)
    mix.inputs[7].default_value=(.10,.04,.018,1)
    l.new(dirt2.outputs[0],mix.inputs['Factor']);l.new(mix.outputs[2],p.inputs['Base Color'])
    rough=n.new('ShaderNodeMapRange');rough.inputs['To Min'].default_value=.42
    rough.inputs['To Max'].default_value=.78;l.new(dirt2.outputs[0],rough.inputs['Value'])
    l.new(rough.outputs[0],p.inputs['Roughness'])
    p.inputs['Metallic'].default_value=.35
    height=n.new('ShaderNodeMath');height.operation='SUBTRACT';height.inputs[0].default_value=1
    l.new(groove.outputs[0],height.inputs[1])
    bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.45
    bump.inputs['Distance'].default_value=.004
    l.new(height.outputs[0],bump.inputs['Height']);l.new(bump.outputs['Normal'],p.inputs['Normal'])
    return mat


copper=aged_copper()
copper_edge=bpy.data.materials['HYBRID copper edge']
cp=copper_edge.node_tree.nodes['Principled BSDF']
cp.inputs['Base Color'].default_value=(.16,.07,.03,1)
cp.inputs['Metallic'].default_value=.5
cp.inputs['Roughness'].default_value=.6

# ---------------------------------------------------------------- halo
halo_objects=[o for o in scene.objects if o.parent==rig and o.parent_bone.startswith('halo')]
arcs=[o for o in halo_objects if o.name.startswith('HALO | independent copper arc')]
arc_world=[o.matrix_world@v.co for o in arcs for v in o.data.vertices]
halo_y=sum(v.y for v in arc_world)/len(arc_world)
old_depth=max(v.y for v in arc_world)-min(v.y for v in arc_world)
DEPTH_SCALE=2.1


def scale_y_world(obj,points_getter):
    mw=obj.matrix_world
    inv=mw.inverted()
    for get,setp in points_getter(obj):
        w=mw@get()
        w.y=halo_y+(w.y-halo_y)*DEPTH_SCALE
        setp(inv@w)


def mesh_points(obj):
    for v in obj.data.vertices:
        yield (lambda v=v:v.co.copy()),(lambda c,v=v:setattr(v,'co',c))


def curve_points(obj):
    for spline in obj.data.splines:
        for bp in spline.bezier_points:
            for attr in ('co','handle_left','handle_right'):
                yield (lambda bp=bp,a=attr:getattr(bp,a).copy()),(lambda c,bp=bp,a=attr:setattr(bp,a,c))
        for pt in spline.points:
            yield (lambda pt=pt:Vector(pt.co[:3])),(lambda c,pt=pt:setattr(pt,'co',(*c,pt.co[3])))


for obj in halo_objects:
    if obj.type=='MESH':
        scale_y_world(obj,mesh_points)
        obj.data.update()
    elif obj.type=='CURVE':
        scale_y_world(obj,curve_points)
BLOCKS_PER_ARC=3
for arc in arcs:
    arc.data.materials[0]=copper
    uvl=arc.data.uv_layers.new(name='HaloUV')
    arc.data.uv_layers.active=uvl
    mw=arc.matrix_world
    angs=[math.degrees(math.atan2((mw@v.co).x-CX,(mw@v.co).z-HALO_Z))%360 for v in arc.data.vertices]
    a0,a1=min(angs),max(angs)
    for loop in arc.data.loops:
        w=mw@arc.data.vertices[loop.vertex_index].co
        ang=math.degrees(math.atan2(w.x-CX,w.z-HALO_Z))%360
        r=math.hypot(w.x-CX,w.z-HALO_Z)
        u=min(BLOCKS_PER_ARC-.0001,max(.0001,(ang-a0)/(a1-a0)*BLOCKS_PER_ARC))
        uvl.data[loop.index].uv=(u,min(1,max(0,(r-HALO_INNER)/(HALO_OUTER-HALO_INNER))))
    for mod in arc.modifiers:
        if mod.type=='BEVEL':
            mod.width=max(mod.width,.006)
            mod.segments=max(mod.segments,3)
report['changes'].append(f'Halo depth x{DEPTH_SCALE} ({old_depth:.3f} -> {old_depth*DEPTH_SCALE:.3f} m), aged copper block shader with seams, darker lips')

arc_ranges={}
for arc in arcs:
    angs=[math.degrees(math.atan2((arc.matrix_world@v.co).x-CX,(arc.matrix_world@v.co).z-HALO_Z))%360 for v in arc.data.vertices]
    arc_ranges[arc.parent_bone]=(min(angs),max(angs))
report['arc_angle_ranges']={k:[round(a,1),round(b,1)] for k,(a,b) in arc_ranges.items()}


def bone_parent(obj,bone):
    world=obj.matrix_world.copy()
    obj.parent=rig
    obj.parent_type='BONE'
    obj.parent_bone=bone
    obj.matrix_world=world


def link(obj):
    scene.collection.objects.link(obj)
    NEW.append(obj)
    return obj


def gap_glow(name,a0,a1,bone,depth_frac=.7,width_frac=.38,material=None,r_inset=.014):
    mid,half_w=(a0+a1)/2,(a1-a0)*width_frac/2
    a0,a1=math.radians(mid-half_w),math.radians(mid+half_w)
    r0,r1=HALO_INNER+r_inset,HALO_OUTER-r_inset
    half=old_depth*DEPTH_SCALE*depth_frac/2
    steps=4
    verts=[]
    for y in (halo_y-half,halo_y+half):
        for i in range(steps+1):
            a=a0+(a1-a0)*i/steps
            for r in (r0,r1):
                verts.append((CX+r*math.sin(a),y,HALO_Z+r*math.cos(a)))
    per=(steps+1)*2
    faces=[]
    for side in (0,per):
        for i in range(steps):
            q=[side+i*2,side+i*2+1,side+i*2+3,side+i*2+2]
            faces.append(q if side else q[::-1])
    for i in range(steps):
        for k in (0,1):
            a,b=i*2+k,(i+1)*2+k
            faces.append((a,b,b+per,a+per) if k else (b,a,a+per,b+per))
    faces.append((0,1,1+per,per));faces.append((per-1,per-2,2*per-2,2*per-1))
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(verts,[],faces)
    mesh.materials.append(material or cyan_gap)
    mesh.update()
    obj=link(bpy.data.objects.new(name,mesh))
    bone_parent(obj,bone)
    return obj


h1,h2,h3,h4=(arc_ranges[b] for b in ('halo 1','halo 2','halo 3','halo 4'))
gap_glow('HALO | V11 gap light right',h1[1],h2[0],'halo 2')
gap_glow('HALO | V11 gap light left',h3[1],h4[0],'halo 3')
if h4[1]>h1[0]:
    gap_glow('HALO | V11 gap light top',h4[1]-360,h1[0],'halo 1',.45,.22,cyan_gap_dim)
report['changes'].append('Thin emissive cyan seams centred in the side gaps, dim top seam (LEFT A detail sheet)')

# v9 shoulder bands were sized for the thin halo and now protrude; rebuild as flush inlays.
for name in ('HALO | HF-style cyan shoulder band L','HALO | HF-style cyan shoulder band R'):
    old=bpy.data.objects.get(name)
    if old:bpy.data.objects.remove(old,do_unlink=True)
for label,angle,bone in (('L',104,'halo 2'),('R',256,'halo 3')):
    gap_glow(f'HALO | V11 shoulder inlay band {label}',angle-1.1,angle+1.1,bone,
             depth_frac=1.06,width_frac=1.0,material=old_cyan,r_inset=.006)
report['changes'].append('v9 shoulder bands kept as flush inlays through the thickened halo')

# ---------------------------------------------------------------- surface decals
bm_verts=[body.matrix_world@v.co for v in body.data.vertices]
bm_polys=[tuple(p.vertices) for p in body.data.polygons]
bvh=BVHTree.FromPolygons(bm_verts,bm_polys)
kd=KDTree(len(bm_verts))
for i,co in enumerate(bm_verts):
    kd.insert(co,i)
kd.balance()
group_names={g.index:g.name for g in body.vertex_groups}
body_weights=[{group_names[g.group]:g.weight for g in v.groups if g.weight>0} for v in body.data.vertices]
body_arm=next(m for m in body.modifiers if m.type=='ARMATURE')


def skin_to_body(obj):
    obj.parent=rig
    obj.parent_type='OBJECT'
    obj.matrix_parent_inverse=rig.matrix_world.inverted()
    groups={}
    for v in obj.data.vertices:
        blended={}
        _,_,poly_index,_=bvh.find_nearest(v.co)
        ids=bm_polys[poly_index] if poly_index is not None else (kd.find(v.co)[1],)
        inv=[1/max((bm_verts[i]-v.co).length,1e-5)**2 for i in ids]
        total=sum(inv)
        for i,f in zip(ids,inv):
            for gname,w in body_weights[i].items():
                blended[gname]=blended.get(gname,0)+w*f/total
        norm=sum(blended.values()) or 1
        for gname,w in blended.items():
            if gname not in groups:
                groups[gname]=obj.vertex_groups.new(name=gname)
            groups[gname].add([v.index],w/norm,'REPLACE')
    mod=obj.modifiers.new('Armature','ARMATURE')
    mod.object=rig
    mod.use_deform_preserve_volume=body_arm.use_deform_preserve_volume


def cast(origin,direction):
    loc,normal,_,_=bvh.ray_cast(Vector(origin),Vector(direction).normalized(),5)
    return (loc,normal) if loc is not None else None


def ribbon(name,samples,width,material,lift=.0018,closed=False,max_jump=.025):
    pts=[]
    for s in samples:
        if s is None:continue
        if pts and (s[0]-pts[-1][0]).length>max_jump:continue
        pts.append(s)
    if len(pts)<2:
        report.setdefault('skipped',[]).append(name)
        return None
    verts=[];faces=[]
    n=len(pts)
    for i,(p,nrm) in enumerate(pts):
        a=pts[(i-1)%n][0] if (closed or i>0) else p
        b=pts[(i+1)%n][0] if (closed or i<n-1) else p
        t=(b-a).normalized()
        side=nrm.cross(t).normalized()*width/2
        base=p+nrm*lift
        verts+= [base-side,base+side]
    for i in range(n-1 if not closed else n):
        j=(i+1)%n
        faces.append((i*2,i*2+1,j*2+1,j*2))
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(v) for v in verts],[],faces)
    mesh.materials.append(material)
    mesh.update()
    obj=link(bpy.data.objects.new(name,mesh))
    skin_to_body(obj)
    return obj


def front_line(p0,p1,steps=24):
    return [cast((p0[0]+(p1[0]-p0[0])*i/steps,-2,p0[1]+(p1[1]-p0[1])*i/steps),(0,1,0)) for i in range(steps+1)]


def back_line(p0,p1,steps=24):
    return [cast((p0[0]+(p1[0]-p0[0])*i/steps,2,p0[1]+(p1[1]-p0[1])*i/steps),(0,-1,0)) for i in range(steps+1)]


def disc(name,center,normal,radius,material,lift=.002,segments=20):
    normal=normal.normalized()
    u=normal.orthogonal().normalized()
    w=normal.cross(u)
    c=center+normal*lift
    verts=[tuple(c)]+[tuple(c+(u*math.cos(2*math.pi*i/segments)+w*math.sin(2*math.pi*i/segments))*radius) for i in range(segments)]
    faces=[(0,1+i,1+(i+1)%segments) for i in range(segments)]
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(verts,[],faces)
    mesh.materials.append(material)
    mesh.update()
    obj=link(bpy.data.objects.new(name,mesh))
    skin_to_body(obj)
    return obj


# Mask centre stripe: front of face, over the crown, toward the back of the head.
head_c=Vector((-0.04,-0.02,1.86))
stripe=[]
for i in range(41):
    phi=math.radians(-32+132*i/40)
    d=Vector((0,-math.cos(phi),math.sin(phi)))
    stripe.append(cast(head_c+d*.4,-d))
ribbon('MASK | V11 cyan centre stripe',stripe,.011,cyan_line,lift=.0015)
for side in (1,-1):
    hit=cast((CX+side*.4,-0.045,1.845),(-side,0,0))
    if hit:disc(f'MASK | V11 temple node {"L" if side>0 else "R"}',hit[0],hit[1],.0075,cyan_line)

# Chest core: hot disc and a ring.
core=Vector((-0.04,0,1.51))
hit=cast((core.x,-2,core.z),(0,1,0))
disc('CHEST | V11 core hot centre',hit[0],hit[1],.030,cyan_hot,lift=.003,segments=28)
ring=[cast((core.x+.060*math.sin(2*math.pi*i/48),-2,core.z+.060*math.cos(2*math.pi*i/48)),(0,1,0)) for i in range(48)]
ribbon('CHEST | V11 core ring',ring,.010,cyan_line,lift=.0025,closed=True,max_jump=.03)

front_paths={
    'NECK | V11 centre conductor':((-0.04,1.585),(-0.04,1.705)),
    'NECK | V11 conductor L':((-0.005,1.60),(0.004,1.705)),
    'NECK | V11 conductor R':((-0.075,1.60),(-0.084,1.705)),
    'ABDOMEN | V11 centre conductor':((-0.04,1.445),(-0.04,1.25)),
    'CHEST | V11 lower branch L':((0.004,1.468),(0.036,1.385)),
    'CHEST | V11 lower branch R':((-0.084,1.468),(-0.116,1.385)),
    'ABDOMEN | V11 side dash L':((0.006,1.335),(0.006,1.245)),
    'ABDOMEN | V11 side dash R':((-0.086,1.335),(-0.086,1.245)),
}
for name,(a,b) in front_paths.items():
    ribbon(name,front_line(a,b),.0075,cyan_line)
back_paths={
    'BACK | V11 upper spine conductor':((-0.038,1.575),(-0.038,1.70)),
    'BACK | V11 lower spine conductor':((-0.038,1.335),(-0.038,1.215)),
}
for name,(a,b) in back_paths.items():
    ribbon(name,back_line(a,b),.0075,cyan_line)
report['changes'].append('Geometric emissive mask stripe, temple nodes, core hot centre/ring, neck/chest/abdomen and spine conductors (weighted to rig)')

# ---------------------------------------------------------------- tabard
def panel(name,x0,x1,z0,z1,side,material,lift,nx=10,nz=36,fallback_y=None):
    origin_y,direction=(-2,(0,1,0)) if side=='front' else (2,(0,-1,0))
    sign=-1 if side=='front' else 1
    verts=[]
    for j in range(nz+1):
        z=z1+(z0-z1)*j/nz
        for i in range(nx+1):
            x=x0+(x1-x0)*i/nx
            hit=cast((x,origin_y,z),direction)
            y=hit[0].y if hit and (fallback_y is None or abs(hit[0].y-fallback_y)<.02) else fallback_y
            verts.append((x,y+sign*lift,z))
    faces=[]
    for j in range(nz):
        for i in range(nx):
            a=j*(nx+1)+i
            q=(a,a+1,a+nx+2,a+nx+1)
            faces.append(q if side=='front' else q[::-1])
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(verts,[],faces)
    mesh.materials.append(material)
    for poly in mesh.polygons:poly.use_smooth=True
    mesh.update()
    obj=link(bpy.data.objects.new(name,mesh))
    skin_to_body(obj)
    return obj


def trim_frame(prefix,x0,x1,z0,z1,side,fy,inset=.011,w=.0065,lift=.0055):
    panel(prefix+' trim top',x0+inset,x1-inset,z1-inset-w,z1-inset,side,trim,lift,4,1,fy)
    panel(prefix+' trim bottom',x0+inset,x1-inset,z0+inset,z0+inset+w,side,trim,lift,4,1,fy)
    panel(prefix+' trim left',x0+inset,x0+inset+w,z0+inset,z1-inset,side,trim,lift,1,20,fy)
    panel(prefix+' trim right',x1-inset-w,x1-inset,z0+inset,z1-inset,side,trim,lift,1,20,fy)


def sigil(prefix,cx,cz,side,fy,z_top,z_bottom,r=.030,w=.0055,lift=.0058):
    origin_y,direction=(-2,(0,1,0)) if side=='front' else (2,(0,-1,0))
    sign=-1 if side=='front' else 1
    pts=[]
    for i in range(40):
        a=2*math.pi*i/40
        x,z=cx+r*math.sin(a),cz+r*math.cos(a)
        hit=cast((x,origin_y,z),direction)
        y=hit[0].y if hit and abs(hit[0].y-fy)<.02 else fy
        pts.append((Vector((x,y,z)),Vector((0,sign,0))))
    ribbon(prefix+' sigil ring',pts,w,trim,lift=lift,closed=True,max_jump=.05)
    panel(prefix+' sigil stem upper',cx-w/2,cx+w/2,cz+r+.006,z_top,side,trim,lift,1,6,fy)
    panel(prefix+' sigil stem lower',cx-w/2,cx+w/2,z_bottom,cz-r-.006,side,trim,lift,1,16,fy)


def panel_edges(side,plane_y,x_mid,zs,tol=.008):
    origin_y,direction=(-2,(0,1,0)) if side=='front' else (2,(0,-1,0))
    def on_panel(x,z):
        hit=cast((x,origin_y,z),direction)
        return hit is not None and abs(hit[0].y-plane_y)<tol
    lo,hi=[],[]
    for z in zs:
        for sign,store in ((-1,lo),(1,hi)):
            x=x_mid
            while on_panel(x+sign*.001,z) and abs(x-x_mid)<.2:
                x+=sign*.001
            store.append(x)
    return min(lo)-.001,max(hi)+.001


FRONT_Y,BACK_Y=-0.0855,0.1950
fx0,fx1=panel_edges('front',FRONT_Y,-0.02,(0.55,0.75,0.95,1.08))
tx0,tx1=panel_edges('front',-0.0872,-0.02,(0.34,0.40))
bx0,bx1=panel_edges('back',BACK_Y,-0.035,(0.55,0.75,0.95,1.08))
report['tabard_edges']={'front':[round(fx0,4),round(fx1,4)],'front_lower_tab':[round(tx0,4),round(tx1,4)],'back':[round(bx0,4),round(bx1,4)]}
panel('TABARD | V11 front cloth',fx0,fx1,0.455,1.128,'front',cloth,.0035,fallback_y=FRONT_Y)
panel('TABARD | V11 front lower tab',tx0,tx1,0.318,0.462,'front',cloth,.0036,6,8,fallback_y=-0.0872)
trim_frame('TABARD | V11 front',fx0,fx1,0.455,1.128,'front',FRONT_Y)
trim_frame('TABARD | V11 front lower tab',tx0,tx1,0.318,0.462,'front',-0.0872,inset=.009)
sigil('TABARD | V11 front',(fx0+fx1)/2,0.93,'front',FRONT_Y,1.07,0.56)
panel('TABARD | V11 back cloth',bx0,bx1,0.450,1.130,'back',cloth,.0035,fallback_y=BACK_Y)
trim_frame('TABARD | V11 back',bx0,bx1,0.450,1.130,'back',BACK_Y)
sigil('TABARD | V11 back',(bx0+bx1)/2,0.93,'back',BACK_Y,1.07,0.56)
panel('TABARD | V11 lower tab rear cloth',tx0,tx1,0.318,0.440,'back',cloth,.0035,6,8,fallback_y=-0.0584)


def edge_cap(name,x,y0,y1,z0,z1,facing):
    verts=[(x,y0,z0),(x,y1,z0),(x,y1,z1),(x,y0,z1)]
    face=(0,1,2,3) if facing>0 else (3,2,1,0)
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(verts,[],[face])
    mesh.materials.append(cloth)
    mesh.update()
    obj=link(bpy.data.objects.new(name,mesh))
    skin_to_body(obj)


for label,x0,x1,y0,y1,z0,z1 in (('front',fx0,fx1,FRONT_Y-.0036,FRONT_Y+.016,0.455,1.128),
                                ('front lower tab',tx0,tx1,-0.0872-.0036,-0.0584+.0036,0.318,0.462),
                                ('back',bx0,bx1,BACK_Y-.016,BACK_Y+.0036,0.450,1.130)):
    edge_cap(f'TABARD | V11 {label} edge L',x1+.0016,y0,y1,z0,z1,1)
    edge_cap(f'TABARD | V11 {label} edge R',x0-.0016,y0,y1,z0,z1,-1)
report['changes'].append('Dark graphite tabard front/back with copper trim frame and circle-line sigil (replaces tan printed cloth read)')

# ---------------------------------------------------------------- fingertips
tips=0
for side in ('L','R'):
    for finger in ('thumb','index','middle','ring','little'):
        bone=rig.data.bones.get(f'{side} {finger}.3')
        if not bone:continue
        head=rig.matrix_world@bone.head_local
        tail=rig.matrix_world@bone.tail_local
        axis=(tail-head).normalized()
        centre=tail-axis*.006
        mesh=bpy.data.meshes.new(f'{side} HAND | V11 {finger} tip light')
        import bmesh
        bm=bmesh.new()
        bmesh.ops.create_uvsphere(bm,u_segments=12,v_segments=8,radius=.0068)
        bm.to_mesh(mesh);bm.free()
        mesh.materials.append(cyan_line)
        obj=link(bpy.data.objects.new(mesh.name,mesh))
        obj.matrix_world=Matrix.Translation(centre)@axis.to_track_quat('Z','Y').to_matrix().to_4x4()@Matrix.Diagonal((1,.8,1.5,1))
        bone_parent(obj,bone.name)
        tips+=1
report['changes'].append(f'{tips} cyan fingertip lights parented to distal finger bones')

# ---------------------------------------------------------------- painted cyan emits
src=bpy.data.materials['HYBRID source texture | ceramic graphite and cyan']
nt=src.node_tree
img=next(n for n in nt.nodes if n.type=='TEX_IMAGE')
p=nt.nodes['Principled BSDF']
sep=nt.nodes.new('ShaderNodeSeparateColor');nt.links.new(img.outputs['Color'],sep.inputs[0])
br=nt.nodes.new('ShaderNodeMath');br.operation='SUBTRACT'
nt.links.new(sep.outputs['Blue'],br.inputs[0]);nt.links.new(sep.outputs['Red'],br.inputs[1])
mask=nt.nodes.new('ShaderNodeMapRange');mask.inputs['From Min'].default_value=.14
mask.inputs['From Max'].default_value=.34;nt.links.new(br.outputs[0],mask.inputs['Value'])
strength=nt.nodes.new('ShaderNodeMath');strength.operation='MULTIPLY';strength.inputs[1].default_value=3.5
nt.links.new(mask.outputs[0],strength.inputs[0])
nt.links.new(img.outputs['Color'],p.inputs['Emission Color'])
nt.links.new(strength.outputs[0],p.inputs['Emission Strength'])
report['changes'].append('Painted cyan in source texture now emissive (knees, elbows, joints)')

# ---------------------------------------------------------------- studio
scene.render.engine='CYCLES'
scene.cycles.samples=128
scene.cycles.use_adaptive_sampling=True
scene.cycles.use_denoising=True
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    for backend in ('OPTIX','CUDA','HIP','ONEAPI'):
        try:
            prefs.compute_device_type=backend
            prefs.get_devices()
            gpus=[d for d in prefs.devices if d.type!='CPU']
            if gpus:
                for d in prefs.devices:d.use=d.type!='CPU'
                scene.cycles.device='GPU'
                report['render_device']=backend+': '+', '.join(d.name for d in gpus)
                break
        except TypeError:
            continue
except Exception as exc:
    report['render_device_error']=str(exc)
report.setdefault('render_device','CPU')
scene.view_settings.view_transform='AgX'
try:
    scene.view_settings.look='AgX - Medium High Contrast'
except TypeError:
    pass
bg=scene.world.node_tree.nodes['Background']
bg.inputs['Color'].default_value=(.30,.30,.31,1)
ground=bpy.data.materials['Studio gray'].node_tree.nodes['Principled BSDF']
ground.inputs['Base Color'].default_value=(.36,.35,.335,1)

target=bpy.data.objects.new('V11 bust target',None)
target.location=(-0.04,0,1.64)
scene.collection.objects.link(target)
bust_cam_data=bpy.data.cameras.new('V11 bust detail')
bust_cam_data.lens=85
bust=bpy.data.objects.new('V11 bust detail',bust_cam_data)
bust.location=(1.25,-2.9,1.95)
scene.collection.objects.link(bust)
track=bust.constraints.new('TRACK_TO');track.target=target
track.track_axis='TRACK_NEGATIVE_Z';track.up_axis='UP_Y'
bpy.context.view_layer.update()
# Camera-only backdrop hides the ground/world horizon in the close-up.
view=(target.location-bust.matrix_world.translation).normalized()
backdrop_mesh=bpy.data.meshes.new('V11 bust backdrop')
backdrop_mesh.from_pydata([(-6,0,-6),(6,0,-6),(6,0,6),(-6,0,6)],[],[(0,1,2,3)])
backdrop_mesh.materials.append(bpy.data.materials['Studio gray'])
backdrop=bpy.data.objects.new('V11 bust backdrop',backdrop_mesh)
backdrop.matrix_world=Matrix.Translation(target.location+view*2.4)@view.to_track_quat('Y','Z').to_matrix().to_4x4()
for attr in ('visible_shadow','visible_diffuse','visible_glossy','visible_transmission','visible_volume_scatter'):
    setattr(backdrop,attr,False)
scene.collection.objects.link(backdrop)
backdrop.hide_render=True

# v10 widened these cameras for animation clearance; tighten stills around the figure.
from bpy_extras.object_utils import world_to_camera_view
STILL_SCALE=2.75
figure_centre=Vector((-0.04,0.05,1.13))
scene.render.resolution_x,scene.render.resolution_y=1100,1400
bpy.context.view_layer.update()
for name in ('Front orthographic','Hero three quarter','Side orthographic','Back orthographic'):
    cam=bpy.data.objects[name]
    u,v,_=world_to_camera_view(scene,cam,figure_centre)
    old=cam.data.ortho_scale
    aspect=scene.render.resolution_x/scene.render.resolution_y
    cam.data.shift_x=(u-.5)*old*aspect/STILL_SCALE+cam.data.shift_x*old/STILL_SCALE
    cam.data.shift_y=(v-.5)*old/STILL_SCALE+cam.data.shift_y*old/STILL_SCALE
    cam.data.ortho_scale=STILL_SCALE
report['changes'].append(f'Still cameras tightened to ortho {STILL_SCALE} and centred on the figure (v10 used 3.25 for animation clearance)')

# ---------------------------------------------------------------- save + renders
rig.data.pose_position='POSE'
idle=bpy.data.actions['HS_v10 | Idle - contained storm']
rig.animation_data.action=idle
scene.frame_set(1)
for obj in NEW:
    obj['v11']='Fidelity pass toward LEFT A; visual study, not game validated'
scene['v11_fidelity']='; '.join(report['changes'])
bpy.ops.wm.save_as_mainfile(filepath=str(OUT_BLEND))
print('SAVED',OUT_BLEND,flush=True)

prefix='hollow-saint-hybrid-v11'
shots=[('front','Front orthographic',1100,1400),('hero','Hero three quarter',1100,1400),
       ('side','Side orthographic',1100,1400),('back','Back orthographic',1100,1400),
       ('gameplay-distance','Simulated gameplay distance',1500,1000),('bust','V11 bust detail',1100,1100)]
for suffix,cam,w,h in shots:
    scene.camera=bpy.data.objects[cam]
    scene.render.resolution_x,scene.render.resolution_y=w,h
    scene.render.filepath=str(OUT_DIR/f'{prefix}-{suffix}.png')
    backdrop.hide_render=suffix!='bust'
    bpy.ops.render.render(write_still=True)
    print('V11_RENDER',suffix,flush=True)
backdrop.hide_render=True
rig.animation_data.action=bpy.data.actions['HS_v10 | Arc Bolt - two finger snap']
scene.frame_set(9)
scene.camera=bpy.data.objects['Hero three quarter']
scene.render.resolution_x,scene.render.resolution_y=1100,1400
scene.render.filepath=str(OUT_DIR/f'{prefix}-pose-arc-bolt.png')
bpy.ops.render.render(write_still=True)
print('V11_RENDER pose-arc-bolt',flush=True)

report['new_objects']=len(NEW)
report['new_object_names']=[o.name for o in NEW]
report['status']='Visual fidelity study; renders are Blender studio evidence only, no game test'
(OUT_DIR/'metrics.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('DONE',flush=True)
