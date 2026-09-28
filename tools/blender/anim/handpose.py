"""Hand-pose library (item 12): natural hands in every clip, applied by HandPass.apply before its limits.

The v18 finger bones are modelled in a claw: the palm is cocked 11-14 deg back off the forearm line, the index
and middle tip bones are pre-hooked 21-25 deg, the little tip is bent back 12 deg and the thumb tip is hooked
56 deg. Clips author curl as the same local X rotation on every joint (Poser.curl and its variants), so every
hand kept that claw. This module rebuilds each finger from anatomical joint bends instead:

- `bend(n, q)`: the true bend of joint n toward the palm (deg): the base (.1) against the wrist -> knuckle line
  (the metacarpal), the middle/tip joints against the parent segment; 0 = straight, < 0 = bent backward.
- The authored intent survives as a curl level per digit: the summed local X over its three joints, divided by
  Poser.curl's index..little ratio (GRADE). Straightening, splay (the swing part of each rotation), per-digit
  offsets, tremors and flicks all come through.
- LIBRARY maps a level to [base, middle, tip] bends per digit. The named poses sit at these levels:
  straight -12 (Arc Bolt point), open 0 (release), relaxed 21 (the Idle hand), casting 36, fist-light 46
  (run), grip 64 (spear / orb), fist 92. Curl grows from index to little at every level, every joint bends the
  same way, and the tip joint never out-curls the middle joint (no hook). Levels between knots are monotone
  cubic, so poses blend without kinks. Past CONVERGE_RAMP[0] the fingers turn toward parallel bend planes, so a
  curled finger's tip doesn't fold across its neighbour.
- THUMB does the same from the thumb's level (mean X of thumb.2/.3): relaxed alongside the index, slightly
  opposed; wraps the fist as it closes.
- `wrist`: on arms below the 9f gate the hand flexes WRIST_FLEX toward the palm, cancelling the modelled cock
  (faded out with handpass.orient_weight, so extended arms keep their authored wrist and 9f roll).
- `settle` (bake, after HandPass.finish): each finger joint trails its pose by a critically damped spring and
  opens a little after big hand moves (follow-through), 0 on seam frames and declared finger accents.
- `leg_clear` (bake, last, on the final matrices): `LegClear` measures the digit mesh against rigid leg proxies
  (hip/thigh/shin skin in bone space) and extends the wrist about the palm axis just enough to keep CLEAR
  (at most CLEAR_EXT_MAX), held and blurred over CLEAR_SPREAD frames; seam frames keep their own per-frame need.
It is a pure function of each frame's authored rotations except `settle` and `leg_clear`, which keep seams by
pinning.
"""
import math
from mathutils import Matrix, Quaternion, Vector

import hs_anim as H
from handfix import ZSIGN

DIGITS = ('index', 'middle', 'ring', 'little')
GRADE = {'index': 0.88, 'middle': 1.0, 'ring': 1.1, 'little': 1.2}   # Poser.curl's (1 + extra*0.4)
JOINT_SUM = 3.15                                                     # Poser.curl's joint weights 1 + 1.15 + 1
POSES = {'straight': -12.0, 'open': 0.0, 'relaxed': 21.0, 'casting': 36.0, 'fist-light': 46.0, 'grip': 64.0,
         'fist': 92.0}
LIBRARY = {   # level: (base, middle, tip) true bend about the finger hinge, deg
    'index':  {-12: (1, 2, 1), 0: (5, 6, 4), 21: (15, 18, 11), 36: (29, 36, 22), 46: (39, 48, 29),
               64: (57, 70, 42), 92: (75, 92, 58)},
    'middle': {-12: (2, 2, 1), 0: (6, 7, 4), 21: (20, 23, 14), 36: (33, 40, 24), 46: (43, 52, 32),
               64: (60, 73, 45), 92: (78, 95, 60)},
    'ring':   {-12: (2, 3, 2), 0: (7, 8, 5), 21: (26, 28, 17), 36: (38, 44, 27), 46: (47, 55, 34),
               64: (63, 75, 47), 92: (80, 97, 61)},
    'little': {-12: (3, 4, 3), 0: (8, 9, 6), 21: (32, 32, 19), 36: (42, 47, 29), 46: (50, 58, 36),
               64: (66, 77, 48), 92: (82, 98, 62)},
}
THUMB = {   # level: (thumb.1 flex, thumb.1 opposition (rest-relative), thumb.2 bend, thumb.3 bend)
    -4: (0, 2, 4, 5), 0: (3, 4, 6, 8), 12: (10, 12, 13, 15), 22: (12, 12, 18, 20), 30: (13, 12, 24, 26),
    46: (20, 10, 34, 34),
}
CONVERGE, CONVERGE_RAMP = 0.9, (28.0, 64.0)   # curled fingers turn toward parallel bend planes (share of each
                                               # finger's rest angle off the middle finger, over this level ramp):
                                               # a finger spread outward folds its tip back across its neighbour
WRIST_FLEX = 6.0          # deg toward the palm on arms below the 9f gate (half the modelled 11-14 cock: the
                          # rest keeps curled fingertips clear of the thighs on the run/walk swing)
ENABLED = True
X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))


def about(axis, deg):
    return Quaternion(axis, math.radians(deg))


def twist_x(q):
    return (math.degrees(2*math.atan2(q.x, q.w))+180.0) % 360.0-180.0


def split_x(q):
    """q = swing @ twist about local X: (swing, twist deg)."""
    t = twist_x(q)
    return q @ about(X, -t), t


def pchip(xs, ys, x):
    """Monotone cubic (Fritsch-Carlson) through (xs, ys), clamped at both ends."""
    if x <= xs[0]:
        return ys[0]
    if x >= xs[-1]:
        return ys[-1]
    n = len(xs)
    h = [xs[i+1]-xs[i] for i in range(n-1)]
    dl = [(ys[i+1]-ys[i])/h[i] for i in range(n-1)]
    m = [dl[0]]+[0.0 if dl[i-1]*dl[i] <= 0 else
                 3*(h[i-1]+h[i])/((2*h[i]+h[i-1])/dl[i-1]+(h[i]+2*h[i-1])/dl[i]) for i in range(1, n-1)]+[dl[-1]]
    i = max(k for k in range(n-1) if xs[k] <= x)
    t = (x-xs[i])/h[i]
    t2, t3 = t*t, t*t*t
    return ((2*t3-3*t2+1)*ys[i]+(t3-2*t2+t)*h[i]*m[i]+(-2*t3+3*t2)*ys[i+1]+(t3-t2)*h[i]*m[i+1])


def table_at(table, level):
    xs = sorted(table)
    return tuple(pchip(xs, [table[k][j] for k in xs], level) for j in range(len(table[xs[0]])))


def pose(digit, level):
    """(base, middle, tip) bends of a digit at a curl level (LIBRARY)."""
    return table_at(LIBRARY[digit], level)


def thumb_pose(level):
    return table_at(THUMB, level)


def signed(a, b, axis):
    """Signed angle a -> b about axis (deg), both projected onto the plane normal to axis."""
    a = a-axis*a.dot(axis)
    b = b-axis*b.dot(axis)
    return math.degrees(math.atan2(a.cross(b).dot(axis), a.dot(b)))


class HandGeo:
    """Rest geometry of both hands (bone frames after handfix), in each hand bone's frame.

    The finger bones' local X axes are rolled off the real curl hinge (the modelled tip hooks read as sideways
    bends about X), so fingers use a hinge of their own: h = metacarpal line (wrist -> knuckle) x palm normal
    (handorient's n), turned with the finger's spread about n. `bend` is the angle about h from the parent
    segment (the metacarpal line for .1) to the segment; `side` is the segment's out-of-plane angle relative to
    its parent (a sideways hook at .2/.3). The thumb keeps its bone X hinge."""

    def __init__(self, poser):
        import handorient
        r3, rest = poser.r3, poser.rest
        self.rel = {}
        self.meta, self.hinge, self.normal, self.spread0, self.dir0 = {}, {}, {}, {}, {}
        self.palm_axis = {}
        axes = handorient.rest_axes(poser.rig)
        for s in H.SIDES:
            hand = f'{s} hand'
            nl = axes[s][0].normalized()
            self.normal[s] = nl
            self.palm_axis[s] = Y.cross(nl).normalized()   # hand-local: +rotation flexes toward the palm
            for d in DIGITS+('thumb',):
                for i in (1, 2, 3):
                    n = f'{s} {d}.{i}'
                    parent = hand if i == 1 else f'{s} {d}.{i-1}'
                    self.rel[n] = r3[parent].inverted() @ r3[n]
                if d == 'thumb':
                    continue
                m = (r3[hand].inverted() @ (rest[f'{s} {d}.1'].translation-rest[hand].translation)).normalized()
                self.meta[(s, d)] = m
                self.hinge[(s, d)] = m.cross(nl).normalized()
                self.spread0[(s, d)] = signed(m, self.rel[f'{s} {d}.1'] @ Y, nl)
            for d in DIGITS:
                self.dir0[(s, d)] = signed(self.rel[f'{s} middle.1'] @ Y, self.rel[f'{s} {d}.1'] @ Y, nl)

    def chain(self, pb, s, d, override=None):
        """Hand-frame 3x3 of the digit's three segments from the pose quaternions (override: {i: q})."""
        out, m = [], Matrix.Identity(3)
        for i in (1, 2, 3):
            n = f'{s} {d}.{i}'
            q = override[i] if override and i in override else pb[n].rotation_quaternion
            m = m @ self.rel[n] @ q.to_matrix()
            out.append(m)
        return out

    def finger_hinge(self, s, d, spread):
        return about(self.normal[s], spread).to_matrix() @ self.hinge[(s, d)]

    def measure(self, pb, s, d, override=None):
        """[(bend, side), ...] for .1..3 of a finger (see class doc; .1 side = spread change vs rest)."""
        ms = self.chain(pb, s, d, override)
        nl = self.normal[s]
        dirs = [self.meta[(s, d)]]+[m @ Y for m in ms]
        # spread: from the base segment's projection on the palm plane, which vanishes as .1 nears 90 deg,
        # blended with the bend plane's own normal (dir1 x dir3), which vanishes on a straight finger
        wp = (dirs[1]-nl*dirs[1].dot(nl)).length
        c = dirs[1].cross(dirs[3])
        wc = c.length
        spread = signed(self.meta[(s, d)], dirs[1], nl)
        if wc > 1e-3:
            if c.dot(self.hinge[(s, d)]) < 0:
                c = -c
            spread = (spread*wp+signed(self.hinge[(s, d)], c/wc, nl)*wc)/(wp+wc)
        h = self.finger_hinge(s, d, spread)
        out = []
        for i in (1, 2, 3):
            p, c = dirs[i-1], dirs[i]
            b = signed(p, c, h)
            side = spread-self.spread0[(s, d)] if i == 1 else \
                math.degrees(math.asin(max(-1.0, min(1.0, c.dot(h)))))-math.degrees(math.asin(max(-1.0, min(1.0, p.dot(h)))))
            out.append((b, side))
        return out

    def build(self, pb, s, d, bends, splay=0.0):
        """Set the finger's three local rotations to a planar arc: the metacarpal line spread like the rest finger
        plus `splay` about the palm normal, bent about the finger hinge by bends[0..2] (deg). Shortest arcs, so
        each segment keeps about its rest roll."""
        nl = self.normal[s]
        spread = self.spread0[(s, d)]+splay
        rs = about(nl, spread).to_matrix()
        m0 = rs @ self.meta[(s, d)]
        h = rs @ self.hinge[(s, d)]
        parent = Matrix.Identity(3)
        acc = 0.0
        out = {}
        for i, b in zip((1, 2, 3), bends):
            acc += b
            want = about(h, acc).to_matrix() @ m0
            n = f'{s} {d}.{i}'
            rel = self.rel[n]
            local_want = parent.inverted() @ want
            arc = (rel @ Y).rotation_difference(local_want).to_matrix()
            q = (rel.inverted() @ arc @ rel).to_quaternion()
            out[i] = q
            if pb is not None:
                pb[n].rotation_quaternion = q
            parent = parent @ rel @ q.to_matrix()
        return out

    def splay_of(self, s, d, q1):
        """Authored spread change at the base: the swing part of q1 (its X twist is the curl), about the palm normal."""
        sw, _ = split_x(q1)
        return signed(self.rel[f'{s} {d}.1'] @ Y, self.rel[f'{s} {d}.1'] @ sw.to_matrix() @ Y, self.normal[s])

    def local_hinge(self, pb, s, d, i):
        """The finger hinge in bone {d}.{i}'s local frame (for small bends about it)."""
        ms = self.chain(pb, s, d)
        spread = signed(self.meta[(s, d)], ms[0] @ Y, self.normal[s])
        return (ms[i-1].inverted() @ self.finger_hinge(s, d, spread)).normalized()

    # thumb: bone X hinge, bend against the parent segment (thumb.1 against the hand's Y)
    def bend(self, n, q):
        m = self.rel[n] @ q.to_matrix()
        y, x = m @ Y, m @ X
        return math.degrees(math.atan2(Y.cross(y).dot(x), Y.dot(y)))

    def solve(self, n, swing, target):
        """Local X twist t with bend(n, swing @ Rx(t)) == target."""
        t = target-self.bend(n, swing)
        for _ in range(4):
            b = self.bend(n, swing @ about(X, t))
            err = target-b
            if abs(err) < 1e-3:
                break
            slope = (self.bend(n, swing @ about(X, t+0.5))-b)/0.5
            t += err/(slope if abs(slope) > 0.3 else 1.0)
        return t


def level(pb, side, digit):
    return sum(twist_x(pb[f'{side} {digit}.{i}'].rotation_quaternion) for i in (1, 2, 3))/(JOINT_SUM*GRADE[digit])


def thumb_level(pb, side):
    return 0.5*(twist_x(pb[f'{side} thumb.2'].rotation_quaternion)+twist_x(pb[f'{side} thumb.3'].rotation_quaternion))


def apply_fingers(poser, geo):
    """Rebuild every finger/thumb joint from LIBRARY at the authored curl level (pure function of the frame)."""
    pb = poser.pb
    out = {}
    for s in H.SIDES:
        k = (1.0 if s == 'L' else -1.0)*ZSIGN
        lv = {'built': {}}
        for d in DIGITS:
            lv[d] = level(pb, s, d)
            splay = geo.splay_of(s, d, pb[f'{s} {d}.1'].rotation_quaternion)
            splay -= CONVERGE*H.smooth((lv[d]-CONVERGE_RAMP[0])/(CONVERGE_RAMP[1]-CONVERGE_RAMP[0]))*geo.dir0[(s, d)]
            bends = pose(d, lv[d])
            geo.build(pb, s, d, bends, splay)
            lv['built'][d] = (bends, splay)
        tl = thumb_level(pb, s)
        fx, op, t2, t3 = thumb_pose(tl)
        pb[f'{s} thumb.1'].rotation_quaternion = about(X, fx) @ about(Z, k*op)
        for i, tgt in ((2, t2), (3, t3)):
            b = pb[f'{s} thumb.{i}']
            sw, _ = split_x(b.rotation_quaternion)
            b.rotation_quaternion = sw @ about(X, geo.solve(b.name, sw, tgt))
        lv['thumb'] = tl
        out[s] = lv
    return out


def wrist(poser, geo, weights):
    """Flex each hand WRIST_FLEX toward the palm, faded out by the 9f gate weight (weights[side] 0..1)."""
    for s in H.SIDES:
        w = 1.0-weights.get(s, 0.0)
        if w > 1e-4:
            b = poser.pb[f'{s} hand']
            b.rotation_quaternion = b.rotation_quaternion @ Quaternion(geo.palm_axis[s], math.radians(WRIST_FLEX*w))


# ----------------------------------------------------------------------------- leg clearance (per frame)
CLEAR = 0.015             # m kept between finger parts and the own-side hip/thigh/shin skin (clearance.py's
                          # locomotion gate is 10 mm; the proxy reads a little closer than the skin)
CLEAR_REACH = 0.05        # m: probes farther than this from the leg are skipped
CLEAR_EXT_MAX = 26.0      # deg of wrist extension (away from the palm) the solve may add
CLEAR_STEP = 1.0


class LegClear:
    """Curled fingers face the thighs on hanging arms. Each frame, the smallest wrist extension (about the palm
    axis, back toward the modelled cock) that keeps every finger part CLEAR from the own-side leg. The legs are
    rigid proxies: body vertices dominated by the hip/thigh/shin bones with their normals, in bone space; the
    probes are the digit meshes' vertices in their bones' space. Signed distance = offset along the nearest
    proxy vertex's normal."""

    def __init__(self, poser):
        import bpy
        from mathutils.kdtree import KDTree
        rig = poser.rig
        body = bpy.data.objects[H.BODY]
        to_arm = rig.matrix_world.inverted() @ body.matrix_world
        names = {g.index: g.name for g in body.vertex_groups}
        bones = rig.data.bones
        pts = {}
        for v in body.data.vertices:
            best, bw = None, 0.0
            for g in v.groups:
                n = names.get(g.group)
                if n in bones and g.weight > bw:
                    best, bw = n, g.weight
            if best is None or best[:1] not in H.SIDES:
                continue
            part = best[2:]
            if not (part.startswith('thigh') or part in ('shin', 'hip')):
                continue
            pts.setdefault(best, []).append((to_arm @ v.co, (to_arm.to_3x3() @ v.normal).normalized()))
        self.legs = {s: {} for s in H.SIDES}
        for b, lst in pts.items():
            inv = bones[b].matrix_local.inverted()
            kd = KDTree(len(lst))
            nrm = []
            for i, (c, n) in enumerate(lst):
                kd.insert(inv @ c, i)
                nrm.append((inv.to_3x3() @ n).normalized())
            kd.balance()
            self.legs[b[:1]][b] = (kd, nrm)
        bpy.context.view_layer.update()
        rmw = rig.matrix_world.inverted()
        self.probes = {s: [] for s in H.SIDES}
        for o in bpy.data.objects:
            if o.type != 'MESH' or o.parent_type != 'BONE' or not o.name.startswith(('L HAND |', 'R HAND |')):
                continue
            tail = o.name.split('|', 1)[1]
            if 'knuckle' in tail or not any(k in tail for k in DIGITS+('thumb',)):
                continue
            inv = poser.pb[o.parent_bone].matrix.inverted() @ rmw @ o.matrix_world
            vs = list(o.data.vertices)
            self.probes[o.name[0]].append((o.parent_bone, [inv @ v.co for v in vs[::max(1, len(vs)//12)]]))

    def clearance(self, legs, pts, pivot, axis, delta):
        R = Matrix.Rotation(math.radians(-delta), 3, axis)
        worst = 1.0
        for inv, kd, nrm in legs:
            for p in pts:
                ql = inv @ (R @ (p-pivot)+pivot)
                co, i, dist = kd.find(ql)
                if dist < CLEAR_REACH:
                    worst = min(worst, (ql-co).dot(nrm[i]))
        return worst

    def need(self, mats, geo, s):
        """Wrist extension (deg, >= 0) side s needs at a pose given as armature-space bone matrices."""
        m = mats[f'{s} hand']
        pivot = m.translation.copy()
        axis = (m.to_3x3() @ geo.palm_axis[s]).normalized()
        legs = [(mats[b].inverted(), kd, nrm) for b, (kd, nrm) in self.legs[s].items()]
        near = []
        for b, cs in self.probes[s]:
            for c in cs:
                p = mats[b] @ c
                if any(kd.find(inv @ p)[2] < CLEAR_REACH+0.06 for inv, kd, _ in legs):
                    near.append(p)
        if not near or self.clearance(legs, near, pivot, axis, 0.0) >= CLEAR:
            return 0.0
        delta = 0.0
        while delta < CLEAR_EXT_MAX:
            delta = min(CLEAR_EXT_MAX, delta+CLEAR_STEP)
            if self.clearance(legs, near, pivot, axis, delta) >= CLEAR:
                break
        lo = delta-CLEAR_STEP
        for _ in range(4):
            mid = 0.5*(lo+delta)
            if self.clearance(legs, near, pivot, axis, mid) >= CLEAR:
                delta = mid
            else:
                lo = mid
        return delta


CLEAR_SPREAD = 3          # frames: the extension is held (running max) and blurred over this radius


def leg_clear(poser, geo, caps, frames, loop, pins=()):
    """Last bake pass (after the clip's post): LegClear's extension per frame from the final matrices, held over
    +-CLEAR_SPREAD frames and blurred (never below the frame's own need, so clearance holds), written into caps
    as wrist extension for the hand and everything under it. Seam frames keep their own per-frame need (a pure
    function of that frame's pose, so both clips of a seam agree)."""
    m = len(frames)
    n = m-1 if loop and m > 2 else m
    if n < 1:
        return {}
    hp = poser.handpass
    if getattr(hp, 'legclear', None) is None:
        hp.legclear = LegClear(poser)
    need = {s: [hp.legclear.need(caps[frames[i]], geo, s) for i in range(n)] for s in H.SIDES}
    bones = poser.bones
    anchors = sorted({q % n for q in pins}) if loop else [0, m-1]
    r = CLEAR_SPREAD

    def at(xs, i):
        return xs[i % n] if loop else xs[max(0, min(n-1, i))]
    gate = []
    for i in range(n):
        g = 1.0
        for q in anchors:
            d = min(abs(i-q), n-abs(i-q)) if loop else abs(i-q)
            g *= H.smooth(d/ANCHOR_FADE)
        gate.append(g)
    peak = {}
    for s in H.SIDES:
        e = need[s]
        peak[s] = round(max(e), 1)
        if max(e) < 1e-3:
            continue
        held = [max(at(e, i+k) for k in range(-r, r+1)) for i in range(n)]
        w = [r+1-abs(k) for k in range(-r, r+1)]
        sm = [sum(wk*at(held, i+k) for wk, k in zip(w, range(-r, r+1)))/sum(w) for i in range(n)]
        hand = f'{s} hand'
        under = [b.name for b in bones[hand].children_recursive]
        for i in range(m):
            j = i % n if loop else i
            add = e[j]+(sm[j]-e[j])*gate[j]
            if add < 1e-3:
                continue
            peak[s] = max(peak[s], round(add, 1))
            f = frames[i]
            old = caps[f][hand].copy()
            caps[f][hand] = old @ Quaternion(geo.palm_axis[s], math.radians(-add)).to_matrix().to_4x4()
            k = caps[f][hand] @ old.inverted()
            for b in under:
                if b in caps[f]:
                    caps[f][b] = k @ caps[f][b]
    return {'hand_leg_clear_ext_deg': peak}


# ----------------------------------------------------------------------------- settle (temporal, after the bake)
LAG = 1.6                 # frames: finger spring time constant (critically damped)
RELAX_GAIN = 0.35         # deg of uncurl per deg/frame of hand angular speed (blurred, 2 frames late)
RELAX_MAX = 6.0           # deg per base joint (middle 0.8x, tip 0.6x)
RELAX_DELAY = 2
DELTA_MAX = 8.0
ANCHOR_FADE = 4.0         # frames over which the settle fades in from seam anchors
ACCENT_FADE = 3.0
JOINT_SCALE = {1: 1.0, 2: 0.8, 3: 0.6}


def _spring(xs, loop, tau=LAG, substeps=4):
    """Critically damped follow of a scalar series (periodic when loop: several passes over the period)."""
    w = 1.0/tau
    dt = 1.0/substeps
    x, v = xs[0], 0.0
    out = xs
    for _ in range(4 if loop else 1):
        out = []
        for i, target in enumerate(xs):
            prev = xs[i-1] if i > 0 else (xs[-1] if loop else target)
            for k in range(substeps):
                tgt = prev+(target-prev)*(k+1)/substeps
                a = w*w*(tgt-x)-2*w*v
                v += a*dt
                x += v*dt
            out.append(x)
    return out


def _blur(xs, loop, r=1):
    n = len(xs)
    at = (lambda i: xs[i % n]) if loop else (lambda i: xs[min(max(i, 0), n-1)])
    return [sum(at(i+j) for j in range(-r, r+1))/(2*r+1) for i in range(n)]


def settle(poser, geo, caps, frames, loop, pins=(), accents=()):
    """Finger lag + follow-through relax, written into caps (world matrices) for the finger bones and everything
    under them. Seam frames (one-shot ends, loop pins) and finger accents keep the per-frame pose exactly."""
    m = len(frames)
    n = m-1 if loop and m > 2 else m
    if n < 4:
        return {}
    bones = poser.bones
    rest = poser.rest
    anchors = sorted({q % n for q in pins}) if loop else [0, m-1]
    acc = sorted(frames.index(f) for f in accents if f in frames)

    def dist(i, q):
        return min(abs(i-q), n-abs(i-q)) if loop else abs(i-q)
    gate = []
    for i in range(n):
        g = 1.0
        for q in anchors:
            g *= H.smooth(dist(i, q)/ANCHOR_FADE)
        for q in acc:
            d = max(0, dist(i, q)-1)
            g *= H.smooth(d/ACCENT_FADE)
        gate.append(g)

    def local(f, b):
        par = bones[b].parent.name
        rel = rest[par].inverted() @ rest[b]
        return rel.inverted() @ caps[f][par].inverted() @ caps[f][b]
    newq = {}
    peak_relax = peak = 0.0
    for s in H.SIDES:
        hq = [caps[frames[i]][f'{s} hand'].to_quaternion() for i in range(n)]
        sp = []
        for i in range(n):
            a, b = hq[i-1] if (loop or i > 0) else hq[i], hq[(i+1) % n] if (loop or i < n-1) else hq[i]
            d = a.rotation_difference(b)
            sp.append(math.degrees(min(d.angle, 2*math.pi-d.angle))*0.5)
        sp = _blur(_blur(sp, loop, 1), loop, 1)
        late = [sp[(i-RELAX_DELAY) % n] if loop else sp[max(i-RELAX_DELAY, 0)] for i in range(n)]
        relax = [min(RELAX_MAX, RELAX_GAIN*v) for v in late]
        for d in DIGITS:
            qs = [{j: local(frames[i], f'{s} {d}.{j}').to_quaternion() for j in (1, 2, 3)} for i in range(n)]
            ms = [geo.measure(None, s, d, q) for q in qs]
            ys = [_spring([mm[j][0] for mm in ms], loop) for j in range(3)]
            for i in range(n):
                if gate[i] < 1e-4:
                    continue
                bends, moved = [], 0.0
                for j in range(3):
                    x = ms[i][j][0]
                    dv = max(-DELTA_MAX, min(DELTA_MAX, ys[j][i]-x-relax[i]*JOINT_SCALE[j+1]))*gate[i]
                    bends.append(max(x+dv, min(x, 0.5)))
                    moved = max(moved, abs(bends[-1]-x))
                peak_relax = max(peak_relax, relax[i]*gate[i])
                if moved < 1e-3:
                    continue
                peak = max(peak, moved)
                q = geo.build(None, s, d, bends, ms[i][0][1])
                for j in (1, 2, 3):
                    newq.setdefault(f'{s} {d}.{j}', {})[i] = q[j]
        for j in (1, 2, 3):
            bn = f'{s} thumb.{j}'
            qs = [local(frames[i], bn).to_quaternion() for i in range(n)]
            xs = [twist_x(q) for q in qs]
            ys = _spring(xs, loop)
            for i in range(n):
                dv = max(-DELTA_MAX, min(DELTA_MAX, ys[i]-xs[i]-relax[i]*JOINT_SCALE[j]*0.5))*gate[i]
                if dv < 0:
                    now = geo.bend(bn, qs[i]) if j > 1 else xs[i]
                    dv = max(dv, min(0.0, 0.5-now))
                if abs(dv) > 1e-3:
                    newq.setdefault(bn, {})[i] = qs[i] @ about(X, dv)
                    peak = max(peak, abs(dv))
    if not newq:
        return {}
    if n < m:
        for d in newq.values():
            for i in range(n, m):
                if i-n in d:
                    d[i] = d[i-n]

    def depth(b):
        k, x = 0, bones[b].parent
        while x is not None:
            k, x = k+1, x.parent
        return k
    order = sorted((b for b in caps[frames[0]] if bones[b].parent is not None), key=depth)
    under = set(newq)
    for b in order:
        if bones[b].parent.name in under:
            under.add(b)
    for i, f in enumerate(frames):
        old = {b: caps[f][b].copy() for b in under}
        for b in order:
            if b not in under:
                continue
            par = bones[b].parent.name
            rel = rest[par].inverted() @ rest[b]
            loc = rel.inverted() @ old.get(par, caps[f][par]).inverted() @ old[b]
            if i in newq.get(b, {}):
                t = loc.translation.copy()
                loc = newq[b][i].to_matrix().to_4x4()
                loc.translation = t
            caps[f][b] = caps[f][par] @ rel @ loc
    return {'hand_settle_max_deg': round(peak, 1), 'hand_relax_peak_deg': round(peak_relax, 1)}
