"""Editable surface-building helpers for the Hollow Saint refinement study."""
import bpy
import math
import random
from mathutils import Vector

PARTS = []


def material(name, color, metallic=0, roughness=.6, emission=0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bs = mat.node_tree.nodes.get('Principled BSDF')
    for key, value in [('Base Color', (*color, 1)), ('Metallic', metallic), ('Roughness', roughness)]:
        bs.inputs[key].default_value = value
    if emission:
        bs.inputs['Emission Color'].default_value = (*color, 1)
        bs.inputs['Emission Strength'].default_value = emission
    return mat


def mesh(name, vertices, faces, mat, bevel=0, smooth=False):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    data.materials.append(mat)
    for poly in data.polygons:
        poly.use_smooth = smooth
    if bevel:
        mod = obj.modifiers.new('Small ceramic edge radii', 'BEVEL')
        mod.width = bevel
        mod.segments = 2
    PARTS.append(obj)
    return obj


def loft(name, rings, mat, sides=12, smooth=False, bevel=0):
    """Rings are (x, y, z, half-width, half-depth); adjustable silhouette topology."""
    vertices = []
    for x, y, z, rx, ry in rings:
        vertices.extend((x + rx*math.cos(2*math.pi*i/sides),
                         y + ry*math.sin(2*math.pi*i/sides), z) for i in range(sides))
    faces = [tuple(reversed(range(sides)))]
    for j in range(len(rings)-1):
        for i in range(sides):
            faces.append((j*sides+i, j*sides+(i+1)%sides,
                          (j+1)*sides+(i+1)%sides, (j+1)*sides+i))
    faces.append(tuple(range((len(rings)-1)*sides, len(rings)*sides)))
    return mesh(name, vertices, faces, mat, bevel, smooth)


def rod(name, points, radius, mat):
    data = bpy.data.curves.new(name, 'CURVE')
    data.dimensions = '3D'
    data.resolution_u = 1
    data.bevel_depth = radius
    data.bevel_resolution = 1
    spline = data.splines.new('POLY')
    spline.points.add(len(points)-1)
    for p, co in zip(spline.points, points):
        p.co = (*co, 1)
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(mat)
    PARTS.append(obj)
    return obj


def bone(name, a, b, radius, mat, end=None):
    a, b = Vector(a), Vector(b)
    axis = b-a
    q = axis.to_track_quat('Z', 'Y')
    end = radius*.85 if end is None else end
    rings = [(0, radius*.8), (.16, radius), (.8, end), (1, end*.8)]
    vertices = [a+axis*t+q@Vector((r*math.cos(i*math.tau/8), r*math.sin(i*math.tau/8), 0))
                for t, r in rings for i in range(8)]
    faces = [tuple(reversed(range(8)))]
    for j in range(3):
        faces.extend((j*8+i, j*8+(i+1)%8, (j+1)*8+(i+1)%8, (j+1)*8+i) for i in range(8))
    faces.append(tuple(range(24, 32)))
    return mesh(name, vertices, faces, mat, .001)


def sphere(name, pos, scale, mat, subdivisions=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1, location=pos)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    PARTS.append(obj)
    return obj


def disc(name, pos, radius, depth, mat):
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=radius, depth=depth,
                                     location=pos, rotation=(math.pi/2, 0, 0))
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    mod = obj.modifiers.new('Rounded rim', 'BEVEL')
    mod.width = .002
    mod.segments = 2
    PARTS.append(obj)
    return obj


def shield(name, outline, depth, mat, ridge=.025):
    """Outlined ceramic plate with a raised inner contour instead of triangle fan."""
    center = Vector(tuple(sum(v[i] for v in outline)/len(outline) for i in range(3)))
    n = len(outline)
    inner = [center+(Vector(v)-center)*.58+Vector((0, -ridge, 0)) for v in outline]
    back = [Vector(v)+Vector((0, depth, 0)) for v in outline]
    verts = list(outline)+inner+back
    faces = [tuple(range(n, 2*n)), tuple(reversed(range(2*n, 3*n)))]
    for i in range(n):
        k=(i+1)%n
        faces += [(i, k, n+k, n+i), (i, 2*n+i, 2*n+k, k)]
    return mesh(name, verts, faces, mat, .002)


def facet_materials(obj, base_color):
    rng = random.Random(obj.name)
    for factor in [.96, 1.025, .985]:
        obj.data.materials.append(material(obj.name+' tonal ceramic '+str(factor),
                                          tuple(c*factor for c in base_color), .06, .48))
    for poly in obj.data.polygons:
        poly.material_index = rng.choice([0, 0, 0, 1, 2, 3])


def ribbon(name, rows, side, mat):
    """Curved armor band, explicit quad rows avoid a concave cap triangulation."""
    verts=[]
    for z,xl,xr,y in rows:
        for t in [0,.33,.67,1]:
            verts.append((side*(xl+(xr-xl)*t),y-.012*math.sin(t*math.pi),z))
    n=len(verts)
    verts += [(x,y+.035,z) for x,y,z in verts]
    faces=[]
    for row in range(len(rows)-1):
        for j in range(3):
            a=row*4+j
            faces += [(a,a+1,a+5,a+4),(n+a+4,n+a+5,n+a+1,n+a)]
    perimeter=list(range(4))+[r*4+3 for r in range(1,len(rows))]+list(range(n-2,n-5,-1))+[r*4 for r in reversed(range(1,len(rows)-1))]
    for a,b in zip(perimeter,perimeter[1:]+perimeter[:1]): faces.append((a,n+a,n+b,b))
    return mesh(name,verts,faces,mat,.002)


def toe(name, x, mat, side):
    rows=[(-.18,.027,.026,.023),(-.148,.042,.03,.031),
          (-.09,.057,.032,.041),(-.015,.088,.026,.06),(.055,.044,.024,.033)]
    verts=[]
    for y,z,rx,rz in rows:
        if 'outer' in name:
            y=.02+(y-.02)*.9
            rx*=.94
        for i in range(12):
            angle=i*math.tau/12
            verts.append((side*(x+rx*math.cos(angle)),y,max(.008,z+rz*math.sin(angle))))
    faces=[tuple(reversed(range(12)))]
    for j in range(len(rows)-1):
        for i in range(12):
            faces.append((j*12+i,j*12+(i+1)%12,(j+1)*12+(i+1)%12,(j+1)*12+i))
    faces.append(tuple(range(48,60)))
    return mesh(name,verts,faces,mat,.002)
