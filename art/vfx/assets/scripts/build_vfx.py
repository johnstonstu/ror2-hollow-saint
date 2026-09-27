"""Hollow Saint VFX source meshes (vfx-run1). Background Blender only:

  blender.exe --background --factory-startup --python build_vfx.py -- --stage N --out hs-vfx-vNN.blend

Stage 1: bolts (short arcs, long bolts, branching bolts, halo ring + gap arcs).
Stage 2: + heel jet set, Conduit Spear set, conductor mark, Arc Step ground trail, quads, preview-only proxy.
Stage 3: + textured materials (relative //textures/ paths) and preview animation (UV scroll, dissolve, draw-on).

Conventions: metres, Blender Z up, character forward = -Y. Every mesh sits at the object origin with identity
transforms. "Forward" meshes (bolts, jets, spear) extend along Blender -Y, which the FBX export (-Z forward, Y up,
bake space transform) turns into Unity +Z. UV0 'UVMap' = 0..1 along the length (U) and across (V); UV1 'UVTile' =
U in texture tiles (for scrolling / tiled textures). Colour attribute 'Col' (white, alpha = end fade).
Never overwrites a .blend.
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from hs_palette import ARC, CORE, COPPER_GLOW, OUTER  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..'))
TEX = os.path.join(ROOT, 'textures')


def arg(name, default=None):
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    return argv[argv.index(name) + 1] if name in argv else default


# ------------------------------------------------------------------ scene helpers
def collection(name, parent=None):
    col = bpy.data.collections.get(name) or bpy.data.collections.new(name)
    if col.name not in (parent or bpy.context.scene.collection).children:
        (parent or bpy.context.scene.collection).children.link(col)
    return col


def new_object(name, me, col, use=None):
    o = bpy.data.objects.new(name, me)
    col.objects.link(o)
    if use:
        o['unity_use'] = use
    return o


def finish_mesh(me, verts, faces, uv0, uv1, alpha, smooth=False):
    """verts: list of Vector; faces: list of index tuples; uv0/uv1/alpha: per-vertex lists."""
    me.from_pydata([tuple(v) for v in verts], [], faces)
    me.update()
    l0 = me.uv_layers.new(name='UVMap')
    l1 = me.uv_layers.new(name='UVTile')
    ca = me.color_attributes.new('Col', 'BYTE_COLOR', 'CORNER')
    for poly in me.polygons:
        poly.use_smooth = smooth
        for li in poly.loop_indices:
            vi = me.loops[li].vertex_index
            l0.data[li].uv = uv0[vi]
            l1.data[li].uv = uv1[vi]
            ca.data[li].color = (1.0, 1.0, 1.0, alpha[vi])
    me.validate()
    return me


# ------------------------------------------------------------------ bolt geometry
def rand_perp(d, rng):
    while True:
        r = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-1, 1)))
        n = r - d * r.dot(d)
        if n.length > 1e-3:
            return n.normalized()


def bolt3d(a, b, rng, levels=6, coarse_levels=2, coarse_amp=0.12, fine_amp=0.24, rng_fine=None):
    pts = [Vector(a), Vector(b)]
    for lv in range(levels):
        r, amp = (rng, coarse_amp) if lv < coarse_levels else (rng_fine or rng, fine_amp)
        new = [pts[0]]
        for p, q in zip(pts[:-1], pts[1:]):
            d = q - p
            L = d.length
            new += [(p + q) / 2 + rand_perp(d / max(L, 1e-9), r) * r.uniform(-1, 1) * amp * L, q]
        pts = new
    return pts


def lengths(pts):
    s = [0.0]
    for p, q in zip(pts[:-1], pts[1:]):
        s.append(s[-1] + (q - p).length)
    return s


def ribbon(pts, width_fn, alpha_fn, tile_len, up=Vector((0, 0, 1)), crossed=True, u_scale=1.0):
    """Crossed (2 strips at 90 deg) or flat ribbon along a polyline. Returns verts, faces, uv0, uv1, alpha."""
    s = lengths(pts)
    total = max(s[-1], 1e-9)
    verts, faces, uv0, uv1, alpha = [], [], [], [], []
    n = len(pts)
    tans = []
    for i in range(n):
        t = pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]
        tans.append(t.normalized())
    strips = 2 if crossed else 1
    for k in range(strips):
        base = len(verts)
        for i, p in enumerate(pts):
            t = tans[i]
            side = t.cross(up)
            if side.length < 1e-4:
                side = t.cross(Vector((1, 0, 0)))
            side.normalize()
            if k == 1:
                side = t.cross(side).normalized()
            u = s[i] / total
            w = width_fn(u) / 2
            for j, sg in enumerate((-1, 1)):
                verts.append(p + side * w * sg)
                uv0.append((u * u_scale, j))
                uv1.append((s[i] / tile_len, j))
                alpha.append(alpha_fn(u))
        for i in range(n - 1):
            a = base + 2 * i
            faces.append((a, a + 2, a + 3, a + 1))
    return verts, faces, uv0, uv1, alpha


def merge(parts):
    V, F, U0, U1, A = [], [], [], [], []
    for v, f, u0, u1, a in parts:
        off = len(V)
        V += v
        F += [tuple(i + off for i in face) for face in f]
        U0 += u0
        U1 += u1
        A += a
    return V, F, U0, U1, A


def end_fade(fin=0.04, fout=0.06, floor=0.0):
    return lambda u: max(floor, min(1.0, u / fin if fin else 1.0, (1 - u) / fout if fout else 1.0))


def taper(w, end=0.35, span=0.15):
    return lambda u: w * (end + (1 - end) * min(1.0, min(u, 1 - u) / span))


def mesh_from(name, parts, smooth=False):
    me = bpy.data.meshes.new(name)
    V, F, U0, U1, A = merge(parts) if isinstance(parts, list) else parts
    return finish_mesh(me, V, F, U0, U1, A, smooth)


def build_bolts(col):
    made = {}
    # short arcs: 0.4 m along -Y, crossed ribbons 0.018 m wide
    for i, tag in enumerate('ABC'):
        rng = random.Random(100 + i)
        L = 0.4
        pts = bolt3d((0, 0, 0), (0, -L, 0), rng, levels=5, coarse_amp=0.16, fine_amp=0.26)
        parts = [ribbon(pts, taper(0.018, 0.3, 0.2), end_fade(0.05, 0.08), 0.018 * 8)]
        if tag == 'C':
            j = len(pts) // 2
            br = bolt3d(pts[j], pts[j] + Vector((rng.uniform(-.06, .06), -0.12, rng.uniform(.03, .07))), rng, 4)
            parts.append(ribbon(br, taper(0.010, 0.2, 0.3), end_fade(0.0, 0.3), 0.010 * 8))
        o = new_object(f'HS_arc_short_{tag}', mesh_from(f'HS_arc_short_{tag}', parts), col,
                       'Arc Bolt chain hop / fingertip crackle / halo gap arc. Mesh particle or scaled mesh.')
        made[o.name] = o
    # long bolts: 10 m along -Y (pivot = start), 0.06 m wide; scale local Z in Unity = distance / 10
    for i, tag in enumerate('ABC'):
        rng = random.Random(200 + i)
        pts = bolt3d((0, 0, 0), (0, -10, 0), rng, levels=8, coarse_levels=3, coarse_amp=0.035, fine_amp=0.22)
        parts = [ribbon(pts, taper(0.06, 0.45, 0.06), end_fade(0.01, 0.03), 0.06 * 8)]
        o = new_object(f'HS_bolt_long_{tag}', mesh_from(f'HS_bolt_long_{tag}', parts), col,
                       'Arc Bolt tracer / Open Circuit pulse strike. Pivot at start, 10 m long along +Z (Unity).')
        made[o.name] = o
    # branching bolts: 3 m, main 0.07 m + 4-5 branches
    for i, tag in enumerate('AB'):
        rng = random.Random(300 + i)
        pts = bolt3d((0, 0, 0), (0, -3, 0), rng, levels=7, coarse_levels=2, coarse_amp=0.08, fine_amp=0.22)
        parts = [ribbon(pts, taper(0.07, 0.4, 0.08), end_fade(0.02, 0.05), 0.07 * 8)]
        for b in range(4 + i):
            j = int(rng.uniform(0.12, 0.7) * (len(pts) - 1))
            fwd = (pts[min(j + 6, len(pts) - 1)] - pts[j]).normalized()
            side = rand_perp(fwd, rng)
            ang = rng.uniform(0.35, 0.8)
            d = (fwd * math.cos(ang) + side * math.sin(ang)).normalized()
            q = pts[j] + d * rng.uniform(0.45, 1.1)
            br = bolt3d(pts[j], q, rng, levels=5, coarse_levels=1, coarse_amp=0.15, fine_amp=0.25)
            parts.append(ribbon(br, taper(0.035, 0.15, 0.3), end_fade(0.0, 0.35), 0.035 * 8))
            if rng.random() < 0.5:
                k = len(br) // 2
                q2 = br[k] + (d + rand_perp(d, rng) * 0.6).normalized() * rng.uniform(0.2, 0.4)
                br2 = bolt3d(br[k], q2, rng, levels=4, coarse_levels=1)
                parts.append(ribbon(br2, taper(0.018, 0.15, 0.3), end_fade(0.0, 0.4), 0.018 * 8))
        o = new_object(f'HS_bolt_branch_{tag}', mesh_from(f'HS_bolt_branch_{tag}', parts), col,
                       'Discharge side lances / empowered shot. Pivot at start, 3 m along +Z (Unity).')
        made[o.name] = o
    return made


HALO_R = 0.32
HALO_GAPS = [45.0, 135.0, 225.0, 315.0]   # gap centres (deg, 0 = +X, CCW seen from the front); 4 arcs between
HALO_GAP_W = 16.0


def ring_point(a_deg, r):
    a = math.radians(a_deg)
    return Vector((math.cos(a) * r, 0.0, math.sin(a) * r))


def build_halo(col):
    parts = []
    w = 0.022
    for k in range(4):
        a0 = HALO_GAPS[k] + HALO_GAP_W / 2
        a1 = HALO_GAPS[(k + 1) % 4] - HALO_GAP_W / 2 + (360 if k == 3 else 0)
        pts = [ring_point(a, HALO_R) for a in [a0 + (a1 - a0) * t / 40 for t in range(41)]]
        parts.append(ribbon(pts, lambda u: w, end_fade(0.03, 0.03), w * 8, up=Vector((0, -1, 0)), crossed=False))
    seg = new_object('HS_halo_ring_segments', mesh_from('HS_halo_ring_segments', parts), col,
                     'Open Circuit crown shimmer / charge rim. Ring in the Unity XY plane (faces +/-Z), r 0.32 m.')
    made = {seg.name: seg}
    rng = random.Random(400)
    # gaps in meter order (1 per 25%): top-right, top-left, bottom-left, bottom-right
    for k, g in enumerate(HALO_GAPS):
        a = ring_point(g - HALO_GAP_W / 2 - 2, HALO_R)
        b = ring_point(g + HALO_GAP_W / 2 + 2, HALO_R)
        pts = bolt3d(a, b, rng, levels=4, coarse_levels=1, coarse_amp=0.25, fine_amp=0.28)
        # bow the bridge outward along the ring curvature
        for i, p in enumerate(pts):
            t = i / (len(pts) - 1)
            radial = Vector((p.x, 0, p.z)).normalized()
            pts[i] = p + radial * math.sin(math.pi * t) * 0.012
        name = f'HS_halo_gap_arc_{k + 1}'
        o = new_object(name, mesh_from(name, [ribbon(pts, taper(0.015, 0.3, 0.25), end_fade(0.1, 0.1), 0.015 * 8)]),
                       col, f'Charge gap bridge {k + 1} (on at {25 * (k + 1)}% meter); Discharge petal-vent flash. '
                            f'Gap centre {g:.0f} deg in the ring plane.')
        made[name] = o
    return made


# ------------------------------------------------------------------ stage 2 geometry
def lathe(name, profile, sides, length, axis_sign=-1, flatten=None, alpha_fn=None, tile_len=None, cap=False):
    """Surface of revolution along Y. profile(v) -> radius, v 0 = start (y 0 going to y = -length)."""
    rings = len(profile)
    verts, faces, uv0, uv1, alpha = [], [], [], [], []
    for j, (v, r) in enumerate(profile):
        y = axis_sign * v * length
        for i in range(sides + 1):
            th = 2 * math.pi * i / sides
            fz = flatten(v) if flatten else 1.0
            verts.append(Vector((math.cos(th) * r, y, math.sin(th) * r * fz)))
            uv0.append((i / sides, v))
            uv1.append((i / sides, v * length / (tile_len or length)))
            alpha.append(alpha_fn(v) if alpha_fn else 1.0)
    for j in range(rings - 1):
        for i in range(sides):
            a = j * (sides + 1) + i
            b = a + sides + 1
            faces.append((a, a + 1, b + 1, b))
    return mesh_from(name, (verts, faces, uv0, uv1, alpha), smooth=True)


def build_jets(col):
    made = {}
    N = 12
    prof = lambda r0, e: [(j / N, r0 * (1 - j / N) ** e + 0.002) for j in range(N + 1)]
    fade = lambda v: min(1.0, 0.25 + v / 0.1) * max(0.0, 1 - v) ** 1.2
    specs = [('HS_jet_cone_outer', prof(0.05, 0.8), 0.5, 'Glide jet / run push-off plume, outer cyan. Pivot = nozzle.'),
             ('HS_jet_cone_core', prof(0.0225, 0.7), 0.35, 'Glide jet hot core cone (white-cyan), inside the outer.')]
    for name, pr, L, use in specs:
        o = new_object(name, lathe(name, pr, 16, L, alpha_fn=fade, tile_len=0.25), col, use)
        made[name] = o
    # burst: radial spikes fanning out of the nozzle in a ~35 deg cone + a thin shock ring (Arc Step launch /
    # glide ignition). Scale 0 -> 1 over ~3 frames and fade in Unity.
    rng = random.Random(510)
    parts = []
    for k in range(14):
        az = 2 * math.pi * k / 14 + rng.uniform(-0.15, 0.15)
        tilt = math.radians(rng.uniform(8, 38))
        d = Vector((math.sin(tilt) * math.cos(az), -math.cos(tilt), math.sin(tilt) * math.sin(az)))
        L = rng.uniform(0.10, 0.24)
        pts = bolt3d(d * 0.01, d * L, rng, levels=3, coarse_levels=0, fine_amp=0.12)
        parts.append(ribbon(pts, lambda u: 0.016 * (1 - u) + 0.002, lambda u: min(1.0, u / 0.15) * (1 - u) ** 0.8,
                            0.12))
    o = new_object('HS_jet_burst_spikes', mesh_from('HS_jet_burst_spikes', parts), col,
                   'Arc Step launch burst / glide ignition: spikes along +Z (Unity), 0.12 s scale-up + fade.')
    made[o.name] = o
    ring_pts = [Vector((math.cos(a) * 0.055, -0.05, math.sin(a) * 0.055))
                for a in [2 * math.pi * t / 48 for t in range(49)]]
    o = new_object('HS_jet_burst_ring', mesh_from('HS_jet_burst_ring', [
        ribbon(ring_pts, lambda u: 0.014, lambda u: 1.0, 0.112, up=Vector((0, -1, 0)), crossed=False)]), col,
        'Shock ring at the burst mouth; scale 1 -> 2.5 over 0.15 s while fading.')
    made[o.name] = o
    # arcs: 3 zigzag crossed ribbons riding the plume (spin about the axis in Unity)
    parts = []
    rng = random.Random(500)
    L = 0.425
    for a in range(3):
        ang = 2 * math.pi * a / 3
        d = Vector((math.cos(ang), 0, math.sin(ang)))
        pts = []
        kinks = 8
        for k in range(kinks + 1):
            u = k / kinks
            r_cone = 0.05 * (1 - 0.85 * u) ** 0.8          # outer cone radius here (arcs span 0.85 of it)
            off = d * r_cone * (0.85 if k % 2 else 0.25) * rng.uniform(0.8, 1.1)
            pts.append(Vector((0, -L * u, 0)) + off)
        parts.append(ribbon(pts, taper(0.008, 0.3, 0.2), lambda u: max(0.0, 1 - u) ** 0.8, 0.064,
                            up=Vector((d.z, 0, -d.x))))
    o = new_object('HS_jet_arcs', mesh_from('HS_jet_arcs', parts), col,
                   'Crackle arcs inside the jet; spin ~130-160 deg/s about local +Z (Unity).')
    made[o.name] = o
    # ribbon: tapered crossed strip for a mesh plume streak (or reference for the TrailRenderer width curve)
    pts = [Vector((0, -0.6 * t / 16, 0)) for t in range(17)]
    o = new_object('HS_jet_ribbon', mesh_from('HS_jet_ribbon', [ribbon(pts, lambda u: 0.06 * (1 - u) + 0.008,
                                                                         lambda u: min(1.0, u / 0.2) * (1 - u) ** 1.3,
                                                                         0.3)]), col,
                   'Mesh exhaust streak (scroll UVTile.x); TrailRenderer width reference 0.06 -> 0.008 m.')
    made[o.name] = o
    return made


SPEAR_L = 1.2


def spear_radius(v):
    keys = [(0.0, 0.003), (0.06, 0.013), (0.30, 0.019), (0.70, 0.015), (0.77, 0.012), (0.84, 0.030),
            (0.93, 0.014), (1.0, 0.0005)]
    for (v0, r0), (v1, r1) in zip(keys[:-1], keys[1:]):
        if v0 <= v <= v1:
            t = (v - v0) / (v1 - v0)
            t = t * t * (3 - 2 * t)
            return r0 + (r1 - r0) * t
    return keys[-1][1]


def build_spear(col):
    made = {}
    vs = [i / 60 for i in range(61)]
    flat = lambda v: 1.0 - 0.55 * math.exp(-((v - 0.86) / 0.07) ** 2)   # blade-flat spearhead
    # lance: tail at +Y 0.6, tip at -Y 0.6 (Unity +Z = tip). UV V 0 tail -> 1 tip = dissolve direction.
    me = lathe('HS_spear_lance_core', [(v, spear_radius(v)) for v in vs], 6, SPEAR_L, flatten=flat, tile_len=0.3)
    me.transform(Matrix.Translation((0, SPEAR_L / 2, 0)))
    made['HS_spear_lance_core'] = new_object('HS_spear_lance_core', me, col,
                                             'Conduit Spear projectile core (white-hot). Pivot = centre, tip +Z.')
    me = lathe('HS_spear_lance_shell', [(v, spear_radius(v) * 1.9 + 0.006) for v in vs], 10, SPEAR_L,
               flatten=flat, alpha_fn=lambda v: min(1.0, v / 0.08, (1 - v) / 0.04), tile_len=0.3)
    me.transform(Matrix.Translation((0, SPEAR_L / 2, 0)))
    made['HS_spear_lance_shell'] = new_object('HS_spear_lance_shell', me, col,
                                              'Outer cyan glow shell (fresnel/additive), same pivot as the core.')
    # trail: from the tail backwards 1.6 m
    rng = random.Random(600)
    pts = [Vector((rng.uniform(-0.004, 0.004) * (i > 0), SPEAR_L / 2 + 1.6 * i / 24, rng.uniform(-0.004, 0.004) * (i > 0)))
           for i in range(25)]
    made['HS_spear_trail'] = new_object('HS_spear_trail', mesh_from('HS_spear_trail', [
        ribbon(pts, lambda u: 0.05 * (1 - u) ** 0.8 + 0.004, lambda u: (1 - u) ** 1.4, 0.4)]), col,
        'Mesh trail behind the lance (or TrailRenderer: 0.12 s, width 0.05 -> 0).')
    # materialize filaments: 6 thin jagged ribbons from the forearm (behind the tail) converging onto the axis.
    parts = []
    for k in range(6):
        ang = 2 * math.pi * k / 6 + rng.uniform(-0.25, 0.25)
        r0 = rng.uniform(0.035, 0.055)
        start = Vector((math.cos(ang) * r0, SPEAR_L / 2 + rng.uniform(0.28, 0.42), math.sin(ang) * r0))
        end = Vector((0, SPEAR_L / 2 - rng.uniform(0.05, 0.75), 0))
        ctrl = [start.lerp(end, t) + Vector((math.cos(ang + t * 1.4), 0, math.sin(ang + t * 1.4))) * r0 * (1 - t) * 0.6
                for t in [i / 6 for i in range(7)]]
        pts = []
        for p, q in zip(ctrl[:-1], ctrl[1:]):
            seg = bolt3d(p, q, rng, levels=3, coarse_levels=0, fine_amp=0.22)
            pts += seg[:-1]
        pts.append(ctrl[-1])
        parts.append(ribbon(pts, taper(0.004, 0.4, 0.2), end_fade(0.05, 0.2), 0.032))
    made['HS_spear_filaments'] = new_object('HS_spear_filaments', mesh_from('HS_spear_filaments', parts), col,
                                            'Materialize f2-6: draw-on by UVMap.x (0 forearm -> 1 lance).')
    return made


MARK_R = 0.25


def build_mark(col):
    made = {}
    parts = []
    for k in range(4):
        mid = 90.0 * k
        pts = [ring_point(mid - 31 + 62 * t / 30, MARK_R) for t in range(31)]
        parts.append(ribbon(pts, lambda u: 0.028, end_fade(0.04, 0.04), 0.028 * 8, up=Vector((0, -1, 0)), crossed=False))
    made['HS_mark_ring_segments'] = new_object('HS_mark_ring_segments', mesh_from('HS_mark_ring_segments', parts), col,
                                               'Conductor mark: 4 broken cyan ring segments (r 0.25 m), billboard.')
    s = 0.035
    v = [Vector((0, 0, s / 0.8)), Vector((s, 0, 0)), Vector((0, 0, -s / 0.8)), Vector((-s, 0, 0))]
    uv = [((p.x / s + 1) / 2, (p.z / (s / 0.8) + 1) / 2) for p in v]
    made['HS_mark_diamond'] = new_object('HS_mark_diamond', mesh_from('HS_mark_diamond', (v, [(0, 3, 2, 1)], uv, uv, [1] * 4)),
                                         col, 'Conductor mark centre diamond.')
    q = MARK_R / 0.36 / 2   # hs_conductor_mark_512.png draws the ring at 0.36 of its width
    v = [Vector((-q, 0, -q)), Vector((q, 0, -q)), Vector((q, 0, q)), Vector((-q, 0, q))]
    uv = [(0, 0), (1, 0), (1, 1), (0, 1)]
    made['HS_mark_quad'] = new_object('HS_mark_quad', mesh_from('HS_mark_quad', (v, [(0, 1, 2, 3)], uv, uv, [1] * 4)), col,
                                      f'Quad for hs_conductor_mark_512.png ({2 * q:.3f} m wide = same ring size).')
    return made


def build_step(col):
    rng = random.Random(700)
    pts = [Vector((0, 0, 0.03))]
    y, side = 0.0, 1
    while y < 9.0:
        y = min(9.0, y + rng.uniform(0.35, 0.8))
        pts.append(Vector((side * rng.uniform(0.05, 0.14) if y < 9.0 else 0, -y, 0.03)))
        side = -side
    dense = []
    for p, q in zip(pts[:-1], pts[1:]):
        for t in range(4):
            dense.append(p.lerp(q, t / 4))
    dense.append(pts[-1])
    me = mesh_from('HS_step_ground_trail', [ribbon(dense, lambda u: 0.05, end_fade(0.02, 0.02), 0.4, crossed=False)])
    o = new_object('HS_step_ground_trail', me, col,
                   'Arc Step thin angular ground trail, 9 m along +Z (Unity), 3 cm above the floor.')
    return {o.name: o}


def build_quads(col):
    made = {}
    v = [Vector((-0.5, -0.5, 0)), Vector((0.5, -0.5, 0)), Vector((0.5, 0.5, 0)), Vector((-0.5, 0.5, 0))]
    uv = [(0, 0), (1, 0), (1, 1), (0, 1)]
    made['HS_decal_quad_1m'] = new_object('HS_decal_quad_1m', mesh_from('HS_decal_quad_1m', (v, [(0, 1, 2, 3)], uv, uv, [1] * 4)),
                                          col, 'Ground decal quad (scorch / static ring), faces up.')
    v = [Vector((-0.5, 0, -0.5)), Vector((0.5, 0, -0.5)), Vector((0.5, 0, 0.5)), Vector((-0.5, 0, 0.5))]
    made['HS_card_quad_1m'] = new_object('HS_card_quad_1m', mesh_from('HS_card_quad_1m', (v, [(0, 1, 2, 3)], uv, uv, [1] * 4)),
                                         col, 'Camera-facing card for glow / flash textures (mesh particles).')
    return made


def build_proxy(col):
    """Preview-only stand-in figure for the afterimage/ghost material (the game bakes the real skinned mesh)."""
    bm = bmesh.new()

    def capsule(a, b, r):
        a, b = Vector(a), Vector(b)
        d = b - a
        m = Matrix.Translation((a + b) / 2) @ d.to_track_quat('Z', 'Y').to_matrix().to_4x4()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=16, radius1=r, radius2=r * 0.85, depth=d.length, matrix=m)
        bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=8, radius=r, matrix=Matrix.Translation(a))
        bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=8, radius=r * 0.85, matrix=Matrix.Translation(b))

    capsule((0, 0, 1.05), (0, 0, 1.52), 0.15)            # torso
    bmesh.ops.create_uvsphere(bm, u_segments=20, v_segments=12, radius=0.11, matrix=Matrix.Translation((0, 0, 1.74)))
    for s in (-1, 1):
        capsule((0.2 * s, 0, 1.48), (0.3 * s, -0.12, 1.18), 0.05)
        capsule((0.3 * s, -0.12, 1.18), (0.33 * s, -0.3, 1.05), 0.045)
        capsule((0.1 * s, 0, 0.98), (0.12 * s, 0.05 if s < 0 else -0.12, 0.52), 0.07)
        capsule((0.12 * s, 0.05 if s < 0 else -0.12, 0.52), (0.13 * s, 0.1 if s < 0 else -0.05, 0.06), 0.055)
    bmesh.ops.create_circle(bm, cap_ends=False, segments=48, radius=0.3,
                            matrix=Matrix.Translation((0, 0.12, 1.72)) @ Matrix.Rotation(math.pi / 2, 4, 'X'))
    me = bpy.data.meshes.new('PREVIEW_afterimage_proxy')
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    o = new_object('PREVIEW_afterimage_proxy', me, col, 'Preview only (not exported). Ghost material demo.')
    return {o.name: o}


# ------------------------------------------------------------------ stage 3 materials
def node_mat(name):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_fake_user = True
    try:
        m.use_nodes = True
    except Exception:
        pass
    for attr, val in (('surface_render_method', 'BLENDED'), ('blend_method', 'BLEND')):
        try:
            setattr(m, attr, val)
        except Exception:
            pass
    try:
        m.use_backface_culling = False
    except Exception:
        pass
    nt = m.node_tree
    nt.nodes.clear()
    return m, nt


def image(name, non_color=False):
    path = os.path.join(TEX, name)
    img = bpy.data.images.get(name) or bpy.data.images.load(path, check_existing=True)
    if non_color:
        img.colorspace_settings.name = 'Non-Color'
    return img


def mat_additive(name, tex, color, strength, uv='UVMap', scroll=None, non_color=False, use_vcol=True,
                 tex_scale=(1, 1), fresnel=None, tex_rot=0.0):
    """Emission = color * tex.rgb * tex.a * vcol.a * strength, added over Transparent (pure additive).
    scroll: (du, dv) per frame on a Mapping node (preview animation; Unity scrolls the material instead)."""
    m, nt = node_mat(name)
    N, L = nt.nodes, nt.links
    out = N.new('ShaderNodeOutputMaterial')
    add = N.new('ShaderNodeAddShader')
    tr = N.new('ShaderNodeBsdfTransparent')
    em = N.new('ShaderNodeEmission')
    L.new(tr.outputs[0], add.inputs[0])
    L.new(em.outputs[0], add.inputs[1])
    L.new(add.outputs[0], out.inputs['Surface'])
    col = N.new('ShaderNodeRGB')
    col.outputs[0].default_value = (*color, 1)
    strength_chain = N.new('ShaderNodeValue')
    strength_chain.outputs[0].default_value = strength
    s_out = strength_chain.outputs[0]
    c_out = col.outputs[0]
    if tex:
        uvn = N.new('ShaderNodeUVMap')
        uvn.uv_map = uv
        mp = N.new('ShaderNodeMapping')
        mp.name = 'HS mapping'
        mp.inputs['Scale'].default_value = (tex_scale[0], tex_scale[1], 1)
        mp.inputs['Rotation'].default_value = (0, 0, math.radians(tex_rot))
        L.new(uvn.outputs[0], mp.inputs['Vector'])
        it = N.new('ShaderNodeTexImage')
        it.image = image(tex, non_color)
        it.extension = 'REPEAT'
        L.new(mp.outputs[0], it.inputs['Vector'])
        mix = N.new('ShaderNodeMix')
        mix.data_type = 'RGBA'
        mix.blend_type = 'MULTIPLY'
        mix.inputs['Factor'].default_value = 1.0
        L.new(c_out, mix.inputs[6])
        L.new(it.outputs['Color'], mix.inputs[7])
        c_out = mix.outputs[2]
        mul = N.new('ShaderNodeMath')
        mul.operation = 'MULTIPLY'
        L.new(s_out, mul.inputs[0])
        L.new(it.outputs['Alpha'], mul.inputs[1])
        s_out = mul.outputs[0]
        if scroll:
            for i, rate in enumerate(scroll):
                if rate:
                    fc = mp.inputs['Location'].driver_add('default_value', i)
                    fc.driver.type = 'SCRIPTED'
                    fc.driver.expression = f'frame*{rate}'
    if use_vcol:
        vc = N.new('ShaderNodeVertexColor')
        vc.layer_name = 'Col'
        mul = N.new('ShaderNodeMath')
        mul.operation = 'MULTIPLY'
        L.new(s_out, mul.inputs[0])
        L.new(vc.outputs['Alpha'], mul.inputs[1])
        s_out = mul.outputs[0]
    if fresnel is not None:
        lw = N.new('ShaderNodeLayerWeight')
        lw.inputs['Blend'].default_value = fresnel
        mul = N.new('ShaderNodeMath')
        mul.operation = 'MULTIPLY'
        L.new(s_out, mul.inputs[0])
        L.new(lw.outputs['Facing'], mul.inputs[1])
        s_out = mul.outputs[0]
    L.new(c_out, em.inputs['Color'])
    L.new(s_out, em.inputs['Strength'])
    m['hs_note'] = f'additive; tex={tex}; colour(linear)={tuple(round(c, 3) for c in color)}; strength={strength}'
    return m


def mat_threshold(name, tex, color, strength, channel, uv, thr_frames, edge=0.07, edge_boost=3.0,
                  fresnel=None):
    """Reveal where mask < threshold (threshold keyed thr_frames=[(frame, value), ...]); hot edge band."""
    m, nt = node_mat(name)
    N, L = nt.nodes, nt.links
    out = N.new('ShaderNodeOutputMaterial')
    add = N.new('ShaderNodeAddShader')
    tr = N.new('ShaderNodeBsdfTransparent')
    em = N.new('ShaderNodeEmission')
    L.new(tr.outputs[0], add.inputs[0])
    L.new(em.outputs[0], add.inputs[1])
    L.new(add.outputs[0], out.inputs['Surface'])
    uvn = N.new('ShaderNodeUVMap')
    uvn.uv_map = uv
    if tex:
        it = N.new('ShaderNodeTexImage')
        it.image = image(tex, True)
        L.new(uvn.outputs[0], it.inputs['Vector'])
        sep = N.new('ShaderNodeSeparateColor')
        L.new(it.outputs['Color'], sep.inputs[0])
        mask = sep.outputs[channel]
    else:
        sep = N.new('ShaderNodeSeparateXYZ')
        L.new(uvn.outputs[0], sep.inputs[0])
        mask = sep.outputs[channel]
    thr = N.new('ShaderNodeValue')
    thr.name = 'HS threshold'
    for f, v in thr_frames:
        thr.outputs[0].default_value = v
        thr.outputs[0].keyframe_insert('default_value', frame=f)
    lt = N.new('ShaderNodeMath')
    lt.operation = 'LESS_THAN'
    L.new(mask, lt.inputs[0])
    L.new(thr.outputs[0], lt.inputs[1])
    lo = N.new('ShaderNodeMath')
    lo.operation = 'SUBTRACT'
    L.new(thr.outputs[0], lo.inputs[0])
    lo.inputs[1].default_value = edge
    gt = N.new('ShaderNodeMath')
    gt.operation = 'GREATER_THAN'
    L.new(mask, gt.inputs[0])
    L.new(lo.outputs[0], gt.inputs[1])
    band = N.new('ShaderNodeMath')
    band.operation = 'MULTIPLY'
    L.new(lt.outputs[0], band.inputs[0])
    L.new(gt.outputs[0], band.inputs[1])
    boost = N.new('ShaderNodeMath')
    boost.operation = 'MULTIPLY_ADD'
    L.new(band.outputs[0], boost.inputs[0])
    boost.inputs[1].default_value = edge_boost
    L.new(lt.outputs[0], boost.inputs[2])
    st = N.new('ShaderNodeMath')
    st.operation = 'MULTIPLY'
    L.new(boost.outputs[0], st.inputs[0])
    st.inputs[1].default_value = strength
    s_out = st.outputs[0]
    vc = N.new('ShaderNodeVertexColor')
    vc.layer_name = 'Col'
    mul = N.new('ShaderNodeMath')
    mul.operation = 'MULTIPLY'
    L.new(s_out, mul.inputs[0])
    L.new(vc.outputs['Alpha'], mul.inputs[1])
    s_out = mul.outputs[0]
    if fresnel is not None:
        lw = N.new('ShaderNodeLayerWeight')
        lw.inputs['Blend'].default_value = fresnel
        mul2 = N.new('ShaderNodeMath')
        mul2.operation = 'MULTIPLY'
        L.new(s_out, mul2.inputs[0])
        L.new(lw.outputs['Facing'], mul2.inputs[1])
        s_out = mul2.outputs[0]
    col = N.new('ShaderNodeRGB')
    col.outputs[0].default_value = (*color, 1)
    L.new(col.outputs[0], em.inputs['Color'])
    L.new(s_out, em.inputs['Strength'])
    m['hs_note'] = f'threshold reveal; mask={tex or "UV"}[{channel}]; edge {edge} x{edge_boost}'
    return m


def mat_decal(name, tex, strength=1.0):
    m, nt = node_mat(name)
    N, L = nt.nodes, nt.links
    out = N.new('ShaderNodeOutputMaterial')
    mix = N.new('ShaderNodeMixShader')
    tr = N.new('ShaderNodeBsdfTransparent')
    em = N.new('ShaderNodeEmission')
    it = N.new('ShaderNodeTexImage')
    it.image = image(tex)
    em.inputs['Strength'].default_value = strength
    L.new(it.outputs['Color'], em.inputs['Color'])
    L.new(it.outputs['Alpha'], mix.inputs['Fac'])
    L.new(tr.outputs[0], mix.inputs[1])
    L.new(em.outputs[0], mix.inputs[2])
    L.new(mix.outputs[0], out.inputs['Surface'])
    m['hs_note'] = f'alpha-blended decal (unlit preview); tex={tex}'
    return m


def assign(o, m):
    o.data.materials.clear()
    o.data.materials.append(m)


def build_materials(objs):
    M = {}
    # Strengths are tuned for the Standard view transform previews (white core, cyan edges, little clipping).
    # Unity HDR intensities are listed separately in the README.
    M['bolt'] = mat_additive('HS_M_bolt', 'hs_bolt_ribbon_tile_512x64.png', CORE, 2.6, uv='UVTile', scroll=(-0.9, 0))
    M['arc'] = mat_additive('HS_M_arc', 'hs_bolt_ribbon_tile_512x64.png', ARC, 1.8, uv='UVTile', scroll=(-1.3, 0))
    M['gap_arc'] = mat_additive('HS_M_halo_gap_arc', 'hs_bolt_ribbon_tile_512x64.png', CORE, 3.2, uv='UVTile',
                                scroll=(-1.6, 0))
    M['halo'] = mat_additive('HS_M_halo_shimmer', 'hs_band_profile_64.png', OUTER, 1.0, uv='UVTile', scroll=(0.03, 0))
    M['burst_ring'] = mat_additive('HS_M_jet_burst_ring', 'hs_band_profile_64.png', ARC, 1.4, uv='UVTile')
    M['jet_outer'] = mat_additive('HS_M_jet_outer', 'hs_jet_exhaust_256.png', OUTER, 0.9, scroll=(0.04, 0))
    M['jet_core'] = mat_additive('HS_M_jet_core', 'hs_jet_exhaust_256.png', CORE, 1.2, scroll=(-0.06, 0))
    M['jet_ribbon'] = mat_additive('HS_M_jet_ribbon', 'hs_noise_streak_256.png', OUTER, 1.2, uv='UVTile',
                                   scroll=(0.12, 0), non_color=True, tex_rot=90)
    M['spear_core'] = mat_threshold('HS_M_spear_core_materialize', 'hs_spear_dissolve_mask_128x512.png', CORE, 1.6,
                                    'Blue', 'UVMap', [(1, 0.0), (12, 1.02), (24, 1.02)], edge_boost=1.5)
    M['spear_shell'] = mat_threshold('HS_M_spear_shell_materialize', 'hs_spear_dissolve_mask_128x512.png', OUTER, 1.1,
                                     'Blue', 'UVMap', [(2, 0.0), (14, 1.02), (24, 1.02)], fresnel=0.35, edge_boost=1.5)
    M['spear_fil'] = mat_threshold('HS_M_spear_filaments_drawon', None, ARC, 0.8, 'X', 'UVMap',
                                   [(1, 0.0), (8, 1.05), (14, 1.05), (20, 1.4)], edge=0.12, edge_boost=1.5)
    M['spear_trail'] = mat_additive('HS_M_spear_trail', 'hs_bolt_ribbon_tile_512x64.png', OUTER, 1.8, uv='UVMap',
                                    scroll=(-0.1, 0), tex_scale=(4, 1))
    M['mark'] = mat_additive('HS_M_mark', 'hs_band_profile_64.png', OUTER, 1.8, uv='UVTile')
    M['mark_core'] = mat_additive('HS_M_mark_core', None, CORE, 1.3)
    M['mark_quad'] = mat_additive('HS_M_mark_quad', 'hs_conductor_mark_512.png', (1, 1, 1), 1.2)
    M['trail'] = mat_additive('HS_M_step_trail', 'hs_bolt_ribbon_tile_512x64.png', ARC, 1.8, uv='UVTile', scroll=(-0.5, 0))
    M['ghost'] = mat_additive('HS_M_afterimage_ghost', 'hs_ghost_scanline_256.png', OUTER, 0.9, uv='UVMap',
                              use_vcol=False, fresnel=0.55, non_color=True)
    M['decal'] = mat_decal('HS_M_scorch_decal', 'hs_scorch_decal_512.png', 1.0)
    M['static_ring'] = mat_additive('HS_M_static_ring', 'hs_static_ring_512.png', (1, 1, 1), 1.2)
    M['card'] = mat_additive('HS_M_card_glow', 'hs_glow_star_256.png', (1, 1, 1), 1.2)
    table = {'HS_arc_short_': 'arc', 'HS_bolt_long_': 'bolt', 'HS_bolt_branch_': 'bolt',
             'HS_halo_ring_segments': 'halo', 'HS_halo_gap_arc_': 'gap_arc', 'HS_jet_cone_outer': 'jet_outer',
             'HS_jet_cone_core': 'jet_core', 'HS_jet_burst_spikes': 'arc', 'HS_jet_burst_ring': 'burst_ring',
             'HS_jet_arcs': 'arc',
             'HS_jet_ribbon': 'jet_ribbon', 'HS_spear_lance_core': 'spear_core', 'HS_spear_lance_shell': 'spear_shell',
             'HS_spear_filaments': 'spear_fil', 'HS_spear_trail': 'spear_trail', 'HS_mark_ring_segments': 'mark',
             'HS_mark_diamond': 'mark_core', 'HS_mark_quad': 'mark_quad', 'HS_step_ground_trail': 'trail',
             'PREVIEW_afterimage_proxy': 'ghost', 'HS_decal_quad_1m': 'decal', 'HS_card_quad_1m': 'card'}
    for name, o in objs.items():
        for prefix, key in table.items():
            if name.startswith(prefix):
                assign(o, M[key])
                break
    # proxy uses generated coords for the scanlines (it has no UVs of its own)
    return M


def ghost_proxy_uv(o):
    me = o.data
    uv = me.uv_layers.new(name='UVMap')
    for li, loop in enumerate(me.loops):
        co = me.vertices[loop.vertex_index].co
        uv.data[li].uv = (math.atan2(co.y, co.x) / (2 * math.pi) * 4, co.z * 4)


def main():
    stage = int(arg('--stage', '3'))
    out = os.path.join(ROOT, arg('--out', f'hs-vfx-v{stage:02d}.blend'))
    if os.path.exists(out):
        raise SystemExit(f'refusing to overwrite {out}')
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.fps = 24
    scene.frame_start, scene.frame_end = 1, 24
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    top = collection('HS VFX')
    objs = {}
    objs.update(build_bolts(collection('01 bolts', top)))
    objs.update(build_halo(collection('02 halo ring', top)))
    if stage >= 2:
        objs.update(build_jets(collection('03 heel jet', top)))
        objs.update(build_spear(collection('04 conduit spear', top)))
        objs.update(build_mark(collection('05 conductor mark', top)))
        objs.update(build_step(collection('06 arc step', top)))
        objs.update(build_quads(collection('07 quads', top)))
        prev = collection('99 preview only (not exported)', top)
        p = build_proxy(prev)
        ghost_proxy_uv(p['PREVIEW_afterimage_proxy'])
        objs.update(p)
    if stage >= 3:
        build_materials(objs)
        w = bpy.data.worlds.new('HS dark')
        w.color = (0.004, 0.005, 0.008)
        if w.node_tree:
            for n in w.node_tree.nodes:
                if n.type == 'BACKGROUND':
                    n.inputs['Color'].default_value = (0.004, 0.005, 0.008, 1)
        scene.world = w
        scene.view_settings.view_transform = 'Standard'
    scene['hs_vfx_stage'] = stage
    scene['hs_vfx_notes'] = ('Hollow Saint VFX sources (vfx-run1). Metres, Z up, forward -Y (Unity +Z after FBX -Z fwd/Y up '
                             'with Apply Transform). See art/vfx/assets/README.md.')
    bpy.ops.wm.save_as_mainfile(filepath=out, relative_remap=True, compress=True)
    stats = {n: (len(o.data.vertices), len(o.data.polygons)) for n, o in objs.items()}
    with open(os.path.join(HERE, f'build_stage{stage}_stats.txt'), 'a') as f:
        f.write(f'{os.path.basename(out)}\n' + '\n'.join(f'  {n}: {v} verts, {p} faces' for n, (v, p) in sorted(stats.items())) + '\n')


main()
