"""Natural-hand pass (item 9c), applied by hs_anim.bake to every frame of every clip after the pose function.

A pure function of the frame's local rotations, so poses that were equal stay equal and clip seams stay
exact. Every limit is a smooth soft limit that is the identity inside the natural range, so poses that
already look right are untouched and no new pops appear.
- Thumb: never bent back or straight out; each joint keeps a slight curl (rest-relative flex floor), and
  when the thumb is curled (resting, gripping) its base tucks along the index finger.
- Fingers: base knuckles (.1) are limited rest-relative; middle/distal joints (.2/.3) are limited on their
  anatomical bend relative to the parent segment, because the rig's distal bones are sculpted pre-bent
  (a fully straightened pointing finger is anatomically 0, not hyperextended).
- Wrist: hand roll about the forearm axis and hand bend are soft-limited (no candy-wrapper), as is the
  forearm's roll relative to the upper arm.
- Orientation (item 9f): on raised arms the forearm (plus a share of upper-arm twist on a straight arm) rolls
  so the thumb edge stays within ORIENT_BAND of a target that turns from thumb-forward (arm low) to thumb-up /
  palm-forward (arm raised); swept-back arms are left alone. It is a soft band, so hands already inside it are
  untouched. When several roll ranges qualify it takes the one nearest the previous frame's roll (bake resets
  `prev` per clip), so the forearm doesn't flip between solutions. Needs a depsgraph update per frame.
"""
import math
from mathutils import Quaternion, Vector
from handfix import ZSIGN

DIGITS = ('index', 'middle', 'ring', 'little')
THUMB_FLOOR = {1: 3.0, 2: 7.0, 3: 7.0}        # rest-relative deg, + curls toward the palm
THUMB_CEIL = 72.0
BASE_FLOOR, BASE_CEIL = -7.0, 90.0            # finger .1, rest-relative
ANAT = {2: (-2.0, 104.0), 3: (-5.0, 88.0)}    # finger .2/.3 anatomical bend limits (deg)
KNEE = 4.0
TUCK = 10.0                                   # thumb-base swing toward the index when curled (deg)
TUCK_RAMP = (4.0, 18.0)                       # thumb.1 flex over which the tuck fades in
TUCK_FIST = (15.0, 45.0)                      # index.1 flex over which it fades out again (thumb wraps the fist)
SEP, SEP_DEAD, SEP_RAMP = 10.0, 6.0, 22.0     # adjacent fingers curled differently fan apart at .1 (deg)
CURL_SEP, CURL_SEP_RAMP = 6.0, (45.0, 80.0)   # ...as do tightly curled neighbours (the mesh fingers collide)
HAND_TWIST, HAND_BEND, FOREARM_ROLL = 26.0, 64.0, 90.0
ORIENT = True
ORIENT_THUMB_UP, ORIENT_PALM_FWD = 0.10, -0.12   # targets, inside the QA limits (0 / -0.25, handorient.py)
ORIENT_RELAXED = (0.03, -0.21)                    # fallback targets when the pose can't reach both
ORIENT_SAFE, ORIENT_KNEE = 3.0, 6.0              # deg kept clear of the allowed arc's edges; soft knee
UA_SHARE = 0.4                                   # upper-arm twist share of the roll on a straight arm
ORIENT_QA_PAD = 0.07                             # HandPass.finish keeps gated frames this far inside the QA limits
X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))


def soft_floor(x, lo, knee=KNEE):
    return x if x >= lo+knee else lo+knee*math.exp((x-lo-knee)/knee)


def soft_ceil(x, hi, knee=KNEE):
    return -soft_floor(-x, -hi, knee)


def soft_band(x, lo, hi, knee=KNEE):
    return soft_ceil(soft_floor(x, lo, knee), hi, knee)


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t*t*(3-2*t)


def twist(q, axis):
    """Twist angle (deg) of q about local X (0) or Y (1)."""
    c = (q.x, q.y)[axis]
    return (math.degrees(2*math.atan2(c, q.w))+180.0) % 360.0-180.0


def about(axis, deg):
    return Quaternion(axis, math.radians(deg))


TWIST_SHARE = 0.5
ROLL_R = 3                                    # frames: orientation-roll blur radius (two box passes)
ROLL_ITERS = 10                               # blur + band-clamp rounds in HandPass.finish


def blur_anchored(vals, n, pins=()):
    """Two box blurs over +-ROLL_R, then a correction that decays smoothly over 2*ROLL_R frames so the anchors
    (a one-shot's first/last frame, a loop's `pins`) keep exactly their raw value. Unlike padpass.smooth_series,
    frames next to an anchor stay blurred instead of easing back to the raw curve (a blip there stays smoothed).
    The first n values are the period when n < len(vals) (loop; the closing frame repeats frame 0)."""
    m = len(vals)
    loop = n < m

    def at(v, i):
        return v[i % n] if loop else v[min(max(i, 0), m-1)]
    r = range(-ROLL_R, ROLL_R+1)
    e = [sum(at(vals, i+j) for j in r)/len(r) for i in range(m)]
    b = [sum(at(e, i+j) for j in r)/len(r) for i in range(m)]
    span = 2*ROLL_R
    if loop:
        anchors = sorted({q % n for q in pins})
        dist = lambda i, q: min(abs(i-q) % n, n-abs(i-q) % n)
    else:
        anchors = [0, m-1]
        dist = lambda i, q: abs(i-q)
    offs = [(q, vals[q]-b[q]) for q in anchors]
    for i in range(n if loop else m):
        b[i] += sum(o*(1.0-smooth(dist(i, q)/span)) for q, o in offs)
    for q in anchors:
        b[q] = vals[q]
    if loop:
        b[n:] = b[:m-n]
    return b


def set_twist(poser):
    """Counter-roll each `<s> forearm twist` bone (bodyfix.forearm_twist) by TWIST_SHARE of its forearm's roll.
    bake runs this on every frame after the other passes, so the bone is captured and keyed like any other."""
    for s in ('L', 'R'):
        tb = poser.pb.get(f'{s} forearm twist')
        if tb is not None:
            tb.rotation_quaternion = about(Y, -TWIST_SHARE*twist(poser.pb[f'{s} forearm'].rotation_quaternion, 1))


class HandPass:
    def __init__(self, poser):
        self.p = poser
        self.max_roll = {}
        self.last_roll = {}
        self.prev = {}
        self.infeasible = 0
        r3 = poser.r3
        self.rest_rel = {}
        for s in ('L', 'R'):
            for d in DIGITS+('thumb',):
                for i in (2, 3):
                    n, parent = f'{s} {d}.{i}', f'{s} {d}.{i-1}'
                    self.rest_rel[n] = r3[parent].inverted() @ r3[n]

    def anat(self, n, q):
        """Bend of joint n toward the palm, in the parent segment's frame (deg)."""
        d = self.rest_rel[n] @ q.to_matrix() @ Y
        return math.degrees(math.atan2(d.z, d.y))

    def set_anat(self, n, q, lo, hi):
        a = self.anat(n, q)
        want = soft_band(a, lo, hi)
        for _ in range(3):
            if abs(want-a) < 1e-3:
                break
            slope = self.anat(n, q @ about(X, 1.0))-a
            if abs(slope) < 0.2:
                break
            q = q @ about(X, (want-a)/slope)
            a = self.anat(n, q)
        return q

    def side(self, s):
        pb = self.p.pb
        k = (1.0 if s == 'L' else -1.0)*ZSIGN
        # Thumb: slight curl floor on every joint, tuck along the index when curled.
        raw1 = twist(pb[f'{s} thumb.1'].rotation_quaternion, 0)
        flex1 = {d: twist(pb[f'{s} {d}.1'].rotation_quaternion, 0) for d in DIGITS}
        fist = 1.0-smooth((flex1['index']-TUCK_FIST[0])/(TUCK_FIST[1]-TUCK_FIST[0]))
        # Opening a gap between two fingers shifts each whole side of the hand, so no third finger is crowded.
        # (This moves the index fingertip muzzle; primary.solve_arm aims with this pass applied.)
        fan = dict.fromkeys(DIGITS, 0.0)
        for j, (a, b) in enumerate(zip(DIGITS, DIGITS[1:])):
            sep = SEP*smooth((abs(flex1[a]-flex1[b])-SEP_DEAD)/SEP_RAMP)
            sep += CURL_SEP*smooth((0.5*(flex1[a]+flex1[b])-CURL_SEP_RAMP[0])/(CURL_SEP_RAMP[1]-CURL_SEP_RAMP[0]))
            for n, d in enumerate(DIGITS):
                fan[d] += 0.5*sep if n > j else -0.5*sep
        for i in (1, 2, 3):
            b = pb[f'{s} thumb.{i}']
            q = b.rotation_quaternion.copy()
            f = twist(q, 0)
            q = q @ about(X, soft_band(f, THUMB_FLOOR[i], THUMB_CEIL)-f)
            if i == 1:
                w = smooth((raw1-TUCK_RAMP[0])/(TUCK_RAMP[1]-TUCK_RAMP[0]))*fist
                q = q @ about(Z, k*TUCK*w)
            b.rotation_quaternion = q
        for d in DIGITS:
            b = pb[f'{s} {d}.1']
            q = b.rotation_quaternion.copy()
            f = flex1[d]
            q = q @ about(X, soft_band(f, BASE_FLOOR, BASE_CEIL)-f)
            b.rotation_quaternion = q @ about(Z, k*fan[d]) if fan[d] else q
            for i in (2, 3):
                b = pb[f'{s} {d}.{i}']
                b.rotation_quaternion = self.set_anat(b.name, b.rotation_quaternion.copy(), *ANAT[i])
        for bone, lim in ((f'{s} hand', HAND_TWIST), (f'{s} forearm', FOREARM_ROLL)):
            b = pb[bone]
            q = b.rotation_quaternion.copy()
            t = twist(q, 1)
            q = q @ about(Y, soft_band(t, -lim, lim, 6.0)-t)
            if bone.endswith('hand'):
                tw = about(Y, twist(q, 1))
                sw = q @ tw.inverted()
                if sw.w < 0:
                    sw.negate()
                ang = math.degrees(sw.angle)
                if ang > 1e-6:
                    sw = about(sw.axis, soft_ceil(ang, HAND_BEND, 8.0))
                q = sw @ tw
            b.rotation_quaternion = q

    @staticmethod
    def orient_weight(st):
        """1 on every frame handorient gates (raised, not swept back), fading to 0 below the gate."""
        raised = max(smooth((st['raise']-30.0)/15.0),
                     smooth((st['raise']-20.0)/10.0)*smooth((st['elbow_open']-125.0)/15.0))
        return raised*(1.0-smooth(st['back_margin']/0.15))

    @staticmethod
    def allowed_arc(f, v, target, m):
        """Arc of forearm rolls psi (rad, about f) with rot(v, psi) . target >= m: (centre, half width);
        half width >= pi means every roll, < 0 means none."""
        a = f.dot(v)*f.dot(target)
        b = v.dot(target)-a
        c = f.cross(v).dot(target)
        amp = math.hypot(b, c)
        if amp < 1e-9:
            return 0.0, (math.pi if a >= m else -1.0)
        k = (m-a)/amp
        if k <= -1.0:
            return 0.0, math.pi
        if k >= 1.0:
            return math.atan2(c, b), -1.0
        return math.atan2(c, b), math.acos(k)

    @staticmethod
    def roll_needed(arcs, ref=None, band=False):
        """Smallest-magnitude roll (deg) into the intersection of the arcs, softly kept off the edges; 0 when the
        hand already sits well inside. ref (the previous frame's roll): when the intersection has several
        pieces, take the piece nearest ref, so the solution doesn't jump branch between frames.
        band=True returns (roll, (lo, hi)): the chosen piece minus the ORIENT_SAFE margins (deg)."""
        lo, hi = -math.pi, math.pi
        pieces = [(lo, hi)]
        for c, h in arcs:
            if h >= math.pi:
                continue
            if h < 0:
                return (None, None) if band else None
            nxt = []
            for shift in (-2*math.pi, 0.0, 2*math.pi):
                a, b = c-h+shift, c+h+shift
                for p0, p1 in pieces:
                    x0, x1 = max(a, p0), min(b, p1)
                    if x1 > x0:
                        nxt.append((x0, x1))
            pieces = nxt
            if not pieces:
                return (None, None) if band else None
        best = best_key = best_band = None
        for p0, p1 in pieces:
            p0d, p1d = math.degrees(p0), math.degrees(p1)
            lo_e, hi_e = p0d+ORIENT_SAFE, p1d-ORIENT_SAFE
            if p0d <= -179.99 and p1d >= 179.99:
                cand, rng = 0.0, (-math.inf, math.inf)
            elif hi_e-lo_e > 2*ORIENT_KNEE:
                cand, rng = soft_band(0.0, lo_e, hi_e, ORIENT_KNEE), (lo_e, hi_e)
            else:
                cand = 0.5*(p0d+p1d)
                rng = (lo_e, hi_e) if hi_e > lo_e else (cand, cand)
            key = abs(cand) if ref is None else max(p0d-ref, ref-p1d, 0.0)+1e-3*abs(cand)
            if best is None or key < best_key:
                best, best_key, best_band = cand, key, rng
        return (best, best_band) if band else best

    @staticmethod
    def roll_compromise(f, terms):
        """No roll satisfies every target: maximise the worst margin (1 deg grid, ties to the smallest roll)."""
        co = []
        for v, target, m in terms:
            a = f.dot(v)*f.dot(target)
            co.append((a-m, v.dot(target)-a, f.cross(v).dot(target)))
        best, best_score = 0.0, -9.0
        for i in range(-180, 181):
            psi = math.radians(i)
            cs, sn = math.cos(psi), math.sin(psi)
            score = min(a+b*cs+c*sn for a, b, c in co)
            if score > best_score+1e-3 or (score > best_score-1e-3 and abs(i) < abs(best)):
                best, best_score = float(i), max(score, best_score)
        return best

    def orient(self):
        import handorient
        p = self.p
        rig = p.rig
        rest_chest = p.rest['chest'].to_3x3()
        done = {}
        for it in range(2):
            p.update()
            fw = handorient.forward(rig, rest_chest)
            for s in ('L', 'R'):
                st = handorient.arm_state(rig, s, rest_chest)
                w = self.orient_weight(st)
                if w < 1e-3:
                    continue
                d, n, r = handorient.hand_axes(rig, s)
                f = st['fa']
                ref = self.prev.get(s) if it == 0 else None
                ref = None if ref is None else ref/max(w, 1e-3)
                terms = ((r, Z, ORIENT_THUMB_UP), (n, fw, ORIENT_PALM_FWD))
                need, rng = self.roll_needed([self.allowed_arc(f, *t) for t in terms], ref, band=True)
                if need is None:
                    self.infeasible += 1
                    relaxed = ((r, Z, ORIENT_RELAXED[0]), (n, fw, ORIENT_RELAXED[1]))
                    need, rng = self.roll_needed([self.allowed_arc(f, *t) for t in relaxed], ref, band=True)
                    if need is None:
                        need = self.roll_compromise(f, terms)
                        rng = (need, need)
                u = UA_SHARE*smooth((st['elbow_open']-120.0)/50.0)
                if w > 0.999:
                    # finish() may move the roll anywhere inside the QA limits (a little inside), not just the targets
                    qa = ((r, Z, handorient.THUMB_UP_MIN+ORIENT_QA_PAD), (n, fw, handorient.PALM_FWD_MIN+ORIENT_QA_PAD))
                    _, wide = self.roll_needed([self.allowed_arc(f, *t) for t in qa], need, band=True)
                    lo, hi = wide if wide is not None and wide[0] <= need <= wide[1] else rng
                    prior = done.get(s, 0.0)
                    self.frame_roll[f'{s} band'] = (prior+lo, prior+hi)
                self.frame_roll[f'{s} share'] = u
                corr = w*need
                if abs(corr) < 1e-4:
                    continue
                for bone, share in ((f'{s} forearm', 1.0-u), (f'{s} upperarm', u)):
                    if share > 0:
                        b = p.pb[bone]
                        b.rotation_quaternion = b.rotation_quaternion @ about(Y, corr*share)
                        self.frame_roll[bone] = self.frame_roll.get(bone, 0.0)+corr*share
                done[s] = done.get(s, 0.0)+corr
        self.last_roll = done
        self.prev = {s: done.get(s, 0.0) for s in ('L', 'R')}
        for s, v in done.items():
            self.max_roll[s] = max(self.max_roll.get(s, 0.0), abs(v))

    def apply(self):
        self.frame_roll = {}
        for s in ('L', 'R'):
            self.side(s)
        if ORIENT:
            self.orient()

    def finish(self, caps, frames, loop, pins=()):
        """Smooth each side's orientation roll over time and rewrite the arm chains in caps: the roll fades in and
        out with the raise gate within a few frames, which read as forearm pops. Alternates blur_anchored with a
        clamp into each gated frame's allowed band (so every frame handorient gates stays inside it; seams stay
        the raw solve), then splits the change between forearm and upper arm by that frame's share.
        `hist`: bake appends frame_roll per frame."""
        if len(self.hist) != len(frames):
            return {}
        m = len(frames)
        n = m-1 if loop and m > 2 else m
        delta = {}
        for s in ('L', 'R'):
            fa, ua = f'{s} forearm', f'{s} upperarm'
            raw = [h.get(fa, 0.0)+h.get(ua, 0.0) for h in self.hist]
            if not any(abs(v) > 1e-4 for v in raw):
                continue
            bands = [h.get(f'{s} band') for h in self.hist]
            x = list(raw)
            for _ in range(ROLL_ITERS):
                x = blur_anchored(x, n, pins)
                for i, bd in enumerate(bands[:n]):
                    if bd is not None:
                        x[i] = min(max(x[i], bd[0]), bd[1])
                if n < m:
                    x[n:] = x[:m-n]
            d = [a-b for a, b in zip(x, raw)]
            share = [h.get(f'{s} share', 0.0) for h in self.hist]
            delta[fa] = [di*(1.0-u) for di, u in zip(d, share)]
            delta[ua] = [di*u for di, u in zip(d, share)]
            if f'{s} forearm twist' in caps[frames[0]]:
                delta[f'{s} forearm twist'] = [-TWIST_SHARE*v for v in delta[fa]]
        if not delta:
            return {}
        bones = self.p.bones

        def depth(b):
            k, x = 0, bones[b].parent
            while x is not None:
                k, x = k+1, x.parent
            return k
        order = sorted((b for b in caps[frames[0]] if bones[b].parent is not None), key=depth)
        under = set(delta)
        for b in order:
            if bones[b].parent.name in under:
                under.add(b)
        for i, f in enumerate(frames):
            old = {b: caps[f][b].copy() for b in under}
            for b in order:
                if b not in under:
                    continue
                par = bones[b].parent.name
                rel = self.p.rest[par].inverted() @ self.p.rest[b]
                local = rel.inverted() @ old.get(par, caps[f][par]).inverted() @ old[b]
                if b in delta:
                    local = local @ about(Y, delta[b][i]).to_matrix().to_4x4()
                caps[f][b] = caps[f][par] @ rel @ local
        return {'orient_smooth_max_deg': round(max(abs(d) for v in delta.values() for d in v[:n]), 1)}
