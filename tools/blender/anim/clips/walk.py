"""Walk forward: calm, upright, slightly regal in-place walk on the digitigrade claw feet.

Heel-toe and grounded (no thrust): the heel spur strikes first with the toes ~10 deg up (pivoting on the
heel, `gait.heel_pivot`), the foot rolls flat by a quarter of stance, then peels off heel-first onto the toes."""
import math
from hs_anim import R, bake, halo_lag, sign, tabard_follow
import gait

N = 26
G = gait.params(speed=1.5, cycle=N, stance=0.6, lift=0.10, lift_peak=0.4, front_bias=0.55,
                swing_delay=0.05, pushoff=30.0, heel_off=0.45, swing_pitch=18.0, contact_pitch=-10.0,
                toe_trail=14.0, toe_reach=6.0)
FOOT_MID = -0.016
WIDTH = 0.25
LEAN = 3.0
HIGH = 0.3                                       # left mid-stance (single support, pelvis highest)
# adduct = adduct + fwd*max(-sw, 0) + back*max(sw, 0) (sw: +1 arm back, -1 forward).
ARM = dict(base=2.0, swing=22.0, adduct=7.0, fwd=-2.0, back=0.0, elbow=16.0, elbow_lag=8.0, twist=0.0, wrist=3.0,
           wrist_lag=4.0, curl=18.0, thumb=10.0)


def thigh_forward(y):
    return math.degrees(math.atan2(-y, 0.85))


def pose(p, f):
    t = ((f-1) % N)/N
    ys = {}
    for s, ph in (('L', 0.0), ('R', 0.5)):
        y, lift, pitch, toe = gait.foot_cycle(t-ph, G)
        ys[s] = y
        p.foot(s, gait.heel_pivot((FOOT_MID+sign(s)*WIDTH, y, gait.BALL_Z+lift), pitch), pitch, toe)
    bob = math.cos(4*math.pi*(t-HIGH))           # +1 at single support, -1 at double support
    side = math.cos(2*math.pi*(t-HIGH))          # +1 over the left foot
    step = math.cos(2*math.pi*t)                 # +1 when the left foot lands
    p.pelvis((0.020*side, 0, -0.042+0.026*bob), R(x=LEAN*0.5, y=-2.4*side, z=-6.5*step))
    p.rot('spine', R(x=LEAN*0.3, y=1.4*side, z=4.5*step))
    p.rot('chest', R(x=LEAN*0.2+0.6*bob, y=1.0*side, z=6.0*math.cos(2*math.pi*(t-0.03))))
    p.rot('neck', R(x=-LEAN*0.5, z=-2.0*step))
    p.rot('head', R(x=-LEAN*0.5+0.6*bob, z=-2.0*step))
    for s in ('L', 'R'):
        k = sign(s)
        sw = math.cos(2*math.pi*(t-0.05))*k      # + = this arm back
        lag = math.cos(2*math.pi*(t-0.13))*k     # forearm follows the upper arm a beat late
        p.rot(f'{s} scapula', R(z=k*3.0*sw))
        a = ARM
        p.arm(s, swing=a['base']+a['swing']*sw, adduct=a['adduct']+a['fwd']*max(-sw, 0.0)+a['back']*max(sw, 0.0),
              elbow=a['elbow']-a['elbow_lag']*lag, twist=a['twist'], wrist=(a['wrist']-a['wrist_lag']*lag, 0, 0))
        p.curl(s, a['curl'], a['thumb'])
    fwd = max(thigh_forward(ys['L']), thigh_forward(ys['R']), 0)
    bwd = max(-thigh_forward(ys['L']), -thigh_forward(ys['R']), 0)
    lag = tabard_follow(lambda u: -6.5*math.cos(2*math.pi*u), lambda u: -2.4*math.cos(2*math.pi*(u-HIGH)), t,
                        gain=1.6)
    flap = math.cos(4*math.pi*(t-HIGH-0.1))
    for n, x in (('tabard front.1', 4-0.5*fwd), ('tabard front.2', 4+1.5*bob), ('tabard front.3', 3+3*flap),
                 ('tabard back.1', 3+0.3*bwd+1.5*bob), ('tabard back.2', 5+3*flap)):
        p.rot(n, R(x=x, y=lag[n][0], z=lag[n][1]))


def post(p, caps, frames):
    return {'halo_max_offset_m': round(halo_lag(p, caps, frames, True, max_offset=0.02), 4)}


def build(p):
    frames = list(range(1, N+2))
    return [bake(p, 'Walk forward', frames, pose, True,
                 markers={'L contact': 1, 'R contact': 1+N//2},
                 meta={'speed_mps': G['speed'], 'meters_per_cycle': round(G['speed']*N/24, 3),
                       'stance': G['stance'], 'kind': 'locomotion'},
                 post=post)]
