"""Tile pad_closeup.py frames (system Python + Pillow).

Run: python tools/blender/anim/pad_sheet.py art/anim/wip/shoulders/<tag>/<clip-slug> [--scale 0.5]
       [--compare art/anim/wip/shoulders/<other-tag>/<clip-slug>] [--out <folder>]
One strip per frame: [front | back | top | outL | outR]. --compare stacks the other folder's strip above
(before on top, after below; frames pair by index). Writes pads.gif and pads-sheet.jpg to --out (default: folder).
"""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw

folder = Path(sys.argv[1])
args = sys.argv[2:]
scale = float(args[args.index('--scale')+1]) if '--scale' in args else 0.5
other = Path(args[args.index('--compare')+1]) if '--compare' in args else None
dest = Path(args[args.index('--out')+1]) if '--out' in args else folder
dest.mkdir(parents=True, exist_ok=True)


def strips(fold):
    info = json.loads((fold/'clip.json').read_text())
    views = info['views']
    out = []
    for i, f in enumerate(info['rendered_frames']):
        ims = [Image.open(fold/f'{v}-{i:03}.png').convert('RGB') for v in views]
        w, h = ims[0].size
        st = Image.new('RGB', (len(views)*w, h+20), '#161a1e')
        dr = ImageDraw.Draw(st)
        for k, im in enumerate(ims):
            st.paste(im, (k*w, 20))
            dr.text((k*w+5, 4), views[k], fill='#a9e6ec')
        dr.text((len(views)*w-220, 4), f"{info['tag']}  {info['title']}  f{f}", fill='#ffd27a')
        out.append(st)
    return out


def stack(a, b):
    im = Image.new('RGB', (max(a.width, b.width), a.height+b.height), '#161a1e')
    im.paste(a, (0, 0))
    im.paste(b, (0, a.height))
    return im


cur = strips(folder)
if other:
    cur = [stack(o, c) for o, c in zip(strips(other), cur)]
cur[0].save(dest/'pads.gif', save_all=True, append_images=cur[1:], duration=120, loop=0)
w, h = int(cur[0].width*scale), int(cur[0].height*scale)
cols = 1 if w > 1200 else 2
rows = (len(cur)+cols-1)//cols
sheet = Image.new('RGB', (cols*w, rows*h), '#161a1e')
for i, s in enumerate(cur):
    sheet.paste(s.resize((w, h)), ((i % cols)*w, (i//cols)*h))
sheet.save(dest/'pads-sheet.jpg', quality=88)
print('PAD SHEET', dest, len(cur))
