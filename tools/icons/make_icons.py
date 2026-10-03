#!/usr/bin/env python3
"""
Hollow Saint icon generator.
Generates 9 RoR2-style icons at 256x256 (supersampled at 1024x1024, LANCZOS downscale).
"""

import math
import random
from PIL import Image, ImageDraw, ImageFilter, ImageChops

# ---------------------------------------------------------------------------
# Config
# ---------------------------------------------------------------------------

SS = 4                      # supersample factor
FINAL = 256
CANVAS = FINAL * SS         # 1024

OUT_DIR = "/mnt/user-data/outputs/icons"

# Palette
WHITE_HOT = (255, 249, 240, 255)
ARC_CYAN = (77, 235, 255, 255)
OUTER_CYAN = (10, 158, 217, 255)
COPPER = (158, 92, 51, 255)
IVORY = (237, 230, 214, 255)

BG_BOTTOM = (12, 18, 32)
BG_TOP = (27, 42, 61)
BORDER = (58, 80, 104, 255)

CORNER_RADIUS = 28 * SS
BORDER_W = 4 * SS


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def new_canvas():
    return Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))


def rounded_rect_mask(size, radius):
    mask = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(mask)
    d.rounded_rectangle([0, 0, size - 1, size - 1], radius=radius, fill=255)
    return mask


def tile_background():
    """Dark rounded-square tile with subtle vertical gradient + steel border."""
    bg = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    grad = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 255))
    px = grad.load()
    for y in range(CANVAS):
        t = y / (CANVAS - 1)  # 0 top .. 1 bottom
        r = int(BG_TOP[0] + (BG_BOTTOM[0] - BG_TOP[0]) * t)
        g = int(BG_TOP[1] + (BG_BOTTOM[1] - BG_TOP[1]) * t)
        b = int(BG_TOP[2] + (BG_BOTTOM[2] - BG_TOP[2]) * t)
        for x in range(0, CANVAS, 1):
            pass
        # faster: fill row
        row = Image.new("RGBA", (CANVAS, 1), (r, g, b, 255))
        grad.paste(row, (0, y))
    mask = rounded_rect_mask(CANVAS, CORNER_RADIUS)
    bg.paste(grad, (0, 0), mask)

    # inner border
    d = ImageDraw.Draw(bg)
    inset = BORDER_W // 2
    d.rounded_rectangle(
        [inset, inset, CANVAS - 1 - inset, CANVAS - 1 - inset],
        radius=max(CORNER_RADIUS - inset, 0),
        outline=BORDER,
        width=BORDER_W,
    )
    return bg, mask


def glow_layer(draw_fn, blur_radius, color, width_mult=2.2, base_width=6 * SS):
    """Render shape via draw_fn onto a transparent layer, thick + colored, then blur."""
    layer = new_canvas()
    d = ImageDraw.Draw(layer)
    draw_fn(d, color, int(base_width * width_mult))
    return layer.filter(ImageFilter.GaussianBlur(blur_radius))


def composite_glow_core(canvas, draw_fn, glow_color=ARC_CYAN, core_color=WHITE_HOT,
                         glow_width=10 * SS, core_width=3.2 * SS, blur=10 * SS,
                         glow_alpha_mult=1.0):
    """Standard lightning render: wide colored glow (blurred) + sharp white core on top."""
    glow = new_canvas()
    gd = ImageDraw.Draw(glow)
    gc = list(glow_color)
    gc[3] = int(gc[3] * glow_alpha_mult)
    draw_fn(gd, tuple(gc), int(glow_width))
    glow = glow.filter(ImageFilter.GaussianBlur(blur))
    canvas.alpha_composite(glow)

    core = new_canvas()
    cd = ImageDraw.Draw(core)
    draw_fn(cd, core_color, int(core_width))
    canvas.alpha_composite(core)


def jagged_path(p0, p1, seed, segments=5, disp=0.22, min_disp_px=6 * SS):
    """Random midpoint displacement between p0 and p1. Deterministic via seed."""
    rng = random.Random(seed)
    pts = [p0, p1]
    length = math.hypot(p1[0] - p0[0], p1[1] - p0[1])
    for _ in range(segments):
        new_pts = [pts[0]]
        for i in range(len(pts) - 1):
            a, b = pts[i], pts[i + 1]
            mx, my = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
            dx, dy = b[0] - a[0], b[1] - a[1]
            seg_len = math.hypot(dx, dy)
            if seg_len < 1e-6:
                new_pts.append(b)
                continue
            # perpendicular
            nx, ny = -dy / seg_len, dx / seg_len
            mag = max(seg_len * disp, min_disp_px) * (0.6 ** _)
            offset = rng.uniform(-1, 1) * mag
            new_pts.append((mx + nx * offset, my + ny * offset))
            new_pts.append(b)
        pts = new_pts
    return pts


def draw_polyline(d, pts, color, width):
    d.line(pts, fill=color, width=width, joint="curve")
    r = width / 2
    for p in pts:
        d.ellipse([p[0] - r, p[1] - r, p[0] + r, p[1] + r], fill=color)


def spark_burst(d, center, color, width, n=5, length=(14, 26), seed=0):
    rng = random.Random(seed)
    cx, cy = center
    for i in range(n):
        ang = rng.uniform(0, math.pi * 2)
        ln = rng.uniform(*length) * SS
        x2 = cx + math.cos(ang) * ln
        y2 = cy + math.sin(ang) * ln
        d.line([cx, cy, x2, y2], fill=color, width=width)


def save_final(canvas, name):
    final = canvas.resize((FINAL, FINAL), Image.LANCZOS)
    final.save(f"{OUT_DIR}/{name}")
    print(f"saved {name}")


def skill_canvas():
    canvas, mask = tile_background()
    return canvas, mask


def finalize_skill(canvas, mask, name):
    # clip to rounded rect just in case glow bled past edges
    clipped = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    clipped.paste(canvas, (0, 0), mask)
    save_final(clipped, name)


# ---------------------------------------------------------------------------
# buff icon helper (transparent bg, white symbol + dark outline)
# ---------------------------------------------------------------------------

def buff_canvas():
    return new_canvas()


def finalize_buff(canvas, name):
    save_final(canvas, name)


def white_symbol_with_outline(draw_fn, outline_width_extra=6 * SS,
                               outline_color=(0, 0, 0, 130)):
    """Draw shape twice: thicker dark translucent outline pass, then pure white on top."""
    layer = new_canvas()
    od = ImageDraw.Draw(layer)
    draw_fn(od, outline_color, extra=outline_width_extra)
    wd = ImageDraw.Draw(layer)
    draw_fn(wd, (255, 255, 255, 255), extra=0)
    return layer


# ===========================================================================
# 1. skill_arc_bolt.png — primary
# ===========================================================================

def make_arc_bolt():
    canvas, mask = skill_canvas()
    cx, cy = CANVAS / 2, CANVAS / 2

    # main bolt: thick, from lower-left point to a central impact point
    start = (CANVAS * 0.16, CANVAS * 0.86)
    impact = (CANVAS * 0.50, CANVAS * 0.50)

    # two chain hops branching from the impact point to smaller spark nodes,
    # forking apart so the shape reads as "a bolt that chains"
    nodeA = (CANVAS * 0.82, CANVAS * 0.20)
    nodeB = (CANVAS * 0.80, CANVAS * 0.58)

    main_pts = jagged_path(start, impact, seed=11, segments=4, disp=0.26)
    branchA_pts = jagged_path(impact, nodeA, seed=22, segments=3, disp=0.30)
    branchB_pts = jagged_path(impact, nodeB, seed=33, segments=3, disp=0.34)

    def draw_main(d, color, width):
        draw_polyline(d, main_pts, color, width)

    def draw_branches(d, color, width):
        draw_polyline(d, branchA_pts, color, width)
        draw_polyline(d, branchB_pts, color, width)

    # main bolt: ~3x thicker core, stronger/wider cyan glow
    composite_glow_core(canvas, draw_main, glow_color=ARC_CYAN, core_color=WHITE_HOT,
                         glow_width=34 * SS, core_width=10 * SS, blur=15 * SS,
                         glow_alpha_mult=1.0)

    # chain-hop branches: thinner than the main bolt, still clearly lightning
    composite_glow_core(canvas, draw_branches, glow_color=ARC_CYAN, core_color=WHITE_HOT,
                         glow_width=15 * SS, core_width=4.2 * SS, blur=9 * SS)

    # spark nodes: point of origin, impact point, and the two branch tips
    glow = new_canvas()
    gd = ImageDraw.Draw(glow)
    for pt, r in [(start, 8), (impact, 11), (nodeA, 8), (nodeB, 8)]:
        rr = r * SS
        gd.ellipse([pt[0] - rr, pt[1] - rr, pt[0] + rr, pt[1] + rr], fill=ARC_CYAN)
    glow = glow.filter(ImageFilter.GaussianBlur(9 * SS))
    canvas.alpha_composite(glow)

    core = new_canvas()
    cd = ImageDraw.Draw(core)
    for pt, r in [(start, 4.0), (impact, 5.2), (nodeA, 4.4), (nodeB, 4.4)]:
        rr = r * SS
        cd.ellipse([pt[0] - rr, pt[1] - rr, pt[0] + rr, pt[1] + rr], fill=WHITE_HOT)
    # extra spark rays at the two branch tips
    spark_burst(cd, nodeA, WHITE_HOT, int(2.4 * SS), n=6, length=(10, 20), seed=99)
    spark_burst(cd, nodeB, WHITE_HOT, int(2.4 * SS), n=6, length=(10, 18), seed=77)
    canvas.alpha_composite(core)

    finalize_skill(canvas, mask, "skill_arc_bolt.png")


# ===========================================================================
# 2. skill_conduit_spear.png — secondary
# ===========================================================================

def make_conduit_spear():
    canvas, mask = skill_canvas()
    cx, cy = CANVAS / 2, CANVAS / 2

    # spear axis: from lower-left to upper-right
    tail = (CANVAS * 0.14, CANVAS * 0.88)
    tip = (CANVAS * 0.90, CANVAS * 0.12)
    dx, dy = tip[0] - tail[0], tip[1] - tail[1]
    length = math.hypot(dx, dy)
    ux, uy = dx / length, dy / length   # unit along axis
    px, py = -uy, ux                    # unit perpendicular

    def pt(along, perp):
        return (tail[0] + ux * along + px * perp,
                 tail[1] + uy * along + py * perp)

    # cyan sheath (wide diamond-ish lance body)
    sheath_pts = [
        pt(0, 0),
        pt(length * 0.55, 16 * SS),
        pt(length * 0.90, 5 * SS),
        pt(length * 1.02, 0),
        pt(length * 0.90, -5 * SS),
        pt(length * 0.55, -16 * SS),
    ]
    sheath_glow = new_canvas()
    sd = ImageDraw.Draw(sheath_glow)
    sd.polygon(sheath_pts, fill=OUTER_CYAN)
    sheath_glow = sheath_glow.filter(ImageFilter.GaussianBlur(7 * SS))
    canvas.alpha_composite(sheath_glow)

    sheath = new_canvas()
    sd2 = ImageDraw.Draw(sheath)
    sd2.polygon(sheath_pts, fill=(10, 130, 180, 235))
    canvas.alpha_composite(sheath)

    # white-hot lance core: thin long triangle
    lance_pts = [
        pt(-4 * SS, 0),
        pt(length * 0.62, 5 * SS),
        pt(length * 1.12, 0),
        pt(length * 0.62, -5 * SS),
    ]
    glow = new_canvas()
    gd = ImageDraw.Draw(glow)
    gd.polygon(lance_pts, fill=ARC_CYAN)
    glow = glow.filter(ImageFilter.GaussianBlur(8 * SS))
    canvas.alpha_composite(glow)

    core = new_canvas()
    cd = ImageDraw.Draw(core)
    cd.polygon(lance_pts, fill=WHITE_HOT)
    canvas.alpha_composite(core)

    # broken ring (conductor mark) centered near tip, tip piercing through it
    ring_center = pt(length * 0.80, 0)
    ring_r = 30 * SS
    draw_broken_ring(canvas, ring_center, ring_r, n_segs=4, gap_deg=24,
                      seg_width=int(6.5 * SS), rotate=20)

    finalize_skill(canvas, mask, "skill_conduit_spear.png")


def draw_broken_ring(canvas, center, radius, n_segs, gap_deg, seg_width, rotate=0,
                      glow=True, arc_jump=True, color_core=WHITE_HOT, color_glow=ARC_CYAN,
                      copper=True):
    cx, cy = center
    step = 360 / n_segs
    arc_span = step - gap_deg
    bbox = [cx - radius, cy - radius, cx + radius, cy + radius]

    # copper base ring segments
    if copper:
        base = new_canvas()
        bd = ImageDraw.Draw(base)
        for i in range(n_segs):
            a0 = rotate + i * step
            a1 = a0 + arc_span
            bd.arc(bbox, a0, a1, fill=COPPER, width=int(seg_width * 1.4))
        canvas.alpha_composite(base)

    if glow:
        g = new_canvas()
        gd = ImageDraw.Draw(g)
        for i in range(n_segs):
            a0 = rotate + i * step
            a1 = a0 + arc_span
            gd.arc(bbox, a0, a1, fill=color_glow, width=int(seg_width * 2.4))
        g = g.filter(ImageFilter.GaussianBlur(min(int(radius * 0.22), 10 * SS)))
        canvas.alpha_composite(g)

    core = new_canvas()
    cd = ImageDraw.Draw(core)
    for i in range(n_segs):
        a0 = rotate + i * step
        a1 = a0 + arc_span
        cd.arc(bbox, a0, a1, fill=color_core, width=seg_width)
    canvas.alpha_composite(core)

    if arc_jump:
        # small lightning jump across each gap
        jd_layer = new_canvas()
        jd = ImageDraw.Draw(jd_layer)
        for i in range(n_segs):
            mid = math.radians(rotate + i * step + arc_span + gap_deg / 2)
            gx = cx + math.cos(mid) * radius
            gy = cy + math.sin(mid) * radius
            a0 = math.radians(rotate + i * step + arc_span)
            a1 = math.radians(rotate + (i + 1) * step)
            x0 = cx + math.cos(a0) * radius
            y0 = cy + math.sin(a0) * radius
            x1 = cx + math.cos(a1) * radius
            y1 = cy + math.sin(a1) * radius
            pts = jagged_path((x0, y0), (x1, y1), seed=int(gx + gy), segments=2, disp=0.5,
                               min_disp_px=2 * SS)
            jd.line(pts, fill=color_core, width=max(1, int(seg_width * 0.5)))
        canvas.alpha_composite(jd_layer)


# ===========================================================================
# 3. skill_arc_step.png — utility (dash)
# ===========================================================================

def make_arc_step():
    canvas, mask = skill_canvas()
    cx, cy = CANVAS / 2, CANVAS / 2

    # main chevron pointing right
    chevron_cx = CANVAS * 0.62
    chevron_cy = CANVAS * 0.46
    csize = 90 * SS

    def chevron_pts(cx0, cy0, size, w):
        return [
            (cx0 - size * 0.5, cy0 - size * 0.55),
            (cx0 + size * 0.5, cy0),
            (cx0 - size * 0.5, cy0 + size * 0.55),
        ]

    glow = new_canvas()
    gd = ImageDraw.Draw(glow)
    gd.line(chevron_pts(chevron_cx, chevron_cy, csize, 0), fill=ARC_CYAN,
             width=16 * SS, joint="curve")
    glow = glow.filter(ImageFilter.GaussianBlur(9 * SS))
    canvas.alpha_composite(glow)

    core = new_canvas()
    cd = ImageDraw.Draw(core)
    cd.line(chevron_pts(chevron_cx, chevron_cy, csize, 0), fill=WHITE_HOT,
             width=7 * SS, joint="curve")
    canvas.alpha_composite(core)

    # fading afterimage chevrons trailing left
    for i, (offset, alpha) in enumerate([(0.34, 140), (0.62, 80), (0.88, 40)]):
        af = new_canvas()
        ad = ImageDraw.Draw(af)
        acx = chevron_cx - csize * offset
        pts = chevron_pts(acx, chevron_cy, csize * (1 - offset * 0.15), 0)
        ad.line(pts, fill=(ARC_CYAN[0], ARC_CYAN[1], ARC_CYAN[2], alpha),
                 width=6 * SS, joint="curve")
        af = af.filter(ImageFilter.GaussianBlur(3 * SS))
        canvas.alpha_composite(af)

    # zigzag ground line beneath
    gy = CANVAS * 0.74
    ground_pts = jagged_path((CANVAS * 0.14, gy), (CANVAS * 0.88, gy - 6 * SS),
                              seed=7, segments=4, disp=0.12, min_disp_px=4 * SS)
    gl_glow = new_canvas()
    gld = ImageDraw.Draw(gl_glow)
    gld.line(ground_pts, fill=ARC_CYAN, width=int(9 * SS), joint="curve")
    gl_glow = gl_glow.filter(ImageFilter.GaussianBlur(5 * SS))
    canvas.alpha_composite(gl_glow)
    gl_core = new_canvas()
    glcd = ImageDraw.Draw(gl_core)
    glcd.line(ground_pts, fill=WHITE_HOT, width=int(3.6 * SS), joint="curve")
    canvas.alpha_composite(gl_core)

    finalize_skill(canvas, mask, "skill_arc_step.png")


# ===========================================================================
# 4. skill_open_circuit.png — special (halo crown)
# ===========================================================================

def make_open_circuit():
    canvas, mask = skill_canvas()
    cx, cy = CANVAS / 2, CANVAS * 0.52

    center = (cx, cy)
    r = int(CANVAS * 0.27)
    n_segs = 4
    gap_deg = 26
    rotate = 10
    step = 360 / n_segs
    arc_span = step - gap_deg
    seg_width = int(7 * SS)

    # spread each segment outward from center along its own bisector so the
    # ring visibly reads as "opening" apart, not just gapped
    spread = r * 0.10
    seg_centers = []
    seg_angles = []
    for i in range(n_segs):
        a0 = rotate + i * step
        a1 = a0 + arc_span
        mid = math.radians(a0 + arc_span / 2)
        sc = (center[0] + math.cos(mid) * spread, center[1] + math.sin(mid) * spread)
        seg_centers.append(sc)
        seg_angles.append((a0, a1))

    # aged copper body (no white overlay — this IS the visible color)
    body = new_canvas()
    bd = ImageDraw.Draw(body)
    for sc, (a0, a1) in zip(seg_centers, seg_angles):
        bbox = [sc[0] - r, sc[1] - r, sc[0] + r, sc[1] + r]
        bd.arc(bbox, a0, a1, fill=COPPER, width=seg_width)
    canvas.alpha_composite(body)

    # lighter copper edge highlight along the outer rim of each segment
    highlight = new_canvas()
    hld = ImageDraw.Draw(highlight)
    hl_color = (201, 133, 82, 255)  # #C98552
    hl_r_offset = seg_width * 0.34
    for sc, (a0, a1) in zip(seg_centers, seg_angles):
        hr = r + hl_r_offset
        bbox = [sc[0] - hr, sc[1] - hr, sc[0] + hr, sc[1] + hr]
        hld.arc(bbox, a0, a1, fill=hl_color, width=max(1, int(seg_width * 0.3)))
    canvas.alpha_composite(highlight)

    # cyan glow traced tightly along each arc (rim light only, not a wash)
    arc_glow = new_canvas()
    agd = ImageDraw.Draw(arc_glow)
    for sc, (a0, a1) in zip(seg_centers, seg_angles):
        bbox = [sc[0] - r, sc[1] - r, sc[0] + r, sc[1] + r]
        agd.arc(bbox, a0, a1, fill=(ARC_CYAN[0], ARC_CYAN[1], ARC_CYAN[2], 130),
                 width=int(seg_width * 1.2))
    arc_glow = arc_glow.filter(ImageFilter.GaussianBlur(int(3 * SS)))
    canvas.alpha_composite(arc_glow)

    # bright cyan lightning jumping across each opened gap
    jump_glow = new_canvas()
    jgd = ImageDraw.Draw(jump_glow)
    jump_core = new_canvas()
    jcd = ImageDraw.Draw(jump_core)
    for i in range(n_segs):
        a0, a1 = seg_angles[i]
        sc = seg_centers[i]
        end_ang = math.radians(a1)
        x0 = sc[0] + math.cos(end_ang) * r
        y0 = sc[1] + math.sin(end_ang) * r

        j = (i + 1) % n_segs
        a0n, _ = seg_angles[j]
        scn = seg_centers[j]
        start_ang = math.radians(a0n)
        x1 = scn[0] + math.cos(start_ang) * r
        y1 = scn[1] + math.sin(start_ang) * r

        pts = jagged_path((x0, y0), (x1, y1), seed=300 + i, segments=2, disp=0.45,
                           min_disp_px=2 * SS)
        jgd.line(pts, fill=ARC_CYAN, width=int(5 * SS), joint="curve")
        jcd.line(pts, fill=WHITE_HOT, width=int(2 * SS), joint="curve")
    jump_glow = jump_glow.filter(ImageFilter.GaussianBlur(int(4 * SS)))
    canvas.alpha_composite(jump_glow)
    canvas.alpha_composite(jump_core)

    # radiating strikes outward, anchored at each segment's (shifted) outer
    # midpoint (crown spikes) so the silhouette stays a clean ring even downscaled
    rng = random.Random(4)
    strike_layer_glow = new_canvas()
    slg = ImageDraw.Draw(strike_layer_glow)
    strike_layer_core = new_canvas()
    slc = ImageDraw.Draw(strike_layer_core)
    for i, (sc, (a0, a1)) in enumerate(zip(seg_centers, seg_angles)):
        mid_ang = math.radians((a0 + a1) / 2)
        x0 = sc[0] + math.cos(mid_ang) * (r + 2 * SS)
        y0 = sc[1] + math.sin(mid_ang) * (r + 2 * SS)
        ln = rng.uniform(0.16, 0.22) * CANVAS
        x1 = sc[0] + math.cos(mid_ang) * (r + 2 * SS + ln)
        y1 = sc[1] + math.sin(mid_ang) * (r + 2 * SS + ln)
        pts = jagged_path((x0, y0), (x1, y1), seed=100 + i, segments=3, disp=0.28)
        slg.line(pts, fill=ARC_CYAN, width=int(6 * SS), joint="curve")
        slc.line(pts, fill=WHITE_HOT, width=int(2.4 * SS), joint="curve")

    strike_layer_glow = strike_layer_glow.filter(ImageFilter.GaussianBlur(6 * SS))
    canvas.alpha_composite(strike_layer_glow)
    canvas.alpha_composite(strike_layer_core)

    finalize_skill(canvas, mask, "skill_open_circuit.png")


# ===========================================================================
# 5. passive_discharge.png — passive core orb
# ===========================================================================

def make_discharge():
    canvas, mask = skill_canvas()
    center = (CANVAS / 2, CANVAS / 2)
    core_r = int(CANVAS * 0.14)
    ring_r = int(CANVAS * 0.30)

    # outer meter ring, fully lit
    bbox = [center[0] - ring_r, center[1] - ring_r, center[0] + ring_r, center[1] + ring_r]
    ring_glow = new_canvas()
    rgd = ImageDraw.Draw(ring_glow)
    rgd.ellipse(bbox, outline=ARC_CYAN, width=9 * SS)
    ring_glow = ring_glow.filter(ImageFilter.GaussianBlur(6 * SS))
    canvas.alpha_composite(ring_glow)

    ring_core = new_canvas()
    rcd = ImageDraw.Draw(ring_core)
    rcd.ellipse(bbox, outline=IVORY, width=3 * SS)
    canvas.alpha_composite(ring_core)

    # tick marks around ring
    ticks = new_canvas()
    td = ImageDraw.Draw(ticks)
    for i in range(24):
        ang = math.radians(i * 15)
        x0 = center[0] + math.cos(ang) * (ring_r - 6 * SS)
        y0 = center[1] + math.sin(ang) * (ring_r - 6 * SS)
        x1 = center[0] + math.cos(ang) * (ring_r + 6 * SS)
        y1 = center[1] + math.sin(ang) * (ring_r + 6 * SS)
        td.line([x0, y0, x1, y1], fill=OUTER_CYAN, width=int(2 * SS))
    canvas.alpha_composite(ticks)

    # radiating strikes (6)
    rng = random.Random(5)
    sg = new_canvas()
    sgd = ImageDraw.Draw(sg)
    sc = new_canvas()
    scd = ImageDraw.Draw(sc)
    for i in range(6):
        ang = (i / 6) * math.pi * 2 + rng.uniform(-0.15, 0.15)
        x0 = center[0] + math.cos(ang) * (ring_r + 8 * SS)
        y0 = center[1] + math.sin(ang) * (ring_r + 8 * SS)
        ln = rng.uniform(0.14, 0.20) * CANVAS
        x1 = center[0] + math.cos(ang) * (ring_r + 8 * SS + ln)
        y1 = center[1] + math.sin(ang) * (ring_r + 8 * SS + ln)
        pts = jagged_path((x0, y0), (x1, y1), seed=200 + i, segments=2, disp=0.35)
        sgd.line(pts, fill=ARC_CYAN, width=int(6 * SS), joint="curve")
        scd.line(pts, fill=WHITE_HOT, width=int(2.4 * SS), joint="curve")
    sg = sg.filter(ImageFilter.GaussianBlur(6 * SS))
    canvas.alpha_composite(sg)
    canvas.alpha_composite(sc)

    # core orb: white-hot center, cyan glow
    orb_glow = new_canvas()
    ogd = ImageDraw.Draw(orb_glow)
    ogd.ellipse([center[0] - core_r * 1.6, center[1] - core_r * 1.6,
                 center[0] + core_r * 1.6, center[1] + core_r * 1.6], fill=ARC_CYAN)
    orb_glow = orb_glow.filter(ImageFilter.GaussianBlur(10 * SS))
    canvas.alpha_composite(orb_glow)

    orb_mid = new_canvas()
    omd = ImageDraw.Draw(orb_mid)
    omd.ellipse([center[0] - core_r * 1.1, center[1] - core_r * 1.1,
                 center[0] + core_r * 1.1, center[1] + core_r * 1.1], fill=OUTER_CYAN)
    canvas.alpha_composite(orb_mid)

    orb_core = new_canvas()
    ocd = ImageDraw.Draw(orb_core)
    ocd.ellipse([center[0] - core_r * 0.7, center[1] - core_r * 0.7,
                 center[0] + core_r * 0.7, center[1] + core_r * 0.7], fill=WHITE_HOT)
    canvas.alpha_composite(orb_core)

    finalize_skill(canvas, mask, "passive_discharge.png")


# ===========================================================================
# Buff icons
# ===========================================================================

def make_buff_conductor_mark():
    canvas = buff_canvas()
    center = (CANVAS / 2, CANVAS / 2)
    r = int(CANVAS * 0.32)

    def outline_ring(d, color, extra):
        bbox = [center[0] - r, center[1] - r, center[0] + r, center[1] + r]
        step = 90
        gap = 24
        for i in range(4):
            a0 = 10 + i * step
            a1 = a0 + (step - gap)
            d.arc(bbox, a0, a1, fill=color, width=int(14 * SS) + extra)

    layer = white_symbol_with_outline(outline_ring, outline_width_extra=8 * SS)
    canvas.alpha_composite(layer)

    # diamond in center
    dsize = int(CANVAS * 0.13)
    diamond = [
        (center[0], center[1] - dsize),
        (center[0] + dsize, center[1]),
        (center[0], center[1] + dsize),
        (center[0] - dsize, center[1]),
    ]
    outline = new_canvas()
    od = ImageDraw.Draw(outline)
    od.polygon(diamond, fill=(0, 0, 0, 130))
    canvas.alpha_composite(outline.filter(ImageFilter.GaussianBlur(3 * SS)))
    fill = new_canvas()
    fd = ImageDraw.Draw(fill)
    fd.polygon(diamond, fill=(255, 255, 255, 255))
    canvas.alpha_composite(fill)

    finalize_buff(canvas, "buff_conductor_mark.png")


def make_buff_open_circuit():
    canvas = buff_canvas()
    center = (CANVAS / 2, CANVAS / 2)
    r = int(CANVAS * 0.34)

    def outline_ring(d, color, extra):
        bbox = [center[0] - r, center[1] - r, center[0] + r, center[1] + r]
        step = 90
        gap = 26
        for i in range(4):
            a0 = 10 + i * step
            a1 = a0 + (step - gap)
            d.arc(bbox, a0, a1, fill=color, width=int(16 * SS) + extra)

    layer = white_symbol_with_outline(outline_ring, outline_width_extra=8 * SS)
    canvas.alpha_composite(layer)

    # small jump ticks in gaps for clarity at small size
    jump = new_canvas()
    jd = ImageDraw.Draw(jump)
    step = 90
    gap = 26
    for i in range(4):
        mid = math.radians(10 + i * step + (step - gap) + gap / 2)
        gx = center[0] + math.cos(mid) * r
        gy = center[1] + math.sin(mid) * r
        jd.ellipse([gx - 5 * SS, gy - 5 * SS, gx + 5 * SS, gy + 5 * SS],
                    fill=(255, 255, 255, 255))
    canvas.alpha_composite(jump)

    finalize_buff(canvas, "buff_open_circuit.png")


def make_buff_discharge_charge():
    canvas = buff_canvas()
    center = (CANVAS / 2, CANVAS / 2)
    r = int(CANVAS * 0.34)

    def outline_ring(d, color, extra):
        bbox = [center[0] - r, center[1] - r, center[0] + r, center[1] + r]
        d.ellipse(bbox, outline=color, width=int(12 * SS) + extra)

    layer = white_symbol_with_outline(outline_ring, outline_width_extra=8 * SS)
    canvas.alpha_composite(layer)

    # bolt inside
    bolt = [
        (center[0] + CANVAS * 0.05, center[1] - CANVAS * 0.16),
        (center[0] - CANVAS * 0.07, center[1] + CANVAS * 0.02),
        (center[0] + CANVAS * 0.005, center[1] + CANVAS * 0.02),
        (center[0] - CANVAS * 0.05, center[1] + CANVAS * 0.16),
        (center[0] + CANVAS * 0.08, center[1] - CANVAS * 0.02),
        (center[0], center[1] - CANVAS * 0.02),
    ]
    outline = new_canvas()
    od = ImageDraw.Draw(outline)
    od.polygon(bolt, fill=(0, 0, 0, 130))
    canvas.alpha_composite(outline.filter(ImageFilter.GaussianBlur(3 * SS)))
    fill = new_canvas()
    fd = ImageDraw.Draw(fill)
    fd.polygon(bolt, fill=(255, 255, 255, 255))
    canvas.alpha_composite(fill)

    finalize_buff(canvas, "buff_discharge_charge.png")


def make_buff_shocked():
    """Shocked debuff: a big diagonal bolt striking a burst of spark ticks. Deliberately not
    a ring (Conductor Mark, Open Circuit, Storm charge are all ring based) so the three
    Storm/spear debuffs read differently in the buff bar."""
    canvas = buff_canvas()
    cx, cy = CANVAS / 2, CANVAS / 2

    bolt = [
        (cx + CANVAS * 0.16, cy - CANVAS * 0.40),
        (cx - CANVAS * 0.18, cy + CANVAS * 0.02),
        (cx - CANVAS * 0.01, cy + CANVAS * 0.02),
        (cx - CANVAS * 0.13, cy + CANVAS * 0.30),
        (cx + CANVAS * 0.22, cy - CANVAS * 0.10),
        (cx + CANVAS * 0.05, cy - CANVAS * 0.10),
    ]
    tip = (cx - CANVAS * 0.13, cy + CANVAS * 0.30)

    def sparks(d, color, extra):
        # four short ticks fanning from the strike point
        for ang, ln in [(-160, 0.15), (-115, 0.13), (-25, 0.15), (20, 0.13)]:
            a = math.radians(ang)
            x0 = tip[0] + math.cos(a) * CANVAS * 0.03
            y0 = tip[1] + math.sin(a) * CANVAS * 0.03
            x1 = tip[0] + math.cos(a) * CANVAS * ln
            y1 = tip[1] + math.sin(a) * CANVAS * ln
            d.line([x0, y0, x1, y1], fill=color, width=int(9 * SS) + extra)

    def bolt_shape(d, color, extra):
        if extra:
            d.polygon(bolt, fill=color)
            d.line(bolt + [bolt[0]], fill=color, width=extra, joint="curve")
        else:
            d.polygon(bolt, fill=color)

    layer = white_symbol_with_outline(sparks, outline_width_extra=8 * SS)
    canvas.alpha_composite(layer)
    layer = white_symbol_with_outline(bolt_shape, outline_width_extra=10 * SS)
    canvas.alpha_composite(layer)
    finalize_buff(canvas, "buff_shocked.png")


# ===========================================================================
# Portrait
# ===========================================================================

def make_portrait():
    canvas, mask = skill_canvas()
    cx = CANVAS / 2

    # helmet (oval), featureless, ivory — shrunk to ~40% of tile height
    helmet_w = CANVAS * 0.16
    helmet_h = CANVAS * 0.20
    helmet_cy = CANVAS * 0.38
    helmet_bbox = [cx - helmet_w, helmet_cy - helmet_h, cx + helmet_w, helmet_cy + helmet_h]

    # halo behind + above head (4-segment copper ring, cyan-lit gaps) — sized and
    # placed so it reads clearly around the smaller head instead of being hidden
    halo_r = int(CANVAS * 0.29)
    halo_center = (cx, helmet_cy - CANVAS * 0.02)
    draw_broken_ring(canvas, halo_center, halo_r, n_segs=4, gap_deg=24,
                      seg_width=int(9 * SS), rotate=15, arc_jump=True, copper=True)

    # shoulders (narrower angular pads, scaled to the smaller head)
    shoulder_y = CANVAS * 0.68
    shoulder_pts_l = [
        (cx - CANVAS * 0.07, CANVAS * 0.60),
        (cx - CANVAS * 0.30, shoulder_y - CANVAS * 0.02),
        (cx - CANVAS * 0.27, shoulder_y + CANVAS * 0.12),
        (cx - CANVAS * 0.10, shoulder_y + CANVAS * 0.05),
    ]
    shoulder_pts_r = [(cx + (cx - x), y) for x, y in shoulder_pts_l]

    sh_layer = new_canvas()
    shd = ImageDraw.Draw(sh_layer)
    for pts in (shoulder_pts_l, shoulder_pts_r):
        shd.polygon(pts, fill=IVORY)
        shd.line(pts + [pts[0]], fill=(150, 145, 130, 255), width=int(2 * SS))
    canvas.alpha_composite(sh_layer)

    # subtle cyan rim light on shoulder edges
    rim = new_canvas()
    rd = ImageDraw.Draw(rim)
    rd.line(shoulder_pts_l[:2], fill=ARC_CYAN, width=int(2.5 * SS))
    rd.line(shoulder_pts_r[:2], fill=ARC_CYAN, width=int(2.5 * SS))
    canvas.alpha_composite(rim.filter(ImageFilter.GaussianBlur(int(2 * SS))))

    # neck/torso hint
    torso = new_canvas()
    trd = ImageDraw.Draw(torso)
    trd.polygon([
        (cx - CANVAS * 0.09, CANVAS * 0.58),
        (cx + CANVAS * 0.09, CANVAS * 0.58),
        (cx + CANVAS * 0.13, shoulder_y),
        (cx - CANVAS * 0.13, shoulder_y),
    ], fill=IVORY)
    canvas.alpha_composite(torso)

    # helmet
    helm_layer = new_canvas()
    hd = ImageDraw.Draw(helm_layer)
    hd.ellipse(helmet_bbox, fill=IVORY)
    canvas.alpha_composite(helm_layer)

    # soft shading on helmet (side shadow)
    shade = new_canvas()
    shdw = ImageDraw.Draw(shade)
    shdw.ellipse([cx + helmet_w * 0.1, helmet_cy - helmet_h, cx + helmet_w * 1.3,
                  helmet_cy + helmet_h], fill=(20, 30, 45, 90))
    shade_masked = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    shade_masked.paste(shade, (0, 0), helm_layer.split()[3])
    canvas.alpha_composite(shade_masked.filter(ImageFilter.GaussianBlur(6 * SS)))

    # chest core orb
    core_r = int(CANVAS * 0.06)
    core_center = (cx, shoulder_y - CANVAS * 0.02)
    orb_glow = new_canvas()
    ogd = ImageDraw.Draw(orb_glow)
    ogd.ellipse([core_center[0] - core_r * 2, core_center[1] - core_r * 2,
                 core_center[0] + core_r * 2, core_center[1] + core_r * 2], fill=ARC_CYAN)
    canvas.alpha_composite(orb_glow.filter(ImageFilter.GaussianBlur(8 * SS)))
    orb_core = new_canvas()
    ocd = ImageDraw.Draw(orb_core)
    ocd.ellipse([core_center[0] - core_r, core_center[1] - core_r,
                 core_center[0] + core_r, core_center[1] + core_r], fill=WHITE_HOT)
    canvas.alpha_composite(orb_core)

    # thin vertical cyan line on helmet (visor line), with glow
    line_glow = new_canvas()
    lgd = ImageDraw.Draw(line_glow)
    lgd.line([cx, helmet_cy - helmet_h * 0.55, cx, helmet_cy + helmet_h * 0.55],
              fill=ARC_CYAN, width=int(5 * SS))
    canvas.alpha_composite(line_glow.filter(ImageFilter.GaussianBlur(4 * SS)))
    line_core = new_canvas()
    lcd = ImageDraw.Draw(line_core)
    lcd.line([cx, helmet_cy - helmet_h * 0.55, cx, helmet_cy + helmet_h * 0.55],
              fill=WHITE_HOT, width=int(1.8 * SS))
    canvas.alpha_composite(line_core)

    finalize_skill(canvas, mask, "portrait.png")



# ===========================================================================
# 4b. skill_gaze.png — alternate special (halo sent out, beam down onto the ground)
# ===========================================================================

def make_gaze():
    canvas, mask = skill_canvas()
    # The halo hangs upper left, seen at an angle (an ellipse of four copper segments).
    hx, hy = CANVAS * 0.33, CANVAS * 0.30
    rx, ry = CANVAS * 0.22, CANVAS * 0.10
    seg_w = int(9 * SS)
    segs = [(i * 90 + 12, i * 90 + 78) for i in range(4)]
    bbox = [hx - rx, hy - ry, hx + rx, hy + ry]
    body = new_canvas(); bd = ImageDraw.Draw(body)
    for a0, a1 in segs: bd.arc(bbox, a0, a1, fill=COPPER, width=seg_w)
    canvas.alpha_composite(body)
    rim = new_canvas(); rd = ImageDraw.Draw(rim)
    for a0, a1 in segs: rd.arc(bbox, a0, a1, fill=(ARC_CYAN[0], ARC_CYAN[1], ARC_CYAN[2], 140), width=int(seg_w * 1.3))
    canvas.alpha_composite(rim.filter(ImageFilter.GaussianBlur(int(3 * SS))))

    # The beam: from the halo's centre down to the impact, a wide cyan glow with a white-hot core.
    ix, iy = CANVAS * 0.74, CANVAS * 0.80
    def beam(d, color, width):
        d.line([hx, hy, ix, iy], fill=color, width=width)
    glow = new_canvas(); beam(ImageDraw.Draw(glow), ARC_CYAN, int(30 * SS))
    canvas.alpha_composite(glow.filter(ImageFilter.GaussianBlur(12 * SS)))
    mid = new_canvas(); beam(ImageDraw.Draw(mid), (ARC_CYAN[0], ARC_CYAN[1], ARC_CYAN[2], 230), int(14 * SS))
    canvas.alpha_composite(mid.filter(ImageFilter.GaussianBlur(2 * SS)))
    core = new_canvas(); beam(ImageDraw.Draw(core), WHITE_HOT, int(6 * SS))
    canvas.alpha_composite(core)
    # A lightning thread wound around the beam so it reads as lightning, not a laser.
    thread = jagged_path((hx, hy), (ix, iy), seed=71, segments=5, disp=0.10, min_disp_px=5 * SS)
    composite_glow_core(canvas, lambda d, c, w: draw_polyline(d, thread, c, w), glow_width=6 * SS, core_width=2 * SS, blur=4 * SS)

    # Impact: a bright splash with two forks running across the ground.
    splash = new_canvas(); sd = ImageDraw.Draw(splash)
    sd.ellipse([ix - 26 * SS, iy - 12 * SS, ix + 26 * SS, iy + 12 * SS], fill=ARC_CYAN)
    canvas.alpha_composite(splash.filter(ImageFilter.GaussianBlur(9 * SS)))
    hot = new_canvas(); hd = ImageDraw.Draw(hot)
    hd.ellipse([ix - 10 * SS, iy - 5 * SS, ix + 10 * SS, iy + 5 * SS], fill=WHITE_HOT)
    canvas.alpha_composite(hot.filter(ImageFilter.GaussianBlur(2 * SS)))
    for k, end in enumerate([(CANVAS * 0.40, CANVAS * 0.86), (CANVAS * 0.90, CANVAS * 0.62)]):
        pts = jagged_path((ix, iy), end, seed=90 + k, segments=3, disp=0.25)
        composite_glow_core(canvas, lambda d, c, w, pts=pts: draw_polyline(d, pts, c, w), glow_width=7 * SS, core_width=2.4 * SS, blur=5 * SS)
    sparks = new_canvas(); spark_burst(ImageDraw.Draw(sparks), (ix, iy), WHITE_HOT, int(2 * SS), n=6, length=(18, 34), seed=5)
    canvas.alpha_composite(sparks)

    finalize_skill(canvas, mask, "skill_gaze.png")

# ===========================================================================
# main
# ===========================================================================

if __name__ == "__main__":
    import os
    import sys
    # Usage: make_icons.py [--out DIR] [--only buff_shocked]
    # --only regenerates a single buff icon without touching the other files.
    argv = sys.argv[1:]
    if "--out" in argv:
        OUT_DIR = argv[argv.index("--out") + 1]
    os.makedirs(OUT_DIR, exist_ok=True)
    if "--only" in argv:
        {"buff_shocked": make_buff_shocked, "skill_gaze": make_gaze}[argv[argv.index("--only") + 1]]()
        sys.exit(0)
    make_arc_bolt()
    make_conduit_spear()
    make_arc_step()
    make_open_circuit()
    make_gaze()
    make_discharge()
    make_buff_conductor_mark()
    make_buff_open_circuit()
    make_buff_discharge_charge()
    make_buff_shocked()
    make_portrait()
    print("done")
