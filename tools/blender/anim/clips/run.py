"""Run forward: light, bounding in-place run cycle on the digitigrade claw feet."""
import math
from hs_anim import R, bake, halo_lag, lerp, sign, tabard_follow, wave, MID_X
import gait

N = 16
G = gait.params(speed=6.0, cycle=N, stance=0.21, lift=0.32, front_bias=0.52, pushoff=46.0, heel_off=0.15,
                lift_ease=1.5, contact_match=0.7, release_match=0.5, swing_delay=0.0, angle_blur=1.0)
HALO = dict(gain=0.35)   # glide.run_offsets must evaluate the run halo with the same settings
SPARK = 0.45             # heel spark (vfx hs_spark_*) flashed by each toe-off
JET_DIR = 12.0           # spark exhaust tilt below horizontal (glide.JET_DIR['run'])
FOOT_MID = -0.016
WIDTH = 0.28
LEAN = 18.0
TWIST = 55.0  # forearm roll: + turns the palm inward (thumb up) when the elbow is flexed
# Around each arm's high point (its own f2 / f10) the orientation pass (handpass) wanted up to ~84 deg less roll in
# 4 frames; authoring most of it here leaves the pass a small, smooth top-up.
ROLL_UP, ROLL_AT = 72.0, 0.0625
# Arm swing path. adduct = adduct + fwd*max(-sw, 0) + back*max(sw, 0) (sw: +1 arm back, -1 forward).
# adduct 15 / twist -25 (elbow in, forearm out) keeps hands and forearms >= 15 mm off the hips, thighs and
# tabard on every frame (clearance.py); the v9 path (27 / 0) brushed them on most frames.
ARM = dict(base=8.0, swing=42.0, adduct=15.0, fwd=-4.0, back=0.0, elbow=56.0, elbow_lag=18.0, twist=-25.0, wrist=8.0,
           curl=44.0, thumb=22.0)


def thigh_forward(y):
    return math.degrees(math.atan2(-y, 0.85))


def pose(p, f):
    t = ((f-1) % N)/N
    ys = {}
    for s, ph in (('L', 0.0), ('R', 0.5)):
        y, lift, pitch, toe = gait.foot_cycle(t-ph, G)
        ys[s] = y
        p.foot(s, (FOOT_MID+sign(s)*WIDTH, y, gait.BALL_Z+lift), pitch, toe)
    low = G['stance']*0.45
    bob = -math.cos(4*math.pi*(t-low))           # -1 at mid-stance, +1 mid-flight
    step = math.cos(2*math.pi*t)                 # +1 when the left foot lands
    roll = math.cos(2*math.pi*(t-low))           # +1 settling onto the left foot
    # Hips and chest counter-rotate (~15 deg apart at contact); neck/head cancel it so the gaze holds.
    p.pelvis((0.006*roll, -0.02, -0.066+0.032*bob), R(x=LEAN*0.6, y=-3.5*roll, z=-9.0*step))
    p.rot('spine', R(x=LEAN*0.3+1.5*bob, y=1.8*roll, z=7.0*step))
    p.rot('chest', R(x=LEAN*0.2+1.0*bob, y=1.2*math.cos(2*math.pi*(t-low-0.06)), z=8.5*math.cos(2*math.pi*(t-0.03))))
    p.rot('neck', R(x=-LEAN*0.45, z=-3.25*step))
    p.rot('head', R(x=-LEAN*0.45-1.0*bob, y=0.5*roll, z=-3.25*step))
    for s in ('L', 'R'):
        k = sign(s)
        sw = math.cos(2*math.pi*(t-0.04))*k      # + = this arm back
        lag = math.cos(2*math.pi*(t-0.10))*k     # forearm trails the upper arm
        p.rot(f'{s} scapula', R(z=k*5.0*sw))    # shoulder rides forward with the arm
        up = (0.5+0.5*math.cos(2*math.pi*(t-ROLL_AT-(0.0 if s == 'L' else 0.5))))**2.5
        a = ARM
        p.arm(s, swing=a['base']+a['swing']*sw, adduct=a['adduct']+a['fwd']*max(-sw, 0.0)+a['back']*max(sw, 0.0),
              elbow=a['elbow']-a['elbow_lag']*lag, twist=a['twist'], wrist=(a['wrist']*lag, 0, 0),
              forearm_twist=TWIST-ROLL_UP*up)
        p.curl(s, a['curl'], a['thumb'])
    fwd = max(thigh_forward(ys['L']), thigh_forward(ys['R']), 0)
    lag = tabard_follow(lambda u: -9.0*math.cos(2*math.pi*u), lambda u: -3.5*math.cos(2*math.pi*(u-low)), t)
    flap = math.cos(4*math.pi*(t-low-0.12))
    for n, x in (('tabard front.1', 12-0.8*fwd), ('tabard front.2', 10+0.3*fwd+4*bob), ('tabard front.3', 8+6*flap),
                 ('tabard back.1', 14+3*bob), ('tabard back.2', 10+6*flap)):
        p.rot(n, R(x=x, y=lag[n][0], z=lag[n][1]))


def post(p, caps, frames):
    return {'halo_max_offset_m': round(halo_lag(p, caps, frames, True, **HALO), 4)}


def spark(f, side):
    """Heel spark for `side` at frame f: rises into its toe-off (stance end) and dies ~2.5 frames after.
    Periodic in the cycle, so the loop wrap is exact."""
    off = G['stance']*N+(0.0 if side == 'L' else N/2)
    d = (f-1-off+1.5) % N
    return SPARK*math.sin(math.pi*min(d/4.0, 1.0))**2 if d < 4.0 else 0.0


def props():
    return {'hs_spark_L': lambda f: spark(f, 'L'), 'hs_spark_R': lambda f: spark(f, 'R'),
            'hs_jet_dir': lambda f: JET_DIR}


def build(p):
    frames = list(range(1, N+2))
    return [bake(p, 'Run forward', frames, pose, True,
                 markers={'L contact': 1, 'R contact': 1+N//2},
                 meta={'speed_mps': G['speed'], 'meters_per_cycle': round(G['speed']*N/24, 3),
                       'stance': G['stance'], 'kind': 'locomotion', 'seam_anchors': [1, 9, 10]},   # Glide exit and the turns start on f1, turns end on f9, enter starts on f10
                 post=post, props=props())]
