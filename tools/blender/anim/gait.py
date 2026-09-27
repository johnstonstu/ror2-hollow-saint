"""Foot trajectories for in-place walk/run cycles (phase 0 = touch-down, character faces -Y).

During stance the ball slides toward +Y at exactly the travel speed, so the planted foot does not
skate once the game moves the body at `speed`.
"""
import math
from hs_anim import FPS, clamp, smooth, lerp, ramp

BALL_Z = 0.055  # ball joint height with the toes flat on the ground
HEEL = (0.0, 0.285, -0.046)   # ball joint -> underside of the heel spur at rest (the heel-strike pivot)


def heel_pivot(ball, pitch):
    """Ball position for `Poser.foot` so a toes-up foot (pitch < 0) pivots on the heel spur instead of the
    ball (which would drive the heel through the ground). Unchanged for pitch >= 0."""
    if pitch >= 0.0:
        return ball
    a = math.radians(pitch)
    _, hy, hz = HEEL
    y, z = -hy, -hz                      # heel -> ball
    return (ball[0], ball[1]+hy+y*math.cos(a)-z*math.sin(a), ball[2]+hz+y*math.sin(a)+z*math.cos(a))


def contact_length(g):
    return g['speed']*g['stance']*g['cycle']/FPS


def foot_cycle(phase, g):
    """Return (y, lift, pitch, toe) for one foot. See defaults in `DEFAULT` for parameters.
    g['angle_blur'] > 0 blurs pitch and toe over neighbouring frames (Gaussian sigma in frames, periodic);
    the ball path is untouched, so a planted foot still pivots on the same contact point."""
    sigma = g.get('angle_blur', 0.0)
    y, lift, pitch, toe = _foot_cycle(phase, g)
    if sigma <= 0.0:
        return y, lift, pitch, toe
    n = int(math.ceil(2.5*sigma))
    wsum = ps = ts = 0.0
    for k in range(-n, n+1):
        w = math.exp(-0.5*(k/sigma)**2)
        _, _, pk, tk = _foot_cycle(phase+k/g['cycle'], g)
        wsum += w
        ps += w*pk
        ts += w*tk
    return y, lift, ps/wsum, ts/wsum


def _foot_cycle(phase, g):
    phase %= 1.0
    L = contact_length(g)
    y_front = g['y_offset']-g['front_bias']*L
    y_back = y_front+L
    st = g['stance']
    if phase < st:
        u = phase/st
        y = lerp(y_front, y_back, u)
        lift = 0.0
        pitch = lerp(g['contact_pitch'], 0.0, ramp(u, 0.0, 0.25))+g['pushoff']*ramp(u, g['heel_off'], 1.0)**1.5
        toe = 0.0
    else:
        u = (phase-st)/(1-st)
        # Swing: foot stays back while it lifts, then drives forward and reaches before contact.
        # Cubic Hermite that lands already moving back at contact_match x the stance slide speed,
        # so the foot doesn't stop dead at touch-down and then snap backward.
        d = g['swing_delay']
        s = clamp((u-d)/(1-d))
        slide = L/st*(1-st)*(1-d)
        m0 = g['release_match']*slide
        m1 = g['contact_match']*slide
        y = (y_back*(2*s**3-3*s**2+1)+m0*(s**3-2*s**2+s)+y_front*(-2*s**3+3*s**2)+m1*(s**3-s**2))
        peak = g['lift_peak']
        a = math.log(0.5)/math.log(peak)
        lift = g['lift']*math.sin(math.pi*clamp(u)**a)**g['lift_ease']
        # Settle a little lower at the end of swing so the ball lands softly.
        pitch = (lerp(g['pushoff'], g['swing_pitch'], ramp(u, 0.0, 0.35))*(1-ramp(u, 0.45, 0.95))
                 + g['contact_pitch']*ramp(u, 0.45, 0.95))
        toe = g['toe_trail']*ramp(u, 0.0, 0.35)*(1-ramp(u, 0.35, 0.75))
        toe -= g['toe_reach']*ramp(u, 0.6, 0.9)*(1-ramp(u, 0.92, 1.0))
    return y, lift, pitch, toe


DEFAULT = dict(
    speed=6.0,          # m/s the planted foot slides back at
    cycle=16,           # frames per full cycle (two steps)
    stance=0.25,        # fraction of the cycle a foot is planted
    front_bias=0.45,    # share of the contact length in front of the hip
    y_offset=0.0,       # shift of the whole footprint (+ = behind)
    lift=0.30,          # swing height of the ball (m)
    lift_peak=0.35,     # when the swing height peaks (fraction of swing)
    swing_delay=0.08,   # fraction of swing before the foot starts forward
    pushoff=48.0,       # heel-up pitch at toe-off (deg)
    heel_off=0.35,      # fraction of stance when the heel starts lifting
    swing_pitch=30.0,   # pitch while swinging (deg)
    contact_pitch=6.0,  # pitch at touch-down (deg)
    toe_trail=25.0,     # toe tip-down angle after toe-off (deg)
    toe_reach=8.0,      # toe tip-up just before contact (deg)
    contact_match=0.5,  # share of the stance slide speed the swinging foot has at touch-down
    release_match=0.0,  # share of it the foot keeps at toe-off (use with swing_delay=0, else it holds first)
    lift_ease=1.0,      # >1 flattens the lift curve near the ground (keeps a moving foot low: skates)
)


def params(**over):
    g = dict(DEFAULT)
    g.update(over)
    return g
