"""Grade raw portrait renders onto vanilla-style backgrounds and build a comparison sheet.

python tools/icons/finish_charselect_candidates.py
Reads art/icons/charselect-candidates/_work/raw_*.png (from render_charselect_candidates.py),
writes 256 px candidates and comparison-sheet.png into art/icons/charselect-candidates/.
"""
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'art/icons/charselect-candidates'
WORK = OUT / '_work'
CURRENT = ROOT / 'HollowSaintMod/Icons/portrait.png'
SIZE = 256

# name: (label, background centre RGB, background edge RGB, glow RGB, glow centre (x, y) as fractions)
CANDIDATES = {
    'a_slate_threequarter': ('A  Slate 3/4', (58, 70, 92), (10, 12, 18), (70, 170, 220), (0.52, 0.34)),
    'b_storm_lowangle': ('B  Storm low angle', (70, 50, 112), (12, 9, 24), (90, 150, 255), (0.45, 0.32)),
    'c_reliquary_closeup': ('C  Reliquary halo', (96, 62, 40), (18, 12, 9), (255, 170, 90), (0.50, 0.30)),
}


def background(size, centre, edge, glow, glow_at):
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32) / (size - 1)
    r = np.sqrt((xx - 0.5) ** 2 + (yy - 0.42) ** 2) / 0.75
    t = np.clip(r, 0, 1) ** 1.4
    base = np.array(centre, np.float32) * (1 - t)[..., None] + np.array(edge, np.float32) * t[..., None]
    g = np.exp(-(((xx - glow_at[0]) ** 2 + (yy - glow_at[1]) ** 2) / (2 * 0.16 ** 2)))
    base += np.array(glow, np.float32) * 0.22 * g[..., None]
    rng = np.random.default_rng(7)
    base += rng.normal(0, 2.0, (size, size, 1)).astype(np.float32)
    return base


def finish(name, centre, edge, glow, glow_at):
    raw = Image.open(WORK / f'raw_{name}.png').convert('RGBA')
    big = raw.size[0]
    rgba = np.asarray(raw).astype(np.float32)
    rgb, alpha = rgba[..., :3], rgba[..., 3:] / 255.0

    bg = background(big, centre, edge, glow, glow_at)
    comp = rgb * alpha + bg * (1 - alpha)

    # Bloom from saturated cyan/emissive pixels only, so the ivory plates stay crisp.
    lum = rgb.max(axis=2)
    sat = lum - rgb.min(axis=2)
    mask = np.clip((lum - 200) / 55, 0, 1) * np.clip(sat / 60, 0, 1) * alpha[..., 0]
    glow_src = Image.fromarray((rgb * mask[..., None]).clip(0, 255).astype(np.uint8))
    bloom = np.asarray(glow_src.filter(ImageFilter.GaussianBlur(big / 48))).astype(np.float32)
    bloom += np.asarray(glow_src.filter(ImageFilter.GaussianBlur(big / 18))).astype(np.float32) * 0.8
    comp = 255 - (255 - comp) * (1 - bloom / 255 * 0.85)

    # Rim separation: a thin dark-to-light halo just outside the silhouette reads at 64 px.
    edge_a = Image.fromarray((alpha[..., 0] * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(big / 160))
    outer = np.clip(np.asarray(edge_a).astype(np.float32) / 255 - alpha[..., 0], 0, 1)
    comp = comp * (1 - outer[..., None] * 0.55)

    yy, xx = np.mgrid[0:big, 0:big].astype(np.float32) / (big - 1)
    vig = 1 - 0.45 * np.clip(np.sqrt((xx - 0.5) ** 2 + (yy - 0.45) ** 2) / 0.72, 0, 1) ** 2.2
    comp *= vig[..., None]

    img = Image.fromarray(comp.clip(0, 255).astype(np.uint8), 'RGB')
    img = img.resize((SIZE, SIZE), Image.LANCZOS).filter(ImageFilter.UnsharpMask(radius=1.2, percent=60, threshold=2))
    path = OUT / f'{name}.png'
    img.save(path, optimize=True)
    return img


def label_font(px):
    for f in ('segoeuib.ttf', 'arialbd.ttf', 'DejaVuSans-Bold.ttf'):
        try:
            return ImageFont.truetype(f, px)
        except OSError:
            continue
    return ImageFont.load_default()


def sheet(entries):
    pad, small, tiny = 24, 64, 32
    col = SIZE + pad
    w = pad + col * len(entries)
    h = pad + 28 + SIZE + pad + small + pad + 20 + pad
    out = Image.new('RGB', (w, h), (24, 24, 28))
    d = ImageDraw.Draw(out)
    font, sfont = label_font(18), label_font(13)
    for i, (label, img) in enumerate(entries):
        x = pad + i * col
        d.text((x, pad), label, fill=(230, 230, 235), font=font)
        y = pad + 28
        out.paste(img, (x, y))
        y += SIZE + pad
        out.paste(img.resize((small, small), Image.LANCZOS), (x, y))
        out.paste(img.resize((tiny, tiny), Image.LANCZOS), (x + small + 16, y + small - tiny))
        d.text((x + small + 16 + tiny + 12, y + small - 16), '64 / 32 px', fill=(150, 150, 160), font=sfont)
    d.text((pad, h - pad - 16), 'Hollow Saint character-select portrait: current vs candidates (256 px full size)',
           fill=(150, 150, 160), font=sfont)
    out.save(OUT / 'comparison-sheet.png', optimize=True)


if __name__ == '__main__':
    entries = [('Current', Image.open(CURRENT).convert('RGB'))]
    for name, (label, centre, edge, glow, glow_at) in CANDIDATES.items():
        entries.append((label, finish(name, centre, edge, glow, glow_at)))
        print('CHARSELECT_FINISHED', name)
    sheet(entries)
    print('CHARSELECT_SHEET', OUT / 'comparison-sheet.png')
