"""Hand QA (item 9c): thumb/finger joint limits, wrist twist, finger pops, finger interpenetration.

Per frame (preview.py calls `frame()` after each frame_set on the baked action):
- flex: each joint's true bend toward the palm (- bends back): fingers about their true hinge
  (handpose.HandGeo.measure), thumb .2/.3 about bone X against the parent (HandGeo.bend), thumb.1 its twist
  about bone X relative to rest; off: the remaining swing (splay/abduction).
- wrist: hand twist about its bone axis relative to the forearm (candy-wrapper), hand bend, and the
  forearm's roll relative to the upper arm.
- pops: the popscan metric (deg a frame's local rotation leaves the slerp midpoint of its neighbours).
- interpenetration: digit meshes (rigid children of the finger bones) against each other and the thumb
  against the palm. Depth = how far a vertex sits inside the other part's closed surface; reported as the
  excess over the rest pose (some parts overlap slightly by construction).
"""
import math
import bpy
from mathutils import Quaternion
from mathutils.bvhtree import BVHTree

import hs_anim as H

DIGITS = ('thumb', 'index', 'middle', 'ring', 'little')
BASE_FLEX = (-3.0, 95.0)      # finger .1, true bend against the wrist -> knuckle line (handpose.HandGeo.bend)
ANAT_FLEX = {2: (-3.0, 110.0), 3: (-3.0, 95.0)}   # finger .2/.3 true bend against the parent segment
THUMB_FLEX = (0.0, 80.0)      # thumb.1, rest-relative: never bent back
THUMB_ANAT = (-3.0, 85.0)     # thumb .2/.3 true bend (the rig's thumb tip is modelled hooked 56 deg)
FINGER_OFF_MAX = 25.0         # .1 splay
THUMB_OFF_MAX = 40.0          # thumb.1 (includes the tuck)
HAND_TWIST_MAX = 30.0
HAND_BEND_MAX = 70.0
FOREARM_TWIST_MAX = 95.0
POP_MAX = 12.0                # deg/f^2 (2x the popscan midpoint metric) on finger/thumb bones
ACCENT_POP_MAX = 24.0         # on frames a clip declares as a deliberate snap (meta 'finger_accents')
PEN_MAX = 0.002               # m of interpenetration beyond the rest pose
PAIRS = [('thumb', 'palm'), ('thumb', 'index'), ('thumb', 'middle'), ('thumb', 'ring'), ('thumb', 'little'),
         ('index', 'middle'), ('index', 'ring'), ('index', 'little'), ('middle', 'ring'), ('middle', 'little'),
         ('ring', 'little')]


def twist_swing(q, axis):
    """Swing-twist split of a local rotation about bone axis 0 (X) or 1 (Y): (twist_deg, swing_deg)."""
    c = (q.x, q.y)[axis]
    tw = math.degrees(2*math.atan2(c, q.w))
    tw = (tw+180.0) % 360.0-180.0
    t = Quaternion((q.w, q.x, 0, 0) if axis == 0 else (q.w, 0, q.y, 0)).normalized()
    sw = (q @ t.inverted()).normalized()
    return tw, math.degrees(2*math.acos(min(1.0, abs(sw.w))))


def part_of(name):
    tail = name.split('|', 1)[1].strip()
    for d in DIGITS:
        if tail.startswith(d) or f' {d} ' in f' {tail} ':
            return 'palm' if 'knuckle' in tail else d
    return 'palm'


class HandQA:
    def __init__(self, poser):
        import handpass
        self.p = poser
        self.geo = handpass.HandPass(poser).geo
        self.bend = self.geo.bend
        self.parts = {s: {} for s in H.SIDES}
        for o in bpy.data.objects:
            if o.type != 'MESH' or o.parent_type != 'BONE':
                continue
            for s in H.SIDES:
                if o.name.startswith(f'{s} HAND |'):
                    me = o.data
                    self.parts[s].setdefault(part_of(o.name), []).append(
                        (o, [v.co.copy() for v in me.vertices], [tuple(pg.vertices) for pg in me.polygons]))
        self.joints = {s: [f'{s} {d}.{i}' for d in DIGITS for i in (1, 2, 3)] for s in H.SIDES}
        self.rest_pen = None
        self.reset()

    def reset(self):
        self.rows = []

    def _world(self, s, part):
        vs, ps = [], []
        for o, verts, polys in self.parts[s].get(part, []):
            mw = o.matrix_world
            base = len(vs)
            vs += [mw @ c for c in verts]
            ps += [tuple(base+i for i in q) for q in polys]
        return vs, ps

    def penetration(self):
        out = {}
        for s in H.SIDES:
            geo = {k: self._world(s, k) for k in self.parts[s]}
            trees = {k: BVHTree.FromPolygons(v, p) for k, (v, p) in geo.items() if p}
            for a, b in PAIRS:
                if a not in geo or b not in trees:
                    continue
                depth = 0.0
                for v in geo[a][0]:
                    hit = trees[b].find_nearest(v, 0.03)
                    if hit[0] is not None and (v-hit[0]).dot(hit[1]) < 0:
                        depth = max(depth, hit[3])
                out[f'{s} {a}/{b}'] = depth
        return out

    def calibrate_rest(self):
        """Rest-pose interpenetration baseline (call with the rig in rest pose)."""
        self.rest_pen = self.penetration()
        return {k: round(v*1000, 1) for k, v in self.rest_pen.items() if v > 0.0005}

    def frame(self, f):
        pb = self.p.pb
        row = {'f': f, 'q': {}, 'viol': [], 'flex': {}}
        for s in H.SIDES:
            true = {f'{s} {d}.{i}': b for d in DIGITS[1:]
                    for i, (b, _) in zip((1, 2, 3), self.geo.measure(pb, s, d))}
            for n in self.joints[s]:
                q = pb[n].rotation_quaternion.copy()
                row['q'][n] = q
                flex, off = twist_swing(q, 0)
                thumb = 'thumb' in n
                seg = int(n[-1])
                if thumb and seg == 1:
                    lo, hi = THUMB_FLEX
                elif thumb:
                    lo, hi = THUMB_ANAT
                    flex = self.bend(n, q)
                else:
                    lo, hi = BASE_FLEX if seg == 1 else ANAT_FLEX[seg]
                    flex = true[n]
                row['flex'][n] = flex
                if flex < lo:
                    row['viol'].append((n, 'bent back' if thumb else 'hyperextended', round(flex, 1)))
                elif flex > hi:
                    row['viol'].append((n, 'over-curled', round(flex, 1)))
                if seg == 1 and off > (THUMB_OFF_MAX if thumb else FINGER_OFF_MAX):
                    row['viol'].append((n, 'off-axis', round(off, 1)))
            tw, bend = twist_swing(pb[f'{s} hand'].rotation_quaternion, 1)
            ftw, _ = twist_swing(pb[f'{s} forearm'].rotation_quaternion, 1)
            row[f'{s} wrist'] = (tw, bend, ftw)
            if abs(tw) > HAND_TWIST_MAX:
                row['viol'].append((f'{s} hand', 'wrist twist', round(tw, 1)))
            if bend > HAND_BEND_MAX:
                row['viol'].append((f'{s} hand', 'wrist bend', round(bend, 1)))
            if abs(ftw) > FOREARM_TWIST_MAX:
                row['viol'].append((f'{s} forearm', 'forearm roll', round(ftw, 1)))
            row['q'][f'{s} hand'] = pb[f'{s} hand'].rotation_quaternion.copy()
        pen = self.penetration()
        row['pen'] = {k: v-(self.rest_pen or {}).get(k, 0.0) for k, v in pen.items()}
        self.rows.append(row)

    def summarize(self, loop, accents=()):
        """accents: frames declared as deliberate finger snaps (pops there may reach ACCENT_POP_MAX)."""
        rows = self.rows[:-1] if loop and len(self.rows) > 2 else self.rows
        n = len(rows)
        pops = []
        for name in rows[0]['q']:
            if 'hand' in name:
                continue
            for i in range(n):
                if not loop and (i == 0 or i == n-1):
                    continue
                a, b, c = rows[i-1]['q'][name], rows[i]['q'][name], rows[(i+1) % n]['q'][name]
                if a.dot(c) < 0:
                    c = -c
                d = a.slerp(c, 0.5).rotation_difference(b)
                pops.append((2*math.degrees(min(d.angle, 2*math.pi-d.angle)), name, rows[i]['f']))
        pops.sort(reverse=True)
        accents = {a+d for a in accents for d in (-1, 0, 1)}
        plain = [x for x in pops if x[2] not in accents]
        accent = [x for x in pops if x[2] in accents]
        viol = {}
        for r in rows:
            for n_, kind, v in r['viol']:
                key = f'{n_} {kind}'
                if key not in viol or abs(v) > abs(viol[key][0]):
                    viol[key] = (v, r['f'])
        pen = {}
        for r in rows:
            for k, v in r['pen'].items():
                if v > pen.get(k, (0.0, 0))[0]:
                    pen[k] = (v, r['f'])
        wrist = {}
        for s in H.SIDES:
            tw = max(rows, key=lambda r: abs(r[f'{s} wrist'][0]))
            ftw = max(rows, key=lambda r: abs(r[f'{s} wrist'][2]))
            wrist[s] = {'hand_twist_deg': round(tw[f'{s} wrist'][0], 1), 'hand_twist_frame': tw['f'],
                        'hand_bend_max_deg': round(max(r[f'{s} wrist'][1] for r in rows), 1),
                        'forearm_roll_deg': round(ftw[f'{s} wrist'][2], 1), 'forearm_roll_frame': ftw['f']}
        flex = {}
        for s in H.SIDES:
            th = [r['flex'][f'{s} thumb.{i}'] for r in rows for i in (1, 2, 3)]
            fi = [r['flex'][f'{s} {d}.{i}'] for r in rows for d in DIGITS[1:] for i in (1, 2, 3)]
            flex[s] = {'thumb_min': round(min(th), 1), 'thumb_max': round(max(th), 1),
                       'finger_min': round(min(fi), 1), 'finger_max': round(max(fi), 1)}
        bad_pen = {k: (round(v*1000, 1), f) for k, (v, f) in pen.items() if v > PEN_MAX}
        worst_pop = plain[0] if plain else (0.0, '', 0)
        worst_acc = accent[0] if accent else (0.0, '', 0)
        ok = not viol and not bad_pen and worst_pop[0] <= POP_MAX and worst_acc[0] <= ACCENT_POP_MAX
        return {'hand_qa': {'violations': {k: v for k, v in sorted(viol.items())},
                            'penetration_mm': bad_pen,
                            'max_penetration_mm': round(max((v for v, _ in pen.values()), default=0.0)*1000, 1),
                            'finger_pops_deg_f2': [(round(r, 2), nm, f) for r, nm, f in plain[:4]],
                            'accent_pops_deg_f2': [(round(r, 2), nm, f) for r, nm, f in accent[:2]],
                            'wrist': wrist, 'flex_range': flex},
                'hand_qa_summary': {'ok': ok, 'violations': len(viol), 'pen_over': len(bad_pen),
                                    'worst_pop_deg_f2': round(worst_pop[0], 2),
                                    'worst_pop': f'{worst_pop[1]} f{worst_pop[2]}',
                                    'worst_accent_pop_deg_f2': round(worst_acc[0], 2),
                                    'max_pen_mm': round(max((v for v, _ in pen.values()), default=0.0)*1000, 1)}}
