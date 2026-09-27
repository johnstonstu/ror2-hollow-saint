"""Hollow Saint VFX textures (vfx-run1). System Python: numpy + Pillow. Writes art/vfx/assets/textures/.

Convention: straight (un-premultiplied) RGBA, sRGB colour, alpha = glow coverage. For additive particles use
rgb * alpha; for alpha-blended decals use the alpha as coverage. Never overwrites: pass --suffix to write a new
variant name if a file exists.
"""
import math
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from hs_palette import ARC, CORE, COPPER_DARK, COPPER_GLOW, COPPER_TRIM, OUTER  # noqa: E402
from hs_io import fresh  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
TEX = os.path.join(ROOT, 'textures')
WRITTEN = []
WHITE = (1.0, 1.0, 1.0)


# ---------------------------------------------------------------- output
def lin2srgb(a):
    a = np.clip(a, 0, 1)
    return np.where(a <= 0.0031308, a * 12.92, 1.055 * np.power(a, 1 / 2.4) - 0.055)


def save_premult(P, name, alpha=None):
    """P: (H, W, 3) linear premultiplied emission. Alpha defaults to the max channel."""
    P = np.clip(P, 0, None)
    a = np.clip(P.max(axis=2), 0, 1) if alpha is None else np.clip(alpha, 0, 1)
    rgb = np.where(a[..., None] > 1e-5, P / np.maximum(a[..., None], 1e-5), 0)
    save_rgba(lin2srgb(rgb), a, name)


def save_rgba(rgb_srgb, a, name):
    path = fresh(os.path.join(TEX, name))
    img = np.dstack([np.clip(rgb_srgb, 0, 1), np.clip(a, 0, 1)])
    Image.fromarray((img * 255 + 0.5).astype(np.uint8), 'RGBA').save(path)
    WRITTEN.append(name)


def save_gray(v, name, alpha=None):
    v = np.clip(v, 0, 1)
    save_rgba(np.dstack([v, v, v]), v if alpha is None else alpha, name)


# ---------------------------------------------------------------- distance fields
class Field:
    def __init__(self, h, w, wrap_x=False):
        self.h, self.w, self.wrap_x = h, w, wrap_x
        self.d = np.full((h, w), np.inf, np.float32)

    def seg(self, a, b, margin):
        shifts = (-self.w, 0, self.w) if self.wrap_x else (0,)
        for s in shifts:
            ax, ay, bx, by = a[0] + s, a[1], b[0] + s, b[1]
            x0 = int(max(0, math.floor(min(ax, bx) - margin)))
            x1 = int(min(self.w, math.ceil(max(ax, bx) + margin) + 1))
            y0 = int(max(0, math.floor(min(ay, by) - margin)))
            y1 = int(min(self.h, math.ceil(max(ay, by) + margin) + 1))
            if x0 >= x1 or y0 >= y1:
                continue
            ys, xs = np.mgrid[y0:y1, x0:x1].astype(np.float32)
            xs += 0.5
            ys += 0.5
            dx, dy = bx - ax, by - ay
            L2 = dx * dx + dy * dy
            t = np.clip(((xs - ax) * dx + (ys - ay) * dy) / L2, 0, 1) if L2 > 1e-9 else 0
            d = np.hypot(xs - (ax + t * dx), ys - (ay + t * dy))
            sub = self.d[y0:y1, x0:x1]
            np.minimum(sub, d, out=sub)

    def poly(self, pts, margin, closed=False):
        n = len(pts)
        for i in range(n - 1 + (1 if closed else 0)):
            self.seg(pts[i], pts[(i + 1) % n], margin)
        return self


def glow_profile(d, wc, wg, core_gain=1.0, glow_gain=0.55):
    """Tails reach exactly 0 at 4.5 * wg, so Field margins must be >= 5 * wg (no visible boxes)."""
    core = np.exp(-(d / wc) ** 2) * core_gain
    glow = np.exp(-d / wg) * glow_gain * np.clip(1 - d / (4.5 * wg), 0, 1) ** 2
    return core, glow


def add_line(P, d, wc, wg, gain=1.0, core_col=CORE, glow_col=OUTER, glow_gain=0.55, white_gain=0.9):
    core, glow = glow_profile(d, wc, wg, 1.0, glow_gain)
    hot = np.exp(-(d / (wc * 0.55)) ** 2) * white_gain
    P += gain * (core[..., None] * np.array(core_col) + glow[..., None] * np.array(glow_col)
                 + hot[..., None] * np.array(WHITE))
    return P


def edge_window(h, w, fx=0.06, fy=0.06):
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    u, v = (xs + 0.5) / w, (ys + 0.5) / h
    wx = np.clip(np.minimum(u, 1 - u) / fx, 0, 1) if fx else 1
    wy = np.clip(np.minimum(v, 1 - v) / fy, 0, 1) if fy else 1
    return (wx * wy) ** 1.5


# ---------------------------------------------------------------- procedural helpers
def bolt_path(a, b, rng_coarse, rng_fine, levels=7, coarse_levels=2, coarse_amp=0.14, fine_amp=0.26):
    """Midpoint displacement; offsets are relative to each segment's length (constant = fractal jaggedness).
    Coarse levels use rng_coarse (overall shape, shared between flipbook frames), fine levels rng_fine."""
    pts = [np.array(a, float), np.array(b, float)]
    for lv in range(levels):
        rng, amp = (rng_coarse, coarse_amp) if lv < coarse_levels else (rng_fine, fine_amp)
        new = [pts[0]]
        for p, q in zip(pts[:-1], pts[1:]):
            dv = q - p
            L = np.hypot(*dv)
            n = np.array([-dv[1], dv[0]]) / max(L, 1e-9)
            new += [(p + q) / 2 + n * rng.uniform(-1, 1) * amp * L, q]
        pts = new
    return pts


def branches(main, rng, count, scale, spread=(0.35, 0.8), length=(0.18, 0.38)):
    out = []
    total = np.hypot(*(np.array(main[-1]) - np.array(main[0])))
    for _ in range(count):
        i = int(rng.uniform(0.15, 0.75) * (len(main) - 1))
        p = np.array(main[i])
        fwd = np.array(main[min(i + 4, len(main) - 1)]) - p
        fwd /= max(np.hypot(*fwd), 1e-9)
        ang = rng.uniform(*spread) * rng.choice([-1, 1])
        c, s = math.cos(ang), math.sin(ang)
        d = np.array([fwd[0] * c - fwd[1] * s, fwd[0] * s + fwd[1] * c])
        q = p + d * total * rng.uniform(*length) * scale
        out.append(bolt_path(p, q, rng, rng, levels=5, coarse_levels=1, coarse_amp=0.2, fine_amp=0.28))
    return out


def periodic_noise(h, w, beta=1.8, seed=0, stretch=(1.0, 1.0), min_cycles_x=0):
    rng = np.random.default_rng(seed)
    wn = rng.standard_normal((h, w))
    fy = np.fft.fftfreq(h)[:, None] * stretch[1]
    fx = np.fft.fftfreq(w)[None, :] * stretch[0]
    f = np.sqrt(fx ** 2 + fy ** 2)
    f[0, 0] = 1
    spec = np.fft.fft2(wn) / f ** beta
    spec[0, 0] = 0
    if min_cycles_x:
        spec[:, np.abs(np.fft.fftfreq(w) * w) < min_cycles_x] = 0
    n = np.real(np.fft.ifft2(spec))
    n = (n - n.min()) / (n.max() - n.min())
    return n


def periodic_1d(n, beta=1.5, seed=0):
    rng = np.random.default_rng(seed)
    wn = rng.standard_normal(n)
    f = np.abs(np.fft.fftfreq(n))
    f[0] = 1
    s = np.fft.fft(wn) / f ** beta
    s[0] = 0
    v = np.real(np.fft.ifft(s))
    return (v - v.min()) / (v.max() - v.min())


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def polar(h, w):
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    x = (xs + 0.5) / w - 0.5
    y = (ys + 0.5) / h - 0.5
    return x, y, np.hypot(x, y), np.arctan2(y, x)


# ---------------------------------------------------------------- textures
def bolt_flipbook(name, branchy, seed):
    C, N = 256, 4
    sheet = np.zeros((C * N, C * N, 3), np.float32)
    rngc = np.random.default_rng(seed)
    a, b = (10, C / 2 + rngc.uniform(-12, 12)), (C - 10, C / 2 + rngc.uniform(-12, 12))
    coarse_seed = int(rngc.integers(1e9))
    gains = [0.55, 0.75, 1.35, 1.2, 1.0, 0.85, 1.05, 0.75, 0.8, 0.62, 0.68, 0.5, 0.38, 0.26, 0.15, 0.07]
    widths = [0.7, 0.8, 1.35, 1.2, 1.0, 1.0, 1.05, 0.95, 0.95, 0.9, 0.9, 0.85, 0.75, 0.65, 0.55, 0.5]
    win = edge_window(C, C, 0.03, 0.05)
    for f in range(16):
        fine = np.random.default_rng(seed * 100 + (f // 2 if f >= 4 else 0) + 7)
        main = bolt_path(a, b, np.random.default_rng(coarse_seed), fine)
        leader = None
        if f == 0:
            leader = 0.3
        elif f == 1:
            leader = 0.68
        pts = main[: max(2, int(len(main) * leader))] if leader else main
        P = np.zeros((C, C, 3), np.float32)
        fld = Field(C, C).poly(pts, 52)
        add_line(P, fld.d, 1.6 * widths[f], 7.5 * widths[f], gains[f])
        if leader:
            tip = pts[-1]
            x, y, _, _ = polar(C, C)
            r = np.hypot((x + 0.5) * C - tip[0], (y + 0.5) * C - tip[1])
            P += (np.exp(-(r / 6) ** 2) * 0.9)[..., None] * np.array(CORE)
        if branchy and not leader:
            brng = np.random.default_rng(seed * 31 + (f // 2) + 3)
            for br in branches(main, brng, 4, 1.0):
                fb = Field(C, C).poly(br, 26)
                add_line(P, fb.d, 1.0 * widths[f], 5.0, gains[f] * 0.6, core_col=ARC, white_gain=0.4)
        P *= win[..., None]
        row, col = divmod(f, N)
        sheet[row * C:(row + 1) * C, col * C:(col + 1) * C] = P
    save_premult(sheet, name)


def bolt_ribbon_tile():
    H, W = 64, 512
    ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
    v = (ys + 0.5) / H - 0.5
    wob = (periodic_1d(W, 1.6, 11) - 0.5)[None, :] * 0.10
    d = np.abs(v - wob) * H
    bright = 0.75 + 0.5 * periodic_1d(W, 1.2, 12)[None, :]
    P = np.zeros((H, W, 3), np.float32)
    core, glow = glow_profile(d, 2.2, 7.0, 1.0, 0.6)
    hot = np.exp(-(d / 1.2) ** 2)
    P += (core * bright)[..., None] * np.array(CORE) + (glow * bright)[..., None] * np.array(OUTER)
    P += (hot * bright * 0.8)[..., None] * np.array(WHITE)
    fil = Field(H, W, wrap_x=True)
    rng = np.random.default_rng(13)
    xsz = np.linspace(0, W, 17)
    pts = [(x, H / 2 + (rng.uniform(-14, 14) if 0 < i < 16 else 9)) for i, x in enumerate(xsz)]
    fil.poly(pts, 12)
    add_line(P, fil.d, 0.7, 3.0, 0.35, core_col=ARC, white_gain=0.2)
    P *= smoothstep(0.0, 0.18, 0.5 - np.abs(v))[..., None]
    save_premult(P, 'hs_bolt_ribbon_tile_512x64.png')


def band_profile():
    """Clean soft band across V (tileable along U): mark segments, halo segments, generic glow strips."""
    H, W = 64, 64
    ys, _ = np.mgrid[0:H, 0:W].astype(np.float32)
    d = np.abs((ys + 0.5) / H - 0.5) * H
    core, glow = glow_profile(d, 4.0, 6.0, 1.0, 0.55)
    hot = np.exp(-(d / 2.2) ** 2) * 0.8
    P = core[..., None] * np.array(CORE) + glow[..., None] * np.array(OUTER) + hot[..., None] * np.array(WHITE)
    save_premult(P, 'hs_band_profile_64.png')


def bolt_ribbon_stretch():
    H, W = 128, 1024
    rng = np.random.default_rng(21)
    main = bolt_path((0, H / 2), (W, H / 2), rng, rng, levels=9, coarse_levels=4, coarse_amp=0.012, fine_amp=0.24)
    P = np.zeros((H, W, 3), np.float32)
    add_line(P, Field(H, W).poly(main, 42).d, 2.2, 8.0, 1.1)
    for br in branches(main, rng, 3, 0.05, length=(0.4, 0.6)):
        add_line(P, Field(H, W).poly(br, 26).d, 1.1, 5.0, 0.5, core_col=ARC, white_gain=0.3)
    P *= edge_window(H, W, 0.015, 0.12)[..., None]
    save_premult(P, 'hs_bolt_ribbon_stretch_1024x128.png')


def spark_textures():
    C = 128
    x, y, r, th = polar(C, C)

    def streak(col_tail, col_head, cell=C):
        X, Y, _, _ = polar(cell, cell)
        t = np.clip((X + 0.40) / 0.78, 0, 1)
        inside = (X > -0.40) & (X < 0.40)
        w = 0.010 + 0.022 * t
        prof = np.exp(-(Y / w) ** 2) * np.where(inside, t ** 1.6, 0)
        head = np.exp(-(((X - 0.36) / 0.07) ** 2 + (Y / 0.05) ** 2))
        halo = np.exp(-np.hypot((X - 0.3) / 2.2, Y) / 0.035) * 0.35 * np.clip(t, 0, 1)
        tail = np.array(col_tail)
        P = prof[..., None] * (tail * 0.9 + np.array(WHITE) * 0.4 * t[..., None])
        P += head[..., None] * np.array(col_head) * 1.2 + halo[..., None] * tail
        return P * edge_window(cell, cell, 0.04, 0.1)[..., None]

    def star(col, cell=C, spikes=4):
        X, Y, R, TH = polar(cell, cell)
        core = np.exp(-(R / 0.045) ** 2) * 1.3
        sp = 0
        for k in range(spikes):
            a = k * math.pi / spikes * 2 / 2 if spikes == 4 else k * math.pi / spikes
            ca, sa = math.cos(a), math.sin(a)
            along = np.abs(X * ca + Y * sa)
            perp = np.abs(-X * sa + Y * ca)
            sp = sp + np.exp(-perp / 0.006) * np.exp(-along / 0.12) * (1.0 if k % 2 == 0 else 0.55)
        glow = np.exp(-R / 0.07) * 0.4
        P = core[..., None] * np.array(WHITE) + sp[..., None] * np.array(CORE) + glow[..., None] * np.array(col)
        return P * edge_window(cell, cell, 0.08, 0.08)[..., None]

    def dot(col, cell=C):
        X, Y, R, _ = polar(cell, cell)
        P = (np.exp(-(R / 0.06) ** 2) * 1.2)[..., None] * np.array(WHITE)
        P += (np.exp(-R / 0.06) * 0.6)[..., None] * np.array(col)
        return P * edge_window(cell, cell, 0.08, 0.08)[..., None]

    s_cyan = streak(ARC, CORE)
    s_copper = streak(COPPER_GLOW, (1.0, 0.8, 0.55))
    st = star(OUTER)
    dt = dot(ARC)
    save_premult(s_cyan, 'hs_spark_streak_128.png')
    save_premult(s_copper, 'hs_spark_streak_copper_128.png')
    save_premult(st, 'hs_spark_star_128.png')
    sheet = np.zeros((256, 256, 3), np.float32)
    sheet[:128, :128], sheet[:128, 128:], sheet[128:, :128], sheet[128:, 128:] = s_cyan, st, s_copper, dt
    save_premult(sheet, 'hs_spark_sheet_2x2_256.png')


def glow_cards():
    C = 256
    x, y, r, th = polar(C, C)
    fade = smoothstep(0.5, 0.40, r)
    soft = np.exp(-(r / 0.10) ** 2) * 1.0 + np.exp(-r / 0.09) * 0.45
    P = np.exp(-(r / 0.05) ** 2)[..., None] * np.array(WHITE) * 0.6
    P += (soft * fade)[..., None] * np.array(ARC)
    save_premult(P * fade[..., None], 'hs_glow_soft_256.png')

    sp = np.zeros_like(r)
    for k, (length, gain) in enumerate([(0.30, 1.0), (0.30, 1.0), (0.14, 0.5), (0.14, 0.5)]):
        a = [0, math.pi / 2, math.pi / 4, -math.pi / 4][k]
        ca, sa = math.cos(a), math.sin(a)
        along = np.abs(x * ca + y * sa)
        perp = np.abs(-x * sa + y * ca)
        taper = 0.004 + 0.010 * np.exp(-along / 0.08)
        sp += np.exp(-perp / taper) * np.exp(-along / (length * 0.45)) * gain
    core = np.exp(-(r / 0.035) ** 2) * 1.4
    P = core[..., None] * np.array(WHITE) + sp[..., None] * np.array(CORE)
    P += (np.exp(-r / 0.06) * 0.5)[..., None] * np.array(OUTER)
    save_premult(P * fade[..., None], 'hs_glow_star_256.png')

    R0 = 0.36
    d = np.abs(r - R0)
    P = (np.exp(-(d / 0.008) ** 2) * 0.9)[..., None] * np.array(WHITE)
    P += (np.exp(-(d / 0.02) ** 2))[..., None] * np.array(CORE)
    P += (np.exp(-d / 0.035) * 0.45)[..., None] * np.array(OUTER)
    save_premult(P * fade[..., None], 'hs_glow_ring_256.png')


def meter_ramp():
    stops = [(0.00, (0.010, 0.009, 0.010)), (0.10, COPPER_DARK), (0.30, COPPER_TRIM),
             (0.45, tuple(0.85 * c for c in COPPER_GLOW)), (0.58, (0.40, 0.55, 0.60)), (0.66, OUTER),
             (0.84, ARC), (0.94, CORE), (1.00, WHITE)]
    for W, H in ((256, 16), (1024, 32)):
        u = (np.arange(W) + 0.5) / W
        col = np.zeros((W, 3))
        for (x0, c0), (x1, c1) in zip(stops[:-1], stops[1:]):
            m = (u >= x0) & (u <= x1)
            t = ((u[m] - x0) / (x1 - x0))[:, None]
            t = t * t * (3 - 2 * t)
            col[m] = np.array(c0) * (1 - t) + np.array(c1) * t
        a = 0.15 + 0.85 * smoothstep(0.0, 0.35, u)
        rgb = np.repeat(lin2srgb(col)[None], H, 0)
        save_rgba(rgb, np.repeat(a[None], H, 0), f'hs_meter_ramp_{W}x{H}.png')


def streak_noise(S=256):
    """Long streaks along V (image y), tileable both ways: 1D noise across U, warped gently along V."""
    def band(lo, hi, seed):
        rng = np.random.default_rng(seed)
        x = np.arange(S) / S
        v = np.zeros(S)
        for f in range(lo, hi + 1):
            v += np.cos(2 * np.pi * (f * x + rng.random())) / f ** 0.7
        return (v - v.min()) / (v.max() - v.min())

    ys, xs = np.mgrid[0:S, 0:S]
    warp = (periodic_1d(S, 2.2, 46) - 0.5) * 8
    xw = ((xs + warp[ys]) % S).astype(np.float32)
    line = 0.65 * band(8, 40, 47) + 0.35 * band(30, 96, 48)
    v = np.interp(xw, np.arange(S), line, period=S)
    life = periodic_noise(S, S, 1.8, 49, stretch=(3.0, 1.0))
    v = v * (0.6 + 0.4 * life)
    return smoothstep(0.35, 0.8, v)


def noise_textures():
    n = periodic_noise(256, 256, 1.7, 41)
    save_gray(smoothstep(0.15, 0.95, n), 'hs_noise_scroll_256.png')
    save_gray(streak_noise(), 'hs_noise_streak_256.png')
    # voronoi F2-F1 crackle, tileable
    rng = np.random.default_rng(43)
    pts = rng.uniform(0, 256, (28, 2))
    ys, xs = np.mgrid[0:256, 0:256].astype(np.float32) + 0.5
    ds = []
    for px, py in pts:
        dx = np.abs(xs - px)
        dy = np.abs(ys - py)
        dx = np.minimum(dx, 256 - dx)
        dy = np.minimum(dy, 256 - dy)
        ds.append(np.hypot(dx, dy))
    ds = np.sort(np.stack(ds), axis=0)
    edge = ds[1] - ds[0]
    warp = periodic_noise(256, 256, 1.5, 44)
    c = np.exp(-edge / (1.1 + 1.5 * warp)) * (0.5 + 0.7 * periodic_noise(256, 256, 1.2, 45))
    save_gray(np.clip(c, 0, 1), 'hs_noise_crackle_256.png')


def conductor_mark():
    C = 512
    R = 0.36 * C
    cx = cy = C / 2
    P = np.zeros((C, C, 3), np.float32)
    fld = Field(C, C)
    seg_len, gap = math.radians(62), math.radians(28)
    for k in range(4):
        mid = k * math.pi / 2 - math.pi / 2
        a0, a1 = mid - seg_len / 2, mid + seg_len / 2
        pts = [(cx + R * math.cos(a), cy + R * math.sin(a)) for a in np.linspace(a0, a1, 48)]
        fld.poly(pts, 72)
    add_line(P, fld.d, 4.0, 14.0, 1.0, glow_gain=0.5)
    x, y, r, th = polar(C, C)
    l1 = (np.abs(x) + np.abs(y) * 0.8) * C
    s = 20.0
    fill = smoothstep(s + 1.5, s - 1.5, l1)
    P += fill[..., None] * np.array(CORE) * 1.1
    P += (np.exp(-np.maximum(l1 - s, 0) / 6.0) * 0.35)[..., None] * np.array(OUTER)
    P += (np.exp(-(np.abs(r * C - R * 0.62) / 1.5) ** 2) * 0.12)[..., None] * np.array(OUTER)
    save_premult(P * edge_window(C, C, 0.05, 0.05)[..., None], 'hs_conductor_mark_512.png')


def lichtenberg(C, rng, rays=8, step=6.0, max_gen=3):
    segs = []

    def walk(p, ang, length, gen):
        steps = int(length / step)
        for _ in range(steps):
            ang += rng.normal(0, 0.35)
            q = (p[0] + math.cos(ang) * step, p[1] + math.sin(ang) * step)
            if math.hypot(q[0] - C / 2, q[1] - C / 2) > C * 0.46:
                return
            segs.append((p, q, gen))
            if gen < max_gen and rng.random() < 0.10:
                walk(q, ang + rng.choice([-1, 1]) * rng.uniform(0.4, 0.9), length * 0.45, gen + 1)
            p = q

    for k in range(rays):
        a = 2 * math.pi * k / rays + rng.uniform(-0.3, 0.3)
        start = (C / 2 + math.cos(a) * C * 0.06, C / 2 + math.sin(a) * C * 0.06)
        walk(start, a, C * rng.uniform(0.26, 0.40), 0)
    return segs


def scorch_textures():
    C = 512
    x, y, r, th = polar(C, C)
    rng = np.random.default_rng(51)
    ang = periodic_1d(1024, 1.3, 52)
    idx = ((th + math.pi) / (2 * math.pi) * 1023).astype(int)
    rad = 0.30 + 0.12 * ang[idx]
    n = periodic_noise(C, C, 1.6, 53)
    mask = smoothstep(rad + 0.03, rad - 0.14, r + (n - 0.5) * 0.10)
    segs = lichtenberg(C, rng)
    by_gen = {}
    for p, q, g in segs:
        by_gen.setdefault(g, []).append((p, q))
    crack = np.zeros((C, C), np.float32)
    glowP = np.zeros((C, C, 3), np.float32)
    for g, lst in by_gen.items():
        f = Field(C, C)
        for p, q in lst:
            f.seg(p, q, 42)
        w = 2.2 / (1 + g * 0.6)
        crack = np.maximum(crack, np.exp(-(f.d / w) ** 2))
        fall = smoothstep(0.46, 0.05, r)
        core, glow = glow_profile(f.d, w * 0.8, 4.0 + 2 * (2 - min(g, 2)), 1.0, 0.45)
        glowP += (core * fall)[..., None] * np.array(ARC) * (1.0 / (1 + g * 0.5))
        glowP += (glow * fall)[..., None] * np.array(OUTER) * (1.0 / (1 + g * 0.5))
    char = np.array((0.030, 0.024, 0.020))
    rim = np.array(COPPER_DARK) * 1.4
    t = smoothstep(0.05, 0.9, mask)[..., None]
    col = rim * (1 - t) + char * t
    col = col * (0.75 + 0.5 * n[..., None])
    alpha = np.clip(mask * (0.55 + 0.45 * n) + crack * 0.4 * mask, 0, 1) * 0.92
    col = col * (1 - 0.6 * crack[..., None])
    save_rgba(lin2srgb(col), alpha, 'hs_scorch_decal_512.png')

    speck = (rng.random((C, C)) > 0.9985).astype(np.float32)
    from PIL import ImageFilter
    sp = np.asarray(Image.fromarray((speck * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2)),
                    np.float32) / 255 * 6
    sp *= smoothstep(0.42, 0.1, r)
    glowP += np.clip(sp, 0, 1)[..., None] * np.array(COPPER_GLOW) * 0.8
    glowP += (np.exp(-(r / 0.06) ** 2) * 0.6)[..., None] * np.array(CORE)
    save_premult(glowP * edge_window(C, C, 0.04, 0.04)[..., None], 'hs_scorch_glow_512.png')

    # static ring: jagged closed loop around r = 0.38
    P = np.zeros((C, C, 3), np.float32)
    n_pts = 96
    radj = periodic_1d(n_pts, 0.8, 54)
    pts = [(C / 2 + math.cos(a) * C * (0.37 + 0.035 * (radj[i] - 0.5) + rng.uniform(-0.008, 0.008)),
            C / 2 + math.sin(a) * C * (0.37 + 0.035 * (radj[i] - 0.5) + rng.uniform(-0.008, 0.008)))
           for i, a in enumerate(np.linspace(0, 2 * math.pi, n_pts, endpoint=False))]
    add_line(P, Field(C, C).poly(pts, 36, closed=True).d, 1.8, 7.0, 1.0)
    f2 = Field(C, C)
    for k in range(10):
        a = rng.uniform(0, 2 * math.pi)
        r0 = C * 0.37
        p = (C / 2 + math.cos(a) * r0, C / 2 + math.sin(a) * r0)
        out = rng.choice([-1, 1]) * rng.uniform(0.05, 0.09) * C
        q = (C / 2 + math.cos(a + rng.uniform(-0.1, 0.1)) * (r0 + out),
             C / 2 + math.sin(a + rng.uniform(-0.1, 0.1)) * (r0 + out))
        f2.poly(bolt_path(p, q, rng, rng, levels=4, coarse_levels=1, coarse_amp=0.3, fine_amp=0.3), 22)
    add_line(P, f2.d, 0.9, 4.0, 0.55, core_col=ARC, white_gain=0.3)
    save_premult(P * edge_window(C, C, 0.04, 0.04)[..., None], 'hs_static_ring_512.png')


def spear_dissolve():
    W, H = 128, 512
    ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
    v = 1 - (ys + 0.5) / H          # image bottom = UV v 0 = lance tail
    n = periodic_noise(H, W, 1.5, 61, stretch=(1.0, 0.35))
    comb = np.clip(0.82 * v + 0.18 * n, 0, 1)
    rgb = np.dstack([v, n, comb])
    save_rgba(rgb, comb, 'hs_spear_dissolve_mask_128x512.png')


def ghost_scanlines():
    S = 256
    ys, xs = np.mgrid[0:S, 0:S].astype(np.float32)
    lines = 0.5 + 0.5 * np.cos(2 * math.pi * (ys + 0.5) / 8)
    lines = lines ** 3
    drop = smoothstep(0.35, 0.65, periodic_noise(S, S, 1.4, 71, stretch=(0.25, 1.0)))
    v = np.clip(0.25 + 0.75 * lines * (0.4 + 0.6 * drop), 0, 1)
    save_gray(v, 'hs_ghost_scanline_256.png')


def trail_angular():
    H, W = 128, 512
    rng = np.random.default_rng(81)
    xsz = np.cumsum(rng.uniform(20, 48, 40))
    xsz = xsz[xsz < W - 20]
    pts = [(0.0, H / 2)] + [(x, H / 2 + (22 if i % 2 else -22) * rng.uniform(0.35, 1.0)) for i, x in enumerate(xsz)]
    pts.append((float(W), H / 2))
    P = np.zeros((H, W, 3), np.float32)
    add_line(P, Field(H, W, wrap_x=True).poly(pts, 36).d, 2.0, 7.0, 1.0)
    ys, _ = np.mgrid[0:H, 0:W].astype(np.float32)
    v = (ys + 0.5) / H - 0.5
    P += (np.exp(-(v / 0.08) ** 2) * 0.10)[..., None] * np.array(OUTER)
    P *= smoothstep(0.5, 0.36, np.abs(v))[..., None]
    save_premult(P, 'hs_trail_angular_512x128.png')


def jet_exhaust():
    S = 256
    ys, xs = np.mgrid[0:S, 0:S].astype(np.float32)
    v = 1 - (ys + 0.5) / S          # UV v 0 = cone base (nozzle), 1 = tip
    streak = periodic_noise(S, S, 1.6, 91, stretch=(1.0, 5.0))
    body = (1 - v) ** 1.4 * (0.55 + 0.6 * streak)
    body *= smoothstep(0.0, 0.04, v) * 0.6 + 0.4
    hot = np.exp(-(v / 0.18) ** 2)
    t = np.clip(v * 1.6, 0, 1)[..., None]
    col = (np.array(CORE) * (1 - t) + np.array(OUTER) * t)
    P = body[..., None] * col + hot[..., None] * np.array(WHITE) * 0.5
    save_premult(P, 'hs_jet_exhaust_256.png')


def main():
    os.makedirs(TEX, exist_ok=True)
    if '--only-band' in sys.argv:
        band_profile()
        print('\n'.join(WRITTEN))
        return
    if '--only-streak' in sys.argv:
        save_gray(streak_noise(), 'hs_noise_streak_256.png')
        print('\n'.join(WRITTEN))
        return
    band_profile()
    bolt_flipbook('hs_bolt_flipbook_4x4_1024.png', False, 3)
    bolt_flipbook('hs_bolt_branch_flipbook_4x4_1024.png', True, 5)
    bolt_ribbon_tile()
    bolt_ribbon_stretch()
    spark_textures()
    glow_cards()
    meter_ramp()
    noise_textures()
    conductor_mark()
    scorch_textures()
    spear_dissolve()
    ghost_scanlines()
    trail_angular()
    jet_exhaust()
    print('\n'.join(WRITTEN))


if __name__ == '__main__':
    main()
