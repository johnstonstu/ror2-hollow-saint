"""Presentation set: in-game idles, spawn intro and the character-select idle / intro.

All clips keep the feet planted with leg IK (zero slide) and are in place. Poses are state dicts
(pelvis, spine chain, arms, per-digit fingers, feet, tabard, halo) applied by `apply`.

- Idle (96 f loop): frame 1 is exactly air's stand (Land's last frame, Jump's first frame). Every
  periodic term is written as f(t)-f(0), so the loop leaves the stand and returns to it: two slow
  breaths through spine/chest/scapulae, one weight shift onto the left leg and back, a small glance,
  two brief finger flicks of current, the halo drifting ~2 deg and bobbing, tabard settling late.
- Idle combat (48 f loop): lower, weight forward, torso turned so the right (casting) hand leads,
  half-raised with index+middle primed; faster breath, halo opened and tipped, finger tremor.
- Spawn (72 f): low half-kneel (right foot a step back on its toes) with the halo folded tight
  (arcs pulled in, bunched upward, tipped back over the head) -> jolt (Awaken) -> the right foot
  steps up (lifted clear, no planted slide) as the body unrolls bottom-up, arcs unfold on a damped
  spring (Halo lit), the head lifts last with a small overshoot (Ready) -> exactly Idle frame 1.
- Select idle (96 f loop): contrapposto on the left leg, hands drifting low and open in a slow
  channeling gesture with rippling fingers, halo swaying about its centre (+-11 deg), slow breath.
- Select intro (44 f): starts and ends on Select idle frame 1; the right hand lifts, two-finger
  snap (Snap), the halo flares open and rings back (Halo flare), hands drift home (Settled).

Halo arcs: each arc is pushed along its ring radius (spread), spun about the ring centre in the
ring plane (spin), tipped about its own tangent (fold, + = outer edge back) and moved along the ring
normal (dy, + = back). The lower arcs rest on the chest's graphite yoke bar; keep them docked there
(no spread / halo-root tilt relative to the chest) or the bar shows as a detached strut.
"""
import math
import bpy
from mathutils import Euler, Vector
from hs_anim import R, axis_rot, bake, halo_offsets, lerp, ramp, sign, smoother
import gait
import air
from handfix import ZSIGN

from padfix import HALO_SHIFT, halo_pose
RING_C = Vector((-0.0375, 0.15, 1.957))+HALO_SHIFT
# the lower arcs (2, 3) sit just above/behind the pads: in the flared states they keep their radius and ride back
LOW_ARCS_CLEAR = dict(a2_spread=-0.02, a3_spread=-0.02, a2_dy=0.012, a3_dy=0.012)
ARC_OBJ = 'HALO | independent copper arc {}'
GEO = {}
TAU = 2*math.pi

N_IDLE = 96
N_COMBAT = 48
N_SPAWN = 72
N_SEL = 96
N_INTRO = 44
SEL_FLICK = 1+round(0.70*N_SEL)                              # Select idle's right ring/little flick open
SEL_FLICK_FRAMES = list(range(SEL_FLICK-3, SEL_FLICK+10))    # handnat curl order exempt (little opens past ring)

ARM_K = ('swing', 'adduct', 'elbow', 'twist', 'ftwist', 'wx', 'wy', 'wz', 'lift',
         'curl', 'thumb', 'idx', 'mid', 'ring', 'lit', 'splay', 'fx', 'fy', 'fz', 'pitch', 'toe', 'yaw', 'kx')
BODY_K = ('ox', 'oy', 'oz', 'px', 'py', 'pz') + tuple(f'{b}_{a}' for b in ('sp', 'ch', 'nk', 'hd') for a in 'xyz')
CLOTH_K = ('tf1', 'tf2', 'tf3', 'tfy', 'tb1', 'tb2', 'tby')
HALO_K = ('h_ox', 'h_oy', 'h_oz', 'h_rx', 'h_ry', 'h_rz', 'spin', 'spread', 'fold') + \
    tuple(f'a{i}_{k}' for i in range(1, 5) for k in ('spin', 'spread', 'fold', 'dy'))
KEYS = BODY_K+CLOTH_K+HALO_K+tuple(f'{s}_{k}' for s in 'LR' for k in ARM_K)


def st(**kw):
    d = dict.fromkeys(KEYS, 0.0)
    for k, v in kw.items():
        if k not in d:
            raise KeyError(k)
        d[k] = v
    return d


def both(**kw):
    out = {}
    for k, v in kw.items():
        out[f'L_{k}'] = v
        out[f'R_{k}'] = v
    return out


def mix(a, b, t):
    return {k: a[k]+(b[k]-a[k])*t for k in a}


def upd(s, **kw):
    s = dict(s)
    for k, v in kw.items():
        if k not in s:
            raise KeyError(k)
        s[k] = v
    return s


def addv(s, **kw):
    s = dict(s)
    for k, v in kw.items():
        s[k] += v
    return s


# ----------------------------------------------------------------------------- curves
def track(f, keys):
    """Monotone cubic (PCHIP) through {frame: value}: no overshoot beyond the keys, flat at the ends."""
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
    return {k: track(f, {fr: s[k] for fr, s in keys.items()}) for k in KEYS}


def ease_in(x, a):
    """C1 time warp: 0 before 0, quadratic start over `a` frames, then linear (no velocity jump)."""
    if x <= 0:
        return 0.0
    return x*x/(2*a) if x < a else x-a/2


def spring(x, freq=2.0, decay=4.0, onset=2.0):
    """Unit step response with a damped overshoot (x in frames after the step), eased onset."""
    x = ease_in(x, onset)
    if x <= 0:
        return 0.0
    return 1.0-math.exp(-x/decay)*math.cos(TAU*x/24*freq)


def kick(x, freq=2.0, decay=4.0, onset=1.5):
    """Impulse response: 0 -> ~1 -> damped ringing -> 0 (x in frames), eased onset."""
    x = ease_in(x, onset)
    if x <= 0:
        return 0.0
    return math.exp(-x/decay)*math.sin(TAU*x/24*freq)/0.62


def per(fn, t):
    """Periodic term shifted so it is exactly zero at t = 0 (loops that start on a fixed pose)."""
    return fn(t)-fn(0.0)


def bump(t, c, w):
    """Periodic raised-cosine bump centred at c with half-width w (loop phase units)."""
    d = (t-c+0.5) % 1.0-0.5
    return 0.5*(1+math.cos(math.pi*d/w)) if abs(d) < w else 0.0


def flick(t, c, n, tau=3.5):
    """Quick twitch peaking (1.0) at loop phase c: smooth ~tau-frame rise, few-frame decay (n = loop length)."""
    def raw(u):
        y = ((u-c+0.5) % 1.0-0.5)*n+tau
        return (y/tau)**2*math.exp(2*(1-y/tau)) if y > 0 else 0.0
    return per(raw, t)


def tremor(t, seed, harmonics):
    v = 0.0
    for j, h in enumerate(harmonics):
        v += math.sin(TAU*(h*t+0.37*seed*(j+1)+0.13*seed*seed))
    return v/len(harmonics)


# ----------------------------------------------------------------------------- apply
def arc_geometry(p):
    p.rig.animation_data.action = None
    p.reset()
    p.update()
    for i in range(1, 5):
        o = bpy.data.objects[ARC_OBJ.format(i)]
        pts = [o.matrix_world @ v.co for v in o.data.vertices]
        g = sum(pts, Vector())/len(pts)
        u = g-RING_C
        u.y = 0.0
        u.normalize()
        GEO[i] = {'g': g, 'u': u, 't': Vector((0, 1, 0)).cross(u).normalized()}


def fingers(p, side, s):
    k = sign(side)*ZSIGN
    g = lambda n: s[f'{side}_{n}']
    for digit, extra, fan, add in (('index', -0.3, -1.0, g('idx')), ('middle', 0.0, -0.3, g('mid')),
                                   ('ring', 0.25, 0.4, g('ring')), ('little', 0.5, 1.0, g('lit'))):
        for i in range(1, 4):
            amount = (g('curl')+add)*(1+extra*0.4)*(1.15 if i == 2 else 1.0)
            z = k*fan*g('splay') if i == 1 else 0.0
            p.pb[f'{side} {digit}.{i}'].rotation_quaternion = Euler((math.radians(amount), 0, math.radians(z))).to_quaternion()
    for i in range(1, 4):
        p.pb[f'{side} thumb.{i}'].rotation_quaternion = Euler((math.radians(g('thumb')), 0, 0)).to_quaternion()


def halo(p, s):
    lean = s['px']+s['sp_x']+s['ch_x']
    halo_pose(p, (s['h_ox'], 0.004+s['h_oy'], s['h_oz']), R(-0.55*lean+s['h_rx'], s['h_ry'], s['h_rz']))
    for i in range(1, 5):
        g, u, t = GEO[i]['g'], GEO[i]['u'], GEO[i]['t']
        spin = s['spin']+s[f'a{i}_spin']
        spread = s['spread']+s[f'a{i}_spread']
        fold = s['fold']+s[f'a{i}_fold']
        m = R(y=spin)
        back = Vector((0.0, s[f'a{i}_dy'], 0.0))
        def ring(x):
            return RING_C+back+m @ (x+u*spread-RING_C)
        g2 = ring(g)
        fw = axis_rot(m @ t, -fold)
        head = p.rest[f'halo {i}'].translation
        target = g2+fw @ (ring(head)-g2)
        p.rot(f'halo {i}', fw @ m)
        p.offset(f'halo {i}', target-head)


def apply(p, s):
    p.pelvis((s['ox'], s['oy'], s['oz']), R(s['px'], s['py'], s['pz']))
    for b, n in (('sp', 'spine'), ('ch', 'chest'), ('nk', 'neck'), ('hd', 'head')):
        p.rot(n, R(s[f'{b}_x'], s[f'{b}_y'], s[f'{b}_z']))
    for side in 'LR':
        k = sign(side)
        g = lambda n: s[f'{side}_{n}']
        p.arm(side, swing=g('swing'), adduct=g('adduct'), elbow=g('elbow'), twist=g('twist'),
              wrist=(g('wx'), g('wy'), g('wz')), forearm_twist=g('ftwist'))
        p.rot(f'{side} scapula', R(y=-k*g('lift')))
        fingers(p, side, s)
        p.foot(side, (g('fx'), g('fy'), gait.BALL_Z+g('fz')), g('pitch'), g('toe'), yaw=g('yaw'),
               pole_shift=(-k*g('kx'), 0, 0))
    p.rot('tabard front.1', R(-s['px']+s['tf1'], s['tfy'], 0))
    p.rot('tabard front.2', R(x=s['tf2']))
    p.rot('tabard front.3', R(x=s['tf3']))
    p.rot('tabard back.1', R(-s['px']+s['tb1'], s['tby'], 0))
    p.rot('tabard back.2', R(x=s['tb2']))
    halo(p, s)


def stand_state():
    """air.stand_pose / Land's last frame / Jump's first frame, expressed as a state."""
    s = st(oz=air.STAND_Z, px=3.0, sp_x=1.0, nk_x=-2.0, hd_x=-1.0)
    a = air.STAND_ARMS
    for side in 'LR':
        x, y = air.ball_xy(side)
        s.update({f'{side}_swing': a['swing'], f'{side}_adduct': a['adduct'], f'{side}_elbow': a['elbow'],
                  f'{side}_wx': a['wrist'], f'{side}_ftwist': air.TWIST, f'{side}_curl': a['curl'],
                  f'{side}_thumb': a['thumb'], f'{side}_fx': x, f'{side}_fy': y,
                  f'{side}_fz': air.ball_z_for_tip(0.0)-gait.BALL_Z,
                  f'{side}_yaw': sign(side)*air.STAND_YAW})
    return s


STAND = None


# ----------------------------------------------------------------------------- idle
def idle_state(t):
    s = dict(STAND)
    b = lambda ph=0.0: per(lambda u: math.sin(TAU*(2*u-ph)), t)      # breath, + inhale
    w = math.sin(TAU*t)                                                # + weight over the left leg
    wl = per(lambda u: math.sin(TAU*(u-0.07)), t)                      # weight shift, cloth lag
    s = addv(s, ox=0.026*w, py=-2.1*w, pz=1.5*w, oz=-0.009*(0.5-0.5*math.cos(2*TAU*t)))
    s = addv(s, sp_y=1.25*w, ch_y=1.0*w, hd_y=-1.0*w)
    s = addv(s, sp_x=-0.9*b(), ch_x=-2.2*b(0.02), nk_x=1.1*b(0.04), hd_x=1.3*b(0.06))
    g = bump(t, 0.6, 0.16)                                             # slow glance to its left
    s = addv(s, hd_z=7.0*g, nk_z=2.5*g, hd_y=2.2*bump(t, 0.63, 0.14))
    for side in 'LR':
        k = sign(side)
        s[f'{side}_lift'] += 2.4*b(0.05)
        s[f'{side}_adduct'] += -1.8*b(0.08)+k*1.1*w
        s[f'{side}_elbow'] += 2.4*b(0.12)
        s[f'{side}_wx'] += 2.0*b(0.18)
        s[f'{side}_curl'] += 3.0*b(0.2)
    # Current crackle: left index/middle flick open twice (fingers fan), later the right ring/little.
    s['L_idx'] += -30*flick(t, 0.30, N_IDLE)-18*flick(t, 0.335, N_IDLE)
    s['L_mid'] += -20*flick(t, 0.305, N_IDLE)-11*flick(t, 0.34, N_IDLE)
    s['L_thumb'] += -9*flick(t, 0.30, N_IDLE)
    s['L_splay'] += 7*flick(t, 0.30, N_IDLE)+4*flick(t, 0.335, N_IDLE)
    s['L_wx'] += -3*flick(t, 0.30, N_IDLE)
    s['R_ring'] += -16*flick(t, 0.80, N_IDLE)
    s['R_lit'] += -18*flick(t, 0.805, N_IDLE)
    s['R_mid'] += -8*flick(t, 0.81, N_IDLE)
    s['R_splay'] += 6*flick(t, 0.805, N_IDLE)
    s['R_wx'] += -3*flick(t, 0.80, N_IDLE)
    s = addv(s, spin=3.5*math.sin(TAU*t), h_oz=0.007*b(0.1), spread=0.004*b(0.14), h_rx=-1.1*b(0.12))
    for i in range(1, 5):
        s[f'a{i}_fold'] += 1.1*per(lambda u: math.sin(TAU*(2*u-0.12-0.08*i)), t)
    s = addv(s, tfy=1.8*wl, tby=1.5*per(lambda u: math.sin(TAU*(u-0.1)), t),
             tf1=1.0*b(0.2), tf2=0.7*b(0.28), tb1=-0.6*b(0.22))
    return s


def idle_pose(p, f):
    apply(p, idle_state(((f-1) % N_IDLE)/N_IDLE))


# ----------------------------------------------------------------------------- idle combat
def combat_base():
    s = dict(STAND)
    s = addv(s, oz=-0.075, oy=-0.012, px=6.0, pz=6.0, sp_x=5.0, sp_z=4.0, ch_x=3.0, ch_z=3.0,
             nk_x=-5.0, nk_z=-4.0, hd_x=-8.0, hd_z=-8.0, hd_y=-1.0)
    s = addv(s, L_fy=-0.07, L_fx=0.01, R_fy=0.07, R_fx=-0.01, L_yaw=-3.0, R_yaw=-6.0, R_pitch=6.0)
    s.update(R_swing=-30, R_adduct=10, R_elbow=92, R_twist=-14, R_ftwist=70, R_wx=-14, R_wz=0, R_lift=3.0,
             R_curl=62, R_idx=-56, R_mid=-50, R_ring=4, R_lit=6, R_thumb=34, R_splay=7)
    s.update(L_swing=-8, L_adduct=11, L_elbow=38, L_ftwist=48, L_wx=6, L_curl=34, L_thumb=20, L_lift=1.0)
    s = addv(s, tf1=-8.0, tf2=5.0, tf3=2.0, tb1=6.0, tb2=2.0)
    s = addv(s, spread=0.02, fold=-7.0, h_rx=-6.0, spin=3.0, h_oz=0.012, h_oy=0.006)
    s = addv(s, **LOW_ARCS_CLEAR)
    return s


def combat_state(t):
    s = dict(COMBAT)
    b = math.sin(2*TAU*t)
    bl = math.sin(TAU*(2*t-0.08))
    sw = math.sin(TAU*t)
    s = addv(s, oz=0.004*b, ox=0.006*sw, py=-0.6*sw, sp_x=-0.7*b, ch_x=-1.5*bl, nk_x=0.8*bl, hd_x=0.9*bl)
    s = addv(s, sp_y=0.5*sw, hd_z=1.2*math.sin(TAU*(t-0.2)))
    for side in 'LR':
        s[f'{side}_lift'] += 1.8*bl
        s[f'{side}_adduct'] -= 1.2*bl
        s[f'{side}_elbow'] += 2.0*math.sin(TAU*(2*t-0.15))
    s['R_idx'] += 3.0*tremor(t, 3, (6, 9, 13))
    s['R_mid'] += 3.0*tremor(t, 7, (6, 9, 13))
    s['R_thumb'] += 2.0*tremor(t, 11, (7, 10))
    s['R_wx'] += 1.2*tremor(t, 13, (5, 8, 11))
    s['R_wz'] += 1.0*tremor(t, 17, (5, 8, 11))
    s['L_curl'] += 2.0*tremor(t, 19, (5, 9))
    s = addv(s, h_oz=0.005*math.sin(TAU*(2*t-0.12)), spread=0.004*math.sin(TAU*(2*t-0.16)))
    for i in range(1, 5):
        s[f'a{i}_fold'] += 1.5*tremor(t, 23+i*3, (6, 9, 13))
    s = addv(s, tf2=0.8*math.sin(TAU*(2*t-0.25)), tfy=1.0*math.sin(TAU*(t-0.1)), tby=0.8*math.sin(TAU*(t-0.13)))
    return s


def combat_pose(p, f):
    apply(p, combat_state(((f-1) % N_COMBAT)/N_COMBAT))


# ----------------------------------------------------------------------------- spawn
AWAKEN, HALO_LIT, READY = 13, 38, 60
R_BACK = 0.20


def crouch_state():
    """Low half-kneel: right foot a step back on its toes, torso folded over the left knee."""
    s = dict(STAND)
    s = addv(s, oz=-0.36, oy=0.08, ox=0.02, px=20.0, py=-3.0, pz=4.0, sp_x=20.0, ch_x=14.0,
             nk_x=11.0, hd_x=17.0, hd_y=3.0, hd_z=4.0)
    s = addv(s, L_pitch=12.0, R_fy=R_BACK, R_pitch=48.0, R_toe=-4.0, R_kx=0.3)
    s.update(both(swing=-16, adduct=12, elbow=58, wx=12, ftwist=60, curl=56, thumb=30, lift=-3.0))
    s = addv(s, L_swing=-8, L_elbow=12, L_adduct=-6, R_adduct=-8, R_elbow=10)
    s = addv(s, tf1=-80.0, tf2=58.0, tf3=36.0, tb1=30.0, tb2=-4.0)
    return s


# Folded halo: the ring stays locked to the chest (lower arcs docked on the yoke bar); the upper
# arcs swing down along the ring and close its bottom gap over the bar. On the way back up they
# pass behind the side arcs (TRAVEL_DY at mid-travel) instead of through them.
FOLD = {1: dict(spin=98.0, dy=0.015, fold=0.0, spread=0.0),
        2: dict(spin=0.0, dy=0.0, fold=0.0, spread=0.0),
        3: dict(spin=0.0, dy=0.0, fold=0.0, spread=0.0),
        4: dict(spin=-98.0, dy=0.015, fold=0.0, spread=0.0)}
TRAVEL_DY = 0.085
ARC_START = {1: HALO_LIT, 2: HALO_LIT-3, 3: HALO_LIT-3, 4: HALO_LIT+2}
HALO_GAIN = 0.10


STEP = {'R_fz': {1: 0, 18: 0, 21: 0.07, 26: 0.08, 29: 0},
        'R_fy': {1: 0, 20: 0, 28: 1.0},
        'R_pitch': {1: 0, 17: 0, 22: 0.45, 27: 0.8, 30: 0.95, 34: 1.0},
        'R_toe': {1: 0, 20: 0, 30: 1.0}}

GROUPS = {
    'pelvis': ('ox', 'oy', 'oz', 'px', 'py', 'pz') +
              tuple(f'{s}_{k}' for s in 'LR' for k in ('fx', 'fy', 'fz', 'pitch', 'toe', 'yaw', 'kx')
                    if f'{s}_{k}' not in STEP),
    'spine': ('sp_x', 'sp_y', 'sp_z'),
    'chest': ('ch_x', 'ch_y', 'ch_z'),
    'neck': ('nk_x', 'nk_y', 'nk_z'),
    'head': ('hd_x', 'hd_y', 'hd_z'),
    'arms': tuple(f'{s}_{k}' for s in 'LR' for k in ('swing', 'adduct', 'elbow', 'twist', 'ftwist', 'wx', 'wy', 'wz', 'lift')),
    'hands': tuple(f'{s}_{k}' for s in 'LR' for k in ('curl', 'thumb', 'idx', 'mid', 'ring', 'lit', 'splay')),
    'cloth': CLOTH_K,
    'halo': ('h_ox', 'h_oy', 'h_oz', 'h_rx', 'h_ry', 'h_rz'),
}
SPAWN_U = {
    'pelvis': {1: 0, 15: 0, 22: 0.2, 33: 0.86, 40: 1.0},
    'spine': {1: 0, 16: 0, 35: 0.88, 42: 1.0},
    'chest': {1: 0, 19: 0, 39: 0.88, 46: 1.0},
    'neck': {1: 0, 25: 0, 45: 0.85, 52: 1.0},
    'head': {1: 0, 29: 0, 49: 0.8, 55: 1.13, 63: 1.0},
    'arms': {1: 0, 16: 0, 30: 0.5, 43: 1.0},
    'hands': {1: 0, 19: 0, 42: 1.0},
    'cloth': {1: 0, 18: 0, 37: 0.8, 45: 1.07, 53: 1.0},
    'halo': {1: 0, 26: 0, 40: 1.04, 48: 1.0},
}
def spawn_state(f):
    s = dict(STAND)
    c = CROUCH
    for grp, names in GROUPS.items():
        u = track(f, SPAWN_U[grp])
        for n in names:
            s[n] = lerp(c[n], STAND[n], u)
    # Right foot steps up from behind: lift clear first, travel, then set down (no planted slide).
    s['R_fz'] = STAND['R_fz']+track(f, STEP['R_fz'])
    for n in ('R_fy', 'R_pitch', 'R_toe'):
        s[n] = lerp(c[n], STAND[n], track(f, STEP[n]))
    end = 1.0-ramp(f, 58, N_SPAWN)
    # Arcs: per-arc spring from folded to open with a small overshoot, forced exact by the end.
    for i in range(1, 5):
        u = 1.0+(spring(f-ARC_START[i], freq=1.5, decay=4.5, onset=5.0)-1.0)*end if f > ARC_START[i] else 0.0
        for k, v in FOLD[i].items():
            s[f'a{i}_{k}'] = v*(1.0-u)
        if FOLD[i]['spin']:
            s[f'a{i}_dy'] += TRAVEL_DY*math.sin(math.pi*min(max(u, 0.0), 1.0))
    # Halo root: cancel the lean follow-through while folded so the ring rides the chest.
    u_h = track(f, SPAWN_U['halo'])
    s['h_rx'] = 0.55*(s['px']+s['sp_x']+s['ch_x'])*(1.0-u_h)
    s['spin'] += 5.0*kick(f-HALO_LIT, freq=1.2, decay=6.0)*end
    # Awaken: a jolt through the chest/head and a flick of the fingers; dormant stir before it.
    j = kick(f-AWAKEN, freq=3.0, decay=2.5)
    stir = (1-ramp(f, AWAKEN, AWAKEN+4))*math.sin(TAU*f/24*0.5)
    s = addv(s, ch_x=-4.0*j+0.6*stir, sp_x=-1.5*j, hd_x=-3.0*j, nk_x=-1.0*j, oz=0.006*j)
    for side in 'LR':
        s[f'{side}_curl'] += -20*kick(f-AWAKEN-(0 if side == 'L' else 1), freq=2.0, decay=3.5, onset=4.0)
        s[f'{side}_thumb'] += -9*kick(f-AWAKEN, freq=2.0, decay=3.5, onset=4.0)
        # arms float a little out and forward as the body rises
        fl = math.sin(math.pi*ramp(f, 18, 48))
        s[f'{side}_adduct'] += -7.0*fl
        s[f'{side}_swing'] += -4.0*fl
        s[f'{side}_ftwist'] += -12.0*fl
        s[f'{side}_idx'] += -10*fl
        s[f'{side}_splay'] += 6*fl
    for i in range(1, 5):
        s[f'a{i}_fold'] += 2.5*tremor(f/24.0, 5+i, (7, 11))*(1-ramp(f, AWAKEN, AWAKEN+10))*ramp(f, AWAKEN-2, AWAKEN+1)
    return s


def spawn_pose(p, f):
    apply(p, spawn_state(f))


# ----------------------------------------------------------------------------- select idle
def select_state(t):
    s = dict(STAND)
    b = math.sin(TAU*2*t)
    bl = lambda ph: math.sin(TAU*(2*t-ph))
    d = math.sin(TAU*t)
    dc = math.cos(TAU*t)
    s = addv(s, ox=0.016, py=-1.6, pz=2.5, oz=-0.004, sp_y=1.0, ch_y=0.8, hd_y=-2.5, hd_x=1.0, nk_x=-0.5)
    s = addv(s, sp_x=-0.5*b, ch_x=-1.2*bl(0.03), nk_x=0.6*bl(0.05), hd_x=0.6*bl(0.07)+0.8*d, hd_z=1.5*d)
    s = addv(s, ox=0.004*d, pz=0.8*dc, ch_z=-0.8*dc)
    # Buoyant rise on the breath (never above the stand height), slow head tilt, chest counter-roll.
    s = addv(s, oz=-0.005+0.005*bl(0.02), hd_y=2.5*math.sin(TAU*(t-0.3)), hd_z=1.0*math.sin(TAU*(2*t-0.2)),
             ch_y=-0.8*math.sin(TAU*(t-0.25)), nk_y=0.8*math.sin(TAU*(t-0.28)))
    # Channeling hands: left low and open, palm turned in, right a little lower; slow counter-phase drift.
    s.update(L_swing=-22+5*d, L_adduct=-2-4*dc, L_elbow=62+9*d, L_twist=-15, L_ftwist=-35,
             L_wx=-14+8*math.sin(TAU*(t-0.12)), L_wz=6, L_curl=14, L_thumb=8, L_splay=10)
    s.update(R_swing=-14-5*d, R_adduct=2+3*dc, R_elbow=52-8*d, R_twist=-15, R_ftwist=-30,
             R_wx=-10-7*math.sin(TAU*(t-0.12)), R_wz=5, R_curl=18, R_thumb=10, R_splay=8)
    for side in 'LR':
        k = sign(side)
        ph = 0.0 if side == 'L' else 0.5
        s[f'{side}_lift'] += 1.4*bl(0.05)
        for n, digit in enumerate(('idx', 'mid', 'ring', 'lit')):
            s[f'{side}_{digit}'] += 9*math.sin(TAU*(2*t-ph-0.07*n))
        s[f'{side}_thumb'] += 4*math.sin(TAU*(2*t-ph+0.05))
        s[f'{side}_splay'] += 3*math.sin(TAU*(t-ph))
        s[f'{side}_wy'] += 2.0*math.sin(TAU*(2*t-ph-0.15))
    # Current crackle: brief finger fans, left then right.
    s['L_idx'] += -22*flick(t, 0.22, N_SEL)-12*flick(t, 0.255, N_SEL)
    s['L_mid'] += -14*flick(t, 0.225, N_SEL)
    s['L_splay'] += 6*flick(t, 0.22, N_SEL)
    s['L_wx'] += -3*flick(t, 0.22, N_SEL)
    s['R_ring'] += -18*flick(t, 0.70, N_SEL)
    s['R_lit'] += -22*flick(t, 0.705, N_SEL)
    s['R_splay'] += 5*flick(t, 0.705, N_SEL)
    s['R_wx'] += -3*flick(t, 0.70, N_SEL)
    for i in range(1, 5):
        s[f'a{i}_fold'] += 3.0*flick(t, 0.23+0.47*(i > 2)+0.01*i, N_SEL)
    s = addv(s, spin=11.0*d, h_oz=0.006*bl(0.1)+0.004, spread=0.008+0.004*bl(0.14), h_rx=-2.0+1.0*dc, fold=-2.0)
    for i in range(1, 5):
        s[f'a{i}_spread'] += 0.006*math.sin(TAU*(t-(i-1)/4))
        s[f'a{i}_fold'] += 2.0*math.sin(TAU*(t-(i-1)/4)+0.9)
    s = addv(s, tfy=1.2*math.sin(TAU*(t-0.08)), tby=1.0*math.sin(TAU*(t-0.1)), tf1=0.6*bl(0.2), tf2=0.5*bl(0.28))
    return s


def select_pose(p, f):
    apply(p, select_state(((f-1) % N_SEL)/N_SEL))


# ----------------------------------------------------------------------------- select intro
SNAP, FLARE = 13, 15


def intro_keys():
    s0 = SEL0
    prep = upd(s0, R_swing=-38, R_adduct=4, R_elbow=112, R_twist=-18, R_ftwist=80, R_wx=-18, R_wz=0,
               R_curl=34, R_idx=8, R_mid=8, R_ring=30, R_lit=34, R_thumb=46, R_splay=0, R_lift=2.5)
    prep = addv(prep, ch_x=2.0, sp_x=1.0, hd_x=4.0, hd_z=-6.0, ch_z=-3.0, spread=-0.016, fold=4.0,
                L_elbow=-10, L_swing=6, L_curl=10)
    cock = addv(prep, R_wx=-8, R_elbow=6, ch_x=0.8)
    snap = upd(prep, R_swing=-46, R_elbow=96, R_wx=14, R_idx=-40, R_mid=-36, R_thumb=12, R_ring=48, R_lit=52,
               R_splay=8)
    snap = addv(snap, ch_x=-5.0, sp_x=-2.0, hd_x=-8.0, hd_z=2.0, spread=0.016, fold=-4.0)
    hold = addv(snap, R_wx=-6, R_elbow=4, hd_x=2.0, ch_x=1.0)
    down = mix(hold, s0, 0.55)
    return {1: s0, 7: mix(s0, prep, 0.8), 10: cock, SNAP: snap, SNAP+4: hold, 28: down, N_INTRO: s0}


def intro_state(f):
    s = keyed(f, INTRO_KEYS)
    end = 1.0-ramp(f, 34, N_INTRO)
    x = f-SNAP
    fl = kick(x+0.5, freq=1.5, decay=5.0)*end
    s = addv(s, spread=0.09*fl, fold=-22*fl, h_oz=0.022*fl, h_oy=-0.01*fl, h_rx=-6*fl,
             spin=12*kick(x, freq=0.9, decay=7.0)*end)
    lf = max(fl, 0.0)
    s = addv(s, a2_spread=-0.07*lf, a3_spread=-0.07*lf, a2_dy=0.02*lf, a3_dy=0.02*lf)
    for i in range(1, 5):
        s[f'a{i}_fold'] += 4*kick(x-0.7*i, freq=2.4, decay=3.5)*end*(1 if i % 2 else -1)
        s[f'a{i}_spread'] += 0.012*kick(x-0.6*i, freq=2.0, decay=3.5)*end
    return s


def intro_pose(p, f):
    apply(p, intro_state(f))


# ----------------------------------------------------------------------------- halo lag / checks
def loop_post(p, caps, frames):
    """Loop halo lag with the frame-1 offset removed, so frame 1 carries no lag (seams stay exact)."""
    pts = [caps[f]['halo root'].translation.copy() for f in frames]
    offs = halo_offsets(pts, True)
    o0 = offs[0].copy()
    out = air.apply_halo(p, caps, frames, [o-o0 for o in offs])
    out.update(checks(p, caps, frames, True))
    return out


HALO_SOFT_CAP = 0.9*0.035*math.tanh(0.9)   # hs_anim.halo_lag counts frames above this as capped


def one_shot_post(start_fn, gain=0.5, fade=4):
    """Spring lag with a held pre-roll, faded to zero at both ends (neighbouring clips carry none)."""
    def post(p, caps, frames):
        pre = air.halo_points(p, lambda q, f: start_fn(q, frames[0]), [frames[0]]*12)
        pts = [caps[f]['halo root'].translation.copy() for f in frames]
        offs = halo_offsets(pre+pts, False, gain=gain)[len(pre):]
        n = len(frames)
        offs = [o*(ramp(i, 0, fade-1)*(1-ramp(i, n-fade, n-1))) for i, o in enumerate(offs)]
        out = air.apply_halo(p, caps, frames, offs)
        out['halo_capped_frames'] = sum(1 for o in offs if o.length > HALO_SOFT_CAP)
        out.update(checks(p, caps, frames, False))
        return out
    return post


def arm_raise(p, caps, f, side):
    """Upper-arm raise in the chest frame (deg from hanging straight down)."""
    ch = caps[f]['chest'].to_3x3()
    d = (p.r3['chest'] @ ch.inverted() @ caps[f][f'{side} upperarm'].to_3x3().col[1]).normalized()
    return math.degrees(d.angle(Vector((0, 0, -1))))


def pops(p, caps, frames, loop):
    """Angular acceleration of every bone's local rotation (deg/frame^2), wrap included for loops.
    A pop shows as a single-frame spike; returns the worst body bones and the worst finger."""
    seq = frames[:-1] if loop else frames
    n = len(seq)
    rows = []
    for name in p.fk:
        par = p.bones[name].parent
        if par is None:
            continue
        qs = [(caps[f][par.name].inverted() @ caps[f][name]).to_quaternion() for f in seq]
        vel = []
        for i in range(n if loop else n-1):
            d = qs[i].rotation_difference(qs[(i+1) % n])
            if d.w < 0:
                d.negate()
            ax, ang = d.to_axis_angle()
            vel.append(ax*math.degrees(ang))
        for i in range(len(vel)) if loop else range(1, len(vel)):
            rows.append(((vel[i]-vel[i-1]).length, name, seq[i]))
    finger = lambda nm: any(k in nm for k in ('index', 'middle', 'ring', 'little', 'thumb', 'muzzle'))
    body = sorted((r for r in rows if not finger(r[1])), reverse=True)[:3]
    fing = max((r for r in rows if finger(r[1])), default=(0.0, '', 0))
    fmt = lambda r: [round(r[0], 2), r[1], r[2]]
    return {'pop_body_deg_f2': [fmt(r) for r in body], 'pop_finger_deg_f2': fmt(fing)}


def checks(p, caps, frames, loop):
    raise_deg = max(arm_raise(p, caps, f, s) for f in frames for s in 'LR')
    # knee flare: lateral knee offset from the hip-ankle line (+ = outward)
    flare = 0.0
    for f in frames:
        for s in 'LR':
            hip, knee, ankle = (caps[f][f'{s} {b}'].translation for b in ('thigh', 'shin', 'foot'))
            mid = hip.lerp(ankle, 0.5)
            flare = max(flare, sign(s)*(knee.x-mid.x))
    return {'max_arm_raise_deg': round(raise_deg, 1), 'max_knee_out_m': round(flare, 4), **pops(p, caps, frames, loop)}


# ----------------------------------------------------------------------------- build
def build(p):
    global STAND, COMBAT, CROUCH, SEL0, INTRO_KEYS
    arc_geometry(p)
    STAND = stand_state()
    COMBAT = combat_base()
    CROUCH = crouch_state()
    SEL0 = select_state(0.0)
    INTRO_KEYS = intro_keys()
    idle = dict(kind='idle', speed_mps=0.0)
    pres = dict(kind='presentation', speed_mps=0.0)
    return [
        bake(p, 'Idle', list(range(1, N_IDLE+2)), idle_pose, True,
             markers={'Inhale': 1, 'Twitch L': 30, 'Glance': 58, 'Twitch R': 78},
             meta={**idle, 'seam_from': ['Land', air.N_LAND], 'seam_to': ['Jump', 1]}, post=loop_post),
        bake(p, 'Idle combat', list(range(1, N_COMBAT+2)), combat_pose, True,
             markers={'Inhale': 1}, meta={**idle, 'hand': 'R'}, post=loop_post),
        bake(p, 'Spawn', list(range(1, N_SPAWN+1)), spawn_pose, False,
             markers={'Awaken': AWAKEN, 'Halo lit': HALO_LIT, 'Ready': READY},
             meta={**pres, 'seam_to': ['Idle', 1]}, post=one_shot_post(spawn_pose, gain=HALO_GAIN)),
        bake(p, 'Select idle', list(range(1, N_SEL+2)), select_pose, True,
             markers={'Inhale': 1}, meta={**pres, 'curl_exempt': SEL_FLICK_FRAMES}, post=loop_post),
        bake(p, 'Select intro', list(range(1, N_INTRO+1)), intro_pose, False,
             markers={'Snap': SNAP, 'Halo flare': FLARE, 'Settled': N_INTRO},
             meta={**pres, 'seam_from': ['Select idle', 1], 'seam_to': ['Select idle', 1], 'finger_accents': [10, SNAP]},
             post=one_shot_post(intro_pose, gain=0.35)),
    ]
