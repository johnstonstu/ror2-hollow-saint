"""Crop the same box out of shoulder_diag renders for several tags and tile them (system Python).

Run: python tools/blender/anim/crop_compare.py <name> <x0,y0,x1,y1> tagA tagB [tagC ...]
  <name> is the render suffix after the tag, e.g. arc-bolt-left-f5-back.
Writes art/anim/wip/shoulder/crop-<name>.png (one column per tag, upscaled 2x).
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

D = Path(__file__).resolve().parents[3]/'art/anim/wip/shoulder'
name, box, tags = sys.argv[1], tuple(int(v) for v in sys.argv[2].split(',')), sys.argv[3:]
w, h = (box[2]-box[0])*2, (box[3]-box[1])*2
out = Image.new('RGB', (w*len(tags), h), '#161a1e')
d = ImageDraw.Draw(out)
for i, t in enumerate(tags):
    im = Image.open(D/f'{t}-{name}.png').convert('RGB').crop(box).resize((w, h))
    out.paste(im, (i*w, 0))
    d.text((i*w+6, 6), t, fill='#ffe08a')
out.save(D/f'crop-{name}.png')
print('WROTE', D/f'crop-{name}.png')
