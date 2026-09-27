"""Turning (item 9h): sprint lean left/right loops, Run pivot 180 left/right, Plant turn 90 left/right.

The game turns the model (CharacterDirection); these clips are authored in character space (facing -Y, left = +X),
with the body's world path given per frame (speed, heading, yaw). Planted feet are fixed in WORLD space and
mapped into character space through that path, so they counter-rotate under the turning body without sliding.
The turn progress is keyed on `hs_turn` (0..1 of meta `turn_deg`, whose sign is the side: + = left) so Unity can
drive the model yaw
from the curve, and `hs_move_x/y` carry the character-space travel direction per frame.

Entry and exit are per-part blends with `run.pose` snapshots (glide.py's method), so frame 1 is Run forward f1
(L contact) and the last frame Run forward f9 (R contact): exact seams both ways.
"""
import math

from mathutils import Vector

from hs_anim import R, FPS, bake, halo_offsets, lerp, ramp, sign
import gait
import run
from glide import snap, apply_blend, add_rot, add_offset, bump, halo_points, one_shot_offsets, apply_halo

N = run.N


def smooth(x):
    x = min(max(x, 0.0), 1.0)
    return x*x*(3-2*x)


def rot2(v, a):
    c, s = math.cos(a), math.sin(a)
    return Vector((c*v.x-s*v.y, s*v.x+c*v.y))


# ----------------------------------------------------------------------------- run reference
def run_feet(f):
    """Run forward's foot targets at frame f: {side: (ball xyz, pitch, toe)}."""
    t = ((f-1) % N)/N
    out = {}
    for s, ph in (('L', 0.0), ('R', 0.5)):
        y, lift, pitch, toe = gait.foot_cycle(t-ph, run.G)
        out[s] = (Vector((run.FOOT_MID+sign(s)*run.WIDTH, y, gait.BALL_Z+lift)), pitch, toe)
    return out


def run_snap(p, f):
    p.reset()
    run.pose(p, f)
    return snap(p)


# ----------------------------------------------------------------------------- lean loops
LEAN = 11.0        # body roll into the turn (deg), pelvis + spine + chest
LEAN_FEET = 0.06   # feet land toward the outside of the curve (the body leans in over them)
LEAN_TOE = 6.0     # toes point into the curve


def make_lean(k):
    """k = +1 leans/turns left, -1 right. Run forward's cycle (same timing, same contacts) so a 1D blend by turn
    rate with Run forward can't slide the feet more than the lateral offset."""
    def pose(p, f):
        run.pose(p, f)
        for s, (ball, pitch, toe) in run_feet(f).items():
            p.foot(s, (ball.x-k*LEAN_FEET, ball.y, ball.z), pitch, toe, yaw=k*LEAN_TOE)
        add_offset(p, 'pelvis', (0, 0, -0.025))    # the outboard feet reach further: keep the knees soft
        add_rot(p, 'pelvis', R(y=k*LEAN*0.55, z=k*4.0))
        add_rot(p, 'spine', R(y=k*LEAN*0.25))
        add_rot(p, 'chest', R(y=k*LEAN*0.2, z=k*5.0))
        add_rot(p, 'neck', R(y=-k*LEAN*0.45, z=k*3.0))
        add_rot(p, 'head', R(y=-k*LEAN*0.35, z=k*4.0))
    return pose


# ----------------------------------------------------------------------------- footstep engine
class Path:
    """Body world path: velocity (world XY, m/s) and yaw (rad, + = left) per frame index i (0-based).
    anchor = (i0, i1, c0, c1): a pivot foot planted over i0..i1 whose character-space position eases c0 -> c1;
    over that window the body orbits the foot (its position follows from the foot) instead of the velocity, so
    a pivot turns about the planted ball rather than about the pelvis."""

    def __init__(self, n, vel, yaw, anchor=None):
        self.n = n
        self.yaw = [yaw(i) for i in range(n)]
        vin = [vel(i) for i in range(n)]
        self.pos = [Vector((0.0, 0.0))]
        w = None
        for i in range(1, n):
            if anchor and anchor[0] < i <= anchor[1]:
                i0, i1, c0, c1 = anchor
                if w is None:
                    w = self.pos[i0]+rot2(Vector(c0), self.yaw[i0])
                c = Vector(c0).lerp(Vector(c1), smooth((i-i0)/(i1-i0)))
                self.pos.append(w-rot2(c, self.yaw[i]))
            else:
                self.pos.append(self.pos[-1]+(vin[i-1]+vin[i])*0.5/FPS)
        self.v = [(self.pos[min(i+1, n-1)]-self.pos[max(i-1, 0)])*FPS/(min(i+1, n-1)-max(i-1, 0))
                  for i in range(n)]

    def _at(self, seq, i):
        j = min(max(int(math.floor(i)), 0), self.n-2)
        u = min(max(i-j, 0.0), 1.0)
        a, b = seq[j], seq[j+1]
        return a.lerp(b, u) if isinstance(a, Vector) else lerp(a, b, u)

    def yaw_at(self, i):
        return self._at(self.yaw, i)

    def to_char(self, i, w):
        return rot2(w-self._at(self.pos, i), -self.yaw_at(i))

    def to_world(self, i, c):
        return self._at(self.pos, i)+rot2(c, self.yaw_at(i))

    def move(self, i):
        """Character-space travel direction (hs_move_x = character right, hs_move_y = forward)."""
        c = rot2(self.v[i], -self.yaw[i])
        if c.length < 0.3:
            return (0.0, 1.0)
        c.normalize()
        return (-c.x, -c.y)


RELEASE_MATCH = 0.5
CONTACT_MATCH = 0.7
PEEL = 2.5          # frames the heel takes to peel up before a lift-off (shorter stalls then jerks the shin)


class Foot:
    """Keys: (i, char xy at that frame or None = where the previous key was in world, char yaw deg at that frame,
    planted). A planted key holds the ball fixed in world until the next key: to a planted key the foot pivots
    on the ball (yaw eases), to an unplanted one it peels the heel up and lifts off there (that key is None).
    Swings run from key to key in world space with a lift arc; unplanted keys after the first are via points."""

    def __init__(self, path, keys, lift=0.22, pushoff=30.0, land_pitch=12.0):
        self.path, self.lift, self.pushoff, self.land_pitch = path, lift, pushoff, land_pitch
        self.keys = []
        for i, c, yaw, planted in keys:
            w = self.keys[-1][1] if c is None else path.to_world(i, Vector(c))
            self.keys.append((i, w, math.radians(yaw)+path.yaw_at(i), planted))

    def at(self, i):
        ks = self.keys
        if i <= ks[0][0]:
            k = ks[0]
            return self._out(i, k[1], k[2], 0.0, 0.0)
        for a, b in zip(ks, ks[1:]):
            if a[0] <= i <= b[0]:
                if a[3]:
                    u = (i-a[0])/(b[0]-a[0])
                    lift = next((k2[0] for k1, k2 in zip(ks, ks[1:]) if k1[3] and not k2[3] and k2[0] >= i), None)
                    pitch = self.pushoff*smooth((i-(lift-PEEL))/PEEL) if lift is not None else 0.0
                    return self._out(i, a[1], lerp(a[2], b[2], smooth(u)), 0.0, pitch)
                # swing: eased in character space between where the foot left and where it lands (a world-space
                # line would bow across the midline under the turning body)
                # Hermite tangents carry the planted foot's character-space velocity out of the lift-off and
                # into the touch-down (gait.py's release_match / contact_match), so neither snaps.
                u = (i-a[0])/(b[0]-a[0])
                T = b[0]-a[0]
                P = self.path
                ca, cb = P.to_char(a[0], a[1]), P.to_char(b[0], b[1])
                va = (ca-P.to_char(max(a[0]-1, 0), a[1]))*RELEASE_MATCH if a[0] > 0 else Vector((0.0, 0.0))
                vb = (P.to_char(min(b[0]+1, P.n-1), b[1])-cb)*CONTACT_MATCH if b[3] and b[0] < P.n-1 else \
                    Vector((0.0, 0.0))
                u2, u3 = u*u, u*u*u
                c = (2*u3-3*u2+1)*ca+(u3-2*u2+u)*T*va+(-2*u3+3*u2)*cb+(u3-u2)*T*vb
                yaw = lerp(a[2]-P.yaw_at(a[0]), b[2]-P.yaw_at(b[0]), smooth(u))
                z = self.lift*math.sin(math.pi*u)**1.6
                pitch = lerp(self.pushoff, self.land_pitch, smooth(u))*(1.0-smooth((u-0.8)/0.2)*0.6)
                return Vector((c.x, c.y, gait.BALL_Z+z)), pitch, math.degrees(yaw)
        k = ks[-1]
        return self._out(i, k[1], k[2], 0.0, 0.0)

    def _out(self, i, w, yaw, z, pitch):
        c = self.path.to_char(i, w)
        return Vector((c.x, c.y, gait.BALL_Z+z)), pitch, math.degrees(yaw-self.path.yaw_at(i))


RUN_LIFT = run.G['stance']*N    # frames after its f1 contact that Run forward's L foot lifts off


def contact(s, f):
    """Run forward's foot position at frame f in character space (xy)."""
    b = run_feet(f)[s][0]
    return (b.x, b.y)


# ----------------------------------------------------------------------------- turn clips
class Turn:
    def __init__(self, title, k, n, deg, path, feet, entry=1, exit_=9, accent=None):
        self.title, self.k, self.n, self.deg, self.path, self.feet = title, k, n, deg, path, feet
        self.entry, self.exit, self.accent = entry, exit_, accent or {}

    def run_frame_a(self, i):
        return self.entry+i

    def run_frame_b(self, i):
        return self.exit-(self.n-1-i)

    def pose(self, p, f):
        i, n, k = f-1, self.n, self.k
        a = run_snap(p, self.run_frame_a(i))
        b = run_snap(p, self.run_frame_b(i))
        acc = self.accent
        body = ramp(i, acc.get('body0', 2.0), acc.get('body1', n-4.0))
        p.reset()
        apply_blend(p, a, b, {'R leg': body, 'L leg': body, 'body': body, 'arms': body, 'tabard': body,
                              'halo': body})
        m = snap(p)
        # engine feet
        for s in ('L', 'R'):
            ball, pitch, yaw = self.feet[s].at(i)
            p.foot(s, tuple(ball), pitch, 0.0, yaw=yaw)
        e = snap(p)
        wl = ramp(i, 0.0, acc.get('leg_in', 3.0))*(1.0-ramp(i, n-1-acc.get('leg_out', 4.0), n-1))
        p.reset()
        apply_blend(p, m, e, {'R leg': wl, 'L leg': wl, 'body': 0.0, 'arms': 0.0, 'tabard': 0.0, 'halo': 0.0})
        # accents: zero at both ends
        turn = self.turn_rate(i)
        brake = bump(i, acc.get('brake0', 0.0), acc.get('brake1', n*0.6))
        add_offset(p, 'pelvis', (0, 0, -acc.get('drop', 0.07)*brake))
        add_rot(p, 'pelvis', R(x=-acc.get('sit', 8.0)*brake, y=k*acc.get('lean', 10.0)*turn, z=-k*6.0*turn))
        add_rot(p, 'spine', R(y=k*acc.get('lean', 10.0)*0.3*turn, z=k*8.0*turn))
        add_rot(p, 'chest', R(z=k*12.0*turn))
        add_rot(p, 'neck', R(y=-k*acc.get('lean', 10.0)*0.4*turn, z=k*10.0*turn))
        add_rot(p, 'head', R(y=-k*acc.get('lean', 10.0)*0.3*turn, z=k*14.0*turn))

    def turn_rate(self, i):
        """0..1 envelope of the yaw rate (the torso and head lead the turn by it), zero at both ends."""
        y = self.path.yaw
        rates = [abs(y[min(j+1, self.n-1)]-y[max(j-1, 0)]) for j in range(self.n)]
        peak = max(rates) or 1.0
        return (rates[i]/peak)*(1.0-ramp(i, self.n-3, self.n-1))*ramp(i, 0, 2)

    def props(self):
        n = self.n
        yaw0 = self.path.yaw

        def spark(side):
            def fn(f):
                i = f-1
                ra = run.spark(self.run_frame_a(i), side)*(1.0-ramp(i, 1.0, 3.0))
                rb = run.spark(self.run_frame_b(i), side)*ramp(i, n-4.0, n-2.0)
                return max(ra, rb, self.push_spark(i, side))
            return fn
        return {'hs_turn': lambda f: yaw0[f-1]/math.radians(self.deg)*self.k if self.deg else 0.0,
                'hs_move_x': lambda f: self.path.move(f-1)[0], 'hs_move_y': lambda f: self.path.move(f-1)[1],
                'hs_spark_L': spark('L'), 'hs_spark_R': spark('R'), 'hs_jet_dir': lambda f: run.JET_DIR}

    def push_spark(self, i, side):
        """A spark on each engine lift-off (the planted foot's heel kicks as it leaves)."""
        out = 0.0
        ks = self.feet[side].keys
        for a, b in zip(ks, ks[1:]):
            if a[3] and not b[3]:
                d = i-(b[0]-1.5)
                if 0.0 <= d < 4.0:
                    out = max(out, run.SPARK*math.sin(math.pi*min(d/4.0, 1.0))**2)
        return out

    def post(self, p, caps, frames):
        n = self.n
        ro = run_offsets(p)
        pre = halo_points(p, run.pose, [self.entry-N*2+j for j in range(N*2)])
        start = [ro[(self.run_frame_a(i)-1) % N] for i in range(n)]
        end = [ro[(self.run_frame_b(i)-1) % N] for i in range(n)]
        offs = one_shot_offsets(caps, frames, pre, start, end)
        return apply_halo(p, caps, frames, offs)


def run_offsets(p):
    frames = list(range(1, N+2))
    return halo_offsets(halo_points(p, run.pose, frames), True, **run.HALO)[:N]


def pivot(k):
    """Run pivot 180: brake on L (f1 contact) and a quick R plant turned into the pivot, spin on it while L steps
    around behind, then drive off into the reversed run (R contact on the last frame)."""
    n = 20
    v0 = run.G['speed']

    def vel(i):
        slow = v0*(1.0-smooth(i/8.0))
        go = v0*smooth((i-9.0)/10.0)
        return Vector((0.0, -slow+go))            # the new facing after 180 is world +Y

    def yaw(i):
        return k*math.pi*smooth((i-3.0)/11.0)

    ra, rb = run_feet(1), run_feet(9)
    W = run.WIDTH
    r0, r1 = (run.FOOT_MID-W*1.1, -0.22), (run.FOOT_MID-W*0.9, 0.12)
    path = Path(n, vel, yaw, anchor=(4, 12, r0, r1))
    feet = {
        'L': Foot(path, [(0, contact('L', 1), 0.0, True),
                         (RUN_LIFT, None, 0.0, False),
                         (10, (run.FOOT_MID+W*1.15, -0.10 if k > 0 else -0.30), 0.0, True),
                         (14 if k > 0 else 13, None, 0.0, False),
                         (19, (rb['L'][0].x, rb['L'][0].y), 0.0, False)], lift=0.20),
        'R': Foot(path, [(0, (ra['R'][0].x, ra['R'][0].y), 0.0, False),
                         (4, r0, k*45.0, True),
                         (12, None, k*8.0, True),
                         (13, None, k*8.0, False),
                         (19, contact('R', 9), 0.0, True)], lift=0.18),
    }
    name = 'left' if k > 0 else 'right'
    return Turn(f'Run pivot 180 {name}', k, n, 180.0, path, feet,
                accent=dict(drop=0.09, sit=10.0, lean=8.0, brake0=0.0, brake1=15.0, body0=3.0, body1=15.0,
                            leg_in=2.0, leg_out=5.0))


def plant(k):
    """Plant turn 90: the outside foot (R for a left turn) plants wide and turned, the body cuts over it, the
    inside foot steps into the new line and the run carries on (R contact on the last frame)."""
    n = 16
    v0 = run.G['speed']

    def vel(i):
        a = k*math.pi/2*smooth((i-2.0)/10.0)
        sp = v0*(1.0-0.5*bump(i, 1.0, 13.0))
        return rot2(Vector((0.0, -sp)), a)

    def yaw(i):
        return k*math.pi/2*smooth((i-3.0)/10.0)

    path = Path(n, vel, yaw)
    ra, rb = run_feet(1), run_feet(9)
    W = run.WIDTH
    # L is planted on f1 (run f1), so R makes the plant for both turns: wide and outside for the left turn,
    # an open step at normal width for the right turn.
    rx = run.FOOT_MID-W*(1.25 if k > 0 else 1.0)
    feet = {
        'R': Foot(path, [(0, (ra['R'][0].x, ra['R'][0].y), 0.0, False),
                         (4, (rx, -0.30), k*30.0, True),
                         (6, None, k*15.0, True),
                         (7, None, k*15.0, False),
                         (15, contact('R', 9), 0.0, True)], lift=0.20),
        'L': Foot(path, [(0, contact('L', 1), 0.0, True),
                         (RUN_LIFT, None, 0.0, False),
                         (10, (run.FOOT_MID+W*(1.0 if k > 0 else 0.8), -0.30), 0.0, True),
                         (12.5 if k > 0 else 12, None, 0.0, False),
                         (15, (rb['L'][0].x, rb['L'][0].y), 0.0, False)], lift=0.16),
    }
    name = 'left' if k > 0 else 'right'
    return Turn(f'Plant turn 90 {name}', k, n, 90.0, path, feet,
                accent=dict(drop=0.05, sit=4.0, lean=14.0, brake0=1.0, brake1=13.0, body0=2.0, body1=12.0,
                            leg_in=5.0, leg_out=5.0))


def lean_props():
    return run.props()


def build(p):
    out = []
    frames = list(range(1, N+2))
    for k, name in ((1, 'left'), (-1, 'right')):
        out.append(bake(p, f'Run lean {name}', frames, make_lean(k), True,
                        markers={'L contact': 1, 'R contact': 1+N//2},
                        meta={'speed_mps': run.G['speed'], 'meters_per_cycle': round(run.G['speed']*N/FPS, 3),
                              'stance': run.G['stance'], 'kind': 'locomotion', 'direction': 'forward',
                              'lean': name, 'blend_with': 'Run forward'},
                        post=run.post, props=lean_props()))
    for t in (pivot(1), pivot(-1), plant(1), plant(-1)):
        frames = list(range(1, t.n+1))
        out.append(bake(p, t.title, frames, t.pose, False,
                        markers={'L contact': 1, 'R contact': t.n},
                        meta={'kind': 'locomotion', 'speed_mps': run.G['speed'], 'turn_deg': t.k*t.deg,
                              'turn_curve': [round(math.degrees(y), 2) for y in t.path.yaw],
                              'seam_from': ['Run forward', t.entry], 'seam_to': ['Run forward', t.exit],
                              'halo_clear_dir': [0.0, 0.0, 1.0],     # Run forward's halo nudge (up)
                              **({'accents': [5]} if t.deg == 90.0 else {})},   # plant turns: the L foot drives off into the cut
                        post=t.post, props=t.props()))
    return out
