"""Shoulder corrective applied on load (v7 and later checkpoints): half-rotation helper bones and a
smoothed chest/upper-arm weight blend, so high arm raises stop crumpling the fused shoulder.

The v18 shoulder skin jumps from ~0.9 upper arm to 0 across one coarse edge ring (4-15 cm edges),
so raises tear it open and flip the armpit faces. Fix, per side:
  - `<s> shoulder`: deform bone at the upper-arm head (same rest frame, parent chest) that follows
    SHOULDER_FOLLOW of the upper arm through a Copy Rotation. It is keyed by `bake` and the
    constraint muted afterwards (as the pauldrons), so Unity plays it from keys.
  - The upper-arm share t = ua / (ua + chest) is diffused over the mesh near the joint, then split
    chest / helper / upper arm so the effective rotation stays t (the blend runs between rotations
    half as far apart, which is what keeps the volume).
Surface meshes (plates, conductors) take their new t from the nearest body vertex, so they stay on
the skin. Other groups (spine, forearm, hand) keep their weights.
"""
import bpy
import math
from mathutils import Vector
from mathutils.kdtree import KDTree

SHOULDER_FOLLOW = 0.5
CONSTRAINT = 'HS shoulder half follow'
BODY = 'HF BODY | retained UV sculpt, corrected posterior'
RADIUS = 0.24       # diffusion zone around the joint (m)
ITERATIONS = 8
SIDES = ('L', 'R')


def helper(side):
    return f'{side} shoulder'


def add_bones(rig):
    if all(helper(s) in rig.data.bones for s in SIDES):
        return False
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
        ua = eb[f'{s} upperarm']
        h = eb.new(helper(s))
        h.head = ua.head.copy()
        h.tail = ua.head+(ua.tail-ua.head)*0.45
        h.roll = ua.roll
        h.parent = eb['chest']
        h.use_connect = False
        h.use_deform = True
        for c in ua.collections:
            c.assign(h)
    bpy.ops.object.mode_set(mode='OBJECT')
    view.objects.active = prev
    for s in SIDES:
        pb = rig.pose.bones[helper(s)]
        pb.rotation_mode = 'QUATERNION'
        c = pb.constraints.new('COPY_ROTATION')
        c.name = CONSTRAINT
        c.target = rig
        c.subtarget = f'{s} upperarm'
        c.mix_mode = 'REPLACE'
        c.target_space = c.owner_space = 'WORLD'
        c.influence = SHOULDER_FOLLOW
    return True


def deform_weights(obj, rig):
    names = {g.index: g.name for g in obj.vertex_groups}
    out = []
    for v in obj.data.vertices:
        out.append({names[g.group]: g.weight for g in v.groups
                    if g.weight > 0 and names.get(g.group) in rig.data.bones and rig.data.bones[names[g.group]].use_deform})
    return out


def split(t):
    """Upper-arm share t -> (chest, helper, upper arm) with the same effective rotation."""
    f = SHOULDER_FOLLOW
    if t <= f:
        h = t/f
        return 1-h, h, 0.0
    u = (t-f)/(1-f)
    return 0.0, 1-u, u


def write(obj, weights_by_vertex, side, t_new):
    groups = {n: obj.vertex_groups.get(n) or obj.vertex_groups.new(name=n)
              for n in ('chest', helper(side), f'{side} upperarm')}
    changed = 0
    for i, t in t_new.items():
        ws = weights_by_vertex[i]
        share = ws.get('chest', 0)+ws.get(f'{side} upperarm', 0)
        if share <= 1e-6:
            continue
        c, h, u = split(t)
        for n, w in (('chest', c), (helper(side), h), (f'{side} upperarm', u)):
            if w*share > 1e-5:
                groups[n].add([i], w*share, 'REPLACE')
            else:
                groups[n].remove([i])
        changed += 1
    return changed


def smooth_body(body, rig):
    me = body.data
    co = [body.matrix_world @ v.co for v in me.vertices]
    nbrs = [[] for _ in me.vertices]
    for e in me.edges:
        a, b = e.vertices
        nbrs[a].append(b)
        nbrs[b].append(a)
    report = {}
    tfield = {}
    for s in SIDES:
        ws = deform_weights(body, rig)
        joint = rig.matrix_world @ rig.data.bones[f'{s} upperarm'].head_local
        ua = f'{s} upperarm'
        t = {}
        free = set()
        for i, w in enumerate(ws):
            share = w.get('chest', 0)+w.get(ua, 0)
            if share <= 1e-6:
                continue
            t[i] = w.get(ua, 0)/share
            # Chest-side verts on the other half of the body never pick up this arm.
            if (co[i]-joint).length < RADIUS and (co[i].x-joint.x*0.35)*math.copysign(1, joint.x) > 0:
                free.add(i)
        before = dict(t)
        for _ in range(ITERATIONS):
            nxt = {}
            for i in free:
                ns = [t[j] for j in nbrs[i] if j in t]
                if ns:
                    # Fade the diffusion out toward the zone edge so it meets the untouched weights.
                    k = 0.5*max(0.0, 1-((co[i]-joint).length/RADIUS)**2)
                    nxt[i] = (1-k)*t[i]+k*sum(ns)/len(ns)
            t.update(nxt)
        moved = {i: t[i] for i in free}
        write(body, ws, s, moved)
        tfield[s] = t
        report[s] = {'free_verts': len(free), 'max_change': round(max((abs(t[i]-before[i]) for i in free), default=0), 3)}
    return report, tfield, co


def follow_body(obj, rig, tfield, body_co):
    """Surface meshes take t from the nearest body vertex (own half of the body only)."""
    if not any(f'{s} upperarm' in w for w in deform_weights(obj, rig) for s in SIDES):
        return 0
    n = 0
    for s in SIDES:
        ws = deform_weights(obj, rig)
        joint_x = rig.data.bones[f'{s} upperarm'].head_local.x
        tree = KDTree(len(tfield[s]))
        for i in tfield[s]:
            tree.insert(body_co[i], i)
        tree.balance()
        t_new = {}
        for i, v in enumerate(obj.data.vertices):
            w = ws[i]
            wc = obj.matrix_world @ v.co
            if w.get(f'{s} upperarm', 0)+w.get('chest', 0) <= 1e-6 or (wc.x-joint_x*0.35)*math.copysign(1, joint_x) <= 0:
                continue
            _, j, _ = tree.find(wc)
            t_new[i] = tfield[s][j]
        n += write(obj, ws, s, t_new)
    return n


def apply(rig):
    """Idempotent: adds the helper bones and re-weights once per loaded file."""
    if rig.get('hs_shoulder_fix'):
        return {'already': True}
    add_bones(rig)
    body = bpy.data.objects[BODY]
    report, tfield, co = smooth_body(body, rig)
    others = {}
    for o in bpy.data.objects:
        if o is body or o.type != 'MESH' or not any(m.type == 'ARMATURE' and m.object == rig for m in o.modifiers):
            continue
        k = follow_body(o, rig, tfield, co)
        if k:
            others[o.name] = k
    rig['hs_shoulder_fix'] = (f'v7 shoulder corrective: helper bones follow {SHOULDER_FOLLOW:.0%} of the upper arm '
                              '(keyed; constraint muted after bake), chest/upper-arm blend diffused around the joint.')
    return {'body': report, 'surface_meshes': others}
