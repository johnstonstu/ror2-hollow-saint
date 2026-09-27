"""Render preview frames for every VFX asset (background Blender; opens a .blend and NEVER saves it).

  blender.exe --background --factory-startup --python render_previews.py -- --blend hs-vfx-v03.blend [--only a,b]

Frames go to previews/frames/<group>/; post_previews.py (system Python) adds bloom, labels and GIFs.
"""
import json
import math
import os
import sys

import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..'))
FRAMES = os.path.join(ROOT, 'previews', 'frames')


def arg(name, default=None):
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    return argv[argv.index(name) + 1] if name in argv else default


def O(name):
    return bpy.data.objects[name]


def setup_scene():
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'
    try:
        sc.eevee.taa_render_samples = 32
    except Exception:
        pass
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = 'PNG'
    sc.render.image_settings.color_mode = 'RGB'
    if sc.world is None:
        sc.world = bpy.data.worlds.new('HS dark')
    w = sc.world
    w.color = (0.004, 0.005, 0.008)
    try:
        w.use_nodes = True
    except Exception:
        pass
    if w.node_tree:
        for n in w.node_tree.nodes:
            if n.type == 'BACKGROUND':
                n.inputs['Color'].default_value = (0.004, 0.005, 0.008, 1)
                n.inputs['Strength'].default_value = 1.0
    cam_data = bpy.data.cameras.new('PREVIEW cam')
    cam = bpy.data.objects.new('PREVIEW cam', cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    return sc, cam


def floor(size=400, z=0.0, color=(0.02, 0.02, 0.022)):
    me = bpy.data.meshes.new('PREVIEW floor')
    s = size / 2
    me.from_pydata([(-s, -s, z), (s, -s, z), (s, s, z), (-s, s, z)], [], [(0, 1, 2, 3)])
    m = bpy.data.materials.new('PREVIEW floor')
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    em = nt.nodes.new('ShaderNodeEmission')
    em.inputs['Color'].default_value = (*color, 1)
    em.inputs['Strength'].default_value = 1.0
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    nt.links.new(em.outputs[0], out.inputs['Surface'])
    me.materials.append(m)
    o = bpy.data.objects.new('PREVIEW floor', me)
    bpy.context.scene.collection.objects.link(o)
    return o


def frame_camera(cam, objs, view_dir, res, margin=1.12, ortho=True, fov_deg=35):
    sc = bpy.context.scene
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.resolution_percentage = 100
    bpy.context.view_layer.update()
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    center = sum(pts, Vector()) / len(pts)
    d = Vector(view_dir).normalized()
    rot = d.to_track_quat('-Z', 'Y')
    cam.rotation_mode = 'QUATERNION'
    cam.rotation_quaternion = rot
    right = rot @ Vector((1, 0, 0))
    up = rot @ Vector((0, 1, 0))
    xs = [(p - center).dot(right) for p in pts]
    ys = [(p - center).dot(up) for p in pts]
    zs = [(p - center).dot(d) for p in pts]
    cx, cy = (max(xs) + min(xs)) / 2, (max(ys) + min(ys)) / 2
    center = center + right * cx + up * cy
    w, h = max(xs) - min(xs), max(ys) - min(ys)
    aspect = res[0] / res[1]
    span = max(w, h * aspect) if aspect >= 1 else max(w / aspect, h)
    depth = max(zs) - min(zs)
    if ortho:
        cam.data.type = 'ORTHO'
        cam.data.ortho_scale = span * margin
        cam.location = center - d * (depth + 5)
    else:
        cam.data.type = 'PERSP'
        cam.data.angle = math.radians(fov_deg)
        dist = span * margin / 2 / math.tan(math.radians(fov_deg) / 2) + depth / 2
        cam.location = center - d * dist
    cam.data.clip_start = 0.01
    cam.data.clip_end = 500


def show_only(objs):
    keep = set(o.name for o in objs)
    for o in bpy.context.scene.objects:
        if o.type == 'MESH':
            o.hide_render = o.name not in keep


def spin_driver(o, axis, rate):
    fc = o.driver_add('rotation_euler', axis)
    fc.driver.type = 'SCRIPTED'
    fc.driver.expression = f'frame*{rate}'


def charge_frame(f):
    """halo_charge preview: frames 1-6 = 0%, then one more gap every 6 frames (25/50/75/100%)."""
    for k in range(1, 5):
        bpy.data.objects[f'HS_halo_gap_arc_{k}'].hide_render = f <= 6 * k


def render(group, frames, still_frame=None, per_frame=None):
    sc = bpy.context.scene
    folder = os.path.join(FRAMES, group)
    os.makedirs(folder, exist_ok=True)
    files = []
    for f in (frames if frames else [still_frame or 24]):
        if per_frame:
            per_frame(f)
        sc.frame_set(f)
        path = os.path.join(folder, f'{group}_{f:03d}.png')
        if os.path.exists(path):
            stale = os.path.join(ROOT, '_old', 'previews', 'frames', group)
            os.makedirs(stale, exist_ok=True)
            os.replace(path, os.path.join(stale, f'{os.path.basename(path)}.{os.getpid()}'))
        sc.render.filepath = path
        bpy.ops.render.render(write_still=True)
        files.append(path)
    return files


SIDE = (-1, 0, -0.18)          # camera on +X looking at -X, slightly from above; Blender -Y (forward) = screen left
FRONT = (0, 1, 0)              # camera in front of the character (-Y) looking back at it
THREE_Q = (-0.8, 0.55, -0.45)


def groups():
    G = []

    def g(name, objs, view, res, frames=None, still=24, offsets=None, ortho=True, label='', fps=24, setup=None,
          margin=1.12):
        G.append(dict(name=name, objs=objs, view=view, res=res, frames=frames, still=still, offsets=offsets or {},
                      ortho=ortho, label=label, fps=fps, setup=setup, margin=margin))

    g('arc_short', ['HS_arc_short_A', 'HS_arc_short_B', 'HS_arc_short_C'], SIDE, (768, 512),
      offsets={'HS_arc_short_A': (0, 0, 0.16), 'HS_arc_short_C': (0, 0, -0.16)},
      label='Short arcs A/B/C (0.4 m) - chain hop, fingertip crackle, halo gap arc')
    g('bolt_long', ['HS_bolt_long_A', 'HS_bolt_long_B', 'HS_bolt_long_C'], SIDE, (1536, 512),
      offsets={'HS_bolt_long_A': (0, 0, 1.1), 'HS_bolt_long_C': (0, 0, -1.1)},
      label='Long bolts A/B/C (10 m, pivot at start) - Arc Bolt tracer, Open Circuit pulse')
    g('bolt_branch', ['HS_bolt_branch_A', 'HS_bolt_branch_B'], SIDE, (1024, 768),
      offsets={'HS_bolt_branch_A': (0, 0, 1.2), 'HS_bolt_branch_B': (0, 0, -1.0)},
      label='Branching bolts A/B (3 m) - Discharge side lances, empowered shot')
    gaps = [f'HS_halo_gap_arc_{k}' for k in range(1, 5)]
    g('halo_ring', ['HS_halo_ring_segments'] + gaps, FRONT, (512, 512), frames=list(range(1, 25)),
      label='Halo ring segments + 4 gap arcs (r 0.32 m), shimmer scroll', margin=1.25)
    g('halo_charge', ['HS_halo_ring_segments'] + gaps, FRONT, (512, 512), frames=list(range(1, 31)),
      setup='charge', label='Charge meter on the halo: 0% dormant, +1 gap bridged per 25%, 100% all 4', margin=1.25,
      fps=6)
    g('heel_jet', ['HS_jet_cone_outer', 'HS_jet_cone_core', 'HS_jet_arcs'], (-1, 0.25, -0.3), (768, 384),
      frames=list(range(1, 25)), label='Heel jet: outer + core cones + crackle arcs (UV scroll, arc spin)',
      setup='jet', margin=1.3)
    g('jet_burst', ['HS_jet_burst_spikes', 'HS_jet_burst_ring'], (-0.75, 0.5, -0.4), (512, 512),
      frames=list(range(1, 11)), setup='burst', still=10,
      label='Jet burst spikes + shock ring (Arc Step launch / glide ignition), scale-up', margin=1.25)
    g('jet_ribbon', ['HS_jet_ribbon'], (-1, 0.1, -0.5), (768, 256), label='Jet exhaust ribbon (mesh streak)',
      margin=1.2)
    g('spear_materialize', ['HS_spear_lance_core', 'HS_spear_lance_shell', 'HS_spear_filaments'],
      (-1, 0, -0.35), (1024, 384), frames=list(range(1, 21)),
      label='Conduit Spear materialize: filaments converge, lance fades in tail -> tip', margin=1.1)
    g('spear_flight', ['HS_spear_lance_core', 'HS_spear_lance_shell', 'HS_spear_trail'], (-1, 0, -0.3),
      (1024, 320), still=24, label='Conduit Spear lance + trail (flight)', margin=1.08)
    g('mark', ['HS_mark_ring_segments', 'HS_mark_diamond'], FRONT, (512, 512), frames=list(range(1, 25)),
      setup='mark', label='Conductor mark mesh (slow segment rotation)', margin=1.3)
    g('mark_quad', ['HS_mark_quad'], FRONT, (512, 512), label='Conductor mark PNG on quad', margin=1.05)
    g('arc_step', ['HS_step_ground_trail', 'PREVIEW_afterimage_proxy'], (-1.0, -0.3, -0.62), (960, 540),
      ortho=False, setup='floor', label='Arc Step: ghost afterimage (proxy figure) + angular ground trail',
      margin=0.95)
    g('decal_scorch', ['HS_decal_quad_1m'], (0.0, 0.35, -1.0), (512, 512), setup='floor_grey',
      label='Scorch decal (alpha blend) on a grey floor', margin=1.05)
    g('decal_scorch_ring', ['HS_decal_quad_1m', 'PREVIEW_static_ring_quad'], (0.0, 0.35, -1.0), (512, 512),
      setup='floor', label='Scorch decal + static ring (additive) on a dark floor', margin=1.05)
    g('card_glow', ['HS_card_quad_1m'], FRONT, (384, 384), label='Card quad with hs_glow_star', margin=1.05)
    return G


def main():
    blend = os.path.join(ROOT, arg('--blend', 'hs-vfx-v03.blend'))
    bpy.ops.wm.open_mainfile(filepath=blend)
    only = arg('--only')
    only = set(only.split(',')) if only else None
    sc, cam = setup_scene()
    ring = bpy.data.objects.new('PREVIEW_static_ring_quad', O('HS_decal_quad_1m').data)
    sc.collection.objects.link(ring)
    ring.material_slots[0].link = 'OBJECT'
    ring.material_slots[0].material = bpy.data.materials['HS_M_static_ring']
    ring.location.z = 0.002
    manifest = {}
    fl = None
    for spec in groups():
        if only and spec['name'] not in only:
            continue
        objs = [O(n) for n in spec['objs']]
        saved = {o.name: o.location.copy() for o in objs}
        for n, off in spec['offsets'].items():
            O(n).location = Vector(off)
        show_only(objs)
        if spec['setup'] == 'jet':
            spin_driver(O('HS_jet_arcs'), 1, 0.25)
        if spec['setup'] == 'mark':
            spin_driver(O('HS_mark_ring_segments'), 1, 0.03)
        if spec['setup'] == 'burst':
            frame_camera(cam, objs, spec['view'], spec['res'], spec['margin'], spec['ortho'])   # frame at full size
            for i in range(3):
                fc = O('HS_jet_burst_spikes').driver_add('scale', i)
                fc.driver.type = 'SCRIPTED'
                fc.driver.expression = 'min(1.0, 0.25 + frame*0.25)'
                fc = O('HS_jet_burst_ring').driver_add('scale', i)
                fc.driver.type = 'SCRIPTED'
                fc.driver.expression = '0.6 + frame*0.14'
        if spec['setup'] in ('floor', 'floor_grey'):
            if fl is None:
                fl = floor()
            fl.hide_render = False
            col = (0.02, 0.02, 0.022) if spec['setup'] == 'floor' else (0.09, 0.085, 0.08)
            fl.data.materials[0].node_tree.nodes['Emission'].inputs['Color'].default_value = (*col, 1)
            fl.location.z = -0.001
        elif fl is not None:
            fl.hide_render = True
        if spec['setup'] != 'burst':
            frame_camera(cam, objs, spec['view'], spec['res'], spec['margin'], spec['ortho'])
        files = render(spec['name'], spec['frames'], spec['still'],
                       charge_frame if spec['setup'] == 'charge' else None)
        manifest[spec['name']] = dict(files=[os.path.relpath(f, ROOT) for f in files], label=spec['label'],
                                      fps=spec['fps'], blend=os.path.basename(blend))
        for n, loc in saved.items():
            O(n).location = loc
        if spec['setup'] == 'jet':
            O('HS_jet_arcs').driver_remove('rotation_euler', 1)
            O('HS_jet_arcs').rotation_euler = (0, 0, 0)
        if spec['setup'] == 'mark':
            O('HS_mark_ring_segments').driver_remove('rotation_euler', 1)
            O('HS_mark_ring_segments').rotation_euler = (0, 0, 0)
        if spec['setup'] == 'burst':
            for n in ('HS_jet_burst_spikes', 'HS_jet_burst_ring'):
                O(n).driver_remove('scale')
                O(n).scale = (1, 1, 1)
    mpath = os.path.join(FRAMES, 'manifest.json')
    old = {}
    if os.path.exists(mpath):
        with open(mpath) as f:
            old = json.load(f)
    old.update(manifest)
    with open(mpath, 'w') as f:
        json.dump(old, f, indent=1)


main()
