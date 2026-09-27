"""Sprint -> thruster glide: glide enter (take-off from the run), glide loop, glide exit (back into the run).

Iron Man style: the body stays nearly upright (<= ~10 deg forward), hovering with the legs straight and
together under it; the feet are only slightly pointed so the heels face back along the travel line, where
the vfx.py heel thrusters stream their exhaust (the toes are not nozzles);
arms relaxed slightly back at the sides, tabard streaming. Legs are placed on IK (knees stay in the leg
plane via the pole). Enter and exit are per-part blends between `run.pose` snapshots and the glide pose,
so the first/last frames are exact run poses.
"""
import math
from mathutils import Matrix, Vector
from hs_anim import (R, bake, lerp, ramp, sign, wave, spring_follow, apply_world_delta, axis_rot, halo_offsets,
                     tabard_follow, MID_X, FPS)
import run

SPEED = 8.7
PREVIEW_ORTHO = 3.4
NL = 32            # glide loop frames (frame 1 repeated as NL+1)
NE = 13            # enter frames
NX = 14            # exit frames
ENTER_RUN_FRAME = 10   # run frame the enter starts on (R foot planted, one frame after contact)
EXIT_RUN_FRAME = 1     # run frame the exit ends on (L contact)

PITCH = 5.0           # pelvis forward tilt; with spine/chest the torso leans ~8 deg (v11: 36, chest-leading)
SPINE_X = 2.0
CHEST_X = 1.0
HOVER = 0.33          # pelvis lift: pointed toes clear the ground by ~17 cm, so the jets mostly clear it
LEG_ANGLE = 9.0       # legs nearly straight down, trailing slightly behind the hips
LEG_REACH = 0.862     # just short of full extension (0.881): a soft knee
ANKLE_X = 0.12        # legs together, with a small gap so the bulky claw feet don't press into each other
FOOT_PITCH = 28.0     # a slight point: the heel spur faces back along the travel line (heel thrusters)
TOE = 14.0
JET_DIR = dict(run=run.JET_DIR, lift=58.0, glide=28.0)   # exhaust tilt below horizontal (vfx hs_jet_dir)
FOOT_OUT = 4.0        # toe-out before pointing (more swings the heels into each other)


def s1(t, phase=0.0, h=1):
    return math.sin(2*math.pi*h*(t-phase))


# ----------------------------------------------------------------------------- glide pose
def place_leg(p, s, hip, angle, reach, ax, pitch, toe, yaw=0.0):
    """Leg IK by hip-relative angle (deg back from vertical) and hip-ankle reach; foot pointed.
    yaw toes the foot out (deg) before it is pointed, so pointed feet splay instead of crossing."""
    k = sign(s)
    x = MID_X+k*ax
    dx = x-hip.x
    d = math.sqrt(max(reach*reach-dx*dx, 1e-6))
    a = math.radians(angle)
    ankle = Vector((x, hip.y+d*math.sin(a), hip.z-d*math.cos(a)))
    out = R(z=k*yaw)
    rw = R(x=pitch) @ out
    m = (rw @ p.r3[f'{s} foot IK']).to_4x4()
    m.translation = ankle
    p.place(f'{s} foot IK', m)
    mid = (hip+ankle)*0.5
    pm = p.rest[f'{s} knee pole'].copy()
    pm.translation = mid+Vector((k*0.04, -0.45*math.cos(a), -0.45*math.sin(a)))
    p.place(f'{s} knee pole', pm)
    p.rot(f'{s} toe', rw.inverted() @ R(x=toe) @ out)


def glide_pose(p, t):
    """Upright thruster glide at loop phase t (0..1)."""
    bob = wave(t, 0.0)+0.25*wave(t, 0.06, 2)   # +1 high at t=0, small second lift
    sway = s1(t, 0.0)
    roll = s1(t, 0.1)
    buzz = 0.5*s1(t, 0.07, 5)+0.3*s1(t, 0.21, 9)  # thrust buzz through the body (integer harmonics)
    flut = lambda ph, h=4: wave(t, ph, h)+0.35*wave(t, ph+0.03, 6)
    pitch = PITCH+1.2*wave(t, 0.12)
    p.pelvis((0.012*sway, 0.0, HOVER+0.028*bob+0.003*buzz), R(x=pitch, y=2.5*roll, z=2.0*sway))
    p.rot('spine', R(x=SPINE_X-0.8*bob, y=-1.0*s1(t, 0.16), z=-1.0*sway))
    p.rot('chest', R(x=CHEST_X+0.6*wave(t, 0.2), y=-0.9*s1(t, 0.2), z=-0.7*s1(t, 0.05)))
    p.rot('neck', R(x=-3.0, y=-0.4*roll, z=-0.4*sway))
    p.rot('head', R(x=-2.5+1.0*wave(t, 0.1), y=-0.6*roll, z=0.5*s1(t, 0.3)))
    p.rot('halo root', R(x=-8.0+1.5*wave(t, 0.15)))
    for s in ('L', 'R'):
        k = sign(s)
        drift = s1(t, 0.2+0.25*(k < 0))
        # Relaxed at the sides, slightly back and flared for balance; palms turned in toward the thighs.
        p.arm(s, swing=17+2.5*wave(t, 0.18)+2.0*k*sway, adduct=-7+2.0*drift, elbow=12+3*wave(t, 0.25+0.1*k),
              twist=-10+2.0*drift, wrist=(14+4*wave(t, 0.3+0.1*k), 0, 0), forearm_twist=60+3.0*s1(t, 0.35))
        p.curl(s, 20+5*wave(t, 0.32+0.12*k, 2), 12+3*wave(t, 0.4+0.12*k, 2))
    lag = tabard_follow(lambda u: 2.0*s1(u), lambda u: 2.5*s1(u, 0.1), t, gain=1.6)
    # Streams back in the airflow; the front panel can only trail as far as the legs allow.
    for n, x, y in (('tabard front.1', 6+2.0*flut(0.0), 1.5*s1(t, 0.15)), ('tabard front.2', 6+3.5*flut(0.05), 2.0*s1(t, 0.22)),
                    ('tabard front.3', 8+6*flut(0.10), 3.0*s1(t, 0.3)), ('tabard back.1', 13+3*flut(0.02), 1.5*s1(t, 0.18)),
                    ('tabard back.2', 22+9*flut(0.08), 2.5*s1(t, 0.26))):
        p.rot(n, R(x=x, y=y+lag[n][0], z=lag[n][1]))
    p.update()
    for s in ('L', 'R'):
        k = sign(s)
        hip = p.world(f'{s} thigh').translation
        angle = LEG_ANGLE+1.5*k*s1(t, 0.2)-1.5*bob
        reach = LEG_REACH-0.012*(0.5+0.5*k*s1(t, 0.32))
        place_leg(p, s, hip, angle, reach, ANKLE_X, FOOT_PITCH+3.0*k*s1(t, 0.25), TOE+2.0*buzz, FOOT_OUT)


def body_pitch(caps, frames):
    """Forward lean of the pelvis -> neck line from vertical (deg), the silhouette's torso angle."""
    out = []
    for f in frames:
        d = caps[f]['neck'].translation-caps[f]['pelvis'].translation
        out.append(math.degrees(math.atan2(-d.y, d.z)))
    return {'body_pitch_max_deg': round(max(out), 2), 'body_pitch_mean_deg': round(sum(out)/len(out), 2)}


# ----------------------------------------------------------------------------- pose blending
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
                return s+' leg'
            return 'arms'
    return 'body'


def apply_blend(p, a, b, w, wloc=None):
    """a/b: snapshots; w: {group: weight toward b}; wloc: optional per-group weights for locations only
    (e.g. pointed feet flatten before their IK targets come down to the ground)."""
    for pb in p.pb:
        g = group(pb.name)
        t = w[g]
        la, qa = a[pb.name]
        lb, qb = b[pb.name]
        if qa.dot(qb) < 0:
            qb = -qb
        pb.location = la.lerp(lb, (wloc or {}).get(g, t))
        pb.rotation_quaternion = qa.slerp(qb, t)


def bump(x, a, b):
    """0 outside [a, b], smooth 1 at the middle."""
    return math.sin(math.pi*min(max((x-a)/(b-a), 0.0), 1.0))**2


def add_offset(p, name, vec):
    """Add a world-axis (rest frame) translation on top of the bone's current offset."""
    p.pb[name].location = p.pb[name].location+p.r3[name].inverted() @ Vector(vec)


def run_snap(p, f):
    p.reset()
    run.pose(p, f)
    return snap(p)


def glide_snap(p, t):
    p.reset()
    glide_pose(p, t % 1.0)
    return snap(p)


# ----------------------------------------------------------------------------- clips
def enter_pose(p, f):
    i = f-1
    a = run_snap(p, ENTER_RUN_FRAME+i)
    b = glide_snap(p, (i-(NE-1))/NL)
    w = {'R leg': ramp(i, 3.5, 9.0), 'L leg': ramp(i, 0.0, 7.5), 'body': ramp(i, 1.0, 10.0),
         'arms': ramp(i, 0.0, 9.0), 'tabard': ramp(i, 3.0, 11.0), 'halo': ramp(i, 1.0, 10.0)}
    p.reset()
    apply_blend(p, a, b, w)
    # Push-off accent: sink into the planted leg, then pop up before settling to hover height (wide enough
    # that the dip stays within the run's own bob acceleration).
    add_offset(p, 'pelvis', (0, 0, -0.02*bump(i, 0.0, 5.5)+0.03*bump(i, 4.0, 11.5)))


def loop_pose(p, f):
    glide_pose(p, ((f-1) % NL)/NL)


def exit_pose(p, f):
    i = f-1
    a = glide_snap(p, i/NL)
    b = run_snap(p, EXIT_RUN_FRAME-(NX-1-i))
    w = {'R leg': ramp(i, 4.0, 10.0), 'L leg': ramp(i, 2.0, 8.5), 'body': ramp(i, 4.0, 12.5),
         'arms': ramp(i, 2.0, 11.0), 'tabard': ramp(i, 4.0, 13.0), 'halo': ramp(i, 2.0, 12.0)}
    wloc = {'R leg': ramp(i, 6.0, 12.0), 'L leg': ramp(i, 4.0, 11.5)}
    p.reset()
    apply_blend(p, a, b, w, wloc)
    # Touch-down: the torso dips forward into the run lean as the feet reach the ground, then the body
    # sinks into the stride. Both bumps are zero on the last frame, so it lands exactly on the run pose.
    add_rot(p, 'pelvis', R(x=5.0*bump(i, 6.0, 12.5)))
    add_offset(p, 'pelvis', (0, 0, -0.035*bump(i, 7.0, 13.0)))


def add_rot(p, name, m3):
    """Apply a world-axis (rest frame) rotation on top of the bone's current local rotation."""
    r3 = p.r3[name]
    p.pb[name].rotation_quaternion = (r3.inverted() @ m3 @ r3).to_quaternion() @ p.pb[name].rotation_quaternion


# ----------------------------------------------------------------------------- halo follow-through
def halo_points(p, pose_fn, frames):
    pts = []
    for f in frames:
        p.reset()
        pose_fn(p, f)
        p.update()
        pts.append(p.world('halo root').translation.copy())
    return pts


HALO_SPRING = (70.0, 11.0)   # stiffness, damping (hs_anim.halo_lag defaults)
HALO_MAX = 0.035
HALO_TILT = 70.0


def clamp_len(o, m=HALO_MAX):
    return o*(m/o.length) if o.length > m else o


def spring_periodic(pts, stiffness, damping, substeps=4, passes=6):
    """Periodic damped follow over one cycle (no repeated last frame), so the loop seam is exact."""
    dt = 1.0/FPS/substeps
    x = pts[-1].copy()
    v = Vector((0, 0, 0))
    out = []
    for _ in range(passes):
        out = []
        for i, q in enumerate(pts):
            prev = pts[i-1]
            for k in range(substeps):
                a = stiffness*(prev.lerp(q, (k+1)/substeps)-x)-damping*v
                v += a*dt
                x += v*dt
            out.append(x.copy())
    return out


def loop_offsets(p):
    pts = halo_points(p, loop_pose, list(range(1, NL+1)))
    return [clamp_len(q-q0) for q0, q in zip(pts, spring_periodic(pts, *HALO_SPRING))]


def run_offsets(p):
    """Halo offsets the run clip carries per run frame 1..N (same evaluation as hs_anim.halo_lag);
    index with (f-1) % run.N."""
    frames = list(range(1, run.N+2))
    pts = halo_points(p, run.pose, frames)
    return halo_offsets(pts, True, **run.HALO)[:run.N]


def apply_halo(p, caps, frames, offsets):
    for f, o in zip(frames, offsets):
        q0 = caps[f]['halo root'].translation.copy()
        horizontal = Vector((o.x, o.y, 0))
        tilt = Matrix.Identity(4)
        if horizontal.length > 1e-6:
            axis = Vector((0, 0, 1)).cross(horizontal).normalized()
            tilt = axis_rot(axis, -HALO_TILT*horizontal.length).to_4x4()
        apply_world_delta(p, caps[f], 'halo root', Matrix.Translation(q0+o) @ tilt @ Matrix.Translation(-q0))
    return {'halo_max_offset_m': round(max(o.length for o in offsets), 4)}


def one_shot_offsets(caps, frames, preroll, start, end, fade=4):
    """Spring follow with pre-roll, cross-faded onto the neighbouring clips' halo offsets at both ends.
    start/end: per-frame offsets of the neighbouring clip over this clip's frames (it keeps moving through
    the fade; a frozen offset put a 7 cm/f^2 halo kick into the glide enter)."""
    pts = [caps[f]['halo root'].translation.copy() for f in frames]
    fol = spring_follow(list(preroll)+pts, False, *HALO_SPRING)[len(preroll):]
    n = len(frames)
    start = start if isinstance(start, (list, tuple)) else [start]*n
    end = end if isinstance(end, (list, tuple)) else [end]*n
    out = []
    for i, (q0, q) in enumerate(zip(pts, fol)):
        o = clamp_len(q-q0)
        o = o.lerp(start[i], 1-ramp(i, 0, fade-1))
        o = o.lerp(end[i], ramp(i, n-fade-1, n-1))
        out.append(o)
    return out


def post_loop(p, caps, frames):
    pts = [caps[f]['halo root'].translation.copy() for f in frames[:-1]]
    offs = [clamp_len(q-q0) for q0, q in zip(pts, spring_periodic(pts, *HALO_SPRING))]
    return {**apply_halo(p, caps, frames, offs+offs[:1]), **body_pitch(caps, frames)}


def post_enter(p, caps, frames):
    preroll = halo_points(p, run.pose, [ENTER_RUN_FRAME-run.N*2+k for k in range(run.N*2)])
    ro, lo, n = run_offsets(p), loop_offsets(p), len(frames)
    start = [ro[(ENTER_RUN_FRAME-1+i) % run.N] for i in range(n)]
    end = [lo[(i-(n-1)) % NL] for i in range(n)]
    offs = one_shot_offsets(caps, frames, preroll, start, end)
    return {**apply_halo(p, caps, frames, offs), **body_pitch(caps, frames)}


def post_exit(p, caps, frames):
    preroll = halo_points(p, loop_pose, list(range(1, NL+1))*2)
    ro, lo, n = run_offsets(p), loop_offsets(p), len(frames)
    start = [lo[i % NL] for i in range(n)]
    end = [ro[(EXIT_RUN_FRAME-1-(n-1-i)) % run.N] for i in range(n)]
    offs = one_shot_offsets(caps, frames, preroll, start, end)
    return {**apply_halo(p, caps, frames, offs), **body_pitch(caps, frames)}


# ----------------------------------------------------------------------------- VFX (vfx.py: hs_glow / hs_jet)
def glow_loop(t):
    return 1.0+0.12*wave(t, 0.0, 2)-0.12


def jet_loop(t):
    """Thrust flicker, periodic in the glide loop (integer harmonics) so the wrap is exact."""
    return 1.0+0.10*math.sin(2*math.pi*3*t)+0.06*math.sin(2*math.pi*(7*t+0.15))+0.04*math.sin(2*math.pi*(11*t+0.4))


def enter_props():
    """The right foot's last push-off (run frame ~12) is the lift-off: its run spark hands over to both heel
    jets igniting with a burst, the exhaust swinging down to lift, then easing back to the glide angle."""
    t = lambda f: (f-1-(NE-1))/NL
    rf = lambda f: ENTER_RUN_FRAME+f-1
    fade = lambda f: 1.0-ramp(f-1, 2.0, 4.0)
    return {'hs_glow': lambda f: ramp(f-1, 1.5, 10.0)*glow_loop(t(f)),
            'hs_jet': lambda f: ramp(f-1, 2.5, 5.0)*jet_loop(t(f))+0.65*bump(f-1, 2.5, 9.0),
            'hs_spark_L': lambda f: fade(f)*run.spark(rf(f), 'L'),
            'hs_spark_R': lambda f: fade(f)*run.spark(rf(f), 'R'),
            'hs_jet_dir': lambda f: lerp(lerp(JET_DIR['run'], JET_DIR['lift'], ramp(f-1, 1.5, 4.0)),
                                         JET_DIR['glide'], ramp(f-1, 5.0, 11.0))}


def loop_props():
    return {'hs_glow': lambda f: glow_loop(((f-1) % NL)/NL), 'hs_jet': lambda f: jet_loop(((f-1) % NL)/NL),
            'hs_jet_dir': lambda f: JET_DIR['glide']}


def exit_props():
    """Jets cut over three frames as the legs swing down; the feet land on run frame 1 with no spark."""
    return {'hs_glow': lambda f: (1.0-ramp(f-1, 1.0, 10.0))*glow_loop((f-1)/NL),
            'hs_jet': lambda f: (1.0-ramp(f-1, 0.5, 3.5))*jet_loop((f-1)/NL),
            'hs_jet_dir': lambda f: JET_DIR['glide']}


def build(p):
    meta = {'kind': 'locomotion', 'speed_mps': SPEED}
    return [
        bake(p, 'Glide enter', list(range(1, NE+1)), enter_pose, False,
             markers={'Push off': 3, 'Airborne': 5},
             meta={**meta, 'speed_mps': run.G['speed'], 'run_frame_start': ENTER_RUN_FRAME}, post=post_enter,
             props=enter_props()),
        bake(p, 'Glide loop', list(range(1, NL+2)), loop_pose, True,
             markers={'Hover high': 1, 'Hover low': 1+NL//2},
             meta={**meta, 'meters_per_cycle': round(SPEED*NL/FPS, 3)}, post=post_loop, props=loop_props()),
        bake(p, 'Glide exit', list(range(1, NX+1)), exit_pose, False,
             markers={'Legs down': 5, 'L contact': NX},
             meta={**meta, 'run_frame_end': EXIT_RUN_FRAME}, post=post_exit, props=exit_props()),
    ]
