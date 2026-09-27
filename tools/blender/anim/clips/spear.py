"""Conduit Spear (item 11): a right-palm thrown lance, the left arm pointing at the target as an aim guide.

Upper-body gesture (RoR2 avatar mask without pelvis/legs, like `special`): only spine/chest/neck/head, scapulae,
arms, fingers and halo move; legs_ik=0. 20 frames (0.83 s), first and last frame exactly at bind rest.
Beats (REFINEMENT-PLAN 11):
- f1-6 anticipation: the chest winds back to the right, the R hand draws up beside the shoulder while the lance
  materializes along the R forearm (`hs_spear` 0 -> 1, core flash f6); the L arm rises to point at the target.
- f7 release: the chest turns through, the R arm drives forward and the lance leaves the open palm (thumb up,
  palm forward/in per 9f). The elbow leads on f6 so the whip spans two frames.
- f7-11 follow-through: the R arm carries on down and across, the L arm pulls in to the ribs.
- f12-20 recovery to rest.
Markers: Materialize f2, Draw f5, Spear release f7, Cancel f11 (Arc Bolt / Arc Step / jump / sprint), Fade f14
(any-state fade), Recovered f20. Locked f1-7.

Poses are solved with `special.solve` in the rest-torso frame: each world target is counter-rotated by the torso
yaw at that key, so the arm lands on it once the chest has turned.
"""
import math

from mathutils import Matrix, Vector

from hs_anim import bake, lerp, ramp, smoother
import special as S

N = 20
MATERIALIZE, DRAW, LEAD, RELEASE, CANCEL, FADE = 2, 5, 6, 7, 11, 14
TARGET = Vector((0.0, -1.0, 0.0))
PROJECTILE_MPS = 150.0

# Torso yaw (spine_z + chest_z, + = toward the character's left) and pitch (+ forward) per key; the spine takes
# 35% of each, the chest the rest. The neck/head counter-turn keeps the face on the target.
TORSO = {1: (0.0, 0.0), 3: (-12.0, -2.0), DRAW: (-22.0, -5.0), LEAD: (-18.0, -3.0), RELEASE: (12.0, 7.0),
         9: (18.0, 9.0), 12: (10.0, 5.0), 16: (3.0, 1.5), N: (0.0, 0.0)}
HEAD_KEEP = 0.85            # share of the torso yaw the neck/head give back (face stays on the target)
SPINE_SHARE = 0.35


def yaw_m(deg):
    return Matrix.Rotation(math.radians(deg), 3, 'Z')


def to_rest(v, yaw):
    """World direction -> rest-torso direction for a torso turned by `yaw`."""
    return yaw_m(-yaw) @ Vector(v)


def reach_target(p, side, direction, yaw, reach, fingers=None):
    """Knuckle target for an arm reaching along world `direction` from its (turned) shoulder."""
    d = to_rest(direction, yaw).normalized()
    f = to_rest(fingers, yaw).normalized() if fingers else d
    w = p.rest[f'{side} upperarm'].translation+d*reach*S.ARM_LEN[side]
    return w+f*S.HAND_LEN[side], f


def solve_one(name, p, side, knuckle, fingers, normal, yaw, elbow=None, straight=False, avoid=False):
    n = to_rest(normal, yaw)
    e = None
    if elbow is not None:
        sh = p.rest[f'{side} upperarm'].translation
        e = sh+to_rest(elbow, yaw)
    a, rep = S.solve(p, side, knuckle, fingers, n, e, straight, ortho=True, avoid=avoid)
    S.SOLVED[name] = {'arms': {side: a}, 'fit': {side: rep}}
    print('SPEAR solve', name, side, a, rep, flush=True)
    return a


POSE = {}
DRAW_KNUCKLE = (-0.28, -0.04, 0.44)     # from the R shoulder, world axes before the torso turn
DRAW_ELBOW = (-0.32, 0.06, 0.02)
DRAW_FINGERS = (0.05, -0.62, 0.78)
FOLLOW = dict(swing=-38.0, adduct=24.0, elbow=30.0)
LEAD_MIX = 0.35
LEAD_FOREARM = 0.3
TUCK = dict(swing=-30.0, adduct=18.0, elbow=100.0, wx=5.0, wy=0.0, wz=0.0)


def solve_poses(p):
    S.calibrate_hands(p)
    yaw = lambda f: TORSO[f][0]
    # Draw: hammer grip beside the ear, the lance pointing forward along the fingers: elbow out at shoulder
    # height, forearm up, palm in toward the head, thumb up.
    sh = p.rest['R upperarm'].translation
    k = sh+to_rest(DRAW_KNUCKLE, yaw(DRAW))
    POSE['draw'] = solve_one('draw', p, 'R', k, to_rest(DRAW_FINGERS, yaw(DRAW)).normalized(),
                             (1.0, 0.0, 0.1), yaw(DRAW), elbow=DRAW_ELBOW)
    # Release: R arm driving forward along the throw line, slightly up; fingers tip up off the palm, palm in/forward.
    k, fd = reach_target(p, 'R', (0.12, -1.0, 0.10), yaw(RELEASE), 0.95, fingers=(0.10, -0.94, 0.32))
    POSE['release'] = solve_one('release', p, 'R', k, fd, (0.75, -0.55, 0.1), yaw(RELEASE), straight=True)
    # Follow-through: the released arm carries on down in front in joint space, keeping the release grip (thumb up,
    # palm in). A solved down-and-across pose put the hand on the other forearm-roll branch of the 9f check, and
    # the hand pass flipped the forearm ~150 deg between f9 and f10.
    f = dict(POSE['release'])
    f.update(FOLLOW)
    POSE['follow'] = f
    # Aim guide: L arm straight at the target, palm in/forward, thumb up.
    k, fd = reach_target(p, 'L', (-0.08, -1.0, 0.08), yaw(DRAW), 0.96)
    POSE['point'] = solve_one('point', p, 'L', k, fd, (-0.75, -0.6, 0.0), yaw(DRAW), straight=True)
    # L arm pulled in: the pointing arm folds at the elbow and drops to the ribs (joint space keeps the forearm roll
    # of the point, so the hand doesn't spin on the way in).
    t = dict(POSE['point'])
    t.update(TUCK)
    POSE['tuck'] = t


def arm_state(side, pose):
    return {f'{side}_{k}': v for k, v in pose.items()}


def keys():
    rest = S.rest_state()

    def st(r=None, l=None, rh=None, lh=None, **kw):
        s = dict(rest)
        if r is not None:
            s.update(r)
        if l is not None:
            s.update(l)
        for side, h in (('R', rh), ('L', lh)):
            if h:
                s[f'{side}_curl'], s[f'{side}_thumb'], s[f'{side}_splay'] = h
        s.update(kw)
        return s

    def mixed(side, a, b, t):
        pa = {k: rest[f'{side}_{k}'] for k in S.ARM} if a is None else a
        pb = {k: rest[f'{side}_{k}'] for k in S.ARM} if b is None else b
        return {f'{side}_{k}': lerp(pa.get(k, 0.0), pb.get(k, 0.0), t) for k in S.ARM}

    R_ = lambda pose: arm_state('R', pose)
    L_ = lambda pose: arm_state('L', pose)
    # The elbow leads: on f6 the upper arm is already half way to the release while the forearm stays cocked.
    lead = dict(POSE['draw'])
    for c in ('swing', 'adduct'):
        lead[c] = lerp(POSE['draw'][c], POSE['release'][c], LEAD_MIX)
    for c in ('ftwist', 'elbow', 'wx', 'wy', 'wz'):     # the forearm starts to roll and open with the elbow lead
        lead[c] = lerp(POSE['draw'][c], POSE['release'][c], LEAD_FOREARM)
    rel2 = dict(POSE['release'])
    rel2['elbow'] = POSE['release']['elbow']+4.0
    rel2['swing'] = POSE['release']['swing']+6.0
    return {
        1: rest,
        3: st(mixed('R', None, POSE['draw'], 0.55), mixed('L', None, POSE['point'], 0.5), rh=(24, 14, 4),
              lh=(14, 10, 2)),
        DRAW: st(R_(POSE['draw']), L_(POSE['point']), rh=(26, 16, 6), lh=(6, 8, 4)),
        LEAD: st(R_(lead), L_(POSE['point']), rh=(22, 14, 8), lh=(6, 8, 4)),
        RELEASE: st(R_(POSE['release']), L_(POSE['point']), rh=(4, 6, 14), lh=(8, 9, 4)),
        8: st(R_(rel2), mixed('L', POSE['point'], POSE['tuck'], 0.35), rh=(3, 5, 14), lh=(14, 12, 3)),
        10: st(mixed('R', POSE['release'], POSE['follow'], 0.85), mixed('L', POSE['point'], POSE['tuck'], 0.9),
               rh=(10, 9, 10), lh=(30, 18, 1)),
        12: st(R_(POSE['follow']), L_(POSE['tuck']), rh=(14, 10, 6), lh=(32, 19, 0)),
        16: st(mixed('R', POSE['follow'], None, 0.6), mixed('L', POSE['tuck'], None, 0.6), rh=(18, 11, 2),
               lh=(26, 15, 0)),
        N: rest,
    }


def halo_kick(f):
    """Small pop up/back and a segment flare on the release, ringing down."""
    x = f-RELEASE+1
    return S.kick(x, freq=2.0, decay=3.5)*(1.0-ramp(f, N-5, N))


def pose(p, f):
    s = S.keyed(f, KEYS)
    yaw = S.track(f, {k: v[0] for k, v in TORSO.items()})
    pitch = S.track(f, {k: v[1] for k, v in TORSO.items()})
    late = S.track(f-1.5, {k: v[0] for k, v in TORSO.items()})     # the head's counter-turn trails a little
    head = lerp(late, yaw, ramp(f, N-6, N))
    s['spine_z'], s['chest_z'] = SPINE_SHARE*yaw, (1-SPINE_SHARE)*yaw
    s['spine_x'], s['chest_x'] = SPINE_SHARE*pitch, (1-SPINE_SHARE)*pitch
    s['neck_z'], s['head_z'] = -0.4*HEAD_KEEP*head, -0.6*HEAD_KEEP*head
    s['neck_x'], s['head_x'] = -0.3*pitch, -0.4*pitch
    # the torso side-bends away from the throw as it turns through
    s['chest_y'] = -0.12*yaw
    kk = halo_kick(f)
    s['h_oy'] += 0.012*kk
    s['h_oz'] += 0.010*kk
    s['h_flare'] += 7.0*kk
    s['h_rx'] += -3.0*kk
    S.apply(p, s)


def materialize(f):
    if f >= RELEASE:
        return 0.0
    return smoother((f-MATERIALIZE+1)/(LEAD-MATERIALIZE+1))


def glow(f):
    return 0.5*math.exp(-((f-LEAD-0.5)/1.4)**2)


def post(p, caps, frames):
    """Release aim: the R hand's travel through the release (the lance's launch velocity) against the target."""
    out = S.gesture_post(False)(p, caps, frames)
    k = lambda f: caps[f]['R middle.1'].translation
    v = (k(RELEASE+1)-k(RELEASE-1))*(24/2)
    fwd = k(RELEASE)-caps[RELEASE]['R hand'].translation
    out.update({'spear_release_pos': [round(c, 3) for c in k(RELEASE)],
                'spear_hand_speed_mps': round(v.length, 2),
                'spear_velocity_angle_to_target_deg': round(math.degrees(v.angle(TARGET)), 1),
                'spear_fingers_angle_to_target_deg': round(math.degrees(fwd.angle(TARGET)), 1)})
    return out


KEYS = {}


def build(p):
    global KEYS
    S.arc_geometry()
    solve_poses(p)
    KEYS = keys()
    markers = {'Materialize': MATERIALIZE, 'Draw': DRAW, 'Spear release': RELEASE, 'Cancel': CANCEL, 'Fade': FADE,
               'Recovered': N}
    meta = {'kind': 'gesture', 'layer': 'upper body (mask excludes pelvis/legs)', 'hand': 'R',
            'accents': [LEAD, RELEASE], 'finger_accents': [RELEASE],
            'cancel': {'locked': [1, RELEASE], 'skills_jump_sprint_from': CANCEL, 'any_state_from': FADE},
            'projectile_mps': PROJECTILE_MPS, 'launch': 'R palm (R middle.1 knuckle, along the hand velocity)',
            'arm_solves': {k: v for k, v in S.SOLVED.items()}}
    act, info = bake(p, 'Conduit Spear', list(range(1, N+1)), pose, False, markers=markers, meta=meta, post=post,
                     legs_ik=0.0, props={'hs_spear': materialize, 'hs_glow': glow})
    return [(act, info)]
