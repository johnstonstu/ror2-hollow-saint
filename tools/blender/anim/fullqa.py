"""Full-body QA (item 9i, FULL-AUDIT permanent checks 1-7), per frame in preview.py.

- contact: every rigid/weighted part group (armour, halo arcs, yoke, scapula shells, tabard panels, chest core,
  plates, hands...) and every limb's skin, tested against the whole body (limbs: minus their own limb) and
  against the closed part groups. A point is inside when the nearest face faces away from it and 4 of 5 rays
  agree (ray parity); depth = distance to that face. Each point's rest depth is subtracted (parts that sit
  in the skin by construction), and anything deeper than CONTACT_MAX fails. Pairs on one rigid driver never
  meet, so they are skipped. The tabard Shrinkwrap is switched off first: it is Blender-only and hid M1.
- rigid: hard-surface parts that are vertex-weighted (core, rim, plates, abdomen plates, sigil ring) keep
  their edge lengths within RIGID_MAX of rest.
- joints: knee/elbow hyperextension, knee hinge off the shin's local X, roll flips > 90 deg/f, hand-vs-forearm
  twist, forearm-vs-upper-arm twist (with forearm twist bones: the larger of the elbow-seam and along-forearm
  skin twist).
- pops: local angular acceleration (2x the popscan slerp-midpoint metric) on every deforming non-finger bone,
  POP_MAX outside the frames a clip declares in meta 'accents' (+-1 frame).
- feet: soles no more than 1 cm below ground; a planted knee bent > 15 deg points within 45 deg of the foot.
- symmetry (rig level, once): L/R rest bones mirrored about MID_X.
- natural hands (item 12, handnat.py): finger bends/planarity/curl order, finger-part contact 2 mm, finger
  interpenetration (preview.py passes handqa's row), finger pops.
"""
import math
from collections import defaultdict

import bpy
import numpy as np
from mathutils import Matrix, Quaternion, Vector
from mathutils.bvhtree import BVHTree

import hs_anim as H
import handnat
from handqa import twist_swing

CONTACT_MAX = 0.005
RIGID_MAX = 0.05
POP_MAX = 20.0
ACCENT_POP_MAX = 60.0
HINGE_OFF_MAX = 30.0
HAND_TWIST_MAX = 70.0
FOREARM_TWIST_MAX = 60.0
ROLL_FLIP_MAX = 90.0
SOLE_MIN = H.GROUND_Z-0.010
KNEE_DIR_MAX = 45.0
SYM_POS, SYM_ANG = 0.005, 3.0
STEP = 2
DIRS = [Vector(d).normalized() for d in ((1, 0.2, 0.1), (-0.3, 1, 0.2), (0.1, -0.2, 1), (-1, -0.4, -0.3),
                                         (0.2, 0.3, -1))]
LIMBS = {'L arm': ('L upperarm', 'L forearm', 'L hand'), 'R arm': ('R upperarm', 'R forearm', 'R hand'),
         'L leg': ('L thigh', 'L shin', 'L foot'), 'R leg': ('R thigh', 'R shin', 'R foot')}
RIGID_GROUPS = ('chest core', 'chest plate L', 'chest plate R', 'rib plate L', 'rib plate R', 'abdomen plates',
                'back node')
RIGID_NAMES = ('sigil ring',)
MIN_EDGE = 0.004
# Joint creases: a limb's first segment folding into the region it hangs from (thigh into pelvis/spine at the
# hip, upper arm into chest at the shoulder) is the skin's own fold, hidden inside the body; reported, not failed.
CREASE = {(f'{s} {a}', b) for s in H.SIDES for a, b in
          (('thigh', 'pelvis'), ('thigh', 'spine'), ('upperarm', 'chest'))}
BURIED = 0.004     # points already this deep at rest are hidden by construction (conductors, mask): skipped


def region_of(g):
    if not g:
        return 'misc'
    for s in H.SIDES:
        if g in (f'{s} upperarm', f'{s} shoulder', f'{s} pauldron') or g.startswith(f'{s} upperarm'):
            return f'{s} upperarm'
        if g.startswith(f'{s} forearm'):
            return f'{s} forearm'
        if g.startswith(s+' ') and any(k in g for k in ('hand', 'index', 'middle', 'ring', 'little', 'thumb')):
            return f'{s} hand'
        if g.startswith(f'{s} thigh') or g == f'{s} hip':
            return f'{s} thigh'
        if g == f'{s} shin':
            return f'{s} shin'
        if g in (f'{s} foot', f'{s} toe'):
            return f'{s} foot'
    if g.startswith('tabard'):
        return 'pelvis'
    if g.startswith('neck'):
        return 'neck'
    return g if g in ('head', 'chest', 'spine', 'pelvis') else 'misc'


def group_of(o):
    n = o.name
    if n.startswith(('VFX', 'Warm', 'V11 bust', 'HF BODY')) or o.type != 'MESH' or o.hide_render:
        return None
    if n.startswith(('L HAND |', 'R HAND |')):
        return f'{n[0]} hand parts'
    if n.startswith('TABARD |'):
        return 'tabard back' if ' back ' in n else 'tabard front'
    if n.startswith('HALO'):
        if o.parent_type == 'BONE' and o.parent_bone.startswith('halo') and o.parent_bone != 'halo root':
            return 'halo arc '+o.parent_bone.split()[-1]
        return 'yoke'
    if n.startswith(('L SHOULDER', 'R SHOULDER')):
        return f'{n[0]} pauldron'
    if n.startswith(('L BACK', 'R BACK')):
        return f'{n[0]} scapula shell'
    if n.startswith('CHEST |'):
        if 'rib plate' in n:
            return 'rib plate '+n[-1]
        if 'upper chest plate' in n:
            return 'chest plate '+n[-1]
        return 'chest core'
    if n.startswith('ABDOMEN'):
        return 'abdomen plates'
    if n.startswith('BACK |'):
        return 'back node' if 'node' in n else 'back conductors'
    if n.startswith('MASK'):
        return 'mask'
    if n.startswith('NECK'):
        return 'neck cables'
    if n.startswith(('L ARM', 'R ARM')):
        return f'{n[0]} forearm conductor'
    return 'other'


def dominant(obj, deform):
    names = {g.index: g.name for g in obj.vertex_groups}
    out = []
    for v in obj.data.vertices:
        best, bw = '', 0.0
        for g in v.groups:
            n = names.get(g.group)
            if n in deform and g.weight > bw:
                best, bw = n, g.weight
        out.append(best)
    return out


def ev_mesh(o, dg):
    ev = o.evaluated_get(dg)
    me = ev.to_mesh()
    n = len(me.vertices)
    co = np.empty(n*3)
    me.vertices.foreach_get('co', co)
    co = co.reshape(-1, 3)
    mw = np.array(o.matrix_world)
    co = co@mw[:3, :3].T+mw[:3, 3]
    polys = [tuple(p.vertices) for p in me.polygons]
    ne = len(me.edges)
    ed = np.empty(ne*2, dtype=np.int64)
    me.edges.foreach_get('vertices', ed)
    ev.to_mesh_clear()
    return co, polys, ed.reshape(-1, 2)


def is_closed(polys):
    cnt = defaultdict(int)
    for p in polys:
        for i in range(len(p)):
            a, b = p[i], p[(i+1) % len(p)]
            cnt[(a, b) if a < b else (b, a)] += 1
    return bool(cnt) and all(c == 2 for c in cnt.values())


def inside(tree, q):
    votes = 0
    for k, d in enumerate(DIRS):
        n, o = 0, q
        for _ in range(24):
            hit = tree.ray_cast(o, d)
            if hit[0] is None:
                break
            n += 1
            o = hit[0]+d*1e-5
        votes += n % 2
        if votes >= 4 or votes+(len(DIRS)-1-k) < 4:
            break
    return votes >= 4


def depth(tree, q, maxd=0.08):
    loc, nrm, idx, dist = tree.find_nearest(q, maxd)
    if loc is None or dist < 0.0005 or (q-loc).dot(nrm) > 0 and dist > 0.004:
        return 0.0, idx
    return (dist, idx) if inside(tree, q) else (0.0, idx)


def driver(objs):
    out = set()
    for o in objs:
        if o.parent_type == 'BONE':
            out.add(o.parent_bone)
        elif any(m.type == 'ARMATURE' for m in o.modifiers):
            return None
        else:
            out.add(o.parent.name if o.parent else o.name)
    return tuple(sorted(out))


def disable_shrinkwrap():
    n = 0
    for o in bpy.data.objects:
        for m in getattr(o, 'modifiers', []):
            if m.type == 'SHRINKWRAP' and (m.show_viewport or m.show_render):
                m.show_viewport = m.show_render = False
                n += 1
    return n


class FullQA:
    def __init__(self, poser):
        self.p = poser
        self.rig = poser.rig
        self.shrinkwrap_disabled = disable_shrinkwrap()
        deform = {b.name for b in self.rig.data.bones if b.use_deform}
        self.body = bpy.data.objects[H.BODY]
        self.breg = [region_of(g) for g in dominant(self.body, deform)]
        self.bpolys = [tuple(p.vertices) for p in self.body.data.polygons]
        self.poly_reg = [self.breg[q[0]] for q in self.bpolys]
        regs = sorted(set(self.breg)-{'misc'})
        self.reg_idx = {r: np.array([i for i, x in enumerate(self.breg) if x == r]) for r in regs}
        self.groups = defaultdict(list)
        for o in bpy.data.objects:
            g = group_of(o)
            if g:
                self.groups[g].append(o)
        self.drivers = {g: driver(v) for g, v in self.groups.items()}
        self.rigid = {o.name for g, v in self.groups.items() for o in v
                      if any(m.type == 'ARMATURE' for m in o.modifiers)
                      and (g in RIGID_GROUPS or any(k in o.name.lower() for k in RIGID_NAMES))}
        self.closed = {}
        self.bones = [b.name for b in self.rig.data.bones if b.use_deform and not any(
            k in b.name for k in ('index', 'middle', 'ring', 'little', 'thumb'))]
        self.rest = None
        self.hn = handnat.HandNat(poser)
        self.finger_mask = {}
        self.reset()

    def reset(self):
        self.rows = []

    # geometry ---------------------------------------------------------------------------------------------
    def _geo(self):
        dg = bpy.context.evaluated_depsgraph_get()
        bco, _, bed = ev_mesh(self.body, dg)
        if len(bco) != len(self.breg):
            raise RuntimeError('body topology changed under evaluation')
        groups = {}
        for g, objs in self.groups.items():
            cos, polys, per, base, closed = [], [], [], 0, True
            for o in objs:
                co, p, e = ev_mesh(o, dg)
                if not p:
                    continue
                if o.name not in self.closed:
                    self.closed[o.name] = is_closed(p)
                closed &= self.closed[o.name]
                cos.append(co)
                polys += [tuple(base+i for i in q) for q in p]
                per.append((o.name, base, e))
                base += len(co)
            if polys:
                groups[g] = {'co': np.vstack(cos), 'polys': polys, 'closed': closed, 'per': per}
                if g.endswith('hand parts') and g not in self.finger_mask:
                    m = np.zeros(base, dtype=bool)
                    for (name, b0, _), co in zip(per, cos):
                        m[b0:b0+len(co)] = handnat.is_finger(name)
                    self.finger_mask[g] = m[::STEP]
        return bco, bed, groups

    def _contact(self, bco, groups):
        """{probe label: depth array} for every probe set (limb skin, part groups), plus obstacle labels."""
        bv = [Vector(v) for v in bco]
        full = BVHTree.FromPolygons(bv, self.bpolys)
        out = {}
        self._points = {}
        for limb, regs in LIMBS.items():
            ids = np.concatenate([self.reg_idx[r] for r in regs if r in self.reg_idx])[::STEP]
            keep = [i for i, q in enumerate(self.bpolys) if self.poly_reg[i] not in regs]
            tree = BVHTree.FromPolygons(bv, [self.bpolys[i] for i in keep])
            res = []
            for i in ids:
                d, idx = depth(tree, bv[i])
                res.append((d, self.poly_reg[keep[idx]] if d else None, self.breg[i]))
            out['body '+limb] = res
            self._points['body '+limb] = bco[ids]
        for g, d in groups.items():
            res = []
            for q in d['co'][::STEP]:
                dd, idx = depth(full, Vector(q))
                res.append((dd, self.poly_reg[idx] if dd else None, g))
            out[g] = res
            self._points[g] = d['co'][::STEP]
        # limb skin and parts inside closed rigid parts of another driver
        trees = {g: BVHTree.FromPolygons([Vector(v) for v in d['co']], d['polys'])
                 for g, d in groups.items() if d['closed']}
        box = {g: (groups[g]['co'].min(0)-0.01, groups[g]['co'].max(0)+0.01) for g in trees}
        probes = [(f'body {limb}', bco[np.concatenate([self.reg_idx[r] for r in regs if r in self.reg_idx])[::STEP]])
                  for limb, regs in LIMBS.items()]
        probes += [(g, d['co'][::STEP]) for g, d in groups.items()]
        for label, pts in probes:
            for g, tree in trees.items():
                if g == label or (label in self.drivers and self.drivers[label] is not None
                                  and self.drivers[label] == self.drivers[g]):
                    continue
                lo, hi = box[g]
                m = np.all((pts >= lo) & (pts <= hi), axis=1)
                res = np.zeros(len(pts))
                for i in np.nonzero(m)[0]:
                    res[i] = depth(tree, Vector(pts[i]), 0.05)[0]
                out[f'{label} @ {g}'] = [(v, g if v else None, label) for v in res]
                self._points[f'{label} @ {g}'] = pts
        return out

    def _rigid(self, groups):
        out = {}
        for g, d in groups.items():
            for name, base, e in d['per']:
                if name not in self.rigid or not len(e):
                    continue
                co = d['co']
                L = np.linalg.norm(co[base+e[:, 0]]-co[base+e[:, 1]], axis=1)
                out[name] = L
        return out

    def calibrate_rest(self):
        """Call with the rig in its rest pose (after poser.reset())."""
        bco, bed, groups = self._geo()
        c = self._contact(bco, groups)
        self.rest = {'contact': {k: np.array([v[0] for v in r]) for k, r in c.items()},
                     'rigid': self._rigid(groups)}
        worst = {}
        for k, r in c.items():
            for d, obs, src in r:
                if d > 0.002:
                    key = f'{src} > {obs}' if not k.count('@') else f'{src} > {obs}'
                    worst[key] = max(worst.get(key, 0.0), d)
        self.rest_overlaps = {k: round(v*1000, 1) for k, v in sorted(worst.items(), key=lambda x: -x[1])}
        return {'rest_overlaps_mm': dict(list(self.rest_overlaps.items())[:40]),
                'shrinkwrap_disabled': self.shrinkwrap_disabled, 'rigid_parts': sorted(self.rigid)}

    # per frame ---------------------------------------------------------------------------------------------
    def frame(self, f, hand_pen=None):
        """hand_pen: handqa.HandQA's finger interpenetration for this frame (row['pen'])."""
        bco, bed, groups = self._geo()
        c = self._contact(bco, groups)
        pen, where, fpen = {}, {}, {}
        for k, r in c.items():
            base = self.rest['contact'].get(k)
            pts = self._points.get(k)
            fm = self.finger_mask.get(k.split(' @ ')[0])
            for i, (d, obs, src) in enumerate(r):
                if not d:
                    continue
                b = base[i] if base is not None and i < len(base) else 0.0
                if b > BURIED:
                    continue
                ex = d-b
                if fm is not None and i < len(fm) and fm[i] and ex > fpen.get(f'{src} > {obs}', 0.0):
                    fpen[f'{src} > {obs}'] = ex
                if ex > 0.001:
                    key = f'{src} > {obs}'
                    if ex > pen.get(key, 0.0):
                        pen[key] = ex
                        if pts is not None:
                            where[key] = [round(float(x), 3) for x in pts[i]]
        rig = {}
        for name, L in self._rigid(groups).items():
            R0 = self.rest['rigid'].get(name)
            if R0 is None or len(R0) != len(L):
                continue
            ok = R0 >= MIN_EDGE
            if ok.any():
                rig[name] = float(np.abs(L[ok]/R0[ok]-1).max())
        pb = self.p.pb
        q = {n: pb[n].rotation_quaternion.copy() for n in self.bones}
        j = self._joints()
        ft = self._feet(bco)
        self.rows.append({'f': f, 'pen': pen, 'where': where, 'rigid': rig, 'q': q, 'joints': j, 'feet': ft,
                          'hand': self.hn.frame(fpen, hand_pen)})

    def _joints(self):
        pb = self.p.pb
        out = {}
        rel = []
        for n in ('chest', 'head'):
            b = pb[n]
            rel.append(b.matrix.to_3x3() @ b.bone.matrix_local.to_3x3().inverted())
        e = (rel[0].inverted() @ rel[1]).to_euler()
        out['head pitch back'] = -math.degrees(e.x)
        out['head yaw'] = math.degrees(e.z)
        for s in H.SIDES:
            # flexion signed about the hinge: knee = the shin's X, elbow = hs_anim.Poser.arm's hinge
            th, sh = pb[f'{s} thigh'], pb[f'{s} shin']
            vt = (sh.head-th.head).normalized()
            vs = (sh.tail-sh.head).normalized()
            h = sh.matrix.to_3x3().col[0].normalized()
            out[f'{s} knee bend'] = math.degrees(math.atan2(vt.cross(vs).dot(h), vt.dot(vs)))
            qk = sh.rotation_quaternion
            ang = math.degrees(2*math.acos(min(1.0, abs(qk.w))))
            if ang > 15:
                ax = Vector((qk.x, qk.y, qk.z)).normalized()
                out[f'{s} knee off-axis'] = math.degrees(math.acos(min(1.0, abs(ax.x))))
            ua, fa = pb[f'{s} upperarm'], pb[f'{s} forearm']
            vu = (fa.head-ua.head).normalized()
            vf = (fa.tail-fa.head).normalized()
            fd = fa.bone.matrix_local.to_3x3().col[1]
            hinge = (ua.matrix.to_3x3() @ ua.bone.matrix_local.to_3x3().inverted()) @ fd.cross(Vector((0, 1, 0)))
            out[f'{s} elbow bend'] = math.degrees(math.atan2(-vu.cross(vf).dot(hinge.normalized()), vu.dot(vf)))
            tw, _ = twist_swing(pb[f'{s} hand'].rotation_quaternion, 1)
            out[f'{s} hand twist'] = tw
            ftw, _ = twist_swing(fa.rotation_quaternion, 1)
            tb = pb.get(f'{s} forearm twist')
            if tb is not None:
                # skin twist per segment: elbow seam (upper arm -> twist bone) and along the forearm
                elbow, _ = twist_swing(fa.rotation_quaternion @ tb.rotation_quaternion, 1)
                along, _ = twist_swing(tb.rotation_quaternion, 1)
                ftw = max(elbow, -along, key=abs)
            out[f'{s} forearm twist'] = ftw
        return out

    def _feet(self, bco):
        out = {}
        pb = self.p.pb
        for s in H.SIDES:
            ids = self.reg_idx.get(f'{s} foot')
            out[f'{s} sole'] = float(bco[ids, 2].min()) if ids is not None else 9.0
            th, sh, ft = pb[f'{s} thigh'], pb[f'{s} shin'], pb[f'{s} foot']
            toe = pb[f'{s} toe']
            planted = toe.head.z < 0.06 and ft.head.z < 0.2
            vt = (sh.head-th.head).normalized()
            vs = (sh.tail-sh.head).normalized()
            if planted and math.degrees(vt.angle(vs, 0.0)) > 15:
                # the way the knee points: leg axis x hinge (shin X), against the foot's forward, in plan
                kd = (sh.tail-th.head).normalized().cross(sh.matrix.to_3x3().col[0])
                kd.z = 0
                fwd = toe.head-ft.head
                fwd.z = 0
                if kd.length > 1e-4 and fwd.length > 1e-4:
                    out[f'{s} knee dir'] = math.degrees(kd.angle(fwd, 0.0))
        return out

    # summary -----------------------------------------------------------------------------------------------
    def summarize(self, loop, accents=(), exempt=(), finger_accents=(), curl_exempt=()):
        """accents: frames declared as deliberate snaps (pops there may reach ACCENT_POP_MAX).
        exempt: contact keys a clip means to have ('L hand > L thigh').
        finger_accents / curl_exempt: meta frames for handnat (finger snaps, deliberate single-finger flicks)."""
        rows = self.rows[:-1] if loop and len(self.rows) > 2 else self.rows
        n = len(rows)
        fails = []
        pen = {}
        at = {}
        for r in rows:
            for k, v in r['pen'].items():
                e = pen.setdefault(k, [0.0, 0, 0])
                if v > e[0]:
                    e[0], e[1] = v, r['f']
                    at[k] = r.get('where', {}).get(k)
                src, obs = k.split(' > ')
                if v > CONTACT_MAX and (src, obs) not in CREASE:
                    e[2] += 1
        for k, (v, f, nf) in pen.items():
            if nf and k not in exempt:
                fails.append(f'contact {k} {v*1000:.1f}mm f{f}')
        rig = {}
        for r in rows:
            for k, v in r['rigid'].items():
                if v > rig.get(k, (0.0, 0))[0]:
                    rig[k] = (v, r['f'])
        for k, (v, f) in rig.items():
            if v > RIGID_MAX:
                fails.append(f'rigid {k} {v*100:.0f}% f{f}')
        jt = {}
        for r in rows:
            for k, v in r['joints'].items():
                cur = jt.get(k)
                if cur is None:
                    jt[k] = [v, v, r['f'], r['f']]
                else:
                    if v < cur[0]:
                        cur[0], cur[2] = v, r['f']
                    if v > cur[1]:
                        cur[1], cur[3] = v, r['f']
        for s in H.SIDES:
            for j in ('knee', 'elbow'):
                k = f'{s} {j} bend'
                if k in jt and jt[k][0] < -5:
                    fails.append(f'{s} {j} hyperextended {jt[k][0]:.0f} f{jt[k][2]}')
            k = f'{s} knee off-axis'
            if k in jt and jt[k][1] > HINGE_OFF_MAX:
                fails.append(f'{s} knee hinge off-axis {jt[k][1]:.0f} f{jt[k][3]}')
            k = f'{s} hand twist'
            if max(abs(jt[k][0]), abs(jt[k][1])) > HAND_TWIST_MAX:
                fails.append(f'{s} hand twist {max(jt[k][:2], key=abs):.0f}')
            k = f'{s} forearm twist'
            if max(abs(jt[k][0]), abs(jt[k][1])) > FOREARM_TWIST_MAX:
                fails.append(f'{s} forearm twist {max(jt[k][:2], key=abs):.0f}')
        # pops and roll flips on local rotations
        acc = {a+d for a in accents for d in (-1, 0, 1)}
        pops, flips = [], []
        seq = rows+([rows[0], rows[1]] if loop and n > 2 else [])
        for name in self.bones:
            for i in range(1, len(seq)-1):
                a, b, c = seq[i-1]['q'][name], seq[i]['q'][name], seq[i+1]['q'][name]
                if a.dot(c) < 0:
                    c = -c
                d = a.slerp(c, 0.5).rotation_difference(b)
                pops.append((2*math.degrees(min(d.angle, 2*math.pi-d.angle)), name, seq[i]['f']))
                t1, _ = twist_swing(a.rotation_difference(b), 1)
                if abs(t1) > ROLL_FLIP_MAX:
                    flips.append((round(abs(t1), 1), name, seq[i]['f']))
        pops.sort(reverse=True)
        plain = [x for x in pops if x[2] not in acc]
        accent = [x for x in pops if x[2] in acc]
        if plain and plain[0][0] > POP_MAX:
            fails.append(f'pop {plain[0][1]} {plain[0][0]:.0f}deg/f2 f{plain[0][2]}')
        if accent and accent[0][0] > ACCENT_POP_MAX:
            fails.append(f'accent pop {accent[0][1]} {accent[0][0]:.0f}deg/f2 f{accent[0][2]}')
        for x in flips[:1]:
            fails.append(f'roll flip {x[1]} {x[0]} f{x[2]}')
        feet = {}
        for r in rows:
            for k, v in r['feet'].items():
                if k.endswith('sole'):
                    if v < feet.get(k, (9.0, 0))[0]:
                        feet[k] = (v, r['f'])
                elif v > feet.get(k, (-1.0, 0))[0]:
                    feet[k] = (v, r['f'])
        for k, (v, f) in feet.items():
            if k.endswith('sole') and v < SOLE_MIN:
                fails.append(f'{k} {1000*(v-H.GROUND_Z):.1f}mm f{f}')
            if k.endswith('knee dir') and v > KNEE_DIR_MAX:
                fails.append(f'{k} {v:.0f}deg f{f}')
        nat, nat_fails = handnat.summarize(rows, loop, finger_accents, curl_exempt)
        fails += nat_fails
        return {'full_qa': {
            'contact_mm': {k: [round(v*1000, 1), f, nf, at.get(k)] for k, (v, f, nf) in
                           sorted(pen.items(), key=lambda x: -x[1][0]) if v > 0.002},
            'rigid_pct': {k: [round(v*100, 1), f] for k, (v, f) in sorted(rig.items(), key=lambda x: -x[1][0])},
            'joints': {k: [round(v[0], 1), round(v[1], 1)] for k, v in sorted(jt.items())},
            'pops_deg_f2': [(round(r, 1), nm, f) for r, nm, f in plain[:6]],
            'accent_pops_deg_f2': [(round(r, 1), nm, f) for r, nm, f in accent[:3]],
            'feet': {k: [round(v, 4) if k.endswith('sole') else round(v, 1), f] for k, (v, f) in feet.items()},
            'hand_nat': nat,
            'fails': fails},
            'full_qa_ok': not fails,
            'hand_nat_ok': not nat_fails,
            'full_contact_max_mm': round(max((v for k, (v, f, nf) in pen.items() if k not in exempt), default=0)*1000, 1),
            'full_pop_max_deg_f2': round(plain[0][0], 1) if plain else 0.0}


def symmetry(rig, mid=H.MID_X):
    """L/R rest bone pairs mirrored about x = mid: head/tail offset (m) and axis angle (deg) over the limits."""
    out = {}
    M = Matrix.Diagonal((-1, 1, 1))
    for b in rig.data.bones:
        if not b.name.startswith('L ') or not b.use_deform:
            continue
        r = rig.data.bones.get('R '+b.name[2:])
        if r is None:
            continue
        def mir(v):
            return Vector((2*mid-v.x, v.y, v.z))
        dh = (mir(b.head_local)-r.head_local).length
        dt = (mir(b.tail_local)-r.tail_local).length
        ml = M @ b.matrix_local.to_3x3() @ M
        zl = ml @ Vector((0, 0, 1))
        zr = r.matrix_local.to_3x3() @ Vector((0, 0, 1))
        roll = math.degrees(zl.angle(zr, 0.0))
        roll = min(roll, 180-roll)
        if max(dh, dt) > SYM_POS or roll > SYM_ANG:
            out[b.name[2:]] = [round(dh*1000, 1), round(dt*1000, 1), round(roll, 1)]
    return out
