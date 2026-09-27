"""Glide VFX, applied on load by hs_anim.open_start (v18 is never saved in place).

- Custom properties on the `root` pose bone, keyed into every clip by `hs_anim.bake`:
  `hs_glow` (0..1) boosts the cyan emissive materials (veins, core, halo gap lights);
  `hs_jet` (0..~1.6) scales both heel thrust jets (0 = hidden);
  `hs_spark_L` / `hs_spark_R` (0..~0.4) add a per-foot flash on top (run push-off sparks);
  `hs_jet_dir` (deg) tilts the exhaust below horizontal (0 = straight back along the travel line);
  `hs_move_x` / `hs_move_y`: character-space travel direction (+x = the character's right, +y = forward; unit
  length while moving, 0/0 = none). The jets yaw so the exhaust trails opposite it (backpedal streams forward,
  strafes sideways). `bake` fills them from meta `move` / `direction` / `travel` when a clip doesn't key them.
  `hs_turn` (not VFX): turn clips' body-yaw progress, 0..1 of meta `turn_deg` (turns.py), for Unity to drive the
  model yaw from; 0 in every other clip.
  `hs_spear` (Conduit Spear, spear.py): 0..1 materialize progress of the lance along the R forearm, 1 at the
  core flash, back to 0 on the release frame (the projectile takes over); 0 in every other clip.
- Heel thrusters: per foot, a hot core cone, an outer cone and three zigzag arcs that spin with the frame
  (crackle). They ride the heel spur (`heel socket` tail, the Achilles base) but keep the character's
  orientation, so the exhaust always trails back along the line of travel, tilted down by `hs_jet_dir`
  whatever the ankle is doing.
Drivers are single-property or simple expressions only, so they evaluate with Python auto-run off.
Unity drops these; the keyed properties are there for its VFX to read.
"""
import math
import bmesh
import bpy
from mathutils import Matrix, Vector

PROPS = ('hs_glow', 'hs_jet', 'hs_spark_L', 'hs_spark_R', 'hs_jet_dir', 'hs_move_x', 'hs_move_y', 'hs_turn',
         'hs_spear')
GLOW_GAIN = 1.6          # emission x (1 + GAIN*hs_glow)
GLOW_MATERIALS = ('V11 cyan conductor light', 'HYBRID cyan conductor', 'V11 cyan core hot',
                  'V11 halo gap light', 'V11 halo top gap light')
JET_LEN = 0.50
JET_R = 0.05
HEEL = Vector((0.0, 0.0, 0.02))     # from the heel spur tip (`heel socket` tail) up to the Achilles base
COLLECTION = 'VFX | glide'


def prop_path(name):
    return f'pose.bones["root"]["{name}"]'


def add_var(drv, rig, name, var='g'):
    v = drv.variables.new()
    v.name = var
    v.type = 'SINGLE_PROP'
    v.targets[0].id_type = 'OBJECT'
    v.targets[0].id = rig
    v.targets[0].data_path = prop_path(name)
    return v


def glow_drivers(rig):
    done = []
    for mname in GLOW_MATERIALS:
        m = bpy.data.materials.get(mname)
        if m is None or not m.use_nodes:
            continue
        for n in m.node_tree.nodes:
            if n.type != 'BSDF_PRINCIPLED':
                continue
            sock = n.inputs['Emission Strength']
            base = sock.default_value
            fc = sock.driver_add('default_value')
            drv = fc.driver
            drv.type = 'SCRIPTED'
            add_var(drv, rig, 'hs_glow')
            drv.expression = f'{base:.4f}*(1+{GLOW_GAIN}*g)'
            done.append(mname)
    return done


def emit_material(name, color, strength):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    em = nt.nodes.new('ShaderNodeEmission')
    em.inputs['Color'].default_value = (*color, 1.0)
    em.inputs['Strength'].default_value = strength
    nt.links.new(em.outputs['Emission'], out.inputs['Surface'])
    return m


def cone_mesh(name, r0, length, segs=12):
    """Cone along -Z from the origin (base) to the tip at z = -length."""
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=True, segments=segs, radius1=r0, radius2=0.002,
                          depth=length, matrix=Matrix.Translation((0, 0, -length/2)))
    bm.to_mesh(me)
    bm.free()
    return me


def arc_mesh(name, length, n_arcs=3, kinks=6, amp=0.03, width=0.006):
    """Thin zigzag ribbons radiating down from the sole (flat strips, both faces emit)."""
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    for a in range(n_arcs):
        ang = 2*math.pi*a/n_arcs
        d = Vector((math.cos(ang), math.sin(ang), 0))
        side = Vector((-d.y, d.x, 0))*width
        prev = None
        for k in range(kinks+1):
            u = k/kinks
            off = d*(0.012+amp*u*(1 if k % 2 else -0.4))
            c = Vector((0, 0, -length*u))+off
            v1, v2 = bm.verts.new(c-side), bm.verts.new(c+side)
            if prev:
                bm.faces.new((prev[0], prev[1], v2, v1))
            prev = (v1, v2)
    bm.to_mesh(me)
    bm.free()
    return me


def scale_drivers(obj, rig, side):
    for i in range(3):
        fc = obj.driver_add('scale', i)
        drv = fc.driver
        drv.type = 'SUM'
        add_var(drv, rig, 'hs_jet', 'g')
        add_var(drv, rig, f'hs_spark_{side}', 's')


def dir_driver(obj, rig):
    """Cone axis (-Z) onto +Y (straight back) at hs_jet_dir 0, tilted down by hs_jet_dir degrees."""
    fc = obj.driver_add('rotation_euler', 0)
    fc.driver.type = 'SCRIPTED'
    add_var(fc.driver, rig, 'hs_jet_dir', 'd')
    fc.driver.expression = '1.570796-d*0.0174533'
    # Yaw after the tilt (XYZ): +Y (behind) turns onto the exhaust direction. Character right is -X in Blender
    # (it faces -Y), so exhaust = -travel = (hs_move_x, hs_move_y) in Blender XY; atan2(0, 0) = 0 keeps it behind.
    fc = obj.driver_add('rotation_euler', 2)
    fc.driver.type = 'SCRIPTED'
    add_var(fc.driver, rig, 'hs_move_x', 'x')
    add_var(fc.driver, rig, 'hs_move_y', 'y')
    fc.driver.expression = 'atan2(-x, y)'


def spin_driver(obj, rate):
    fc = obj.driver_add('rotation_euler', 2)
    fc.driver.type = 'SCRIPTED'
    fc.driver.expression = f'frame*{rate}'


def build_jets(rig):
    col = bpy.data.collections.get(COLLECTION)
    if col is None:
        col = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(col)
    outer = emit_material('VFX | heel jet', (0.25, 0.8, 1.0), 9.0)
    core = emit_material('VFX | heel jet core', (0.85, 0.97, 1.0), 22.0)
    arc = emit_material('VFX | heel jet arc', (0.55, 0.9, 1.0), 16.0)
    rig.animation_data.action = None
    for pb in rig.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()
    made = []
    for s in ('L', 'R'):
        heel = rig.data.bones[f'{s} heel socket']
        # Mount: follows the heel spur, but its axes stay the character's (Copy Rotation from the rig).
        mount = bpy.data.objects.new(f'VFX | {s} heel jet', None)
        mount.empty_display_size = 0.03
        col.objects.link(mount)
        mount.parent = rig
        mount.parent_type = 'BONE'
        mount.parent_bone = heel.name
        bpy.context.view_layer.update()
        mount.matrix_world = Matrix.Translation(rig.matrix_world @ heel.tail_local+HEEL)
        con = mount.constraints.new('COPY_ROTATION')
        con.target = rig
        made.append(mount.name)
        cones = []
        for tag, me, mat in (('outer', cone_mesh(f'VFX {s} jet outer', JET_R, JET_LEN), outer),
                             ('core', cone_mesh(f'VFX {s} jet core', JET_R*0.45, JET_LEN*0.7), core)):
            me.materials.append(mat)
            o = bpy.data.objects.new(f'VFX | {s} heel jet {tag}', me)
            col.objects.link(o)
            o.parent = mount
            o.matrix_parent_inverse = Matrix.Identity(4)
            dir_driver(o, rig)
            scale_drivers(o, rig, s)
            cones.append(o)
            made.append(o.name)
        # Arcs ride on the outer cone (inherit its scale) and spin about the jet axis: crackle.
        me = arc_mesh(f'VFX {s} jet arcs', JET_LEN*0.85)
        me.materials.append(arc)
        o = bpy.data.objects.new(f'VFX | {s} heel jet arcs', me)
        col.objects.link(o)
        o.parent = cones[0]
        o.matrix_parent_inverse = Matrix.Identity(4)
        o.scale = (1.0, 1.0, 1.1)
        spin_driver(o, 2.3 if s == 'L' else -2.7)
        made.append(o.name)
    return made


def apply(rig):
    if rig.get('hs_vfx'):
        return {'skipped': True}
    root = rig.pose.bones['root']
    for n in PROPS:
        root[n] = 0.0
    out = {'glow_materials': glow_drivers(rig), 'jets': build_jets(rig)}
    rig['hs_vfx'] = 1
    return out
