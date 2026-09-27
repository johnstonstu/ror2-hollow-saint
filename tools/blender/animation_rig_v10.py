"""Repair attachment/pivot setup in an already loaded copy of hybrid v9."""
import math
import bpy
from mathutils import Vector, Matrix


def prepare_rig(rig):
    rig.animation_data.action = None
    for p in rig.pose.bones:
        p.matrix_basis.identity()
        p.rotation_mode = 'QUATERNION'
    bpy.context.view_layer.update()
    attached = {o: o.matrix_world.copy() for o in bpy.context.scene.objects
                if o.parent == rig and o.parent_type == 'BONE'}
    for o, world in attached.items():
        if o.type in {'CAMERA', 'LIGHT'}:
            o.parent = None
            o.matrix_world = world
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    bones = rig.data.edit_bones
    for s, side, wx in [(1, 'L', .475), (-1, 'R', -.516)]:
        def p(x, y, z):
            return Vector((wx+s*x, y, z))
        pivot = p(-.008, -.038, 1.035)
        target = Vector((.461, 0, 1.025) if s == 1 else (-.508, -.05, 1.025))
        down = Vector((s*.30, .025, -.954)).normalized()
        align = Vector((0, 0, -1)).rotation_difference(down).to_matrix().to_4x4()
        transform = (Matrix.Translation(target) @ align
                     @ Matrix.Rotation(math.radians(-s*48), 4, 'Z')
                     @ Matrix.Translation(-pivot))
        # Copy the exact modeled joint positions, including the relaxed palm roll.
        for digit, dx, length, fan, curl in [
                ('index', -.035, .167, -.015, .042),
                ('middle', -.010, .190, -.003, .05),
                ('ring', .018, .177, .018, .066),
                ('little', .043, .148, .028, .084)]:
            points = [p(.012+dx, -.059, .865),
                      p(.013+dx+fan*.5, -.060, .865-length*.43),
                      p(.014+dx+fan, -.076, .865-length*.81),
                      p(.009+dx+fan, -.076-curl, .865-length*.93)]
            fit_digit(bones, side, digit, [transform @ v for v in points])
        points = [p(-.022, -.052, .915), p(-.064, -.065, .882),
                  p(-.09, -.081, .845), p(-.08, -.119, .821)]
        fit_digit(bones, side, 'thumb', [transform @ v for v in points])
    for i in range(1, 5):
        obj = bpy.data.objects[f'HALO | independent copper arc {i}']
        center = sum((attached[obj] @ Vector(v) for v in obj.bound_box), Vector()) / 8
        bones[f'halo {i}'].head = center
        bones[f'halo {i}'].tail = center + Vector((0, 0, .15))
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.view_layer.update()
    for o, world in attached.items():
        o.matrix_world = world
    rig['status'] = 'v10 custom animation study; fused-body skinning remains provisional'
    bpy.context.view_layer.update()
    repair_body_weights(rig)


def fit_digit(bones, side, digit, points):
    for i in range(3):
        b = bones[f'{side} {digit}.{i+1}']
        b.head, b.tail = points[i], points[i+1]
        # Local X follows the palm's bend axis, giving consistent finger curls.
        b.align_roll(Vector((0, -1, 0)))


def repair_body_weights(rig):
    """Restrict influences by anatomy so hands cannot pull skirt/thigh vertices."""
    body = bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
    adjacency = [[] for _ in body.data.vertices]
    for edge in body.data.edges:
        a, b = edge.vertices
        adjacency[a].append(b)
        adjacency[b].append(a)
    arm_regions = {}
    for side, wrist in [('L', Vector((.461, 0, 1.025))),
                        ('R', Vector((-.508, -.05, 1.025)))]:
        seed = min(body.data.vertices, key=lambda v: (v.co-wrist).length_squared).index
        for ceiling in (1.40, 1.35, 1.30):
            visited, queue = {seed}, [seed]
            while queue:
                index = queue.pop()
                for other in adjacency[index]:
                    if other not in visited and body.data.vertices[other].co.z < ceiling:
                        visited.add(other)
                        queue.append(other)
            if min(body.data.vertices[i].co.z for i in visited) > .94:
                break
        else:
            raise RuntimeError('Cannot isolate forearm topology from torso: '+side)
        arm_regions[side] = visited
        print('ARM REGION', side, len(visited), ceiling, flush=True)
    indices = [v.index for v in body.data.vertices]
    for group in body.vertex_groups:
        group.remove(indices)
    for v in body.data.vertices:
        p = body.matrix_world @ v.co
        x, y, z = p
        lateral = abs(x + .0375)
        side = 'L' if x > -.0375 else 'R'
        if v.index in arm_regions[side]:
            names = [side+' upperarm', side+' forearm', side+' hand']
        elif z < 1.30:
            names = [side+' thigh', side+' shin', side+' foot', 'pelvis']
            if z > 1.12:
                names = ['pelvis', 'spine']
            if lateral < .115 and (y < -.055 or y > .08):
                names = ['pelvis']
        elif lateral > .255 and z < 1.50:
            names = [side+' upperarm', side+' forearm', side+' hand']
        elif z > 1.73:
            names = ['head', 'neck']
        elif z > 1.46:
            names = ['chest'] if lateral > .15 else ['chest', 'neck', 'head']
        else:
            names = ['pelvis', 'spine', 'chest']
        distances = []
        for name in names:
            b = rig.data.bones[name]
            ab = b.tail_local-b.head_local
            t = max(0, min(1, (p-b.head_local).dot(ab)/ab.length_squared))
            d = (p-b.head_local-t*ab).length
            distances.append((d, name))
        closest = sorted(distances)[:2]
        weights = [1/max(d, .015)**4 for d, name in closest]
        for (_, name), weight in zip(closest, weights):
            body.vertex_groups[name].add([v.index], weight/sum(weights), 'REPLACE')
    body['rig_note'] = 'v10 regional weights prevent hand-to-leg contamination; shoulders remain a fused-mesh approximation.'
