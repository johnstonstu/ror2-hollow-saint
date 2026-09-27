"""Tabard push-off (FULL-AUDIT M1), run by hs_anim.bake on every frame after the pad pass.

The body mesh carries its own loincloth flaps (front on `tabard front.1-3`, back on `tabard back.1-2`) under the
TABARD cloth. Knees, shins and hips moving through them were hidden by the Shrinkwrap bodyfix removes. Per
frame, each chain swings about its bones' X (front tips forward, back tips back), split between the bones by
SHARE, just far enough that no leg or torso skin comes within MARGIN of the flap's inner (body-side) surface
beyond where it already sat at rest. That surface is interpolated over cells of the flap (across x along) from the
rest flap points of the bone: body flap verts plus the TABARD pieces it drives.
Each solve depends only on the frame's pose; `finish` max-envelopes and blurs the swings over time
(padpass.smooth_series, seams pinned to the raw solve) and rewrites the chain in the captured matrices.
"""
import math

import bpy
import numpy as np
from mathutils import Matrix, Quaternion, Vector

import hs_anim as H
from padpass import smooth_series

CHAINS = {'front': (H.TABARD_FRONT, -1.0), 'back': (H.TABARD_BACK, 1.0)}   # push sign about local X
MARGIN = 0.004        # m kept between the flap's inner surface and the skin
SHARE = {'tabard front.1': 0.45, 'tabard front.2': 0.35, 'tabard front.3': 0.2,   # of the chain's push
         'tabard back.1': 0.55, 'tabard back.2': 0.45}
# Bend limit per bone relative to its parent (clip + solve, deg, soft). The flap is a few large faces blending
# neighbouring bones; past ~12-15 deg per joint, skinning bends a point on such a face away from the face
# itself, and the flap shows through the TABARD cloth (probe: 15 mm at 27 deg on front.1, 10 mm at 12 deg on
# back.1, 11 mm at 18 deg on front.3). Any excess of the clip's own bend passes down the chain, so the hem still travels about as far.
BEND_LIMIT = {'tabard front.1': 12.0, 'tabard front.2': 16.0, 'tabard front.3': 13.0,
              'tabard back.1': 10.0, 'tabard back.2': 22.0}
MAX_SWING = 60.0      # deg, solver search range
DEPTH_FADE, DEPTH_MAX = 0.10, 0.15   # m: skin this far ahead of the inner surface is a leg ahead of the flap,
                                     # not through it; its demand fades out over this range
EDGE_SLOPE = 3.0      # m of demand per m inside the flap's side/hem edge or below the hinge
CELL_U, CELL_Y = 0.03, 0.04
HINGE_SKIP = 0.05     # m down the bone: skin this close to (or above) the hinge can't be cleared by swinging
COLLIDE = ('thigh', 'shin', 'foot', 'toe', 'hip', 'pelvis', 'spine')


def soft_limit(x, lim, knee=0.6):
    k = knee*lim
    if abs(x) <= k:
        return x
    return math.copysign(k+(lim-k)*math.tanh((abs(x)-k)/(lim-k)), x)


def split_x(q):
    """q = rest @ about(X, deg): (rest quaternion, deg)."""
    deg = math.degrees(2*math.atan2(q.x, q.w))
    if deg > 180:
        deg -= 360
    elif deg < -180:
        deg += 360
    return q @ Quaternion((1, 0, 0), math.radians(deg)).inverted(), deg


def rx(deg):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    return np.array([[1, 0, 0], [0, c, -s], [0, s, c]])


def m4(m):
    return np.array(m)


class Cells:
    """Inner-surface height (max local z toward the body, sign-folded) per (x, y) cell of one bone's flap."""

    def __init__(self, pts, inward):
        self.inward = inward
        u = np.floor(pts[:, 0]/CELL_U).astype(int)
        y = np.floor(pts[:, 1]/CELL_Y).astype(int)
        z = pts[:, 2]*inward
        grid = {}
        for a, b, c in zip(u, y, z):
            grid[(a, b)] = max(grid.get((a, b), -1.0), c)
        full = dict(grid)
        rows = {}
        for a, b in grid:
            lo, hi = rows.get(b, (a, a))
            rows[b] = (min(lo, a), max(hi, a))
        for b, (lo, hi) in rows.items():      # fill holes inside each row's width only
            for a in range(lo, hi+1):
                if (a, b) not in grid:
                    near = [grid[k] for k in ((a-1, b), (a+1, b), (a, b-1), (a, b+1)) if k in grid]
                    if near:
                        full[(a, b)] = max(near)
        self.grid = full
        self.xs = {b: (lo*CELL_U, (hi+1)*CELL_U) for b, (lo, hi) in rows.items()}
        self.y_hi = (max(rows)+1)*CELL_Y

    def height(self, a, b, x, y):
        """Bilinear between cell centres (missing neighbours drop out), so the surface has no steps."""
        fx, fy = x/CELL_U-0.5, y/CELL_Y-0.5
        a0, b0 = math.floor(fx), math.floor(fy)
        tx, ty = fx-a0, fy-b0
        s = w = 0.0
        for da, wx in ((0, 1-tx), (1, tx)):
            for db, wy in ((0, 1-ty), (1, ty)):
                h = self.grid.get((a0+da, b0+db))
                if h is not None and wx*wy > 0:
                    s += h*wx*wy
                    w += wx*wy
        return s/w if w > 0 else self.grid[(a, b)]

    def violation(self, loc):
        """MARGIN minus the skin's depth behind the inner surface, per point (nan where no flap covers it).
        Demand ramps in over EDGE_SLOPE from the flap's side and hem edges and the hinge, and fades out between
        DEPTH_FADE and DEPTH_MAX ahead of the surface, so a limb sliding into the footprint starts the push
        gradually instead of at full depth in one frame."""
        u = np.floor(loc[:, 0]/CELL_U).astype(int)
        y = np.floor(loc[:, 1]/CELL_Y).astype(int)
        c = np.array([self.height(a, b, x, yy) if (a, b) in self.grid else np.nan
                      for a, b, x, yy in zip(u, y, loc[:, 0], loc[:, 1])])
        v = c+MARGIN-loc[:, 2]*self.inward
        lo = np.array([self.xs.get(b, (np.nan, np.nan))[0] for b in y])
        hi = np.array([self.xs.get(b, (np.nan, np.nan))[1] for b in y])
        d_in = np.minimum.reduce([loc[:, 0]-lo, hi-loc[:, 0], self.y_hi-loc[:, 1], loc[:, 1]-HINGE_SKIP])
        fade = np.clip((DEPTH_MAX-v)/(DEPTH_MAX-DEPTH_FADE), 0.0, 1.0)
        v = np.where(v > 0, np.minimum(v*fade, EDGE_SLOPE*d_in), v)
        v[(v <= 0) & ((d_in < 0) | (fade <= 0))] = np.nan
        return v


class TabardPass:
    def __init__(self, poser):
        self.p = poser
        self.hist = []
        rig = poser.rig
        self.enabled = all(n in poser.pb for ch, _ in CHAINS.values() for n in ch)
        if not self.enabled:
            return
        inv = rig.matrix_world.inverted()
        deform = {b.name for b in rig.data.bones if b.use_deform}
        chain_bones = {n for ch, _ in CHAINS.values() for n in ch}
        self.body = bpy.data.objects[H.BODY]

        def dom(o):
            names = {g.index: g.name for g in o.vertex_groups}
            out = []
            for v in o.data.vertices:
                best, bw = '', 0.0
                for g in v.groups:
                    n = names.get(g.group)
                    if n in deform and g.weight > bw:
                        best, bw = n, g.weight
                out.append(best)
            return out
        saved = rig.data.pose_position
        rig.data.pose_position = 'REST'
        bpy.context.view_layer.update()
        dg = bpy.context.evaluated_depsgraph_get()

        def rest_co(o):
            ev = o.evaluated_get(dg)
            me = ev.to_mesh()
            co = np.empty(len(me.vertices)*3)
            me.vertices.foreach_get('co', co)
            ev.to_mesh_clear()
            mw = m4(inv @ o.matrix_world)
            return co.reshape(-1, 3)@mw[:3, :3].T+mw[:3, 3]
        bdom = dom(self.body)
        bco = rest_co(self.body)
        flap = {n: [bco[i] for i, d in enumerate(bdom) if d == n] for n in chain_bones}
        for o in bpy.data.objects:
            if o.type == 'MESH' and o.name.startswith('TABARD |') and not o.hide_render:
                co = rest_co(o)
                for q, d in zip(co, dom(o)):
                    if d in flap:
                        flap[d].append(q)
        rig.data.pose_position = saved
        bpy.context.view_layer.update()
        self.cells = {}
        for n in chain_bones:
            rest_inv = m4(poser.rest[n].inverted())
            pts = np.array(flap[n])@rest_inv[:3, :3].T+rest_inv[:3, 3]
            inward = 1.0 if n in H.TABARD_FRONT else -1.0
            self.cells[n] = Cells(pts, inward)
        cx = [q for n in chain_bones for q in flap[n]]
        lo_x = min(q[0] for q in cx)-0.15
        hi_x = max(q[0] for q in cx)+0.15
        keep = [i for i, d in enumerate(bdom) if d and d not in chain_bones and any(k in d for k in COLLIDE)
                and lo_x < bco[i][0] < hi_x and bco[i][2] < 1.25]
        self.ids = np.array(keep[::2])
        self.rest_rel = {n: m4(poser.rest[poser.bones[n].parent.name].inverted() @ poser.rest[n])
                         for n in chain_bones}
        self.rest_v = {}
        pts = bco[self.ids]
        for key, (chain, _) in CHAINS.items():
            for n in chain:
                rest_inv = m4(poser.rest[n].inverted())
                self.rest_v[n] = self.cells[n].violation(pts@rest_inv[:3, :3].T+rest_inv[:3, 3])

    def skin(self):
        dg = bpy.context.evaluated_depsgraph_get()
        ev = self.body.evaluated_get(dg)
        me = ev.to_mesh()
        co = np.empty(len(me.vertices)*3)
        me.vertices.foreach_get('co', co)
        ev.to_mesh_clear()
        mw = m4(self.p.rig.matrix_world.inverted() @ self.body.matrix_world)
        return co.reshape(-1, 3)[self.ids]@mw[:3, :3].T+mw[:3, 3]

    @staticmethod
    def basis(loc, q):
        return m4(Matrix.Translation(loc) @ q.to_matrix().to_4x4())

    def excess(self, n, loc):
        v = self.cells[n].violation(loc)
        b = np.nan_to_num(self.rest_v[n], nan=0.0)
        e = v-np.maximum(b, 0.0)
        return np.nanmax(e) if np.any(~np.isnan(e)) else -1.0

    def solve_chain(self, chain, sign, P, w_par, basis, bend):
        """Swing (deg, signed) per bone of `chain` given skin points P (armature space), parent world matrix,
        the (limited) clip local bases and their bends about X; returns {bone: deg}. One amount s is searched:
        every bone swings SHARE[bone]*s (capped at its bend limit), so the chain moves as one panel and the
        result varies smoothly as the skin moves (a greedy root-first solve handed the whole load from one
        bone to the next between frames)."""
        tops = {n: min(MAX_SWING, max(0.0, BEND_LIMIT.get(n, MAX_SWING)-sign*bend[n])) for n in chain}
        self.tops.update(tops)

        def swings(s):
            return {n: min(tops[n], SHARE[n]*s) for n in chain}

        def worst(s):
            w, e = w_par, -1.0
            for n, th in swings(s).items():
                r4 = np.eye(4)
                r4[:3, :3] = rx(th*sign)
                w = w@self.rest_rel[n]@basis[n]@r4
                winv = np.linalg.inv(w)
                e = max(e, self.excess(n, P@winv[:3, :3].T+winv[:3, 3]))
            return e
        s_top = max((tops[n]/SHARE[n] for n in chain if tops[n] > 0), default=0.0)
        s = 0.0
        if s_top > 0 and worst(0.0) > 0:
            if worst(s_top) > 0:
                # can't clear within the limits: least penetration, smallest swing on near-ties (1 mm)
                grid = [s_top*k/16 for k in range(17)]
                ws = [worst(g) for g in grid]
                s = next(g for g, e in zip(grid, ws) if e <= min(ws)+0.001)
            else:
                lo, hi = 0.0, s_top
                for _ in range(14):
                    mid = 0.5*(lo+hi)
                    lo, hi = (mid, hi) if worst(mid) > 0 else (lo, mid)
                s = hi
        return {n: th*sign for n, th in swings(s).items()}

    def apply(self):
        if not self.enabled:
            return {}
        self.p.update()
        P = self.skin()
        pb = self.p.pb
        rec = {}
        self.tops = {}
        for key, (chain, sign) in CHAINS.items():
            clip, bend, carry = {}, {}, 0.0
            for n in chain:
                rest, deg = split_x(pb[n].rotation_quaternion)
                lim = soft_limit(deg+carry, BEND_LIMIT[n])
                carry = deg+carry-lim
                bend[n] = lim
                clip[n] = (pb[n].location.copy(), rest @ Quaternion((1, 0, 0), math.radians(lim)))
            basis = {n: self.basis(*clip[n]) for n in chain}
            w_par = m4(pb[self.p.bones[chain[0]].parent.name].matrix)
            sw = self.solve_chain(chain, sign, P, w_par, basis, bend)
            for n in chain:
                pb[n].rotation_quaternion = clip[n][1] @ Quaternion((1, 0, 0), math.radians(sw[n]))
                rec[n] = (clip[n][0], clip[n][1], sw[n], self.tops[n])
        self.p.update()
        self.hist.append(rec)
        return {n: v[2] for n, v in rec.items()}

    def finish(self, caps, frames, loop, pins=()):
        """Envelope + blur each bone's swing over time and rewrite the chain (and anything parented under it) in
        caps. `pins`: loop frame indices kept at the raw solve (seams)."""
        if not self.enabled or len(self.hist) != len(frames):
            return {}
        n = len(frames)-1 if loop and len(frames) > 2 else len(frames)
        sm = {}
        for key, (chain, sign) in CHAINS.items():
            for b in chain:
                mag = smooth_series([abs(h[b][2]) for h in self.hist], n, max, pins)
                # the envelope must not carry a joint past its bend limit on frames where the clip already bends it
                sm[b] = [sign*min(v, h[b][3]) for v, h in zip(mag, self.hist)]
        order = [b.name for b in self.p.rig.data.bones if b.name in caps[frames[0]]]
        under = set(sm)
        for b in order:
            par = self.p.bones[b].parent
            if par is not None and par.name in under:
                under.add(b)
        for i, f in enumerate(frames):
            old = {b: caps[f][b].copy() for b in under}
            for b in order:
                if b not in under:
                    continue
                par = self.p.bones[b].parent.name
                rel = self.p.rest[par].inverted() @ self.p.rest[b]
                if b in sm:
                    loc, q = self.hist[i][b][:2]
                    local = Matrix.Translation(loc) @ (q @ Quaternion((1, 0, 0), math.radians(sm[b][i]))).to_matrix().to_4x4()
                else:
                    local = rel.inverted() @ old[par].inverted() @ old[b]
                caps[f][b] = caps[f][par] @ rel @ local
        return {'tabard_swing_max_deg': {b: round(max((abs(v) for v in sm[b][:n]), default=0.0), 1) for b in sm},
                'tabard_swing_frames': sum(1 for i in range(n) if any(abs(sm[b][i]) > 0.05 for b in sm))}
