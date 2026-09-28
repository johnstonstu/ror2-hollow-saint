"""Shared toolkit for Hollow Saint clip authoring (isolated background Blender only).

Clips are procedural pose functions evaluated per frame with the rig's leg/arm IK switched on,
then baked to FK on the regular bones (IK influence back to 0). Rotations given to `Poser.rot`
are world axes in the rest frame, applied relative to the parent (character faces -Y):
  +X pitches a hanging/upright chain so its tip swings back (legs, arms, tabard) or leans the
  spine forward; +Z yaws toward the character's left (+X side); +Y tilts the top toward +X.
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion, Euler

ROOT = Path(__file__).resolve().parents[3]
START = ROOT/'art/hybrid/hollow-saint-hybrid-v18.blend'
RIG = 'Hollow Saint | v8 rig'
BODY = 'HF BODY | retained UV sculpt, corrected posterior'
FPS = 24
PREFIX = 'HS_anim | '
MID_X = -0.0375
GROUND_Z = 0.003
LEG_LENGTH = 0.881  # thigh + shin; hip-to-ankle distance at rest is 0.878 (nearly straight)
SIDES = ('L', 'R')


def require_background():
    if not bpy.app.background:
        raise RuntimeError('Use isolated blender --background --factory-startup only')


def open_start(path=START, fix=True):
    """Open v18 (never saved in place). fix applies the R arm refit (armfit.py, M6), the pauldron/halo
    placement fix (padfix.py, 9g), the shoulder corrective (rigfix.py), the hand chirality fix (handfix.py, 9f),
    the full-body fixes (bodyfix.py, 9i) and the VFX (vfx.py: heel thrust jets, emissive glow, keyed through root-bone properties)."""
    require_background()
    bpy.ops.wm.open_mainfile(filepath=str(path))
    bpy.context.preferences.filepaths.save_version = 0
    rig = bpy.data.objects[RIG]
    if rig.animation_data:
        rig.animation_data.action = None
    if fix:
        import armfit
        import padfix
        import rigfix
        import handfix
        import bodyfix
        import vfx
        print('ARMFIT', json.dumps(armfit.apply(rig)), flush=True)
        print('PADFIX', json.dumps(padfix.apply(rig)), flush=True)
        print('RIGFIX', json.dumps(rigfix.apply(rig)), flush=True)
        print('HANDFIX', json.dumps(handfix.apply(rig)), flush=True)
        print('BODYFIX', json.dumps(bodyfix.apply(rig)), flush=True)
        print('VFX', json.dumps(vfx.apply(rig)), flush=True)
    return Poser(rig)


# ----------------------------------------------------------------------------- math helpers
def clamp(x, a=0.0, b=1.0):
    return max(a, min(b, x))


def smooth(t):
    t = clamp(t)
    return t*t*(3-2*t)


def smoother(t):
    t = clamp(t)
    return t*t*t*(t*(6*t-15)+10)


def lerp(a, b, t):
    return a+(b-a)*t


def ramp(x, a, b):
    """0 before a, 1 after b, smooth between."""
    return smooth((x-a)/(b-a)) if b != a else float(x >= a)


def wave(t, phase=0.0, harmonic=1):
    return math.cos(2*math.pi*harmonic*(t-phase))


def chain_lag(fn, t, delays, gain=1.0):
    """Follow-through for a chain hanging off a body part whose angle is fn(t) (periodic in loop phase t).
    Returns per-segment local angles such that segment i's world angle trails fn by delays[i] (cumulative,
    in loop phase units), scaled by `gain`. A pure function of t, so derived clips keep exact seams."""
    out, prev = [], fn(t)
    for d in delays:
        cur = fn(t-d)
        out.append(gain*(cur-prev))
        prev = cur
    return out


TABARD_FRONT = ('tabard front.1', 'tabard front.2', 'tabard front.3')
TABARD_BACK = ('tabard back.1', 'tabard back.2')


def tabard_follow(yaw, roll, t, front=(0.05, 0.11, 0.18), back=(0.06, 0.14), gain=1.4):
    """Tabard lag behind the pelvis: {bone: (roll_deg, yaw_deg)} to add on top of a clip's own tabard pose.
    yaw/roll: pelvis world yaw (Z) and roll (Y) as functions of loop phase."""
    out = {}
    for names, delays in ((TABARD_FRONT, front), (TABARD_BACK, back)):
        ys = chain_lag(yaw, t, delays, gain)
        rs = chain_lag(roll, t, delays, gain)
        for n, r, y in zip(names, rs, ys):
            out[n] = (r, y)
    return out


def R(x=0.0, y=0.0, z=0.0):
    """World-axis rotation in degrees, applied X then Y then Z."""
    return Euler((math.radians(x), math.radians(y), math.radians(z)), 'XYZ').to_matrix()


def axis_rot(axis, deg):
    return Matrix.Rotation(math.radians(deg), 3, Vector(axis).normalized())


def sign(side):
    return 1 if side == 'L' else -1


# ----------------------------------------------------------------------------- poser
class Poser:
    def __init__(self, rig):
        self.rig = rig
        self.pb = rig.pose.bones
        self.bones = rig.data.bones
        self.rest = {b.name: b.matrix_local.copy() for b in self.bones}
        self.r3 = {n: m.to_3x3() for n, m in self.rest.items()}
        self.controls = [n for n in self.rest if n.endswith((' IK', ' pole'))]
        self.order = []
        def walk(b):
            self.order.append(b.name)
            for c in b.children:
                walk(c)
        for b in self.bones:
            if b.parent is None:
                walk(b)
        self.fk = [n for n in self.order if n not in self.controls]
        for p in self.pb:
            p.rotation_mode = 'QUATERNION'
        if rig.animation_data is None:
            rig.animation_data_create()
        self.foot_rest = {}
        for s in SIDES:
            self.foot_rest[s] = {'ankle': self.rest[f'{s} foot IK'].translation.copy(),
                                 'ball': self.rest[f'{s} toe'].translation.copy(),
                                 'pole': self.rest[f'{s} knee pole'].translation.copy(),
                                 'hip': self.rest[f'{s} thigh'].translation.copy()}
        self.pole_angles = self.calibrate_poles()
        self.rest_pole_shift = self.calibrate_rest_poles()

    def calibrate_rest_poles(self):
        """Lateral pole shift that makes IK reproduce the rest knee (the rest knee points slightly
        outward, the calibrated pole points it forward). Use via foot(..., rest_match=w)."""
        out = {}
        for s in SIDES:
            rest_knee = self.rest[f'{s} shin'].translation
            fr = self.foot_rest[s]
            def err(v):
                self.reset()
                self.ik(1.0, 0.0)
                self.foot(s, fr['ball'], 0, 0, pole_shift=(v[0], v[1], 0))
                self.update()
                return (self.pb[f'{s} shin'].matrix.translation-rest_knee).length
            best = min(((d*0.1, 0.0) for d in range(-40, 41)), key=err)
            for step in (0.05, 0.01, 0.002, 0.0005):
                for _ in range(3):
                    best = min(((best[0]+i*step, best[1]+j*step) for i in range(-4, 5) for j in range(-4, 5)),
                               key=err)
            out[s] = (Vector((best[0], best[1], 0)), round(err(best), 5))
        self.reset()
        self.ik(0.0, 0.0)
        self.update()
        return out

    def calibrate_poles(self):
        """Re-solve knee pole angles on a bent leg (the rest leg is nearly straight, so the v16
        angles are ill-conditioned) so the knee points straight at the pole."""
        out = {}
        for s in SIDES:
            c = next(c for c in self.pb[f'{s} shin'].constraints if c.type == 'IK')
            self.reset()
            self.ik(1.0, 0.0)
            fr = self.foot_rest[s]
            ball = fr['ball']+Vector((0, 0.10, 0.32))
            self.foot(s, ball, 20, 0)
            def score(deg):
                c.pole_angle = math.radians(deg)
                self.update()
                hip = self.pb[f'{s} thigh'].matrix.translation
                knee = self.pb[f'{s} shin'].matrix.translation
                ankle = self.pb[f'{s} foot'].matrix.translation
                pole = self.pb[f'{s} knee pole'].matrix.translation
                axis = (ankle-hip).normalized()
                k = knee-hip
                k -= axis*k.dot(axis)
                q = pole-hip
                q -= axis*q.dot(axis)
                return k.normalized().dot(q.normalized())
            best = max(range(-180, 180, 3), key=score)
            best = max((best+d*0.25 for d in range(-12, 13)), key=score)
            out[s] = (round(best, 2), round(score(best), 5))
            c.pole_angle = math.radians(best)
        self.reset()
        self.ik(0.0, 0.0)
        self.update()
        return out

    # -- state
    def reset(self):
        for p in self.pb:
            p.matrix_basis.identity()

    def ik(self, legs=1.0, arms=0.0):
        for s in SIDES:
            for bone, w in ((f'{s} shin', legs), (f'{s} foot', legs), (f'{s} forearm', arms), (f'{s} hand', arms)):
                for c in self.pb[bone].constraints:
                    if c.type in {'IK', 'COPY_ROTATION'}:
                        c.influence = w

    def followers(self, active):
        """Shoulder helper (rigfix) Copy Rotation of the upper arm. On while posing so captures include
        it, muted for baked playback: the keys already hold its effect, and Unity drops constraints.
        The pauldrons are driven by padpass.py instead (their Copy Rotation stays muted)."""
        for s in SIDES:
            for bone in (f'{s} shoulder',):
                if bone not in self.pb:
                    continue
                for c in self.pb[bone].constraints:
                    if c.type == 'COPY_ROTATION':
                        c.mute = not active

    def update(self):
        bpy.context.view_layer.update()

    # -- bone setters
    def rot(self, name, m3):
        """Rotate `name` by world-axis matrix m3 (rest frame) relative to its parent."""
        if not isinstance(m3, Matrix):
            m3 = R(*m3)
        r3 = self.r3[name]
        self.pb[name].rotation_quaternion = (r3.inverted() @ m3 @ r3).to_quaternion()

    def offset(self, name, vec):
        """Translate `name` by a world-axis vector (rest frame) relative to its parent."""
        self.pb[name].location = self.r3[name].inverted() @ Vector(vec)

    def place(self, name, m4):
        """Set an armature-space matrix on a bone whose parent is unposed (IK controls under root)."""
        self.pb[name].matrix_basis = self.rest[name].inverted() @ m4

    def curl(self, side, fingers=0.0, thumb=None, spread=0.0):
        for digit in ('index', 'middle', 'ring', 'little'):
            extra = {'index': -0.3, 'middle': 0.0, 'ring': 0.25, 'little': 0.5}[digit]
            for i in range(1, 4):
                amount = fingers*(1+extra*0.4)*(1.15 if i == 2 else 1.0)
                self.pb[f'{side} {digit}.{i}'].rotation_quaternion = Euler((math.radians(amount), 0, 0)).to_quaternion()
        t = fingers*0.5 if thumb is None else thumb
        for i in range(1, 4):
            self.pb[f'{side} thumb.{i}'].rotation_quaternion = Euler((math.radians(t), 0, 0)).to_quaternion()

    def foot(self, side, ball, pitch=0.0, toe=0.0, yaw=0.0, pole_shift=(0, 0, 0), rest_match=0.0):
        """Plant the foot IK by the ball joint.

        ball: world position of the ball joint (rest z 0.055 sits the toe on the ground).
        pitch: heel-up rotation about the ball (deg). toe: world toe angle (+ tips the toe down).
        yaw: rotation about Z (deg, + toward the character's left).
        rest_match: 0..1 blend of the pole shift that reproduces the rest knee (use 1 on frames
        that must equal the rest pose, fade toward 0 as the leg leaves rest).
        """
        if rest_match and hasattr(self, 'rest_pole_shift'):
            pole_shift = Vector(pole_shift)+self.rest_pole_shift[side][0]*rest_match
        fr = self.foot_rest[side]
        rw = R(z=yaw) @ R(x=pitch)
        ball = Vector(ball)
        ankle = ball+rw @ (fr['ankle']-fr['ball'])
        m = (rw @ self.r3[f'{side} foot IK']).to_4x4()
        m.translation = ankle
        self.place(f'{side} foot IK', m)
        # The pole rides with the hip-ankle midpoint, so the knee plane stays aligned with the leg.
        mid0 = (fr['hip']+fr['ankle'])*0.5
        pole = (fr['hip']+ankle)*0.5+R(z=yaw) @ (fr['pole']-mid0)+Vector(pole_shift)
        pm = self.rest[f'{side} knee pole'].copy()
        pm.translation = pole
        self.place(f'{side} knee pole', pm)
        self.rot(f'{side} toe', rw.inverted() @ R(z=yaw) @ R(x=toe))
        return ankle

    def arm(self, side, swing=0.0, adduct=0.0, elbow=0.0, twist=0.0, wrist=(0, 0, 0), forearm_twist=0.0):
        """FK arm. swing: + back; adduct: + toward the body; elbow: + flexes the forearm forward.
        twist / forearm_twist roll the upper arm / forearm about their own axes (mirrored per side)."""
        s = sign(side)
        up = self.r3[f'{side} upperarm'].col[1]
        self.rot(f'{side} upperarm', R(x=swing) @ R(y=s*adduct) @ axis_rot(up, s*twist))
        fd = self.r3[f'{side} forearm'].col[1]
        hinge = fd.cross(Vector((0, 1, 0)))
        self.rot(f'{side} forearm', axis_rot(hinge, -elbow) @ axis_rot(fd, s*forearm_twist))
        self.rot(f'{side} hand', R(*wrist) if s > 0 else R(wrist[0], -wrist[1], -wrist[2]))

    def pelvis(self, offset=(0, 0, 0), m3=None):
        self.offset('pelvis', offset)
        if m3 is not None:
            self.rot('pelvis', m3)

    # -- evaluation
    def world(self, name):
        return self.pb[name].matrix.copy()

    def capture(self):
        self.update()
        return {n: self.pb[n].matrix.copy() for n in self.fk}

    def descendants(self, name):
        out = []
        def walk(b):
            out.append(b.name)
            for c in b.children:
                walk(c)
        walk(self.bones[name])
        return out


# ----------------------------------------------------------------------------- secondary motion
def spring_follow(points, loop, stiffness=90.0, damping=12.0, substeps=4, passes=3):
    """Critically-ish damped follow of a list of Vectors sampled at FPS; periodic when loop."""
    if loop and len(points) > 2 and (points[-1]-points[0]).length < 1e-6:
        # The repeated seam frame must not be integrated twice.
        out = spring_follow(points[:-1], True, stiffness, damping, substeps, max(passes, 6))
        return out+[out[0].copy()]
    dt = 1.0/FPS/substeps
    x = points[0].copy()
    v = Vector((0, 0, 0))
    out = None
    for _ in range(passes if loop else 1):
        out = []
        for i, p in enumerate(points):
            target_prev = points[i-1] if i > 0 else (points[-1] if loop and len(points) > 1 else p)
            for k in range(substeps):
                tgt = target_prev.lerp(p, (k+1)/substeps)
                a = stiffness*(tgt-x)-damping*v
                v += a*dt
                x += v*dt
            out.append(x.copy())
    return out


def apply_world_delta(poser, caps, bone, delta):
    for n in poser.descendants(bone):
        if n in caps:
            caps[n] = delta @ caps[n]


def soft_limit(o, max_offset):
    if o.length < 1e-9:
        return o.copy()
    return o*(max_offset*math.tanh(o.length/max_offset)/o.length)


HALO_TETHER_DOWN = 0.008   # m: soft limit of the halo lag's drop (lower arcs onto the pads)
HALO_TETHER_FWD = 0.008    # m: soft limit of its forward (-Y) lag


def halo_tether(o):
    """Soft-limit a halo offset's downward and forward components (both close the lower-arc/pad gap)."""
    o = o.copy()
    if o.z < 0:
        o.z = -HALO_TETHER_DOWN*math.tanh(-o.z/HALO_TETHER_DOWN)
    if o.y < 0:
        o.y = -HALO_TETHER_FWD*math.tanh(-o.y/HALO_TETHER_FWD)
    return o


def halo_offsets(pts, loop, max_offset=0.035, stiffness=70.0, damping=11.0, gain=0.5):
    """World offsets for the halo root given its chest-attached positions per frame (tethered, halo_tether)."""
    follow = spring_follow(pts, loop, stiffness, damping)
    return [halo_tether(soft_limit((q-p)*gain, max_offset)) for p, q in zip(pts, follow)]


def halo_lag(poser, caps_by_frame, frames, loop, max_offset=0.035, tilt_per_m=70.0, stiffness=70.0, damping=11.0,
             gain=0.5):
    """Halo root trails its chest-attached rest position (small follow-through), tipping away from travel.

    gain < 1 keeps only part of the spring's lag (a full-strength slow spring nearly pins the halo in
    world space); the length is soft-limited toward max_offset instead of clipped."""
    pts = [caps_by_frame[f]['halo root'].translation.copy() for f in frames]
    offsets = halo_offsets(pts, loop, max_offset, stiffness, damping, gain)
    report = 0.0
    capped = sum(1 for o in offsets if o.length > 0.9*max_offset*math.tanh(0.9))
    for f, p, o in zip(frames, pts, offsets):
        report = max(report, o.length)
        pivot = p
        horizontal = Vector((o.x, o.y, 0))
        tilt = Matrix.Identity(3)
        if horizontal.length > 1e-6:
            axis = Vector((0, 0, 1)).cross(horizontal).normalized()
            tilt = axis_rot(axis, -tilt_per_m*horizontal.length)
        d = Matrix.Translation(pivot+o) @ tilt.to_4x4() @ Matrix.Translation(-pivot)
        apply_world_delta(poser, caps_by_frame[f], 'halo root', d)
    halo_lag.capped_frames = capped
    if capped:
        print(f'HALO LAG capped on {capped}/{len(frames)} frames', flush=True)
    return report


# ----------------------------------------------------------------------------- baking
MOVE_DIRS = {'forward': (0.0, 1.0), 'backward': (0.0, -1.0), 'left': (-1.0, 0.0), 'right': (1.0, 0.0)}


def move_vector(meta):
    """Character-space travel direction (+x right, +y forward) for vfx hs_move_x/y: meta 'move' [x, y], else
    'direction' (forward/backward/left/right), else 'travel' (Blender XY; the character faces -Y, so its right
    is -X), else forward for locomotion and (0, 0) for everything else."""
    if 'move' in meta:
        x, y = meta['move']
    elif meta.get('direction') in MOVE_DIRS:
        x, y = MOVE_DIRS[meta['direction']]
    elif 'travel' in meta:
        x, y = -meta['travel'][0], -meta['travel'][1]
    elif meta.get('kind') == 'locomotion':
        x, y = MOVE_DIRS['forward']
    else:
        return 0.0, 0.0
    n = math.hypot(x, y)
    return (round(x/n, 4), round(y/n, 4)) if n > 1e-9 else (0.0, 0.0)


def bake(poser, title, frames, pose_fn, loop, markers=None, meta=None, post=None, legs_ik=1.0, arms_ik=0.0,
         props=None, hands=True):
    """Evaluate pose_fn(poser, frame) with IK on, bake FK to a new action, verify the bake.
    props: {name: fn(frame)} for root-bone VFX properties (vfx.PROPS); any not given are keyed at 0,
    so every clip sets them and none inherits a stale value.
    hands: run the natural-hand pass (handpass.py) on every frame after pose_fn."""
    rig = poser.rig
    rig.animation_data.action = None
    caps = {}
    ik_miss = 0.0
    extension = 0.0
    ext_at = miss_at = ''
    # per side: the v18 legs differ (R rest hip-to-ankle is 1.014 x LEG_LENGTH)
    leg_len = {s: poser.bones[f'{s} thigh'].length+poser.bones[f'{s} shin'].length for s in SIDES}
    miss_frames = {}
    poser.followers(True)
    if hands and not hasattr(poser, 'handpass'):
        import handpass
        poser.handpass = handpass.HandPass(poser)
    if hands:
        poser.handpass.max_roll = {}
        poser.handpass.infeasible = 0
        poser.handpass.prev = {}
        poser.handpass.hist = []
    from handpass import set_twist
    if not hasattr(poser, 'padpass'):
        import padpass
        poser.padpass = padpass.PadPass(poser)
    poser.padpass.hist = []
    if not hasattr(poser, 'tabardpass'):
        import tabardpass
        poser.tabardpass = tabardpass.TabardPass(poser)
    poser.tabardpass.hist = []
    for f in frames:
        poser.reset()
        poser.ik(legs_ik, arms_ik)
        pose_fn(poser, f)
        if hands:
            poser.handpass.apply()
        poser.padpass.apply()
        poser.tabardpass.apply()
        set_twist(poser)
        caps[f] = poser.capture()
        if hands:
            poser.handpass.hist.append(dict(poser.handpass.frame_roll))
        if legs_ik > 0:
            for s in SIDES:
                target = poser.world(f'{s} foot IK').translation
                got = caps[f][f'{s} foot'].translation
                miss = (target-got).length
                if miss > ik_miss:
                    ik_miss, miss_at = miss, f'{s}{f}'
                if miss > 0.005:
                    miss_frames[f'{s}{f}'] = round(miss, 3)
                hip = caps[f][f'{s} thigh'].translation
                e = (got-hip).length/leg_len[s]
                if e > extension:
                    extension, ext_at = e, f'{s}{f}'
    # Seam frames of a loop keep the raw pad/halo solve: meta `seam_anchors` (frames other clips start or end on,
    # e.g. Run forward [1, 10] for Glide exit/enter), default [first frame]; [] for loops nothing hands off to.
    anchors = (meta or {}).get('seam_anchors', [frames[0]])
    pins = sorted(frames.index(f) for f in anchors if f in frames) if loop else []
    extra = poser.handpass.finish(caps, frames, loop, pins) if hands else {}
    import handpose
    if hands and handpose.ENABLED:
        extra.update(handpose.settle(poser, poser.handpass.geo, caps, frames, loop, pins,
                                     (meta or {}).get('finger_accents', ())))
    extra.update(poser.padpass.finish(caps, frames, loop, pins))
    extra.update(poser.tabardpass.finish(caps, frames, loop, pins))
    if hands:
        extra['orient_roll_max_deg'] = {s: round(v, 1) for s, v in poser.handpass.max_roll.items()}
        extra['orient_infeasible'] = poser.handpass.infeasible
    if post:
        extra.update(post(poser, caps, frames) or {})
    if hands and handpose.ENABLED and handpose.CLEAR:
        extra.update(handpose.leg_clear(poser, poser.handpass.geo, caps, frames, loop, pins))
    # meta `halo_clear_dir` forces the halo nudge direction (chest space) to match a clip this one hands off to.
    hdir = (meta or {}).get('halo_clear_dir')
    extra.update(poser.padpass.halo_clear(caps, frames, loop, pins, [Vector(hdir).normalized()] if hdir else None))
    per = len(frames)-1 if loop and len(frames) > 2 else len(frames)
    for key, bone in (('pad_pop_mm_f2', '{} pauldron'), ('halo_pop_mm_f2', 'halo root')):
        pops = {}
        for s in (SIDES if '{}' in bone else ('',)):
            rel = [(caps[f]['chest'].inverted() @ caps[f][bone.format(s)]).translation for f in frames[:per]]
            idx = range(per) if loop else range(1, per-1)
            pops[s or 'ring'] = round(max(((rel[i-1]+rel[(i+1) % per]-2*rel[i]).length*1000 for i in idx),
                                          default=0.0), 1)
        extra[key] = pops
    name = PREFIX+title
    old = bpy.data.actions.get(name)
    if old:
        bpy.data.actions.remove(old)
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    rig.animation_data.action = act
    poser.ik(0, 0)
    poser.followers(False)
    poser.reset()
    prev = {}
    for f in frames:
        m = caps[f]
        for n in poser.fk:
            b = poser.bones[n]
            if b.parent is not None:
                p = b.parent.name
                local = (poser.rest[p].inverted() @ poser.rest[n]).inverted() @ m[p].inverted() @ m[n]
            else:
                local = poser.rest[n].inverted() @ m[n]
            loc, q, _ = local.decompose()
            if n in prev:
                q.make_compatible(prev[n])
            prev[n] = q
            pb = poser.pb[n]
            pb.location = loc
            pb.rotation_quaternion = q
            pb.scale = (1, 1, 1)
            for ch in ('location', 'rotation_quaternion', 'scale'):
                pb.keyframe_insert(data_path=ch, frame=f, group=n)
    root = poser.pb.get('root')
    from vfx import PROPS
    keyed_props = [k for k in PROPS if root is not None and k in root.keys()]
    move = move_vector(meta or {})
    props = dict(props or {})
    for k, v in zip(('hs_move_x', 'hs_move_y'), move):
        props.setdefault(k, lambda _f, v=v: v)
    prop_peak = {}
    for f in frames:
        for k in keyed_props:
            v = float((props or {}).get(k, lambda _f: 0.0)(f))
            root[k] = v
            root.keyframe_insert(data_path=f'["{k}"]', frame=f, group='root')
            prop_peak[k] = max(prop_peak.get(k, 0.0), v)
    for k in keyed_props:
        root[k] = 0.0
    act.use_frame_range = True
    act.frame_start, act.frame_end = frames[0], frames[-1]
    act.use_cyclic = loop
    for label, frame in (markers or {}).items():
        act.pose_markers.new(label).frame = frame
    info = {'title': title, 'frames': [frames[0], frames[-1]], 'loop': loop, 'markers': markers or {},
            'ik_miss_m': round(ik_miss, 5), 'max_leg_extension': round(extension, 4), 'max_leg_extension_at': ext_at,
            'ik_miss_at': miss_at, 'ik_miss_frames': miss_frames}
    info.update(meta or {})
    info.update(extra)
    if props:
        info['vfx_peak'] = {k: round(v, 3) for k, v in prop_peak.items()}
    act['clip_json'] = json.dumps(info)
    # Verify: evaluated FK pose must reproduce the IK capture.
    err = 0.0
    rot_err = 0.0
    scene = bpy.context.scene
    for f in frames[::max(1, len(frames)//6)]+[frames[-1]]:
        scene.frame_set(f)
        poser.update()
        for n in poser.fk:
            got, want = poser.pb[n].matrix, caps[f][n]
            err = max(err, (got.translation-want.translation).length)
            q = got.to_quaternion().rotation_difference(want.to_quaternion())
            rot_err = max(rot_err, math.degrees(min(q.angle, 2*math.pi-q.angle)))
    info['bake_error_m'] = round(err, 6)
    info['bake_error_deg'] = round(rot_err, 4)
    act['clip_json'] = json.dumps(info)
    if err > 1e-3 or rot_err > 0.1:
        raise RuntimeError(f'{name}: bake mismatch {err:.5f} m / {rot_err:.3f} deg')
    print('BAKED', name, json.dumps(info), flush=True)
    return act, info


# ----------------------------------------------------------------------------- rendering
def gpu_cycles(scene, samples=64):
    scene.render.engine = 'CYCLES'
    prefs = bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type = 'OPTIX'
    prefs.get_devices()
    for d in prefs.devices:
        d.use = d.type != 'CPU'
    scene.cycles.device = 'GPU'
    scene.cycles.samples = samples


def eevee(scene, samples=12):
    scene.render.engine = 'BLENDER_EEVEE'
    scene.eevee.taa_render_samples = samples


def render_still(scene, camera, path, res=(640, 800)):
    scene.camera = bpy.data.objects[camera]
    scene.render.resolution_x, scene.render.resolution_y = res
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
