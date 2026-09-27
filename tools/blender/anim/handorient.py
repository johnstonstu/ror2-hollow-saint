"""Hand orientation on extended arms (item 9f): thumb up, palm forward, fingers curl in, mirrored L/R.

Axes are rigid in the hand bone's frame (taken from the rest pose, so finger and thumb animation never
moves them), evaluated in world space per frame (character faces -Y; forward = the chest's facing, flat):
- d: hand bone +Y (wrist -> knuckles)
- n: palm normal = the side the fingers curl toward (rest mean +Z of the four finger base bones; a +X
  rotation of a finger bone swings its tip toward +Z)
- r: thumb side = rest (thumb.1 tail - middle.1 head), perpendicular to d
- handedness = (d x n) . r: a real left hand is -1, a right hand +1 (the 9f rig check, at rest)
- thumb_up = r.z, palm_fwd = n . forward
Gate (the 9f rule is for arms extended outward or forward): the upper arm raised more than GATE_RAISE
from the torso's down axis, or the elbow opened past GATE_ELBOW with the arm raised more than
GATE_RAISE_ELBOW, unless the shoulder -> wrist vector points mainly backward (a swept-back arm, as in the
Arc Step dash, naturally turns the thumb down; reported as swept_back, not gated).
Fails: thumb_up < THUMB_UP_MIN or palm_fwd < PALM_FWD_MIN on a gated frame. An arm pointing straight
forward (Arc Bolt fingertip) has its palm facing inward, palm_fwd ~ 0, which passes.
"""
import math
from mathutils import Vector

import hs_anim as H

DIGITS = ('index', 'middle', 'ring', 'little')
GATE_RAISE, GATE_ELBOW, GATE_RAISE_ELBOW = 45.0, 140.0, 30.0
THUMB_UP_MIN = 0.0
PALM_FWD_MIN = -0.25


def rest_axes(rig):
    """{side: (n_local, r_local)} in the hand bone's rest frame."""
    b = rig.data.bones
    out = {}
    for s in H.SIDES:
        h3 = b[f'{s} hand'].matrix_local.to_3x3()
        d = h3.col[1].normalized()
        n = sum((b[f'{s} {dg}.1'].matrix_local.col[2].xyz for dg in DIGITS), Vector())
        n = (n-d*n.dot(d)).normalized()
        t = b[f'{s} thumb.1'].tail_local-b[f'{s} middle.1'].head_local
        r = (t-d*t.dot(d)).normalized()
        out[s] = (h3.inverted() @ n, h3.inverted() @ r)
    return out


_AXES = {}


def hand_axes(rig, side):
    key = rig.name_full+str(rig.get('hs_hand_fix', ''))
    if key not in _AXES:
        _AXES[key] = rest_axes(rig)
    nl, rl = _AXES[key][side]
    m3 = rig.matrix_world.to_3x3() @ rig.pose.bones[f'{side} hand'].matrix.to_3x3()
    d = m3.col[1].normalized()
    n = m3 @ nl
    r = m3 @ rl
    n = (n-d*n.dot(d)).normalized()
    r = (r-d*r.dot(d)).normalized()
    return d, n, r


def handedness(rig, side):
    d, n, r = hand_axes(rig, side)
    return d.cross(n).dot(r)


def rest_handedness_from_bones(rig, side):
    """Handedness straight from the rest bone layout (independent of the cached axes)."""
    b = rig.data.bones
    d = b[f'{side} hand'].matrix_local.col[1].xyz.normalized()
    n = sum((b[f'{side} {dg}.1'].matrix_local.col[2].xyz for dg in DIGITS), Vector())
    n = (n-d*n.dot(d)).normalized()
    t = b[f'{side} thumb.1'].tail_local-b[f'{side} middle.1'].head_local
    r = (t-d*t.dot(d)).normalized()
    return d.cross(n).dot(r)


def chest_delta(rig, rest_chest):
    return rig.matrix_world.to_3x3() @ rig.pose.bones['chest'].matrix.to_3x3() @ rest_chest.inverted()


def forward(rig, rest_chest):
    f = chest_delta(rig, rest_chest) @ Vector((0, -1, 0))
    f.z = 0
    return f.normalized() if f.length > 1e-6 else Vector((0, -1, 0))


def arm_state(rig, side, rest_chest):
    pb = rig.pose.bones
    mw = rig.matrix_world
    m3 = mw.to_3x3()
    chest = chest_delta(rig, rest_chest)
    down = chest @ Vector((0, 0, -1))
    fwd = chest @ Vector((0, -1, 0))
    out = chest @ Vector((H.sign(side), 0, 0))
    ua = (m3 @ pb[f'{side} upperarm'].matrix.col[1].xyz).normalized()
    fa = (m3 @ pb[f'{side} forearm'].matrix.col[1].xyz).normalized()
    raise_deg = math.degrees(ua.angle(down))
    elbow_open = 180.0-math.degrees(ua.angle(fa))
    a = mw @ pb[f'{side} hand'].head-mw @ pb[f'{side} upperarm'].head
    back = -a.dot(fwd)
    back_margin = (back-max(a.dot(fwd), a.dot(out), 0.0))/max(a.length, 1e-6)
    swept_back = back_margin > 0 and raise_deg > GATE_RAISE_ELBOW
    raised = raise_deg > GATE_RAISE or (elbow_open > GATE_ELBOW and raise_deg > GATE_RAISE_ELBOW)
    return {'raise': raise_deg, 'elbow_open': elbow_open, 'gated': raised and not swept_back,
            'swept_back': raised and swept_back, 'back_margin': back_margin, 'fa': fa, 'ua': ua}


class OrientQA:
    def __init__(self, rig):
        self.rig = rig
        self.rest_chest = rig.data.bones['chest'].matrix_local.to_3x3()
        self.reset()

    def reset(self):
        self.rows = []

    def calibrate_rest(self):
        """Handedness from the rest bone layout: L must be -1-ish and R +1-ish for a correctly handed rig."""
        return {s: round(rest_handedness_from_bones(self.rig, s), 3) for s in H.SIDES}

    def frame(self, f):
        fwd = forward(self.rig, self.rest_chest)
        row = {'f': f}
        for s in H.SIDES:
            d, n, r = hand_axes(self.rig, s)
            st = arm_state(self.rig, s, self.rest_chest)
            row[s] = {'thumb_up': r.z, 'palm_fwd': n.dot(fwd), 'hand': d.cross(n).dot(r),
                      **{k: v for k, v in st.items() if k in ('raise', 'elbow_open', 'gated', 'swept_back')}}
        self.rows.append(row)

    def summarize(self, loop):
        rows = self.rows[:-1] if loop and len(self.rows) > 2 else self.rows
        per = {}
        ok = True
        for s in H.SIDES:
            gated = [r for r in rows if r[s]['gated']]
            bad = [r['f'] for r in gated if r[s]['thumb_up'] < THUMB_UP_MIN or r[s]['palm_fwd'] < PALM_FWD_MIN]
            wrong_hand = [r['f'] for r in rows if r[s]['hand']*(1 if s == 'R' else -1) < 0]
            swept = [r['f'] for r in rows if r[s]['swept_back']]
            e = {'gated_frames': len(gated), 'fail_frames': ranges(bad), 'wrong_handed_frames': ranges(wrong_hand),
                 'swept_back_frames': ranges(swept)}
            if gated:
                wt = min(gated, key=lambda r: r[s]['thumb_up'])
                wp = min(gated, key=lambda r: r[s]['palm_fwd'])
                e.update({'worst_thumb_up': round(wt[s]['thumb_up'], 2), 'worst_thumb_up_f': wt['f'],
                          'worst_palm_fwd': round(wp[s]['palm_fwd'], 2), 'worst_palm_fwd_f': wp['f'],
                          'thumb_up_at_worst_palm': round(wp[s]['thumb_up'], 2)})
            ok &= not bad and not wrong_hand
            per[s] = e
        return {'hand_orient': per, 'hand_orient_ok': ok}


def ranges(frames):
    out, start, prev = [], None, None
    for f in frames:
        if start is None:
            start = prev = f
        elif f == prev+1:
            prev = f
        else:
            out.append(f'{start}-{prev}' if prev != start else f'{start}')
            start = prev = f
    if start is not None:
        out.append(f'{start}-{prev}' if prev != start else f'{start}')
    return ','.join(out)
