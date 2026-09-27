import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

root = Path(__file__).resolve().parent
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(root / 'hollow-saint-sam3d.glb'))
meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
lo = Vector([min(p[i] for p in points) for i in range(3)])
hi = Vector([max(p[i] for p in points) for i in range(3)])
center = (lo + hi) / 2
extent = max(hi - lo)
report = {
    'mesh_objects': len(meshes),
    'vertices': sum(len(o.data.vertices) for o in meshes),
    'polygons': sum(len(o.data.polygons) for o in meshes),
    'armatures': sum(o.type == 'ARMATURE' for o in bpy.context.scene.objects),
    'dimensions': list(hi - lo),
    'objects': [o.name for o in meshes],
    'embedded_images': len(bpy.data.images),
}
(root / 'inspection.json').write_text(json.dumps(report, indent=2))
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 16
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.world = bpy.data.worlds.new('Preview World')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (0.23, 0.23, 0.23, 1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = 0.7
scene.view_settings.view_transform = 'Standard'
cam_data = bpy.data.cameras.new('Preview Camera')
cam = bpy.data.objects.new('Preview Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam_data.type = 'ORTHO'
cam_data.ortho_scale = extent * 1.3
cam_data.clip_end = max(100, extent * 20)
for i, offset in enumerate([(2, -3, 4), (-3, -1, 2), (1, 3, 3)]):
    data = bpy.data.lights.new('Preview Light ' + str(i), 'AREA')
    data.energy = extent * extent * (350 if i == 0 else 180)
    data.shape = 'DISK'
    data.size = extent * 2
    light = bpy.data.objects.new(data.name, data)
    scene.collection.objects.link(light)
    light.location = center + Vector(offset) * extent
    light.rotation_euler = (center - light.location).to_track_quat('-Z', 'Y').to_euler()
for name, direction in [('front', (0, -1, 0.05)), ('back', (0, 1, 0.05)), ('side', (1, 0, 0.05))]:
    cam.location = center + Vector(direction).normalized() * extent * 4
    cam.rotation_euler = (center - cam.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.filepath = str(root / ('preview-' + name + '.png'))
    bpy.ops.render.render(write_still=True)
print(json.dumps(report))
