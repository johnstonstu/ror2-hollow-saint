"""Compose rendered frames into final previews: bloom (localized, like the game's), label, PNG + GIF."""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from hs_io import fresh, log  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
PRE = os.path.join(ROOT, 'previews')


def s2l(a):
    return np.where(a <= 0.04045, a / 12.92, ((a + 0.055) / 1.055) ** 2.4)


def l2s(a):
    a = np.clip(a, 0, 1)
    return np.where(a <= 0.0031308, a * 12.92, 1.055 * a ** (1 / 2.4) - 0.055)


def bloom(img):
    a = s2l(np.asarray(img.convert('RGB'), np.float32) / 255)
    bright = np.clip(a - 0.05, 0, None)
    b8 = Image.fromarray((l2s(bright) * 255).astype(np.uint8))
    w = img.width
    out = a.copy()
    for rad, k in ((max(2, w // 160), 0.45), (max(4, w // 50), 0.30)):
        out += s2l(np.asarray(b8.filter(ImageFilter.GaussianBlur(rad)), np.float32) / 255) * k
    return Image.fromarray((l2s(out) * 255 + 0.5).astype(np.uint8), 'RGB')


def labelled(img, text):
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, img.width, 16], fill=(8, 9, 12))
    d.text((6, 3), text, fill=(150, 170, 185))
    return img


def main():
    with open(os.path.join(PRE, 'frames', 'manifest.json')) as f:
        man = json.load(f)
    made = []
    for group, spec in man.items():
        frames = [labelled(bloom(Image.open(os.path.join(ROOT, p))), f'{spec["label"]}  [{spec["blend"]}]')
                  for p in spec['files']]
        key = frames[len(frames) * 2 // 3] if len(frames) > 1 else frames[0]
        out = fresh(os.path.join(PRE, f'{group}.png'))
        key.save(out)
        made.append(os.path.basename(out))
        if len(frames) > 1:
            g = fresh(os.path.join(PRE, f'{group}.gif'))
            gif_frames = [fr.convert('P', palette=Image.ADAPTIVE, colors=255) for fr in frames]
            gif_frames[0].save(g, save_all=True, append_images=gif_frames[1:], duration=int(1000 / spec['fps']),
                               loop=0)
            made.append(os.path.basename(g))
    log(f'mesh previews composed: {", ".join(made)}')
    print('\n'.join(made))


if __name__ == '__main__':
    main()
