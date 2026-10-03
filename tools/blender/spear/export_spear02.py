"""Preserve FBX bone axes by exporting the hand socket with its original rig."""
import bpy
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'HollowSaintUnityProject/Assets/HollowSaint/Source/spear02'
assert bpy.app.background and not OUT.exists()
OUT.mkdir(parents=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'art/anim/hollow-saint-anim-v37.blend'))
rig = bpy.data.objects['Hollow Saint | v8 rig']
rig.animation_data.action = None
for track in rig.animation_data.nla_tracks:
    track.mute = True
for bone in rig.pose.bones:
    bone.matrix_basis.identity()
socket = bpy.data.objects['Spear grip socket — hand locked']
socket.name = 'SpearGripSocket'
parts = list(socket.children)
for name, pos in [('SpearTip', (0, 0, 1.30)), ('SpearTail', (0, 0, -0.84)), ('SpearContact', (0.025, -0.02, 0.04))]:
    marker = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(marker)
    marker.parent = socket
    marker.location = Vector(pos)
    parts.append(marker)
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT')
for ob in [rig, socket] + parts:
    ob.hide_set(False)
    ob.select_set(True)
bpy.context.view_layer.objects.active = rig
# Curves must be converted explicitly; FBX does not export them as object_types MESH.
for ob in parts:
    if ob.type == 'CURVE':
        bpy.ops.object.select_all(action='DESELECT')
        ob.select_set(True)
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.convert(target='MESH')
        ob.hide_set(False)
bpy.ops.object.select_all(action='DESELECT')
for ob in [rig, socket] + list(socket.children):
    ob.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=str(OUT / 'ConduitSpear.fbx'), use_selection=True,
    object_types={'ARMATURE', 'MESH', 'EMPTY'}, bake_anim=False,
    axis_forward='-Z', axis_up='Y', global_scale=1, apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS', add_leaf_bones=False,
    use_armature_deform_only=False, use_custom_props=True)
print('SPEAR_SOCKET_EXPORTED', flush=True)
