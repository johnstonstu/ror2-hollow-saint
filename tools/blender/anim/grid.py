"""Tile a preview folder's frames into a views x frames grid (system Python, Pillow).

Run: python tools/blender/anim/grid.py <folder> <out.png> [--views front,side,back,hero] [--scale 0.5] [--crop x0,y0,x1,y1]
Frames are `<view>-NNN.png` in <folder> (e.g. a clip's inspect/ folder).
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

args = sys.argv[1:]
folder, out = Path(args[0]), Path(args[1])


def opt(name, default):
    return args[args.index(name)+1] if name in args else default


views = opt('--views', 'front,side,back,hero').split(',')
scale = float(opt('--scale', '0.5'))
crop = tuple(int(v) for v in opt('--crop', '').split(',')) if '--crop' in args else None
rows = []
for v in views:
    fs = sorted(folder.glob(f'{v}-[0-9][0-9][0-9].png'))
    if fs:
        rows.append((v, [Image.open(f).convert('RGB') for f in fs]))
ims = [im.crop(crop) if crop else im for _, r in rows for im in r]
w, h = int(ims[0].size[0]*scale), int(ims[0].size[1]*scale)
cols = max(len(r) for _, r in rows)
sheet = Image.new('RGB', (w*cols, h*len(rows)), '#161a1e')
d = ImageDraw.Draw(sheet)
for j, (v, r) in enumerate(rows):
    for i, im in enumerate(r):
        sheet.paste((im.crop(crop) if crop else im).resize((w, h)), (i*w, j*h))
        d.text((i*w+4, j*h+4), f'{v} {i}', fill='#ffe08a')
out.parent.mkdir(parents=True, exist_ok=True)
sheet.save(out)
print('WROTE', out, sheet.size)
