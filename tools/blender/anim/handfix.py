"""Hand chirality fix (item 9f), applied on load by hs_anim.open_start (v18 itself stays untouched).

The v18 hands are mirror-handed: the left hand carries a right hand's layout and vice versa (thumb and
index on the medial/back edge with the palm facing forward-medial; handedness (d x n) . r = +0.75 on the
left, where a real left hand is -1; see handorient.py and art/anim/wip/hands/orientation/v18-check/).

Fix, per side: reflect the digits (thumb, index..little chains, the muzzle socket and every other bone under
the hand) and the hand meshes that ride them (digit segments, tip lights, knuckles, palm) across the plane
through the knuckle centre whose normal is the index -> little axis (perpendicular to the hand bone). The
curl side of the hand lies in that plane, so fingers still curl the same way and the palm keeps facing
where it did, but the thumb and index move to the other edge: each side now carries a correctly handed
hand. The wrist cuff and wrist conductor stay (they belong to the forearm silhouette).

Reflected bones get the frame (-Rx, Ry, Rz): a +X rotation still curls toward the palm, but rotations about
local Z (finger fan, thumb tuck) now run the other way anatomically, so handpass/presentation flip their
per-side Z signs by ZSIGN.
"""
import bpy
import bmesh
from mathutils import Matrix, Vector

SIDES = ('L', 'R')
KEEP = ('wrist cuff', 'wrist conductor')
ZSIGN = -1.0   # handpass/presentation multiply their per-side local-Z finger/thumb signs by this


def descendants(bone):
    out = []
    for c in bone.children:
        out.append(c.name)
        out += descendants(c)
    return out


def plane(rig, side):
    b = rig.data.bones
    y = b[f'{side} hand'].matrix_local.col[1].xyz.normalized()
    i, l = b[f'{side} index.1'].head_local, b[f'{side} little.1'].head_local
    k = l-i
    k = (k-y*k.dot(y)).normalized()
    return (i+l)*0.5, k


def reflect_point(v, c, k):
    return v-2.0*(v-c).dot(k)*k


def reflect_dir(v, k):
    return v-2.0*v.dot(k)*k


def apply(rig):
    """Idempotent per loaded file."""
    if rig.get('hs_hand_fix'):
        return {'already': True}
    mw = rig.matrix_world
    pose_position = rig.data.pose_position
    rig.data.pose_position = 'REST'
    bpy.context.view_layer.update()
    report = {}
    planes = {s: plane(rig, s) for s in SIDES}
    bones = {s: descendants(rig.data.bones[f'{s} hand']) for s in SIDES}
    # Meshes skinned to digit bones would not follow a rest change cleanly: refuse rather than guess.
    digit_set = set(bones['L']+bones['R'])
    for o in bpy.data.objects:
        if o.type == 'MESH' and any(m.type == 'ARMATURE' and m.object == rig for m in o.modifiers):
            if any(g.name in digit_set for g in o.vertex_groups):
                used = {o.vertex_groups[g.group].name for v in o.data.vertices for g in v.groups if g.weight > 0}
                if used & digit_set:
                    raise RuntimeError(f'handfix: {o.name} is skinned to digit bones {sorted(used & digit_set)[:4]}')
    for pb in rig.pose.bones:
        if pb.name in digit_set and pb.constraints:
            raise RuntimeError(f'handfix: {pb.name} has constraints')
    parts = {}
    for s in SIDES:
        c, k = planes[s]
        cw, kw = mw @ c, (mw.to_3x3() @ k).normalized()
        objs = [o for o in bpy.data.objects if o.parent == rig and o.parent_type == 'BONE' and
                o.name.startswith(f'{s} HAND |') and (o.parent_bone in bones[s] or o.parent_bone == f'{s} hand')
                and not any(t in o.name for t in KEEP)]
        for o in objs:
            if o.type != 'MESH':
                raise RuntimeError(f'handfix: {o.name} is not a mesh')
            if o.data.users > 1:
                o.data = o.data.copy()
            parts[o.name] = (o, o.matrix_world.copy(), [reflect_point(o.matrix_world @ v.co, cw, kw)
                                                         for v in o.data.vertices])
        report[s] = {'bones': len(bones[s]), 'meshes': len(objs), 'plane_c': [round(x, 4) for x in c],
                     'plane_n': [round(x, 3) for x in k]}
    view = bpy.context.view_layer
    prev = view.objects.active
    for o in view.objects:
        o.select_set(False)
    rig.hide_set(False)
    rig.hide_viewport = False
    view.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    eb = rig.data.edit_bones
    for s in SIDES:
        c, k = planes[s]
        new = {}
        for n in bones[s]:
            m = eb[n].matrix
            x, y, z = (m.col[i].xyz for i in range(3))
            r3 = Matrix((-reflect_dir(x, k), reflect_dir(y, k), reflect_dir(z, k))).transposed()
            m2 = r3.to_4x4()
            m2.translation = reflect_point(m.translation, c, k)
            new[n] = (m2, eb[n].length)
        for n in bones[s]:   # parents first (descendants() is depth-first), connected children follow
            eb[n].matrix = new[n][0]
            eb[n].length = new[n][1]
    bpy.ops.object.mode_set(mode='OBJECT')
    view.objects.active = prev
    view.update()
    for name, (o, _, world) in parts.items():
        inv = o.matrix_world.inverted()
        me = o.data
        for v, w in zip(me.vertices, world):
            v.co = inv @ w
        bm = bmesh.new()
        bm.from_mesh(me)
        bmesh.ops.reverse_faces(bm, faces=bm.faces[:])
        bm.to_mesh(me)
        bm.free()
        me.update()
    rig.data.pose_position = pose_position
    view.update()
    rig['hs_hand_fix'] = ('9f: digits and hand meshes reflected across the knuckle plane (index <-> little axis), '
                          'so each side carries a correctly handed hand; curl side unchanged.')
    return report
