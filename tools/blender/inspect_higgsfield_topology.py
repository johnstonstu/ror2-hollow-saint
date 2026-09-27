"""Read-only topology audit distinguishes glTF seam splits from geometric gaps."""
import bpy
import bmesh
import json
from pathlib import Path

if not bpy.app.background:
    raise RuntimeError('Use isolated background Blender')
root = Path(__file__).resolve().parents[2]
source = root/'output'/'higgsfield-hollow-saint'/'hollow-saint-sam3d.glb'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
records = []
for obj in bpy.context.scene.objects:
    if obj.type != 'MESH':
        continue
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-6)
    unseen = set(bm.verts)
    sizes = []
    while unseen:
        queue = [unseen.pop()]
        count = 0
        while queue:
            vertex = queue.pop()
            count += 1
            for edge in vertex.link_edges:
                neighbor = edge.other_vert(vertex)
                if neighbor in unseen:
                    unseen.remove(neighbor)
                    queue.append(neighbor)
        sizes.append(count)
    records.append({
        'object': obj.name, 'coincident_vertex_weld_tolerance_source_units': 1e-6,
        'welded_vertices': len(bm.verts), 'components_after_weld': len(sizes),
        'component_vertex_counts': sorted(sizes, reverse=True),
        'boundary_edges_after_weld': sum(e.is_boundary for e in bm.edges),
        'nonmanifold_edges_after_weld': sum(not e.is_manifold for e in bm.edges),
        'note': 'Welding performed on temporary analysis BMesh only; GLB unchanged.'
    })
    bm.free()
materials = []
for material in bpy.data.materials:
    if not material.use_nodes:
        continue
    for node in material.node_tree.nodes:
        if node.type == 'BSDF_PRINCIPLED':
            materials.append({
                'name': material.name,
                'base_color_texture_linked': node.inputs['Base Color'].is_linked,
                'emission_color': list(node.inputs['Emission Color'].default_value),
                'emission_color_linked': node.inputs['Emission Color'].is_linked,
                'emission_strength': node.inputs['Emission Strength'].default_value,
                'metallic': node.inputs['Metallic'].default_value,
                'roughness': node.inputs['Roughness'].default_value
            })
report = {'topology': records, 'materials': materials}
(root/'art'/'comparison'/'higgsfield-topology-audit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
