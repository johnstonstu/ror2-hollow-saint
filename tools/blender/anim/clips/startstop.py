"""Run start / Run stop (item 10): short transition clips between Idle and Run forward, so neither handoff
has to be a cross-fade (a fade slides the planted feet and drops the run's momentum in one blend).

- Run stop (16 f): enters on Run forward f1 (L contact; Glide exit ends there too). R makes a braking plant
  (f6), the pelvis sits back and drops, the torso pitches on over it from momentum, L steps up beside it (f11)
  and the body settles into Idle f1. The tabard swings on forward and settles.
  Run stop R: the mirror from Run forward f9 (R contact; L brakes), so a stop starts within 8 frames.
- Run start (17 f): from Idle f1, a short first step with L (lands between f7 and f8), R drives off and makes
  Run forward's R contact on the last frame (Run forward f9). The body leans into the drive and the tabard
  trails back.

Built on turns.py's footstep engine: the body's world path (meta speed_curve, m/s per frame, for Unity root
motion) and world-planted feet mapped into character space, so a planted foot never slides. Body, arms, tabard
and halo cross-fade per part between the Idle f1 and Run forward snapshots (glide.py's method), so both ends are
exact seams (meta seam_from / seam_to).
"""
from mathutils import Vector

from hs_anim import R, bake, ramp, sign
import air
import presentation as P
import run
import turns as T
from glide import snap, apply_blend, add_rot, add_offset, bump, halo_points, one_shot_offsets, apply_halo

N = run.N
V0 = run.G['speed']
STOP_N = 16
START_N = 17
IDLE = None


def stand(s):
    return Vector(air.ball_xy(s))


def idle_snap(p):
    p.reset()
    P.idle_pose(p, 1)
    return snap(p)


def planted_until_end(path, i, s):
    """Character xy at frame i of the world spot that is the stand spot of foot s on the last frame."""
    return tuple(path.to_char(i, path.to_world(path.n-1, stand(s))))


def engine_feet(p, feet, i, follow=None):
    """follow = (run frame, weight): ease the engine's foot targets onto Run forward's (ball, pitch, toe) at
    that frame, so the leg blend back to the run snapshot has nothing left to correct."""
    m = snap(p)
    for s in ('L', 'R'):
        ball, pitch, yaw = feet[s].at(i)
        toe = 0.0
        if follow and follow[1] > 0.0:
            rball, rpitch, rtoe = T.run_feet(follow[0])[s]
            w = follow[1]
            ball, pitch, toe, yaw = ball.lerp(rball, w), pitch+(rpitch-pitch)*w, rtoe*w, yaw*(1.0-w)
        p.foot(s, tuple(ball), pitch, toe, yaw=yaw)
    return m, snap(p)


# ----------------------------------------------------------------------------- Run stop
def stop_path():
    return T.Path(STOP_N, lambda i: Vector((0.0, -V0*(1.0-T.smooth(i/10.0)))), lambda i: 0.0)


STOP_PATH = stop_path()


STOP_TABARD = (('tabard front.1', -12.0, 3.0), ('tabard front.2', -7.0, 3.5), ('tabard front.3', -7.0, 4.0),
               ('tabard back.1', -4.0, 3.0), ('tabard back.2', -4.0, 3.5))


class Stop:
    """entry: Run forward frame the stop starts on, 1 (L contact: R brakes, L steps up) or 9 (R contact: mirrored)."""

    def __init__(self, entry):
        self.entry = entry
        self.lead = 'L' if entry == 1 else 'R'           # planted on frame 1, steps up beside at the end
        self.brake = 'R' if entry == 1 else 'L'          # swinging on frame 1, makes the braking plant
        self.title = 'Run stop' if entry == 1 else 'Run stop R'
        path, last = STOP_PATH, STOP_N-1
        ra = T.run_feet(entry)
        yaw = lambda s: sign(s)*air.STAND_YAW
        lead, brake = self.lead, self.brake
        self.feet = {
            lead: T.Foot(path, [(0, T.contact(lead, entry), 0.0, True),
                                (T.RUN_LIFT, None, 0.0, False),
                                (10, planted_until_end(path, 10, lead), yaw(lead), True),
                                (last, planted_until_end(path, last, lead), yaw(lead), True)], lift=0.12),
            brake: T.Foot(path, [(0, (ra[brake][0].x, ra[brake][0].y), 0.0, False),
                                 (5, planted_until_end(path, 5, brake), yaw(brake)*0.5, True),
                                 (last, planted_until_end(path, last, brake), yaw(brake), True)], lift=0.14),
        }

    def pose(self, p, f):
        i, n = f-1, STOP_N
        a = T.run_snap(p, self.entry+i)
        p.reset()
        body = ramp(i, 1.0, 11.0)
        apply_blend(p, a, IDLE, {'R leg': body, 'L leg': body, 'body': body, 'arms': ramp(i, 1.0, 12.0),
                                 'tabard': ramp(i, 2.0, 13.0), 'halo': ramp(i, 1.0, 12.0)})
        m, e = engine_feet(p, self.feet, i)
        wl = ramp(i, 0.0, 3.0)*(1.0-ramp(i, n-5.0, n-1.0))
        p.reset()
        apply_blend(p, m, e, {'R leg': wl, 'L leg': wl, 'body': 0.0, 'arms': 0.0, 'tabard': 0.0, 'halo': 0.0})
        brake = bump(i, 1.0, 11.0)          # pelvis sits back and drops over the braking plant
        surge = bump(i, 4.0, 14.0)          # the torso carries on forward over it, then settles
        add_offset(p, 'pelvis', (0, 0, -0.055*brake))
        add_rot(p, 'pelvis', R(x=-8.0*brake))
        add_rot(p, 'spine', R(x=4.0*surge))
        add_rot(p, 'chest', R(x=5.0*surge))
        add_rot(p, 'neck', R(x=-1.5*surge))
        add_rot(p, 'head', R(x=-5.0*surge))
        for name, x, t0 in STOP_TABARD:
            add_rot(p, name, R(x=x*bump(i, t0, 15.0)))

    def props(self):
        def spark(side):
            def fn(f):
                i = f-1
                carry = run.spark(self.entry+i, side)*(1.0-ramp(i, 2.0, 5.0))
                plant = run.SPARK*bump(i, 4.0, 9.0) if side == self.brake else 0.5*run.SPARK*bump(i, 9.0, 13.0)
                return max(carry, plant)
            return fn
        moving = lambda f: STOP_PATH.v[f-1].length > 0.3
        return {'hs_spark_L': spark('L'), 'hs_spark_R': spark('R'),
                'hs_jet_dir': lambda f: run.JET_DIR*(1.0-ramp(f-1, 8.0, STOP_N-1.0)),
                'hs_move_x': lambda f: 0.0, 'hs_move_y': lambda f: 1.0 if moving(f) else 0.0}

    def post(self, p, caps, frames):
        n = len(frames)
        ro = T.run_offsets(p)
        pre = halo_points(p, run.pose, [self.entry-N*2+j for j in range(N*2)])
        offs = one_shot_offsets(caps, frames, pre, [ro[(self.entry-1+i) % N] for i in range(n)],
                                [Vector((0, 0, 0))]*n)
        out = apply_halo(p, caps, frames, offs)
        out.update(P.checks(p, caps, frames, False))
        return out


# ----------------------------------------------------------------------------- Run start
def start_path():
    """Two frames past the clip, so the last-frame R contact gets a planted-foot touchdown velocity."""
    return T.Path(START_N+2, lambda i: Vector((0.0, -V0*T.smooth((i-1.0)/12.0))), lambda i: 0.0)


START_PATH = start_path()
START_L_LIFT = 2
START_L_LAND = 6.5                 # a short first step; R lifts right after, before the body outruns it
START_R_LIFT = 7.0


def start_feet():
    path, last = START_PATH, START_N-1
    rb = T.run_feet(9)
    return {
        'L': T.Foot(path, [(0, tuple(stand('L')), air.STAND_YAW, True),
                           (START_L_LIFT, None, air.STAND_YAW, False),
                           (START_L_LAND, T.contact('L', 1), 0.0, True),
                           (START_L_LAND+T.RUN_LIFT, None, 0.0, False),
                           (last, (rb['L'][0].x, rb['L'][0].y), 0.0, False)], lift=0.10),
        'R': T.Foot(path, [(0, tuple(stand('R')), -air.STAND_YAW, True),
                           (START_R_LIFT-1, None, 0.0, True),
                           (START_R_LIFT, None, 0.0, False),
                           (last, T.contact('R', 9), 0.0, True),
                           (last+1, None, 0.0, True)], lift=0.16),
    }


START_FEET = start_feet()
START_TABARD = (('tabard front.1', 8.0, 2.0), ('tabard front.2', 6.0, 2.5), ('tabard front.3', 6.0, 3.0),
                ('tabard back.1', 4.0, 2.0), ('tabard back.2', 4.0, 2.5))


def start_run_frame(i):
    return 9-(START_N-1-i)


def start_pose(p, f):
    i, n = f-1, START_N
    b = T.run_snap(p, start_run_frame(i))
    p.reset()
    body = ramp(i, 1.0, 11.0)
    apply_blend(p, IDLE, b, {'R leg': body, 'L leg': body, 'body': body, 'arms': ramp(i, 1.0, 10.0),
                             'tabard': ramp(i, 2.0, 12.0), 'halo': ramp(i, 1.0, 11.0)})
    m, e = engine_feet(p, START_FEET, i, (start_run_frame(i), ramp(i, n-7.0, n-2.0)))
    wl = ramp(i, 0.0, 2.0)*(1.0-ramp(i, n-3.0, n-1.0))
    p.reset()
    apply_blend(p, m, e, {'R leg': wl, 'L leg': wl, 'body': 0.0, 'arms': 0.0, 'tabard': 0.0, 'halo': 0.0})
    drive = bump(i, 0.0, 11.0)          # drop and lean into the first steps
    add_offset(p, 'pelvis', (0, 0, -0.045*drive))
    add_rot(p, 'pelvis', R(x=6.0*drive))
    add_rot(p, 'chest', R(x=3.0*drive))
    add_rot(p, 'neck', R(x=-2.0*drive))
    add_rot(p, 'head', R(x=-6.0*drive))
    for name, x, t0 in START_TABARD:
        add_rot(p, name, R(x=x*bump(i, t0, n-1.0)))


def start_push(i, side):
    lift = START_R_LIFT if side == 'R' else START_L_LIFT
    d = i-(lift-1.5)
    return (1.0 if side == 'R' else 0.5)*run.SPARK*bump(d, 0.0, 4.0) if 0.0 <= d < 4.0 else 0.0


def start_props():
    def spark(side):
        def fn(f):
            i = f-1
            return max(run.spark(start_run_frame(i), side)*ramp(i, START_N-4.0, START_N-2.0), start_push(i, side))
        return fn
    moving = lambda f: START_PATH.v[f-1].length > 0.3
    return {'hs_spark_L': spark('L'), 'hs_spark_R': spark('R'),
            'hs_jet_dir': lambda f: run.JET_DIR*ramp(f-1, 0.0, 6.0),
            'hs_move_x': lambda f: 0.0, 'hs_move_y': lambda f: 1.0 if moving(f) else 0.0}


def start_post(p, caps, frames):
    n = len(frames)
    ro = T.run_offsets(p)
    pre = halo_points(p, lambda q, f: P.idle_pose(q, 1), [1]*12)
    offs = one_shot_offsets(caps, frames, pre, [Vector((0, 0, 0))]*n,
                            [ro[(start_run_frame(i)-1) % N] for i in range(n)])
    out = apply_halo(p, caps, frames, offs)
    out.update(P.checks(p, caps, frames, False))
    return out


# ----------------------------------------------------------------------------- build
def build(p):
    global IDLE
    P.arc_geometry(p)
    P.STAND = P.stand_state()
    IDLE = idle_snap(p)
    meta = {'kind': 'locomotion', 'halo_clear_dir': [0.0, 0.0, 1.0]}
    out = []
    for st in (Stop(1), Stop(9)):
        other = 'R' if st.lead == 'L' else 'L'
        out.append(bake(p, st.title, list(range(1, STOP_N+1)), st.pose, False,
                        markers={f'{st.lead} contact': 1, f'{other} plant': 6, f'{st.lead} plant': 11, 'Stopped': STOP_N},
                        meta={**meta, 'speed_mps': V0, 'speed_curve': [round(v.length, 3) for v in STOP_PATH.v],
                              'seam_from': ['Run forward', st.entry], 'seam_to': ['Idle', 1],
                              'role': f"Run forward -> Idle from the {st.lead} contact (Run forward f{st.entry}"
                                      f"{'; Glide exit ends there too' if st.entry == 1 else ''}): on stick release "
                                      "play whichever stop comes first (at most 8 frames)"},
                        post=st.post, props=st.props()))
    return out+[
        bake(p, 'Run start', list(range(1, START_N+1)), start_pose, False,
             markers={'L step': round(START_L_LAND+1), 'R contact': START_N},
             meta={**meta, 'speed_mps': V0, 'speed_curve': [round(v.length, 3) for v in START_PATH.v[:START_N]],
                   'seam_from': ['Idle', 1], 'seam_to': ['Run forward', 9],
                   'role': 'Idle -> Run forward: continue into Run forward f10'},
             post=start_post, props=start_props()),
    ]
