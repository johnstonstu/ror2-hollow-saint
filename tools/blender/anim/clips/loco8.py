"""Eight-direction locomotion (item 9h): the run diagonals and the walk directions missing from the blend space.

Directions are relative to facing (the character always faces -Y; the game moves the body along `travel`):
Run forward right / forward left / backward right / backward left, on Run forward's 16-frame cycle and foot timing
(run_dirs.TIMING, L contact f1, R f9), and Walk backward / left / right plus the four walk diagonals on Walk
forward's 26-frame cycle (walk.G timing).

Run diagonals: the upper body (pelvis, spine up, arms, tabard) is the 50/50 blend of the two neighbouring
cardinal poses, exactly as a blend tree would mix them, except that the backward diagonals keep the backward
run's arms (the strafe and backward arm swings run in opposite phase and would cancel). The feet are solved
along the diagonal with the shared timing and the two cardinals' gait heights/angles averaged, so the
planted foot slides at exactly the diagonal travel speed and 50/50 blends with either neighbour don't skate.
"""
import math

from mathutils import Vector

from hs_anim import R, FPS, bake, halo_lag, lerp, sign, tabard_follow
import gait
import run
import run_dirs
import walk

N_RUN = run.N
ARM_BONES = ('scapula', 'upperarm', 'forearm', 'forearm twist', 'hand', 'shoulder', 'thumb', 'index', 'middle',
             'ring', 'little')
GAIT_KEYS = ('speed', 'lift', 'lift_peak', 'front_bias', 'pushoff', 'heel_off', 'swing_pitch', 'contact_pitch',
             'toe_trail', 'toe_reach', 'lift_ease', 'angle_blur')


def unit(x, y):
    n = math.hypot(x, y)
    return (x/n, y/n)


# Cardinal run styles: pose function, gait, feet footprint (width, base y per side), foot yaw (lead, trail
# toward travel) and knee pole shift outward.
def _strafe(k):
    return dict(pose=run_dirs.make_strafe(k)[0], g=run_dirs.GS, width=run_dirs.WIDTH_S,
                base_y=lambda s, k=k: sign(s)*k*run_dirs.STAGGER, travel=(float(k), 0.0), k=k,
                yaw=(run_dirs.FOOT_YAW, run_dirs.FOOT_YAW_TRAIL), knee_out=run_dirs.KNEE_OUT, l_out=run_dirs.L_OUT)


RUN_STYLE = {
    'forward': dict(pose=run.pose, g=run.G, width=run.WIDTH, base_y=lambda s: 0.0, travel=(0.0, -1.0), k=0,
                    yaw=(0.0, 0.0), knee_out=0.0),
    'backward': dict(pose=run_dirs.pose_back, g=run_dirs.GB, width=run_dirs.WIDTH_B,
                     base_y=lambda s: run_dirs.BACK_Y, travel=(0.0, 1.0), k=0, yaw=(0.0, 0.0), knee_out=0.0),
    'left': _strafe(1),
    'right': _strafe(-1),
}
# title: (cardinal A, cardinal B, bones that take B's pose only / A's only)
RUN_DIAG = {
    'Run forward right': ('forward', 'right', None),
    'Run forward left': ('forward', 'left', None),
    'Run backward right': ('backward', 'right', 'arms_from_a'),
    'Run backward left': ('backward', 'left', 'arms_from_a'),
}


def is_arm(name):
    parts = name.split(' ', 1)
    return len(parts) == 2 and parts[0] in ('L', 'R') and parts[1].split('.')[0] in ARM_BONES


def capture_basis(p):
    return {b.name: (b.location.copy(), b.rotation_quaternion.copy()) for b in p.pb
            if not b.name.endswith((' IK', ' pole')) and not b.name.endswith(' toe')}


def apply_blend(p, a, b, w, keep_a=(), arm_w=None):
    for n, (la, qa) in a.items():
        lb, qb = b[n]
        u = 0.0 if n in keep_a else (arm_w if arm_w is not None and is_arm(n) else w)
        qb = qb if qa.dot(qb) >= 0 else -qb
        pb = p.pb[n]
        pb.location = la.lerp(lb, u)
        pb.rotation_quaternion = qa.slerp(qb, u)


def diag_gait(a, b):
    ga, gb = RUN_STYLE[a]['g'], RUN_STYLE[b]['g']
    g = gait.params(**run_dirs.TIMING)
    for k in GAIT_KEYS:
        g[k] = lerp(ga.get(k, gait.DEFAULT.get(k, 0.0)), gb.get(k, gait.DEFAULT.get(k, 0.0)), 0.5)
    return g


# Extra half-width on the diagonals: the stride crosses the body at 45 degrees, so the averaged footprint lets
# the ankles pass inside each other (forward only; wider than this over-reaches the IK).
DIAG_WIDEN = {'forward': 0.025, 'backward': 0.0}
# The forward diagonals reach further than either neighbour; a slightly lower pelvis keeps the knee off full
# extension at toe-off (a near-straight IK knee snaps the shin).
DIAG_DROP = {'forward': -0.045, 'backward': 0.0}
# Forward diagonals carry the arms mostly as the strafe does (forearms high over the lead thigh swinging out under
# them); a 50/50 arm mix drops the L hand into the L thigh.
DIAG_ARM_W = {'forward': 0.75, 'backward': None}
# On the left diagonals the L hand swings past the lead L thigh (the widened footprint puts it under the hand), so
# the L upper arm opens out by this many degrees.
DIAG_L_ABDUCT = 5.0


def make_run_diag(title):
    a, b, mode = RUN_DIAG[title]
    sa, sb = RUN_STYLE[a], RUN_STYLE[b]
    travel = unit(sa['travel'][0]+sb['travel'][0], sa['travel'][1]+sb['travel'][1])
    g = diag_gait(a, b)
    k = sb['k']                                  # the strafe side of the diagonal: +1 left, -1 right

    def pose(p, f):
        t = ((f-1) % N_RUN)/N_RUN
        p.reset()
        p.ik(1.0, 0.0)
        sa['pose'](p, f)
        pa = capture_basis(p)
        p.reset()
        p.ik(1.0, 0.0)
        sb['pose'](p, f)
        pb = capture_basis(p)
        keep = {n for n in pa if is_arm(n)} if mode == 'arms_from_a' else ()
        apply_blend(p, pa, pb, 0.5, keep, arm_w=DIAG_ARM_W[a])
        if k > 0:
            r3 = p.r3['L upperarm']
            q = p.pb['L upperarm']
            q.rotation_quaternion = (r3.inverted() @ R(y=-DIAG_L_ABDUCT) @ r3).to_quaternion() @ q.rotation_quaternion
        p.pb['pelvis'].location += p.r3['pelvis'].inverted() @ Vector((0.0, 0.0, DIAG_DROP[a]))
        for s, ph in (('L', 0.0), ('R', 0.5)):
            l_out = max(sa.get('l_out', 0.0), sb.get('l_out', 0.0)) if s == 'L' else 0.0
            base = (run.FOOT_MID+sign(s)*(lerp(sa['width'], sb['width'], 0.5)+DIAG_WIDEN[a])+l_out,
                    lerp(sa['base_y'](s), sb['base_y'](s), 0.5))
            (x, y), lift, pitch, toe, _ = run_dirs.track(t-ph, g, travel, base)
            lead = sign(s) == k
            yaw = 0.5*(sb['yaw'][0] if lead else sb['yaw'][1])
            p.foot(s, (x, y, gait.BALL_Z+lift), pitch, toe, yaw=k*yaw,
                   pole_shift=(sign(s)*0.5*sb['knee_out'], 0, 0))
    return pose, travel, g


# ----------------------------------------------------------------------------- walk
# Every walk direction uses Walk forward's timing (26 f, stance 0.6, L contact f1) so 50/50 blends plant and lift
# together. At that stance a side step plants for 0.65 s, so the side walk is authored slow (0.6 m/s, 0.39 m per
# contact) to keep the feet ~19 cm apart as they close (a wider side stride needs a crossover).
N_WALK = walk.N
WALK_TIMING = {k: walk.G.get(k, gait.DEFAULT[k]) for k in ('cycle', 'stance', 'swing_delay', 'release_match',
                                                            'contact_match')}
GWB = gait.params(speed=1.0, lift=0.09, lift_peak=0.5, front_bias=0.5, pushoff=10.0, heel_off=0.5, swing_pitch=10.0,
                  contact_pitch=12.0, toe_trail=8.0, toe_reach=4.0, **WALK_TIMING)
GWS = gait.params(speed=0.6, lift=0.08, lift_peak=0.45, front_bias=0.5, pushoff=18.0, heel_off=0.4, swing_pitch=12.0,
                  contact_pitch=4.0, toe_trail=10.0, toe_reach=4.0, **WALK_TIMING)
WIDTH_WS = 0.29
STAGGER_W = 0.05
HIP_YAW_W = 10.0


def walk_body(p, t, step_sign=1.0, arm_sign=1.0, arm_gain=1.0, lean=walk.LEAN, k=0, hip_yaw=0.0, adduct=0.0,
              elbow=0.0):
    """walk.pose's upper body (identical at the defaults). step_sign flips the step-coupled yaws (backward: the
    leg that lands is behind), arm_sign the arm phase; k/hip_yaw turn the hips toward a side travel with the
    spine, chest and head counter-rotating back to the aim."""
    H = walk.HIGH
    bob = math.cos(4*math.pi*(t-H))
    side = math.cos(2*math.pi*(t-H))
    step = math.cos(2*math.pi*t)*step_sign
    y = k*hip_yaw
    p.pelvis((0.020*side, 0, -0.042+0.026*bob), R(x=lean*0.5, y=-2.4*side+k*2.0, z=-6.5*step+y))
    p.rot('spine', R(x=lean*0.3, y=1.4*side, z=4.5*step-0.45*y))
    p.rot('chest', R(x=lean*0.2+0.6*bob, y=1.0*side, z=6.0*math.cos(2*math.pi*(t-0.03))*step_sign-0.4*y))
    p.rot('neck', R(x=-lean*0.5, z=-2.0*step-0.1*y))
    p.rot('head', R(x=-lean*0.5+0.6*bob, z=-2.0*step-0.05*y))
    for s in ('L', 'R'):
        ks = sign(s)
        sw = math.cos(2*math.pi*(t-0.05))*ks*arm_sign
        lag = math.cos(2*math.pi*(t-0.13))*ks*arm_sign
        p.rot(f'{s} scapula', R(z=ks*3.0*sw*arm_gain))
        a = walk.ARM
        p.arm(s, swing=a['base']+a['swing']*sw*arm_gain,
              adduct=a['adduct']+adduct+a['fwd']*max(-sw, 0.0)+a['back']*max(sw, 0.0),
              elbow=a['elbow']+elbow-a['elbow_lag']*lag*arm_gain, twist=a['twist'],
              wrist=(a['wrist']-a['wrist_lag']*lag*arm_gain, 0, 0))
        p.curl(s, a['curl'], a['thumb'])
    lagt = tabard_follow(lambda u: -6.5*math.cos(2*math.pi*u)*step_sign,
                         lambda u: -2.4*math.cos(2*math.pi*(u-H)), t, gain=1.6)
    flap = math.cos(4*math.pi*(t-H-0.1))
    return bob, flap, lagt


def walk_tabard(p, ys, bob, flap, lagt, k=0, hip_yaw=0.0):
    fwd = max(walk.thigh_forward(ys['L']), walk.thigh_forward(ys['R']), 0)
    bwd = max(-walk.thigh_forward(ys['L']), -walk.thigh_forward(ys['R']), 0)
    for n, x in (('tabard front.1', 4-0.5*fwd), ('tabard front.2', 4+1.5*bob), ('tabard front.3', 3+3*flap),
                 ('tabard back.1', 3+0.3*bwd+1.5*bob), ('tabard back.2', 5+3*flap)):
        z = -k*hip_yaw*0.5 if n.endswith('.1') else 0.0
        p.rot(n, R(x=x, y=lagt[n][0], z=lagt[n][1]+z))


def walk_feet(p, t, g, travel, base, yaw=lambda s: 0.0, pole=lambda s: 0.0):
    ys = {}
    for s, ph in (('L', 0.0), ('R', 0.5)):
        (x, y), lift, pitch, toe, _ = run_dirs.track(t-ph, g, travel, base(s))
        ys[s] = y
        p.foot(s, gait.heel_pivot((x, y, gait.BALL_Z+lift), pitch), pitch, toe, yaw=yaw(s),
               pole_shift=(sign(s)*pole(s), 0, 0))
    return ys


def walk_back_pose(p, f):
    t = ((f-1) % N_WALK)/N_WALK
    ys = walk_feet(p, t, GWB, (0.0, 1.0), lambda s: (walk.FOOT_MID+sign(s)*walk.WIDTH, -0.03))
    bob, flap, lagt = walk_body(p, t, step_sign=-1.0, arm_sign=-1.0, arm_gain=0.8, lean=1.0)
    walk_tabard(p, ys, bob, flap, lagt)


def make_walk_side(k):
    travel = (float(k), 0.0)

    def base(s):
        return (walk.FOOT_MID+sign(s)*WIDTH_WS+(run_dirs.L_OUT*0.5 if s == 'L' else 0.0), sign(s)*k*STAGGER_W)

    def pose(p, f):
        t = ((f-1) % N_WALK)/N_WALK
        ys = walk_feet(p, t, GWS, travel, base, yaw=lambda s: k*(6.0 if sign(s) == k else 3.0),
                       pole=lambda s: 0.03)
        # Arms carried out (adduct < 0) and bent: the wide stance puts the thighs under the hanging hands.
        bob, flap, lagt = walk_body(p, t, step_sign=0.5, arm_gain=0.45, lean=walk.LEAN, k=k, hip_yaw=HIP_YAW_W,
                                    adduct=-14.0, elbow=30.0)
        walk_tabard(p, ys, bob, flap, lagt, k, HIP_YAW_W)
    return pose, travel


WALK_STYLE = {
    'forward': dict(pose=walk.pose, g=walk.G, width=walk.WIDTH, base_y=lambda s: 0.0, travel=(0.0, -1.0), k=0,
                    yaw=(0.0, 0.0), knee_out=0.0, l_out=0.0),
    'backward': dict(pose=walk_back_pose, g=GWB, width=walk.WIDTH, base_y=lambda s: -0.03, travel=(0.0, 1.0), k=0,
                     yaw=(0.0, 0.0), knee_out=0.0, l_out=0.0),
}
for _k, _n in ((1, 'left'), (-1, 'right')):
    WALK_STYLE[_n] = dict(pose=make_walk_side(_k)[0], g=GWS, width=WIDTH_WS,
                          base_y=lambda s, k=_k: sign(s)*k*STAGGER_W, travel=(float(_k), 0.0), k=_k,
                          yaw=(6.0, 3.0), knee_out=0.03, l_out=run_dirs.L_OUT*0.5)
WALK_DIAG = {
    'Walk forward right': ('forward', 'right', None),
    'Walk forward left': ('forward', 'left', None),
    'Walk backward right': ('backward', 'right', 'arms_from_a'),
    'Walk backward left': ('backward', 'left', 'arms_from_a'),
}


def make_walk_diag(title):
    a, b, mode = WALK_DIAG[title]
    sa, sb = WALK_STYLE[a], WALK_STYLE[b]
    travel = unit(sa['travel'][0]+sb['travel'][0], sa['travel'][1]+sb['travel'][1])
    g = gait.params(**WALK_TIMING)
    for key in GAIT_KEYS:
        g[key] = lerp(sa['g'].get(key, gait.DEFAULT.get(key, 0.0)), sb['g'].get(key, gait.DEFAULT.get(key, 0.0)), 0.5)
    k = sb['k']

    def base(s):
        return (walk.FOOT_MID+sign(s)*lerp(sa['width'], sb['width'], 0.5)+(max(sa['l_out'], sb['l_out'])
                                                                          if s == 'L' else 0.0),
                lerp(sa['base_y'](s), sb['base_y'](s), 0.5))

    def pose(p, f):
        t = ((f-1) % N_WALK)/N_WALK
        p.reset()
        p.ik(1.0, 0.0)
        sa['pose'](p, f)
        pa = capture_basis(p)
        p.reset()
        p.ik(1.0, 0.0)
        sb['pose'](p, f)
        pb = capture_basis(p)
        keep = {n for n in pa if is_arm(n)} if mode == 'arms_from_a' else ()
        apply_blend(p, pa, pb, 0.5, keep)
        walk_feet(p, t, g, travel, base, yaw=lambda s: k*0.5*(sb['yaw'][0] if sign(s) == k else sb['yaw'][1]),
                  pole=lambda s: 0.5*sb['knee_out'])
    return pose, travel, g


def walk_meta(direction, travel, g):
    return {'direction': direction, 'travel': [round(travel[0], 4), round(travel[1], 4)],
            'speed_mps': round(g['speed'], 3), 'meters_per_cycle': round(g['speed']*N_WALK/FPS, 3),
            'stance': g['stance'], 'kind': 'locomotion'}


def walk_clips():
    out = [('Walk backward', walk_back_pose, (0.0, 1.0), GWB)]
    for k, n in ((1, 'left'), (-1, 'right')):
        pose, travel = make_walk_side(k)
        out.append((f'Walk {n}', pose, travel, GWS))
    for title in WALK_DIAG:
        pose, travel, g = make_walk_diag(title)
        out.append((title, pose, travel, g))
    return out


def build(p):
    frames = list(range(1, N_RUN+2))
    markers = {'L contact': 1, 'R contact': 1+N_RUN//2}
    out = []
    for title in RUN_DIAG:
        pose, travel, g = make_run_diag(title)
        out.append(bake(p, title, frames, pose, True, markers=markers,
                        meta={'direction': title[4:].replace(' ', '_'), 'travel': [round(travel[0], 4),
                                                                              round(travel[1], 4)],
                              'speed_mps': round(g['speed'], 3), 'meters_per_cycle': round(g['speed']*N_RUN/FPS, 3),
                              'stance': g['stance'], 'kind': 'locomotion'},
                        post=run_dirs.make_post(travel, g['speed']), props=run.props()))
    wframes = list(range(1, N_WALK+2))
    wmarkers = {'L contact': 1, 'R contact': 1+N_WALK//2}
    for title, pose, travel, g in walk_clips():
        out.append(bake(p, title, wframes, pose, True, markers=wmarkers,
                        meta=walk_meta(title[5:].replace(' ', '_'), travel, g),
                        post=make_walk_post(travel, g['speed'])))
    return out


def make_walk_post(travel, speed):
    def post(p, caps, frames):
        out = run_dirs.checks(caps, frames, travel, speed)
        out['halo_max_offset_m'] = round(halo_lag(p, caps, frames, True, max_offset=0.02), 4)
        return out
    return post
