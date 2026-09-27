"""Special: charge / discharge gestures and the Open Circuit crown.

All clips are upper-body gestures (RoR2 avatar mask without pelvis/legs): only spine, chest, neck,
head, scapulae, arms, fingers and halo move; pelvis, legs and tabard stay at rest (legs_ik=0).
One-shots start and end exactly at rest unless they declare a seam; loops repeat frame 1.

- Charge loop: hands frame an orb of current low in front of the belly, wide enough that the hands
  flank the waist from the chase camera; fingers curled and trembling over slow breathing, a
  heartbeat pulse through the chest, head bowed, halo arcs breathe open.
- Charge full: escalated: a much bigger orb held higher, elbows flared, chest up and leaning back,
  clawed splayed fingers, faster/stronger tremor and beat, halo held wide open and crackling.
- Discharge: gather to the core, then fling both arms out forward/sideways below shoulder height
  with a chest thrust; the halo arcs burst outward on a damped spring and settle.
- Open Circuit: hands rise to frame the core and spread while the four arcs lift, fan out and tip
  into a horizontal crown above the head; ends on 'Open Circuit hold' frame 1.
- Open Circuit hold: crown open, swaying about its axis with a rippling pulse; arms held low and
  open so it can sit on the gesture layer while running.
- Open Circuit end: starts on the hold pose, the crown tips back upright and descends into the
  rest halo while the hands close in front of the abdomen, then rest.

Crown math: each arc is pushed radially out of the ring (so gaps open), the whole ring is tilted
about X through its centre and lifted/moved over the head, then each arc is tipped about its own
tangent so the outer edge rises (a flared crown). Lift leads tilt so the lower arcs clear the
pauldrons; `diagnose` measures arc clearance to the head/shoulder mesh on every frame.
"""
import json
import math
import bpy
from mathutils import Euler, Quaternion, Vector, kdtree
from mathutils.bvhtree import BVHTree
from hs_anim import R, axis_rot, bake, lerp, ramp, sign, smoother

# ----------------------------------------------------------------------------- geometry
from padfix import HALO_SHIFT, halo_pose
RING_C = Vector((-0.0375, 0.15, 1.957))+HALO_SHIFT   # centre of the arc meshes (rest)
RING_N = Vector((0.0, 1.0, 0.0))          # rest ring normal (the halo stands in the XZ plane)
CORE = Vector((-0.04, -0.115, 1.51))
ORB = Vector((-0.04, -0.27, 1.38))        # the held "orb" of current in front of the core
# Charge orbs sit low and wide so the hands flank the slim waist and read from the chase camera
# (behind/above); the full charge is higher, further out and much wider.
ORB_LOOP = Vector((-0.04, -0.30, 1.13))
ORB_FULL = Vector((-0.04, -0.36, 1.21))
ARC_OBJ = 'HALO | independent copper arc {}'
GEO = {}

# Crown pose: tilt about X (deg, top goes back), lift/forward (m), radial spread (m), flare (deg).
CROWN = dict(tilt=80.0, lift=0.36, fwd=-0.10, spread=0.08, flare=24.0)


def arc_geometry():
    """Arc centroids (rest) and in-plane radial/tangent axes."""
    for i in range(1, 5):
        o = bpy.data.objects[ARC_OBJ.format(i)]
        pts = [o.matrix_world @ v.co for v in o.data.vertices]
        g = sum(pts, Vector())/len(pts)
        u = g-RING_C
        u.y = 0.0
        u.normalize()
        GEO[i] = {'g': g, 'u': u, 't': RING_N.cross(u).normalized()}


# ----------------------------------------------------------------------------- state
ARM = ('swing', 'adduct', 'twist', 'elbow', 'ftwist', 'wx', 'wy', 'wz')
OPT = ('swing', 'adduct', 'elbow', 'ftwist', 'wx', 'wy', 'wz')   # upper-arm twist stays 0: it swings the pauldrons out
HAND = ('curl', 'thumb', 'splay')
HALO = ('tilt', 'lift', 'fwd', 'spread', 'spin', 'flare', 'ox', 'oy', 'oz', 'rx', 'ry', 'rz')
KEYS = ([f'{b}_{a}' for b in ('spine', 'chest', 'neck', 'head') for a in 'xyz'] +
        [f'{s}_{k}' for s in 'LR' for k in ARM+HAND+('sx', 'sy', 'sz')] +
        [f'h_{k}' for k in HALO] + [f'a{i}_{k}' for i in range(1, 5) for k in ('spread', 'flare', 'lift', 'lag')])
ZERO = dict.fromkeys(KEYS, 0.0)


def rest_state():
    """Bind-rest arms with the relaxed Idle hand (air.STAND_ARMS curl/thumb), not flat paddles."""
    return st(**both(curl=21.0, thumb=12.0))


def st(**kw):
    d = dict(ZERO)
    for k, v in kw.items():
        if k not in d:
            raise KeyError(k)
        d[k] = v
    return d


def arms(s, pose, **extra):
    """Merge solved arm dicts {'L': {...}, 'R': {...}} into a state."""
    for side in 'LR':
        for k, v in pose[side].items():
            s[f'{side}_{k}'] = v
    s.update(extra)
    return s


def both(**kw):
    """Same value on both sides: both(curl=40) -> L_curl, R_curl."""
    out = {}
    for k, v in kw.items():
        out[f'L_{k}'] = v
        out[f'R_{k}'] = v
    return out


def mix(a, b, t):
    return {k: a[k]+(b[k]-a[k])*t for k in a}


def add(a, b, w=1.0):
    return {k: a[k]+w*b.get(k, 0.0) for k in a}


def track(f, keys):
    """Monotone cubic (PCHIP) through {frame: value}: no overshoot, flat at extrema and ends."""
    ks = sorted(keys.items())
    if f <= ks[0][0]:
        return ks[0][1]
    if f >= ks[-1][0]:
        return ks[-1][1]
    xs = [k for k, _ in ks]
    ys = [v for _, v in ks]
    d = [(ys[i+1]-ys[i])/(xs[i+1]-xs[i]) for i in range(len(ks)-1)]
    m = [0.0]*len(ks)
    for i in range(1, len(ks)-1):
        if d[i-1]*d[i] > 0:
            h0, h1 = xs[i]-xs[i-1], xs[i+1]-xs[i]
            w1, w2 = 2*h1+h0, h1+2*h0
            m[i] = (w1+w2)/(w1/d[i-1]+w2/d[i])
    i = max(j for j in range(len(ks)-1) if xs[j] <= f)
    h = xs[i+1]-xs[i]
    u = (f-xs[i])/h
    u2, u3 = u*u, u*u*u
    return (2*u3-3*u2+1)*ys[i]+(u3-2*u2+u)*h*m[i]+(-2*u3+3*u2)*ys[i+1]+(u3-u2)*h*m[i+1]


def keyed(f, keys):
    """Interpolate every state channel through {frame: state}."""
    return {k: track(f, {fr: s[k] for fr, s in keys.items()}) for k in KEYS}


def tremor(t, seed, harmonics):
    """Periodic jitter in [-1, 1] (integer harmonics of the loop so loops wrap)."""
    v = 0.0
    for j, h in enumerate(harmonics):
        v += math.sin(2*math.pi*(h*t+0.37*seed*(j+1)+0.13*seed*seed))
    return v/len(harmonics)


def spring(x, freq=2.2, decay=4.0):
    """Unit step response with overshoot (x in frames after the impulse)."""
    if x <= 0:
        return 0.0
    return 1.0-math.exp(-x/decay)*math.cos(2*math.pi*x/24*freq)


def kick(x, freq=2.0, decay=4.0):
    """Impulse response: 0 -> peak -> damped ringing -> 0."""
    if x <= 0:
        return 0.0
    return math.exp(-x/decay)*math.sin(2*math.pi*x/24*freq)*smoother(x/2)/0.62


# ----------------------------------------------------------------------------- apply
SPLAY_AXES = {}


def splay_axes(p, side):
    """Per-finger local axis for splay: the rest palm normal, made perpendicular to the bone."""
    if side not in SPLAY_AXES:
        head = lambda n: p.rest[n].translation
        n = (head(f'{side} index.1')-head(f'{side} little.1')).cross(head(f'{side} middle.1')-head(f'{side} hand'))
        axes = {}
        for d in ('index', 'middle', 'ring', 'little'):
            m = p.rest[f'{side} {d}.1'].to_3x3().normalized()
            a = m.inverted() @ n
            a -= Vector((0, 1, 0))*a.y
            axes[d] = a.normalized()
        SPLAY_AXES[side] = axes
    return SPLAY_AXES[side]


def fingers(p, side, curl, thumb, splay):
    axes = splay_axes(p, side)
    for digit, extra, fan in (('index', -0.25, -1.0), ('middle', 0.0, -0.3), ('ring', 0.2, 0.4), ('little', 0.45, 1.0)):
        for i in range(1, 4):
            amount = curl*(1+extra*0.4)*(1.15 if i == 2 else 1.0)
            q = Euler((math.radians(amount), 0, 0)).to_quaternion()
            if i == 1:
                q = Quaternion(axes[digit], math.radians(0.7*fan*splay)) @ q
            p.pb[f'{side} {digit}.{i}'].rotation_quaternion = q
    for i in range(1, 4):
        p.pb[f'{side} thumb.{i}'].rotation_quaternion = Euler((math.radians(thumb*(0.6 if i == 1 else 1.0)), 0, 0)).to_quaternion()


def halo(p, s):
    halo_pose(p, (s['h_ox'], s['h_oy'], s['h_oz']), R(s['h_rx'], s['h_ry'], s['h_rz']))
    for i in range(1, 5):
        g, u, t = GEO[i]['g'], GEO[i]['u'], GEO[i]['t']
        lag = s[f'a{i}_lag']   # 0..1 per-arc stagger: fraction of the crown this arc still lacks
        tilt = s['h_tilt']*(1-lag)
        lift = s['h_lift']+s[f'a{i}_lift']
        spread = s['h_spread']+s[f'a{i}_spread']
        flare = s['h_flare']+s[f'a{i}_flare']
        m = R(x=-tilt) @ R(y=s['h_spin'])
        L = Vector((0.0, s['h_fwd'], lift))
        def ring(x):
            return RING_C+L+m @ (x+u*spread-RING_C)
        g2 = ring(g)
        fw = m @ axis_rot(t, flare) @ m.inverted()
        head = p.rest[f'halo {i}'].translation
        target = g2+fw @ (ring(head)-g2)
        p.rot(f'halo {i}', fw @ m)
        p.offset(f'halo {i}', target-head)


# 'L/R pauldron' carry a LOCAL->LOCAL Copy Rotation (40% of the upper arm, about the pauldron's own
# differently rolled axes). `bake` captures with it on and mutes it for playback, so the keys hold
# exactly 40%. The scapula counter-rotates so the pauldron equals a clean world-space design follow.
PAULDRON_PLAYBACK = 0.4
PREVIEW_ORTHO = 3.2


def pauldron_design(side, swing, adduct):
    """Deltoid-pad behaviour: 40% of swing and of raising the arm out, 10% of drawing it inward
    (a full follow of inward adduction lifts the pad's outer edge into a wing)."""
    return R(x=0.4*swing) @ R(y=sign(side)*(0.4*adduct if adduct < 0 else 0.1*adduct))


def pauldron_fix(p, side, shrug, swing, adduct):
    ru, rp = p.r3[f'{side} upperarm'], p.r3[f'{side} pauldron']
    qu = p.pb[f'{side} upperarm'].rotation_quaternion.copy()
    got = rp @ Quaternion().slerp(qu, PAULDRON_PLAYBACK).to_matrix() @ rp.inverted()
    return shrug @ pauldron_design(side, swing, adduct) @ got.inverted()


def apply(p, s):
    for b in ('spine', 'chest', 'neck', 'head'):
        p.rot(b, R(s[f'{b}_x'], s[f'{b}_y'], s[f'{b}_z']))
    for side in 'LR':
        k = sign(side)
        g = lambda n: s[f'{side}_{n}']
        p.arm(side, swing=g('swing'), adduct=g('adduct'), elbow=g('elbow'), twist=g('twist'),
              wrist=(g('wx'), g('wy'), g('wz')), forearm_twist=g('ftwist'))
        p.rot(f'{side} scapula', pauldron_fix(p, side, R(g('sx'), k*g('sy'), k*g('sz')), g('swing'), g('adduct')))
        fingers(p, side, g('curl'), g('thumb'), g('splay'))
    halo(p, s)


# ----------------------------------------------------------------------------- arm solver
def fk_hand(p, side):
    """Arm chain world matrices with the torso at rest (pure math, no depsgraph)."""
    rest = p.rest
    ua = rest[f'{side} upperarm'] @ p.pb[f'{side} upperarm'].matrix_basis
    fa = ua @ rest[f'{side} upperarm'].inverted() @ rest[f'{side} forearm'] @ p.pb[f'{side} forearm'].matrix_basis
    hd = fa @ rest[f'{side} forearm'].inverted() @ rest[f'{side} hand'] @ p.pb[f'{side} hand'].matrix_basis
    loc = hd @ rest[f'{side} hand'].inverted()
    pt = lambda n: loc @ rest[n].translation
    return {'shoulder': ua.translation, 'elbow': fa.translation, 'wrist': hd.translation, 'knuckle': pt(f'{side} middle.1'),
            'index': pt(f'{side} index.1'), 'little': pt(f'{side} little.1')}


def palm_normal(side, j):
    n = (j['index']-j['little']).cross(j['knuckle']-j['wrist'])
    return n.normalized()*PALM_SIGN[side]


PALM_SIGN = {'L': 1.0, 'R': 1.0}
HAND_LEN = {}
ARM_LEN = {}
LIMITS = {'swing': (-110, 60), 'adduct': (-85, 70), 'twist': (-90, 90), 'elbow': (0, 150), 'ftwist': (-120, 120),
          'wx': (-75, 75), 'wy': (-60, 60), 'wz': (-60, 60)}


def calibrate_hands(p):
    p.reset()
    p.update()
    for side in 'LR':
        j = fk_hand(p, side)
        HAND_LEN[side] = (j['knuckle']-j['wrist']).length
        ARM_LEN[side] = (j['wrist']-p.rest[f'{side} upperarm'].translation).length
        n = (j['index']-j['little']).cross(j['knuckle']-j['wrist']).normalized()
        # Rest palms face the thigh (toward the midline).
        PALM_SIGN[side] = 1.0 if n.x*sign(side) < 0 else -1.0
        # sanity: FK math must agree with the depsgraph
        p.arm(side, swing=-30, adduct=20, elbow=60, twist=10, wrist=(10, 5, -5), forearm_twist=30)
        j = fk_hand(p, side)
        p.update()
        err = (j['knuckle']-p.world(f'{side} middle.1').translation).length
        print(f'SPECIAL hand {side} len {HAND_LEN[side]:.3f} palm_sign {PALM_SIGN[side]} fk_err {err:.6f}', flush=True)
        if err > 1e-4:
            raise RuntimeError('fk_hand disagrees with the depsgraph')
    p.reset()


def mirror(v, x0=CORE.x):
    return Vector((2*x0-v.x, v.y, v.z))


TORSO_C = Vector((-0.0375, 0.045))   # rest torso cross-section at elbow height (z 1.1-1.4): centre, half-axes
TORSO_R = (0.17, 0.15)
TORSO_MARGIN = 0.045
ARM_MARGIN = 0.09     # upper arm / elbow: the arm mesh is ~6 cm in radius there (M5)
WAIST_MARGIN = 0.13   # the same for elbows low at the waist, where the flank flares past TORSO_R (solve avoid=)


def torso_depth(q, margin=TORSO_MARGIN):
    """How far a point sits inside the torso ellipse grown by `margin` (0 outside; z 0.95-1.6 only)."""
    if not 0.95 < q.z < 1.6:
        return 0.0
    dx = (q.x-TORSO_C.x)/(TORSO_R[0]+margin)
    dy = (q.y-TORSO_C.y)/(TORSO_R[1]+margin)
    return max(0.0, 1.0-math.hypot(dx, dy))


def solve(p, side, knuckle, direction, normal, elbow=None, straight=False, init=None, ortho=False, avoid=False):
    """Arm channels placing the knuckle (middle.1 head) at `knuckle`, fingers along `direction`,
    palm normal toward `normal`; optional soft elbow position. `ortho` makes the palm target
    perpendicular to the fingers (the palm can't face along its own fingers). `avoid` keeps the elbow
    and forearm out of the torso (poses whose hands meet in front of the chest); True, or the elbow margin (m)."""
    k_t = Vector(knuckle)
    d_t = Vector(direction).normalized()
    n_t = n_raw = Vector(normal).normalized()
    if ortho:
        n_t = (n_t-d_t*n_t.dot(d_t)).normalized()
    w_t = k_t-d_t*HAND_LEN[side]
    e_t = Vector(elbow) if elbow is not None else None

    def err(v):
        a = dict(zip(OPT, v), twist=0.0)
        p.arm(side, swing=a['swing'], adduct=a['adduct'], elbow=a['elbow'], twist=a['twist'],
              wrist=(a['wx'], a['wy'], a['wz']), forearm_twist=a['ftwist'])
        j = fk_hand(p, side)
        e = 100*(j['wrist']-w_t).length+100*(j['knuckle']-k_t).length
        e += 12*(1-palm_normal(side, j).dot(n_t))
        if e_t is not None:
            e += (15 if avoid else 6)*(j['elbow']-e_t).length
        if straight:
            e += 0.001*a['elbow']**2
        e += 0.00015*a['ftwist']**2+0.0006*(a['wx']**2+a['wy']**2+a['wz']**2)
        e += 0.003*max(0.0, a['adduct']-40)**2   # the upper arm would cross the chest
        if avoid:
            am = ARM_MARGIN if avoid is True else avoid
            for q, m in ((j['shoulder'].lerp(j['elbow'], 0.6), am), (j['elbow'], am),
                         (j['elbow'].lerp(j['wrist'], 0.5), TORSO_MARGIN), (j['wrist'], TORSO_MARGIN)):
                e += 300*torso_depth(q, m)
        for k, (lo, hi) in LIMITS.items():
            if a[k] < lo or a[k] > hi:
                e += 10*(max(lo-a[k], a[k]-hi))
        return e
    starts = [init] if init else []
    starts += [(sw, ad, el, ft, 0, 0, 0) for sw in (-60, -40, -20, 0) for ad in (50, 25, 0, -30)
               for el in ((10, 40) if straight else (90, 110, 130)) for ft in (-30, 0, 45, 90)]
    if avoid:   # hands meeting in front of the chest: the elbow comes forward, not across
        starts += [(sw, ad, el, ft, 0, 0, 0) for sw in (-75, -60, -45, -30) for ad in (-15, 0, 15, 30)
                   for el in (95, 110, 125, 140) for ft in (0, 30, 60, 90)]
    best = min((tuple(float(x) for x in s0) for s0 in starts), key=err)
    step = 16.0
    score = err(best)
    while step > 0.05:
        improved = False
        for i in range(len(OPT)):
            for dv in (-step, step):
                v = list(best)
                v[i] += dv
                e = err(v)
                if e < score:
                    best, score, improved = tuple(v), e, True
        if not improved:
            step *= 0.5
    a = dict(zip(OPT, best), twist=0.0)
    p.arm(side, swing=a['swing'], adduct=a['adduct'], elbow=a['elbow'], twist=a['twist'],
          wrist=(a['wx'], a['wy'], a['wz']), forearm_twist=a['ftwist'])
    j = fk_hand(p, side)
    report = {'miss_mm': round(1000*(j['knuckle']-k_t).length, 1), 'wrist_mm': round(1000*(j['wrist']-w_t).length, 1),
              'palm_deg': round(math.degrees(palm_normal(side, j).angle(n_t)), 1), 'score': round(score, 3)}
    if ortho:
        report['palm_raw_deg'] = round(math.degrees(palm_normal(side, j).angle(n_raw)), 1)
        report['finger_deg'] = round(math.degrees((j['knuckle']-j['wrist']).angle(d_t)), 1)
    p.reset()
    return {k: round(v, 2) for k, v in a.items()}, report


def solve_pair(p, name, knuckle_L, direction_L, normal_L, elbow_L=None, straight=False, ortho=False, elbow_R=None,
               avoid=False):
    out, rep = {}, {}
    for side in 'LR':
        m = (lambda v: Vector(v)) if side == 'L' else (lambda v: mirror(Vector(v)))
        md = (lambda v: Vector(v)) if side == 'L' else (lambda v: Vector((-v[0], v[1], v[2])))
        e = elbow_L if side == 'L' or elbow_R is None else elbow_R
        e = None if e is None else (Vector(e) if side == 'L' or elbow_R is not None else mirror(Vector(e), 0.0))
        out[side], rep[side] = solve(p, side, m(knuckle_L), md(direction_L), md(normal_L), e, straight, ortho=ortho,
                                     avoid=avoid)
    SOLVED[name] = {'arms': out, 'fit': rep}
    print('SPECIAL solve', name, json.dumps(SOLVED[name]), flush=True)
    return out


def solve_reach(p, name, rel_L, d_L, n_L, reach=0.95, ortho=False):
    """Nearly straight arm along world direction rel (from the shoulder), fingers along d."""
    out, rep = {}, {}
    for side in 'LR':
        k = sign(side)
        rel = Vector((k*rel_L[0], rel_L[1], rel_L[2])).normalized()
        d = Vector((k*d_L[0], d_L[1], d_L[2])).normalized()
        n = Vector((k*n_L[0], n_L[1], n_L[2]))
        w = p.rest[f'{side} upperarm'].translation+rel*reach*ARM_LEN[side]
        out[side], rep[side] = solve(p, side, w+d*HAND_LEN[side], d, n, straight=True, ortho=ortho)
    SOLVED[name] = {'arms': out, 'fit': rep}
    print('SPECIAL solve', name, json.dumps(SOLVED[name]), flush=True)
    return out


SOLVED = {}
POSES = {}
RELEASE_DIR = (0.92, -0.25, -0.46)
RELEASE_EXT = 30.0


def solve_poses(p):
    calibrate_hands(p)
    POSES['charge'] = solve_pair(p, 'charge', ORB_LOOP+Vector((0.24, 0.0, 0.01)), (-0.4, -0.8, 0.3),
                                 (-1, 0.0, 0.2), elbow_L=(0.37, 0.06, 1.17))
    POSES['charge_wide'] = solve_pair(p, 'charge_wide', ORB_LOOP+Vector((0.27, -0.01, 0.02)), (-0.35, -0.8, 0.3),
                                      (-1, 0.0, 0.15), elbow_L=(0.39, 0.06, 1.17))
    # Palms cradle the orb but tip ~20 deg forward: raised arms keep palms off backward (handorient gate).
    POSES['charge_full'] = solve_pair(p, 'charge_full', ORB_FULL+Vector((0.30, 0.0, 0.06)), (-0.4, -0.75, 0.35),
                                      (-1, -0.36, 0.1), elbow_L=(0.45, 0.03, 1.27))
    POSES['charge_full_wide'] = solve_pair(p, 'charge_full_wide', ORB_FULL+Vector((0.34, -0.01, 0.07)),
                                           (-0.35, -0.75, 0.35), (-1, -0.36, 0.05), elbow_L=(0.47, 0.03, 1.29))
    # avoid: without it the left solve hit the adduct limit and sank the elbow ~5 cm into the ribs (contact.py).
    # Elbows forward and out (M5): with the elbow at the flank the solve crossed the upper arm over the belly
    # (adduct ~48) and the arm skin sank 45-58 mm into the flank and hip.
    POSES['gather'] = solve_pair(p, 'gather', ORB+Vector((0.13, -0.09, -0.02)), (-0.4, -0.45, 0.7), (-1, 0.3, 0),
                                 elbow_L=(0.31, -0.22, 1.20), avoid=WAIST_MARGIN)
    # Release: a wide lateral fling, palms pushed forward/out, wrists extended ~30 deg (fingers tip back).
    r = Vector(RELEASE_DIR).normalized()
    n = Vector((0.3, -0.95, 0.1))
    n = (n-r*n.dot(r)).normalized()
    ext = math.radians(RELEASE_EXT)
    d = r*math.cos(ext)-n*math.sin(ext)
    n = n*math.cos(ext)+r*math.sin(ext)
    POSES['release'] = solve_reach(p, 'release', RELEASE_DIR, d, n, ortho=True)
    POSES['frame'] = solve_pair(p, 'frame', CORE+Vector((0.15, -0.17, -0.04)), (-0.5, -0.45, 0.75), (-0.3, -1, 0.1),
                                elbow_L=(0.31, -0.05, 1.20), ortho=True, avoid=WAIST_MARGIN)
    POSES['spread'] = solve_reach(p, 'spread', (0.85, -0.3, -0.45), (0.85, -0.42, -0.08), (0.1, -0.9, 0.45))
    POSES['hold'] = solve_reach(p, 'hold', (0.62, -0.30, -0.72), (0.5, -0.4, -0.75), (0.2, -0.95, 0.2), reach=0.93)
    POSES['recall'] = solve_pair(p, 'recall', Vector((CORE.x+0.15, -0.38, 1.29)), (-0.45, -0.5, 0.6), (-1, 0.2, 0),
                                 elbow_L=(0.31, -0.22, 1.16), avoid=0.11)


# ----------------------------------------------------------------------------- clips
CHARGE_N = 40
FULL_N = 24
HOLD_N = 24


def charge_state(t, full):
    """Charge hold at loop phase t in [0, 1)."""
    tau = 2*math.pi
    b = math.sin(tau*t)                      # breath: + inhale
    hp = 0.5-0.5*math.cos(tau*(t-0.08))      # halo pulse 0..1, trails the breath slightly
    # Heartbeat: sharp periodic surges (2 per loop; 3 in the shorter, more urgent full charge).
    beat = max(0.0, math.cos(tau*(3 if full else 2)*(t-0.05)))**6
    if full:
        # Escalated: orb held high and wide, elbows flared, chest up with a slight lean back,
        # head bowed into the light, fingers clawed and splayed.
        s = arms(st(), POSES['charge_full'])
        s.update(spine_x=-4.0-1.0*b-1.5*beat, chest_x=-9.0-1.6*b-2.5*beat, neck_x=7.0+0.5*b, head_x=13.0+1.0*b)
        s.update(both(curl=62+3*b, thumb=40, splay=22, sx=-6-1.5*b-1.5*beat, sz=-7))
        s.update(h_spread=0.050+0.014*hp+0.012*beat, h_flare=11.0+3.5*hp+3.0*beat, h_tilt=6.0+1.5*hp,
                 h_lift=0.018+0.006*hp+0.006*beat, h_spin=2.5*math.sin(tau*t))
        amp_arm, amp_fing, harm = 3.4, 2.5, (7, 10, 13)
        wide, widen = POSES['charge_full_wide'], 0.35+0.25*b+0.3*beat
    else:
        s = arms(st(), POSES['charge'])
        s.update(spine_x=-1.5-0.8*b-0.8*beat, chest_x=-4.0-1.2*b-1.4*beat, neck_x=4.5+0.4*b, head_x=10.0+0.8*b)
        s.update(both(curl=40+3*b, thumb=28, splay=8, sx=-2-1.0*b-0.8*beat, sz=-3))
        s.update(h_spread=0.006+0.014*hp+0.006*beat, h_flare=1.0+4.0*hp+1.5*beat, h_tilt=1.5*hp, h_lift=0.006*hp,
                 h_spin=1.5*math.sin(tau*t))
        amp_arm, amp_fing, harm = 1.5, 3.5, (6, 9, 13)
        wide, widen = POSES['charge_wide'], 0.35+0.35*b+0.2*beat
    # Inhale (and each beat) widens the orb: blend toward the wide solve.
    wide = arms(st(), wide)
    for side in 'LR':
        for k in ARM:
            s[f'{side}_{k}'] = lerp(s[f'{side}_{k}'], wide[f'{side}_{k}'], widen)
    # Current tremor: small high-frequency jitter on the forearm/wrist/fingers and halo arcs.
    for n, side in enumerate('LR'):
        for j, k in enumerate(('elbow', 'wx', 'wz', 'ftwist')):
            s[f'{side}_{k}'] += amp_arm*tremor(t, 3+n*7+j, harm)
        s[f'{side}_curl'] += amp_fing*tremor(t, 11+n*5, harm)
        s[f'{side}_thumb'] += 0.6*amp_fing*tremor(t, 17+n*3, harm)
    s['head_z'] += 0.4*amp_arm*tremor(t, 29, harm[:2])
    for i in range(1, 5):
        s[f'a{i}_flare'] += (1.6 if full else 0.8)*tremor(t, 31+i*4, harm)
        s[f'a{i}_spread'] += (0.004 if full else 0.002)*tremor(t, 41+i*3, harm)
    return s


def charge_pose(full):
    n = FULL_N if full else CHARGE_N
    def fn(p, f):
        apply(p, charge_state(((f-1) % n)/n, full))
    return fn


# -- Discharge
DIS_N = 28
GATHER, RELEASE = 5, 9
DIS_CLENCH, DIS_THUMB = 34.0, 26.0   # finger curl at the gather (discharge_pose drives curl/thumb up to the release)


def discharge_keys():
    rest = rest_state()
    gather = arms(st(spine_x=6, chest_x=8, neck_x=-2, head_x=9), POSES['gather'],
                  **both(curl=DIS_CLENCH, thumb=DIS_THUMB, splay=0, sx=-4, sz=-6),
                  h_spread=-0.004, h_flare=-6.0, h_lift=0.008, h_rx=3.0)
    tense = add(gather, st(spine_x=1, chest_x=1.5, head_x=1.5, h_spread=-0.004))
    release = arms(st(spine_x=-5, chest_x=-15, neck_x=-3, head_x=-9), POSES['release'],
                   **both(curl=10, thumb=6, splay=20, sx=2, sz=7))
    recoil = arms(st(spine_x=-5, chest_x=-15.5, neck_x=-3.5, head_x=-9.5), POSES['release'],
                  **both(curl=6, thumb=5, splay=20, sx=2, sz=6))
    for side in 'LR':
        recoil[f'{side}_elbow'] += 14
        recoil[f'{side}_swing'] += 8
        recoil[f'{side}_adduct'] += 6
        recoil[f'{side}_wx'] -= 12
    settle = mix(recoil, rest, 0.4)
    settle.update(both(curl=10, splay=6))
    low = mix(recoil, rest, 0.8)
    low.update(both(curl=6, splay=2))
    # Hit-stop: the release pose holds for a frame before the recoil.
    return {1: rest, GATHER: gather, 7: tense, RELEASE: release, RELEASE+HITSTOP: dict(release),
            12: recoil, 16: settle, 22: low, DIS_N: rest}


HITSTOP = 1


def burst_env(x):
    """Discharge halo burst: snaps up over the frame before release, holds through the hit-stop,
    then rings down (negative lobes clipped so the lower arcs don't sink)."""
    if x < -1:
        return 0.0
    if x < 0:
        return smoother(x+1)
    if x <= HITSTOP:
        return 1.0
    y = x-HITSTOP
    v = math.exp(-y/4.5)*math.cos(2*math.pi*y/24*1.6)
    return v if v > 0 else 0.25*v


BURST = dict(spread=0.22, lift=0.07, flare=32.0)


def discharge_pose(p, f):
    s = keyed(f, DIS_KEYS)
    x = f-RELEASE
    fade = 1.0-ramp(f, DIS_N-6, DIS_N)
    burst = burst_env(x)*fade
    s['h_spread'] += BURST['spread']*burst
    s['h_lift'] += BURST['lift']*burst
    s['h_flare'] += BURST['flare']*burst
    s['h_spin'] += 9*kick(x-HITSTOP+1, freq=2.2, decay=4.0)*fade
    s['h_oy'] += 0.025*burst
    s['h_oz'] += 0.02*burst
    s['h_rx'] += -5*burst
    for i in range(1, 5):
        s[f'a{i}_flare'] += 5*kick(x-HITSTOP+1.5-0.5*i, freq=2.4, decay=3.5)*fade*(1 if i % 2 else -1)
    # tension tremor through the gather, gone by the release
    tt = ramp(f, 2, GATHER)*(1-ramp(f, RELEASE-1, RELEASE))
    if f <= RELEASE:
        # Fingers: even clench into the gather, then burst open under constant acceleration into the hit-stop.
        r0, r1 = DIS_KEYS[1], DIS_KEYS[RELEASE]
        for side in 'LR':
            for k, peak in (('curl', DIS_CLENCH), ('thumb', DIS_THUMB)):
                a, b = r0[f'{side}_{k}'], r1[f'{side}_{k}']
                if f <= GATHER:
                    x = (f-1)/(GATHER-1)
                    s[f'{side}_{k}'] = lerp(a, peak, 2*x*x if x < 0.5 else 1-2*(1-x)**2)
                else:
                    s[f'{side}_{k}'] = lerp(peak, b, ((f-GATHER)/(RELEASE-GATHER))**2)
    for n, side in enumerate('LR'):
        for j, k in enumerate(('elbow', 'wx', 'wz')):
            s[f'{side}_{k}'] += 1.8*tt*math.sin(2.3*f+j+n*1.7)
    apply(p, s)


# -- Open Circuit
OC_N = 30
UNFOLD, CROWN_ACTIVE = 6, 22


def crown_state(**over):
    c = dict(CROWN)
    c.update(over)
    return {'h_tilt': c['tilt'], 'h_lift': c['lift'], 'h_fwd': c['fwd'], 'h_spread': c['spread'], 'h_flare': c['flare']}


def hold_state(t):
    tau = 2*math.pi
    b = math.sin(tau*t)
    pulse = math.sin(2*tau*t)                 # in step with the crown's spread pulse
    s = arms(st(spine_x=-1.5-0.9*b, chest_x=-3.0-1.3*b, neck_x=1.0-0.3*b, head_x=0.5+0.9*b,
                spine_y=0.8*math.sin(tau*(t-0.2)), chest_y=-0.6*math.sin(tau*(t-0.25)),
                head_y=1.2*math.sin(tau*(t-0.35)), head_z=0.8*math.sin(tau*(t-0.1))), POSES['hold'],
             **both(curl=12+2*b-3*pulse, thumb=8-2*pulse, splay=12+4*pulse, sx=-1-1.5*b, sz=0.8*pulse))
    for n, side in enumerate('LR'):
        s[f'{side}_adduct'] += -3.5*b-1.2*pulse
        s[f'{side}_swing'] += -1.5*math.sin(tau*(t-0.1-0.05*n))
        s[f'{side}_elbow'] += 2.5*math.sin(tau*(2*t-0.06))
        s[f'{side}_wx'] += -3.0*math.sin(tau*(2*t-0.1))
    s.update(crown_state())
    s['h_spin'] = 9.0*math.sin(tau*t)
    s['h_lift'] += 0.012*math.sin(tau*t-0.6)
    s['h_spread'] += 0.010*math.sin(2*tau*t)
    for i in range(1, 5):
        ph = tau*(t-(i-1)/4)
        s[f'a{i}_lift'] = 0.014*math.sin(ph)
        s[f'a{i}_flare'] = 3.0*math.sin(ph+0.9)
    for n, side in enumerate('LR'):
        s[f'{side}_curl'] += 2.5*tremor(t, 5+n, (5, 8, 11))
        s[f'{side}_wx'] += 1.0*tremor(t, 9+n, (5, 8, 11))
    return s


def hold_pose(p, f):
    apply(p, hold_state(((f-1) % HOLD_N)/HOLD_N))


def oc_keys():
    rest = rest_state()
    full = arms(st(spine_x=1, chest_x=1, head_x=3), POSES['frame'], **both(curl=10, thumb=10))
    rise = mix(rest, full, 0.35)
    for side in 'LR':
        for k, w in (('elbow', 0.68), ('ftwist', 0.6), ('adduct', 0.55)):   # tuck the forearm in as it flexes, not out
            rise[f'{side}_{k}'] = w*full[f'{side}_{k}']
        rise[f'{side}_swing'] -= 5   # lifts forward off the hip on the way up (contact.py)
    frame = arms(st(spine_x=-1, chest_x=-2, neck_x=1, head_x=4), POSES['frame'], **both(curl=18, thumb=22, splay=6))
    frame2 = arms(st(spine_x=-2, chest_x=-4, neck_x=1, head_x=2), POSES['frame'], **both(curl=14, thumb=18, splay=10))
    spread = arms(st(spine_x=-3, chest_x=-6, neck_x=-1, head_x=-4), POSES['spread'],
                  **both(curl=-4, thumb=0, splay=16, sx=2, sz=2))
    keys = {1: rest, UNFOLD: rise, 9: frame, 12: frame2, 20: spread, CROWN_ACTIVE: spread}
    for s in keys.values():
        for k in ('h_tilt', 'h_lift', 'h_fwd', 'h_spread', 'h_flare'):
            s[k] = 0.0
    return keys


def oc_pose(p, f):
    s = keyed(f, OC_KEYS)
    # Crown timing: lift leads, fan-out with it, tilt later (lower arcs clear the pauldrons).
    c = CROWN
    for i in range(1, 5):
        lead = 0.0 if i in (2, 3) else 1.5
        s[f'a{i}_lag'] = 1.0-smoother((f-9-lead)/11.0)
        s[f'a{i}_lift'] = c['lift']*(smoother((f-4-0.6*lead)/11.0)-smoother((f-4)/11.0))
    s['h_lift'] = c['lift']*smoother((f-4)/11.0)+0.035*kick(f-15, freq=1.5, decay=4.0)
    s['h_tilt'] = c['tilt']
    s['h_fwd'] = c['fwd']*smoother((f-8)/12.0)
    s['h_spread'] = c['spread']*smoother((f-4)/10.0)+0.02*kick(f-13, freq=1.8, decay=3.5)
    s['h_flare'] = c['flare']*smoother((f-12)/9.0)+5*kick(f-19, freq=1.8, decay=3.0)
    s['h_oz'] += 0.01*kick(f-6, freq=1.2, decay=4.0)
    # Blend into the hold loop so the last frame is exactly 'Open Circuit hold' frame 1.
    w = smoother((f-CROWN_ACTIVE)/(OC_N-CROWN_ACTIVE))
    h = hold_state(((f-OC_N) % HOLD_N)/HOLD_N)
    for i in range(1, 5):
        h[f'a{i}_lag'] = 0.0
    s = mix(s, h, w)
    apply(p, s)


# -- Open Circuit end
END_N = 22
RECALL = 3


def end_keys():
    rest = rest_state()
    recall = arms(st(spine_x=3, chest_x=3, neck_x=1, head_x=6), POSES['recall'], **both(curl=30, thumb=28, splay=2))
    relax = mix(recall, rest, 0.5)
    relax.update(both(curl=16, thumb=14))
    for side in 'LR':   # hands come down in front of and outside the thighs, not through them
        relax[f'{side}_adduct'] -= 12
        relax[f'{side}_swing'] -= 6
        relax[f'{side}_ftwist'] -= 40   # the opening arm enters the orientation gate thumb-up (handpass rolled ~50 in 2 f)
    return {9: recall, 12: recall, 17: relax, END_N: rest}


def end_pose(p, f):
    h = hold_state(((f-1) % HOLD_N)/HOLD_N)
    body_w = smoother((f-1)/8.0)
    s = mix(h, keyed(f, END_KEYS), body_w)
    # halo: tilt back upright first, then descend and close.
    c = CROWN
    tilt_k = 1.0-smoother((f-2)/9.0)
    lift_k = 1.0-smoother((f-6)/10.0)
    s['h_tilt'] = c['tilt']*tilt_k
    s['h_fwd'] = c['fwd']*(1.0-smoother((f-3)/10.0))
    s['h_lift'] = c['lift']*lift_k+(h['h_lift']-c['lift'])*(1-body_w)
    s['h_spread'] = c['spread']*(1.0-smoother((f-5)/11.0))+(h['h_spread']-c['spread'])*(1-body_w)
    s['h_flare'] = c['flare']*(1.0-smoother((f-2)/8.0))
    s['h_spin'] = h['h_spin']*(1-body_w)
    for i in range(1, 5):
        s[f'a{i}_lag'] = 0.0
        s[f'a{i}_lift'] = h[f'a{i}_lift']*(1-body_w)
        s[f'a{i}_flare'] = h[f'a{i}_flare']*(1-body_w)
    settle = ramp(f, END_N-4, END_N)
    s['h_oz'] = -0.012*kick(f-16, freq=1.6, decay=3.0)*(1-settle)
    apply(p, s)


# ----------------------------------------------------------------------------- diagnostics
def rest_error(p, caps, frame):
    return max((caps[frame][n].translation-p.rest[n].translation).length for n in p.fk)


def arm_elevation(p, caps, f, side):
    """Upper-arm raise in the chest frame (deg from hanging straight down)."""
    ch = caps[f]['chest'].to_3x3()
    ch0 = p.r3['chest']
    d = (ch0 @ ch.inverted() @ caps[f][f'{side} upperarm'].to_3x3().col[1]).normalized()
    return math.degrees(d.angle(Vector((0, 0, -1))))


def gesture_post(loop):
    def post(p, caps, frames):
        out = {'max_arm_raise_deg': round(max(arm_elevation(p, caps, f, s) for f in frames for s in 'LR'), 1)}
        if not loop:
            out['start_rest_err_m'] = round(rest_error(p, caps, frames[0]), 6)
            out['end_rest_err_m'] = round(rest_error(p, caps, frames[-1]), 6)
        return out
    return post


def _mesh_world(o, dg):
    ev = o.evaluated_get(dg)
    m = ev.to_mesh()
    pts = [o.matrix_world @ v.co for v in m.vertices]
    ev.to_mesh_clear()
    return pts


def diag_setup(p):
    """Vertex subsets at rest: head/neck/shoulder shell, chest front, arcs and hand meshes."""
    p.reset()
    p.update()
    dg = bpy.context.evaluated_depsgraph_get()
    body = bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
    pts = _mesh_world(body, dg)
    D = {'body': body,
         'upper': [i for i, q in enumerate(pts) if q.z > 1.5],
         'chest': [i for i, q in enumerate(pts) if abs(q.x) < 0.17 and 1.25 < q.z < 1.62 and q.y < 0.0],
         'pauldrons': [o for o in bpy.data.objects if 'pauldron upper' in o.name],
         'arcs': [bpy.data.objects[ARC_OBJ.format(i)] for i in range(1, 5)],
         'hands': {s: [o for o in bpy.data.objects if o.type == 'MESH' and o.parent_bone.startswith(f'{s} ') and
                       any(k in o.parent_bone for k in ('hand', 'index', 'middle', 'ring', 'little', 'thumb'))]
                   for s in 'LR'}}
    D['rest'] = diag_frame(D, dg)
    return D


def _bvh(o, dg):
    ev = o.evaluated_get(dg)
    m = ev.to_mesh()
    verts = [o.matrix_world @ v.co for v in m.vertices]
    polys = [tuple(pl.vertices) for pl in m.polygons]
    ev.to_mesh_clear()
    return BVHTree.FromPolygons(verts, polys), verts


def diag_frame(D, dg):
    """Signed arc distance to body/pauldrons (negative = inside), hand-to-chest distance."""
    body_tree, pts = _bvh(D['body'], dg)
    trees = [body_tree]+[_bvh(o, dg)[0] for o in D['pauldrons']]
    arc_pts = [q for o in D['arcs'] for q in _mesh_world(o, dg)]
    arc = 9.0
    inside = 0
    for q in arc_pts:
        best = None
        for tr in trees:
            loc, nrm, _, dist = tr.find_nearest(q, 0.3)
            if loc is None:
                continue
            sd = dist if (q-loc).dot(nrm) >= 0 else -dist
            if best is None or abs(sd) < abs(best):
                best = sd
        # The body is not closed, so a "deep inside" reading is a sign error, not penetration.
        if best is not None and best > -0.04:
            arc = min(arc, best)
            inside += best < -0.002
    ch = [pts[i] for i in D['chest']]
    t2 = kdtree.KDTree(len(ch))
    for i, q in enumerate(ch):
        t2.insert(q, i)
    t2.balance()
    hand = min(t2.find(q)[2] for s in 'LR' for o in D['hands'][s] for q in _mesh_world(o, dg))
    return {'arc_signed_m': arc, 'arc_inside': inside, 'hand_chest_m': hand}


SMOOTH_SKIP = ('index', 'middle', 'ring', 'little', 'thumb', 'muzzle', 'socket', ' IK', 'pole')


def smoothness(p, act, info):
    """Per-frame angular velocity (deg/frame) and its change (deg/frame^2) on every body/halo bone,
    in the bone's local frame; loops are treated as periodic. Fingers are skipped (tremor is intended)."""
    scene = bpy.context.scene
    first, last = info['frames']
    frames = list(range(first, last+1))
    names = [n for n in p.fk if not any(k in n for k in SMOOTH_SKIP)]
    quats = {n: [] for n in names}
    locs = {n: [] for n in names}
    p.rig.animation_data.action = act
    for f in frames:
        scene.frame_set(f)
        for n in names:
            quats[n].append(p.pb[n].rotation_quaternion.copy())
            locs[n].append(p.pb[n].matrix.translation.copy())
    if info['loop']:
        for n in names:
            quats[n] = quats[n][:-1]
            locs[n] = locs[n][:-1]
    worst_v, worst_a, worst_p = (0.0, '', 0), (0.0, '', 0), (0.0, '', 0)
    for n in names:
        q, x = quats[n], locs[n]
        m = len(q)
        rng = range(m) if info['loop'] else range(m-1)
        vel = {}
        for i in rng:
            d = q[i].rotation_difference(q[(i+1) % m])
            ang = d.angle if d.angle <= math.pi else d.angle-2*math.pi
            vel[i] = Vector(d.axis)*math.degrees(ang)
            if abs(math.degrees(ang)) > worst_v[0]:
                worst_v = (abs(math.degrees(ang)), n, frames[i])
        for i in vel:
            j = (i+1) % m if info['loop'] else i+1
            if j in vel:
                a = (vel[j]-vel[i]).length
                if a > worst_a[0]:
                    worst_a = (a, n, frames[j])
        rng2 = range(m) if info['loop'] else range(1, m-1)
        for i in rng2:
            acc = (x[(i+1) % m]-2*x[i]+x[i-1]).length*1000
            if acc > worst_p[0]:
                worst_p = (acc, n, frames[i])
    info['max_ang_vel_deg_f'] = [round(worst_v[0], 2), worst_v[1], worst_v[2]]
    info['max_ang_acc_deg_f2'] = [round(worst_a[0], 2), worst_a[1], worst_a[2]]
    info['max_pos_acc_mm_f2'] = [round(worst_p[0], 2), worst_p[1], worst_p[2]]
    print('SPECIAL smooth', info['title'], 'vel', info['max_ang_vel_deg_f'], 'acc', info['max_ang_acc_deg_f2'],
          'pos', info['max_pos_acc_mm_f2'], flush=True)


def splay_test(scale=0.32):
    """python-expr entry: which way does `splay` move each finger? Prints fingertip span (index to
    little tip) and the out-of-palm-plane part of each tip's motion, then renders close-ups."""
    import hs_anim as H
    p = H.open_start()
    out = H.ROOT/'art/anim/wip/special/_splay'
    out.mkdir(parents=True, exist_ok=True)
    def tips(side):
        p.update()
        return {d: p.pb[f'{side} {d}.3'].matrix @ Vector((0, p.pb[f'{side} {d}.3'].length, 0))
                for d in ('index', 'middle', 'ring', 'little')}
    for side in 'LR':
        p.reset()
        fingers(p, side, 10, 0, 0)
        t0 = tips(side)
        j = fk_hand(p, side)
        n = (j['index']-j['little']).cross(j['knuckle']-j['wrist']).normalized()
        fingers(p, side, 10, 0, 20)
        t1 = tips(side)
        span0 = (t0['index']-t0['little']).length
        span1 = (t1['index']-t1['little']).length
        off = {d: round(abs((t1[d]-t0[d]).dot(n))*1000, 1) for d in t0}
        mov = {d: round((t1[d]-t0[d]).length*1000, 1) for d in t0}
        print(f'SPLAY {side} span {span0*1000:.1f} -> {span1*1000:.1f} mm; move {mov}; out-of-plane {off}', flush=True)
    scene = bpy.context.scene
    H.eevee(scene, 16)
    cam = bpy.data.objects.new('splay cam', bpy.data.cameras.new('splay cam'))
    scene.collection.objects.link(cam)
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = scale
    if p.rig.animation_data:
        p.rig.animation_data.action = None   # the start file's idle would override the test pose
    for side in 'LR':
        for amount in (0, 20):
            p.reset()
            p.arm(side, swing=-40, adduct=-10, elbow=60, forearm_twist=-60)
            fingers(p, side, 10, 0, amount)
            p.update()
            j = fk_hand(p, side)
            h = p.pb[f'{side} hand'].matrix
            centre = h @ Vector((0, 0.09, 0))
            n = (j['index']-j['little']).cross(j['knuckle']-j['wrist']).normalized()
            away = centre-p.pb['chest'].matrix.translation
            away.z = 0
            if n.dot(away) < 0:
                n = -n
            W = p.rig.matrix_world
            centre = W @ centre
            n = (W.to_3x3() @ n).normalized()
            cam.location = centre+n*0.35
            cam.data.clip_start = 0.01
            cam.rotation_euler = (centre-cam.location).to_track_quat('-Z', 'Y').to_euler()
            H.render_still(scene, cam.name, out/f'{side}-splay{amount}.png', (800, 1000))
    print('SPLAY renders', out, flush=True)


def pauldron_error(p, side):
    """Played-back pauldron rotation vs pauldron_design (deg, chest frame; includes any shrug)."""
    rel = lambda n: p.pb[n].matrix.to_3x3() @ p.r3[n].inverted()
    c = rel('chest').inverted()
    e = (c @ rel(f'{side} upperarm')).to_euler('YXZ')
    want = pauldron_design(side, math.degrees(e.x), sign(side)*math.degrees(e.y)).to_quaternion()
    got = (c @ rel(f'{side} pauldron')).to_quaternion()
    return math.degrees(want.rotation_difference(got).angle) % 360


def diagnose(p, D, act, info):
    rig = p.rig
    rig.animation_data.action = act
    scene = bpy.context.scene
    worst = {'arc_signed_m': (9.0, 0), 'hand_chest_m': (9.0, 0)}
    inside = (0, 0)
    pauldron = (0.0, 0)
    for f in range(info['frames'][0], info['frames'][1]+1):
        scene.frame_set(f)
        p.update()
        for side in 'LR':
            e = pauldron_error(p, side)
            if e > pauldron[0]:
                pauldron = (e, f)
        r = diag_frame(D, bpy.context.evaluated_depsgraph_get())
        for k in worst:
            if r[k] < worst[k][0]:
                worst[k] = (r[k], f)
        if r['arc_inside'] > inside[0]:
            inside = (r['arc_inside'], f)
    rest = D['rest']
    info['arc_signed_min_mm'] = round(1000*worst['arc_signed_m'][0], 1)
    info['arc_signed_frame'] = worst['arc_signed_m'][1]
    info['arc_signed_rest_mm'] = round(1000*rest['arc_signed_m'], 1)
    info['arc_verts_inside_max'] = inside[0]
    info['arc_verts_inside_frame'] = inside[1]
    info['arc_verts_inside_rest'] = rest['arc_inside']
    info['hand_chest_min_mm'] = round(1000*worst['hand_chest_m'][0], 1)
    info['hand_chest_frame'] = worst['hand_chest_m'][1]
    info['pauldron_follow_err_deg'] = round(pauldron[0], 2)
    act['clip_json'] = json.dumps(info)
    print('SPECIAL diag', info['title'], 'arcs signed', info['arc_signed_min_mm'], 'mm @', info['arc_signed_frame'],
          f"(rest {info['arc_signed_rest_mm']}) inside {inside[0]} @ {inside[1]} (rest {rest['arc_inside']})",
          '| hands', info['hand_chest_min_mm'], 'mm @', info['hand_chest_frame'],
          '| pauldron err', info['pauldron_follow_err_deg'], 'deg @', pauldron[1], flush=True)


# ----------------------------------------------------------------------------- build
def build(p):
    global DIS_KEYS, OC_KEYS, END_KEYS
    arc_geometry()
    solve_poses(p)
    D = diag_setup(p)
    DIS_KEYS = discharge_keys()
    OC_KEYS = oc_keys()
    END_KEYS = end_keys()
    g = dict(kind='gesture', layer='upper body (mask excludes pelvis/legs)')
    specs = [
        ('Charge loop', list(range(1, CHARGE_N+2)), charge_pose(False), True,
         {'Pulse high': 1+CHARGE_N//4, 'Pulse low': 1+3*CHARGE_N//4}, {}),
        ('Charge full', list(range(1, FULL_N+2)), charge_pose(True), True,
         {'Pulse high': 1+FULL_N//4}, {'seam_anchors': []}),
        ('Discharge', list(range(1, DIS_N+1)), discharge_pose, False,
         {'Gather': GATHER, 'Release': RELEASE, 'Recovered': DIS_N}, {'socket_release': ['L muzzle', 'R muzzle', 'core socket'],
                                                              'finger_accents': [RELEASE],
                                                              'accents': [7, RELEASE]}),   # fling out of the tense, release
        ('Open Circuit', list(range(1, OC_N+1)), oc_pose, False,
         {'Unfold': UNFOLD, 'Crown active': CROWN_ACTIVE}, {'seam_to': ['Open Circuit hold', 1]}),
        ('Open Circuit hold', list(range(1, HOLD_N+2)), hold_pose, True, {'Pulse': 1}, {}),
        ('Open Circuit end', list(range(1, END_N+1)), end_pose, False,
         {'Recall': RECALL, 'Recovered': END_N}, {'seam_from': ['Open Circuit hold', 1]}),
    ]
    out = []
    for title, frames, fn, loop, markers, meta in specs:
        act, info = bake(p, title, frames, fn, loop, markers=markers, meta={**g, **meta},
                         post=gesture_post(loop), legs_ik=0.0)
        smoothness(p, act, info)
        diagnose(p, D, act, info)
        out.append((act, info))
    info0 = out[0][1]
    info0['arm_solves'] = SOLVED
    return out


def inspect_scene():
    """Debug: dump halo/head/pauldron extents (python-expr entry point)."""
    import hs_anim as H
    p = H.open_start()
    arc_geometry()
    print('SPECIAL arcs', {i: [round(v, 3) for v in GEO[i]['g']] for i in GEO}, flush=True)
