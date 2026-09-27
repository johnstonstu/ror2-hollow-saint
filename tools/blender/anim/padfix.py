"""Pauldron and halo placement fix applied on load (9g; FULL-AUDIT M4 rest clearance), after armfit.

- `R scapula` / `R pauldron` were mirrored about x = 0 like the old R arm (armfit.py); both pad meshes and the
  scapula shells are symmetric about the mesh centre (hs_anim.MID_X), so the R bones become the mirror of the L
  ones about MID_X. The pad is rigid-skinned, so its rest shape doesn't move; bone-parented shells keep their
  world placement.
- The halo lower arcs rest inside the pads' back flaps and the yoke bar runs through both pads (static overlap,
  10-13 mm). The whole halo assembly moves HALO_SHIFT (back and up): the halo bones (arcs, gap lights and inlay
  bands follow), the yoke bar and light, and the strut is sheared so its front still meets the back. The lower
  arcs stay docked on the yoke ends. Clips that hard-code the ring centre add HALO_SHIFT.
- Moved back, the yoke bar's ends stuck out ~6 cm past the lower-arc docks and behind the pads (bare rods in side
  views once the pads follow the arm), so each end is trimmed YOKE_TRIM toward the middle (end caps keep their shape).
- The pauldron "Follow upper arm 40%" Copy Rotation is muted for good: padpass.py drives the pads.
"""
import bpy
from mathutils import Matrix, Vector

import hs_anim as H

HALO_SHIFT = Vector((0.0, 0.065, 0.03))
HALO_BONES = ('halo root', 'halo 1', 'halo 2', 'halo 3', 'halo 4')
YOKE_MOVE = ('HALO | V17 yoke bar', 'HALO | V17 yoke light')
YOKE_STRUT = 'HALO | V17 yoke strut'
MIRROR_R = ('R scapula', 'R pauldron')
YOKE_BAR = 'HALO | V17 yoke bar'
YOKE_TRIM = 0.055                 # m off each end of the yoke bar
YOKE_KEEP = 0.12                  # m either side of the mesh centre left untouched


HALO_LOW_ARCS = ('HALO | independent copper arc 2', 'HALO | independent copper arc 3')


def halo_pivot(p):
    """Armature-space rest centroid of the two lower arcs, where the ring docks on the yoke ends (cached)."""
    piv = getattr(p, 'halo_pivot', None)
    if piv is None:
        rig = p.rig
        saved = rig.data.pose_position
        rig.data.pose_position = 'REST'
        bpy.context.view_layer.update()
        inv = rig.matrix_world.inverted()
        pts = [inv @ (o.matrix_world @ v.co) for o in map(bpy.data.objects.get, HALO_LOW_ARCS) if o
               for v in o.data.vertices]
        rig.data.pose_position = saved
        bpy.context.view_layer.update()
        piv = p.halo_pivot = sum(pts, Vector())/len(pts) if pts else p.rest['halo root'].translation.copy()
    return piv


def halo_pose(p, off, m3):
    """Offset the halo root by `off` and rotate it by m3 (both world axes, rest frame, relative to the chest like
    Poser.offset/rot) pivoting on the lower-arc dock instead of the root head at the ring centre: recoil, lag and
    lean tilts swing the top of the ring while the lower arcs stay clear of the pads (FULL-AUDIT M4)."""
    if not isinstance(m3, Matrix):
        m3 = H.R(*m3)
    d = halo_pivot(p)-p.rest['halo root'].translation
    p.offset('halo root', Vector(off)+d-m3 @ d)
    p.rot('halo root', m3)


def _mirror(v):
    return Vector((2*H.MID_X-v.x, v.y, v.z))


def _edit(rig):
    view = bpy.context.view_layer
    for o in view.objects:
        o.select_set(False)
    rig.hide_set(False)
    rig.hide_viewport = False
    view.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    return rig.data.edit_bones


def shear_strut(obj, shift):
    """Front (min y) end stays; the back end moves with the yoke."""
    if obj.data.users > 1:
        obj.data = obj.data.copy()
    mw = obj.matrix_world
    inv = mw.inverted()
    pts = [mw @ v.co for v in obj.data.vertices]
    y0, y1 = min(q.y for q in pts), max(q.y for q in pts)
    for v, q in zip(obj.data.vertices, pts):
        t = (q.y-y0)/max(y1-y0, 1e-6)
        v.co = inv @ (q+shift*t)
    obj.data.update()


def trim_yoke(obj, trim):
    """Move every vertex further than YOKE_KEEP from MID_X toward the middle by `trim` (world x)."""
    if obj.data.users > 1:
        obj.data = obj.data.copy()
    mw = obj.matrix_world
    inv = mw.inverted()
    for v in obj.data.vertices:
        q = mw @ v.co
        dx = q.x-H.MID_X
        if abs(dx) > YOKE_KEEP:
            q.x -= trim if dx > 0 else -trim
            v.co = inv @ q
    obj.data.update()


def apply(rig):
    """Idempotent (rig['hs_pad_fix'])."""
    if rig.get('hs_pad_fix'):
        return {'already': True}
    prev = bpy.context.view_layer.objects.active
    pose_position = rig.data.pose_position
    rig.data.pose_position = 'REST'
    keep = {o: o.matrix_world.copy() for o in bpy.data.objects
            if o.parent == rig and o.parent_type == 'BONE' and o.parent_bone in MIRROR_R}
    halo_kids = {o: o.matrix_world.copy() for o in bpy.data.objects
                 if o.parent == rig and o.parent_type == 'BONE' and o.parent_bone in HALO_BONES}
    eb = _edit(rig)
    report = {}
    for n in MIRROR_R:
        e, src = eb[n], eb['L'+n[1:]]
        z = src.z_axis.copy()
        old = e.head.copy()
        e.head = _mirror(src.head)
        e.tail = _mirror(src.tail)
        e.align_roll(Vector((-z.x, z.y, z.z)))
        report[n] = {'head_moved_mm': round((e.head-old).length*1000, 1)}
    for n in HALO_BONES:
        e = eb[n]
        e.head += HALO_SHIFT
        e.tail += HALO_SHIFT
    bpy.ops.object.mode_set(mode='OBJECT')
    rig.data.pose_position = pose_position
    bpy.context.view_layer.objects.active = prev
    bpy.context.view_layer.update()
    for o, m in keep.items():
        o.matrix_world = m
    for n in YOKE_MOVE:
        o = bpy.data.objects.get(n)
        if o:
            o.matrix_world = Matrix.Translation(HALO_SHIFT) @ o.matrix_world
    strut = bpy.data.objects.get(YOKE_STRUT)
    if strut:
        shear_strut(strut, HALO_SHIFT)
    bar = bpy.data.objects.get(YOKE_BAR)
    if bar:
        trim_yoke(bar, YOKE_TRIM)
    for pb in (rig.pose.bones.get('L pauldron'), rig.pose.bones.get('R pauldron')):
        for c in (pb.constraints if pb else ()):
            if c.type == 'COPY_ROTATION':
                c.mute = True
                c.influence = 0.0
    bpy.context.view_layer.update()
    report['halo_children_err_mm'] = round(max(((o.matrix_world.translation-m.translation-HALO_SHIFT).length
                                                for o, m in halo_kids.items()), default=0.0)*1000, 3)
    report['kept_meshes'] = len(keep)
    rig['hs_pad_fix'] = (f'9g: R scapula/pauldron mirrored from L about x={H.MID_X}; halo assembly moved '
                         f'{tuple(round(x, 3) for x in HALO_SHIFT)}, yoke bar ends trimmed {YOKE_TRIM} m; '
                         f'pauldron copy-rotation muted (padpass drives it).')
    return report
