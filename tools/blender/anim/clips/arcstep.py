"""Arc Step (utility): dash start, in-dash loop, arrival. In place; the game moves the body.

Every frame is one channel dict fed to `apply`: body, arms, fingers, tabard, halo and per-leg
ground/air placement. Start and end are monotone key tracks whose first/last keys are exactly the
rest channels or `dash(0)`, so the hand-offs are exact by construction; frame 1 of the start and the
last frame of the end are the rest pose. Legs stay on IK: a grounded leg plants the ball joint
(`Poser.foot`), an airborne leg is placed from world thigh/shin angles, and `gw` blends the two.
"""
import math
from mathutils import Euler, Vector
from hs_anim import R, bake, lerp, sign, tabard_follow
import glide

PREVIEW_ORTHO = 3.4
N_S = 7     # start frames (rest -> dash)
N_L = 10    # loop frames (frame 1 repeated as N_L+1)
N_E = 16    # end frames (dash -> rest)
ARRIVE = 5
RECOVERED = 14

T_LEN, S_LEN = 0.322, 0.559
AIR_REACH = 0.985     # max airborne hip-to-ankle distance, fraction of that leg's thigh+shin
REST_SHIN_BACK = 9.4
BALL = {'L': Vector((0.412, -0.03, 0.055)), 'R': Vector((-0.444, -0.03, 0.055))}
HALO_CENTRE = Vector((-0.038, 0.15, 1.98))
DIGITS = ('index', 'middle', 'ring', 'little')
FAN = {'index': -1.0, 'middle': -0.3, 'ring': 0.45, 'little': 1.0}
SPREAD_SIGN = {'L': 1.0, 'R': 1.0}   # re-solved in build (finger roll differs per hand)
REST_POLE_SHIFT = {'L': Vector(), 'R': Vector()}   # set in build: pole shift that reproduces the rest knee
USE_REST_MATCH = False   # hs_anim rest_match shifts the pole in X only (4.4 mm rest residual here vs 0.5 mm)


def s1(t, phase=0.0, h=1):
    return math.sin(2*math.pi*h*(t-phase))


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


# ----------------------------------------------------------------------------- channels
def legs_rest(s):
    return {s+'gw': 1.0, s+'rk': 1.0, s+'gx': BALL[s].x, s+'gy': BALL[s].y, s+'gz': BALL[s].z, s+'gp': 0.0, s+'gt': 0.0,
            s+'th': 0.0, s+'sh': REST_SHIN_BACK, s+'spl': 0.0, s+'pl': 0.0, s+'at': 0.0}


REST = {'px': 0.0, 'py': 0.0, 'pz': 0.0, 'pp': 0.0, 'pr': 0.0, 'pyaw': 0.0,
        'sp': 0.0, 'spr': 0.0, 'ch': 0.0, 'chr': 0.0, 'chz': 0.0, 'nk': 0.0, 'hd': 0.0, 'hdr': 0.0, 'scap': 0.0,
        'sw': 0.0, 'asw': 0.0, 'ad': 0.0, 'aad': 0.0, 'el': 0.0, 'wr': 0.0, 'ft': 0.0,
        'cu': 21.0, 'cu2': 24.0, 'spd': 0.0, 'thb': 12.0, 'tr': 0.0,   # relaxed Idle hand, not flat

        'f1': 0.0, 'f2': 0.0, 'f3': 0.0, 'b1': 0.0, 'b2': 0.0,
        'hy': 0.0, 'hz': 0.0, 'hr': 0.0, 'hs': 0.0, 'hst': 0.0, 'hj': 0.0, 'tlag': 0.0,
        **legs_rest('L'), **legs_rest('R')}

PITCH = 50.0          # pelvis pitch in the dash (spine+chest add ~9 deg)
DASH_LEG = {'th': -63.0, 'sh': 71.0, 'spl': -0.062, 'pl': 48.0, 'at': 22.0}


def dash(t):
    """In-dash channels at loop phase t: arrow silhouette, tight trailing legs, crackling hands."""
    b = s1(t, 0.0)                    # slow surge, one per loop
    c = s1(t, 0.25)
    fl = lambda ph, h=3: s1(t, ph, h)
    out = dict(REST)
    out.update(px=0.007*c, py=0.10, pz=-0.20+0.013*b, pp=PITCH+1.8*b, pr=3.0*c, pyaw=1.2*b,
               sp=6.0-1.0*b, spr=-1.4*s1(t, 0.33), ch=3.0+0.8*s1(t, 0.1), chr=-1.4*s1(t, 0.42), chz=-1.5*b,
               nk=-30.0+0.8*b, hd=-24.0-1.2*b, hdr=-1.0*s1(t, 0.5), scap=1.5*s1(t, 0.15),
               sw=30.0+3.0*b, asw=4.0*c, ad=-6.0, aad=2.0*s1(t, 0.35), el=5.0+2.5*s1(t, 0.2), wr=24.0, ft=25.0,
               cu=-12.0, cu2=4.0, spd=16.0, thb=-14.0, tr=1.3,
               f1=5.0+1.8*fl(0.0, 2), f2=12.0+3.2*fl(0.07), f3=10.0+4.5*fl(0.15),
               b1=13.0+2.6*fl(0.03, 2), b2=6.0+5.0*fl(0.11),
               hy=0.10+0.006*b, hz=-0.03, hr=-46.0-2.5*b, hs=1.0+0.18*fl(0.05, 2), hst=1.0+0.1*b, hj=1.8, tlag=1.0)
    for s in ('L', 'R'):
        k = sign(s)
        out.update({s+'gw': 0.0, s+'rk': 0.0, s+'th': DASH_LEG['th']+2.2*k*c-1.2*b, s+'sh': DASH_LEG['sh']+2.2*k*c,
                    s+'spl': DASH_LEG['spl'], s+'pl': DASH_LEG['pl']+3.0*k*s1(t, 0.3), s+'at': DASH_LEG['at']})
    return out


def keyed(f, keys):
    """keys: {frame: partial channel dict}; channels interpolate only between frames that key them."""
    out = {}
    for ch in REST:
        pts = {fr: k[ch] for fr, k in keys.items() if ch in k}
        out[ch] = track(f, pts)
    return out


# ----------------------------------------------------------------------------- posing
def splay(p, side, curl, curl2, spread, thumb, tremor, tt):
    """Stiff, fanned fingers; `tremor` (0..1) adds a 1-2 deg periodic crackle (phase tt)."""
    k = SPREAD_SIGN[side]
    for n, digit in enumerate(DIGITS):
        jit = tremor*(1.1*s1(tt, 0.13*n+(0.3 if side == 'L' else 0.0), 3)+0.7*s1(tt, 0.21*n+0.1, 4))
        for i in range(1, 4):
            c = (curl if i == 1 else curl2)+jit*(1.0 if i == 1 else 0.6)
            z = k*(spread*FAN[digit]+0.8*jit) if i == 1 else 0.0
            p.pb[f'{side} {digit}.{i}'].rotation_quaternion = Euler((math.radians(c), 0, math.radians(z))).to_quaternion()
    jt = tremor*1.2*s1(tt, 0.4, 4)
    for i in range(1, 4):
        p.pb[f'{side} thumb.{i}'].rotation_quaternion = Euler((math.radians(thumb*(0.6 if i == 1 else 1.0)+jt), 0, 0)).to_quaternion()


def parent_delta(p, name):
    """World rotation the parent of `name` currently carries relative to its rest orientation."""
    par = p.bones[name].parent.name
    return p.pb[par].matrix.to_3x3() @ p.r3[par].inverted()


def place_ankle(p, s, ankle, pitch, toe, knee_dir):
    rw = R(x=pitch)
    m = (rw @ p.r3[f'{s} foot IK']).to_4x4()
    m.translation = ankle
    p.place(f'{s} foot IK', m)
    hip = p.world(f'{s} thigh').translation
    pm = p.rest[f'{s} knee pole'].copy()
    pm.translation = (hip+ankle)*0.5+knee_dir.normalized()*0.45
    p.place(f'{s} knee pole', pm)
    p.rot(f'{s} toe', rw.inverted() @ R(x=toe))


def air_leg(p, s, thigh, shin, splay_x, plantar, toe):
    """Airborne leg: thigh deg forward of vertical, shin deg back of vertical (world);
    splay_x < 0 pulls the ankle toward the midline."""
    k = sign(s)
    hip = p.world(f'{s} thigh').translation
    th, sh = math.radians(thigh), math.radians(shin)
    knee = hip+Vector((k*splay_x*0.37, -T_LEN*math.sin(th), -T_LEN*math.cos(th)))
    ankle = knee+Vector((k*splay_x*0.63, S_LEN*math.sin(sh), -S_LEN*math.cos(sh)))
    reach = AIR_REACH*(p.bones[f'{s} thigh'].length+p.bones[f'{s} shin'].length)
    if (ankle-hip).length > reach:            # keep a soft knee: a locked leg misses the IK target
        ankle = hip+(ankle-hip).normalized()*reach
    ax = ankle-hip
    ax.x = 0.0
    ax.normalize()
    d = Vector((0.0, ax.z, -ax.y))            # sagittal perpendicular on the knee's front side
    bend = knee-(hip+ankle)*0.5
    bend.x = 0.0
    if bend.length > 0.03 and bend.dot(d) > 0:
        d = bend.normalized()
    d += Vector((k*0.10, 0, 0))
    pitch = shin-REST_SHIN_BACK+plantar
    place_ankle(p, s, ankle, pitch, pitch+toe, d)


LEG_BONES = ('foot IK', 'knee pole', 'toe')


def leg(p, s, c):
    g = c[s+'gw']
    snaps = []
    if g > 1e-6:
        ball = (c[s+'gx'], c[s+'gy'], c[s+'gz'])
        if USE_REST_MATCH:
            p.foot(s, ball, c[s+'gp'], c[s+'gt'], rest_match=c[s+'rk'])
        else:
            p.foot(s, ball, c[s+'gp'], c[s+'gt'], pole_shift=REST_POLE_SHIFT[s]*c[s+'rk'])
        snaps.append((g, {b: (p.pb[f'{s} {b}'].location.copy(), p.pb[f'{s} {b}'].rotation_quaternion.copy()) for b in LEG_BONES}))
    if g < 1-1e-6:
        air_leg(p, s, c[s+'th'], c[s+'sh'], c[s+'spl'], c[s+'pl'], c[s+'at'])
        snaps.append((1-g, {b: (p.pb[f'{s} {b}'].location.copy(), p.pb[f'{s} {b}'].rotation_quaternion.copy()) for b in LEG_BONES}))
    if len(snaps) == 2:
        (wg, a), (_, b) = snaps
        for n in LEG_BONES:
            la, qa = a[n]
            lb, qb = b[n]
            if qa.dot(qb) < 0:
                qb = -qb
            pb = p.pb[f'{s} {n}']
            pb.location = lb.lerp(la, wg)
            pb.rotation_quaternion = qb.slerp(qa, wg)


def apply(p, c, tt=0.0):
    tr = c['tr']
    p.pelvis((c['px'], c['py'], c['pz']), R(x=c['pp'], y=c['pr'], z=c['pyaw']))
    p.rot('spine', R(x=c['sp'], y=c['spr']))
    p.rot('chest', R(x=c['ch'], y=c['chr'], z=c['chz']))
    p.rot('neck', R(x=c['nk']))
    p.rot('head', R(x=c['hd'], y=c['hdr']))
    for s in ('L', 'R'):
        k = sign(s)
        p.rot(f'{s} scapula', R(x=c['scap']))
        w = tr*(0.8*s1(tt, 0.05+0.37*(k > 0), 4)+0.5*s1(tt, 0.2, 3))
        p.arm(s, swing=c['sw']+k*c['asw'], adduct=c['ad']+k*c['aad'], elbow=c['el'],
              wrist=(c['wr']+w, 0.6*w, 0), forearm_twist=c['ft']+0.8*w)
        splay(p, s, c['cu'], c['cu2'], c['spd'], c['thb'], tr, tt)
    # Dash follow-through (weight `tlag`, 1 in the loop) keyed on the dash roll/yaw at phase tt.
    lag = tabard_follow(lambda u: 1.2*s1(u), lambda u: 3.0*s1(u, 0.25), tt, gain=1.6)
    for n, ch in (('tabard front.1', 'f1'), ('tabard front.2', 'f2'), ('tabard front.3', 'f3'),
                  ('tabard back.1', 'b1'), ('tabard back.2', 'b2')):
        r, y = lag[n]
        p.rot(n, R(x=c[ch], y=c['tlag']*r, z=c['tlag']*y))
    p.update()
    # Halo: world-space trail behind the head, partly counter-pitched against the chest.
    d = parent_delta(p, 'halo root')
    p.offset('halo root', d.inverted() @ Vector((0.0, c['hy'], c['hz'])))
    p.rot('halo root', R(x=c['hr']))
    for i in range(1, 5):
        n = f'halo {i}'
        r = p.rest[n].translation-HALO_CENTRE
        r.y = 0
        low = r.z < 0
        jit = c['hj']*0.003*s1(tt, 0.23*i, 3+(i % 2))
        p.offset(n, r.normalized()*(0.018*c['hs']+jit)+Vector((0, (0.035 if low else 0.018)*c['hst']+jit, 0)))
    for s in ('L', 'R'):
        leg(p, s, c)


# ----------------------------------------------------------------------------- clips
def loop_pose(p, f):
    t = ((f-1) % N_L)/N_L
    apply(p, dash(t), t)


def start_keys():
    d = dash(0.0)
    gl = lambda s, **kw: {s+k: v for k, v in kw.items()}
    return {
        1: dict(REST),
        3: dict(px=0.0, py=0.03, pz=-0.17, pp=17.0, sp=9.0, ch=5.0, nk=-9.0, hd=-9.0, scap=6.0,
                sw=26.0, ad=-13.0, el=26.0, wr=10.0, ft=12.0, cu=2.0, cu2=7.0, spd=6.0, thb=0.0, tr=0.0,
                f1=-8.0, f2=4.0, f3=2.0, b1=4.0, b2=2.0, hy=0.012, hz=-0.02, hr=-2.0, hs=0.1, hst=0.0,
                **gl('L', gw=1.0, rk=0.6, gy=BALL['L'].y, gz=BALL['L'].z, gp=8.0),
                **gl('R', gw=1.0, rk=0.6, gy=BALL['R'].y, gz=BALL['R'].z, gp=8.0)),
        4: dict(py=-0.05, pz=-0.10, pp=34.0, sp=9.0, ch=4.0, nk=-20.0, hd=-16.0, scap=12.0,
                sw=42.0, ad=-10.0, el=14.0, wr=24.0, ft=22.0, cu=-8.0, cu2=4.0, spd=12.0, thb=-10.0, tr=0.5,
                f1=-6.0, f2=10.0, f3=10.0, b1=14.0, b2=8.0, hy=0.07, hz=-0.035, hr=-18.0, hs=0.7, hst=0.8,
                **gl('L', gw=1.0, rk=0.0, gy=0.0, gz=0.075, gp=50.0, gt=6.0),
                **gl('R', gw=1.0, rk=0.0, gy=-0.01, gz=0.068, gp=44.0, gt=5.0)),
        5: dict(py=0.02, pz=-0.18, pp=44.0, sp=7.0, ch=3.0, nk=-27.0, hd=-22.0,
                sw=38.0, ad=-7.0, el=8.0, tr=1.0, hy=0.09, hr=-26.0, hs=1.1, hst=1.2,
                f1=0.0, f2=16.0, f3=16.0, b1=26.0, b2=18.0,
                **gl('L', gw=0.0, th=4.0, sh=80.0, spl=0.17, pl=30.0, at=8.0),
                **gl('R', gw=0.0, th=-4.0, sh=82.0, spl=0.17, pl=30.0, at=8.0)),
        6: dict(**gl('L', th=-36.0, sh=78.0, spl=0.03, pl=44.0, at=18.0),
                **gl('R', th=-42.0, sh=78.0, spl=0.03, pl=44.0, at=18.0)),
        N_S: d,
    }


def end_keys():
    d = dash(0.0)
    gl = lambda s, **kw: {s+k: v for k, v in kw.items()}
    return {
        1: d,
        2: dict(pp=44.0, pz=-0.18, py=0.09, sp=5.0, ch=2.0, nk=-25.0, hd=-19.0,
                sw=34.0, ad=-14.0, el=8.0, wr=20.0, tr=0.8, hy=0.07, hr=-28.0,
                b1=18.0, b2=10.0,
                **gl('L', gw=0.0, th=-12.0, sh=76.0, spl=0.02, pl=26.0, at=10.0),
                **gl('R', gw=0.0, th=-60.0, sh=74.0)),
        3: dict(pp=24.0, pz=-0.15, py=0.10, sp=1.0, ch=0.0, nk=-14.0, hd=-9.0,
                sw=20.0, ad=-22.0, el=14.0, wr=8.0, tr=0.5, hy=0.02, hr=-10.0,
                f1=-6.0, f2=2.0, f3=0.0, b1=16.0, b2=10.0,
                **gl('L', gw=0.0, th=32.0, sh=64.0, spl=0.16, pl=-14.0, at=-8.0),
                **gl('R', gw=0.0, th=-50.0, sh=74.0, pl=36.0)),
        4: dict(pp=6.0, pz=-0.09, py=0.12, sp=-2.0, ch=-1.0, nk=-7.0, hd=-4.0, hy=-0.01, hr=2.0,
                **gl('L', gw=1.0, rk=0.0, gx=0.37, gy=-0.01, gz=0.10, gp=30.0, gt=16.0),
                **gl('R', gw=0.0, th=-42.0, sh=73.0, pl=22.0)),
        ARRIVE: dict(px=0.02, py=0.10, pz=-0.13, pp=-6.0, pr=-3.0, pyaw=-4.0, sp=-3.0, ch=-1.0, nk=-4.0, hd=-2.0,
                     scap=4.0, sw=-2.0, asw=4.0, ad=-26.0, el=26.0, wr=-6.0, ft=10.0, cu=-4.0, cu2=6.0, spd=10.0,
                     thb=-6.0, tr=0.25, f1=-20.0, f2=-10.0, f3=-6.0, b1=10.0, b2=6.0,
                     hy=-0.03, hz=0.01, hr=10.0, hs=0.6, hst=0.2,
                     **gl('L', gw=1.0, gx=BALL['L'].x, gy=BALL['L'].y, gz=BALL['L'].z, gp=8.0, gt=0.0),
                     **gl('R', gw=0.0, rk=0.0, th=-32.0, sh=72.0, spl=0.04, pl=14.0, at=4.0)),
        6: dict(pp=2.0, **gl('R', gw=0.0, th=10.0, sh=80.0, spl=0.14, pl=0.0, at=0.0)),
        7: dict(px=0.035, py=0.07, pz=-0.21, pp=8.0, pr=-5.0, pyaw=-5.0, sp=6.0, ch=3.0, chz=5.0, nk=-12.0, hd=-8.0,
                sw=6.0, asw=5.0, ad=-36.0, el=34.0, wr=-10.0, tr=0.0, f1=-14.0, f2=8.0, f3=8.0, b1=6.0, b2=6.0,
                hy=-0.035, hz=-0.02, hr=8.0, hs=0.3, hst=0.0,
                **gl('L', gp=4.0), **gl('R', gw=1.0, gx=-0.33, gy=0.42, gz=0.14, gp=62.0, gt=28.0)),
        8: dict(pp=7.0, **gl('R', gw=1.0, gx=-0.35, gy=0.36, gz=BALL['R'].z, gp=56.0, gt=-8.0)),
        9: dict(px=0.015, py=0.05, pz=-0.14, pp=5.0, pr=-2.0, pyaw=-2.0, sp=3.0, ch=1.0, chz=2.0, nk=-5.0, hd=-3.0,
                sw=0.0, asw=2.0, ad=-24.0, el=26.0, wr=-4.0, hy=0.008, hz=0.006, hr=-2.0,
                f1=2.0, f2=-2.0, f3=-2.0, b1=-2.0, b2=-2.0, **gl('L', gp=0.0)),
        10: dict(**gl('R', gx=-0.35, gy=0.36, gz=BALL['R'].z, gp=46.0, gt=-8.0)),
        11: dict(**gl('R', gw=1.0, gx=-0.38, gy=0.25, gz=0.13, gp=32.0, gt=12.0)),
        12: dict(pz=-0.06, pp=2.5, sp=2.0, ch=0.8, nk=-1.5, hd=-1.0, sw=2.0, el=10.0,
                 **gl('R', gx=-0.43, gy=0.08, gz=0.12, gp=16.0, gt=4.0)),
        13: dict(**gl('R', gx=BALL['R'].x, gy=BALL['R'].y, gz=BALL['R'].z, gp=4.0, gt=0.0)),
        # Settle: the torso rises a touch past upright and the arms swing just past rest before stopping.
        RECOVERED: dict(px=0.0, py=0.005, pz=-0.008, pp=-0.8, pr=0.0, pyaw=0.0, sp=-0.8, ch=-0.5, nk=0.4, hd=0.8,
                        scap=0.0, sw=-1.5, asw=0.0, ad=-3.0, el=5.0, wr=0.0, ft=1.0, cu=1.0, cu2=1.0, spd=1.0, thb=0.0,
                        f1=0.0, f2=0.0, f3=0.0, b1=0.0, b2=0.0, hy=0.0, hz=0.0, hr=0.0, hs=0.0, hst=0.0,
                        **gl('R', gp=0.0, rk=0.7), **gl('L', rk=0.7)),
        N_E: dict(REST),
    }


START_KEYS = start_keys()
END_KEYS = end_keys()


def start_pose(p, f):
    apply(p, keyed(f, START_KEYS), (f-N_S)/N_L)


def end_pose(p, f):
    apply(p, keyed(f, END_KEYS), (f-1)/N_L)


# ----------------------------------------------------------------------------- halo follow-through
def loop_offsets(p):
    pts = glide.halo_points(p, loop_pose, list(range(1, N_L+1)))
    return [glide.clamp_len(q-q0) for q0, q in zip(pts, glide.spring_periodic(pts, *glide.HALO_SPRING))]


def rest_err(p, caps, f):
    return round(max((caps[f][n].translation-p.rest[n].translation).length for n in p.fk), 6)


def post_loop(p, caps, frames):
    offs = loop_offsets(p)
    return glide.apply_halo(p, caps, frames, offs+offs[:1])


def post_start(p, caps, frames):
    zero = Vector((0, 0, 0))
    preroll = glide.halo_points(p, lambda q, f: None, [1]*8)
    offs = glide.one_shot_offsets(caps, frames, preroll, zero, loop_offsets(p)[0], fade=3)
    out = glide.apply_halo(p, caps, frames, offs)
    out['start_rest_err_m'] = rest_err(p, caps, frames[0])
    return out


def post_end(p, caps, frames):
    preroll = glide.halo_points(p, loop_pose, list(range(1, N_L+1))*2)
    offs = glide.one_shot_offsets(caps, frames, preroll, loop_offsets(p)[0], Vector((0, 0, 0)), fade=4)
    out = glide.apply_halo(p, caps, frames, offs)
    out['end_rest_err_m'] = rest_err(p, caps, frames[-1])
    return out


def solve_spread(p):
    """Pick the local-Z sign per hand that fans index and little apart."""
    for s in ('L', 'R'):
        best = None
        for k in (1.0, -1.0):
            SPREAD_SIGN[s] = k
            p.reset()
            splay(p, s, 0.0, 0.0, 15.0, 0.0, 0.0, 0.0)
            p.update()
            tip = lambda d: p.pb[f'{s} {d}.3'].tail
            gap = (tip('index')-tip('little')).length
            if best is None or gap > best[0]:
                best = (gap, k)
        SPREAD_SIGN[s] = best[1]
    p.reset()
    p.update()
    print('ARCSTEP spread signs', SPREAD_SIGN, flush=True)


def solve_rest_poles(p):
    """The rest knee sits off the hip-ankle axis slightly outward, not toward the calibrated pole;
    shift the pole into that plane so a planted rest leg on IK equals the rest pose."""
    for s in ('L', 'R'):
        hip = p.rest[f'{s} thigh'].translation
        knee = p.rest[f'{s} shin'].translation
        ankle = p.rest[f'{s} foot'].translation
        axis = (ankle-hip).normalized()
        k = knee-hip
        perp = (k-axis*k.dot(axis)).normalized()
        mid = (hip+ankle)*0.5
        pole = p.foot_rest[s]['pole']
        REST_POLE_SHIFT[s] = mid+perp*(pole-mid).length-pole
    print('ARCSTEP rest pole shift', {s: tuple(round(v, 3) for v in REST_POLE_SHIFT[s]) for s in 'LR'}, flush=True)


def motion_report(p, built, top=10, skip=('IK', 'pole', 'socket', 'muzzle')):
    """Per-bone pop scan of baked actions: `rot` is the local-rotation second difference (deg the
    frame leaves the slerp midpoint of its neighbours), `pos` the world head second difference (mm)."""
    import bpy
    scene = bpy.context.scene
    out = {}
    for act, info in built:
        p.rig.animation_data.action = act
        f0, f1 = info['frames']
        frames = list(range(f0, f1+1))
        loop = info['loop']
        if loop:
            frames = frames[:-1]
        q, x = {}, {}
        for f in frames:
            scene.frame_set(f)
            p.update()
            for pb in p.pb:
                if any(k in pb.name for k in skip):
                    continue
                q.setdefault(pb.name, []).append(pb.rotation_quaternion.copy())
                x.setdefault(pb.name, []).append(pb.matrix.translation.copy())
        n = len(frames)
        hits = []
        for name in q:
            for i in range(n):
                if not loop and (i == 0 or i == n-1):
                    continue
                a, b, c = q[name][i-1], q[name][i], q[name][(i+1) % n]
                if a.dot(c) < 0:
                    c = -c
                mid = a.slerp(c, 0.5)
                d = mid.rotation_difference(b)
                rot = math.degrees(min(d.angle, 2*math.pi-d.angle))
                pa, pb_, pc = x[name][i-1], x[name][i], x[name][(i+1) % n]
                pos = ((pa+pc)*0.5-pb_).length*1000
                vel = math.degrees(a.rotation_difference(b).angle) if a.dot(b) >= 0 else math.degrees((-a).rotation_difference(b).angle)
                hits.append((rot, pos, vel, name, frames[i]))
        hits.sort(reverse=True)
        byp = sorted(hits, key=lambda h: -h[1])
        out[info['title']] = {'rot': [(round(r, 2), nm, f, round(v, 1)) for r, _, v, nm, f in hits[:top]],
                              'pos': [(round(ps, 1), nm, f) for _, ps, _, nm, f in byp[:top]]}
        print('POPS', info['title'], 'rot', out[info['title']]['rot'], flush=True)
        print('POPS', info['title'], 'pos', out[info['title']]['pos'], flush=True)
    return out


def build(p):
    solve_spread(p)
    solve_rest_poles(p)
    meta = {'kind': 'utility'}
    return [
        bake(p, 'Arc Step start', list(range(1, N_S+1)), start_pose, False,
             markers={'Dash start': N_S},
             meta={**meta, 'speed_mps': 0.0, 'seam_from': ['Arc Step end', N_E], 'seam_to': ['Arc Step loop', 1],
                   'finger_accents': [4]},
             post=post_start),
        bake(p, 'Arc Step loop', list(range(1, N_L+2)), loop_pose, True,
             meta={**meta, 'dash_distance_m': [8, 12]}, post=post_loop),
        bake(p, 'Arc Step end', list(range(1, N_E+1)), end_pose, False,
             markers={'Arrive': ARRIVE, 'Recovered': RECOVERED},
             meta={**meta, 'speed_mps': 0.0, 'seam_from': ['Arc Step loop', 1], 'seam_to': ['Arc Step start', 1]},
             post=post_end),
    ]
