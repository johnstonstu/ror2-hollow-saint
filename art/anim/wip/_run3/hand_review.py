"""Readable review pages from hand_closeup.py frames: rows of [L ctx | L back | L palm | R back | R palm | R ctx].

python art/anim/wip/_run3/hand_review.py <slug> [--step N] [--rows 8] [--scale 0.42]
Writes art/anim/wip/_run3/handrev/<slug>-<page>.png
"""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw

WIP = Path(__file__).resolve().parents[1]
slug = sys.argv[1]
args = sys.argv[2:]
opt = lambda n, d: type(d)(args[args.index(n)+1]) if n in args else d
folder = WIP/'hands'/slug
info = json.loads((folder/'clip.json').read_text())
frames = info['rendered_frames']
step, rows, scale = opt('--step', 1), opt('--rows', 8), opt('--scale', 0.42)
TILES = ('L-context', 'L-back', 'L-palm', 'R-back', 'R-palm', 'R-context')
out = Path(__file__).parent/'handrev'
out.mkdir(exist_ok=True)
idx = list(range(0, len(frames), step))
pages = [idx[i:i+rows] for i in range(0, len(idx), rows)]
for pn, page in enumerate(pages):
    strips = []
    for n in page:
        ims = [Image.open(folder/f'{t}-{n:03}.png').convert('RGB') for t in TILES]
        w, h = ims[0].size
        s = Image.new('RGB', (len(TILES)*w, h), '#161a1e')
        for i, im in enumerate(ims):
            s.paste(im, (i*w, 0))
        s = s.resize((int(s.width*scale), int(s.height*scale)))
        ImageDraw.Draw(s).text((4, 2), f'f{frames[n]}', fill='#ffd27a')
        strips.append(s)
    pg = Image.new('RGB', (strips[0].width, sum(s.height for s in strips)), '#161a1e')
    y = 0
    for s in strips:
        pg.paste(s, (0, y))
        y += s.height
    pg.save(out/f'{slug}-{pn}.png')
print('PAGES', slug, len(pages))
