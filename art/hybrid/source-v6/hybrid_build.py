"""Derived HF hybrid: preserve UV body, replace fused halo/hands, correct posterior."""
import bpy
import bmesh
import math
import sys
import json
from pathlib import Path
from mathutils import Vector

if not bpy.app.background:
    raise RuntimeError('Use isolated --background --factory-startup only')
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/hybrid'
OUT.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(Path(__file__).parent))
from refine_helpers import material, PARTS
from hybrid_components import halo, hands, back_details
from hybrid_presentation import present
from hybrid_surgery import trim_posterior_shoulders,semantic_boundaries,dome_shoulder_surfaces,finish_inner_shoulder_walls
from fit_hybrid_materials import tune_body_material,tune_copper
from hybrid_rear_material import blend_posterior

bpy.ops.wm.open_mainfile(filepath=str(ROOT/'art/comparison/higgsfield-sam3d-review.blend'))
bpy.context.preferences.filepaths.save_version=0
body=bpy.data.objects['geometry_0']
body.name='HF BODY | retained UV sculpt, corrected posterior'
body.data=body.data.copy()
body.data.name='HF derived body topology | UV retained'
dark=material('HYBRID graphite',(.014,.020,.024),.78,.83)
ivory=material('HYBRID warm ivory',(.72,.685,.575),.05,.52)
cyan=material('HYBRID cyan conductor',(.016,.68,.87),.05,.28,2)
copper=material('HYBRID aged copper',(.37,.16,.053),.62,.46)
copper_trim=material('HYBRID copper edge',(.47,.225,.087),.7,.37)
body.data.materials.append(dark)
body.data.materials.append(ivory)
body.data.materials.append(dark)
cx=-.0375
bm=bmesh.new()
bm.from_mesh(body.data)
bm.verts.ensure_lookup_table()
bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000002)
bm.normal_update()
removed={'halo':0,'hands':0}
delete=[]
# Flood only below each wrist from a hand seed. Legs cannot be reached without
# traversing the pelvis above this height, unlike a global x/z rectangle.
hand_vertices=set()
for wx in [.475,-.516]:
    seed=min(bm.verts,key=lambda v:(v.co-Vector((wx,-.065,.86))).length_squared)
    queue=[seed]
    hand_vertices.add(seed)
    while queue:
        v=queue.pop()
        for edge in v.link_edges:
            other=edge.other_vert(v)
            if other not in hand_vertices and other.co.z<.974:
                hand_vertices.add(other)
                queue.append(other)
for face in bm.faces:
    co=face.calc_center_median()
    radius=math.hypot(co.x-cx,co.z-1.78)
    is_halo=co.y>.095 and co.z>1.515 and ((co.z>1.70 and radius>.195) or abs(co.x-cx)>.235)
    is_hand=any(v in hand_vertices for v in face.verts)
    if is_halo or is_hand:
        delete.append(face)
        removed['halo' if is_halo else 'hands']+=1
bmesh.ops.delete(bm,geom=delete,context='FACES')
loose=[v for v in bm.verts if not v.link_faces]
if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
boundary=set(e for e in bm.edges if e.is_boundary)
filled=[]
boundary_report=[]
while boundary:
    first=boundary.pop()
    edges={first}
    queue=list(first.verts)
    while queue:
        v=queue.pop()
        for edge in v.link_edges:
            if edge in boundary:
                boundary.remove(edge)
                edges.add(edge)
                queue.append(edge.other_vert(v))
    vertices=set(v for edge in edges for v in edge.verts)
    bounds=[max(v.co[i] for v in vertices)-min(v.co[i] for v in vertices) for i in range(3)]
    boundary_report.append({'edges':len(edges),'extents':bounds,
                            'min':[min(v.co[i] for v in vertices) for i in range(3)],
                            'max':[max(v.co[i] for v in vertices) for i in range(3)]})
    if max(bounds)>.30:
        print('CUT_BOUNDARIES '+json.dumps(boundary_report),flush=True)
        raise RuntimeError('Cut boundary unexpectedly large; refuse broad fill')
    if min(v.co.z for v in vertices)>1.3:
        # Shoulder loops wrap a bowed shell and are not planar ngon caps.
        center=sum((v.co for v in vertices),Vector())/len(vertices)
        center.y=max(v.co.y for v in vertices)+.012
        pole=bm.verts.new(center)
        for border in edges:
            face=bm.faces.new((border.verts[0],border.verts[1],pole))
            face.smooth=True
            filled.append(face)
    else:
        filled.extend(bmesh.ops.holes_fill(bm,edges=list(edges),sides=0)['faces'])
print('CUT_BOUNDARIES '+json.dumps(boundary_report),flush=True)
for face in filled:
    face.material_index=1 if face.calc_center_median().z<1.3 else 2
    face.smooth=face.calc_center_median().z>1.3
bm.to_mesh(body.data)
bm.free()
trim_posterior_shoulders(body,cx)
bm=bmesh.new()
bm.from_mesh(body.data)
dome_shoulder_surfaces(bm,cx)
recolored=semantic_boundaries(bm,cx)
finish_inner_shoulder_walls(bm,cx)

# Suppress the generated front-chest projection on the posterior geometry.
for vertex in bm.verts:
    co=vertex.co
    if 1.255<co.z<1.715 and co.y>.06 and abs(co.x-cx)<.215:
        z=co.z
        t=(z-1.255)/(1.715-1.255)
        width=.12+.075*math.sin(t*math.pi*.8)
        depth=.069+.026*math.sin(t*math.pi)
        radial=max(0,1-((co.x-cx)/width)**2)**.5
        target=.025+depth*radial
        blend=min(1,(z-1.255)/.075,(1.715-z)/.075)
        if co.y>target:co.y+=(target-co.y)*blend
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
bm.to_mesh(body.data)
bm.free()
body.data.update()
for polygon in body.data.polygons:
    if polygon.material_index==1 and polygon.center.z<1.25:
        polygon.material_index=3
tune_body_material(body)
tune_copper(copper)
blend_posterior(body)
body['status']='Derived HF mesh; welded UV seam vertices with UV loops retained; local halo/hand removal and back correction'
body['source']='output/higgsfield-hollow-saint/hollow-saint-sam3d.glb'
body['original_texture_preserved']=True
body['original_glb_unchanged']=True
PARTS.append(body)
halo(cx,copper,copper_trim,cyan,dark)
hands(dark,cyan)
back_details(cx,dark,ivory,cyan,body)
report={'source':'output/higgsfield-hollow-saint/hollow-saint-sam3d.glb',
        'deleted_source_faces':removed,'cut_caps':len(filled),'cut_boundary_loops':boundary_report,'posterior_reassigned_faces':recolored,
        'status':'Hybrid visual study; unrigged, untested in game',
        'texture':'Original packed 1024px HF texture retained on preserved body faces',
        'halo_center_x':cx,'halo_center_z':1.79,'halo_outer_radius':.433,
        'fingers_per_hand':5}
present(ROOT,OUT,PARTS,report,'v6')
