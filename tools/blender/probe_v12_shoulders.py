"""Read-only: spherical raycast of the rest-pose shoulder shells in v11 to plan v12 pauldrons."""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

if not bpy.app.background:
    raise RuntimeError('Use isolated --background only')
ROOT=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'art/hybrid/hollow-saint-hybrid-v11.blend'))
rig=bpy.data.objects['Hollow Saint | v8 rig']
rig.data.pose_position='REST'
bpy.context.view_layer.update()
body=bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
verts=[body.matrix_world@v.co for v in body.data.vertices]
bvh=BVHTree.FromPolygons(verts,[tuple(p.vertices) for p in body.data.polygons])
mat_of=[p.material_index for p in body.data.polygons]
out={}
for side,sx in (('L',1),('R',-1)):
    region=[v for v in verts if v.z>1.30 and sx*(v.x+0.0375)>0.12]
    top=max(region,key=lambda v:v.z)
    outer=max(region,key=lambda v:sx*v.x)
    centre=Vector((sx*0.215-0.0375,-0.01,1.44))
    rows=[]
    for ti in range(0,121,15):
        row=[]
        for pi in range(-90,91,30):
            t,p=math.radians(ti),math.radians(pi)
            d=Vector((sx*math.sin(t)*math.cos(p),math.sin(t)*math.sin(p),math.cos(t)))
            loc,n,idx,_=bvh.ray_cast(centre+d*.4,-d,.8)
            row.append(None if loc is None else {'r':round((loc-centre).length,3),'y':round(loc.y,3),'z':round(loc.z,3),'x':round(loc.x,3),'mat':mat_of[idx]})
        rows.append({'theta':ti,'hits':row})
    out[side]={'top':[round(c,3) for c in top],'outer':[round(c,3) for c in outer],'centre':list(centre),'rays':rows}
(ROOT/'art/hybrid/v11/shoulder-probe.json').write_text(json.dumps(out,indent=1),encoding='utf-8')
for side in out:
    print(side,'top',out[side]['top'],'outer',out[side]['outer'])
    for r in out[side]['rays']:
        print(' theta',r['theta'],' '.join('----' if h is None else f"{h['r']:.3f}/{h['mat']}" for h in r['hits']))
