"""v34 arm/hand pass: post-processes the saved v32 checkpoint (never re-bakes). Supersedes v33's arm look.

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/armpass_v34.py --
     0,1,2,3,4,5,6 art/anim/hollow-saint-anim-v34.blend
Stu's v33 feedback: keep v32's arms out at the sides with open energized hands and hurling casts, but make them flow.
Stages (cumulative, always from v32):
 0 Idle combat arms = v32 gesture-neutral ready pose (arms out at sides, open claws, per-finger cascade) with
   loop-exact overlapping sway; Idle arms eased 45% toward it with the same sway (no curled fists at the hips)
 1 gestures start/end on the new Idle combat pose, NO anticipation fist; Arc Bolt = quick hurl from the ready pose
   (shoulder leads, elbow/wrist/fingers drag 1-3 frames) with follow-through back to the ready pose; the other
   gestures keep v32 action with lagged forearm/hand/fingers
 2 Combat ready / Combat relax (as v33, onto the new poses)
 3 locomotion: v32 hands kept (no v33 half-curl), fingers trail the arm swing; v33 run counter-swing retime kept;
   glide upper-arm flow kept
 4 Open Circuit arms / hold / end (as v33, soft gather instead of a fist)
 5 Conduit Spear recover / Discharge recover, Land (trailing fingers + overshoot)
 6 Idle fidget 1-3 / Idle combat fidget, finger life in Idle / Idle combat
Legs, feet, root, pelvis, cloth, halo and props of existing clips are untouched, as are frame counts and markers.
"""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Quaternion, Vector

sys.path.insert(0, str(Path(__file__).parent))
import hs_anim as H
import handpose as HP
from handfix import ZSIGN

H.require_background()
args = sys.argv[sys.argv.index('--')+1:]
STAGES = set(args[0].split(','))
OUTB = H.ROOT/args[1]
REPDIR = H.ROOT/'art/anim/wip/v34arms'
SRC = H.ROOT/'art/anim/hollow-saint-anim-v32.blend'
assert not OUTB.exists(), 'never overwrite ' + str(OUTB)
bpy.ops.wm.open_mainfile(filepath=str(SRC))
bpy.context.preferences.filepaths.save_version = 0
rig = bpy.data.objects[H.RIG]
for t in rig.animation_data.nla_tracks:
    t.mute = True
pb = rig.pose.bones
X = Vector((1, 0, 0))
REPORT = {'changed': [], 'new': [], 'seamfix': [], 'phase_shift': {}}

DIG = ('index', 'middle', 'ring', 'little', 'thumb')
FING = {s: [f'{s} {d}.{i}' for d in DIG for i in (1, 2, 3)] for s in 'LR'}
ARMB = {s: [f'{s} upperarm', f'{s} forearm', f'{s} forearm twist', f'{s} hand', f'{s} shoulder', f'{s} scapula',
            f'{s} pauldron', f'{s} muzzle'] + FING[s] for s in 'LR'}
ARMS = ARMB['L'] + ARMB['R']


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
    ang = 2*math.atan2(s, q.w)   # atan2, not acos: float32 quats make acos useless for small angles
    return Quaternion(Vector((q.x/s, q.y/s, q.z/s)), ang*t)


def qslerp(a, b, t):
    if a.dot(b) < 0:
        b = -b
    return a @ qpow(a.inverted() @ b, t)


def qang(a, b):
    return math.degrees(2*math.acos(min(1.0, abs(a.dot(b)))))


def clamp(x, a=0.0, b=1.0):
    return max(a, min(b, x))


S = H.smooth
SS = H.smoother


def bump(u, c, w):
    """Smooth 0..1..0 bump centred on c, half-width w."""
    return S(1-abs(u-c)/w) if abs(u-c) < w else 0.0


class Clip:
    def __init__(self, title):
        self.title = title
        self.act = bpy.data.actions[H.PREFIX+title]
        self.info = json.loads(self.act['clip_json'])
        self.f0, self.f1 = self.info['frames']
        self.fcs = {(fc.data_path, fc.array_index): fc for fc in fcurves(self.act)}
        self.frames = [{k: fc.evaluate(f) for k, fc in self.fcs.items()} for f in range(self.f0, self.f1+1)]
        self.bones = sorted({k[0].split('"')[1] for k in self.fcs if k[0].endswith('rotation_quaternion')})
        self.loop = bool(self.info.get('loop'))

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


CLIPS = {}


def clip(title):
    if title not in CLIPS:
        CLIPS[title] = Clip(title)
    return CLIPS[title]


def touch(title, new=False):
    (REPORT['new'] if new else REPORT['changed']).append(title) if title not in REPORT['new']+REPORT['changed'] \
        else None


def new_clip(title, template, n, loop, markers, kind, extra=None):
    src = bpy.data.actions[H.PREFIX+template]
    act = src.copy()
    act.name = H.PREFIX+title
    act.use_fake_user = True
    if act.use_frame_range:
        act.frame_start, act.frame_end = 1, n
    info = {'title': title, 'frames': [1, n], 'loop': loop, 'markers': markers, 'kind': kind}
    if extra:
        info.update(extra)
    act['clip_json'] = json.dumps(info)
    c = Clip(title)
    CLIPS[title] = c
    touch(title, new=True)
    return c


def blend_pose(fa, fb, t, bones, out, props=False):
    for b in bones:
        setq(out, b, qslerp(getq(fa, b), getq(fb, b), t))
        for p in ('location', 'scale'):
            setv(out, b, getv(fa, b, p).lerp(getv(fb, b, p), t), p)
    if props:
        for k in out:
            if not k[0].endswith(('rotation_quaternion', 'location', 'scale')):
                out[k] = fa[k]+(fb[k]-fa[k])*t


def copy_bones(src, dst, bones):
    for b in bones:
        for p, n in (('location', 3), ('rotation_quaternion', 4), ('scale', 3)):
            for j in range(n):
                dst[K(b, p, j)] = src[K(b, p, j)]


def curl(fr, bones, deg):
    """Add a local-X curl (the authored curl axis, as Poser.curl) to finger joints."""
    for b in bones:
        setq(fr, b, getq(fr, b) @ HP.about(X, deg))


# ---------------------------------------------------------------- hand library
poser = type('P', (), {})()
poser.rig, poser.pb = rig, pb
poser.rest = {b.name: b.matrix_local.copy() for b in rig.data.bones}
poser.r3 = {n: m.to_3x3() for n, m in poser.rest.items()}
GEO = HP.HandGeo(poser)


def hand_quats(s, levels, thumb_lv):
    """Finger/thumb local quats from the handpose LIBRARY at a curl level per digit (dict or float)."""
    out = {}
    k = (1.0 if s == 'L' else -1.0)*ZSIGN
    for d in HP.DIGITS:
        lv = levels[d] if isinstance(levels, dict) else levels
        conv = S((lv-HP.CONVERGE_RAMP[0])/(HP.CONVERGE_RAMP[1]-HP.CONVERGE_RAMP[0]))
        splay = -HP.CONVERGE*conv*GEO.dir0[(s, d)]
        qs = GEO.build(None, s, d, HP.pose(d, lv), splay)
        for i in (1, 2, 3):
            out[f'{s} {d}.{i}'] = qs[i]
    fx, op, t2, t3 = HP.thumb_pose(thumb_lv)
    out[f'{s} thumb.1'] = HP.about(HP.X, fx) @ HP.about(HP.Z, k*op)
    for i, tgt in ((2, t2), (3, t3)):
        n = f'{s} thumb.{i}'
        out[n] = HP.about(HP.X, GEO.solve(n, Quaternion(), tgt))
    return out


def set_hand(fr, s, quats, w=1.0):
    for b, q in quats.items():
        setq(fr, b, qslerp(getq(fr, b), q, w) if w < 1.0 else q)


IC = clip('Idle combat')
IDLE = clip('Idle')
IC0 = dict(IC.frames[0])
I0 = dict(IDLE.frames[0])


def active_sides(c, thresh=20.0):
    out = []
    for s in 'LR':
        a0 = getq(c.frames[0], f'{s} upperarm')
        m = max(qang(a0, getq(fr, f'{s} upperarm')) for fr in c.frames)
        f0 = getq(c.frames[0], f'{s} forearm')
        m2 = max(qang(f0, getq(fr, f'{s} forearm')) for fr in c.frames)
        if max(m, m2) > thresh:
            out.append(s)
    return out or ['R']


def fi(c, frame):
    return frame-c.f0



# ---------------------------------------------------------------- stage 0: ready pose, idles
Zax = Vector((0, 0, 1))
READY = dict(clip('Arc Bolt left').frames[0])   # v32 gesture-neutral: arms out at the sides, open claws
CASCADE = {'index': -2.0, 'middle': 1.0, 'ring': 3.5, 'little': 6.0}   # deg per-finger offsets (not a rigid fan)


def ready_arms(fr, w=1.0):
    tgt = dict(fr)
    copy_bones(READY, tgt, ARMS)
    for s in 'LR':
        for d, cdeg in CASCADE.items():
            for j, g in ((1, 1.0), (2, 0.8), (3, 0.6)):
                b = f'{s} {d}.{j}'
                setq(tgt, b, getq(tgt, b) @ HP.about(X, cdeg*g))
    if w >= 1.0:
        copy_bones(tgt, fr, ARMS)
    else:
        blend_pose(dict(fr), tgt, w, ARMS, fr)


def arm_sway(c, amp, cycles, lr_phase):
    """Loop-exact, zero on frame 1: shoulder leads, elbow/wrist drag, fingers trail last."""
    m = c.n-1
    for i, fr in enumerate(c.frames):
        ph = 2*math.pi*cycles*i/m
        for s in 'LR':
            so = 0.0 if s == 'L' else lr_phase
            sg = 1.0 if s == 'L' else -1.0

            def w(lag):
                return math.sin(ph+so-lag)-math.sin(so-lag)
            ub = f'{s} upperarm'
            setq(fr, ub, getq(fr, ub) @ HP.about(X, amp*w(0.0)) @ HP.about(Zax, sg*0.6*amp*w(0.35)))
            setq(fr, f'{s} forearm', getq(fr, f'{s} forearm') @ HP.about(X, 0.8*amp*w(0.55)))
            setq(fr, f'{s} hand', getq(fr, f'{s} hand') @ HP.about(X, 1.2*amp*w(1.0)))
            for j, d in enumerate(DIG):
                curl(fr, [f'{s} {d}.{k}' for k in (1, 2, 3)], 1.3*amp*w(1.5+0.3*j))


def stage0():
    global IC0, I0
    for fr in IC.frames:
        ready_arms(fr)
    arm_sway(IC, 2.2, 1, 0.9)
    for fr in IDLE.frames:
        ready_arms(fr, 0.45)
    arm_sway(IDLE, 2.8, 2, 1.1)
    for c in (IC, IDLE):
        copy_bones(c.frames[0], c.frames[-1], ARMS)
        touch(c.title)
    IC0 = dict(IC.frames[0])
    I0 = dict(IDLE.frames[0])


def pose_at(orig, x, bones):
    x = max(0.0, min(len(orig)-1.0, x))
    i = min(int(x), len(orig)-2)
    t = x-i
    return {b: (qslerp(getq(orig[i], b), getq(orig[i+1], b), t), getv(orig[i], b).lerp(getv(orig[i+1], b), t),
                getv(orig[i], b, 'scale').lerp(getv(orig[i+1], b, 'scale'), t)) for b in bones}


def put(fr, b, p):
    setq(fr, b, p[0])
    setv(fr, b, p[1])
    setv(fr, b, p[2], 'scale')


def soften(c, bones, iters=2, amt=0.5):
    """Acceleration relief: Jacobi 1-2-1 smoothing of rotations on interior frames (ends stay exact)."""
    for _ in range(iters):
        prev = [dict(fr) for fr in c.frames]
        for i in range(1, c.n-1):
            for b in bones:
                mid = qslerp(getq(prev[i-1], b), getq(prev[i+1], b), 0.5)
                setq(c.frames[i], b, qslerp(getq(prev[i], b), mid, amt))


def lag_of(b):
    """Overlap: shoulder chain leads, elbow ~1 f, wrist ~1.5 f, fingers 2-3 f (per digit and joint)."""
    for j, d in enumerate(DIG):
        if f' {d}.' in b:
            return 2.0+0.2*j+0.25*(int(b[-1])-1)
    if b.endswith(('hand', 'muzzle')):
        return 1.5
    if 'forearm' in b:
        return 0.9
    return 0.0

# ---------------------------------------------------------------- stage 1: gestures
GESTURES = {  # title: (in_end frame, release frame, out_start frame)
    'Arc Bolt left': (4, 5, 13), 'Arc Bolt right': (4, 5, 13), 'Conduit Spear': (5, 7, 11),
    'Discharge': (7, 9, 15), 'Discharge snap': (2, 3, 7), 'Meter full flourish': (6, 15, 18),
}
ADDITIVE = {'Discharge snap'}   # too short to travel from the combat pose: the authored flick rides on it
TORSO = ['spine', 'chest', 'neck', 'head']


def hurl(c, act, rel):
    """Arc Bolt: no wind-up. Each arm bone travels ready -> v32 release pose with an accelerating ease (lagged per
    bone), then plays v32's follow-through 1.25x faster so the arm is back on the ready pose by ~f17."""
    orig = [dict(fr) for fr in c.frames]
    R = rel-c.f0
    N = c.n-1
    rate = (N-R)/(N-4-R)
    for s in 'LR':
        bones = ARMB[s]
        for b in bones:
            L = lag_of(b) if s in act else 0.0
            a_i = R+0.6*L
            s_i = 0.4*L
            relp = pose_at(orig, R, [b])[b]
            st = (getq(IC0, b), getv(IC0, b), getv(IC0, b, 'scale'))
            for i, fr in enumerate(c.frames):
                if s in act:
                    if i <= a_i:
                        u = clamp((i-s_i)/(a_i-s_i))
                        w = u*u*(2.2-1.2*u)
                        p = (qslerp(st[0], relp[0], w), st[1].lerp(relp[1], w), st[2].lerp(relp[2], w))
                    else:
                        p = pose_at(orig, R+(i-a_i)*rate, [b])[b]
                    put(fr, b, p)
                else:   # the off arm rides its small v32 motion on the ready pose
                    q = getq(IC0, b) @ (getq(orig[0], b).inverted() @ getq(orig[i], b))
                    setq(fr, b, q)
    for i, fr in enumerate(c.frames):
        for b in TORSO:
            if i <= R:
                setq(fr, b, qslerp(getq(IC0, b), getq(orig[R], b), S(i/R)))
            else:
                setq(fr, b, pose_at(orig, R+(i-R)*rate, [b])[b][0])


def stage1():
    for title, (fin, rel, fout) in GESTURES.items():
        c = clip(title)
        orig = [dict(fr) for fr in c.frames]
        A0 = orig[0]
        act = active_sides(c)
        REPORT.setdefault('gesture_sides', {})[title] = act
        if title.startswith('Arc Bolt'):
            hurl(c, act, rel)
            fout = 13
        else:
            lagk = 0.6 if title in ADDITIVE else 1.0
            for i, fr in enumerate(c.frames):
                f = c.f0+i
                ramp = S((f-rel)/3.0) if title == 'Conduit Spear' else 1.0   # spear grip stays exact to release
                w_in = S((f-c.f0)/max(1, fin-c.f0))
                for b in TORSO:
                    setq(fr, b, qslerp(getq(IC0, b), getq(orig[i], b), w_in))
                for s in 'LR':
                    for b in ARMB[s]:
                        L = lag_of(b)*lagk*ramp if s in act else 0.0
                        p = pose_at(orig, i-L, [b])[b]
                        if s in act and title not in ADDITIVE:
                            put(fr, b, (qslerp(getq(IC0, b), p[0], w_in), getv(IC0, b).lerp(p[1], w_in),
                                        getv(IC0, b, 'scale').lerp(p[2], w_in)))
                        else:
                            setq(fr, b, getq(IC0, b) @ (getq(A0, b).inverted() @ p[0]))
                            setv(fr, b, getv(IC0, b)+(p[1]-getv(A0, b)))
                            setv(fr, b, getv(IC0, b, 'scale')+(p[2]-getv(A0, b, 'scale')), 'scale')
        # settle onto the Idle combat pose (arms + torso), eased, exact on the last frame
        for i, fr in enumerate(c.frames):
            f = c.f0+i
            w_out = SS((f-fout)/(c.f1-fout))
            if w_out <= 0:
                continue
            for b in TORSO+ARMS:
                setq(fr, b, qslerp(getq(fr, b), getq(IC0, b), w_out))
                for p in ('location', 'scale'):
                    setv(fr, b, getv(fr, b, p).lerp(getv(IC0, b, p), w_out), p)
        if not title.startswith('Arc Bolt'):
            soften(c, TORSO+ARMS)
        touch(title)


# ---------------------------------------------------------------- stage 2: combat ready / relax
NON_ARM = None


def non_arm(c):
    return [b for b in c.bones if b not in ARMS]


def overshoot(u, peak=0.75, amt=0.06):
    return SS(u/peak) if u < peak else 1.0+amt*math.sin(math.pi*(u-peak)/(1-peak))


def stage2():
    idlef = {s: {b: getq(I0, b) for b in FING[s]} for s in 'LR'}
    icf = {s: {b: getq(IC0, b) for b in FING[s]} for s in 'LR'}
    openf = {s: hand_quats(s, 3.0, 2.0) for s in 'LR'}
    n = 11
    c = new_clip('Combat ready', 'Idle combat', n, False, {'Claw': 8, 'Ready': n}, 'gesture',
                 {'seam_from': ['Idle', 1], 'seam_to': ['Idle combat', 1], 'finger_accents': [8]})
    for i, fr in enumerate(c.frames):
        u = i/(n-1)
        blend_pose(I0, IC0, SS(u), non_arm(c), fr, props=True)
        for s in 'LR':
            t = overshoot(u) if s == 'R' else SS(clamp((u-0.12)/0.88))
            blend_pose(I0, IC0, t, [b for b in ARMB[s] if b not in FING[s]], fr)
            o = 0.45*math.sin(math.pi*clamp(u/0.35))
            tf = overshoot(clamp((u-0.2)/0.8), 0.7, 0.1)
            for b in FING[s]:
                q = qslerp(idlef[s][b], openf[s][b], o)
                setq(fr, b, qslerp(q, icf[s][b], tf))
                copy_bones(I0, {}, [])
    n = 18
    c = new_clip('Combat relax', 'Idle', n, False, {'Shake': 7, 'Relaxed': n}, 'gesture',
                 {'seam_from': ['Idle combat', 1], 'seam_to': ['Idle', 1], 'finger_accents': list(range(3, 13))})
    phases = {'index': 0.0, 'middle': 0.7, 'ring': 1.4, 'little': 2.1}
    for i, fr in enumerate(c.frames):
        u = i/(n-1)
        blend_pose(IC0, I0, SS(u), non_arm(c), fr, props=True)
        for s in 'LR':
            lag = 0.0 if s == 'R' else 0.1
            t = SS(clamp((u-0.08-lag)/(0.85-lag)))
            blend_pose(IC0, I0, t, [b for b in ARMB[s] if b not in FING[s]], fr)
            env = math.sin(math.pi*clamp((u-0.12)/0.6))
            sh = env*math.sin(2*math.pi*2.5*u)
            hb = f'{s} hand'
            setq(fr, hb, getq(fr, hb) @ Quaternion(GEO.palm_axis[s], math.radians(12*sh))
                 @ Quaternion((0, 1, 0), math.radians(8*sh*(1 if s == 'L' else -1))))
            flutter = hand_quats(s, {d: 6+9*math.sin(2*math.pi*3*u+phases[d]) for d in HP.DIGITS},
                                 6+5*math.sin(2*math.pi*3*u))
            tf = SS(clamp((u-0.2)/0.8))
            for b in FING[s]:
                q = qslerp(icf[s][b], idlef[s][b], tf)
                setq(fr, b, qslerp(q, flutter[b], 0.85*env))


# ---------------------------------------------------------------- stage 3: locomotion hands
LOCO = {  # prefix: trailing finger amplitude (deg per joint)
    'Walk': 4.0, 'Run': 6.0, 'Plant turn': 5.0, 'Glide': 5.0, 'Ascend': 3.0, 'Descend': 4.0, 'Jump': 4.0, 'Land': 4.0,
}
PH = {'index': 0.0, 'middle': 0.45, 'ring': 0.9, 'little': 1.35}


def loco_params(title):
    for p, v in LOCO.items():
        if title.startswith(p):
            return v
    return None


def pitch_series(c, bone):
    act = c.act
    rig.animation_data.action = act
    rig.animation_data.action_slot = act.slots[0]
    out = []
    for f in range(c.f0, c.f1+1):
        bpy.context.scene.frame_set(f)
        v = pb[bone].tail-pb[bone].head
        out.append(math.degrees(math.atan2(v.y, -v.z)))
    return out


def counter_swing(c):
    n = c.n-1
    la = pitch_series(c, 'L upperarm')[:n]
    rt = pitch_series(c, 'R thigh')[:n]
    if max(rt)-min(rt) < 15 or max(la)-min(la) < 4:
        return 0, None
    ma, mt = sum(la)/n, sum(rt)/n
    la = [x-ma for x in la]
    rt = [x-mt for x in rt]
    na = math.sqrt(sum(x*x for x in la))
    nt = math.sqrt(sum(x*x for x in rt))
    cc = [sum(la[i]*rt[(i-k) % n] for i in range(n))/(na*nt) for k in range(n)]
    k = max(range(n), key=lambda j: cc[j])
    orig = [dict(fr) for fr in c.frames]
    if k:
        for i, fr in enumerate(c.frames):
            copy_bones(orig[(i+k) % n], fr, ARMS)
    return k, (round(cc[0], 2), round(cc[k], 2))


RUN_ABDUCT = 7.0   # deg the retimed run arms swing wider (armature-forward axis) to keep hands off the thighs


def abduct(c, deg):
    from mathutils import Matrix
    rig.animation_data.action = c.act
    rig.animation_data.action_slot = c.act.slots[0]
    for i, fr in enumerate(c.frames):
        bpy.context.scene.frame_set(c.f0+i)
        ch = pb['chest'].matrix.to_3x3()
        for s, sg in (('L', -1.0), ('R', 1.0)):
            b = f'{s} upperarm'
            A = ch @ poser.r3['chest'].inverted() @ poser.r3[b]
            R = Matrix.Rotation(math.radians(sg*deg), 3, 'Y')
            setq(fr, b, (A.inverted() @ R @ A @ getq(fr, b).to_matrix()).to_quaternion())


def swing_series(c, s):
    """Upper-arm swing proxy from the (possibly retimed) frames: local rotation about X."""
    out = []
    for fr in c.frames:
        q = getq(fr, f'{s} upperarm')
        if q.w < 0:
            q = -q
        out.append(2*math.atan2(q.x, q.w))
    return out


def loco_hands(c, params):
    """Keep the v32 open hands; fingers trail the arm swing 2-3 frames late (per digit), plus a soft cascade."""
    amp = params
    n = c.n-1 if c.loop else c.n
    orig = [dict(fr) for fr in c.frames]
    for s in 'LR':
        sw = swing_series(c, s)[:n]
        mu = sum(sw)/n
        pk = max(1e-6, max(abs(x-mu) for x in sw))
        nrm = [(x-mu)/pk for x in sw]
        for i, fr in enumerate(c.frames):
            for j, d in enumerate(DIG):
                lag = 2.0+0.5*j if d != 'thumb' else 1.5
                x = i-lag
                if c.loop:
                    x %= n
                    i0_ = int(x)
                    t = x-i0_
                    v = nrm[i0_]*(1-t)+nrm[(i0_+1) % n]*t
                else:
                    x = max(0.0, x)
                    i0_ = min(int(x), n-1)
                    t = x-i0_
                    v = nrm[i0_]*(1-t)+nrm[min(i0_+1, n-1)]*t
                k = (0.6 if d == 'thumb' else 1.0)*amp
                for jj, g in ((1, 1.0), (2, 0.8), (3, 0.6)):
                    b = f'{s} {d}.{jj}'
                    setq(fr, b, getq(orig[i], b) @ HP.about(X, g*(k*v+0.7*CASCADE.get(d, 0.0))))


def glide_arms(c):
    n = c.n-1
    for i, fr in enumerate(c.frames):
        ph = 2*math.pi*i/n
        for s in 'LR':
            so = 0.0 if s == 'L' else 0.5*math.pi
            for b, amp, lag in ((f'{s} upperarm', 7.0, 0.0), (f'{s} forearm', 5.0, 0.6), (f'{s} hand', 6.0, 1.2)):
                setq(fr, b, getq(fr, b) @ HP.about(X, amp*math.sin(ph+so-lag)))


def stage3():
    for a in list(bpy.data.actions):
        if not a.name.startswith(H.PREFIX):
            continue
        title = a.name[len(H.PREFIX):]
        params = loco_params(title)
        if not params or title == 'Land' or title.startswith(('Glide exit', 'Run pivot 180')):
            continue
        c = clip(title)
        if c.loop and title.startswith('Run') and title not in ('Run left', 'Run right'):
            # pure strafes keep their authored arms: a counter-swing retime drove the lead hand into the thigh
            k, cc = counter_swing(c)
            REPORT['phase_shift'][title] = {'frames': k, 'corr_before_after': cc}
            if k:
                abduct(c, {'Run forward right': 12.0, 'Run forward left': 10.0}.get(title, RUN_ABDUCT))
        if title == 'Glide loop':
            glide_arms(c)
        loco_hands(c, params)
        touch(title)


# ---------------------------------------------------------------- stage 4: Open Circuit arms
def stage4():
    end = clip('Open Circuit end')
    Hp = dict(end.frames[0])
    fistf = {s: hand_quats(s, 30.0, 18.0) for s in 'LR'}
    n = 15
    c = new_clip('Open Circuit arms', 'Idle combat', n, False, {'Gather': 4, 'Open': 10, 'Held': n}, 'gesture',
                 {'layer': 'upper body (arms)', 'seam_from': ['Idle combat', 1],
                  'seam_to': ['Open Circuit arms hold', 1], 'finger_accents': [4, 10], 'accents': [4, 10]})
    for i, fr in enumerate(c.frames):
        u = i/(n-1)
        copy_bones(IC0, fr, non_arm(c))
        g = 0.14*math.sin(math.pi*clamp(u/0.35))
        t = overshoot(clamp((u-0.2)/0.8), 0.65, 0.07)
        for s in 'LR':
            for b in ARMB[s]:
                gg = 0.0 if ('forearm' in b or b.endswith(('hand', 'muzzle')) or ' ' in b[2:] and '.' in b) else g
                q = qslerp(getq(IC0, b), getq(Hp, b), -gg+t if t > 0 else -gg)
                setq(fr, b, q)
                for p in ('location', 'scale'):
                    setv(fr, b, getv(IC0, b, p).lerp(getv(Hp, b, p), clamp(t)), p)
            set_hand(fr, s, fistf[s], 0.4*math.sin(math.pi*clamp(u/0.45)))
    n = 25
    c = new_clip('Open Circuit arms hold', 'Idle combat', n, True, {'Pulse': 1}, 'gesture',
                 {'layer': 'upper body (arms)'})
    m = n-1
    for i, fr in enumerate(c.frames):
        ph = 2*math.pi*i/m
        copy_bones(IC0, fr, non_arm(c))
        copy_bones(Hp, fr, ARMS)
        for s in 'LR':
            so = 0.0 if s == 'L' else 0.8
            setq(fr, f'{s} upperarm', getq(fr, f'{s} upperarm') @ HP.about(X, 2.5*(math.sin(ph+so)-math.sin(so))))
            setq(fr, f'{s} hand', getq(fr, f'{s} hand') @ HP.about(X, 3.0*(math.sin(ph+so-0.8)-math.sin(so-0.8))))
            for d in DIG:
                curl(fr, [f'{s} {d}.{j}' for j in (1, 2, 3)],
                     3.0*math.sin(2*ph*2+PH.get(d, 0.3)*2+so)*math.sin(ph/2)**2)
    orig = [dict(fr) for fr in end.frames]
    for i, fr in enumerate(end.frames):
        f = end.f0+i
        w = overshoot(clamp((f-3)/(end.f1-3)), 0.7, 0.05)
        for b in ARMS:
            setq(fr, b, qslerp(getq(orig[i], b), getq(IC0, b), w))
            for p in ('location', 'scale'):
                setv(fr, b, getv(orig[i], b, p).lerp(getv(IC0, b, p), clamp(w)), p)
    end.info['seam_from_arms'] = ['Open Circuit arms hold', 1]
    end.info['seam_to_arms'] = ['Idle combat', 1]
    touch('Open Circuit end')


# ---------------------------------------------------------------- stage 5: recovers, Land
def stage5():
    for title, src, sf in (('Conduit Spear recover', 'Conduit Spear', 11), ('Discharge recover', 'Discharge', 15)):
        g = clip(src)
        P = dict(g.frames[fi(g, sf)])
        n = 8
        c = new_clip(title, 'Idle combat', n, False, {'Recovered': n}, 'gesture',
                     {'layer': 'upper body (mask excludes pelvis/legs)', 'seam_from': [src, sf],
                      'seam_to_arms': ['Idle combat', 1]})   # upper body ends on Idle combat; legs hold
        upper = TORSO+[b for b in c.bones if b.startswith('halo')]
        for i, fr in enumerate(c.frames):
            u = i/(n-1)
            copy_bones(P, fr, [b for b in non_arm(c) if b not in upper])   # legs/pelvis/cloth hold the gesture's
            for k in fr:
                if not k[0].endswith(('rotation_quaternion', 'location', 'scale')):
                    fr[k] = P[k]+(IC0[k]-P[k])*SS(u)
            blend_pose(P, IC0, SS(u), upper, fr)
            blend_pose(P, IC0, SS(u), ARMS, fr)
        soften(c, TORSO+ARMS, 1)
    c = clip('Land')
    params = LOCO['Land']
    loco_hands(c, params)
    comp = c.info['markers'].get('Compress', 6)
    orig = [dict(fr) for fr in c.frames]
    for i, fr in enumerate(c.frames):
        f = c.f0+i
        e = bump(f, comp+1, 4.5)
        if e > 0:
            for s in 'LR':
                for b in (f'{s} upperarm', f'{s} forearm', f'{s} hand'):
                    setq(fr, b, qslerp(getq(orig[0], b), getq(orig[i], b), 1+0.3*e))
    touch('Land')


# ---------------------------------------------------------------- stage 6: fidgets, idle life
def idle_base(c, src, pin_all=True):
    for i, fr in enumerate(c.frames):
        copy_bones(src.frames[i % (src.n-1)], fr, c.bones)
        for k in fr:
            if not k[0].endswith(('rotation_quaternion', 'location', 'scale')):
                fr[k] = src.frames[i % (src.n-1)][k]
    # close onto the source's frame-1 pose over the last 20 frames (every bone)
    last = dict(c.frames[-1])
    tgt = src.frames[0]
    for i, fr in enumerate(c.frames):
        w = SS((i-(c.n-21))/20)
        if w <= 0:
            continue
        for b in c.bones:
            d = getq(tgt, b) @ getq(last, b).inverted()
            setq(fr, b, qpow(d, w) @ getq(fr, b))
            for p in ('location', 'scale'):
                setv(fr, b, getv(fr, b, p)+(getv(tgt, b, p)-getv(last, b, p))*w, p)
        for k in fr:
            if not k[0].endswith(('rotation_quaternion', 'location', 'scale')):
                fr[k] += (tgt[k]-last[k])*w


def stage6():
    # 1 finger crackle
    n = 61
    c = new_clip('Idle fidget 1', 'Idle', n, False, {'Crackle': 20, 'Crackle 2': 38}, 'idle',
                 {'seam_from': ['Idle', 1], 'seam_to': ['Idle', 1], 'finger_accents': list(range(14, 46))})
    idle_base(c, IDLE)
    for i, fr in enumerate(c.frames):
        u = i/(n-1)
        env = S(u/0.2)*S((1-u)/0.2)
        for s, gain in (('R', 1.0), ('L', 0.45)):
            lv = {}
            prev = -99.0
            for j, d in enumerate(HP.DIGITS):   # ripple little -> index, curl order kept (index <= ... <= little)
                lv[d] = max(prev, 21+30*(bump(u, 0.3+0.05*(3-j), 0.08)+bump(u, 0.58+0.04*(3-j), 0.07)))
                prev = lv[d]
            q = hand_quats(s, lv, 18+14*bump(u, 0.45, 0.2))
            set_hand(fr, s, q, gain*env)
            setq(fr, f'{s} hand', getq(fr, f'{s} hand') @ Quaternion(GEO.palm_axis[s], math.radians(-10*gain*env)))
    # 2 wrist roll
    c = new_clip('Idle fidget 2', 'Idle', n, False, {'Roll': 30}, 'idle',
                 {'seam_from': ['Idle', 1], 'seam_to': ['Idle', 1]})
    idle_base(c, IDLE)
    for i, fr in enumerate(c.frames):
        u = i/(n-1)
        env = S(u/0.25)*S((1-u)/0.25)
        for s, gain in (('R', 1.0), ('L', 0.35)):
            sg = 1 if s == 'L' else -1
            ang = 2*math.pi*1.5*u
            hb = f'{s} hand'
            setq(fr, hb, getq(fr, hb) @ Quaternion((0, 1, 0), math.radians(sg*28*gain*env*math.sin(ang)))
                 @ Quaternion(GEO.palm_axis[s], math.radians(12*gain*env*math.cos(ang))))
            tb = f'{s} forearm twist'
            setq(fr, tb, getq(fr, tb) @ Quaternion((0, 1, 0), math.radians(sg*10*gain*env*math.sin(ang))))
            for d in DIG:
                curl(fr, [f'{s} {d}.{j}' for j in (1, 2, 3)], 4*gain*env*math.sin(ang+0.6))
    # 3 halo glance
    n = 73
    c = new_clip('Idle fidget 3', 'Idle', n, False, {'Glance': 30, 'Return': 52}, 'idle',
                 {'seam_from': ['Idle', 1], 'seam_to': ['Idle', 1]})
    idle_base(c, IDLE)
    for i, fr in enumerate(c.frames):
        u = i/(n-1)
        env = S((u-0.08)/0.3)*S((0.92-u)/0.3)
        for b, pitch, yaw in (('neck', -6, 7), ('head', -14, 12)):
            setq(fr, b, getq(fr, b) @ HP.about(X, pitch*env) @ Quaternion((0, 1, 0), math.radians(yaw*env)))
        setq(fr, 'chest', getq(fr, 'chest') @ Quaternion((0, 1, 0), math.radians(3*env)))
        for d in DIG:
            curl(fr, [f'R {d}.{j}' for j in (1, 2, 3)], 6*env*S((u-0.35)/0.1)*S((0.7-u)/0.1))
    # IC fidget: spark rolled between fingers
    n = 49
    c = new_clip('Idle combat fidget', 'Idle combat', n, False, {'Roll': 24}, 'idle',
                 {'seam_from': ['Idle combat', 1], 'seam_to': ['Idle combat', 1],
                  'finger_accents': list(range(10, 40))})
    idle_base(c, IC)
    for i, fr in enumerate(c.frames):
        u = i/(n-1)
        env = S(u/0.2)*S((1-u)/0.2)
        ph = 2*math.pi*3*u
        s = 'R'
        for d, amp, off in (('thumb', 6, 0.0), ('index', 6, math.pi), ('middle', 4.5, math.pi+0.7),
                            ('ring', 2.5, math.pi+1.4), ('little', 1.5, math.pi+2.1)):
            curl(fr, [f'{s} {d}.{j}' for j in (1, 2, 3)], amp*env*(0.5-0.5*math.cos(ph+off)))
        setq(fr, 'R hand', getq(fr, 'R hand') @ Quaternion((0, 1, 0), math.radians(-6*env*math.sin(ph/3))))
    # finger life in Idle / Idle combat (zero at frame 1, loop-exact, lengths unchanged)
    for c, amp in ((IDLE, 2.5), (IC, 3.0)):
        m = c.n-1
        for i, fr in enumerate(c.frames):
            for s in 'LR':
                for j, d in enumerate(DIG):
                    k = 2+j % 3
                    v = amp*math.sin(2*math.pi*k*i/m)*(0.6 if s == 'L' else 1.0)
                    curl(fr, [f'{s} {d}.{jj}' for jj in (1, 2, 3)], v)
        touch(c.title)


# ---------------------------------------------------------------- seams (arm channels)
def seam_pairs():
    """(fix clip, 'start'|'end', target clip, target frame) with the looping side authoritative."""
    out = []
    for c in list(CLIPS.values()):
        pass
    for a in bpy.data.actions:
        if not a.name.startswith(H.PREFIX):
            continue
        info = json.loads(a['clip_json'])
        t = info['title']
        loop = info.get('loop')
        decl = []
        for key, end in (('seam_from', 'start'), ('seam_to', 'end'), ('seam_from_arms', 'start'),
                         ('seam_to_arms', 'end')):
            if info.get(key):
                decl.append((end, info[key][0], info[key][1]))
        if 'run_frame_start' in info:
            decl.append(('start', 'Run forward', info['run_frame_start']))
        if 'run_frame_end' in info:
            decl.append(('end', 'Run forward', info['run_frame_end']))
        if info.get('starts_from'):
            decl.append(('start', info['starts_from'], 1))
        if info.get('ends_in'):
            decl.append(('end', info['ends_in'], 1))
        for end, other, f in decl:
            oa = bpy.data.actions.get(H.PREFIX+other)
            if not oa:
                continue
            oinfo = json.loads(oa['clip_json'])
            if loop and not oinfo.get('loop'):
                # the loop is authoritative: fix the other clip's matching end
                oend = 'end' if f == oinfo['frames'][1] else 'start' if f == oinfo['frames'][0] else None
                if oend:
                    out.append((other, oend, t, info['frames'][0]))
            else:
                if oinfo.get('loop'):
                    f0, f1 = oinfo['frames']
                    f = f0+(f-f0) % (f1-f0)
                out.append((t, end, other, f))
    return out


def seamfix(touched):
    for title, end, other, f in seam_pairs():
        if title not in touched and other not in touched:
            continue
        if title in ('Open Circuit end',) and other == 'Open Circuit hold':
            continue   # the arms follow Open Circuit arms hold (seam_from_arms)
        c, o = clip(title), clip(other)
        tgt = o.frames[fi(o, f)]
        i_end = 0 if end == 'start' else c.n-1
        cur = dict(c.frames[i_end])
        worst = max(qang(getq(cur, b), getq(tgt, b)) for b in ARMS if b in c.bones)
        cur_all = [(getq(cur, b), getq(tgt, b)) for b in ARMS]
        if all(abs(a.dot(b)) > 1-1e-12 for a, b in cur_all):
            continue
        noisy = worst < 0.3   # float32 key noise reads as ~0.05 deg; still closed exactly, not reported
        span = 3 if title == 'Jump' else max(2, min(8, c.n//2))
        for i, fr in enumerate(c.frames):
            dist = i if end == 'start' else c.n-1-i
            w = SS(1-dist/span)
            if w <= 0:
                continue
            if dist == 0:
                copy_bones(tgt, fr, ARMS)   # the seam frame itself: exact copy
                continue
            for b in ARMS:
                d = getq(tgt, b) @ getq(cur, b).inverted()
                setq(fr, b, qpow(d, w) @ getq(fr, b))
                for p in ('location', 'scale'):
                    setv(fr, b, getv(fr, b, p)+(getv(tgt, b, p)-getv(cur, b, p))*w, p)
        if not noisy:
            REPORT['seamfix'].append([title, end, other, f, round(worst, 2)])
            touch(title)


for st, fn in (('0', stage0), ('1', stage1), ('2', stage2), ('3', stage3), ('4', stage4), ('5', stage5), ('6', stage6)):
    if st in STAGES:
        fn()
        print('STAGE', st, 'done', flush=True)
for _ in range(2):
    seamfix(set(REPORT['new']+REPORT['changed']))


def abduct_curve(c, s, degs):
    """Per-frame outward swing of one upper arm (chest frame, as abduct); degs is 0 on the clip ends."""
    from mathutils import Matrix
    rig.animation_data.action = c.act
    rig.animation_data.action_slot = c.act.slots[0]
    sg = -1.0 if s == 'L' else 1.0
    b = f'{s} upperarm'
    for i, fr in enumerate(c.frames):
        if abs(degs[i]) < 1e-6:
            continue
        bpy.context.scene.frame_set(c.f0+i)
        ch = pb['chest'].matrix.to_3x3()
        A = ch @ poser.r3['chest'].inverted() @ poser.r3[b]
        R = Matrix.Rotation(math.radians(sg*degs[i]), 3, 'Y')
        setq(fr, b, (A.inverted() @ R @ A @ getq(fr, b).to_matrix()).to_quaternion())


# post-seam clearance: seam deltas from the new ready/loco arms brought the R hand onto the leg here
for title, ctr, wid, deg in (('Glide exit', 10, 3.0, 9.0), ('Run pivot 180 right', 3, 3.0, 8.0)):
    c = clip(title)
    abduct_curve(c, 'R', [deg*bump(i, ctr, wid) for i in range(c.n)])
    touch(title)
c = clip('Idle combat fidget')
abduct_curve(c, 'R', [4.0*S(i/(0.2*(c.n-1)))*S((c.n-1-i)/(0.2*(c.n-1))) for i in range(c.n)])
for c in CLIPS.values():
    if c.title in REPORT['new'] or c.title in REPORT['changed']:
        if c.loop:
            copy_bones(c.frames[0], c.frames[-1], ARMS)
        c.save()
rig.animation_data.action = None
OUTB.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUTB))
(REPDIR/'armpass-report.json').write_text(json.dumps(REPORT, indent=1), encoding='utf-8')
print('ARMPASS', json.dumps(REPORT), flush=True)
print('ARMPASS DONE', flush=True)
