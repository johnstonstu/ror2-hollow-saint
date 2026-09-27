"""Bounded geometry cuts with exact boundary interpolation and retained body UVs."""
import bpy
import bmesh
import math
from mathutils import Vector


def trim_posterior_shoulders(body,cx):
    for s in [-1,1]:
        lo=cx+.175 if s==1 else cx-.49
        hi=cx+.49 if s==1 else cx-.175
        bpy.ops.mesh.primitive_cube_add(size=1,location=((lo+hi)/2,.3025,1.615))
        cutter=bpy.context.object
        cutter.name='Temporary bounded posterior shoulder plane'
        cutter.scale=(hi-lo,.495,.33)
        for mat in body.data.materials:cutter.data.materials.append(mat)
        for poly in cutter.data.polygons:poly.material_index=2
        bpy.context.view_layer.objects.active=body
        modifier=body.modifiers.new('Clean posterior shoulder cut '+str(s),'BOOLEAN')
        modifier.operation='DIFFERENCE'
        modifier.solver='EXACT'
        modifier.object=cutter
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        bpy.data.objects.remove(cutter,do_unlink=True)


def semantic_boundaries(bm,cx):
    planes=[((0,.055,0),(0,1,0)),((0,0,1.255),(0,0,1)),
            ((0,0,1.715),(0,0,1)),((cx-.205,0,0),(1,0,0)),
            ((cx+.205,0,0),(1,0,0)),((0,.025,0),(0,1,0)),
            ((0,0,1.776),(0,0,1))]
    for point,normal in planes:
        faces=[f for f in bm.faces if f.calc_center_median().z>1.22
               and abs(f.calc_center_median().x-cx)<.24]
        edges=set(e for f in faces for e in f.edges)
        verts=set(v for f in faces for v in f.verts)
        bmesh.ops.bisect_plane(bm,geom=list(verts)+list(edges)+faces,
                              plane_co=point,plane_no=normal,dist=.0000001)
    bm.normal_update()
    recolored=0
    for face in bm.faces:
        co=face.calc_center_median()
        if 1.255<co.z<1.715 and co.y>.055 and abs(co.x-cx)<.205:
            face.material_index=1
            face.smooth=True
            recolored+=1
        if co.z>1.776 and co.y>.025 and abs(co.x-cx)<.12:
            face.material_index=2
            face.smooth=True
    return recolored


def dome_shoulder_surfaces(bm,cx):
    def is_cap(face):
        co=face.calc_center_median()
        return abs(co.x-cx)>.174 and 1.449<co.z<1.781 and all(abs(v.co.y-.055)<.00002 for v in face.verts)
    caps=[f for f in bm.faces if is_cap(f)]
    if not caps:raise RuntimeError('No exact posterior shoulder cap surfaces found')
    # The ceramic leaf ends diagonally; do not turn the upper-arm rear into a slab.
    for s in [-1,1]:
        selected=[f for f in caps if (f.calc_center_median().x-cx)*s>0]
        edges=set(e for f in selected for e in f.edges)
        verts=set(v for f in selected for v in f.verts)
        bmesh.ops.bisect_plane(bm,geom=list(verts)+list(edges)+selected,
                              plane_co=(cx,0,1.72),plane_no=(s*.5,0,1),dist=.0000001)
    caps=[f for f in bm.faces if is_cap(f)]
    bmesh.ops.triangulate(bm,faces=caps)
    caps=[f for f in bm.faces if is_cap(f)]
    edges=set(e for f in caps for e in f.edges)
    bmesh.ops.subdivide_edges(bm,edges=list(edges),cuts=2,use_grid_fill=True)
    caps=set(f for f in bm.faces if is_cap(f))
    borders=[e for f in caps for e in f.edges if any(other not in caps for other in e.link_faces)]
    segments=[(Vector((e.verts[0].co.x,e.verts[0].co.z)),Vector((e.verts[1].co.x,e.verts[1].co.z))) for e in borders]
    if not segments:raise RuntimeError('No cap boundary found for fixed-edge dome')
    for v in set(v for f in caps for v in f.verts):
        p=Vector((v.co.x,v.co.z))
        distances=[]
        for a,b in segments:
            d=b-a
            t=max(0,min(1,(p-a).dot(d)/max(d.length_squared,1e-15)))
            distances.append((p-(a+d*t)).length)
        height=.026*math.sin(min(1,min(distances)/.07)*math.pi/2)
        v.co.y+=height
    for f in caps:
        co=f.calc_center_median()
        f.material_index=2 if co.z+.5*abs(co.x-cx)>1.72 else 3
        f.smooth=True
