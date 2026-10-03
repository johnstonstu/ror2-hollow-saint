#!/usr/bin/env python3
"""
Hollow Saint skill icons, v2 (v0.9.11): bolder versions of the skill icons so they read at the
64 px size of the character select and loadout screens. Same palette and helpers as
make_icons.py; run from this folder:  python make_icons_v2.py --out ../../HollowSaintMod/Icons
"""
import math
import os
import sys
import random
from PIL import Image, ImageDraw, ImageFilter

import make_icons as base
from make_icons import (SS, CANVAS, ARC_CYAN, OUTER_CYAN, WHITE_HOT, COPPER, new_canvas,
                        jagged_path, draw_polyline, spark_burst, composite_glow_core,
                        skill_canvas, finalize_skill)


def bolt_glow(canvas, pts, glow, core, blur):
    composite_glow_core(canvas, lambda d, c, w: draw_polyline(d, pts, c, w),
                        glow_width=glow * SS, core_width=core * SS, blur=blur * SS)


def dots(canvas, points):
    g = new_canvas(); gd = ImageDraw.Draw(g)
    for (x, y), r in points:
        rr = r * 2.2 * SS
        gd.ellipse([x - rr, y - rr, x + rr, y + rr], fill=ARC_CYAN)
    canvas.alpha_composite(g.filter(ImageFilter.GaussianBlur(10 * SS)))
    c = new_canvas(); cd = ImageDraw.Draw(c)
    for (x, y), r in points:
        rr = r * SS
        cd.ellipse([x - rr, y - rr, x + rr, y + rr], fill=WHITE_HOT)
    canvas.alpha_composite(c)


def make_arc_bolt():
    canvas, mask = skill_canvas()
    start = (CANVAS * 0.14, CANVAS * 0.88)
    impact = (CANVAS * 0.50, CANVAS * 0.50)
    a = (CANVAS * 0.84, CANVAS * 0.18)
    b = (CANVAS * 0.84, CANVAS * 0.62)
    bolt_glow(canvas, jagged_path(start, impact, seed=11, segments=4, disp=0.24), 56, 17, 16)
    for seed, end in ((22, a), (33, b)):
        bolt_glow(canvas, jagged_path(impact, end, seed=seed, segments=3, disp=0.28), 30, 8, 10)
    dots(canvas, [(start, 9), (impact, 13), (a, 10), (b, 10)])
    sp = new_canvas(); sd = ImageDraw.Draw(sp)
    spark_burst(sd, a, WHITE_HOT, int(4 * SS), n=6, length=(14, 26), seed=99)
    spark_burst(sd, b, WHITE_HOT, int(4 * SS), n=6, length=(14, 24), seed=77)
    canvas.alpha_composite(sp)
    finalize_skill(canvas, mask, "skill_arc_bolt.png")


def make_stormspear():
    """A thick lance of lightning, its tip lodged in a burst (the spear sticks, then bursts)."""
    canvas, mask = skill_canvas()
    tail = (CANVAS * 0.12, CANVAS * 0.90)
    tip = (CANVAS * 0.70, CANVAS * 0.32)
    burst = (CANVAS * 0.73, CANVAS * 0.29)
    dx, dy = tip[0] - tail[0], tip[1] - tail[1]
    L = math.hypot(dx, dy); ux, uy = dx / L, dy / L; px, py = -uy, ux
    pt = lambda a, p: (tail[0] + ux * a + px * p, tail[1] + uy * a + py * p)

    # The burst first, so the lance reads in front of it.
    g = new_canvas(); gd = ImageDraw.Draw(g)
    r = 70 * SS
    gd.ellipse([burst[0] - r, burst[1] - r, burst[0] + r, burst[1] + r], fill=OUTER_CYAN)
    canvas.alpha_composite(g.filter(ImageFilter.GaussianBlur(26 * SS)))
    for k in range(7):
        ang = math.radians(-160 + k * 47)
        end = (burst[0] + math.cos(ang) * 60 * SS, burst[1] + math.sin(ang) * 60 * SS)
        bolt_glow(canvas, jagged_path(burst, end, seed=200 + k, segments=2, disp=0.35), 18, 5, 6)

    sheath = [pt(0, 0), pt(L * 0.55, 26 * SS), pt(L * 0.92, 10 * SS), pt(L * 1.04, 0), pt(L * 0.92, -10 * SS), pt(L * 0.55, -26 * SS)]
    s = new_canvas(); ImageDraw.Draw(s).polygon(sheath, fill=OUTER_CYAN)
    canvas.alpha_composite(s.filter(ImageFilter.GaussianBlur(10 * SS)))
    s2 = new_canvas(); ImageDraw.Draw(s2).polygon(sheath, fill=(10, 140, 190, 240))
    canvas.alpha_composite(s2)
    lance = [pt(-4 * SS, 0), pt(L * 0.62, 9 * SS), pt(L * 1.08, 0), pt(L * 0.62, -9 * SS)]
    g2 = new_canvas(); ImageDraw.Draw(g2).polygon(lance, fill=ARC_CYAN)
    canvas.alpha_composite(g2.filter(ImageFilter.GaussianBlur(9 * SS)))
    c = new_canvas(); ImageDraw.Draw(c).polygon(lance, fill=WHITE_HOT)
    canvas.alpha_composite(c)
    dots(canvas, [(burst, 15)])
    finalize_skill(canvas, mask, "skill_conduit_spear.png")


def make_arc_step():
    """A solid arrowhead blinking right out of three fading copies of itself, over a spark line."""
    canvas, mask = skill_canvas()
    cy = CANVAS * 0.47

    def head(cx, size):
        return [(cx - size * 0.55, cy - size * 0.62), (cx + size * 0.55, cy), (cx - size * 0.55, cy + size * 0.62),
                (cx - size * 0.18, cy)]

    size = 112 * SS
    for offset, alpha in ((0.95, 50), (0.66, 95), (0.36, 150)):
        gh = new_canvas()
        ImageDraw.Draw(gh).polygon(head(CANVAS * 0.64 - size * offset, size * (1 - offset * 0.18)),
                                   fill=(ARC_CYAN[0], ARC_CYAN[1], ARC_CYAN[2], alpha))
        canvas.alpha_composite(gh.filter(ImageFilter.GaussianBlur(2 * SS)))
    g = new_canvas(); ImageDraw.Draw(g).polygon(head(CANVAS * 0.64, size), fill=ARC_CYAN)
    canvas.alpha_composite(g.filter(ImageFilter.GaussianBlur(14 * SS)))
    c = new_canvas(); ImageDraw.Draw(c).polygon(head(CANVAS * 0.64, size), fill=WHITE_HOT)
    canvas.alpha_composite(c)
    gy = CANVAS * 0.80
    bolt_glow(canvas, jagged_path((CANVAS * 0.12, gy), (CANVAS * 0.88, gy - 8 * SS), seed=7, segments=4, disp=0.1, min_disp_px=5 * SS), 22, 6, 8)
    finalize_skill(canvas, mask, "skill_arc_step.png")


def make_gaze():
    canvas, mask = skill_canvas()
    hx, hy = CANVAS * 0.33, CANVAS * 0.27
    rx, ry = CANVAS * 0.25, CANVAS * 0.115
    seg_w = int(15 * SS)
    segs = [(i * 90 + 12, i * 90 + 78) for i in range(4)]
    bbox = [hx - rx, hy - ry, hx + rx, hy + ry]
    body = new_canvas(); bd = ImageDraw.Draw(body)
    for a0, a1 in segs: bd.arc(bbox, a0, a1, fill=COPPER, width=seg_w)
    canvas.alpha_composite(body)
    rim = new_canvas(); rd = ImageDraw.Draw(rim)
    for a0, a1 in segs: rd.arc(bbox, a0, a1, fill=(ARC_CYAN[0], ARC_CYAN[1], ARC_CYAN[2], 160), width=int(seg_w * 1.3))
    canvas.alpha_composite(rim.filter(ImageFilter.GaussianBlur(4 * SS)))
    ix, iy = CANVAS * 0.74, CANVAS * 0.80

    def beam(d, color, width):
        d.line([hx, hy, ix, iy], fill=color, width=width)
    g = new_canvas(); beam(ImageDraw.Draw(g), ARC_CYAN, int(52 * SS))
    canvas.alpha_composite(g.filter(ImageFilter.GaussianBlur(16 * SS)))
    m = new_canvas(); beam(ImageDraw.Draw(m), (ARC_CYAN[0], ARC_CYAN[1], ARC_CYAN[2], 235), int(26 * SS))
    canvas.alpha_composite(m.filter(ImageFilter.GaussianBlur(3 * SS)))
    c = new_canvas(); beam(ImageDraw.Draw(c), WHITE_HOT, int(12 * SS))
    canvas.alpha_composite(c)
    for k, end in enumerate([(CANVAS * 0.38, CANVAS * 0.87), (CANVAS * 0.92, CANVAS * 0.60)]):
        bolt_glow(canvas, jagged_path((ix, iy), end, seed=90 + k, segments=3, disp=0.25), 20, 6, 7)
    dots(canvas, [((ix, iy), 16)])
    finalize_skill(canvas, mask, "skill_gaze.png")


def make_open_circuit():
    """The halo opened into four separated, thick copper segments with lightning across the gaps
    and crown spikes outward."""
    canvas, mask = skill_canvas()
    cx, cy = CANVAS / 2, CANVAS * 0.52
    r = int(CANVAS * 0.27)
    seg_w = int(15 * SS)
    step, gap, rot = 90, 26, 10
    spread = r * 0.12
    segs = []
    for i in range(4):
        a0 = rot + i * step; a1 = a0 + step - gap
        mid = math.radians(a0 + (step - gap) / 2)
        segs.append(((cx + math.cos(mid) * spread, cy + math.sin(mid) * spread), a0, a1))
    rim = new_canvas(); rd = ImageDraw.Draw(rim)
    for (sx, sy), a0, a1 in segs: rd.arc([sx - r, sy - r, sx + r, sy + r], a0, a1, fill=ARC_CYAN, width=int(seg_w * 1.7))
    canvas.alpha_composite(rim.filter(ImageFilter.GaussianBlur(8 * SS)))
    body = new_canvas(); bd = ImageDraw.Draw(body)
    for (sx, sy), a0, a1 in segs: bd.arc([sx - r, sy - r, sx + r, sy + r], a0, a1, fill=COPPER, width=seg_w)
    canvas.alpha_composite(body)
    hl = new_canvas(); hd = ImageDraw.Draw(hl)
    for (sx, sy), a0, a1 in segs:
        rr = r + seg_w * 0.3
        hd.arc([sx - rr, sy - rr, sx + rr, sy + rr], a0, a1, fill=(201, 133, 82, 255), width=int(seg_w * 0.3))
    canvas.alpha_composite(hl)
    rng = random.Random(4)
    for i, ((sx, sy), a0, a1) in enumerate(segs):
        e = math.radians(a1); nxt = segs[(i + 1) % 4]; s = math.radians(nxt[1])
        p0 = (sx + math.cos(e) * r, sy + math.sin(e) * r)
        p1 = (nxt[0][0] + math.cos(s) * r, nxt[0][1] + math.sin(s) * r)
        bolt_glow(canvas, jagged_path(p0, p1, seed=300 + i, segments=2, disp=0.45, min_disp_px=2 * SS), 16, 5, 5)
        mid = math.radians((a0 + a1) / 2)
        q0 = (sx + math.cos(mid) * (r + 6 * SS), sy + math.sin(mid) * (r + 6 * SS))
        ln = rng.uniform(0.15, 0.2) * CANVAS
        q1 = (q0[0] + math.cos(mid) * ln, q0[1] + math.sin(mid) * ln)
        bolt_glow(canvas, jagged_path(q0, q1, seed=100 + i, segments=3, disp=0.26), 18, 5, 7)
    finalize_skill(canvas, mask, "skill_open_circuit.png")


if __name__ == "__main__":
    argv = sys.argv[1:]
    out = argv[argv.index("--out") + 1] if "--out" in argv else "/tmp/icons_v2"
    os.makedirs(out, exist_ok=True)
    base.OUT_DIR = out
    for f in (make_arc_bolt, make_stormspear, make_arc_step, make_gaze, make_open_circuit):
        f()
    print("done ->", out)
