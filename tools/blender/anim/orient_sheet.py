"""Tile orient_closeup.py frames (system Python + Pillow).

Run: python tools/blender/anim/orient_sheet.py art/anim/wip/hands/orientation/<tag>/<clip-slug> [--scale 0.5]
       [--compare art/anim/wip/hands/orientation/<other-tag>/<clip-slug>]
One strip per frame: [L front | L back | L palm | L thumb | R front | R back | R palm | R thumb] with the
per-hand readout (hand = handedness, -1 left / +1 right; up = thumb_up; fwd = palm_fwd; G = gated frame).
--compare stacks the other folder's strip above (before/after). Writes orient.gif and orient-sheet.jpg.
"""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw

folder = Path(sys.argv[1])
args = sys.argv[2:]
scale = float(args[args.index('--scale')+1]) if '--scale' in args else 0.5
other = Path(args[args.index('--compare')+1]) if '--compare' in args else None
VIEWS = ('front', 'back', 'palm', 'thumb')


def strips(fold):
    info = json.loads((fold/'clip.json').read_text())
    out = []
    for i, row in enumerate(info['rows']):
        ims = [Image.open(fold/f'{s}-{v}-{i:03}.png').convert('RGB') for s in 'LR' for v in VIEWS]
        w, h = ims[0].size
        st = Image.new('RGB', (8*w, h+34), '#161a1e')
        dr = ImageDraw.Draw(st)
        for k, im in enumerate(ims):
            st.paste(im, (k*w, 34))
            dr.text((k*w+5, 19), f"{'LR'[k//4]} {VIEWS[k % 4]}", fill='#a9e6ec')
        for j, s in enumerate('LR'):
            r = row[s]
            bad = r['gated'] and (r['thumb_up'] < 0 or r['palm_fwd'] < -0.25)
            txt = (f"{s}: hand {r['hand']:+.2f}  up {r['thumb_up']:+.2f}  fwd {r['palm_fwd']:+.2f}  "
                   f"raise {r['raise']:.0f}  elbow {r['elbow_open']:.0f}" + ('  G' if r['gated'] else '') +
                   ('  back' if r['swept_back'] else ''))
            dr.text((j*4*w+5, 3), txt, fill='#ff7a7a' if bad else '#d6f5c8')
        dr.text((8*w-150, 19), f"{info['tag']} {info['title']} f{row['f']}", fill='#ffd27a')
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
cur[0].save(folder/'orient.gif', save_all=True, append_images=cur[1:], duration=160, loop=0)
w, h = int(cur[0].width*scale), int(cur[0].height*scale)
cols = 1 if w > 1200 else 2
rows = (len(cur)+cols-1)//cols
sheet = Image.new('RGB', (cols*w, rows*h), '#161a1e')
for i, s in enumerate(cur):
    sheet.paste(s.resize((w, h)), ((i % cols)*w, (i//cols)*h))
sheet.save(folder/'orient-sheet.jpg', quality=88)
print('ORIENT SHEET', folder, len(cur))
