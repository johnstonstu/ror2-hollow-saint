"""R arm skeleton refit applied on load (FULL-AUDIT M6), before rigfix/handfix.

The body mesh is centred on x = -0.0375 (hs_anim.MID_X), but the v18 arm bones were mirrored about x = 0,
so the R shoulder, elbow and wrist pivots sit 4-12 cm off the R arm mesh (the L chain fits within ~1 cm).
The R finger bones and hand meshes already fit each other (they are the L hand mirrored about the mesh
centre and shifted), so only `R upperarm`, `R forearm` and `R hand` move.

Each R joint goes to its mesh seam centroid (dominant-group boundary: chest/upperarm, upperarm/forearm,
forearm/hand) plus the L bone's offset from its own seam, mirrored in x. The seam centroids are biased
(the rings aren't planar), so the L offset carries that bias across instead of assuming the centroid is
the pivot. Bone rolls keep the old local Z axis. Skinned meshes are unchanged at rest (the bind is the
rest pose), bone-parented meshes keep their world placement, and the IK/pole helpers move with their joint.
"""
import bpy
from mathutils import Vector

import hs_anim as H

CHAIN = (('chest', 'upperarm'), ('upperarm', 'forearm'), ('forearm', 'hand'))
FOLLOW = {'hand IK': 'hand', 'elbow pole': 'forearm'}


def seam_centroids(rig):
    body = bpy.data.objects[H.BODY]
    names = {g.index: g.name for g in body.vertex_groups}
    dom = [names[max(v.groups, key=lambda e: e.weight).group] if v.groups else '' for v in body.data.vertices]
    mw = body.matrix_world
    out = {}
    for s in H.SIDES:
        for a, b in CHAIN:
            ga = a if a == 'chest' else f'{s} {a}'
            gb = f'{s} {b}'
            vs = {k for e in body.data.edges if {dom[e.vertices[0]], dom[e.vertices[1]]} == {ga, gb} for k in e.vertices}
            if not vs:
                raise RuntimeError(f'armfit: no {ga}/{gb} seam')
            out[(s, b)] = sum((mw @ body.data.vertices[i].co for i in vs), Vector())/len(vs)
    return out


def apply(rig):
    """Idempotent (rig['hs_arm_fit'])."""
    if rig.get('hs_arm_fit'):
        return {'already': True}
    seams = seam_centroids(rig)
    inv = rig.matrix_world.inverted()
    mw = rig.matrix_world
    targets = {}
    for _, b in CHAIN:
        bias = (mw @ rig.data.bones[f'L {b}'].head_local)-seams[('L', b)]
        bias.x = -bias.x
        targets[b] = inv @ (seams[('R', b)]+bias)
    moved = {f'R {b}' for _, b in CHAIN} | {f'R {k}' for k in FOLLOW}
    keep = {o: o.matrix_world.copy() for o in bpy.data.objects
            if o.parent == rig and o.parent_type == 'BONE' and o.parent_bone in moved}
    view = bpy.context.view_layer
    prev = view.objects.active
    for o in view.objects:
        o.select_set(False)
    rig.hide_set(False)
    rig.hide_viewport = False
    view.objects.active = rig
    rig.select_set(True)
    pose_position = rig.data.pose_position
    rig.data.pose_position = 'REST'
    bpy.ops.object.mode_set(mode='EDIT')
    eb = rig.data.edit_bones
    report = {}
    for c in eb:
        if c.parent and c.parent.name in moved and c.name not in moved and c.use_connect:
            c.use_connect = False
    old = {n: (eb[n].head.copy(), eb[n].tail.copy()) for n in moved}
    l_hand = eb['L hand']
    hand_vec = l_hand.tail-l_hand.head
    hand_vec.x = -hand_vec.x
    ends = {'upperarm': targets['forearm'], 'forearm': targets['hand'], 'hand': targets['hand']+hand_vec}
    for _, b in CHAIN:
        e = eb[f'R {b}']
        z = e.z_axis.copy()
        e.use_connect = False
        e.head = targets[b]
        e.tail = ends[b]
        e.align_roll(z)
        report[e.name] = {'head_moved_mm': round((e.head-old[e.name][0]).length*1000, 1),
                          'length_mm': [round((old[e.name][1]-old[e.name][0]).length*1000, 1), round(e.length*1000, 1)]}
    for k, joint in FOLLOW.items():
        e = eb.get(f'R {k}')
        if e is None:
            continue
        d = eb[f'R {joint}'].head-old[f'R {joint}'][0]
        e.head += d
        e.tail += d
        report[e.name] = {'head_moved_mm': round(d.length*1000, 1)}
    bpy.ops.object.mode_set(mode='OBJECT')
    rig.data.pose_position = pose_position
    view.objects.active = prev
    bpy.context.view_layer.update()
    for o, m in keep.items():
        o.matrix_world = m
    bpy.context.view_layer.update()
    drift = max(((o.matrix_world.translation-m.translation).length for o, m in keep.items()), default=0.0)
    rig['hs_arm_fit'] = 'M6: R upperarm/forearm/hand refit to the R arm mesh seams (L seam offsets mirrored).'
    report['kept_meshes'] = len(keep)
    report['kept_drift_mm'] = round(drift*1000, 3)
    return report
