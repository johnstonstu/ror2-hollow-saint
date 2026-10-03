"""Export every v31 clip as an armature-only FBX for the game bundle.

Read-only on the .blend (never saved). Writes a new numbered folder under
HollowSaintUnityProject/Assets/HollowSaint/Source/ and refuses to overwrite one.
Uses the same FBX options as export_unity_probe.py (v31_probe03), whose model and
avatar the clips bind to, so bone paths match the shipped model exactly.

Usage:
  blender --background --factory-startup --python-exit-code 1 --python tools/blender/export_unity_clips.py -- v31_clips02
Optional second/third args select a preserved source .blend and semicolon-separated
clip titles; bundle04 uses v32_clips01 art/anim/hollow-saint-anim-v32.blend "Run forward".
"""
import json
import re
import sys
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[2]
args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
SOURCE = ROOT / (args[1] if len(args) > 1 else 'art/anim/hollow-saint-anim-v31.blend')
name = args[0] if args else 'v31_clips02'
wanted = set(args[2].split(';')) if len(args) > 2 else None
OUT = ROOT / 'HollowSaintUnityProject/Assets/HollowSaint/Source' / name
assert bpy.app.background
assert not OUT.exists(), 'Choose a new numbered export folder; do not overwrite: ' + str(OUT)
OUT.mkdir(parents=True)

bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
rig = bpy.data.objects['Hollow Saint | v8 rig']
scene = bpy.context.scene
for track in rig.animation_data.nla_tracks:
    track.mute = True
rig.data.pose_position = 'POSE'

options = dict(use_selection=True, object_types={'ARMATURE', 'MESH', 'EMPTY'},
               axis_forward='-Z', axis_up='Y', global_scale=1.0, apply_unit_scale=True,
               apply_scale_options='FBX_SCALE_UNITS', add_leaf_bones=False,
               use_armature_deform_only=False, use_custom_props=True,
               bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
               bake_anim_use_all_bones=True, bake_anim_force_startend_keying=True,
               bake_anim_step=1.0, bake_anim_simplify_factor=0.0, path_mode='AUTO')

clips = []
actions = sorted((a for a in bpy.data.actions if a.name.startswith('HS_anim | ')), key=lambda a: a.name)
for action in actions:
    title = action.name[len('HS_anim | '):]
    if wanted and title not in wanted:
        continue
    info = json.loads(action['clip_json'])
    rig.animation_data.action = action
    if hasattr(rig.animation_data, 'action_slot') and action.slots:
        rig.animation_data.action_slot = action.slots[0]
    start, end = info['frames']
    scene.frame_start, scene.frame_end = start, end
    scene.frame_set(start)
    bpy.ops.object.select_all(action='DESELECT')
    rig.hide_set(False)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    filename = re.sub(r'[^a-zA-Z0-9]+', '_', title).strip('_') + '.fbx'
    bpy.ops.export_scene.fbx(filepath=str(OUT / filename), bake_anim=True, **options)
    clips.append({'title': title, 'file': filename, 'start': start, 'end': end,
                  'loop': bool(info.get('loop')), 'fps': 24,
                  'markers': [{'name': k, 'frame': v} for k, v in info.get('markers', {}).items()]})
    print('EXPORTED', title, flush=True)

(OUT / 'clips-manifest.json').write_text(json.dumps({
    'source': str(SOURCE.relative_to(ROOT)), 'modelFrom': 'v31_probe03', 'clips': clips}, indent=1))
print('DONE', len(clips), flush=True)
