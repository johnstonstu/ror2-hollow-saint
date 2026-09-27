"""Hybrid v17: concept silhouette pass on the v16 rig.

- Head raised on a longer, thinner cabled neck (head/neck/halo bones moved to match), ear sockets.
- Chest: raised core housing, layered ivory chest plates, segmented dark abdomen with cyan slits
  (replaces the radiating 'spider' lines).
- Sleeker swept-up pauldrons on the v16 pauldron bones.
- Halo yoke bracket from the upper back to the lower halo arcs.
- Slimmer waist; crisper plate edges via smooth-by-angle shading.

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/build_hybrid_v17.py
"""
import bpy
import bmesh
import json
import math
import os
from pathlib import Path
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

if not bpy.app.background:
    raise RuntimeError('Use isolated --background --factory-startup only')
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'art/hybrid/hollow-saint-hybrid-v16.blend'
# v17 = first concept pass (neck +9 cm, pauldron tilt 16); v18 = short follow-up with this file's values.
TAG=os.environ.get('HS_OUT','v18')
OUT_BLEND=ROOT/f'art/hybrid/hollow-saint-hybrid-{TAG}.blend'
OUT_DIR=ROOT/f'art/hybrid/{TAG}'
OUT_DIR.mkdir(parents=True,exist_ok=True)

bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
rig=bpy.data.objects['Hollow Saint | v8 rig']
body=bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
rig.data.pose_position='REST'
bpy.context.view_layer.update()
report={'source':str(SOURCE.relative_to(ROOT)),'changes':[]}
mat=bpy.data.materials
ivory=mat['V12 ivory ceramic plate']
graphite=mat['HYBRID graphite']
cyan=mat['V11 cyan conductor light']

CX=-0.04          # body centre line in x
NECK_LIFT=0.11
WAIST_CX,WAIST_CY=-0.035,0.05


def smooth(x,a,b):
    t=min(1,max(0,(x-a)/(b-a)))
    return t*t*(3-2*t)


# ---------------------------------------------------------------- rest-shape edits
def chest_front(p):
    # Core, core ring and upper-chest plates carry some neck weight; they must not lift or thin.
    return p.y<-0.06 and p.z<1.60


def lift_amount(p,w):
    if abs(p.x-CX)>0.16 or p.z<1.44 or chest_front(p):
        return 0.0
    return NECK_LIFT*min(1.0,w.get('head',0)+w.get('neck',0)*smooth(p.z,1.57,1.63))


def reshape(p,w,allow_waist):
    q=p.copy()
    wn=w.get('neck',0)
    if wn>0.3 and 1.58<p.z<1.66 and abs(p.x-CX)<0.12 and not chest_front(p):
        q.x=CX+(q.x-CX)*0.86
        q.y=0.01+(q.y-0.01)*0.86
    q.z+=lift_amount(p,w)
    if allow_waist:
        bump=smooth(p.z,1.10,1.20)*(1-smooth(p.z,1.26,1.38))
        q.x=WAIST_CX+(q.x-WAIST_CX)*(1-0.18*bump)
        q.y=WAIST_CY+(q.y-WAIST_CY)*(1-0.08*bump)
    return q


def limb_or_tabard(w):
    return any((k.startswith(('L ','R ')) or k.startswith('tabard')) and v>0.05 for k,v in w.items())


mw=body.matrix_world
inv=mw.inverted()
moved=0
for v in body.data.vertices:
    w={body.vertex_groups[g.group].name:g.weight for g in v.groups}
    p=mw@v.co
    q=reshape(p,w,not limb_or_tabard(w))
    if (q-p).length>1e-6:
        v.co=inv@q;moved+=1
body.data.update()
overlays=0
for obj in scene.objects:
    if obj==body or obj.type!='MESH' or obj.name.startswith(('TABARD','L SHOULDER','R SHOULDER')):continue
    if not any(m.type=='ARMATURE' for m in obj.modifiers):continue
    omw=obj.matrix_world;oinv=omw.inverted()
    for v in obj.data.vertices:
        w={obj.vertex_groups[g.group].name:g.weight for g in v.groups}
        p=omw@v.co
        v.co=oinv@reshape(p,w,not limb_or_tabard(w))
    obj.data.update();overlays+=1
report['changes'].append(f'Head raised {NECK_LIFT*100:.0f} cm on a thinner neck; waist slimmed up to 18% ({moved} body verts, {overlays} overlays follow)')

bpy.context.view_layer.objects.active=rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
eb=rig.data.edit_bones
up=Vector((0,0,NECK_LIFT))
eb['neck'].tail+=up
for name in ('head','head socket','halo root','halo 1','halo 2','halo 3','halo 4','halo socket'):
    eb[name].head+=up;eb[name].tail+=up
bpy.ops.object.mode_set(mode='OBJECT')
bpy.context.view_layer.update()
report['changes'].append('Head, head socket and halo bones raised to match (halo stays centred on the helmet)')

# ---------------------------------------------------------------- helpers on the edited body
bverts=[mw@v.co for v in body.data.vertices]
bpolys=[tuple(p.vertices) for p in body.data.polygons]
bvh=BVHTree.FromPolygons(bverts,bpolys)
body_weights=[{body.vertex_groups[g.group].name:g.weight for g in v.groups if g.weight>0} for v in body.data.vertices]
NEW=[]


def link(obj):
    scene.collection.objects.link(obj)
    obj[TAG]='Concept silhouette pass; visual study'
    NEW.append(obj.name)
    return obj


def skin(obj,keep=None,rigid=None):
    obj.parent=rig
    obj.matrix_parent_inverse=rig.matrix_world.inverted()
    if rigid:
        obj.vertex_groups.new(name=rigid).add([v.index for v in obj.data.vertices],1.0,'REPLACE')
    else:
        groups={}
        for v in obj.data.vertices:
            _,_,pi,_=bvh.find_nearest(v.co)
            ids=bpolys[pi]
            wts=[1/max((bverts[i]-v.co).length,1e-5)**2 for i in ids]
            total=sum(wts);blended={}
            for i,f in zip(ids,wts):
                for g,wt in body_weights[i].items():
                    blended[g]=blended.get(g,0)+wt*f/total
            if keep:
                blended={g:x for g,x in blended.items() if keep(g)} or {'chest':1.0}
            norm=sum(blended.values())
            for g,x in blended.items():
                if g not in groups:groups[g]=obj.vertex_groups.new(name=g)
                groups[g].add([v.index],x/norm,'REPLACE')
    obj.modifiers.new('Armature','ARMATURE').object=rig


def bone_parent(obj,bone):
    world=obj.matrix_world.copy()
    obj.parent=rig;obj.parent_type='BONE';obj.parent_bone=bone
    obj.matrix_world=world


def armature_last(obj):
    # Angle-limited bevel must see the rest shape, otherwise topology changes with the pose.
    i=obj.modifiers.find('Armature')
    if i>=0:
        obj.modifiers.move(i,len(obj.modifiers)-1)


def front_hit(x,z):
    loc,n,_,_=bvh.ray_cast(Vector((x,-1.0,z)),Vector((0,1,0)),3)
    return (loc,n) if loc is not None else None


def fan_plate(name,outline,material,lift,thickness,keep,rings=4,bevel=.0018):
    """Plate filling a 2D (x,z) outline, projected onto the body front and solidified outward."""
    c=Vector((sum(p[0] for p in outline)/len(outline),sum(p[1] for p in outline)/len(outline)))
    n=len(outline)
    verts=[];faces=[]
    def put(x,z):
        hit=front_hit(x,z)
        if hit is None:
            return None
        loc,nrm=hit
        verts.append(tuple(loc+nrm*lift))
        return len(verts)-1
    centre=put(c.x,c.y)
    def put_inside(p,t):
        # Pull outline points toward the centre until they land on front-facing skin.
        s=t
        while s>t*0.5:
            x,z=c.x+(p[0]-c.x)*s,c.y+(p[1]-c.y)*s
            hit=front_hit(x,z)
            if hit is not None and hit[1].y<-0.15:
                return put(x,z)
            s-=t*0.04
        return None
    grid=[]
    for r in range(1,rings+1):
        grid.append([put_inside(p,r/rings) for p in outline])
    if centre is None or any(i is None for ring in grid for i in ring):
        report.setdefault('skipped',[]).append(name);return None
    for i in range(n):
        faces.append((centre,grid[0][i],grid[0][(i+1)%n]))
    for r in range(rings-1):
        for i in range(n):
            j=(i+1)%n
            faces.append((grid[r][i],grid[r+1][i],grid[r+1][j],grid[r][j]))
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(verts,[],faces)
    mesh.materials.append(material)
    mesh.update()
    for poly in mesh.polygons:poly.use_smooth=True
    obj=link(bpy.data.objects.new(name,mesh))
    # Faces wind toward -y (outward on the front); make sure normals face the camera side.
    if sum(p.normal.y for p in mesh.polygons)>0:
        for poly in mesh.polygons:poly.flip()
    skin(obj,keep)
    sol=obj.modifiers.new('Plate thickness','SOLIDIFY')
    sol.thickness=thickness;sol.offset=1;sol.use_rim=True;sol.use_even_offset=True
    bev=obj.modifiers.new('Chamfer','BEVEL')
    bev.width=bevel;bev.segments=2;bev.limit_method='ANGLE';bev.angle_limit=math.radians(35);bev.harden_normals=True
    armature_last(obj)
    return obj


def ribbon(name,pts,width,material,keep=None):
    pts=[p for p in pts if p is not None]
    if len(pts)<2:
        report.setdefault('skipped',[]).append(name);return None
    verts=[];faces=[]
    for i,(p,n) in enumerate(pts):
        a=pts[max(i-1,0)][0];b=pts[min(i+1,len(pts)-1)][0]
        side=n.cross((b-a).normalized()).normalized()*width/2
        verts+=[p-side,p+side]
    for i in range(len(pts)-1):
        faces.append((i*2,i*2+1,i*2+3,i*2+2))
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(v) for v in verts],[],faces)
    mesh.materials.append(material);mesh.update()
    obj=link(bpy.data.objects.new(name,mesh))
    skin(obj,keep)
    return obj


def front_line(p0,p1,lift,steps=10):
    out=[]
    for i in range(steps+1):
        x=p0[0]+(p1[0]-p0[0])*i/steps;z=p0[1]+(p1[1]-p0[1])*i/steps
        hit=front_hit(x,z)
        out.append((hit[0]+hit[1]*lift,hit[1]) if hit else None)
    return out


def tube(name,pts,radius,material,segments=8,cap=True):
    bm=bmesh.new()
    rings=[]
    for i,p in enumerate(pts):
        a=pts[max(i-1,0)];b=pts[min(i+1,len(pts)-1)]
        t=(b-a).normalized()
        u=t.orthogonal().normalized();w=t.cross(u)
        rings.append([bm.verts.new(p+(u*math.cos(2*math.pi*k/segments)+w*math.sin(2*math.pi*k/segments))*radius) for k in range(segments)])
    for r in range(len(rings)-1):
        for k in range(segments):
            j=(k+1)%segments
            bm.faces.new((rings[r][k],rings[r][j],rings[r+1][j],rings[r+1][k]))
    if cap:
        bm.faces.new(rings[0][::-1]);bm.faces.new(rings[-1])
    mesh=bpy.data.meshes.new(name)
    bm.to_mesh(mesh);bm.free()
    mesh.materials.append(material)
    for poly in mesh.polygons:poly.use_smooth=True
    return link(bpy.data.objects.new(name,mesh))


def disc(name,centre,normal,radius,material,thickness=0.0,segments=24):
    normal=normal.normalized();u=normal.orthogonal().normalized();w=normal.cross(u)
    verts=[tuple(centre)]+[tuple(centre+(u*math.cos(2*math.pi*i/segments)+w*math.sin(2*math.pi*i/segments))*radius) for i in range(segments)]
    faces=[(0,1+i,1+(i+1)%segments) for i in range(segments)]
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(verts,[],faces);mesh.materials.append(material);mesh.update()
    if sum(p.normal.dot(normal) for p in mesh.polygons)<0:
        for poly in mesh.polygons:poly.flip()
    obj=link(bpy.data.objects.new(name,mesh))
    if thickness:
        s=obj.modifiers.new('Thickness','SOLIDIFY');s.thickness=thickness;s.offset=-1;s.use_rim=True
    return obj


torso=None  # chest pieces follow the full interpolated body weights; rib-side skin also carries arm weight

# ---------------------------------------------------------------- neck cables and ear sockets
head_bone=rig.data.bones['head']
cables=0
for angle in (35,-35,145,-145):
    a=math.radians(angle)
    direction=Vector((math.sin(a),-math.cos(a),0))
    pts=[]
    for i in range(9):
        z=1.505+(1.705-1.505)*i/8
        origin=Vector((CX,0.01,z))+direction*0.4
        loc,n,_,_=bvh.ray_cast(origin,-direction,0.5)
        if loc is None:break
        pts.append(loc+direction*0.010)
    if len(pts)>=5:
        obj=tube(f'NECK | V17 cable {angle:+d}',pts,.0080,graphite)
        skin(obj,lambda g:g in ('neck','head','chest'))
        cables+=1
for side,sx in (('L',1),('R',-1)):
    origin=Vector((CX+sx*0.5,0.005,1.905))
    loc,n,_,_=bvh.ray_cast(origin,Vector((-sx,0,0)),0.6)
    if loc is None:
        report.setdefault('skipped',[]).append(f'ear {side}');continue
    sock=disc(f'MASK | V17 ear socket {side}',loc+n*.002,n,.027,graphite,thickness=.006)
    dot=disc(f'MASK | V17 ear socket light {side}',loc+n*.0045,n,.008,cyan)
    for o in (sock,dot):bone_parent(o,'head')
report['changes'].append(f'{cables} dark neck cables; dark ear sockets with cyan cores bone-parented to the head')

# ---------------------------------------------------------------- chest redesign
for name in ('CHEST | V11 lower branch L','CHEST | V11 lower branch R','ABDOMEN | V11 centre conductor',
             'ABDOMEN | V11 side dash L','ABDOMEN | V11 side dash R'):
    o=bpy.data.objects.get(name)
    if o:bpy.data.objects.remove(o,do_unlink=True)
core=bpy.data.objects['CHEST | V11 core hot centre']
core_c=sum((core.matrix_world@v.co for v in core.data.vertices),Vector())/len(core.data.vertices)
rim=[]
for i in range(48):
    a=2*math.pi*i/48
    hit=front_hit(core_c.x+.074*math.sin(a),core_c.z+.074*math.cos(a))
    if hit:rim.append(hit[0]+hit[1]*.006)
rim.append(rim[0])
housing=tube('CHEST | V17 core housing rim',rim,.0075,graphite,cap=False)
skin(housing,torso)
# Torso edge is only ~0.11 m from the centre line at z 1.41, so the plates stay inside it.
PEC=[(.082,1.602),(.150,1.594),(.176,1.515),(.146,1.446),(.100,1.430),(.083,1.462)]
# Right-side body mesh has a gap near dx .09, z 1.33.
RIB=[(.062,1.412),(.106,1.420),(.082,1.350),(.058,1.320)]
for side,sx in (('L',1),('R',-1)):
    for label,outline in (('upper chest plate',PEC),('rib plate',RIB)):
        pts=[(CX+sx*dx,z) for dx,z in outline]
        if sx<0:pts=pts[::-1]
        fan_plate(f'CHEST | V17 {label} {side}',pts,ivory,.002,.010,torso)
SEGMENTS=[(1.372,1.318,.050),(1.302,1.248,.045),(1.232,1.180,.040)]
for i,(top,bot,hw) in enumerate(SEGMENTS):
    c=.008
    outline=[(CX-hw+c,top),(CX+hw-c,top),(CX+hw,top-c),(CX+hw*.86,bot+c),(CX+hw*.86-c,bot),(CX-hw*.86+c,bot),(CX-hw*.86,bot+c),(CX-hw,top-c)]
    fan_plate(f'ABDOMEN | V17 segment plate {i+1}',outline,graphite,.002,.007,torso,rings=3,bevel=.0015)
    ribbon(f'ABDOMEN | V17 segment slit {i+1}',front_line((CX,top-.010),(CX,bot+.010),.0105,6),.0048,cyan,torso)
ribbon('CHEST | V17 core drop slit',front_line((CX,1.428),(CX,1.385),.004,5),.0048,cyan,torso)
for side,sx in (('L',1),('R',-1)):
    for j,((x0,z0),(x1,z1)) in enumerate((((.098,1.352),(.072,1.302)),((.094,1.282),(.070,1.236)))):
        ribbon(f'ABDOMEN | V17 side slit {side}{j+1}',front_line((CX+sx*x0,z0),(CX+sx*x1,z1),.0025,6),.0050,cyan,torso)
report['changes'].append('Chest: raised dark core housing, layered ivory upper-chest and rib plates, three segmented dark abdomen plates with cyan slits, angled side slits (radiating lines removed)')

# ---------------------------------------------------------------- sleeker pauldrons
for o in [o for o in scene.objects if 'SHOULDER | V13 pauldron' in o.name]:
    m=o.data;bpy.data.objects.remove(o);bpy.data.meshes.remove(m)
CAPS={'L':((0.131,0.353),(-0.047,0.094),(1.528,1.760)),'R':((-0.411,-0.198),(-0.047,0.094),(1.528,1.760))}


def cap_pauldron(name,side,inner_x,length,axis_z,tilt_deg,u_half,facets,peak,margin,thickness,v_from=0.0):
    """Faceted shell wrapped around a tilted shoulder axis; outer top edge sweeps up to a point."""
    sx=1 if side=='L' else -1
    t=math.radians(tilt_deg)
    d=Vector((sx*math.cos(t),0,-math.sin(t)))
    n0=Vector((sx*math.sin(t),0,math.cos(t)))
    yv=Vector((0,1,0))
    origin=Vector((inner_x,0.024,axis_z))
    bins=[0.0]*11
    for p in bverts:
        a=(p-origin).dot(d)
        if not (-0.02<a<length+0.02) or p.z<1.47:continue
        rel=p-origin-d*a
        if rel.dot(n0)<-0.02:continue
        k=min(10,max(0,round(a/length*10)))
        bins[k]=max(bins[k],rel.length)
    for k in range(1,11):bins[k]=max(bins[k],bins[k-1]*0.9)
    def radius(v):
        k=v*10;i=min(9,int(k));f=k-i
        return (bins[i]*(1-f)+bins[i+1]*f)+margin+0.012*v
    V=6
    verts=[];faces=[]
    for i in range(facets+1):
        u=-u_half+2*u_half*i/facets
        vmax=1-0.30*(abs(u)/u_half)**1.6
        for j in range(V+1):
            v=v_from+(vmax-v_from)*j/V
            p=origin+d*(v*length)+(n0*math.cos(u)+yv*math.sin(u))*radius(v)
            p+=n0*(peak*v**2*max(0.0,math.cos(u))**3)
            verts.append(tuple(p))
    for i in range(facets):
        for j in range(V):
            a=i*(V+1)+j
            faces.append((a,a+1,a+V+2,a+V+1))
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(verts,[],faces);mesh.materials.append(ivory);mesh.update()
    centre=origin+d*(length*.5)
    if sum((Vector(pl.center)-centre).dot(pl.normal) for pl in mesh.polygons)<0:
        for pl in mesh.polygons:pl.flip()
    for pl in mesh.polygons:pl.use_smooth=False
    obj=link(bpy.data.objects.new(name,mesh))
    skin(obj,rigid=f'{side} pauldron')
    s=obj.modifiers.new('Plate thickness','SOLIDIFY');s.thickness=thickness;s.offset=-1;s.use_even_offset=True;s.use_rim=True
    b=obj.modifiers.new('Chamfer','BEVEL');b.width=.003;b.segments=2;b.limit_method='ANGLE';b.angle_limit=math.radians(20);b.harden_normals=True
    armature_last(obj)
    return obj


for side in ('L','R'):
    sx=1 if side=='L' else -1
    (x0,x1),_,_=CAPS[side]
    inner=x0 if sx>0 else x1
    length=abs(x1-x0)*1.08
    cap_pauldron(f'{side} SHOULDER | V17 pauldron upper',side,inner,length*1.06,1.630,30,1.80,6,.055,.026,.020)
report['changes'].append('Pauldrons rebuilt as faceted 20 mm caps wrapped around the shoulder with a swept-up pointed outer corner; rigid to v16 pauldron bones')

# ---------------------------------------------------------------- halo yoke
ends=[]
for n in ('HALO | independent copper arc 2','HALO | independent copper arc 3'):
    o=bpy.data.objects[n]
    vs=[o.matrix_world@v.co for v in o.data.vertices]
    lo=min(vs,key=lambda v:v.z)
    ends.append(Vector((lo.x,0.150,lo.z+.018)))
mid=Vector((CX,0.178,max(e.z for e in ends)+.012))
path=[]
for i in range(13):
    t=i/12
    a,b=(ends[0],mid) if t<=.5 else (mid,ends[1])
    s=t*2 if t<=.5 else (t-.5)*2
    p=a.lerp(b,s)
    p.y+=0.012*math.sin(math.pi*t)
    path.append(p)
bar=tube('HALO | V17 yoke bar',path,.018,graphite,segments=4)
bar.modifiers.new('Chamfer','BEVEL').width=.002
node=bpy.data.objects['BACK | small dark node housing']
node_c=sum((node.matrix_world@v.co for v in node.data.vertices),Vector())/len(node.data.vertices)
strut=tube('HALO | V17 yoke strut',[node_c+Vector((0,.008,0)),node_c.lerp(mid,.5)+Vector((0,0,.004)),mid],.014,graphite,segments=4)
light=disc('HALO | V17 yoke light',mid+Vector((0,.019,0)),Vector((0,1,0)),.011,cyan)
for o in (bar,strut,light):bone_parent(o,'chest')
report['changes'].append('Dark halo yoke: bar joining the lower halo arcs behind the shoulders, strut to the back node, cyan node light')

# ---------------------------------------------------------------- crisper plate edges
CRISP_DEG=38
with bpy.context.temp_override(object=body,active_object=body,selected_objects=[body],selected_editable_objects=[body]):
    bpy.ops.object.shade_auto_smooth(angle=math.radians(CRISP_DEG))
report['changes'].append(f'Body shaded smooth-by-angle {CRISP_DEG} deg for crisper plate edges')
report['new_objects']=NEW

try:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type='OPTIX';prefs.get_devices()
    for d in prefs.devices:d.use=d.type!='CPU'
    scene.cycles.device='GPU';report['render_device']='OPTIX'
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
for action,frame,label in (('HS_v10 | Arc Bolt - two finger snap',9,'pose-arc-bolt'),('HS_v10 | Arc Step - in place dash',9,'pose-arc-step'),('HS_v10 | Open Circuit - unfolding crown',37,'pose-open-circuit')):
    rig.animation_data.action=bpy.data.actions[action]
    scene.frame_set(frame)
    scene.render.filepath=str(OUT_DIR/f'{prefix}-{label}.png')
    bpy.ops.render.render(write_still=True)
    print('RENDER',label,flush=True)
report['status']='Visual fidelity study; renders are Blender studio evidence only, no game test'
(OUT_DIR/'metrics.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('DONE',flush=True)
