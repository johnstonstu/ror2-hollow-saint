"""Natural-hand QA (item 12), part of fullqa: FullQA.frame records it, FullQA.summarize reports and fails it.

- bends: every finger joint's true bend (handpose.HandGeo.measure, about the finger's hinge) and thumb .2/.3
  (HandGeo.bend) stay inside NAT_RANGE: nothing bends backward past BACK_MIN.
- planar: the middle/tip joints turn about the finger's own hinge, not sideways (SIDE_MAX): no hooks/talons.
- graduated: each digit's total curl is at least the previous digit's minus MONO_TOL (index -> little), except
  on frames a clip declares in meta 'curl_exempt' (deliberate single-finger flicks).
- contact: finger parts (digit meshes of '<s> HAND |', not the palm/knuckle plates) no deeper than CONTACT_MAX
  beyond rest into the body or any closed part (fullqa's contact probes); finger/finger and thumb/palm
  interpenetration (handqa.HandQA's rows) no deeper than CONTACT_MAX beyond rest.
- pops: finger/thumb local angular acceleration (fullqa's metric) under POP_MAX, ACCENT_POP_MAX on meta
  'finger_accents' frames +-1.
- orientation (9f) is scored by handorient.OrientQA; preview.py copies its verdict into the block.
"""
import math

import hs_anim as H

DIGITS = ('index', 'middle', 'ring', 'little')
BACK_MIN = -1.0
NAT_RANGE = {1: (BACK_MIN, 95.0), 2: (BACK_MIN, 110.0), 3: (BACK_MIN, 95.0), 'thumb': (BACK_MIN, 85.0)}
SIDE_MAX = 12.0
MONO_TOL = 10.0
CONTACT_MAX = 0.002
POP_MAX = 12.0
ACCENT_POP_MAX = 24.0
FINGER_KEYS = ('index', 'middle', 'ring', 'little', 'thumb')


def is_finger(name):
    return name.startswith(('L HAND |', 'R HAND |')) and any(k in name.split('|', 1)[1] for k in FINGER_KEYS) \
        and 'knuckle' not in name


class HandNat:
    def __init__(self, poser):
        import handpose
        self.p = poser
        self.geo = handpose.HandGeo(poser)
        self.bones = [f'{s} {d}.{i}' for s in H.SIDES for d in DIGITS+('thumb',) for i in (1, 2, 3)]

    def frame(self, finger_pen, hand_pen=None):
        pb = self.p.pb
        bends, side, total = {}, {}, {}
        for s in H.SIDES:
            for d in DIGITS:
                m = self.geo.measure(pb, s, d)
                for i, (b, sd) in zip((1, 2, 3), m):
                    bends[f'{s} {d}.{i}'] = b
                    if i > 1:
                        side[f'{s} {d}.{i}'] = sd
                total[f'{s} {d}'] = sum(b for b, _ in m)
            for i in (2, 3):
                n = f'{s} thumb.{i}'
                bends[n] = self.geo.bend(n, pb[n].rotation_quaternion)
        return {'bends': bends, 'side': side, 'total': total, 'finger_pen': dict(finger_pen),
                'hand_pen': dict(hand_pen or {}),
                'q': {n: pb[n].rotation_quaternion.copy() for n in self.bones}}


def summarize(rows, loop, accents=(), curl_exempt=()):
    """rows: FullQA rows (each with 'hand'). Returns (block, fails)."""
    rows = [r for r in rows if r.get('hand')]
    if not rows:
        return {}, []
    fails = []
    lo_b, hi_b, side_m, mono, fpen, hpen = {}, {}, {}, {}, {}, {}
    exempt = {a+d for a in curl_exempt for d in (-1, 0, 1)}
    for r in rows:
        h, f = r['hand'], r['f']
        for n, b in h['bends'].items():
            if b < lo_b.get(n, (1e9, 0))[0]:
                lo_b[n] = (b, f)
            if b > hi_b.get(n, (-1e9, 0))[0]:
                hi_b[n] = (b, f)
        for n, v in h['side'].items():
            if abs(v) > abs(side_m.get(n, (0.0, 0))[0]):
                side_m[n] = (v, f)
        if f not in exempt:
            for s in H.SIDES:
                for a, b in zip(DIGITS, DIGITS[1:]):
                    gap = h['total'][f'{s} {a}']-h['total'][f'{s} {b}']
                    k = f'{s} {a}>{b}'
                    if gap > mono.get(k, (-1e9, 0))[0]:
                        mono[k] = (gap, f)
        for src, dst in ((h['finger_pen'], fpen), (h['hand_pen'], hpen)):
            for k, v in src.items():
                if v > dst.get(k, (0.0, 0))[0]:
                    dst[k] = (v, f)
    for n, (b, f) in lo_b.items():
        lo = NAT_RANGE['thumb' if 'thumb' in n else int(n[-1])][0]
        if b < lo:
            fails.append(f'hand-nat {n} bent back {b:.1f} f{f}')
    for n, (b, f) in hi_b.items():
        hi = NAT_RANGE['thumb' if 'thumb' in n else int(n[-1])][1]
        if b > hi:
            fails.append(f'hand-nat {n} over-curled {b:.1f} f{f}')
    for n, (v, f) in side_m.items():
        if abs(v) > SIDE_MAX:
            fails.append(f'hand-nat {n} sideways {v:.1f} f{f}')
    for k, (gap, f) in mono.items():
        if gap > MONO_TOL:
            fails.append(f'hand-nat curl order {k} {gap:.1f} f{f}')
    for k, (v, f) in fpen.items():
        if v > CONTACT_MAX:
            fails.append(f'hand-nat finger contact {k} {v*1000:.1f}mm f{f}')
    for k, (v, f) in hpen.items():
        if v > CONTACT_MAX:
            fails.append(f'hand-nat finger pen {k} {v*1000:.1f}mm f{f}')
    acc = {a+d for a in accents for d in (-1, 0, 1)}
    seq = rows+([rows[0], rows[1]] if loop and len(rows) > 2 else [])
    pops = []
    for n in rows[0]['hand']['q']:
        for i in range(1, len(seq)-1):
            a, b, c = (seq[j]['hand']['q'][n] for j in (i-1, i, i+1))
            if a.dot(c) < 0:
                c = -c
            d = a.slerp(c, 0.5).rotation_difference(b)
            pops.append((2*math.degrees(min(d.angle, 2*math.pi-d.angle)), n, seq[i]['f']))
    pops.sort(reverse=True)
    plain = [x for x in pops if x[2] not in acc]
    accent = [x for x in pops if x[2] in acc]
    if plain and plain[0][0] > POP_MAX:
        fails.append(f'hand-nat finger pop {plain[0][1]} {plain[0][0]:.1f}deg/f2 f{plain[0][2]}')
    if accent and accent[0][0] > ACCENT_POP_MAX:
        fails.append(f'hand-nat finger accent pop {accent[0][1]} {accent[0][0]:.1f}deg/f2 f{accent[0][2]}')
    worst = lambda d: max(d.items(), key=lambda kv: abs(kv[1][0]), default=(None, (0.0, 0)))
    wb, ws, wm = min(lo_b.items(), key=lambda kv: kv[1][0]), worst(side_m), worst(mono)
    block = {'min_bend_deg': [round(wb[1][0], 1), wb[0], wb[1][1]],
             'max_bend_deg': max((round(v, 1), n, f) for n, (v, f) in hi_b.items()),
             'max_sideways_deg': [round(ws[1][0], 1), ws[0], ws[1][1]],
             'curl_order_gap_deg': [round(wm[1][0], 1), wm[0], wm[1][1]],
             'finger_contact_mm': {k: [round(v*1000, 1), f] for k, (v, f) in fpen.items() if v > 0.0005},
             'finger_pen_mm': {k: [round(v*1000, 1), f] for k, (v, f) in hpen.items() if v > 0.0005},
             'pops_deg_f2': [(round(r, 1), n, f) for r, n, f in plain[:3]],
             'accent_pops_deg_f2': [(round(r, 1), n, f) for r, n, f in accent[:2]],
             'fails': fails}
    return block, fails
