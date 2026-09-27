"""Pauldron drive (9g), run by hs_anim.bake on every frame after the hand pass.

The pads ride on the shoulder instead of copying 40% of the full upper-arm rotation about the shoulder joint
(which drove their inner edge into the collar on big raises and their underside into the deltoid).
Per side, from the upper arm's swing relative to the chest (twist ignored), in rest armature axes:
  - the pad rotates by a share of that swing per axis: raise sideways (about Y) SHARE_Y, forward/back (about X)
    SHARE_X, horizontal (about Z) SHARE_Z
  - about a hinge on its inner (collar-side) edge, so the outer edge lifts with the arm while the inner edge
    stays on the collar
  - plus a lift up and out as the arm rises above its rest angle.
Then two clearance steps, each a pure function of the frame:
  - body: where the collar, neck or upper-arm skin still ends up inside a pad (the neck bends onto it in the
    aims, Arc Step, Spawn), the pad lifts up and out just far enough (`apply`, before capture)
  - halo: after the clip's own halo follow-through (bake `post`), the whole ring is pushed up and back just far
    enough that the lower arcs clear the pads by HALO_CLEAR (`halo_clear`, on the captured matrices).
It depends only on the current frame's pose, so seams stay exact. The pads are keyed by bake like any FK bone.
"""
import math

import bpy
from mathutils import Matrix, Quaternion, Vector
from mathutils.bvhtree import BVHTree

import hs_anim as H

SHARE_X, SHARE_Y, SHARE_Z = 0.25, 0.5, 0.2
LIFT, OUT = 0.015, 0.008          # m at full lift
LIFT_RANGE = 90.0                 # deg above the rest raise for full lift
HINGE_FRACTION = 0.2              # inner fraction of the pad's width averaged into the hinge point
PAD_MESH = '{} SHOULDER | V17 pauldron upper'
BODY_NEAR = 0.08                  # body verts within this of a rest pad are tested every frame
BODY_TOL = 0.0015                 # m of skin inside a pad (beyond rest) left alone
PUSH_DIR = (0.35, 0.0, 1.0)       # pad push-off direction (x mirrored per side), normalised
HALO_ARC = {'L': 'halo 2', 'R': 'halo 3'}
HALO_ARC_MESH = {'L': 'HALO | independent copper arc 2', 'R': 'HALO | independent copper arc 3'}
HALO_BONES = ('halo root', 'halo 1', 'halo 2', 'halo 3', 'halo 4')
HALO_CLEAR = 0.004                # m kept between the lower arcs and the pads
YIELD_CLEAR = HALO_CLEAR+0.004    # the pads yield to this gap before the halo springs (bake `post`) are added
HALO_DIR = Vector((0.0, 0.45, 1.0)).normalized()   # up and back
# residual halo nudge directions (rest axes, carried by the chest); per clip the one needing the least travel is kept
HALO_DIRS = [HALO_DIR, Vector((0.0, 1.0, 0.0)), Vector((0.0, 1.0, 0.45)).normalized(), Vector((0.0, 0.0, 1.0))]
SMOOTH_R = 3                      # frames: envelope + blur radius for the pad solve and the halo nudge
DIRS = [Vector(d).normalized() for d in ((1, 0.2, 0.1), (-0.3, 1, 0.2), (0.1, -0.2, 1), (-1, -0.4, -0.3),
                                         (0.2, 0.3, -1))]


def inside(tree, q):
    votes = 0
    for k, d in enumerate(DIRS):
        n, o = 0, q
        for _ in range(16):
            hit = tree.ray_cast(o, d)
            if hit[0] is None:
                break
            n += 1
            o = hit[0]+d*1e-5
        votes += n % 2
        if votes >= 3 or votes+(len(DIRS)-1-k) < 3:
            break
    return votes >= 3


def eval_mesh(o, inv, dg):
    ev = o.evaluated_get(dg)
    me = ev.to_mesh()
    v = [inv @ (o.matrix_world @ x.co) for x in me.vertices]
    p = [tuple(x.vertices) for x in me.polygons]
    ev.to_mesh_clear()
    return v, p


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t*t*(3-2*t)


def clearance(arc, ap, pad, pp):
    """Signed gap (m) between two closed meshes given as (points, polys): negative = deepest penetration
    either way, capped at 3*HALO_CLEAR when they are far apart."""
    ptree = BVHTree.FromPolygons(pad, pp)
    c = HALO_CLEAR*3
    for q in arc:
        loc, _, _, d = ptree.find_nearest(q, HALO_CLEAR*3)
        if loc is not None:
            c = min(c, -d if inside(ptree, q) else d)
    if c < HALO_CLEAR*3:
        atree = BVHTree.FromPolygons(arc, ap)
        for q in pad:
            loc, _, _, d = atree.find_nearest(q, 0.05)
            if loc is not None and inside(atree, q):
                c = min(c, -d)
    return c


def smooth_series(vals, n, env, pins=()):
    """Envelope (env=max/min over +-SMOOTH_R) then box blur over +-SMOOTH_R. The first n values are the period
    when n < len(vals) (loop: the closing frame repeats frame 0); otherwise the ends clamp and ease back to the raw
    values over 2*SMOOTH_R frames, so the first/last frames (seams) are exactly the per-frame solve. A loop eases
    back to the raw value the same way around each index in `pins` (its seam frames), so a one-shot clip handing
    off there meets it exactly. Every value lies between its raw value and the enveloped blur, so it keeps at
    least its own frame's clearance."""
    m = len(vals)

    def at(v, i):
        return v[i % n] if n < m else v[min(max(i, 0), m-1)]
    r = range(-SMOOTH_R, SMOOTH_R+1)
    e = [env(at(vals, i+j) for j in r) for i in range(m)]
    b = [sum(at(e, i+j) for j in r)/len(r) for i in range(m)]
    if n < m:
        for i in range(n):
            if pins:
                d = min(min(abs(i-q) % n, n-abs(i-q) % n) for q in pins)
                w = smooth(d/(2*SMOOTH_R))
                b[i] = vals[i]+w*(b[i]-vals[i])
        b[n:] = b[:m-n]
    else:
        for i in range(m):
            w = smooth(min(i, m-1-i)/(2*SMOOTH_R))
            b[i] = vals[i]+w*(b[i]-vals[i])
    return b


class PadPass:
    def __init__(self, poser):
        self.p = poser
        self.hist = []
        rig = poser.rig
        self.enabled = all(f'{s} pauldron' in poser.pb for s in H.SIDES)
        if not self.enabled:
            return
        inv = self.inv = rig.matrix_world.inverted()
        self.hinge, self.pad_rest, self.arc_rest = {}, {}, {}
        saved = rig.data.pose_position
        rig.data.pose_position = 'REST'
        bpy.context.view_layer.update()
        dg = bpy.context.evaluated_depsgraph_get()
        self.body = bpy.data.objects[H.BODY]
        bv, _ = eval_mesh(self.body, inv, dg)
        near = set()
        for s in H.SIDES:
            pts, polys = eval_mesh(bpy.data.objects[PAD_MESH.format(s)], inv, dg)
            self.pad_rest[s] = (pts, polys)
            d = [abs(q.x-H.MID_X) for q in pts]
            lo, hi = min(d), max(d)
            inner = [q for q, di in zip(pts, d) if di <= lo+HINGE_FRACTION*(hi-lo)]
            self.hinge[s] = sum(inner, Vector())/len(inner)
            tree = BVHTree.FromPolygons(pts, polys)
            near |= {i for i, q in enumerate(bv) if tree.find_nearest(q, BODY_NEAR)[0] is not None}
            arc = bpy.data.objects.get(HALO_ARC_MESH[s])
            if arc is not None:
                self.arc_rest[s] = eval_mesh(arc, inv, dg)
        self.near = sorted(near)
        rig.data.pose_position = saved
        bpy.context.view_layer.update()
        self.body_rest = {s: self.body_depth(s, [bv[i] for i in self.near], Matrix.Identity(4)) for s in H.SIDES}
        self.push = {s: Vector((PUSH_DIR[0]*(1 if s == 'L' else -1), PUSH_DIR[1], PUSH_DIR[2])).normalized()
                     for s in H.SIDES}
        self.rest_ch = poser.rest['chest']
        self.rest_ua = {s: poser.rest[f'{s} upperarm'] for s in H.SIDES}
        self.rest_pad = {s: poser.rest[f'{s} pauldron'] for s in H.SIDES}
        down = Vector((0, 0, -1))
        self.a0 = {s: (self.rest_ua[s].to_3x3() @ Vector((0, 1, 0))).normalized() for s in H.SIDES}
        self.raise0 = {s: self.a0[s].angle(down) for s in H.SIDES}

    def swing(self, s):
        """(chest matrix, shared swing rotation vector in rest axes, lift weight) for side s this frame."""
        pb = self.p.pb
        m_ch = pb['chest'].matrix.copy()
        r_c = m_ch.to_3x3() @ self.rest_ch.to_3x3().inverted()
        delta = r_c.inverted() @ pb[f'{s} upperarm'].matrix.to_3x3() @ self.rest_ua[s].to_3x3().inverted()
        a0 = self.a0[s]
        a1 = (delta @ a0).normalized()
        q = a0.rotation_difference(a1)
        ang = q.angle
        if ang > math.pi:
            ang -= 2*math.pi
        w = Vector(q.axis)*ang if abs(ang) > 1e-9 else Vector()
        w = Vector((w.x*SHARE_X, w.y*SHARE_Y, w.z*SHARE_Z))
        up = smooth(math.degrees(a1.angle(Vector((0, 0, -1)))-self.raise0[s])/LIFT_RANGE)
        return m_ch, w, up

    def compose(self, s, m_ch, w, up, k, t):
        """Pad matrix for follow weight k (share of swing and lift) and body push-off t (m along push)."""
        wk = w*k
        rp = Quaternion(wk.normalized(), wk.length) if wk.length > 1e-9 else Quaternion()
        hinge = self.hinge[s]
        tr = hinge-rp @ hinge+Vector((math.copysign(OUT, hinge.x-H.MID_X)*up*k, 0.0, LIFT*up*k))+self.push[s]*t
        local = Matrix.Translation(tr) @ rp.to_matrix().to_4x4()
        return m_ch @ self.rest_ch.inverted() @ local @ self.rest_pad[s]

    def arc_clearance(self, s, pad_m, arc_m):
        """Signed gap (m) between side s's lower halo arc (bone matrix arc_m) and its pad (bone matrix pad_m)."""
        av, ap = self.arc_rest[s]
        pv, pp = self.pad_rest[s]
        arc = [arc_m @ self.p.rest[HALO_ARC[s]].inverted() @ v for v in av]
        pm = pad_m @ self.rest_pad[s].inverted()
        return clearance(arc, ap, [pm @ v for v in pv], pp)

    def body_depth(self, s, pts, delta):
        """Deepest body point inside the pad moved by `delta` (armature space, relative to its rest)."""
        rv, polys = self.pad_rest[s]
        tree = BVHTree.FromPolygons([delta @ v for v in rv], polys)
        worst = 0.0
        for q in pts:
            loc, _, _, d = tree.find_nearest(q, 0.05)
            if loc is not None and d > worst and inside(tree, q):
                worst = d
        return worst

    def apply(self):
        """Solve this frame's pads (follow weight k yielding to the halo arcs, then body push-off t), pose them
        and record the solve in self.hist for `finish`."""
        if not self.enabled:
            return {}
        self.p.update()
        dg = bpy.context.evaluated_depsgraph_get()
        ev = self.body.evaluated_get(dg)
        me = ev.to_mesh()
        mw = self.inv @ self.body.matrix_world
        pts = [mw @ me.vertices[i].co for i in self.near]
        ev.to_mesh_clear()
        rec = {}
        for s in H.SIDES:
            m_ch, w, up = self.swing(s)
            k = 1.0
            if s in self.arc_rest:
                arc_m = self.p.pb[HALO_ARC[s]].matrix.copy()

                def gap(kk):
                    return self.arc_clearance(s, self.compose(s, m_ch, w, up, kk, 0.0), arc_m)
                if gap(1.0) < YIELD_CLEAR:
                    lo, hi = 0.0, 1.0
                    if gap(0.0) < YIELD_CLEAR:
                        hi = 0.0
                    else:
                        for _ in range(8):
                            mid = 0.5*(lo+hi)
                            lo, hi = (lo, mid) if gap(mid) < YIELD_CLEAR else (mid, hi)
                        hi = lo
                    k = hi
            t = 0.0
            for _ in range(8):
                delta = self.compose(s, m_ch, w, up, k, t) @ self.rest_pad[s].inverted()
                d = self.body_depth(s, pts, delta)-self.body_rest[s]
                if d <= BODY_TOL:
                    break
                t += d-BODY_TOL*0.5
            self.p.pb[f'{s} pauldron'].matrix = self.compose(s, m_ch, w, up, k, t)
            rec[s] = (m_ch, w, up, k, t)
        self.p.update()
        self.hist.append(rec)
        return {s: (math.degrees(v[1].length*v[3]), v[2]*v[3], v[4]) for s, v in rec.items()}

    def finish(self, caps, frames, loop, pins=()):
        """Smooth the per-frame solves over time and rewrite the pad matrices in caps: the follow weight never
        rises above any neighbour's within SMOOTH_R frames' reach, the push never drops below it (so every frame
        keeps at least its own clearance), then both are box-blurred over SMOOTH_R. `pins`: frame indices a loop
        keeps at its raw solve (seam frames)."""
        if not self.enabled or len(self.hist) != len(frames):
            return {}
        n = len(frames)-1 if loop and len(frames) > 2 else len(frames)
        out = {}
        for s in H.SIDES:
            ks = smooth_series([h[s][3] for h in self.hist], n, min, pins)
            ts = smooth_series([h[s][4] for h in self.hist], n, max, pins)
            for i, f in enumerate(frames):
                m_ch, w, up, _, _ = self.hist[i][s]
                caps[f][f'{s} pauldron'] = self.compose(s, m_ch, w, up, ks[i], ts[i])
            out[s] = {'deg': round(max(math.degrees(h[s][1].length)*k for h, k in zip(self.hist, ks)), 1),
                      'lift': round(max(h[s][2]*k for h, k in zip(self.hist, ks)), 2),
                      'push_mm': round(max(ts)*1000, 1), 'yield_min_k': round(min(ks), 2),
                      'yield_frames': sum(1 for k in ks[:n] if k < 0.999)}
        return {'pad_follow_max': out}

    def halo_clear(self, caps, frames, loop, pins=(), dirs=None):
        """Residual only (the pads already yield in `apply`): after the clip's halo follow-through, nudge the
        ring up/back on captured frames where a lower arc still comes within HALO_CLEAR of its pad. The nudge is
        max-enveloped and blurred over time like the pad solve (loops pinned to the raw need at `pins`).
        `dirs` restricts the chest-space push directions (default HALO_DIRS); a clip handing off to another on a
        frame that needs a push must use that clip's direction."""
        if not self.enabled or len(self.arc_rest) < 2:
            return {}
        r_ch = {f: caps[f]['chest'].to_3x3() @ self.rest_ch.to_3x3().inverted() for f in frames}
        hit = [f for f in frames if any(self.arc_clearance(s, caps[f][f'{s} pauldron'], caps[f][HALO_ARC[s]])
                                        < HALO_CLEAR for s in H.SIDES)]
        if not hit:
            return {'halo_clear_push_max_mm': 0.0, 'halo_clear_frames': 0}

        def need(f, d):
            m = caps[f]
            out = 0.0
            for s in H.SIDES:
                pad_m = m[f'{s} pauldron']

                def gap(t):
                    return self.arc_clearance(s, pad_m, Matrix.Translation(d*t) @ m[HALO_ARC[s]])
                if gap(0.0) >= HALO_CLEAR:
                    continue
                lo, hi = 0.0, 0.01
                while gap(hi) < HALO_CLEAR and hi < 0.24:
                    lo, hi = hi, hi*2
                for _ in range(10):
                    mid = 0.5*(lo+hi)
                    lo, hi = (mid, hi) if gap(mid) < HALO_CLEAR else (lo, mid)
                out = max(out, hi)
            return out
        best = None
        for d0 in dirs or HALO_DIRS:
            nd = {f: need(f, r_ch[f] @ d0) for f in hit}
            worst = max(nd.values())
            if best is None or worst < best[0]-1e-4:
                best = (worst, d0, nd)
        _, d0, nd = best
        needs = [nd.get(f, 0.0) for f in frames]
        n = len(frames)-1 if loop and len(frames) > 2 else len(frames)
        sm = smooth_series(needs, n, max, pins)
        for f, v in zip(frames, sm):
            if v > 0:
                T = Matrix.Translation(r_ch[f] @ d0*v)
                for b in HALO_BONES:
                    if b in caps[f]:
                        caps[f][b] = T @ caps[f][b]
        return {'halo_clear_push_max_mm': round(max(sm, default=0.0)*1000, 1),
                'halo_clear_frames': sum(1 for v in sm[:n] if v > 0),
                'halo_clear_dir': [round(c, 2) for c in d0]}
