"""Seat small conductors on final surfaces, avoiding float after proportion fitting."""
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree


def seat_inlays(parts):
    bpy.context.view_layer.update()
    for obj in parts:
        if obj.type!='CURVE': continue
        name=obj.name
        back=name.startswith('Back')
        if 'tabard | conductor' in name:
            targets=[p for p in parts if p.type=='MESH' and p.name.startswith('Back' if back else 'Front') and 'tabard | folded' in p.name]
        elif name=='Back | spine filament':
            targets=[p for p in parts if p.name=='Torso | shaped graphite understructure']
        elif name=='Throat | cyan filament':
            targets=[p for p in parts if p.name=='Neck | flared sinew']
        else: continue
        vertices=[]
        faces=[]
        for target in targets:
            offset=len(vertices)
            vertices += [target.matrix_world@v.co for v in target.data.vertices]
            faces += [tuple(offset+i for i in p.vertices) for p in target.data.polygons]
        tree=BVHTree.FromPolygons(vertices,faces)
        spline=obj.data.splines[0]
        old=[Vector(p.co[:3]) for p in spline.points]
        if len(old)<8:
            coords=[]
            for a,b in zip(old,old[1:]):
                coords.extend(a.lerp(b,i/12) for i in range(12))
            coords.append(old[-1])
            obj.data.splines.remove(spline)
            spline=obj.data.splines.new('POLY')
            spline.points.add(len(coords)-1)
        else: coords=old
        sign=1 if back else -1
        for point,co in zip(spline.points,coords):
            hit,normal,idx,dist=tree.ray_cast(Vector((co.x,sign*2,co.z)),Vector((0,-sign,0)))
            if hit is None:
                hit,normal,idx,dist=tree.find_nearest(co)
            if hit is None:
                raise RuntimeError('Cannot seat conductor '+name)
            # Cloth has 3mm Solidify thickness; seat the motif outside its final skin.
            clearance=.004 if 'tabard | conductor' in name else .001
            co.y=hit.y+sign*clearance
            point.co=(*co,1)
