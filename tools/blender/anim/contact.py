"""Hand/forearm-into-body interpenetration (item 9c: no fingers inside the body or tabard), every clip.

clearance.py measures unsigned distance (fine for the locomotion >= 10 mm rule, but 0 mm can mean a light
touch or a claw buried in the thigh). This measures how deep hand geometry sits inside the body:
- probes: every 2nd vertex of each bone-parented `<s> HAND |` mesh and every 3rd body vertex dominated by
  `<s> forearm` / `<s> hand`
- body: the evaluated body mesh without the arm faces (upper arm, forearm, hand). A probe is inside when
  at least 4 of 5 rays leave through an odd number of faces (ray parity; majority vote because removing
  the arms leaves holes at the shoulders); depth = distance to the nearest body face. The obstacle is
  named by the dominant group of that face (thigh, pelvis, chest...).
- tabard: closer than the cloth half-thickness (clearance.TABARD_SHELL); depth = shell - distance
- reported as the excess over the rest pose per (side, obstacle), as some parts touch by construction
Used by preview.py (`hand_contact*`, part of PASS: deepest excess <= CONTACT_MAX).
"""
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

import hs_anim as H
from clearance import dominant_groups, TABARD_SHELL

CONTACT_MAX = 0.004       # m beyond rest: a hand may rest on a thigh or brush the cloth, not sink into it
HAND_STEP, ARM_STEP = 2, 3
ARM_GROUPS = ('L upperarm', 'L forearm', 'L hand', 'R upperarm', 'R forearm', 'R hand')
DIRS = [Vector(d).normalized() for d in ((1, 0.2, 0.1), (-0.3, 1, 0.2), (0.1, -0.2, 1), (-1, -0.4, -0.3),
                                         (0.2, 0.3, -1))]


def obstacle(group):
    if group.endswith(('thigh', 'shin', 'foot', 'toe')):
        return group
    return 'torso' if group else 'body'


class Contact:
    def __init__(self):
        self.body = bpy.data.objects[H.BODY]
        dom = dominant_groups(self.body)
        self.n_body = len(dom)
        self.arm_idx = {s: [i for i, g in enumerate(dom) if g in (f'{s} forearm', f'{s} hand')][::ARM_STEP]
                        for s in H.SIDES}
        self.polys = [tuple(p.vertices) for p in self.body.data.polygons if not any(dom[v] in ARM_GROUPS for v in p.vertices)]
        self.poly_group = [obstacle(dom[q[0]]) for q in self.polys]
        self.tabard = [o for o in bpy.data.objects if o.type == 'MESH' and o.name.startswith('TABARD |')]
        self.hand = {s: [(o, [v.co.copy() for v in list(o.data.vertices)[::HAND_STEP]])
                         for o in bpy.data.objects if o.type == 'MESH' and o.name.startswith(f'{s} HAND |')
                         and o.parent_type == 'BONE'] for s in H.SIDES}
        self.dom = dom
        self.rest = None
        self.worst = {}

    @staticmethod
    def _mesh(obj, dg):
        ev = obj.evaluated_get(dg)
        me = ev.to_mesh()
        mw = obj.matrix_world
        v = [mw @ x.co for x in me.vertices]
        p = [tuple(x.vertices) for x in me.polygons]
        ev.to_mesh_clear()
        return v, p

    @staticmethod
    def _inside(tree, q):
        votes = 0
        for d in DIRS:
            n, o = 0, q
            for _ in range(24):
                hit = tree.ray_cast(o, d)
                if hit[0] is None:
                    break
                n += 1
                o = hit[0]+d*1e-5
            votes += n % 2
            if votes >= 4 or votes+(len(DIRS)-1-DIRS.index(d)) < 4:
                break
        return votes >= 4

    def frame(self):
        """{'L': {obstacle: depth_m}, 'R': ...}: deepest probe inside each obstacle (absent = none)."""
        dg = bpy.context.evaluated_depsgraph_get()
        bv, _ = self._mesh(self.body, dg)
        if len(bv) != self.n_body:
            raise RuntimeError('body topology changed under evaluation')
        solid = BVHTree.FromPolygons(bv, self.polys)
        cv, cp = [], []
        for o in self.tabard:
            v, p = self._mesh(o, dg)
            base = len(cv)
            cv += v
            cp += [tuple(base+i for i in q) for q in p]
        cloth = BVHTree.FromPolygons(cv, cp) if cp else None
        out = {}
        self.worst = {}
        for s in H.SIDES:
            pts = [(bv[i], 'forearm') for i in self.arm_idx[s]]
            for o, local in self.hand[s]:
                mw = o.matrix_world
                label = o.name.split('|', 1)[1].strip()
                pts += [(mw @ c, label) for c in local]
            d = {}
            for q, label in pts:
                loc, _, idx, dist = solid.find_nearest(q, 0.08)
                if loc is not None and dist > 0.0005 and self._inside(solid, q):
                    g = self.poly_group[idx]
                    if dist > d.get(g, 0.0):
                        d[g] = dist
                        self.worst[f'{s} {g}'] = (label, self.dom[self.polys[idx][0]])
                if cloth is not None:
                    loc, _, _, dist = cloth.find_nearest(q, TABARD_SHELL)
                    if loc is not None:
                        d['tabard'] = max(d.get('tabard', 0.0), TABARD_SHELL-dist)
            out[s] = {k: round(v, 5) for k, v in d.items()}
        return out

    def calibrate_rest(self):
        self.rest = self.frame()
        return self.rest


def summarize(rows, rest, exempt=()):
    """rows: [(frame, Contact.frame())] -> QA fields (depth beyond the rest pose).
    exempt: frames a clip declares as deliberate contact (meta 'hand_contacts', e.g. a hand pushing on a knee);
    they are reported but not failed."""
    worst = (0.0, '', 0)
    per = {}
    for f, r in rows:
        for s in H.SIDES:
            for g, v in r[s].items():
                ex = v-(rest or {}).get(s, {}).get(g, 0.0)
                key = f'{s} hand > {g}'
                if ex > per.get(key, (0.0, 0))[0]:
                    per[key] = (ex, f)
                if ex > worst[0] and f not in exempt:
                    worst = (ex, key, f)
    return {'hand_contact': {k: (round(v*1000, 1), f) for k, (v, f) in sorted(per.items()) if v > 0.0005},
            'hand_contact_max_mm': round(worst[0]*1000, 1), 'hand_contact_worst': f'{worst[1]} f{worst[2]}',
            'hand_contact_ok': worst[0] <= CONTACT_MAX}
