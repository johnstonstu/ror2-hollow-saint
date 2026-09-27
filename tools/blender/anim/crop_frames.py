"""Tile the same crop box out of several preview frames (system Python).

Run: python tools/blender/anim/crop_frames.py <out.png> <x0,y0,x1,y1|auto> <frame.png> [<frame.png> ...]
'auto' crops the central 40% of each frame. Each tile is upscaled 2x and labelled with its path tail.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

out_path, box_arg, frames = Path(sys.argv[1]), sys.argv[2], [Path(p) for p in sys.argv[3:]]
ims = [Image.open(p).convert('RGB') for p in frames]
if box_arg == 'auto':
    W, H = ims[0].size
    box = (int(W*0.3), int(H*0.2), int(W*0.7), int(H*0.7))
else:
    box = tuple(int(v) for v in box_arg.split(','))
w, h = (box[2]-box[0])*2, (box[3]-box[1])*2
out = Image.new('RGB', (w*len(ims), h), '#161a1e')
d = ImageDraw.Draw(out)
for i, (p, im) in enumerate(zip(frames, ims)):
    out.paste(im.crop(box).resize((w, h)), (i*w, 0))
    d.text((i*w+6, 6), f'{p.parent.name}/{p.stem}', fill='#ffe08a')
out_path.parent.mkdir(parents=True, exist_ok=True)
out.save(out_path)
print('WROTE', out_path, out.size)
