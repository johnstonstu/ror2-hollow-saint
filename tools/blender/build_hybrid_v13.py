"""Hybrid v13: thicker layered angular pauldrons, quiet-glow core, bloom, weathered two-plate halo copper.

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/build_hybrid_v13.py
"""
import bpy
import bmesh
import json
import math
from pathlib import Path
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

if not bpy.app.background:
    raise RuntimeError('Use isolated --background --factory-startup only')
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'art/hybrid/hollow-saint-hybrid-v12.blend'
OUT_BLEND=ROOT/'art/hybrid/hollow-saint-hybrid-v13.blend'
OUT_DIR=ROOT/'art/hybrid/v13'
OUT_DIR.mkdir(parents=True,exist_ok=True)
TAG='v13'

bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
rig=bpy.data.objects['Hollow Saint | v8 rig']
rig.data.pose_position='REST'
bpy.context.view_layer.update()
report={'source':str(SOURCE.relative_to(ROOT)),'changes':[]}

for obj in [o for o in scene.objects if 'SHOULDER | V12 pauldron' in o.name]:
    mesh=obj.data
    bpy.data.objects.remove(obj)
    bpy.data.meshes.remove(mesh)
ivory=bpy.data.materials['V12 ivory ceramic plate']

body=bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
bverts=[body.matrix_world@v.co for v in body.data.vertices]
bpolys=[tuple(p.vertices) for p in body.data.polygons]
bvh=BVHTree.FromPolygons(bverts,bpolys)
group_names={g.index:g.name for g in body.vertex_groups}
body_weights=[{group_names[g.group]:g.weight for g in v.groups if g.weight>0} for v in body.data.vertices]

CAPS={'L':((0.131,0.353),(-0.047,0.094),(1.528,1.760)),
      'R':((-0.411,-0.198),(-0.047,0.094),(1.528,1.760))}


def skin(obj,keep):
    groups={}
    for v in obj.data.vertices:
        _,_,pi,_=bvh.find_nearest(v.co)
        ids=bpolys[pi]
        inv=[1/max((bverts[i]-v.co).length,1e-5)**2 for i in ids]
        total=sum(inv);blended={}
        for i,f in zip(ids,inv):
            for g,w in body_weights[i].items():
                blended[g]=blended.get(g,0)+w*f/total
        blended={g:w for g,w in blended.items() if keep(g)}
        if not blended:blended={'chest':1.0}
        norm=sum(blended.values())
        for g,w in blended.items():
            if g not in groups:groups[g]=obj.vertex_groups.new(name=g)
            groups[g].add([v.index],w/norm,'REPLACE')


def shell(name,pivot,radii,tilt_deg,sx,segments,lower_cut,inner_cut,flatten,thickness,keep):
    tilt=Matrix.Rotation(math.radians(-tilt_deg*sx),3,'Y')
    bm=bmesh.new()
    bmesh.ops.create_uvsphere(bm,u_segments=segments[0],v_segments=segments[1],radius=1)
    drop=[v for v in bm.verts if v.co.z<lower_cut or v.co.x*sx<inner_cut]
    bmesh.ops.delete(bm,geom=drop,context='VERTS')
    for v in bm.verts:
        local=Vector((v.co.x*radii.x,v.co.y*radii.y,v.co.z*radii.z))
        local.z-=max(0,local.z-radii.z*.62)*flatten
        v.co=pivot+tilt@local
    mesh=bpy.data.meshes.new(name)
    bm.to_mesh(mesh);bm.free()
    mesh.materials.append(ivory)
    for poly in mesh.polygons:poly.use_smooth=True
    obj=bpy.data.objects.new(name,mesh)
    scene.collection.objects.link(obj)
    obj.parent=rig
    obj.matrix_parent_inverse=rig.matrix_world.inverted()
    skin(obj,keep)
    obj.modifiers.new('Armature','ARMATURE').object=rig
    solid=obj.modifiers.new('Plate thickness','SOLIDIFY')
    solid.thickness=thickness
    solid.offset=-1
    solid.use_quality_normals=True
    solid.use_rim=True
    bevel=obj.modifiers.new('Chamfer','BEVEL')
    bevel.width=.005
    bevel.segments=2
    bevel.limit_method='ANGLE'
    bevel.angle_limit=math.radians(28)
    bevel.harden_normals=True
    obj[TAG]='Layered angular pauldron toward LEFT A; visual study, not game validated'
    return obj


def upper_keep(g):
    return g in ('chest','spine','neck') or 'upperarm' in g or 'scapula' in g


def lower_keep(g):
    return 'upperarm' in g or 'scapula' in g or g=='chest'


for side in ('L','R'):
    sx=1 if side=='L' else -1
    (x0,x1),(y0,y1),(z0,z1)=CAPS[side]
    pivot=Vector(((x0+x1)/2,(y0+y1)/2+.008,(z0+z1)/2+.006))
    base=Vector(((x1-x0)/2*1.20,(y1-y0)/2*1.28,(z1-z0)/2*1.06))
    tilt=Matrix.Rotation(math.radians(-18*sx),3,'Y').inverted()
    cap=[v for v in bverts if x0<v.x<x1 and y0<v.y<y1 and z0+.06<v.z<z1]
    need=max(Vector(((tilt@(v-pivot)).x/base.x,(tilt@(v-pivot)).y/base.y,(tilt@(v-pivot)).z/base.z)).length for v in cap)
    base*=max(1.0,need/0.95)
    # Low segment count keeps broad readable facets; the angle-limited bevel chamfers each crease.
    shell(f'{side} SHOULDER | V13 pauldron upper',pivot,base,18,sx,(10,8),-.42,-.40,.60,.026,upper_keep)
    under_pivot=pivot+Vector((.028*sx,0,-.050))
    shell(f'{side} SHOULDER | V13 pauldron under-plate',under_pivot,
          Vector((base.x*.92,base.y*.94,base.z*.80)),30,sx,(10,8),-.55,-.10,.35,.020,lower_keep)
    report.setdefault('pauldron',{})[side]={'radii_m':[round(r,3) for r in base]}
report['changes'].append('Pauldrons rebuilt: thicker (26 mm) faceted upper plate plus a stacked under-plate, 18/30 deg outward tilt')

# Idle core should read as the concept's quiet glow rather than a white-out.
hot=bpy.data.materials['V11 cyan core hot'].node_tree.nodes['Principled BSDF']
hot.inputs['Emission Color'].default_value=(.30,.88,1.0,1)
hot.inputs['Emission Strength'].default_value=7.0
hot.inputs['Base Color'].default_value=(.55,.95,1.0,1)
report['changes'].append('Core hot centre emission 14 -> 7 and tinted cyan (quiet glow)')

copper=bpy.data.materials['V11 aged copper blocks']
cn=copper.node_tree.nodes
mix=next(n for n in cn if n.bl_idname=='ShaderNodeMix')
mix.inputs[6].default_value=(.27,.115,.048,1)
mix.inputs[7].default_value=(.075,.032,.016,1)
sep=next(n for n in cn if n.bl_idname=='ShaderNodeSeparateXYZ')
fract=next(n for n in cn if n.bl_idname=='ShaderNodeMath' and n.operation=='FRACT')
plates=cn.new('ShaderNodeMath');plates.operation='MULTIPLY';plates.inputs[1].default_value=2/3
copper.node_tree.links.new(sep.outputs['X'],plates.inputs[0])
copper.node_tree.links.new(plates.outputs[0],fract.inputs[0])
groove=next(n for n in cn if n.bl_idname=='ShaderNodeMapRange' and abs(n.inputs['From Max'].default_value-.045)<1e-4)
groove.inputs['From Max'].default_value=.028
report['changes'].append('Halo copper browner and less saturated; two plates per arc with finer seams')

tree=bpy.data.node_groups.new('V13 bloom','CompositorNodeTree')
tree.interface.new_socket('Image',in_out='OUTPUT',socket_type='NodeSocketColor')
layers=tree.nodes.new('CompositorNodeRLayers')
glare=tree.nodes.new('CompositorNodeGlare')
glare.inputs['Type'].default_value='Bloom'
glare.inputs['Quality'].default_value='High'
glare.inputs['Threshold'].default_value=1.4
glare.inputs['Strength'].default_value=.55
glare.inputs['Size'].default_value=.55
out=tree.nodes.new('NodeGroupOutput')
tree.links.new(layers.outputs['Image'],glare.inputs['Image'])
tree.links.new(glare.outputs['Image'],out.inputs[0])
scene.compositing_node_group=tree
report['changes'].append('Compositor bloom on emissive highlights')

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
