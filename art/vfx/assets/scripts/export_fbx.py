"""Export every VFX mesh asset to FBX (Unity-friendly) and verify by re-importing each file.

  blender.exe --background --factory-startup --python export_fbx.py -- --blend hs-vfx-v06.blend

Opens the .blend and never saves it. Existing FBX files are moved into _old/ first.
Axis: FBX -Z forward, Y up, Apply Transform (bake_space_transform) -> Blender -Y = Unity +Z, Blender +Z = Unity +Y.
Scale: FBX_SCALE_ALL + apply unit scale -> 1 Blender metre = 1 Unity unit with transform scale 1.
"""
import datetime
import json
import os
import shutil
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..'))
FBX = os.path.join(ROOT, 'fbx')

GROUPS = {
    'HS_arc_short.fbx': ['HS_arc_short_A', 'HS_arc_short_B', 'HS_arc_short_C'],
    'HS_bolt_long.fbx': ['HS_bolt_long_A', 'HS_bolt_long_B', 'HS_bolt_long_C'],
    'HS_bolt_branch.fbx': ['HS_bolt_branch_A', 'HS_bolt_branch_B'],
    'HS_halo_ring.fbx': ['HS_halo_ring_segments', 'HS_halo_gap_arc_1', 'HS_halo_gap_arc_2', 'HS_halo_gap_arc_3',
                         'HS_halo_gap_arc_4'],
    'HS_heel_jet.fbx': ['HS_jet_cone_outer', 'HS_jet_cone_core', 'HS_jet_arcs', 'HS_jet_ribbon'],
    'HS_jet_burst.fbx': ['HS_jet_burst_spikes', 'HS_jet_burst_ring'],
    'HS_spear.fbx': ['HS_spear_lance_core', 'HS_spear_lance_shell', 'HS_spear_trail'],
    'HS_spear_materialize.fbx': ['HS_spear_filaments'],
    'HS_conductor_mark.fbx': ['HS_mark_ring_segments', 'HS_mark_diamond', 'HS_mark_quad'],
    'HS_step_ground_trail.fbx': ['HS_step_ground_trail'],
    'HS_quads.fbx': ['HS_decal_quad_1m', 'HS_card_quad_1m'],
}


def arg(name, default=None):
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    return argv[argv.index(name) + 1] if name in argv else default


def fresh(path):
    if os.path.exists(path):
        stamp = datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
        dst = os.path.join(ROOT, '_old', 'fbx', f'{os.path.basename(path)}.{stamp}')
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.move(path, dst)
    return path


def export(blend):
    bpy.ops.wm.open_mainfile(filepath=blend)
    info = {}
    for fname, names in GROUPS.items():
        for o in bpy.context.view_layer.objects:
            o.select_set(False)
        objs = [bpy.data.objects[n] for n in names]
        for o in objs:
            o.hide_set(False)
            o.hide_viewport = False
            assert tuple(o.location) == (0, 0, 0) and tuple(o.rotation_euler) == (0, 0, 0) \
                and tuple(o.scale) == (1, 1, 1), f'{o.name} has a transform'
            o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        path = fresh(os.path.join(FBX, fname))
        bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH'},
                                 apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
                                 axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                                 use_mesh_modifiers=True, mesh_smooth_type='FACE', use_tspace=False,
                                 add_leaf_bones=False, bake_anim=False, path_mode='STRIP', embed_textures=False,
                                 colors_type='SRGB', use_custom_props=True)
        info[fname] = {o.name: dict(verts=len(o.data.vertices), tris=sum(len(p.vertices) - 2 for p in o.data.polygons),
                                    uv=[l.name for l in o.data.uv_layers],
                                    material=o.data.materials[0].name if o.data.materials else None,
                                    unity_use=o.get('unity_use', ''),
                                    size_m=[round(v, 3) for v in o.dimensions])
                       for o in objs}
    return info


def verify(info):
    report = {}
    for fname in GROUPS:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        path = os.path.join(FBX, fname)
        try:
            bpy.ops.import_scene.fbx(filepath=path)
            importer = 'import_scene.fbx'
        except Exception:
            bpy.ops.wm.fbx_import(filepath=path)
            importer = 'wm.fbx_import'
        rep = {'importer': importer, 'objects': {}}
        for o in bpy.context.scene.objects:
            if o.type != 'MESH':
                continue
            me = o.data
            bb = [o.matrix_world @ __import__('mathutils').Vector(c) for c in o.bound_box]
            ys = [p.y for p in bb]
            rep['objects'][o.name] = dict(
                verts=len(me.vertices), uv=[l.name for l in me.uv_layers],
                colors=[c.name for c in me.color_attributes],
                dims=[round(v, 3) for v in o.dimensions], scale=[round(v, 4) for v in o.scale],
                y_range=[round(min(ys), 3), round(max(ys), 3)])
        report[fname] = rep
    return report


def main():
    blend = os.path.join(ROOT, arg('--blend', 'hs-vfx-v07.blend'))
    os.makedirs(FBX, exist_ok=True)
    info = export(blend)
    report = verify(info)
    out = {'blend': os.path.basename(blend), 'export': info, 'reimport_check': report}
    path = fresh(os.path.join(FBX, 'fbx_manifest.json'))
    with open(path, 'w') as f:
        json.dump(out, f, indent=1)


main()
