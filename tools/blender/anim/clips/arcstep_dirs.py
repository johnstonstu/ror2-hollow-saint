"""Arc Step back / left / right (item 9h): start, loop and end per direction; diagonals by blending neighbours.

Each direction is a channel transform of the forward Arc Step (arcstep.py) weighted by how far into the dash a
frame is (w = pelvis pitch / dash pitch; airborne legs by 1 - ground weight), so the rest frames (start f1,
end last frame) are untouched and every hand-off stays exact:
- back: the body leans back instead of pitching into an arrow, the knees tuck up ahead of the hips, the arms
  brace forward and the halo and tabard stream the other way (the concept's escape read);
- left / right: a roll into the travel spread over pelvis, spine and chest with the head levelling, the feet
  folded up behind and trailing sideways away from the travel, the arms tucked in low.
The loop is airborne, so the step can be taken from the air (start crossfades in from any air pose).
"""
from mathutils import Vector

from hs_anim import bake, lerp
import arcstep as A
import glide

PITCH = A.PITCH
BACK_LEAN = -0.45       # x the forward pelvis pitch
SIDE_ROLL = 36.0        # deg at full dash
SIDE_ARM = (20.0, 10.0, 75.0)   # arm swing / adduct / elbow at full dash
SIDE_ARM_LEAD = 0.5     # fraction of the dash pitch by which the arms are tucked
SIDE_TRAIL_IN = 0.06    # m the airborne ankles swing away from the travel: the leg on the travel side
SIDE_TRAIL_OUT = 0.20   # the other leg
LEFT_ANKLE_CLEARANCE = 0.06  # additional airborne splay clears the back tabard in the left bank
BACK_TUCK = (55.0, 95.0)    # air_leg thigh (deg forward of vertical) / shin (deg back of vertical)
SIDE_TUCK = (-30.0, 95.0)


def clamp(x):
    return min(max(x, 0.0), 1.0)


def transform(c, d):
    c = dict(c)
    w = clamp(c['pp']/PITCH)
    if d == 'back':
        c['pp'] = BACK_LEAN*c['pp']
        c['py'] = -c['py']
        c['sp'], c['ch'] = -0.5*c['sp'], -0.5*c['ch']
        c['nk'], c['hd'] = lerp(c['nk'], 0.25*abs(c['nk']), w), lerp(c['hd'], 0.2*abs(c['hd']), w)
        c['sw'] = lerp(c['sw'], -0.45*c['sw'], w)
        c['ad'] = c['ad']-8.0*w
        c['hy'], c['hr'] = -c['hy'], -0.6*c['hr']
        c['f1'], c['f2'], c['f3'], c['b1'], c['b2'] = (lerp(c['f1'], c['b1'], w), lerp(c['f2'], c['b2'], w),
                                                       lerp(c['f3'], 0.8*c['b2'], w), lerp(c['b1'], c['f1'], w),
                                                       lerp(c['b2'], c['f2'], w))
        for s in ('L', 'R'):
            # knees tucked up in front (thighs forward, shins folded back): straighter legs reach the ground
            a = 1.0-c[s+'gw']
            c[s+'th'] = lerp(c[s+'th'], BACK_TUCK[0], a*w)
            c[s+'sh'] = lerp(c[s+'sh'], BACK_TUCK[1], a*w)
            c[s+'pl'] = lerp(c[s+'pl'], 0.5*c[s+'pl'], a)
        return c
    k = 1.0 if d == 'left' else -1.0
    # (the pitch gives way to the roll late: upright early in the crouch, the shoulders sit over the knees)
    c['pp'] = lerp(c['pp'], 0.3*c['pp'], w*w)
    c['pr'] = c['pr']+k*SIDE_ROLL*0.55*w
    c['spr'] = c['spr']+k*SIDE_ROLL*0.25*w
    c['chr'] = c['chr']+k*SIDE_ROLL*0.2*w
    c['hdr'] = c['hdr']-k*SIDE_ROLL*0.7*w
    c['nk'], c['hd'] = lerp(c['nk'], 0.3*c['nk'], w), lerp(c['hd'], 0.3*c['hd'], w)
    # Arms tucked low and bent (streamlined), under handorient's raise gate: its world thumb-up target on a
    # rolled body needs a near-180 deg forearm roll whose solution flips branch through the start and end.
    # They tuck ahead of the pitch, before the raised forward-dash arms would meet the rolled body (tucking
    # later means unwinding the forward dash's ~100 deg orientation roll within a frame or two).
    wa = clamp(w/SIDE_ARM_LEAD)
    c['sw'], c['ad'], c['el'] = (lerp(c['sw'], SIDE_ARM[0], wa), lerp(c['ad'], SIDE_ARM[1], wa),
                                 lerp(c['el'], SIDE_ARM[2], wa))
    c['hy'] = 0.4*c['hy']
    for s in ('L', 'R'):
        # feet folded up behind; both ankles swing away from the travel, the outside leg further so the inside
        # foot doesn't close onto it
        a = 1.0-c[s+'gw']
        ks = 1.0 if s == 'L' else -1.0
        lead = ks == k
        c[s+'th'] = lerp(c[s+'th'], SIDE_TUCK[0], a*w)
        c[s+'sh'] = lerp(c[s+'sh'], SIDE_TUCK[1], a*w)
        c[s+'spl'] = c[s+'spl']-ks*k*(SIDE_TRAIL_IN if lead else SIDE_TRAIL_OUT)*a
        if d == 'left':
            c[s+'spl'] += LEFT_ANKLE_CLEARANCE*a*w
    return c


def apply(p, c, tt, d):
    A.apply(p, c, tt)


def start_channels(d):
    """Grounded load, compact arm tuck, then airborne launch; both seam poses stay exact."""
    start_keys = {f: transform(A.keyed(f, A.START_KEYS), d) for f in range(1, A.N_S+1)}
    if d in ('left', 'right'):
        # Keep the elbows compact and the hands above the rising knees through launch.
        # Recover the original flight pose before the seam; widening the arms instead
        # crosses the orientation solver's roll branch on this banked body.
        for f, c in start_keys.items():
            tuck = A.track(f, {1: 0.0, 3: 0.75, 4: 1.0, 5: 0.65, 7: 0.0})
            for channel, target in (('sw', 30.0), ('ad', 10.0), ('el', 115.0)):
                c[channel] = lerp(c[channel], target, tuck)
            if f in (3, 4):
                c['pz'] += 0.06
            if f == 4:
                # Release the planted targets on the launch frame. Holding them for
                # one extra frame reverses the thigh before the airborne knee tuck.
                c['Lgw'] = c['Rgw'] = 0.0
                if d == 'left':
                    w = clamp(A.keyed(f, A.START_KEYS)['pp']/PITCH)
                    for s in ('L', 'R'):
                        c[s+'spl'] += LEFT_ANKLE_CLEARANCE*w
    return start_keys


def make(d):
    start_keys = start_channels(d)
    end_keys = {f: transform(A.keyed(f, A.END_KEYS), d) for f in range(1, A.N_E+1)}

    def start_pose(p, f):
        apply(p, start_keys[f], (f-A.N_S)/A.N_L, d)

    def loop_pose(p, f):
        t = ((f-1) % A.N_L)/A.N_L
        apply(p, transform(A.dash(t), d), t, d)

    def end_pose(p, f):
        apply(p, end_keys[f], (f-1)/A.N_L, d)

    def loop_offsets(p):
        pts = glide.halo_points(p, loop_pose, list(range(1, A.N_L+1)))
        return [glide.clamp_len(q-q0) for q0, q in zip(pts, glide.spring_periodic(pts, *glide.HALO_SPRING))]

    def post_loop(p, caps, frames):
        offs = loop_offsets(p)
        return glide.apply_halo(p, caps, frames, offs+offs[:1])

    def post_start(p, caps, frames):
        zero = Vector((0, 0, 0))
        preroll = glide.halo_points(p, lambda q, f: None, [1]*8)
        offs = glide.one_shot_offsets(caps, frames, preroll, zero, loop_offsets(p)[0], fade=3)
        out = glide.apply_halo(p, caps, frames, offs)
        out['start_rest_err_m'] = A.rest_err(p, caps, frames[0])
        return out

    def post_end(p, caps, frames):
        preroll = glide.halo_points(p, loop_pose, list(range(1, A.N_L+1))*2)
        offs = glide.one_shot_offsets(caps, frames, preroll, loop_offsets(p)[0], Vector((0, 0, 0)), fade=4)
        out = glide.apply_halo(p, caps, frames, offs)
        out['end_rest_err_m'] = A.rest_err(p, caps, frames[-1])
        return out

    return start_pose, loop_pose, end_pose, post_start, post_loop, post_end


TRAVEL = {'back': (0.0, 1.0), 'left': (1.0, 0.0), 'right': (-1.0, 0.0)}


def build(p):
    A.solve_spread(p)
    A.solve_rest_poles(p)
    out = []
    for d in ('back', 'left', 'right'):
        sp, lp, ep, ps, pl, pe = make(d)
        name = f'Arc Step {d}'
        meta = {'kind': 'utility', 'direction': {'back': 'backward'}.get(d, d), 'travel': list(TRAVEL[d])}
        out += [
            bake(p, f'{name} start', list(range(1, A.N_S+1)), sp, False, markers={'Dash start': A.N_S},
                 meta={**meta, 'speed_mps': 0.0, 'seam_from': [f'{name} end', A.N_E], 'seam_to': [f'{name} loop', 1],
                       'finger_accents': [4], 'accents': [3, 4]}, post=ps),
            bake(p, f'{name} loop', list(range(1, A.N_L+2)), lp, True,
                 meta={**meta, 'dash_distance_m': [8, 12], 'air_ok': True}, post=pl),
            bake(p, f'{name} end', list(range(1, A.N_E+1)), ep, False,
                 markers={'Arrive': A.ARRIVE, 'Cancel': A.ARRIVE, 'Recovered': A.RECOVERED},
                 meta={**meta, 'speed_mps': 0.0, 'seam_from': [f'{name} loop', 1], 'seam_to': [f'{name} start', 1],
                       'accents': [4, 7], 'cancel': {'any_state_from': A.ARRIVE}}, post=pe),
        ]
    return out
