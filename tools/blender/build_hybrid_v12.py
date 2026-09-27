"""Hybrid v12: broad faceted LEFT A pauldrons over the v11 shoulder caps.

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/build_hybrid_v12.py
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

if not bpy.app.background:
    raise RuntimeError('Use isolated --background --factory-startup only')
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'art/hybrid/hollow-saint-hybrid-v11.blend'
OUT_BLEND=ROOT/'art/hybrid/hollow-saint-hybrid-v12.blend'
OUT_DIR=ROOT/'art/hybrid/v12'
OUT_DIR.mkdir(parents=True,exist_ok=True)

bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
rig=bpy.data.objects['Hollow Saint | v8 rig']
rig.data.pose_position='REST'
bpy.context.view_layer.update()
report={'source':str(SOURCE.relative_to(ROOT)),'changes':[]}


def ivory_ceramic():
    mat=bpy.data.materials.new('V12 ivory ceramic plate')
    mat.use_nodes=True
    nt=mat.node_tree
    p=nt.nodes['Principled BSDF']
    tex=nt.nodes.new('ShaderNodeTexCoord')
    noise=nt.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=22
    noise.inputs['Detail'].default_value=5
    nt.links.new(tex.outputs['Object'],noise.inputs['Vector'])
    ramp=nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position=.40
    ramp.color_ramp.elements[0].color=(.64,.60,.50,1)
    ramp.color_ramp.elements[1].position=.60
    ramp.color_ramp.elements[1].color=(.72,.685,.575,1)
    nt.links.new(noise.outputs['Fac'],ramp.inputs['Fac'])
    nt.links.new(ramp.outputs['Color'],p.inputs['Base Color'])
    p.inputs['Roughness'].default_value=.46
    p.inputs['Metallic'].default_value=0
    return mat


ivory=ivory_ceramic()

import bmesh
from mathutils.bvhtree import BVHTree

body=bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
bverts=[body.matrix_world@v.co for v in body.data.vertices]
bpolys=[tuple(p.vertices) for p in body.data.polygons]
bvh=BVHTree.FromPolygons(bverts,bpolys)
group_names={g.index:g.name for g in body.vertex_groups}
body_weights=[{group_names[g.group]:g.weight for g in v.groups if g.weight>0} for v in body.data.vertices]

# Visible shoulder-cap boxes measured on the calibrated v11 front/side renders.
CAPS={'L':((0.131,0.353),(-0.047,0.094),(1.528,1.760)),
      'R':((-0.411,-0.198),(-0.047,0.094),(1.528,1.760))}
BROADEN=Vector((1.18,1.25,1.05))


def outward_visible(centre,normal):
    hit=bvh.ray_cast(centre+normal*.004,normal,.35)
    return hit[0] is None


def skin(obj):
    groups={}
    for v in obj.data.vertices:
        _,_,pi,_=bvh.find_nearest(v.co)
        ids=bpolys[pi]
        inv=[1/max((bverts[i]-v.co).length,1e-5)**2 for i in ids]
        total=sum(inv);blended={}
        for i,f in zip(ids,inv):
            for g,w in body_weights[i].items():
                blended[g]=blended.get(g,0)+w*f/total
        # Plates must not follow the head or fingers.
        blended={g:w for g,w in blended.items() if g in ('chest','spine','neck') or 'upperarm' in g or 'scapula' in g}
        norm=sum(blended.values()) or 1
        if not blended:blended={'chest':1.0};norm=1
        for g,w in blended.items():
            if g not in groups:groups[g]=obj.vertex_groups.new(name=g)
            groups[g].add([v.index],w/norm,'REPLACE')


from mathutils import Matrix

TILT_DEG=16
LOWER_CUT=-.50
INNER_CUT=-.45


def pauldron(side):
    sx=1 if side=='L' else -1
    (x0,x1),(y0,y1),(z0,z1)=CAPS[side]
    pivot=Vector(((x0+x1)/2,(y0+y1)/2+.008,(z0+z1)/2+.004))
    radii=Vector(((x1-x0)/2*BROADEN.x,(y1-y0)/2*BROADEN.y,(z1-z0)/2*BROADEN.z))
    tilt=Matrix.Rotation(math.radians(-TILT_DEG*sx),3,'Y')
    inv_tilt=tilt.inverted()
    # Grow the ellipsoid until it encloses the upper shoulder-cap surface.
    cap=[v for v in bverts if x0<v.x<x1 and y0<v.y<y1 and z0+.06<v.z<z1]
    def norm_radius(v):
        d=inv_tilt@(v-pivot)
        return Vector((d.x/radii.x,d.y/radii.y,d.z/radii.z)).length
    need=max(norm_radius(v) for v in cap)
    grow=max(1.0,need/0.96)
    radii*=grow
    bm=bmesh.new()
    bmesh.ops.create_uvsphere(bm,u_segments=14,v_segments=10,radius=1)
    drop=[v for v in bm.verts if v.co.z<LOWER_CUT or v.co.x*sx<INNER_CUT]
    bmesh.ops.delete(bm,geom=drop,context='VERTS')
    for v in bm.verts:
        local=Vector((v.co.x*radii.x,v.co.y*radii.y,v.co.z*radii.z))
        local.z-=max(0,local.z-radii.z*.72)*.55
        v.co=pivot+tilt@local
    name=f'{side} SHOULDER | V12 pauldron'
    mesh=bpy.data.meshes.new(name)
    bm.to_mesh(mesh);bm.free()
    mesh.materials.append(ivory)
    for poly in mesh.polygons:poly.use_smooth=True
    obj=bpy.data.objects.new(name,mesh)
    scene.collection.objects.link(obj)
    obj.parent=rig
    obj.matrix_parent_inverse=rig.matrix_world.inverted()
    skin(obj)
    obj.modifiers.new('Armature','ARMATURE').object=rig
    solid=obj.modifiers.new('Plate thickness','SOLIDIFY')
    solid.thickness=.014
    solid.offset=-1
    solid.use_even_offset=False
    solid.use_quality_normals=True
    solid.use_rim=True
    bevel=obj.modifiers.new('Chamfer','BEVEL')
    bevel.width=.0035
    bevel.segments=2
    bevel.limit_method='ANGLE'
    bevel.angle_limit=math.radians(40)
    bevel.harden_normals=True
    obj['v12']='Broad faceted pauldron toward LEFT A; visual study, not game validated'
    report.setdefault('pauldron',{})[side]={'radii_m':[round(r,3) for r in radii],'enclose_growth':round(grow,3)}
    return obj


for side in ('L','R'):
    pauldron(side)
report['changes'].append('Broad faceted ivory pauldrons over the shoulder caps (flat top sloping outward, peaked outer corner, hanging outer face), chest-to-upperarm weight blend')

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
scene['v12_fidelity']='; '.join(report['changes'])
bpy.ops.wm.save_as_mainfile(filepath=str(OUT_BLEND))
print('SAVED',OUT_BLEND,flush=True)

backdrop=bpy.data.objects['V11 bust backdrop']
prefix='hollow-saint-hybrid-v12'
shots=[('front','Front orthographic',1100,1400),('hero','Hero three quarter',1100,1400),
       ('side','Side orthographic',1100,1400),('back','Back orthographic',1100,1400),
       ('gameplay-distance','Simulated gameplay distance',1500,1000),('bust','V11 bust detail',1100,1100)]
for suffix,cam,w,h in shots:
    scene.camera=bpy.data.objects[cam]
    scene.render.resolution_x,scene.render.resolution_y=w,h
    scene.render.filepath=str(OUT_DIR/f'{prefix}-{suffix}.png')
    backdrop.hide_render=suffix!='bust'
    bpy.ops.render.render(write_still=True)
    print('V12_RENDER',suffix,flush=True)
backdrop.hide_render=True
rig.animation_data.action=bpy.data.actions['HS_v10 | Arc Bolt - two finger snap']
scene.frame_set(9)
scene.camera=bpy.data.objects['Hero three quarter']
scene.render.resolution_x,scene.render.resolution_y=1100,1400
scene.render.filepath=str(OUT_DIR/f'{prefix}-pose-arc-bolt.png')
bpy.ops.render.render(write_still=True)
print('V12_RENDER pose-arc-bolt',flush=True)
report['status']='Visual fidelity study; renders are Blender studio evidence only, no game test'
(OUT_DIR/'metrics.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('DONE',flush=True)
