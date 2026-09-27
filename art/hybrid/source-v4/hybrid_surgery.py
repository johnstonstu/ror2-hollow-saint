"""Bounded geometry cuts with exact boundary interpolation and retained body UVs."""
import bpy
import bmesh
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
            ((0,0,1.803),(0,0,1))]
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
        if co.z>1.803 and co.y>.025 and abs(co.x-cx)<.12:
            face.material_index=2
            face.smooth=True
    return recolored
