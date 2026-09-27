"""Arm clearance QA: per frame, the closest approach of each forearm/hand to the torso, hips and tabard.

Probe points: body verts dominated by `<s> forearm` / `<s> hand` and every bone-parented `<s> HAND |`
mesh (palm, knuckles, finger segments, cuff). Obstacles: body faces whose verts are all
dominated by pelvis/spine/chest/thigh groups plus the abdomen/rib plates, and the tabard meshes (minus
the cloth half-thickness). Distances are unsigned: the torso subset is an open surface, so a normal-based
inside test is unreliable, but the arm geometry is continuous, so any penetration drives some probe
vertex onto the surface (min distance ~0).
Used by preview.py; `ARM_CLEAR_MIN` applies to locomotion clips (kind == 'locomotion').
"""
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

import hs_anim as H

ARM_CLEAR_MIN = 0.010         # m, locomotion: hands/forearms stay at least this far off torso/hips/tabard
TABARD_SHELL = 0.004          # m, cloth half-thickness counted as contact for the unsigned tabard distance
TORSO_GROUPS = ('pelvis', 'spine', 'chest', 'L thigh', 'R thigh')
PLATE_PREFIXES = ('ABDOMEN | V17 segment plate', 'CHEST | V17 rib plate')
HAND_STEP = 3                 # every Nth hand-mesh vertex (the segment meshes are dense bevels)


def dominant_groups(obj):
    names = {g.index: g.name for g in obj.vertex_groups}
    out = []
    for v in obj.data.vertices:
        out.append(names[max(v.groups, key=lambda e: e.weight).group] if v.groups else '')
    return out


class Clearance:
    def __init__(self):
        self.body = bpy.data.objects[H.BODY]
        dom = dominant_groups(self.body)
        self.n_body = len(dom)
        self.arm_idx = {s: [i for i, g in enumerate(dom) if g in (f'{s} forearm', f'{s} hand')] for s in H.SIDES}
        torso = {i for i, g in enumerate(dom) if g in TORSO_GROUPS}
        self.torso_polys = [tuple(p.vertices) for p in self.body.data.polygons if all(v in torso for v in p.vertices)]
        self.torso_poly_group = [dom[q[0]] for q in self.torso_polys]
        self.plates = [o for o in bpy.data.objects if o.type == 'MESH' and o.name.startswith(PLATE_PREFIXES)]
        self.tabard = [o for o in bpy.data.objects if o.type == 'MESH' and o.name.startswith('TABARD |')]
        # Hand meshes are rigid children of the hand/finger bones, so object-space verts are enough.
        self.hand_objs = {s: [o for o in bpy.data.objects if o.type == 'MESH' and o.name.startswith(f'{s} HAND |')
                              and o.parent_type == 'BONE'] for s in H.SIDES}
        self.hand_pts = {s: [(o, o.name.split('|', 1)[1].strip(), [v.co.copy() for v in list(o.data.vertices)[::HAND_STEP]])
                             for o in self.hand_objs[s]] for s in H.SIDES}

    @staticmethod
    def _world_mesh(obj, dg):
        ev = obj.evaluated_get(dg)
        me = ev.to_mesh()
        mw = obj.matrix_world
        verts = [mw @ v.co for v in me.vertices]
        polys = [tuple(p.vertices) for p in me.polygons]
        ev.to_mesh_clear()
        return verts, polys

    def frame(self):
        """Measure the current evaluated pose: {side: (min_m, part, against)}."""
        dg = bpy.context.evaluated_depsgraph_get()
        bverts, _ = self._world_mesh(self.body, dg)
        if len(bverts) != self.n_body:
            raise RuntimeError('body topology changed under evaluation; clearance indices invalid')
        tv, tp, tg = list(bverts), list(self.torso_polys), list(self.torso_poly_group)
        for o in self.plates:
            v, p = self._world_mesh(o, dg)
            base = len(tv)
            tv += v
            tp += [tuple(base+i for i in q) for q in p]
            tg += [o.name.split('|', 1)[1].strip()]*len(p)
        torso = BVHTree.FromPolygons(tv, tp)
        cv, cp, cg = [], [], []
        for o in self.tabard:
            v, p = self._world_mesh(o, dg)
            base = len(cv)
            cv += v
            cp += [tuple(base+i for i in q) for q in p]
            cg += [o.name.split('|', 1)[1].strip()]*len(p)
        cloth = BVHTree.FromPolygons(cv, cp)
        out = {}
        for s in H.SIDES:
            pts = [('forearm', bverts[i]) for i in self.arm_idx[s]]
            for o, part, local in self.hand_pts[s]:
                mw = o.matrix_world
                pts += [(part, mw @ c) for c in local]
            best = (9.0, '', '')
            for part, q in pts:
                hit = torso.find_nearest(q)
                if hit[0] is not None and hit[3] < best[0]:
                    best = (hit[3], part, tg[hit[2]])
                hit = cloth.find_nearest(q)
                if hit[0] is not None:
                    d = hit[3]-TABARD_SHELL
                    if d < best[0]:
                        best = (d, part, 'tabard '+cg[hit[2]])
            out[s] = best
        return out


def summarize(per_frame, kind):
    """per_frame: [(frame, {side: (d, part, against)})] -> QA fields."""
    rep = {}
    worst = 9.0
    for s in H.SIDES:
        f, (d, part, against) = min(((f, m[s]) for f, m in per_frame), key=lambda x: x[1][0])
        rep[s] = {'min_m': round(d, 4), 'frame': f, 'part': part, 'against': against,
                  'frames_under_min': sum(1 for _, m in per_frame if m[s][0] < ARM_CLEAR_MIN),
                  'per_frame_mm': [round(m[s][0]*1000) for _, m in per_frame],
                  'hits': {str(fr): f'{m[s][1]} > {m[s][2]}' for fr, m in per_frame if m[s][0] < ARM_CLEAR_MIN}}
        worst = min(worst, d)
    return {'arm_clearance': rep, 'arm_clear_min_m': round(worst, 4),
            'arm_clear_ok': (worst >= ARM_CLEAR_MIN) if kind == 'locomotion' else None}
