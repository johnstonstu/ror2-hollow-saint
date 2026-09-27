"""Read-only: front/back raycast map of the v10 rest-pose body with texture colour classes."""
import bpy
import json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

if not bpy.app.background:
    raise RuntimeError('Use isolated --background only')
ROOT=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'art/hybrid/hollow-saint-hybrid-v10.blend'))
scene=bpy.context.scene
rig=bpy.data.objects['Hollow Saint | v8 rig']
rig.data.pose_position='REST'
bpy.context.view_layer.update()
body=bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
dg=bpy.context.evaluated_depsgraph_get()
ev=body.evaluated_get(dg)
mesh=ev.to_mesh()
mesh.transform(body.matrix_world)
mesh.calc_loop_triangles()
verts=[v.co.copy() for v in mesh.vertices]
tris=[tuple(t.vertices) for t in mesh.loop_triangles]
bvh=BVHTree.FromPolygons(verts,tris)
uv=mesh.uv_layers.active.data
image=None
for m in body.data.materials:
    if m and m.use_nodes:
        for n in m.node_tree.nodes:
            if n.type=='TEX_IMAGE' and n.image:
                image=n.image
                break
    if image:break
W,H=image.size
px=image.pixels[:]

def colour(tri_index,hit):
    t=mesh.loop_triangles[tri_index]
    a,b,c=(verts[i] for i in t.vertices)
    from mathutils.geometry import barycentric_transform
    uvs=[uv[l].uv for l in t.loops]
    p=barycentric_transform(hit,a,b,c,Vector((*uvs[0],0)),Vector((*uvs[1],0)),Vector((*uvs[2],0)))
    x=min(W-1,max(0,int(p.x*W)));y=min(H-1,max(0,int(p.y*H)))
    i=(y*W+x)*4
    return px[i:i+3],mesh.polygons[t.polygon_index].material_index

def classify(rgb,mat):
    if mat==2:return 'o'
    if mat in (1,3):return '#'
    r,g,b=rgb
    lum=.3*r+.59*g+.11*b
    if b>r*1.6 and g>r*1.3 and b>.25:return '*'
    if r>g*1.25 and r>b*1.6 and lum>.12:return 'c'
    if lum>.45:return 'o'
    if lum<.10:return '#'
    return '+'

report={}
for label,origin_y,direction in [('front',-3.0,Vector((0,1,0))),('back',3.0,Vector((0,-1,0)))]:
    rows=[]
    ydata={}
    z=2.30
    while z>0.0:
        row=''
        x=-0.62
        while x<0.58:
            loc,normal,index,dist=bvh.ray_cast(Vector((x,origin_y,z)),direction,10)
            if loc is None:
                row+=' '
            else:
                rgb,mat=colour(index,loc)
                ch=classify(rgb,mat)
                row+=ch
                ydata[f'{x:.2f},{z:.2f}']=[round(loc.y,4),ch]
            x+=0.02
        rows.append(f'{z:5.2f} '+row)
        z-=0.02
    report[label]=ydata
    print('MAP',label)
    print('\n'.join(rows))
(ROOT/'art/hybrid/v11').mkdir(parents=True,exist_ok=True)
(ROOT/'art/hybrid/v11/surface-probe.json').write_text(json.dumps(report),encoding='utf-8')
cams={o.name:{'ortho':o.data.ortho_scale if o.data.type=='ORTHO' else None,'lens':o.data.lens,'type':o.data.type} for o in scene.objects if o.type=='CAMERA'}
print('CAMS',json.dumps(cams))
print('WORLD',scene.world.node_tree.nodes.keys() if scene.world and scene.world.use_nodes else None)
ev.to_mesh_clear()
