"""Air set: jump (takeoff), ascend loop, descend loop, land.

All four are in place: the game moves the body vertically, so the pelvis stays near its standing
height and the jump reads through the legs, tabard and halo. Legs stay on IK throughout; airborne
legs are placed from sagittal thigh/shin angles (`leg_angles`) or by the ball joint (`Poser.foot`).
Jump and Land are per-part blends between key poses so their ends match the loops/stand exactly.
"""
import math
from mathutils import Matrix, Vector
from hs_anim import R, bake, lerp, ramp, sign, wave, apply_world_delta, axis_rot, halo_lag, halo_offsets, tabard_follow
import gait

N_JUMP = 11
N_ASC = 20
N_DESC = 20
N_LAND = 15

FOOT_MID = -0.016
WIDTH = 0.26
STAND_Y = {'L': -0.06, 'R': 0.02}
STAND_YAW = 6.0
STAND_Z = -0.035
T_LEN, S_LEN = 0.322, 0.559
REST_SHIN_BACK = 9.4
TWIST = 40.0
TOE_TIP = Vector((0.0, -0.166, -0.035))   # ball -> claw tip at rest (the mesh reaches ~1.7 cm lower)
TIP_Z = 0.021                             # claw tip bone height that sets the mesh on the ground


def s1(t, phase=0.0, h=1):
    return math.sin(2*math.pi*h*(t-phase))


def track(f, keys):
    """Smooth curve through {frame: value} keys (Catmull-Rom tangents, flat at the ends)."""
    ks = sorted(keys.items())
    if f <= ks[0][0]:
        return ks[0][1]
    if f >= ks[-1][0]:
        return ks[-1][1]
    i = max(j for j in range(len(ks)-1) if ks[j][0] <= f)
    (f0, v0), (f1, v1) = ks[i], ks[i+1]
    h = f1-f0

    def slope(j):
        if j == 0 or j == len(ks)-1:
            return 0.0
        return (ks[j+1][1]-ks[j-1][1])/(ks[j+1][0]-ks[j-1][0])
    m0, m1 = slope(i)*h, slope(i+1)*h
    u = (f-f0)/h
    u2, u3 = u*u, u*u*u
    return (2*u3-3*u2+1)*v0+(u3-2*u2+u)*m0+(-2*u3+3*u2)*v1+(u3-u2)*m1


def ball_xy(s):
    return FOOT_MID+sign(s)*WIDTH, STAND_Y[s]


def ball_z_for_tip(toe, clearance=0.0):
    """Ball height that puts the claw tip on the ground (plus clearance) for a world toe angle."""
    return max(gait.BALL_Z, TIP_Z+clearance-(R(x=toe) @ TOE_TIP).z)


# ----------------------------------------------------------------------------- part helpers
def place_ankle(p, s, ankle, pitch, toe, knee_dir):
    """Foot IK at `ankle` with world pitch; pole in front of the knee along knee_dir."""
    rw = R(x=pitch)
    m = (rw @ p.r3[f'{s} foot IK']).to_4x4()
    m.translation = ankle
    p.place(f'{s} foot IK', m)
    hip = p.world(f'{s} thigh').translation
    pm = p.rest[f'{s} knee pole'].copy()
    pm.translation = (hip+ankle)*0.5+knee_dir.normalized()*0.45
    p.place(f'{s} knee pole', pm)
    p.rot(f'{s} toe', rw.inverted() @ R(x=toe))


def leg_angles(p, s, thigh, shin, splay=0.07, plantar=40.0, toe=15.0):
    """Airborne leg from world angles: thigh deg forward of vertical, shin deg back of vertical.
    plantar points the foot beyond the shin line; toe curls the claws past the foot line."""
    k = sign(s)
    hip = p.world(f'{s} thigh').translation
    th, sh = math.radians(thigh), math.radians(shin)
    knee = hip+Vector((k*splay*0.37, -T_LEN*math.sin(th), -T_LEN*math.cos(th)))
    ankle = knee+Vector((k*splay*0.63, S_LEN*math.sin(sh), -S_LEN*math.cos(sh)))
    d = knee-(hip+ankle)*0.5
    d.x = 0.0
    if d.length < 0.03:
        d = Vector((0, -1, 0))
    d = d.normalized()+Vector((k*0.12, 0, 0))
    pitch = shin-REST_SHIN_BACK+plantar
    place_ankle(p, s, ankle, pitch, pitch+toe, d)


def tabard(p, lean, f1, f2, f3, b1, b2, lag=None):
    """Tabard swings relative to vertical: `lean` (pelvis world pitch) is undone on the top links.
    lag: optional hs_anim.tabard_follow result ({bone: (roll, yaw)}) added on top."""
    lag = lag or {}
    for n, x in (('tabard front.1', -lean+f1), ('tabard front.2', f2), ('tabard front.3', f3),
                 ('tabard back.1', -lean+b1), ('tabard back.2', b2)):
        r, y = lag.get(n, (0.0, 0.0))
        p.rot(n, R(x=x, y=r, z=y))


def halo(p, lean, lift, keep=0.55):
    """Halo floats: lifts/drops by `lift` and only partly follows the torso's forward lean."""
    p.offset('halo root', (0, 0.004, lift))
    p.rot('halo root', R(x=-keep*lean))


def arms(p, a):
    for s in ('L', 'R'):
        k = sign(s)
        p.arm(s, swing=a['swing']+k*a.get('sway', 0.0), adduct=a['adduct'], elbow=a['elbow'],
              forearm_twist=a.get('twist', TWIST), wrist=(a.get('wrist', 0.0), 0, 0))
        p.curl(s, a.get('curl', 24), a.get('thumb', 12))


def mix(a, b, t):
    return {k: lerp(a.get(k, 0.0), b.get(k, 0.0), t) for k in set(a) | set(b)}


# ----------------------------------------------------------------------------- key poses
# adduct 9 hangs the hands beside the thighs (~25-40 mm clear, contact.py); the old 22 sank the claws ~4 cm into them.
STAND_ARMS = dict(swing=4, adduct=9, elbow=18, wrist=4, twist=TWIST, curl=21, thumb=12)


def stand_pose(p):
    """Neutral stand near rest; Jump starts here and Land ends here."""
    pp = 3.0
    p.pelvis((0, 0, STAND_Z), R(x=pp))
    p.rot('spine', R(x=1.0))
    p.rot('neck', R(x=-2.0))
    p.rot('head', R(x=-1.0))
    for s in ('L', 'R'):
        x, y = ball_xy(s)
        p.foot(s, (x, y, ball_z_for_tip(0.0)), 0, 0, yaw=sign(s)*STAND_YAW)   # as Jump f1 / Land end
    arms(p, STAND_ARMS)
    tabard(p, pp, 0, 0, 0, 0, 0)


def ascend_arms(t):
    return dict(swing=1+3.0*wave(t, 0.15), sway=2.5*s1(t, 0.1), adduct=0-4.0*wave(t, 0.2),
                elbow=14+5*wave(t, 0.12), wrist=-6+5*wave(t, 0.27), twist=30, curl=20+4*wave(t, 0.3), thumb=10)


def ascend_pose(p, t):
    """Rising: left knee up, right leg swept back, chest lifted, arms low and a little out."""
    b = wave(t)
    sw = s1(t, 0.1)
    pp = 2.0+1.5*b
    p.pelvis((0.009*sw, 0.0, 0.042+0.012*b), R(x=pp, y=2.0*sw, z=3.0*s1(t, 0.25)))
    p.rot('spine', R(x=-3.0-1.2*b, y=-1.0*s1(t, 0.16), z=-1.2*s1(t, 0.3)))
    p.rot('chest', R(x=-4.0-1.0*wave(t, 0.06), y=-0.8*s1(t, 0.22), z=-1.0*s1(t, 0.36)))
    p.rot('neck', R(x=2.0, y=-0.5*sw))
    p.rot('head', R(x=-2.0+1.3*wave(t, 0.1), y=-0.8*s1(t, 0.3)))
    halo(p, 0.0, -0.018+0.006*wave(t, 0.08))
    arms(p, ascend_arms(t))
    fl = lambda ph: wave(t, ph, 2)
    lag = tabard_follow(lambda u: 3.0*s1(u, 0.25), lambda u: 2.0*s1(u, 0.1), t, gain=2.0)
    tabard(p, pp, -24+3*fl(0.0), 20+4.5*fl(0.05), 4+6*fl(0.1), 2+3*fl(0.02), -2+4.5*fl(0.08), lag)
    p.update()
    leg_angles(p, 'L', 54+5*wave(t, 0.05), 40+4*wave(t, 0.12), splay=0.06+0.01*s1(t, 0.2), plantar=40+4*wave(t, 0.2), toe=14)
    leg_angles(p, 'R', -2-4*wave(t, 0.3), 56-3*wave(t, 0.38), splay=0.07, plantar=46+4*wave(t, 0.45), toe=18)


DESC_FEET = {'L': dict(z=0.14, pitch=60, toe=22, ph=0.1), 'R': dict(z=0.30, pitch=80, toe=34, ph=0.35)}


def descend_arms(t):
    return dict(swing=-6+2.0*wave(t, 0.2), sway=1.0*s1(t, 0.1), adduct=2-3*wave(t, 0.25),
                elbow=20+3*wave(t, 0.15), wrist=-10+3*wave(t, 0.3), twist=32, curl=18, thumb=10)


def descend_pose(p, t):
    """Falling: left leg reaching down, right leg bent behind, arms slightly out, cloth/halo float up."""
    b = wave(t)
    sw = s1(t, 0.1)
    pp = 2.0+0.8*b
    p.pelvis((0.006*sw, 0.0, 0.004+0.008*b), R(x=pp, y=1.0*sw, z=-1.5*s1(t, 0.25)))
    p.rot('spine', R(x=1.0+0.6*b, y=-0.5*sw))
    p.rot('chest', R(x=0.5))
    p.rot('neck', R(x=1.0))
    p.rot('head', R(x=5.0-0.8*b))
    halo(p, 0.0, 0.022+0.004*wave(t, 0.3))
    arms(p, descend_arms(t))
    fl = lambda ph: wave(t, ph, 2)
    lag = tabard_follow(lambda u: -1.5*s1(u, 0.25), lambda u: 1.0*s1(u, 0.1), t, gain=2.5)
    tabard(p, pp, -16+4*fl(0.0), -10+5*fl(0.06), -6+6*fl(0.12), 18+4*fl(0.03), 10+6*fl(0.1), lag)
    for s in ('L', 'R'):
        d = DESC_FEET[s]
        x, y = ball_xy(s)
        p.foot(s, (x, y, d['z']+0.012*wave(t, d['ph'])), d['pitch']+3*wave(t, d['ph']+0.1), d['toe'],
               yaw=sign(s)*STAND_YAW)


# ----------------------------------------------------------------------------- blending
def snap(p):
    return {b.name: (b.location.copy(), b.rotation_quaternion.copy()) for b in p.pb}


def group(name):
    if name.startswith('tabard'):
        return 'tabard'
    if name.startswith('halo'):
        return 'halo'
    for s in ('L', 'R'):
        if name.startswith(s+' '):
            if any(k in name for k in ('thigh', 'shin', 'foot', 'toe', 'knee', 'heel')):
                return 'legs'
            return 'arms'
    return 'body'


def apply_blend(p, a, b, w):
    for pb in p.pb:
        t = w[group(pb.name)]
        la, qa = a[pb.name]
        lb, qb = b[pb.name]
        if qa.dot(qb) < 0:
            qb = -qb
        pb.location = la.lerp(lb, t)
        pb.rotation_quaternion = qa.slerp(qb, t)


def snap_of(p, fn, *args):
    p.reset()
    fn(p, *args)
    return snap(p)


# ----------------------------------------------------------------------------- jump
def jump_ground(p, f):
    """Crouch (1-3) and push-off (4-6) on the balls; the body holds after 6, the feet keep rising."""
    ff = f
    f = min(f, 6)
    pp = track(f, {1: 3, 3: 15, 4: 8, 5: -2, 6: -2})
    sp = track(f, {1: 1, 3: 7, 5: -4, 6: -4})
    ch = track(f, {1: 0, 3: 4, 5: -3, 6: -3})
    p.pelvis((0, track(f, {1: 0, 3: 0.065, 5: -0.005, 6: -0.005}),
              track(f, {1: STAND_Z, 2: -0.15, 3: -0.21, 4: -0.10, 5: 0.03, 6: 0.035})), R(x=pp))
    p.rot('spine', R(x=sp))
    p.rot('chest', R(x=ch))
    p.rot('neck', R(x=track(f, {1: -2, 3: -9, 5: 2, 6: 2})))
    p.rot('head', R(x=track(f, {1: -1, 3: -9, 5: -3, 6: -3})))
    halo(p, pp+sp+ch, track(f, {1: 0, 3: 0.008, 5: -0.018, 6: -0.022}))
    for s in ('L', 'R'):
        x, y = ball_xy(s)
        toe = track(ff, {1: 0, 4: 0, 5: 4, 6: 24, 7: 40, 8: 50})
        z = ball_z_for_tip(toe, track(ff, {1: 0, 5: 0, 6: 0.04, 7: 0.13, 9: 0.22, 11: 0.25}))
        p.foot(s, (x, y, z), track(ff, {1: 0, 3: 0, 4: 18, 5: 46, 6: 64, 8: 78}), toe, yaw=sign(s)*STAND_YAW)
    # Arms load back in the crouch and drive forward-up through the takeoff.
    a = STAND_ARMS
    arms(p, dict(swing=track(f, {1: a['swing'], 3: 20, 4: 2, 5: -28, 6: -36}),
                 adduct=track(f, {1: a['adduct'], 3: -4, 5: 6, 6: 7}),
                 elbow=track(f, {1: a['elbow'], 3: 22, 5: 30, 6: 32}), wrist=track(f, {1: a['wrist'], 3: 14, 5: -10, 6: -8}),
                 twist=track(f, {1: a['twist'], 5: 30}), curl=track(f, {1: a['curl'], 3: 25, 5: 18}),
                 thumb=track(f, {1: a['thumb'], 4: 12})))
    tabard(p, pp, track(f, {1: 0, 3: -18, 5: 2, 6: 3}), track(f, {1: 0, 3: 8, 5: 4}), track(f, {1: 0, 4: 4, 6: 2}),
           track(f, {1: 0, 3: -6, 5: 4, 6: 5}), track(f, {1: 0, 3: -4, 5: 3}))


def jump_pose(p, f):
    if f <= 5:
        jump_ground(p, f)
        return
    a = snap_of(p, jump_ground, f)
    b = snap_of(p, ascend_pose, ((f-N_JUMP) % N_ASC)/N_ASC)
    w = {'legs': ramp(f, 5.0, 10.0), 'body': ramp(f, 5.0, N_JUMP), 'arms': ramp(f, 5.0, 10.0),
         'tabard': ramp(f, 5.5, N_JUMP), 'halo': ramp(f, 5.0, N_JUMP)}
    p.reset()
    apply_blend(p, a, b, w)


# ----------------------------------------------------------------------------- land
LAND_FOOT = {
    'L': dict(z={1: 0.0}, pitch={1: 60, 2: 38, 3: 22, 4: 14, 6: 10, 9: 3, 12: 0},
              toe={1: 22, 2: 4, 3: 0}),
    'R': dict(z={1: None, 2: 0.13, 3: 0.0}, pitch={1: 80, 2: 66, 3: 40, 4: 22, 6: 12, 9: 4, 13: 0},
              toe={1: 34, 2: 26, 3: 5, 4: 0}),
}


def land_pose(p, f):
    """Frame 1 is exactly Descend frame 1; the landing (`land_raw`) runs one frame later and each part
    fades in from the Descend pose."""
    a = snap_of(p, descend_pose, 0.0)
    b = snap_of(p, land_raw, f-1)
    w = {'legs': ramp(f, 1.0, 2.0), 'body': ramp(f, 1.0, 3.0), 'arms': ramp(f, 1.0, 4.0),
         'tabard': ramp(f, 1.0, 5.0), 'halo': ramp(f, 1.0, 4.0)}
    p.reset()
    apply_blend(p, a, b, w)


def land_raw(p, f):
    """Left claws touch (1), right follows (3), compress (4-6), recover to the stand (7-14).
    Balls stay on the stand spots, so the planted feet never slide."""
    w = ramp(f, 1.0, 4.0)
    # Compression holds ~2 frames (5-7), then a slower rise that finishes exactly on the stand.
    pp = lerp(2.0, 3.0, w)+track(f, {1: 0, 3: 6, 5: 13, 7: 12, 10: 5, 12: 1.5, 14: 0})
    sp = track(f, {1: 1.6, 3: 4.5, 5: 9, 7: 8, 10: 3, 14: 1})
    ch = track(f, {1: 0.5, 5: 4, 7: 3.5, 10: 1, 14: 0})
    dip = track(f, {1: 0.0, 2: -0.04, 3: -0.10, 4: -0.16, 5: -0.195, 6: -0.195, 7: -0.185, 9: -0.13, 11: -0.065,
                    13: -0.015, 14: 0.0})
    p.pelvis((0, track(f, {1: 0, 5: 0.025, 14: 0}), lerp(0.012, STAND_Z, w)+dip), R(x=pp))
    p.rot('spine', R(x=sp))
    p.rot('chest', R(x=ch))
    p.rot('neck', R(x=track(f, {1: 1, 5: -5, 9: -3, 14: -2})))
    p.rot('head', R(x=track(f, {1: 4, 5: -6, 9: -2, 14: -1})))
    halo(p, pp+sp+ch, track(f, {1: 0.022, 4: 0.012, 6: -0.004, 9: 0.002, 14: 0.0}))
    for s in ('L', 'R'):
        x, y = ball_xy(s)
        d = LAND_FOOT[s]
        toe = track(f, d['toe'])
        zk = {k: (DESC_FEET[s]['z'] if v is None else v) for k, v in d['z'].items()}
        z = max(ball_z_for_tip(toe), gait.BALL_Z+track(f, {k: v-gait.BALL_Z if k == 1 else v for k, v in zk.items()}))
        p.foot(s, (x, y, z), track(f, d['pitch']), toe, yaw=sign(s)*STAND_YAW)
    a = mix(descend_arms(0.0), STAND_ARMS, ramp(f, 2.0, 12.0))
    bal = track(f, {1: 0, 3: 0.6, 5: 1.0, 8: 0.5, 12: 0})
    a['adduct'] -= 10*bal
    a['swing'] -= 8*bal
    a['elbow'] += 8*bal
    arms(p, a)
    p.rot('tabard front.1', R(x=-pp+track(f, {1: -16, 3: -8, 5: -16, 8: -3, 14: 0})))
    p.rot('tabard front.2', R(x=track(f, {1: -10, 4: 6, 7: -2, 14: 0})))
    p.rot('tabard front.3', R(x=track(f, {1: -6, 4: 6, 8: -2, 14: 0})))
    p.rot('tabard back.1', R(x=-pp+track(f, {1: 18, 4: 4, 7: -3, 14: 0})))
    p.rot('tabard back.2', R(x=track(f, {1: 10, 4: 8, 7: -3, 14: 0})))


# ----------------------------------------------------------------------------- halo follow-through
def halo_points(p, pose_fn, frames):
    pts = []
    for f in frames:
        p.reset()
        pose_fn(p, f)
        p.update()
        pts.append(p.world('halo root').translation.copy())
    return pts


HALO_TILT = 70.0


def apply_halo(p, caps, frames, offsets):
    """Same world delta as hs_anim.halo_lag for given offsets."""
    for f, o in zip(frames, offsets):
        q0 = caps[f]['halo root'].translation.copy()
        horizontal = Vector((o.x, o.y, 0))
        tilt = Matrix.Identity(4)
        if horizontal.length > 1e-6:
            axis = Vector((0, 0, 1)).cross(horizontal).normalized()
            tilt = axis_rot(axis, -HALO_TILT*horizontal.length).to_4x4()
        apply_world_delta(p, caps[f], 'halo root', Matrix.Translation(q0+o) @ tilt @ Matrix.Translation(-q0))
    return {'halo_max_offset_m': round(max(o.length for o in offsets), 4)}


def loop_offset(p, loop_fn, n, f):
    """Halo offset the loop clip carries at frame f (same evaluation as its halo_lag bake)."""
    pts = halo_points(p, loop_fn, list(range(1, n+2)))
    return halo_offsets(pts, True)[(f-1) % n]


def one_shot_offsets(caps, frames, preroll, start, end, fade=4):
    """Spring lag with pre-roll, cross-faded onto the neighbouring clips' offsets at both ends."""
    pts = [caps[f]['halo root'].translation.copy() for f in frames]
    offs = halo_offsets(list(preroll)+pts, False)[len(preroll):]
    n = len(frames)
    out = []
    for i, o in enumerate(offs):
        o = o.lerp(start, 1-ramp(i, 0, fade-1))
        o = o.lerp(end, ramp(i, n-fade, n-1))
        out.append(o)
    return out


def asc_loop(p, f):
    ascend_pose(p, ((f-1) % N_ASC)/N_ASC)


def desc_loop(p, f):
    descend_pose(p, ((f-1) % N_DESC)/N_DESC)


def post_loop(p, caps, frames):
    return {'halo_max_offset_m': round(halo_lag(p, caps, frames, True), 4)}


def post_jump(p, caps, frames):
    preroll = halo_points(p, lambda q, f: stand_pose(q), [1]*12)
    zero = Vector((0, 0, 0))
    return apply_halo(p, caps, frames, one_shot_offsets(caps, frames, preroll, zero, loop_offset(p, asc_loop, N_ASC, 1)))


def post_land(p, caps, frames):
    preroll = halo_points(p, desc_loop, list(range(1, N_DESC+1))*2)
    zero = Vector((0, 0, 0))
    return apply_halo(p, caps, frames, one_shot_offsets(caps, frames, preroll, loop_offset(p, desc_loop, N_DESC, 1), zero))


def build(p):
    meta = {'kind': 'air'}
    return [
        bake(p, 'Jump', list(range(1, N_JUMP+1)), jump_pose, False,
             markers={'Crouch': 3, 'Takeoff': 6}, meta={**meta, 'ends_in': 'Ascend'}, post=post_jump),
        bake(p, 'Ascend', list(range(1, N_ASC+2)), asc_loop, True, meta=meta, post=post_loop),
        bake(p, 'Descend', list(range(1, N_DESC+2)), desc_loop, True, meta=meta, post=post_loop),
        bake(p, 'Land', list(range(1, N_LAND+1)), land_pose, False,
             markers={'L touch': 2, 'R touch': 4, 'Compress': 6, 'Recovered': N_LAND},
             meta={**meta, 'speed_mps': 0.0, 'starts_from': 'Descend', 'ends_in': 'Stand'}, post=post_land),
    ]
