"""Texture previews on a dark background (additive, like the in-game shaders) + animated GIFs."""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from hs_io import fresh, log  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
TEX = os.path.join(ROOT, 'textures')
PRE = os.path.join(ROOT, 'previews')
BG = np.array([0.006, 0.007, 0.010])   # linear, ~#10121a


def s2l(a):
    return np.where(a <= 0.04045, a / 12.92, ((a + 0.055) / 1.055) ** 2.4)


def l2s(a):
    a = np.clip(a, 0, 1)
    return np.where(a <= 0.0031308, a * 12.92, 1.055 * a ** (1 / 2.4) - 0.055)


def load(name):
    a = np.asarray(Image.open(os.path.join(TEX, name)).convert('RGBA'), np.float32) / 255
    return s2l(a[..., :3]), a[..., 3]


def composite(rgb, a, mode='add', gain=1.0, bloom=True, bg=BG):
    h, w = a.shape
    base = np.ones((h, w, 3)) * bg
    if mode == 'add':
        emit = rgb * a[..., None] * gain
        out = base + emit
        if bloom:
            e8 = Image.fromarray((np.clip(l2s(emit), 0, 1) * 255).astype(np.uint8))
            bl = s2l(np.asarray(e8.filter(ImageFilter.GaussianBlur(max(2, w // 64))), np.float32) / 255)
            out += bl * 0.6
    else:
        out = base * (1 - a[..., None]) + rgb * a[..., None]
    return Image.fromarray((l2s(out) * 255 + 0.5).astype(np.uint8), 'RGB')


def label(img, text):
    d = ImageDraw.Draw(img)
    d.text((6, 4), text, fill=(150, 170, 185))
    return img


def fit(img, max_w=768):
    if img.width > max_w:
        img = img.resize((max_w, int(img.height * max_w / img.width)), Image.LANCZOS)
    return img


def still(name, mode='add', gain=1.0, pad=24, tile=None, scale=1, bg=BG):
    rgb, a = load(name)
    if tile:
        rgb, a = np.tile(rgb, (tile[1], tile[0], 1)), np.tile(a, (tile[1], tile[0]))
    img = composite(rgb, a, mode, gain, bg=bg)
    if scale != 1:
        img = img.resize((img.width * scale, img.height * scale), Image.NEAREST if scale > 2 else Image.LANCZOS)
    img = fit(img)
    canvas = Image.new('RGB', (img.width + 2 * pad, img.height + 2 * pad + 16), tuple((l2s(BG) * 255).astype(int)))
    canvas.paste(img, (pad, pad + 16))
    extra = f'  tiled {tile[0]}x{tile[1]}' if tile else ''
    label(canvas, f'{name}  ({mode}){extra}')
    out = fresh(os.path.join(PRE, 'tex_' + name.replace('.png', '') + '.png'))
    canvas.save(out)
    return out


def gif(frames, name, fps=24):
    out = fresh(os.path.join(PRE, name))
    frames[0].save(out, save_all=True, append_images=frames[1:], duration=int(1000 / fps), loop=0, disposal=2)
    return out


def flipbook_gif(name, out_name, fps=24):
    rgb, a = load(name)
    C = a.shape[0] // 4
    frames = []
    for f in list(range(16)) + [15] * 8:
        r, c = divmod(f, 4)
        img = composite(rgb[r * C:(r + 1) * C, c * C:(c + 1) * C], a[r * C:(r + 1) * C, c * C:(c + 1) * C])
        frames.append(label(img.resize((384, 384), Image.LANCZOS), f'{name} f{f}'))
    return gif(frames, out_name, fps)


def scroll_gif(name, out_name, axis=1, tile=(2, 2), steps=24, mode='add'):
    rgb, a = load(name)
    rgb, a = np.tile(rgb, (tile[1], tile[0], 1)), np.tile(a, (tile[1], tile[0]))
    size = a.shape[0] // tile[1] if axis == 0 else a.shape[1] // tile[0]
    frames = []
    for i in range(steps):
        sh = int(size * i / steps)
        img = composite(np.roll(rgb, sh, axis=axis), np.roll(a, sh, axis=axis), mode)
        frames.append(label(fit(img, 512), f'{name} scroll (tiling check)'))
    return gif(frames, out_name)


def meter_gif():
    rgb, a = load('hs_meter_ramp_1024x32.png')
    frames = []
    for i in range(41):
        pct = i / 40
        W, H = 520, 150
        img = Image.new('RGB', (W, H), tuple((l2s(BG) * 255).astype(int)))
        d = ImageDraw.Draw(img)
        idx = int(pct * 1023)
        col = tuple((l2s(rgb[0, idx]) * 255).astype(int))
        # the ramp itself + a bar filled to pct + a "core" swatch coloured by the ramp at pct
        strip = composite(rgb, a, 'alpha').resize((480, 20))
        img.paste(strip, (20, 30))
        d.rectangle([20, 70, 20 + int(480 * pct), 90], fill=col)
        d.rectangle([20, 70, 500, 90], outline=(60, 70, 80))
        d.ellipse([230, 100, 290, 140], fill=col)
        d.text((20, 8), f'hs_meter_ramp  meter {int(pct * 100)}%', fill=(150, 170, 185))
        frames.append(img)
    frames += [frames[-1]] * 12
    return gif(frames, 'anim_meter_ramp_fill.gif', 12)


def main():
    made = []
    for n in sorted(os.listdir(TEX)):
        if not n.endswith('.png'):
            continue
        if n.startswith('hs_scorch_decal'):
            made.append(still(n, 'alpha', bg=np.array([0.10, 0.09, 0.08])))   # on a stone-grey floor
        elif 'meter_ramp' in n:
            made.append(still(n, 'alpha'))
        elif any(k in n for k in ('noise', 'ghost', 'trail_angular', 'ribbon_tile')):
            made.append(still(n, 'add', tile=(2, 2) if 'ribbon' not in n and 'trail' not in n else (2, 1)))
        elif 'dissolve' in n:
            made.append(still(n, 'alpha'))
        else:
            made.append(still(n, 'add'))
    made.append(flipbook_gif('hs_bolt_flipbook_4x4_1024.png', 'anim_bolt_flipbook.gif'))
    made.append(flipbook_gif('hs_bolt_branch_flipbook_4x4_1024.png', 'anim_bolt_branch_flipbook.gif'))
    made.append(scroll_gif('hs_noise_scroll_256.png', 'anim_noise_scroll.gif', axis=1))
    made.append(scroll_gif('hs_noise_streak_256.png', 'anim_noise_streak_scroll.gif', axis=0))
    made.append(scroll_gif('hs_jet_exhaust_256.png', 'anim_jet_exhaust_scroll_u.gif', axis=1, tile=(2, 1)))
    made.append(meter_gif())
    log(f'texture previews: {len(made)} files in previews/ (tex_*.png, anim_*.gif)')
    print('\n'.join(os.path.basename(m) for m in made))


if __name__ == '__main__':
    main()
