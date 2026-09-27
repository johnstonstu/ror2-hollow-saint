"""Directional runs for the locomotion blend tree: backward, left and right strafe.

Same 16-frame cycle as `run` (L contact frame 1, R contact frame 9) so blend-tree phases match.
The character always faces -Y; `travel` is the world direction the game moves the body, and the
planted foot slides the opposite way at exactly the authored speed.
"""
import math
from hs_anim import R, FPS, bake, halo_lag, sign, smooth, tabard_follow
import gait
from run import N, FOOT_MID, TWIST, thigh_forward


def track(phase, g, travel, base):
    """gait.foot_cycle re-aimed along `travel` (unit XY). Returns ((x, y), lift, pitch, toe, s)
    where s is the forward-cycle offset (+ = behind along the travel direction).
    g['swing_rush'] > 1 finishes the swing travel early, so the foot reaches and then sets down."""
    s, lift, pitch, toe = gait.foot_cycle(phase, g)
    rush = g.get('swing_rush', 1.0)
    u = ((phase % 1.0)-g['stance'])/(1-g['stance'])
    if rush != 1.0 and u >= 0:
        L = gait.contact_length(g)
        front = g['y_offset']-g['front_bias']*L
        s = front+L*(1-smooth(u*rush))
    return (base[0]-travel[0]*s, base[1]-travel[1]*s), lift, pitch, toe, s


def checks(caps, frames, travel, speed):
    """Planted-foot slide against the travel velocity, and leg-crossing gaps, on the solved pose."""
    expect = (-travel[0]*speed/FPS, -travel[1]*speed/FPS)
    slide, pairs = 0.0, 0
    for s in ('L', 'R'):
        for a, b in zip(frames, frames[1:]):
            pa, pb = caps[a][f'{s} toe'].translation, caps[b][f'{s} toe'].translation
            if pa.z < 0.06 and pb.z < 0.06:
                pairs += 1
                slide = max(slide, math.hypot(pb.x-pa.x-expect[0], pb.y-pa.y-expect[1]))
    ball_gap = min(caps[f]['L toe'].translation.x-caps[f]['R toe'].translation.x for f in frames)
    knee_gap = min(caps[f]['L shin'].translation.x-caps[f]['R shin'].translation.x for f in frames)
    ankle_gap = min(caps[f]['L foot'].translation.x-caps[f]['R foot'].translation.x for f in frames)
    out = {'direction_slide_m_per_frame': round(slide, 5), 'direction_slide_pairs': pairs,
           'min_ball_gap_x_m': round(ball_gap, 4), 'min_ankle_gap_x_m': round(ankle_gap, 4),
           'min_knee_gap_x_m': round(knee_gap, 4)}
    print('RUN_DIRS CHECK', out, flush=True)
    return out


def make_post(travel, speed):
    def post(p, caps, frames):
        out = checks(caps, frames, travel, speed)
        out['halo_max_offset_m'] = round(halo_lag(p, caps, frames, True, gain=0.35), 4)
        return out
    return post


# ----------------------------------------------------------------------------- backward
GB = gait.params(speed=4.25, cycle=N, stance=0.23, lift=0.18, lift_peak=0.55, front_bias=0.6,
                 swing_delay=0.0, swing_rush=1.4, pushoff=14.0, heel_off=0.55, swing_pitch=26.0, contact_pitch=20.0,
                 toe_trail=10.0, toe_reach=5.0)
BACK = (0.0, 1.0)
WIDTH_B = 0.27
BACK_Y = -0.05
LEAN_B = 3.0
# adduct = adduct + fwd*max(-sw, 0) + back*max(sw, 0) (sw: +1 arm back, -1 forward).
ARM_B = dict(base=14.0, swing=30.0, adduct=7.0, fwd=-3.0, back=-10.0, elbow=40.0, elbow_lag=13.0, twist=-25.0, wrist=7.0,
             curl=44.0, thumb=22.0)


def pose_back(p, f):
    t = ((f-1) % N)/N
    ys = {}
    for s, ph in (('L', 0.0), ('R', 0.5)):
        (x, y), lift, pitch, toe, _ = track(t-ph, GB, BACK, (FOOT_MID+sign(s)*WIDTH_B, BACK_Y))
        ys[s] = y
        p.foot(s, (x, y, gait.BALL_Z+lift), pitch, toe)
    low = GB['stance']*0.45
    bob = -math.cos(4*math.pi*(t-low))
    step = math.cos(2*math.pi*t)                 # +1 when the left foot lands (behind)
    roll = math.cos(2*math.pi*(t-low))
    # Torso leans back into the travel (~6 deg over spine+chest); neck/head bring the gaze level.
    p.pelvis((0.005*roll, 0.02, -0.09+0.026*bob), R(x=LEAN_B, y=-3.0*roll, z=7.5*step))
    p.rot('spine', R(x=-2.5+1.0*bob, y=1.5*roll, z=-5.5*step))
    p.rot('chest', R(x=-4.0+0.8*bob, z=-7.5*math.cos(2*math.pi*(t-0.03))))
    p.rot('neck', R(x=1.5, z=2.5*step))
    p.rot('head', R(x=2.0-0.8*bob, z=2.5*step))
    for s in ('L', 'R'):
        k = sign(s)
        sw = -math.cos(2*math.pi*(t-0.04))*k     # + = this arm back (leg behind -> arm forward)
        lag = -math.cos(2*math.pi*(t-0.10))*k
        p.rot(f'{s} scapula', R(z=k*4.0*sw))
        a = ARM_B
        p.arm(s, swing=a['base']+a['swing']*sw, adduct=a['adduct']+a['fwd']*max(-sw, 0.0)+a['back']*max(sw, 0.0),
              elbow=a['elbow']-a['elbow_lag']*lag, twist=a['twist'], wrist=(a['wrist']*lag, 0, 0), forearm_twist=TWIST)
        p.curl(s, a['curl'], a['thumb'])
    fwd = max(thigh_forward(ys['L']), thigh_forward(ys['R']), 0)
    bwd = max(-thigh_forward(ys['L']), -thigh_forward(ys['R']), 0)
    lag = tabard_follow(lambda u: 7.5*math.cos(2*math.pi*u), lambda u: -3.0*math.cos(2*math.pi*(u-low)), t)
    flap = math.cos(4*math.pi*(t-low-0.12))
    for n, x in (('tabard front.1', 4-0.8*fwd), ('tabard front.2', 4+0.3*fwd+3*bob), ('tabard front.3', 3+5*flap),
                 ('tabard back.1', 2+0.5*bwd+2*bob), ('tabard back.2', 6+5*flap)):
        p.rot(n, R(x=x, y=lag[n][0], z=lag[n][1]))


# ----------------------------------------------------------------------------- strafe
GS = gait.params(speed=4.5, cycle=N, stance=0.15, lift=0.14, lift_peak=0.45, front_bias=0.5,
                 swing_delay=0.0, swing_rush=1.25, pushoff=30.0, heel_off=0.4, swing_pitch=22.0, contact_pitch=8.0,
                 toe_trail=16.0, toe_reach=6.0)
WIDTH_S = 0.35
FOOT_YAW = 14.0                                  # lead foot toes toward travel
FOOT_YAW_TRAIL = 4.0                             # more on the trailing foot knocks its knee inward
KNEE_OUT = 0.12                                  # pole shift outward, keeps the knees apart
HIP_YAW = 16.0
LEAN_S = 8.0
# adduct = adduct - lead*j*k (lead arm lifts out, trail arm tucks) + fwd*max(-sw, 0) + back*max(sw, 0).
# The lead thigh swings far out under the arm at contact, so the forearms ride high (elbow 85) above it.
ARM_S = dict(base=2.0, swing=20.0, adduct=11.0, lead=5.0, fwd=0.0, back=0.0, elbow=85.0, elbow_lag=11.0, twist=-25.0,
             wrist=5.0, curl=44.0, thumb=22.0)


def make_strafe(k):
    """k = +1 runs left (+X), -1 runs right."""
    travel = (float(k), 0.0)

    def pose(p, f):
        t = ((f-1) % N)/N
        for s, ph in (('L', 0.0), ('R', 0.5)):
            base = (FOOT_MID+sign(s)*WIDTH_S, sign(s)*k*0.03)
            (x, y), lift, pitch, toe, _ = track(t-ph, GS, travel, base)
            lead = sign(s) == k
            p.foot(s, (x, y, gait.BALL_Z+lift), pitch, toe, yaw=k*(FOOT_YAW if lead else FOOT_YAW_TRAIL),
                   pole_shift=(sign(s)*KNEE_OUT, 0, 0))
        low = GS['stance']*0.45
        bob = -math.cos(4*math.pi*(t-low))
        step = math.cos(2*math.pi*t)
        roll = math.cos(2*math.pi*(t-low))
        # Leans ~16 deg into the travel through pelvis/spine/chest; neck/head take most of it back out.
        p.pelvis((0, -0.01, -0.095+0.028*bob),
                 R(x=LEAN_S*0.6, y=k*6.5-2.5*roll, z=k*HIP_YAW-4.5*step))
        p.rot('spine', R(x=LEAN_S*0.3+1.5*bob, y=k*5.0+1.0*roll, z=-k*HIP_YAW*0.45+3.5*step))
        p.rot('chest', R(x=LEAN_S*0.2+0.8*bob, y=k*4.0, z=-k*HIP_YAW*0.35+4.5*math.cos(2*math.pi*(t-0.03))))
        p.rot('neck', R(x=-LEAN_S*0.45, y=-k*6.5, z=-k*HIP_YAW*0.1-1.5*step))
        p.rot('head', R(x=-LEAN_S*0.45-1.0*bob, y=-k*5.5, z=-k*HIP_YAW*0.1-1.5*step))
        for s in ('L', 'R'):
            j = sign(s)
            sw = math.cos(2*math.pi*(t-0.04))*j
            lag = math.cos(2*math.pi*(t-0.10))*j
            p.rot(f'{s} scapula', R(z=j*3.0*sw))
            # Lead arm (travel side) lifts out a little for balance, trail arm tucks.
            a = ARM_S
            p.arm(s, swing=a['base']+a['swing']*sw,
                  adduct=a['adduct']-a['lead']*j*k+a['fwd']*max(-sw, 0.0)+a['back']*max(sw, 0.0),
                  elbow=a['elbow']-a['elbow_lag']*lag, twist=a['twist'], wrist=(a['wrist']*lag, 0, 0),
                  forearm_twist=TWIST)
            p.curl(s, a['curl'], a['thumb'])
        lag = tabard_follow(lambda u: -4.5*math.cos(2*math.pi*u), lambda u: -2.5*math.cos(2*math.pi*(u-low)), t,
                            gain=1.8)
        flap = math.cos(4*math.pi*(t-low-0.12))
        # Static part: panels trail against the travel (roll) and hang square to the travel (yaw).
        for n, x, y, z in (('tabard front.1', 4, k*1.5, -k*HIP_YAW*0.5), ('tabard front.2', 6+3*bob, k*2.5, 0),
                           ('tabard front.3', 5+5*flap, k*3, 0), ('tabard back.1', 8+2*bob, k*3, -k*HIP_YAW*0.5),
                           ('tabard back.2', 7+5*flap, k*3, 0)):
            p.rot(n, R(x=x, y=y+lag[n][0], z=z+lag[n][1]))
    return pose, travel


# ----------------------------------------------------------------------------- build
def build(p):
    frames = list(range(1, N+2))
    markers = {'L contact': 1, 'R contact': 1+N//2}
    out = [bake(p, 'Run backward', frames, pose_back, True, markers=markers,
                meta={'direction': 'backward', 'speed_mps': GB['speed'],
                      'meters_per_cycle': round(GB['speed']*N/FPS, 3), 'stance': GB['stance'],
                      'kind': 'locomotion'},
                post=make_post(BACK, GB['speed']))]
    for title, k, direction in (('Run left', 1, 'left'), ('Run right', -1, 'right')):
        pose, travel = make_strafe(k)
        out.append(bake(p, title, frames, pose, True, markers=markers,
                        meta={'direction': direction, 'speed_mps': GS['speed'],
                              'meters_per_cycle': round(GS['speed']*N/FPS, 3), 'stance': GS['stance'],
                              'kind': 'locomotion'},
                        post=make_post(travel, GS['speed'])))
    return out
