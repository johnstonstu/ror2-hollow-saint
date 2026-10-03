"""Actual v36 hand, molded grip and Halo Lance study; never changes game assets.

Background Blender only. Numbered output directory is immutable after completion.
"""
import ast
import json
import math
import random
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[3]
args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
OUT = ROOT / (args[0] if args else 'artifacts/spear-hand-fit05/study')
assert OUT.resolve().is_relative_to((ROOT / 'artifacts').resolve()), 'Study must stay inside artifacts'
assert not (OUT / 'hand-fit-study.blend').exists(), 'Use a new numbered study directory'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE = ROOT / 'tools/blender/anim/armpass_v35.py'
sys.path.insert(0, str(SOURCE.parent))
sys.argv = [str(SOURCE), '--', 'artifacts/spear-hand-fit01/unused.blend']
tree = ast.parse(SOURCE.read_text(encoding='utf-8'))
nodes = []
for node in tree.body:
    if isinstance(node, ast.Expr) and isinstance(node.value, ast.Call) and isinstance(node.value.func, ast.Name) and node.value.func.id == 'arm_ik_off':
        break
    if isinstance(node, ast.Assign) and any(isinstance(t, ast.Name) and t.id == 'SRC' for t in node.targets):
        node.value = ast.parse("H.ROOT/'art/anim/hollow-saint-anim-v36.blend'", mode='eval').body
    nodes.append(node)
ns = {'__file__': str(SOURCE), '__name__': 'grip_helpers'}
exec(compile(ast.fix_missing_locations(ast.Module(body=nodes, type_ignores=[])), str(SOURCE), 'exec'), ns)
rig, pb, H, HP = (ns[k] for k in ('rig', 'pb', 'H', 'HP'))
ns['arm_ik_off']()
rig.animation_data.action = None
base = ns['Clip']('Idle combat')
ns['pose_from'](base.frames[0], base.bones)
ns['P'].update()
ns['solve_arm']('R', pb['R upperarm'].head + Vector((-0.18, -0.22, -0.38)), Vector((-1, 0.1, -0.6)))
for bone, q in ns['hand_quats']('R', 64, 46).items():
    pb[bone].rotation_quaternion = q
ns['P'].update()


def circle_center(a, b, c):
    u, v = b - a, c - a
    cross = u.cross(v)
    return a + (v.cross(cross) * u.length_squared + cross.cross(u) * v.length_squared) / (2 * cross.length_squared)


def local_bone(name, tail=False):
    p = pb[name].tail if tail else pb[name].head
    return pb['R hand'].matrix.inverted() @ p


centers = [circle_center(*(local_bone(f'R {d}.{i}') for i in (1, 2, 3)))
           for d in ('index', 'middle', 'ring', 'little')]
grip_center = sum(centers, Vector()) / 4
axis = (centers[0] - centers[-1]).normalized()
normal = ns['GEO'].normal['R']
x = (normal - axis * normal.dot(axis)).normalized()
local_frame = Matrix((x, axis.cross(x), axis)).transposed()
up = Vector((0.10, -0.18, 1)).normalized()
wx = (Vector((1, 0, 0)) - up * up.x).normalized()
desired_frame = Matrix((wx, up.cross(wx), up)).transposed()
pb['R hand'].rotation_quaternion = ns['basis_for']('R hand', desired_frame @ local_frame.transposed())
ns['P'].update()
ns['solve_followers']()
ns['P'].update()
hand_world = rig.matrix_world @ pb['R hand'].matrix
weapon_world = hand_world @ Matrix.Translation(grip_center) @ local_frame.to_4x4()
scene = bpy.context.scene
H.eevee(scene, 32)


def solid(name, color, metal=0.0, rough=0.45):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Metallic'].default_value = metal
    p.inputs['Roughness'].default_value = rough
    return m


def emission(name, color, strength):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    n = m.node_tree.nodes
    n.clear()
    out = n.new('ShaderNodeOutputMaterial')
    e = n.new('ShaderNodeEmission')
    e.inputs['Color'].default_value = (*color, 1)
    e.inputs['Strength'].default_value = strength
    m.node_tree.links.new(e.outputs[0], out.inputs[0])
    return m


COPPER = solid('Study aged copper', (0.28, 0.105, 0.04), 0.65)
DARK = solid('Study molded charcoal grip', (0.018, 0.025, 0.028), 0.15, 0.65)
CYAN = emission('Study connected current', (0.04, 0.65, 1), 5)
WHITE = emission('Study electric core', (0.65, 0.95, 1), 6)
assembly = []
effects = []


def loft(name, rings, material, matrix=weapon_world, segments=32):
    vs = [(rx * math.cos(j * 2 * math.pi / segments), ry * math.sin(j * 2 * math.pi / segments), z)
          for z, rx, ry in rings for j in range(segments)]
    fs = [tuple(reversed(range(segments)))]
    for k in range(len(rings) - 1):
        for j in range(segments):
            a, b = k * segments + j, k * segments + (j + 1) % segments
            fs.append((a, b, b + segments, a + segments))
    fs.append(tuple((len(rings) - 1) * segments + j for j in range(segments)))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vs, [], fs)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    scene.collection.objects.link(ob)
    ob.matrix_world = matrix
    ob.data.materials.append(material)
    assembly.append(ob)
    return ob


def line(name, coords, radius, mat, matrix=None):
    cu = bpy.data.curves.new(name, 'CURVE')
    cu.dimensions = '3D'
    cu.bevel_depth = radius
    cu.bevel_resolution = 1
    sp = cu.splines.new('POLY')
    sp.points.add(len(coords) - 1)
    for p, c in zip(sp.points, coords):
        p.co = (*c, 1)
    ob = bpy.data.objects.new(name, cu)
    scene.collection.objects.link(ob)
    ob.data.materials.append(mat)
    if matrix is not None:
        ob.matrix_world = matrix
    effects.append(ob)
    return ob


# The actual hand is separate bone-parented pieces, not part of the body skin.
# Use every finger segment and the sculpted palm as individual closed cutters.
body = bpy.data.objects[H.BODY]
deps = bpy.context.evaluated_depsgraph_get()
hand_parts = [obj for obj in bpy.data.objects if obj.type == 'MESH' and obj.name.startswith('R HAND |')]
hand_local = []
for part in hand_parts:
    use = part.evaluated_get(deps)
    mesh = use.to_mesh()
    hand_local.extend(weapon_world.inverted() @ use.matrix_world @ v.co for v in mesh.vertices)
    use.to_mesh_clear()
grip_top = max(0.095, max(v.z for v in hand_local if math.hypot(v.x, v.y) < 0.055) + 0.008)
grip = loft('Custom grip — palm thumb and finger seats',
            [(-0.095, 0.028, 0.032), (-0.070, 0.038, 0.041), (-0.045, 0.043, 0.047),
             (0, 0.043, 0.047), (0.045, 0.039, 0.043), (0.070, 0.031, 0.035),
             (0.095, 0.027, 0.031), (grip_top, 0.024, 0.027)], DARK)
for part in hand_parts:
    evaluated = part.evaluated_get(deps)
    cutter_mesh = bpy.data.meshes.new_from_object(evaluated, depsgraph=deps)
    # Convex contact proxy + a 1.2 mm world-space box dilation. This encloses the
    # real part robustly even where the source palm has concave/open faces.
    bm = bmesh.new()
    for v in cutter_mesh.vertices:
        position = evaluated.matrix_world @ v.co
        for a in (-1, 1):
            for b in (-1, 1):
                for c in (-1, 1):
                    bm.verts.new(position + Vector((a, b, c)) * 0.0012)
    hull = bmesh.ops.convex_hull(bm, input=list(bm.verts))
    bmesh.ops.delete(bm, geom=list(set(hull['geom_interior'] + hull['geom_unused'])), context='VERTS')
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(cutter_mesh)
    bm.free()
    cutter_mesh.update()
    cutter = bpy.data.objects.new('Clearance mold ' + part.name, cutter_mesh)
    scene.collection.objects.link(cutter)
    cutter.matrix_world = Matrix.Identity(4)
    boolean = grip.modifiers.new('Actual hand contact relief', 'BOOLEAN')
    boolean.operation = 'DIFFERENCE'
    boolean.solver = 'EXACT'
    boolean.object = cutter
    bpy.context.view_layer.objects.active = grip
    grip.select_set(True)
    bpy.ops.object.modifier_apply(modifier=boolean.name)
    grip.select_set(False)
    cutter.hide_render = True
    cutter.hide_set(True)

# Remove small disconnected Boolean scraps; the contact shell stays one piece.
bm = bmesh.new()
bm.from_mesh(grip.data)
unseen = set(bm.verts)
components = []
while unseen:
    stack = [unseen.pop()]
    component = set(stack)
    while stack:
        for edge in stack.pop().link_edges:
            for vertex in edge.verts:
                if vertex in unseen:
                    unseen.remove(vertex)
                    component.add(vertex)
                    stack.append(vertex)
    components.append(component)
largest = max(components, key=len)
scraps = set().union(*(c for c in components if c is not largest))
bmesh.ops.delete(bm, geom=list(scraps), context='VERTS')
bm.to_mesh(grip.data)
bm.free()


def tree_for(obj, evaluated=False):
    use = obj.evaluated_get(bpy.context.evaluated_depsgraph_get()) if evaluated else obj
    mesh = use.to_mesh() if evaluated else use.data
    tree = BVHTree.FromPolygons([use.matrix_world @ v.co for v in mesh.vertices],
                               [tuple(p.vertices) for p in mesh.polygons], all_triangles=False)
    if evaluated:
        use.to_mesh_clear()
    return tree


verts, faces = [], []
for part in hand_parts:
    use = part.evaluated_get(deps)
    mesh = use.to_mesh()
    offset = len(verts)
    verts.extend(use.matrix_world @ v.co for v in mesh.vertices)
    faces.extend(tuple(offset + i for i in p.vertices) for p in mesh.polygons)
    use.to_mesh_clear()
body_tree = BVHTree.FromPolygons(verts, faces)
grip_tree = tree_for(grip)
overlaps = len(body_tree.overlap(grip_tree))
clearance = min(body_tree.find_nearest(grip.matrix_world @ v.co)[3] for v in grip.data.vertices)
print('ACTUAL_HAND_FIT', 'overlapping face pairs', overlaps, 'nearest surface mm', clearance * 1000, flush=True)
if overlaps:
    print('FIT_OVERLAPS', [(p.name, len(tree_for(p, True).overlap(grip_tree))) for p in hand_parts], flush=True)
assert overlaps == 0, 'Grip penetrates the actual skin; adjust the clearance mold'
assert 0.0002 < clearance < 0.003, 'Grip must sit close to the actual hand, not float clear of it'

# Copper collars stay beyond the hand envelope. Metal head is two open crescents.
for z in (-0.112, grip_top + 0.014):
    loft('Copper grip collar', [(z - 0.008, 0.028, 0.031), (z + 0.008, 0.028, 0.031)], COPPER)
for z in (-0.090, grip_top - 0.003):
    loft('Conductive contact cuff', [(z - 0.0025, 0.030, 0.034), (z + 0.0025, 0.030, 0.034)], COPPER)
line('Contained lightning spine', [(0, 0, -0.73), (0, 0, -0.12)], 0.012, WHITE, weapon_world)
line('Contained lightning spine', [(0, 0, grip_top + 0.025), (0, 0, 1.04)], 0.012, WHITE, weapon_world)
for z in (0.60, 0.65, -0.65):
    loft('Shaft conductor', [(z - 0.010, 0.024, 0.024), (z + 0.010, 0.024, 0.024)], COPPER)
for sign in (-1, 1):
    rings = [(0.67, 0.018, 0.018), (0.75, 0.024, 0.017), (0.89, 0.021, 0.014),
             (1.03, 0.014, 0.010), (1.16, 0.0015, 0.0015)]
    ob = loft('Open copper crescent', rings, COPPER, segments=6)
    offsets = [0.012, 0.09, 0.14, 0.12, 0.055]
    for v in ob.data.vertices:
        k = min(range(len(rings)), key=lambda i: abs(v.co.z - rings[i][0]))
        v.co.x += sign * offsets[k]
    ob.data.update()
loft('Sharp contained energy point', [(0.75, 0.018, 0.016), (0.96, 0.041, 0.022),
                                    (1.30, 0.0007, 0.0007)], WHITE, segments=6)
loft('Tail energy point', [(-0.84, 0.0007, 0.0007), (-0.73, 0.016, 0.016)], WHITE, segments=6)
collar_overlaps = {ob.name: len(tree_for(ob).overlap(body_tree)) for ob in assembly if ob.name.startswith(('Copper grip collar', 'Conductive contact cuff'))}
assert sum(collar_overlaps.values()) == 0, 'Copper grip collars must also clear the actual hand'


def w(p):
    return rig.matrix_world @ p


def radial(v, direction):
    direction = direction.normalized()
    return (v - direction * v.dot(direction)).normalized()


def arm_current():
    ua, fa, hand = (pb['R ' + n] for n in ('upperarm', 'forearm', 'hand'))
    def anatomical(bone, d):
        return bone.matrix.to_3x3() @ rig.data.bones[bone.name].matrix_local.to_3x3().inverted() @ d
    rear = radial(anatomical(ua, Vector((0, 1, 0))), fa.head - ua.head)
    under = radial(anatomical(fa, Vector((0, 0, -1))), hand.head - fa.head)
    shoulder = ua.head + rear * 0.095
    dock = min((pb[f'halo {i}'].head.copy() for i in range(1, 5)), key=lambda p: (p - shoulder).length_squared)
    palm_tree = tree_for(bpy.data.objects['R HAND | tapered sculpted palm'], True)
    contact = min((grip.matrix_world @ v.co for v in grip.data.vertices), key=lambda p: palm_tree.find_nearest(p)[3])
    route = [w(dock), w(shoulder), w(ua.head.lerp(fa.head, 0.55) + rear * 0.085),
             w(fa.head + rear * 0.09), w(fa.head + (rear + under).normalized() * 0.11),
             w(fa.head.lerp(hand.head, 0.6) + under * 0.08), w(hand.head + under * 0.055),
             contact]
    rng = random.Random(971)
    def crackling(points, jitter):
        out = []
        for a, b in zip(points, points[1:]):
            for j in range(6):
                u = j / 6
                noise = Vector(tuple(rng.uniform(-jitter, jitter) for _ in range(3)))
                out.append(a.lerp(b, u) + noise * math.sin(u * math.pi))
        return out + [points[-1]]
    path = crackling(route, 0.012)
    line('Ring arm hand grip connected current', path, 0.018, CYAN)
    line('Ring arm hand grip connected core', path, 0.006, WHITE)
    feed = [w(pb['core socket'].head), w(pb['chest'].head + Vector((-0.18, 0.07, 0.09))), w(shoulder), w(dock)]
    line('Core to ring feed', crackling(feed, 0.012), 0.004, CYAN)
    exit_contact = [contact, weapon_world @ Vector((0.025, 0, grip_top)), weapon_world @ Vector((0, 0, grip_top + 0.030))]
    line('Contact feeds spear spine', exit_contact, 0.006, CYAN)
    for sign in (-1, 1):
        local = [Vector((sign * 0.13, 0, 0.91)), Vector((sign * 0.065, 0.012, 0.99)), Vector((0, 0, 1.01))]
        line('Head gap discharge', crackling([weapon_world @ p for p in local], 0.009), 0.005, CYAN)
    line('Pulse entering spear', [(0, 0, grip_top + 0.030), (0, 0, 0.40), (0, 0, 0.53)], 0.021, CYAN, weapon_world)


arm_current()
target = weapon_world.translation

# A dedicated hand socket owns the whole weapon. Bone parenting in Blender uses
# the bone tail; cancel that offset so the saved Unity socket uses the hand head.
socket = bpy.data.objects.new('Spear grip socket — hand locked', None)
scene.collection.objects.link(socket)
socket.parent = rig
socket.parent_type = 'BONE'
socket.parent_bone = 'R hand'
socket.matrix_parent_inverse = Matrix.Identity(4)
socket.matrix_basis = Matrix.Translation((0, -rig.data.bones['R hand'].length, 0)) @ hand_world.inverted() @ weapon_world
ns['P'].update()
error = max(abs(socket.matrix_world[i][j] - weapon_world[i][j]) for i in range(4) for j in range(4))
assert error < 0.00001, 'Bone-parent grip socket failed to preserve the fitted placement'
local_effects = [ob for ob in effects if max(abs(ob.matrix_world[i][j] - weapon_world[i][j]) for i in range(4) for j in range(4)) < 0.00001]
for ob in assembly + local_effects:
    ob.parent = socket
    ob.matrix_parent_inverse = Matrix.Identity(4)
    ob.matrix_basis = Matrix.Identity(4)
ns['P'].update()
attachment_errors = []
held_quats = {name: pb[name].rotation_quaternion.copy() for name in ('R upperarm', 'R forearm', 'R hand')}
for upper_delta, elbow_delta, wrist_delta in ((-8, 6, -4), (8, -6, 4), (-4, -8, 8), (4, 8, -8)):
    for name, delta in zip(held_quats, (upper_delta, elbow_delta, wrist_delta)):
        pb[name].rotation_quaternion = held_quats[name] @ HP.about(Vector((1, 0, 0)), delta)
    ns['P'].update()
    expected = rig.matrix_world @ pb['R hand'].matrix @ hand_world.inverted() @ weapon_world
    attachment_errors.append(max(abs(socket.matrix_world[i][j] - expected[i][j]) for i in range(4) for j in range(4)))
for name, q in held_quats.items():
    pb[name].rotation_quaternion = q
ns['P'].update()
assert max(attachment_errors) < 0.00001, 'Grip slipped relative to the hand under arm movement'


def render(name, position, target, scale, res):
    cam = bpy.data.objects.new(name, bpy.data.cameras.new(name))
    scene.collection.objects.link(cam)
    cam.location = position
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = scale
    H.render_still(scene, cam.name, OUT / (name + '.png'), res)


render('rear-connected', (-0.9, 3.8, 2.0), (0, -0.15, 1.35), 3.15, (800, 900))
render('front-held', (-3, -4, 2.4), (0, -0.25, 1.35), 3.15, (800, 900))
# Two views of the actual hand; hide FX so fit remains legible.
for ob in effects:
    if 'spine' not in ob.name:
        ob.hide_render = True
render('grip-contact', target + hand_world.to_3x3() @ Vector((0.35, 0.15, -0.17)), target, 0.42, (800, 800))
render('grip-back', target + hand_world.to_3x3() @ Vector((-0.35, 0.2, 0.17)), target, 0.42, (800, 800))
render_states = {obj: obj.hide_render for obj in scene.objects}
body.hide_render = True
for obj in scene.objects:
    if obj.type == 'MESH' and obj not in assembly:
        obj.hide_render = True
render('molded-grip', target + hand_world.to_3x3() @ Vector((0.35, 0.15, -0.17)), target, 0.33, (800, 800))
for obj, hidden in render_states.items():
    obj.hide_render = hidden
for ob in effects:
    ob.hide_render = False
report = {'source': 'hollow-saint-anim-v36.blend', 'preview_only': True,
          'grip_axis_hand_local': list(axis), 'grip_center_hand_local': list(grip_center),
          'weapon_matrix_hand_local': [list(row) for row in (hand_world.inverted() @ weapon_world)],
          'grip_skin_overlap_face_pairs': overlaps, 'grip_vertex_nearest_skin_mm': clearance * 1000,
          'mold_box_dilation_mm': 1.2, 'grip_vertices': len(grip.data.vertices),
          'discarded_boolean_scrap_components': len(components) - 1,
          'grip_collar_hand_overlap_face_pairs': collar_overlaps,
          'max_socket_matrix_error_under_arm_motion': max(attachment_errors),
          'grip_triangles': sum(len(p.vertices) - 2 for p in grip.data.polygons)}
(OUT / 'fit-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'hand-fit-study.blend'))
print('HAND_FIT_STUDY_SAVED', str(OUT), flush=True)
