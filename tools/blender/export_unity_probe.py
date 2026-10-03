"""Export a numbered Unity import proof from v31 without modifying the .blend."""
import json
import re
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'art/anim/hollow-saint-anim-v31.blend'
OUT = ROOT / 'HollowSaintUnityProject/Assets/HollowSaint/Source/v31_probe03'
TITLES = ['Idle', 'Run forward', 'Run start', 'Run stop', 'Jump', 'Ascend', 'Descend', 'Land',
          'Glide enter', 'Glide loop', 'Glide exit', 'Arc Step left start',
          'Arc Step left loop', 'Arc Step left end', 'Arc Bolt right']
PROPS = ['hs_glow', 'hs_jet', 'hs_spark_L', 'hs_spark_R', 'hs_jet_dir',
         'hs_move_x', 'hs_move_y', 'hs_turn', 'hs_spear']
assert bpy.app.background
assert not OUT.exists(), 'Choose a new numbered export folder; do not overwrite a proof'
OUT.mkdir(parents=True)
stamp = SOURCE.stat().st_mtime_ns
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
rig = bpy.data.objects['Hollow Saint | v8 rig']
body = bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
for index, material in enumerate(list(body.data.materials)):
    copied = material.copy()
    copied.name = material.name + ' Unity body'
    body.data.materials[index] = copied
scene = bpy.context.scene
for track in rig.animation_data.nla_tracks:
    track.mute = True
rig.animation_data.action = None
for bone in rig.pose.bones:
    bone.matrix_basis.identity()
rig.data.pose_position = 'REST'
bpy.context.view_layer.update()


def select(objects):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.hide_set(False)
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig


def bake_body(channel):
    """Evaluate the body shader into its existing UV layout, independent of lighting."""
    select([body])
    bpy.context.view_layer.objects.active = body
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 1
    scene.cycles.device = 'CPU'
    image = bpy.data.images.new('Unity body ' + channel, 1024, 1024, alpha=False)
    restore = []
    for material in body.data.materials:
        nodes, links = material.node_tree.nodes, material.node_tree.links
        principled = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
        output = next(n for n in nodes if n.type == 'OUTPUT_MATERIAL' and n.is_active_output)
        original = output.inputs['Surface'].links[0].from_socket
        emission = nodes.new('ShaderNodeEmission')
        value = principled.inputs['Base Color' if channel == 'base' else 'Emission Color']
        if value.is_linked:
            links.new(value.links[0].from_socket, emission.inputs['Color'])
        else:
            emission.inputs['Color'].default_value = value.default_value
        if channel == 'emission':
            strength = principled.inputs['Emission Strength']
            if strength.is_linked:
                links.new(strength.links[0].from_socket, emission.inputs['Strength'])
            else:
                emission.inputs['Strength'].default_value = strength.default_value
        links.new(emission.outputs[0], output.inputs['Surface'])
        target = nodes.new('ShaderNodeTexImage')
        target.image = image
        nodes.active = target
        restore.append((material, output, original, emission, target))
    bpy.ops.object.bake(type='EMIT', margin=8, use_clear=True)
    image.filepath_raw = str(OUT / ('body_' + channel + '.png'))
    image.file_format = 'PNG'
    image.save()
    for material, output, original, emission, target in restore:
        material.node_tree.links.new(original, output.inputs['Surface'])
        material.node_tree.nodes.remove(emission)
        material.node_tree.nodes.remove(target)
    return 'body_' + channel + '.png'


base_map, emission_map = bake_body('base'), bake_body('emission')
objects = [rig] + [o for o in rig.children_recursive if o.type in {'MESH', 'EMPTY'}
                  and not o.hide_render and not o.name.startswith('VFX |')]
material_set = {slot.material for o in objects if o.type == 'MESH' for slot in o.material_slots if slot.material}
materials = []
for material in sorted(material_set, key=lambda m: m.name):
    p = next((n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if p is None:
        raise RuntimeError('Unsupported exported material: ' + material.name)
    color = list(p.inputs['Base Color'].default_value)
    # Procedural armor remains an explicit approximation in this first import proof.
    if 'ivory' in material.name.lower() or 'ceramic plate' in material.name:
        color = [0.72, 0.68, 0.56, 1]
    if 'aged copper' in material.name:
        color = [0.30, 0.125, 0.055, 1]
    is_body = material.name in {m.name for m in body.data.materials}
    materials.append({'name': material.name, 'color': color,
                      'metallic': p.inputs['Metallic'].default_value,
                      'roughness': p.inputs['Roughness'].default_value,
                      'emission': list(p.inputs['Emission Color'].default_value),
                      'emissionStrength': p.inputs['Emission Strength'].default_value,
                      'baseMap': base_map if is_body else '',
                      'emissionMap': emission_map if is_body else ''})

options = dict(use_selection=True, object_types={'ARMATURE', 'MESH', 'EMPTY'},
               axis_forward='-Z', axis_up='Y', global_scale=1.0, apply_unit_scale=True,
               apply_scale_options='FBX_SCALE_UNITS', add_leaf_bones=False,
               use_armature_deform_only=False, use_custom_props=True,
               bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
               bake_anim_use_all_bones=True, bake_anim_force_startend_keying=True,
               bake_anim_step=1.0, bake_anim_simplify_factor=0.0, path_mode='AUTO')
select(objects)
bpy.ops.export_scene.fbx(filepath=str(OUT / 'HollowSaint.fbx'), bake_anim=False, **options)
rig.data.pose_position = 'POSE'
clips = []
for title in TITLES:
    action = bpy.data.actions['HS_anim | ' + title]
    info = json.loads(action['clip_json'])
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    start, end = info['frames']
    scene.frame_start, scene.frame_end = start, end
    curves = [{'name': key, 'values': []} for key in PROPS]
    samples = []
    for frame in range(start, end + 1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        for curve in curves:
            curve['values'].append(float(rig.pose.bones['root'].get(curve['name'], 0)))
        samples.append({'frame': frame, 'bones': [
            {'name': name, 'position': list(rig.matrix_world @ rig.pose.bones[name].matrix.translation)}
            for name in ['L muzzle', 'R muzzle', 'core socket', 'L heel socket', 'R heel socket', 'tabard front.3']]})
    filename = re.sub(r'[^a-zA-Z0-9]+', '_', title).strip('_') + '.fbx'
    select([rig])
    bpy.ops.export_scene.fbx(filepath=str(OUT / filename), bake_anim=True, **options)
    clips.append({'title': title, 'file': filename, 'start': start, 'end': end,
                  'loop': info['loop'], 'fps': 24,
                  'markers': [{'name': k, 'frame': v} for k, v in info.get('markers', {}).items()],
                  'curves': curves, 'samples': samples})
    print('EXPORTED', title, flush=True)
manifest = {'source': str(SOURCE.relative_to(ROOT)), 'model': 'HollowSaint.fbx',
            'materials': materials, 'clips': clips,
            'bones': [b.name for b in rig.data.bones],
            'meshCount': sum(o.type == 'MESH' for o in objects),
            'limitations': ['Procedural armor noise approximated by fixed material colors.',
                           'Blender VFX meshes excluded; VFX curves exported as explicit sampled data.',
                           'Existing sockets retained; complete Phase E socket additions remain pending.']}
(OUT / 'export-manifest.json').write_text(json.dumps(manifest, indent=2))
assert SOURCE.stat().st_mtime_ns == stamp
print('EXPORT COMPLETE', len(clips), 'clips', manifest['meshCount'], 'meshes', flush=True)
