"""Inspect supplied GLB and render it in the local model's studio; source unchanged."""
import bpy
import bmesh
import json
from pathlib import Path
from mathutils import Vector

if not bpy.app.background:
    raise RuntimeError('Run in isolated background Blender only')
root = Path(__file__).resolve().parents[2]
out = root / 'art' / 'comparison'
out.mkdir(parents=True, exist_ok=True)
source = root / 'output' / 'higgsfield-hollow-saint' / 'hollow-saint-sam3d.glb'
template = root / 'art' / 'refinement' / 'hollow-saint-refinement-v5.blend'
bpy.ops.wm.open_mainfile(filepath=str(template))
bpy.context.preferences.filepaths.save_version = 0
for collection in list(bpy.data.collections):
    if collection.name.startswith('HOLLOW SAINT |'):
        for obj in list(collection.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(collection)
before = set(bpy.data.objects)
bpy.ops.import_scene.gltf(filepath=str(source))
imported = [o for o in bpy.data.objects if o not in before]
meshes = [o for o in imported if o.type == 'MESH']
assert meshes, 'Supplied GLB contains no meshes'
bpy.context.view_layer.update()
points = [o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
lo = Vector([min(p[i] for p in points) for i in range(3)])
hi = Vector([max(p[i] for p in points) for i in range(3)])
scale = 2.2 / (hi.z - lo.z)
center = (lo + hi) / 2
records = []
for obj in meshes:
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    unseen = set(bm.verts)
    components = []
    while unseen:
        queue = [unseen.pop()]
        size = 0
        while queue:
            v = queue.pop()
            size += 1
            for edge in v.link_edges:
                other = edge.other_vert(v)
                if other in unseen:
                    unseen.remove(other)
                    queue.append(other)
        components.append(size)
    obj.data.calc_loop_triangles()
    records.append({
        'name': obj.name, 'vertices': len(obj.data.vertices),
        'polygons': len(obj.data.polygons), 'triangles': len(obj.data.loop_triangles),
        'uv_layers': len(obj.data.uv_layers), 'material_slots': len(obj.material_slots),
        'connected_components': len(components), 'largest_components_vertices': sorted(components, reverse=True)[:15],
        'boundary_edges': sum(e.is_boundary for e in bm.edges),
        'nonmanifold_edges': sum(not e.is_manifold for e in bm.edges),
        'zero_area_faces': sum(f.calc_area() < 1e-12 for f in bm.faces),
        'vertex_groups': len(obj.vertex_groups), 'modifiers': [m.type for m in obj.modifiers]
    })
    bm.free()
    # Normalize the overall halo-to-ground height, preserving source shape/texture.
    matrix = obj.matrix_world.copy()
    obj.parent = None
    obj.matrix_world.identity()
    for vertex in obj.data.vertices:
        co = matrix @ vertex.co
        vertex.co = ((co.x-center.x)*scale, (co.y-center.y)*scale, (co.z-lo.z)*scale+.008)
    obj['source'] = str(source)
    obj['status'] = 'Imported generated starter; unmodified topology, normalized display scale only'

scene = bpy.context.scene
scene['status'] = 'Supplied Higgsfield SAM3D inspection; no rig or game validation'
scene['reference'] = str(source)
scene.cycles.samples = 40
textures = [i for i in bpy.data.images if i.name != 'Render Result']
report = {
    'source': str(source), 'source_dimensions': list(hi-lo), 'display_total_height_m': 2.2,
    'meshes': records, 'armatures': sum(o.type == 'ARMATURE' for o in imported),
    'actions': len(bpy.data.actions),
    'images': [{'name': i.name, 'size': list(i.size), 'packed': bool(i.packed_file)} for i in textures],
    'comparison': 'Same studio/cameras as local v5; normalized overall height; original textures retained.'
}
(out/'higgsfield-inspection.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(out/'higgsfield-sam3d-review.blend'))
for suffix, camera in [('front','Front orthographic'), ('hero','Hero three quarter'),
                       ('side','Side orthographic'), ('back','Back orthographic'),
                       ('gameplay-distance','Simulated gameplay distance')]:
    scene.camera = bpy.data.objects[camera]
    scene.render.resolution_x = 1500 if suffix == 'gameplay-distance' else 1100
    scene.render.resolution_y = 1000 if suffix == 'gameplay-distance' else 1400
    scene.render.filepath = str(out/('higgsfield-sam3d-'+suffix+'.png'))
    bpy.ops.render.render(write_still=True)
    print('COMPARISON_RENDER '+suffix, flush=True)
clay = bpy.data.materials.new('Inspection clay — geometry only')
clay.diffuse_color = (.42,.42,.42,1)
clay.use_nodes = True
clay.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.42,.42,.42,1)
clay.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .8
for obj in meshes:
    obj.data.materials.clear()
    obj.data.materials.append(clay)
scene.camera = bpy.data.objects['Hero three quarter']
scene.render.resolution_x, scene.render.resolution_y = 1100, 1400
scene.render.filepath = str(out/'higgsfield-sam3d-clay.png')
bpy.ops.render.render(write_still=True)
print('INSPECTION '+json.dumps(report), flush=True)
