"""Full-body rig fixes applied on load (item 9i, FULL-AUDIT M1-M3, S4, S5, S8, m6), after handfix.

- M1: every Shrinkwrap is removed (Blender-only, it hid the tabard in the body and does not export). The TABARD
  pieces take the weights of the body flap under them (tabard_follow) and sit TABARD_LIFT further off it. The
  back panel flares out toward its hem (BACK_FLARE, 0 at the waist) so the seat clears at rest; motion
  clearance comes from tabardpass.py.
- M2/S4: the collar skin (|x - MID_X| < NECK_R) gets a smooth chest -> neck -> head gradient by height
  (the head bone starts at z 1.68, but skin down to z 1.53 carried 40-50% head, so every head pitch dragged
  the collar and cowl into the back hardware). The scapula shells and back node move out of the skin.
- M3/S5: hard-surface parts move as blocks (BLOCKS): the chest core set, each upper chest plate, each rib
  plate, each abdomen plate with its slits, and each sigil ring take one uniform set of weights, sampled from
  the skin (or cloth) under the part, so they ride the skin without bending or tearing.
- m6 and the rest: conductors, neck cables and sigil stems take their weights from the skin (or cloth) under
  them, so they follow it instead of tearing across it.
- S3: thigh weights fade into the pelvis from 3 to 10 cm above the hip pivot (they reached 12 cm).
- S8: the shins are re-rolled so local X is the knee hinge, and their IK is locked to X (knee_hinges).
- S6: forearm twist bones take the elbow half of the forearm skin and half the forearm roll (forearm_twist).
Skinned meshes are unchanged at rest; bone-parented meshes keep their world placement.
"""
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

import hs_anim as H

NECK_R = 0.17
NECK_BAND = (1.50, 1.64)     # z: chest -> neck (front; the back band starts higher, over the trapezius)
NECK_BAND_BACK = (1.60, 1.72)
HEAD_BAND = (1.66, 1.80)     # z: neck -> head
HEAD_BAND_BACK = (1.70, 1.84)
BACK_FLARE = 0.016           # m at the back panel hem (+Y), 0 at the waist
TABARD_LIFT = 0.005          # m every TABARD piece sits further off the body flap
FLARE_TOP, FLARE_SPAN = 1.10, 0.35
SHELL_SHIFT = Vector((0.010, 0.020, -0.030))   # L (x mirrored for R)
NODE_SHIFT = Vector((0.0, 0.006, -0.020))
# Hard parts move as one block: every vertex of a set gets the skin weights found under the set's anchor part
# (a linear blend of the bones there: stays on the skin, no tearing; shrink < 4% up to ~35 deg between them).
BLOCKS = [('CHEST | V17 core housing rim', ('CHEST | V11 core hot centre', 'CHEST | V11 core ring',
                                            'CHEST | V17 core drop slit', 'CHEST | V17 core housing rim')),
          ('CHEST | V17 upper chest plate L', ('CHEST | V17 upper chest plate L',)),
          ('CHEST | V17 upper chest plate R', ('CHEST | V17 upper chest plate R',)),
          ('CHEST | V17 rib plate L', ('CHEST | V17 rib plate L',)),
          ('CHEST | V17 rib plate R', ('CHEST | V17 rib plate R',)),
          ('ABDOMEN | V17 segment plate 1', ('ABDOMEN | V17 segment plate 1', 'ABDOMEN | V17 segment slit 1',
                                             'ABDOMEN | V17 side slit L1', 'ABDOMEN | V17 side slit R1')),
          ('ABDOMEN | V17 segment plate 2', ('ABDOMEN | V17 segment plate 2', 'ABDOMEN | V17 segment slit 2',
                                             'ABDOMEN | V17 side slit L2', 'ABDOMEN | V17 side slit R2')),
          ('ABDOMEN | V17 segment plate 3', ('ABDOMEN | V17 segment plate 3', 'ABDOMEN | V17 segment slit 3'))]
UNDER = ('CHEST | V17 core housing rim', 'CHEST | V17 upper chest plate L', 'CHEST | V17 upper chest plate R',
         'CHEST | V17 rib plate L', 'CHEST | V17 rib plate R', 'ABDOMEN | V17 segment plate 1',
         'ABDOMEN | V17 segment plate 2', 'ABDOMEN | V17 segment plate 3')   # block anchors the skin follows
UNDER_GAP, UNDER_FEATHER = 0.02, 0.04      # m from the plate: full follow, then back to the skin's own weights
UNDER_BONES = {'pelvis', 'spine', 'chest', 'neck'}
HIP_FADE = (0.03, 0.10)      # m above the hip pivot: thigh weight fades into the pelvis (S3)
TWIST_BLEND = (0.15, 0.85)   # forearm fraction (elbow 0 -> wrist 1) over which skin moves from twist bone to forearm
SKIN_FOLLOW = ('BACK | V11 lower spine conductor', 'BACK | V11 upper spine conductor', 'NECK |')
CLOTH_FOLLOW = {'TABARD | V11 back sigil stem': 'TABARD | V11 back cloth',
                'TABARD | V11 front sigil stem': 'TABARD | V11 front cloth'}


def smooth(a, b, x):
    t = min(1.0, max(0.0, (x-a)/(b-a)))
    return t*t*(3-2*t)


def own_mesh(o):
    if o.data.users > 1:
        o.data = o.data.copy()
    return o.data


def remove_shrinkwrap():
    n = 0
    for o in bpy.data.objects:
        for m in list(getattr(o, 'modifiers', [])):
            if m.type == 'SHRINKWRAP':
                o.modifiers.remove(m)
                n += 1
    return n


def set_weights(o, weights):
    """weights: per vertex {group: w}. Replaces every existing assignment."""
    for g in list(o.vertex_groups):
        g.remove(range(len(o.data.vertices)))
    groups = {}
    for i, w in enumerate(weights):
        for name, v in w.items():
            if v <= 1e-4:
                continue
            g = groups.get(name) or o.vertex_groups.get(name) or o.vertex_groups.new(name=name)
            groups[name] = g
            g.add([i], v, 'REPLACE')


def surface_weights(src, deform=None, keep=None):
    """keep(vertex weights of a polygon) -> bool: sample only the polygons it accepts."""
    names = {g.index: g.name for g in src.vertex_groups}
    sw = [{names[g.group]: g.weight for g in v.groups if deform is None or names[g.group] in deform}
          for v in src.data.vertices]
    smw = src.matrix_world
    sv = [smw @ v.co for v in src.data.vertices]
    polys = [tuple(p.vertices) for p in src.data.polygons]
    if keep is not None:
        polys = [q for q in polys if keep([sw[i] for i in q])]
    tree = BVHTree.FromPolygons(sv, polys)

    def at(q):
        loc, _, idx, _ = tree.find_nearest(q)
        vs = polys[idx]
        iw = [1.0/max((sv[i]-loc).length, 1e-5) for i in vs]
        tot = sum(iw)
        w = {}
        for i, k in zip(vs, iw):
            for g, x in sw[i].items():
                w[g] = w.get(g, 0.0)+x*k/tot
        s = sum(w.values()) or 1.0
        return {g: x/s for g, x in w.items()}
    return at


def centroid(o):
    mw = o.matrix_world
    return sum((mw @ v.co for v in o.data.vertices), Vector())/max(1, len(o.data.vertices))


def block(objs, w):
    for o in objs:
        own_mesh(o)
        set_weights(o, [w]*len(o.data.vertices))


def neck_gradient(body, deform):
    """Collar reweight: the chest+neck+head share of each vertex is redistributed by height."""
    names = {g.index: g.name for g in body.vertex_groups}
    mw = body.matrix_world
    weights, changed = [], 0
    for v in body.data.vertices:
        w = {names[g.group]: g.weight for g in v.groups if names[g.group] in deform}
        c = mw @ v.co
        dx = abs(c.x-H.MID_X)
        f = 1.0-smooth(NECK_R-0.05, NECK_R, dx)
        a = sum(w.get(k, 0.0) for k in ('chest', 'neck', 'head'))
        if f > 0 and a > 0.05 and 1.40 < c.z < 1.98:
            b = smooth(-0.02, 0.06, c.y)
            h = smooth(*[x+b*(y-x) for x, y in zip(HEAD_BAND, HEAD_BAND_BACK)], c.z)
            n = smooth(*[x+b*(y-x) for x, y in zip(NECK_BAND, NECK_BAND_BACK)], c.z)*(1-h)
            prof = {'chest': 1-n-h, 'neck': n, 'head': h}
            for k in ('chest', 'neck', 'head'):
                w[k] = f*prof[k]*a+(1-f)*w.get(k, 0.0)
            changed += 1
        weights.append(w)
    set_weights(body, weights)
    return changed


def hip_fade(body, rig):
    """S3: thigh weights reached 12 cm above the hip pivot; above HIP_FADE they blend into the pelvis."""
    names = {g.index: g.name for g in body.vertex_groups}
    mw = body.matrix_world
    pivots = {f'{s} thigh': rig.data.bones[f'{s} thigh'].head_local.z for s in H.SIDES}
    weights, changed = [], 0
    for v in body.data.vertices:
        w = {names[g.group]: g.weight for g in v.groups}
        z = (mw @ v.co).z
        for t, pz in pivots.items():
            x = w.get(t, 0.0)
            k = smooth(*HIP_FADE, z-pz)
            if x > 0 and k > 0:
                w[t] = x*(1-k)
                w['pelvis'] = w.get('pelvis', 0.0)+x*k
                changed += 1
        weights.append(w)
    set_weights(body, weights)
    return changed


def transfer(o, src, deform=None):
    """Weights of each vertex of o = the weights at the nearest point of src's surface (rest pose)."""
    own_mesh(o)
    at = surface_weights(src, deform)
    mw = o.matrix_world
    set_weights(o, [at(mw @ v.co) for v in o.data.vertices])


def flap_hem(body, rig):
    """M1: the back flap's hem row (z ~0.43, knee height) was weighted 100% pelvis, so every back swing folded
    the hem into a wedge under the cloth. Pelvis-dominant verts inside a flap's footprint below its upper bone
    take the weights of the nearest flap vert."""
    names = {g.index: g.name for g in body.vertex_groups}
    mw = body.matrix_world
    co = [mw @ v.co for v in body.data.vertices]
    ws = [{names[g.group]: g.weight for g in v.groups} for v in body.data.vertices]
    dom = [max(w, key=w.get) if w else '' for w in ws]
    n = 0
    for chain in (H.TABARD_FRONT, H.TABARD_BACK):
        flap = [i for i, d in enumerate(dom) if d in chain]
        lo = [min(co[i][k] for i in flap)-0.01 for k in range(3)]
        hi = [max(co[i][k] for i in flap)+0.01 for k in range(3)]
        below = rig.data.bones[chain[1]].head_local.z
        for i, d in enumerate(dom):
            c = co[i]
            if d == 'pelvis' and c.z < below and all(lo[k] <= c[k] <= hi[k] for k in range(2)) and c.z >= lo[2]-0.05:
                j = min(flap, key=lambda f: (co[f]-c).length)
                ws[i] = dict(ws[j])
                n += 1
    if n:
        set_weights(body, ws)
    return n


def under_blocks(body, sets):
    """M3: skin under a rigid plate follows the plate. sets: [(objects, block weights)]. Torso verts within
    UNDER_GAP of a plate take its weights, feathered back to their own over UNDER_FEATHER, so the plate's edges
    stop sinking where the skin under them used to bend with a different bone mix."""
    names = {g.index: g.name for g in body.vertex_groups}
    mw = body.matrix_world
    trees = []
    for objs, w in sets:
        verts, polys = [], []
        for o in objs:
            om = o.matrix_world
            base = len(verts)
            verts += [om @ v.co for v in o.data.vertices]
            polys += [tuple(base+i for i in p.vertices) for p in o.data.polygons]
        trees.append((BVHTree.FromPolygons(verts, polys), w))
    weights, n = [], 0
    for v in body.data.vertices:
        w = {names[g.group]: g.weight for g in v.groups}
        if w and set(w) <= UNDER_BONES:
            c = mw @ v.co
            best = None
            for tree, bw in trees:
                hit = tree.find_nearest(c, UNDER_GAP+UNDER_FEATHER)
                if hit[0] is not None and (best is None or hit[3] < best[0]):
                    best = (hit[3], bw)
            if best is not None:
                t = 1.0-smooth(UNDER_GAP, UNDER_GAP+UNDER_FEATHER, best[0])
                w = {k: (1-t)*w.get(k, 0.0)+t*best[1].get(k, 0.0) for k in set(w) | set(best[1])}
                n += 1
        weights.append(w)
    set_weights(body, weights)
    return n


def wrist_cuffs(rig, body, deform):
    """m4/hand-parts contact: the wrist cuff was rigid on the hand bone, so wrist flexion swung its rim 15-35 mm
    into the forearm skin. It becomes skinned with the weights of that arm's skin under it (world placement
    unchanged), so it rides the wrist skin."""
    n = 0
    for s in H.SIDES:
        o = bpy.data.objects.get(f'{s} HAND | wrist cuff')
        if o is None or o.parent_type != 'BONE':
            continue
        own_mesh(o)
        mw = o.matrix_world.copy()
        arm = (f'{s} forearm', f'{s} forearm twist', f'{s} hand')
        at = surface_weights(body, deform, keep=lambda ws: all(max(w, key=w.get, default='') in arm for w in ws))
        o.parent, o.parent_type = rig, 'OBJECT'
        o.matrix_world = mw
        set_weights(o, [at(mw @ v.co) for v in o.data.vertices])
        m = o.modifiers.new('Armature', 'ARMATURE')
        m.object = rig
        n += 1
    return n


def skin_blocks(rig, body, deform, names):
    """M2: bone-parented hard parts (the scapula shells) become rigid skinned blocks with the skin weights under
    their centre, and the skin under them follows them (under_blocks); on the scapula bone they sank 10-22 mm into
    the upper back as the chest arched and the arms swung back. World placement unchanged."""
    at = surface_weights(body, deform)
    sets = []
    for name in names:
        o = bpy.data.objects.get(name)
        if o is None or o.parent_type != 'BONE':
            continue
        own_mesh(o)
        mw = o.matrix_world.copy()
        w = at(centroid(o))
        o.parent, o.parent_type = rig, 'OBJECT'
        o.matrix_world = mw
        block([o], w)
        o.modifiers.new('Armature', 'ARMATURE').object = rig
        sets.append(([o], w))
    under_blocks(body, sets)
    return len(sets)


def tabard_follow(body, deform):
    """M1: every TABARD piece takes the weights of the body flap under it (the body's own loincloth panels on the
    tabard bones), so cloth, trims and edges ride the flap instead of cutting through it when the chain bends.
    Only flap polygons (any vertex on a tabard bone, including the pelvis-heavy flap top) are sampled, so edge
    pieces never pick up the thighs. The sigil parts follow afterwards (block / CLOTH_FOLLOW)."""
    at = surface_weights(body, deform, keep=lambda ws: any(
        x > 0.01 for w in ws for g, x in w.items() if g.startswith('tabard')))
    n = 0
    for o in bpy.data.objects:
        if (o.type == 'MESH' and o.name.startswith('TABARD |') and 'sigil' not in o.name
                and any(x.type == 'ARMATURE' for x in o.modifiers)):
            own_mesh(o)
            mw = o.matrix_world
            set_weights(o, [at(mw @ v.co) for v in o.data.vertices])
            n += 1
    return n


def lift_tabard():
    """Every TABARD piece moves TABARD_LIFT off the body flap (front -Y, back +Y): the flap's large faces bend
    a little away from points skinned over them, and this is the margin (see tabardpass.BEND_LIMIT)."""
    n = 0
    for o in bpy.data.objects:
        if o.type == 'MESH' and o.name.startswith('TABARD |'):
            me = own_mesh(o)
            d = Vector((0, TABARD_LIFT if ' back ' in o.name else -TABARD_LIFT, 0))
            mw, inv = o.matrix_world, o.matrix_world.inverted()
            for v in me.vertices:
                v.co = inv @ (mw @ v.co+d)
            n += 1
    return n


def flare_back(objs):
    n = 0
    for o in objs:
        me = own_mesh(o)
        mw, inv = o.matrix_world, o.matrix_world.inverted()
        for v in me.vertices:
            c = mw @ v.co
            d = BACK_FLARE*smooth(0.0, 1.0, (FLARE_TOP-c.z)/FLARE_SPAN)
            if d > 0:
                v.co = inv @ (c+Vector((0, d, 0)))
                n += 1
    return n


def knee_hinges(rig):
    """The v18 rest knees bend sideways with the mesh (the R one 54 mm off the hip-ankle line), and Blender's IK
    bends along the rest bend plane, so knees hinged 60-80 deg off the shin's X and the thighs twisted to aim
    them. The pivots stay on the mesh; each shin is rolled so local X is the normal of the hip-ankle-pole
    plane, and its IK is locked to X (lock_ik_y/z), so the knee hinges about X."""
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
    out = {}
    for s in H.SIDES:
        hip, sh = eb[f'{s} thigh'].head, eb[f'{s} shin']
        ankle, pole = sh.tail, eb[f'{s} knee pole'].head
        hinge = (ankle-hip).cross(pole-hip).normalized()
        if hinge.x < 0:
            hinge = -hinge
        y = sh.y_axis.copy()
        hinge = (hinge-y*hinge.dot(y)).normalized()
        before = sh.x_axis.angle(hinge)
        sh.align_roll(hinge.cross(y))
        out[s] = {'roll_change_deg': round(before*57.2958, 1)}
    bpy.ops.object.mode_set(mode='OBJECT')
    for s in H.SIDES:
        pb = rig.pose.bones[f'{s} shin']
        pb.lock_ik_y = pb.lock_ik_z = True
    rig.data.pose_position = pose_position
    view.objects.active = prev
    return out


def forearm_twist(rig):
    """S6: `<s> forearm twist` (deform, child of the forearm, same axis, pivot at the elbow). handpass.set_twist
    counter-rolls it by TWIST_SHARE of the forearm's roll, and the elbow half of the forearm skin blends onto it,
    so a 90 deg roll twists the skin 45 deg at the elbow seam and 45 deg along the forearm instead of 90 at the
    seam."""
    view = bpy.context.view_layer
    prev = view.objects.active
    for o in view.objects:
        o.select_set(False)
    view.objects.active = rig
    rig.select_set(True)
    pose_position = rig.data.pose_position
    rig.data.pose_position = 'REST'
    bpy.ops.object.mode_set(mode='EDIT')
    eb = rig.data.edit_bones
    axes = {}
    for s in H.SIDES:
        fa = eb[f'{s} forearm']
        tb = eb.new(f'{s} forearm twist')
        tb.head = fa.head.copy()
        tb.tail = fa.head+(fa.tail-fa.head)*0.5
        tb.align_roll(fa.z_axis)
        tb.parent = fa
        tb.use_connect = False
        tb.use_deform = True
        axes[s] = (fa.head.copy(), fa.tail.copy())
    bpy.ops.object.mode_set(mode='OBJECT')
    rig.data.pose_position = pose_position
    view.objects.active = prev
    for s in H.SIDES:
        pb = rig.pose.bones[f'{s} forearm twist']
        pb.rotation_mode = 'QUATERNION'
    n = 0
    targets = [bpy.data.objects[H.BODY]]+[o for o in bpy.data.objects if o.type == 'MESH'
                                          and o.name.startswith(('L ARM |', 'R ARM |'))]
    for o in targets:
        names = {g.index: g.name for g in o.vertex_groups}
        if not any(f'{s} forearm' in names.values() for s in H.SIDES):
            continue
        own_mesh(o)
        mw = o.matrix_world
        weights = []
        for v in o.data.vertices:
            w = {names[g.group]: g.weight for g in v.groups}
            for s in H.SIDES:
                wf = w.get(f'{s} forearm', 0.0)
                if wf <= 0:
                    continue
                a, b = axes[s]
                t = (mw @ v.co-a).dot(b-a)/(b-a).length_squared
                share = 1.0-smooth(*TWIST_BLEND, t)
                w[f'{s} forearm'] = wf*(1-share)
                w[f'{s} forearm twist'] = w.get(f'{s} forearm twist', 0.0)+wf*share
                n += 1
            weights.append(w)
        set_weights(o, weights)
    return {'verts': n}


def apply(rig):
    """Idempotent (rig['hs_body_fix'])."""
    if rig.get('hs_body_fix'):
        return {'already': True}
    pose_position = rig.data.pose_position
    rig.data.pose_position = 'REST'
    bpy.context.view_layer.update()
    deform = {b.name for b in rig.data.bones if b.use_deform}
    body = bpy.data.objects[H.BODY]
    report = {'shrinkwrap_removed': remove_shrinkwrap()}
    report['neck_verts'] = neck_gradient(body, deform)
    report['hip_verts'] = hip_fade(body, rig)
    skin = surface_weights(body, deform)
    blocks, under = {}, []
    for anchor, names in BLOCKS:
        objs = [bpy.data.objects[n] for n in names if n in bpy.data.objects]
        if anchor in bpy.data.objects and objs:
            w = skin(centroid(bpy.data.objects[anchor]))
            block(objs, w)
            blocks[anchor.split('|')[1].strip()] = {k: round(v, 2) for k, v in w.items() if v > 0.01}
            if anchor in UNDER:
                under.append((objs, w))
    report['under_block_verts'] = under_blocks(body, under)
    report['flap_hem_verts'] = flap_hem(body, rig)
    report['tabard_follow'] = tabard_follow(body, deform)
    for side, cloth in (('back', 'TABARD | V11 back cloth'), ('front', 'TABARD | V11 front cloth')):
        ring = bpy.data.objects.get(f'TABARD | V11 {side} sigil ring')
        if ring and cloth in bpy.data.objects:
            w = surface_weights(bpy.data.objects[cloth])(centroid(ring))
            block([ring], w)
            blocks[f'{side} sigil ring'] = {k: round(v, 2) for k, v in w.items() if v > 0.01}
    report['blocks'] = blocks
    m = 0
    for o in bpy.data.objects:
        if o.type == 'MESH' and o.name.startswith(SKIN_FOLLOW) and any(x.type == 'ARMATURE' for x in o.modifiers):
            transfer(o, body, deform)
            m += 1
        for pre, src in CLOTH_FOLLOW.items():
            if o.name.startswith(pre) and src in bpy.data.objects:
                transfer(o, bpy.data.objects[src])
                m += 1
    report['weight_transfer'] = m
    report['back_flare_verts'] = flare_back([o for o in bpy.data.objects if o.type == 'MESH'
                                             and o.name.startswith('TABARD |') and ' back ' in o.name])
    report['tabard_lift'] = lift_tabard()
    for o in bpy.data.objects:
        if o.parent != rig or o.parent_type != 'BONE':
            continue
        d = None
        if o.name.endswith('small scapula shell'):
            d = SHELL_SHIFT.copy()
            if o.name.startswith('R'):
                d.x = -d.x
        elif o.name.startswith('BACK |') and 'node' in o.name:
            d = NODE_SHIFT
        if d is not None:
            mat = o.matrix_world.copy()
            mat.translation += d
            o.matrix_world = mat
    bpy.context.view_layer.update()
    report['skin_blocks'] = skin_blocks(rig, body, deform, [o.name for o in bpy.data.objects
                                                            if o.name.endswith('small scapula shell')])
    keep = {o: o.matrix_world.copy() for o in bpy.data.objects
            if o.parent == rig and o.parent_type == 'BONE' and o.parent_bone.endswith(('thigh', 'shin'))}
    report['knees'] = knee_hinges(rig)
    report['forearm_twist'] = forearm_twist(rig)
    report['wrist_cuffs'] = wrist_cuffs(rig, body, deform | {f'{s} forearm twist' for s in H.SIDES})
    bpy.context.view_layer.update()
    for o, mat in keep.items():
        o.matrix_world = mat
    rig.data.pose_position = pose_position
    bpy.context.view_layer.update()
    rig['hs_body_fix'] = ('9i: Shrinkwrap removed, tabard pieces on the body flap weights, back panel flare, '
                          'collar gradient, chest/abdomen/sigil parts as '
                          'skin-sampled blocks (skin under plates and shells follows them), skin-following '
                          'conductors and wrist cuffs, shells/back node out of the skin, '
                          'shin X as the knee hinge (IK locked to X), hip weight fade, forearm twist bones.')
    return report
