"""Export the fitted assembly and rest-pose grip calibration, never edit source."""
import bpy
import json
from pathlib import Path
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'HollowSaintUnityProject/Assets/HollowSaint/Source/spear01'
assert bpy.app.background and not OUT.exists()
OUT.mkdir(parents=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'art/anim/hollow-saint-anim-v37.blend'))
rig = bpy.data.objects['HS_RIG'] if 'HS_RIG' in bpy.data.objects else next(o for o in bpy.data.objects if o.type == 'ARMATURE')
socket = bpy.data.objects['Spear grip socket — hand locked']
bpy.context.view_layer.update()
hand_pose = rig.matrix_world @ rig.pose.bones['R hand'].matrix
local = hand_pose.inverted() @ socket.matrix_world
rest = rig.matrix_world @ rig.data.bones['R hand'].matrix_local
# Blender world -> Unity world: X right, Z up, -Y forward.
conversion = Matrix(((1, 0, 0, 0), (0, 0, 1, 0), (0, -1, 0, 0), (0, 0, 0, 1)))
desired = conversion @ rest @ local @ conversion.inverted()
report = {'socketRestWorld': [x for row in desired for x in row],
          'tip': [0, 1.30, 0], 'tail': [0, -0.84, 0], 'contact': [0.025, 0.04, 0.02],
          'source': 'hollow-saint-anim-v37.blend', 'parts': []}
inverse = socket.matrix_world.inverted()
deps = bpy.context.evaluated_depsgraph_get()
parts = []
for original in socket.children:
    if original.type not in ('CURVE', 'MESH'):
        continue
    evaluated = original.evaluated_get(deps)
    mesh = bpy.data.meshes.new_from_object(evaluated, depsgraph=deps)
    mesh.transform(inverse @ original.matrix_world)
    copy = bpy.data.objects.new(original.name, mesh)
    bpy.context.scene.collection.objects.link(copy)
    parts.append(copy)
    report['parts'].append({'name': original.name, 'triangles': sum(len(p.vertices) - 2 for p in mesh.polygons)})
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.export_scene.fbx(filepath=str(OUT / 'ConduitSpear.fbx'), use_selection=True,
                         object_types={'MESH'}, bake_anim=False, global_scale=1,
                         apply_unit_scale=True, axis_forward='-Z', axis_up='Y',
                         use_mesh_modifiers=True, add_leaf_bones=False)
(OUT / 'grip-calibration.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('SPEAR_EXPORTED', len(parts), sum(p['triangles'] for p in report['parts']), flush=True)
