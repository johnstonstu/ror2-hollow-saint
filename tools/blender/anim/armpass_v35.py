"""v35 Arc Bolt pass: palm push from the chest. Post-processes the saved v34 checkpoint (never re-bakes).

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/armpass_v35.py --
     art/anim/hollow-saint-anim-v35.blend
Stu's v0.6.3 playtest: the v34 Arc Bolt hurl swings up from the hip (underhand) and reads as slinging from the crotch.
New direction: PALM PUSH FROM THE CHEST. Only 'Arc Bolt left' / 'Arc Bolt right' change:
 - f1 and f20 stay exactly on the Idle combat ready pose (arms out at the sides, open claws), so every seam and the
   back-to-back alternation (ArcBoltState plays L/R alternately, retriggered each shot) are unchanged
 - f1-f4: the casting hand rises past the ribs into a chamber in front of the chest (no pause, no wind-up: the path
   itself is the lift), the fingers gather slightly, the palm turns to face forward
 - f5 (Bolt release, ArcBoltReleaseNormalizedTime 0.2105 = 4/19): arm ~89% extended, wrist at shoulder height and palm at chest/core height, in front of the
   body, slightly inward of the shoulder, wrist extended, fingers up and splayed, palm facing the target. The muzzle
   socket (projectile origin, 'MuzzleRight'/'MuzzleLeft') is moved from the index fingertip onto the palm centre for
   the push, so the bolt leaves the palm
 - f6 snap (extra reach, max splay), f7-f9 small recoil; f10-f12 the hand swings out to the side at chest level
   (IK), then f13-f20 eases down the side onto the ready pose in joint space (carrying the IK's speed, per-bone lag),
   so the return never passes down the front of the body
 - the wrist path is two-bone IK on the rest elbow hinge, with the pole taken from the ready arm's own hinge so the
   upper arm does not roll on the way up; the release pronation is split 45% forearm roll / 55% wrist, and the wrist
   turns by a local slerp from the ready hand (no roll flips)
 - the other arm counterbalances (small swing back, elbow flex); torso: small shoulder yaw into the push through
   spine/chest with the neck/head countering (UpperBody only; the arms-only layer carries the same chest-relative arm)
 - shoulder helpers re-solved per frame (rigfix half follow, as a delta on the ready pose); pauldrons are the direct
   padpass solve (clears the body), eased in and out of the ready pose's keys over the first and last two frames
Legs, pelvis, cloth, halo, frame counts and markers are untouched.
"""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Matrix, Quaternion, Vector

sys.path.insert(0, str(Path(__file__).parent))
import hs_anim as H
import handpose as HP
import padpass

H.require_background()
args = sys.argv[sys.argv.index('--')+1:]
OUTB = H.ROOT/args[0]
REPDIR = H.ROOT/'art/anim/wip/v35arms'
SRC = H.ROOT/'art/anim/hollow-saint-anim-v34.blend'
assert not OUTB.exists(), 'never overwrite ' + str(OUTB)
bpy.ops.wm.open_mainfile(filepath=str(SRC))
bpy.context.preferences.filepaths.save_version = 0
rig = bpy.data.objects[H.RIG]
for t in rig.animation_data.nla_tracks:
    t.mute = True
pb = rig.pose.bones
X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
REPORT = {'changed': [], 'release': {}}

DIG = ('index', 'middle', 'ring', 'little', 'thumb')
FING = {s: [f'{s} {d}.{i}' for d in DIG for i in (1, 2, 3)] for s in 'LR'}
ARMB = {s: [f'{s} upperarm', f'{s} forearm', f'{s} forearm twist', f'{s} hand', f'{s} shoulder', f'{s} scapula',
            f'{s} pauldron', f'{s} muzzle'] + FING[s] for s in 'LR'}
ARMS = ARMB['L'] + ARMB['R']
TORSO = ['spine', 'chest', 'neck', 'head']
S, SS = H.smooth, H.smoother


# ---------------------------------------------------------------- frame-dict helpers (as armpass_v34)
def fcurves(act):
    out = []
    for layer in act.layers:
        for strip in layer.strips:
            for cb in strip.channelbags:
                out += list(cb.fcurves)
    return out


def K(b, p, j):
    return (f'pose.bones["{b}"].{p}', j)


def getq(fr, b):
    return Quaternion([fr[K(b, 'rotation_quaternion', j)] for j in range(4)])


def setq(fr, b, q):
    for j in range(4):
        fr[K(b, 'rotation_quaternion', j)] = q[j]


def getv(fr, b, p='location'):
    return Vector([fr[K(b, p, j)] for j in range(3)])


def setv(fr, b, v, p='location'):
    for j in range(3):
        fr[K(b, p, j)] = v[j]


def qpow(q, t):
    if q.w < 0:
        q = -q
    s = math.sqrt(q.x*q.x+q.y*q.y+q.z*q.z)
    if s < 1e-12:
        return Quaternion()
    ang = 2*math.atan2(s, q.w)
    return Quaternion(Vector((q.x/s, q.y/s, q.z/s)), ang*t)


def qslerp(a, b, t):
    if a.dot(b) < 0:
        b = -b
    return a @ qpow(a.inverted() @ b, t)


def qang(a, b):
    return math.degrees(2*math.acos(min(1.0, abs(a.dot(b)))))


def clamp(x, a=0.0, b=1.0):
    return max(a, min(b, x))


def bump(u, c, w):
    return S(1-abs(u-c)/w) if abs(u-c) < w else 0.0


def pchip_keys(keys, x):
    xs = sorted(keys)
    return HP.pchip(xs, [keys[k] for k in xs], x)


class Clip:
    def __init__(self, title):
        self.title = title
        self.act = bpy.data.actions[H.PREFIX+title]
        self.info = json.loads(self.act['clip_json'])
        self.f0, self.f1 = self.info['frames']
        self.fcs = {(fc.data_path, fc.array_index): fc for fc in fcurves(self.act)}
        self.frames = [{k: fc.evaluate(f) for k, fc in self.fcs.items()} for f in range(self.f0, self.f1+1)]
        self.bones = sorted({k[0].split('"')[1] for k in self.fcs if k[0].endswith('rotation_quaternion')})

    @property
    def n(self):
        return len(self.frames)

    def save(self):
        for b in self.bones:
            prev = None
            for fr in self.frames:
                q = getq(fr, b)
                if prev is not None and q.dot(prev) < 0:
                    q = -q
                    setq(fr, b, q)
                prev = q
        for k, fc in self.fcs.items():
            kp = fc.keyframe_points
            kp.clear()
            kp.add(self.n)
            for i, fr in enumerate(self.frames):
                kp[i].co = (self.f0+i, fr[k])
                kp[i].interpolation = 'LINEAR'
            fc.update()
        self.act['clip_json'] = json.dumps(self.info)


# ---------------------------------------------------------------- rig access
class Poser:
    """Minimal poser for padpass.PadPass (no IK calibration, so the saved rig stays untouched)."""
    def __init__(self):
        self.rig, self.pb = rig, pb
        self.rest = {b.name: b.matrix_local.copy() for b in rig.data.bones}
        self.r3 = {n: m.to_3x3() for n, m in self.rest.items()}

    def update(self):
        bpy.context.view_layer.update()


P = Poser()
R3 = P.r3
GEO = HP.HandGeo(P)
ZSIGN = __import__('handfix').ZSIGN


def hand_quats(s, levels, thumb_lv, splay_k=0.0):
    """Finger/thumb local quats from the handpose LIBRARY; splay_k > 0 fans the fingers wider than rest."""
    out = {}
    k = (1.0 if s == 'L' else -1.0)*ZSIGN
    for d in HP.DIGITS:
        lv = levels[d] if isinstance(levels, dict) else levels
        conv = S((lv-HP.CONVERGE_RAMP[0])/(HP.CONVERGE_RAMP[1]-HP.CONVERGE_RAMP[0]))
        splay = (-HP.CONVERGE*conv+splay_k)*GEO.dir0[(s, d)]
        qs = GEO.build(None, s, d, HP.pose(d, lv), splay)
        for i in (1, 2, 3):
            out[f'{s} {d}.{i}'] = qs[i]
    fx, op, t2, t3 = HP.thumb_pose(thumb_lv)
    out[f'{s} thumb.1'] = HP.about(HP.X, fx) @ HP.about(HP.Z, k*op)
    for i, tgt in ((2, t2), (3, t3)):
        n = f'{s} thumb.{i}'
        out[n] = HP.about(HP.X, GEO.solve(n, Quaternion(), tgt))
    return out


def pose_from(fr, bones):
    for b in bones:
        p = pb[b]
        p.rotation_quaternion = getq(fr, b)
        p.location = getv(fr, b)
        p.scale = getv(fr, b, 'scale')


def rot3(b):
    return pb[b].matrix.to_3x3().normalized()


def basis_for(b, W3):
    """Local rotation that gives bone b the armature-space rotation W3 under its current (posed) parent."""
    par = rig.data.bones[b].parent
    rel = R3[par.name].inverted() @ R3[b]
    Pm = rot3(par.name)
    return ((Pm @ rel).inverted() @ W3).to_quaternion().normalized()


def world_rot_rest(b, G):
    """Local rotation for a world-axis (rest frame) rotation G applied on top of the bone's basis."""
    return (R3[b].inverted() @ G @ R3[b]).to_quaternion()


def frame_of(yv, nv, s):
    """Armature rotation of the hand bone whose Y points along yv and palm normal (GEO.normal) along nv."""
    nl = GEO.normal[s]
    yl = Y
    nlp = (nl-yl*nl.dot(yl)).normalized()
    Lm = Matrix((yl, nlp, yl.cross(nlp))).transposed()
    yw = yv.normalized()
    nw = (nv-yw*nv.dot(yw)).normalized()
    Wm = Matrix((yw, nw, yw.cross(nw))).transposed()
    return Wm @ Lm.inverted()


# ---------------------------------------------------------------- design (rest armature axes, -Y is forward)
REL = 5                        # 'Bolt release' marker; ArcBoltReleaseNormalizedTime 0.2105 = (5-1)/19
# wrist offset from the shoulder joint in chest-rest axes: (inward, forward, up) or ('ext', reach share, up)
WRIST_KEYS = {2: (-0.24, 0.08, -0.33), 3: (-0.10, 0.26, -0.14), 4: (0.0, 0.38, -0.04),
              5: ('ext', 0.89, 0.0), 6: ('ext', 0.95, 0.012), 7: ('ext', 0.92, 0.015),
              8: ('ext', 0.85, 0.03), 9: ('ext', 0.83, 0.02),
              # return: out to the side at chest level first (IK), then down the side (joint ease), never the front
              10: ('ext', 0.77, 0.005), 12: (-0.06, 0.30, -0.14)}
IK_END = 12                    # IK to here (arm out at the side, chest level); then joint-space ease onto the ready pose
RET_LAG = {'upperarm': 0.0, 'shoulder': 0.0, 'forearm': 0.5, 'forearm twist': 0.8, 'hand': 1.0, 'muzzle': 0.0}
ROLL_SHARE = 0.45              # share of the release pronation carried by the forearm roll (rest: the wrist)
ROLL_W = {1: 0.0, 2: 0.1, 3: 0.4, 4: 0.8, 5: 1.0, 9: 1.0, 10: 0.95, 18: 0.0, 19: 0.0}
HAND_W = {1: 0.0, 2: 0.12, 3: 0.45, 4: 0.82, 5: 1.0}   # wrist, local slerp ready -> release
TB_SHARE = 0.15                # forearm twist bone share of the wrist twist (skin)
PUSH_DIR = (0.06, 1.0)         # (inward, forward) of the release line: slightly in toward the centreline
POLE_PUSH = (-0.5, -0.25, -1.0)   # elbow down and out, slightly back (inward, forward, up)
POLE_W = {1: 0.0, 2: 0.35, 3: 0.75, 4: 1.0, 10: 1.0, 13: 0.6, 18: 0.0, 19: 0.0}
# hand: fingers direction / palm normal (inward, forward, up) per frame; frames 1-4 turn from the ready hand
FINGER_DIR = {3: (0.15, 0.75, 0.55), 4: (0.12, 0.50, 0.85), 5: (0.10, 0.30, 0.95), 6: (0.10, 0.16, 0.98),
              7: (0.10, 0.22, 0.97), 8: (0.08, 0.36, 0.93), 9: (0.06, 0.42, 0.905)}
PALM_N = {3: (0.10, 0.55, -0.80), 4: (0.12, 1.0, -0.35)}   # later frames: PALM_DEFAULT (palm to the target)
PALM_DEFAULT = (0.12, 1.0, 0.05)
# Optional per-side tip of the fingers forward (less wrist extension). Kept 0: the R forearm conductor carries 26%
# hand weight (L: 7%), so wrist extension sinks it ~8 mm into the forearm skin; 0.2 only buys 0.7 mm (see STATUS v35).
WRIST_EASE = {'L': 0.0, 'R': 0.0}
# fingers: (curl level, thumb level, extra splay, share of the library pose vs the ready hand)
FINGERS = {2: (12.0, 12.0, 0.0, 0.45), 3: (15.0, 14.0, 0.0, 0.85), 4: (2.0, 4.0, 0.15, 1.0),
           5: (-8.0, -4.0, 0.32, 1.0), 6: (-10.0, -4.0, 0.40, 1.0), 7: (-6.0, -2.0, 0.32, 1.0),
           8: (2.0, 2.0, 0.18, 1.0), 9: (6.0, 4.0, 0.10, 1.0)}
MUZZLE_W = {1: 0.0, 2: 0.0, 3: 0.4, 4: 0.9, 5: 1.0, 9: 1.0, 13: 0.0}
MUZZLE_FWD = 0.045             # m in front of the palm centre
RETURN_LAG = {'upperarm': 0.0, 'forearm': 0.7, 'forearm twist': 1.0, 'hand': 1.2, 'muzzle': 0.0,
              'shoulder': 0.0}
YAW_PEAK, LEAN_PEAK = 7.0, 2.0
YAW_SHARE = {'spine': 0.4, 'chest': 0.6, 'neck': -0.2, 'head': -0.6}
CORR_W = {1: 1.0, 2: 0.7, 3: 0.35, 4: 0.1, 5: 0.0, 11: 0.0, 18: 1.0, 19: 1.0}   # ready-pose arm roll vs the IK hinge
OFF_SWING, OFF_FLEX = 7.0, 6.0
FA_KEEP = 0.0                  # the ready pose's own forearm roll kept through the push (R forearm conductor)


def v3(t, s):
    """(inward, forward, up) -> rest armature vector for side s (R is at -X, so inward is +X)."""
    sg = 1.0 if s == 'R' else -1.0
    return Vector((sg*t[0], -t[1], t[2]))


def key_interp(keys, f):
    xs = sorted(keys)
    if f <= xs[0]:
        return keys[xs[0]]
    if f >= xs[-1]:
        return keys[xs[-1]]
    return HP.pchip(xs, [keys[k] for k in xs], f)


def body_curve(f):
    """0 at f1, peak through the release and snap, 0 again by f17 (torso yaw, off-arm counter)."""
    return S(clamp((f-1)/4.0))*(1-SS(clamp((f-7)/10.0)))


def lagged(f, lag):
    return SS(clamp((f-9-lag)/7.5))


def finger_lag(b):
    for j, d in enumerate(DIG):
        if f' {d}.' in b:
            return 1.6+0.2*j+0.2*(int(b[-1])-1)
    return None


def return_lag(b):
    fl = finger_lag(b)
    if fl is not None:
        return fl
    for k, v in RETURN_LAG.items():
        if b[2:] == k:
            return v
    return 0.0


def twist_y(q):
    if q.w < 0:
        q = -q
    return math.degrees(2*math.atan2(q.y, q.w))


# ---------------------------------------------------------------- solvers
def chest_rest3():
    """Target frame. Armature axes (not chest-relative): the ready-pose chest leans forward, so chest axes would
    aim the push down; the arm's local rotations still ride the chest on the arms-only layer."""
    return Matrix.Identity(3)


def rest_hinge(s):
    fd = R3[f'{s} forearm'].col[1]
    return fd.cross(Vector((0, 1, 0))).normalized()


def two_vec(b, yw, hw, hl):
    """Armature rotation for bone b: local Y -> yw, local hinge hl -> hw (orthogonalised)."""
    yl = Y
    hlp = (hl-yl*hl.dot(yl)).normalized()
    Lm = Matrix((yl, hlp, yl.cross(hlp))).transposed()
    yw = yw.normalized()
    hwp = (hw-yw*hw.dot(yw)).normalized()
    Wm = Matrix((yw, hwp, yw.cross(hwp))).transposed()
    return Wm @ Lm.inverted()


def solve_arm(s, W, pole):
    """Two-bone IK: set upperarm/forearm so the wrist (hand head) lands on W with the elbow toward pole."""
    ua, fa = f'{s} upperarm', f'{s} forearm'
    Sh = pb[ua].head.copy()
    a = rig.data.bones[ua].length
    bl = rig.data.bones[fa].length
    d = W-Sh
    dist = min(d.length, a+bl-1e-4)
    u = d.normalized()
    ca = clamp((a*a+dist*dist-bl*bl)/(2*a*dist), -1, 1)
    p = (pole-u*u.dot(pole)).normalized()
    E = Sh+u*a*ca+p*a*math.sqrt(1-ca*ca)
    Wr = Sh+u*dist
    n = (E-Sh).cross(Wr-E)
    hw = -n.normalized()
    h = rest_hinge(s)
    pb[ua].rotation_quaternion = basis_for(ua, two_vec(ua, E-Sh, hw, R3[ua].inverted() @ h))
    P.update()
    pb[fa].rotation_quaternion = basis_for(fa, two_vec(fa, Wr-E, hw, R3[fa].inverted() @ h))
    P.update()
    return E, Wr


HELPER_CON = 'HS shoulder half follow'


def solve_helper(s):
    b = f'{s} shoulder'
    cons = [c for c in pb[b].constraints if c.type == 'COPY_ROTATION']
    if not cons:
        return pb[b].rotation_quaternion.copy()
    saved = [c.mute for c in cons]
    keep = pb[b].rotation_quaternion.copy()
    pb[b].rotation_quaternion = Quaternion()
    for c in cons:
        c.mute = False
    P.update()
    q = basis_for(b, rot3(b))
    for c, m in zip(cons, saved):
        c.mute = m
    pb[b].rotation_quaternion = keep
    P.update()
    return q


def arm_ik_off():
    for s in 'LR':
        for bone in (f'{s} forearm', f'{s} hand'):
            for c in pb[bone].constraints:
                if c.type in {'IK', 'COPY_ROTATION'} and not c.mute and c.influence > 0:
                    print('ARM CONSTRAINT ACTIVE', bone, c.name, c.influence, flush=True)
                    raise SystemExit('arm constraint active in the checkpoint; FK authoring would be overridden')


# ---------------------------------------------------------------- authoring
PADS = padpass.PadPass(P)


def read_basis(fr, bones):
    for b in bones:
        setq(fr, b, pb[b].rotation_quaternion.copy())
        setv(fr, b, pb[b].location.copy())
        setv(fr, b, pb[b].scale.copy(), 'scale')


def solve_followers():
    """(helper quats, pad (quat, loc)) for both sides at the current pose."""
    hq = {x: solve_helper(x) for x in 'LR'}
    PADS.apply()
    pads = {x: (pb[f'{x} pauldron'].rotation_quaternion.copy(), pb[f'{x} pauldron'].location.copy()) for x in 'LR'}
    return hq, pads


def author(title, s):
    c = Clip(title)
    o = 'L' if s == 'R' else 'R'
    sg = 1.0 if s == 'R' else -1.0
    IC = dict(c.frames[0])
    rig.animation_data.action = None
    allb = c.bones
    ua, fa, tw, hd, mz = (f'{s} {k}' for k in ('upperarm', 'forearm', 'forearm twist', 'hand', 'muzzle'))
    # ready-pose geometry (chest-rest axes)
    pose_from(IC, allb)
    P.update()
    C3 = chest_rest3()
    Sh0 = pb[ua].head.copy()
    E0, W0 = pb[fa].head.copy(), pb[hd].head.copy()
    u0 = (W0-Sh0).normalized()
    pe = (E0-Sh0)-u0*u0.dot(E0-Sh0)
    pole0 = (C3.inverted() @ pe).normalized()
    off0 = C3.inverted() @ (W0-Sh0)
    H0c = (C3.inverted() @ rot3(hd)).to_quaternion()
    La = rig.data.bones[ua].length+rig.data.bones[fa].length
    ref_h, ref_p = solve_followers()
    # wrist key path
    dirv = v3((PUSH_DIR[0], PUSH_DIR[1], 0.0), s).normalized()
    wk = {1: off0}
    for f, t in WRIST_KEYS.items():
        wk[f] = dirv*t[1]*La+Vector((0, 0, t[2])) if t[0] == 'ext' else v3(t, s)
    hand_c = {}
    for f, fd in FINGER_DIR.items():
        fd = (fd[0], fd[1]+(WRIST_EASE[s] if f >= REL else 0.0), fd[2])
        hand_c[f] = frame_of(v3(fd, s), v3(PALM_N.get(f, PALM_DEFAULT), s), s).to_quaternion()
    IC_h, IC_tw, IC_fa = getq(IC, hd), getq(IC, tw), getq(IC, fa)
    # IK of the ready pose itself: what the rest-hinge solve misses there (a roll) is blended back near the ends
    pose_from(IC, allb)
    P.update()
    # pole from the ready arm's own hinge (its elbow bend is only ~15 deg, so the elbow plane is ill-defined):
    # the IK then reproduces the ready upper-arm roll instead of rolling it on the way up
    hw0 = rot3(ua) @ (R3[ua].inverted() @ rest_hinge(s))
    hw0 = (hw0-u0*u0.dot(hw0)).normalized()
    pole0 = hw0.cross(u0).normalized()
    solve_arm(s, Sh0+off0, pole0)
    d_ua = pb[ua].rotation_quaternion.inverted() @ getq(IC, ua)
    d_fa = pb[fa].rotation_quaternion.inverted() @ IC_fa
    print('READY IK RESIDUAL', title, 'ua', round(qang(Quaternion(), d_ua), 2), 'swing', round(
        HP.twist_x(d_ua), 2), 'fa', round(qang(Quaternion(), d_fa), 2), flush=True)

    def place(f, fr):
        """Legs/cloth as v34, torso yaw, off-arm counter and the casting arm's IK (no hand yet) for frame f."""
        pose_from(fr, allb)
        pose_from(IC, TORSO+ARMS)
        bc = body_curve(f)
        for b in TORSO:
            G = H.R(z=sg*YAW_PEAK*YAW_SHARE[b]*bc) @ H.R(x=LEAN_PEAK*bc*(0.5 if b in ('spine', 'chest') else 0.0))
            pb[b].rotation_quaternion = world_rot_rest(b, G) @ getq(IC, b)
        P.update()
        pb[f'{o} upperarm'].rotation_quaternion = world_rot_rest(f'{o} upperarm', H.R(x=OFF_SWING*bc)) @ \
            getq(IC, f'{o} upperarm')
        pb[f'{o} forearm'].rotation_quaternion = world_rot_rest(
            f'{o} forearm', H.axis_rot(rest_hinge(o), -OFF_FLEX*body_curve(f-1))) @ getq(IC, f'{o} forearm')
        P.update()
        if f <= IK_END:
            vec = Vector([key_interp({k: v[j] for k, v in wk.items()}, f) for j in range(3)])
            pw = key_interp(POLE_W, f)
            pole = (pole0*(1-pw)+v3(POLE_PUSH, s).normalized()*pw).normalized()
            solve_arm(s, pb[ua].head+vec, pole)
            cw = key_interp(CORR_W, f)
            if cw > 0 or FA_KEEP > 0:   # the ready pose is not a pure rest hinge: blend its own upper-arm/forearm roll back in
                pb[ua].rotation_quaternion = pb[ua].rotation_quaternion @ qpow(d_ua, cw)
                P.update()
                pb[fa].rotation_quaternion = pb[fa].rotation_quaternion @ qpow(d_fa, cw) @ HP.about(Y, FA_KEEP*(1-cw)*twist_y(d_fa))
                P.update()

    # release geometry first: the pronation the palm push needs, shared between forearm roll and wrist
    place(REL, c.frames[REL-c.f0])
    raw = basis_for(hd, hand_c[REL].to_matrix())
    r5 = ROLL_SHARE*(twist_y(raw)-twist_y(IC_h))
    fa_q = pb[fa].rotation_quaternion.copy()
    pb[fa].rotation_quaternion = fa_q @ HP.about(Y, r5)
    P.update()
    rel_h = basis_for(hd, hand_c[REL].to_matrix())
    rec9 = rec_end = rec_pre = None
    report = {'forearm_roll_deg': round(r5, 1)}
    for i, fr in enumerate(c.frames):
        f = c.f0+i
        if f == c.f0:
            continue
        place(f, fr)
        if f <= IK_END:
            # forearm roll (share of the pronation) ramps in with the lift and out on the return
            pb[fa].rotation_quaternion = pb[fa].rotation_quaternion @ HP.about(Y, r5*key_interp(ROLL_W, f))
            P.update()
            if f < REL:     # local slerp from the ready wrist: extension and pronation spread evenly, no flips
                qh = qslerp(IC_h, rel_h, key_interp(HAND_W, f))
            elif f <= 9:
                qh = basis_for(hd, hand_c[f].to_matrix())
            else:
                qh = qslerp(rec9[hd][0], IC_h, SS(clamp((f-9-1.2)/7.5)))
            pb[hd].rotation_quaternion = qh
            dt = twist_y(qh)-twist_y(IC_h)
            pb[tw].rotation_quaternion = IC_tw @ HP.about(Y, TB_SHARE*dt)
            if f <= 9:
                lv, tlv, spl, share = FINGERS[f]
                lib = hand_quats(s, lv, tlv, spl)
                for b in FING[s]:
                    pb[b].rotation_quaternion = qslerp(getq(IC, b), lib[b], share)
            else:
                for b in FING[s]:
                    pb[b].rotation_quaternion = qslerp(rec9[b][0], getq(IC, b), lagged(f, finger_lag(b)))
            P.update()
            # muzzle socket onto the palm, in front of it
            mw = key_interp(MUZZLE_W, f)
            if f > 9:
                pb[mz].location = rec9[mz][1].lerp(getv(IC, mz), lagged(f, 0.0))
            elif mw > 0:
                pc = (pb[hd].head+pb[f'{s} middle.1'].head)*0.5
                pn = (rot3(hd) @ GEO.normal[s]).normalized()
                par = rig.data.bones[mz].parent.name
                rel4 = P.rest[par].inverted() @ P.rest[mz]
                M = pb[par].matrix @ rel4
                full = M.inverted() @ (pc+pn*MUZZLE_FWD)
                pb[mz].location = getv(IC, mz).lerp(full, mw)
            P.update()
            if f in (REL, 9, IK_END-1, IK_END):
                snap = {b: (pb[b].rotation_quaternion.copy(), pb[b].location.copy()) for b in ARMB[s]}
                if f == 9:
                    rec9 = snap
                if f == IK_END:
                    rec_end = snap
                if f == IK_END-1:
                    rec_pre = snap
                if f == REL:
                    report = {'frame': f, 'twist_deg': round(dt, 1),
                              'wrist': [round(x, 3) for x in pb[hd].head],
                              'palm_centre': [round(x, 3) for x in (pb[hd].head+pb[f'{s} middle.1'].head)*0.5],
                              'muzzle': [round(x, 3) for x in pb[mz].head],
                              'shoulder': [round(x, 3) for x in pb[ua].head],
                              'core_socket': [round(x, 3) for x in pb['core socket'].head],
                              'pelvis': [round(x, 3) for x in pb['pelvis'].head],
                              'palm_normal': [round(x, 3) for x in (rot3(hd) @ GEO.normal[s]).normalized()],
                              'fingers_dir': [round(x, 3) for x in rot3(hd).col[1]]}
        else:
            for b in ARMB[s]:
                if b.endswith(('shoulder', 'scapula', 'pauldron')):
                    continue
                lag = finger_lag(b) if finger_lag(b) is not None else RET_LAG.get(b[2:], 0.0)
                u = clamp((f-IK_END-lag)/(c.f1-1-IK_END-lag))
                q0, l0 = rec_end[b]
                # carry the IK's last per-frame rotation on (decaying) while easing onto the ready pose
                vel = qpow(q0 @ rec_pre[b][0].inverted(), 1.0)
                ext = qpow(vel, (f-IK_END)*(1-0.5*u)) @ q0
                w = SS(u)
                pb[b].rotation_quaternion = qslerp(qslerp(q0, getq(IC, b), w), qslerp(ext, getq(IC, b), w), 1-w)
                pb[b].location = l0.lerp(getv(IC, b), w)
            P.update()
        # followers: helper + pads as deltas on the ready pose
        hq, pads = solve_followers()
        for x in 'LR':
            hb, pbn = f'{x} shoulder', f'{x} pauldron'
            pb[hb].rotation_quaternion = getq(IC, hb) @ ref_h[x].inverted() @ hq[x]
            # pads: the direct padpass solve (it clears the body), eased in/out of the ready pose's keys
            we = S(clamp((f-c.f0)/2.0))*S(clamp((c.f1-f)/2.0))
            pb[pbn].rotation_quaternion = qslerp(getq(IC, pbn), pads[x][0], we)
            pb[pbn].location = getv(IC, pbn).lerp(pads[x][1], we)
        P.update()
        read_basis(fr, TORSO+ARMS)
        if f == c.f1:
            for b in TORSO+ARMS:
                for p, n in (('location', 3), ('rotation_quaternion', 4), ('scale', 3)):
                    for j in range(n):
                        fr[K(b, p, j)] = IC[K(b, p, j)]
    c.info.setdefault('accents', [])
    c.info['accents'] = sorted(set(c.info['accents']) | {3, 4, 5, 6})
    c.info['finger_accents'] = sorted(set(c.info.get('finger_accents', [])) | {3, 4, 5, 6})
    c.info['v35'] = 'palm push from the chest (armpass_v35)'
    c.save()
    REPORT['changed'].append(title)
    REPORT['release'][title] = report
    print('AUTHORED', title, json.dumps(report), flush=True)


arm_ik_off()
for title, side in (('Arc Bolt right', 'R'), ('Arc Bolt left', 'L')):
    author(title, side)
rig.animation_data.action = None
OUTB.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUTB))
REPDIR.mkdir(parents=True, exist_ok=True)
(REPDIR/'armpass-report.json').write_text(json.dumps(REPORT, indent=1), encoding='utf-8')
print('ARMPASS', json.dumps(REPORT), flush=True)
print('ARMPASS DONE', flush=True)
