"""Tile hand2_closeup.py frames (item 12): hand GIFs and before/after sheets (system Python + Pillow).

Run: python tools/blender/anim/hand2_sheet.py <after clip dir> [--before <clip dir>] [--out <dir>] [--rows 6]
     [--labels v25,v26]
Per frame a strip of [L profile | L back | R back | R profile | pair (both hands + thighs)]. Writes to --out
(default: the after dir):
  hands.gif           the after strips, every rendered frame
  compare.gif         before strip above the after strip (frames pair by index), with --before
  before-after.jpg    --rows evenly spaced frames: before strip | after strip side by side, with --before
  body.gif            body-<view>-NNN.png frames of the after dir, if hand2_closeup ran with --body
"""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw

args = sys.argv[1:]


def opt(name, default=None):
    return args[args.index(name)+1] if name in args else default


after = Path(args[0])
before = Path(opt('--before')) if opt('--before') else None
out = Path(opt('--out', str(after)))
out.mkdir(parents=True, exist_ok=True)
rows = int(opt('--rows', '6'))
labels = opt('--labels', 'before,after').split(',')
TILES = ('L-profile', 'L-back', 'R-back', 'R-profile', 'pair')
BG, INK, HOT = '#161a1e', '#a9e6ec', '#ffd27a'


def strips(folder, tag):
    info = json.loads((folder/'clip.json').read_text(encoding='utf-8'))
    res = []
    for n, f in enumerate(info['rendered_frames']):
        ims = [Image.open(folder/f'{t}-{n:03}.png').convert('RGB') for t in TILES]
        w = sum(i.width for i in ims)
        h = max(i.height for i in ims)
        s = Image.new('RGB', (w, h+18), BG)
        d = ImageDraw.Draw(s)
        x = 0
        for t, im in zip(TILES, ims):
            s.paste(im, (x, 18))
            d.text((x+6, 3), t, fill=INK)
            x += im.width
        d.text((w-120, 3), f'{tag}  f{f}', fill=HOT)
        res.append(s)
    return info, res


info, cur = strips(after, labels[-1])
ms = max(60, int(1000/24*2))
cur[0].save(out/'hands.gif', save_all=True, append_images=cur[1:], duration=ms, loop=0)
if before is not None:
    _, old = strips(before, labels[0])
    n = min(len(old), len(cur))
    pairs = []
    for a, b in zip(old[:n], cur[:n]):
        s = Image.new('RGB', (max(a.width, b.width), a.height+b.height), BG)
        s.paste(a, (0, 0))
        s.paste(b, (0, a.height))
        pairs.append(s)
    pairs[0].save(out/'compare.gif', save_all=True, append_images=pairs[1:], duration=ms*2, loop=0)
    pick = sorted({round(i*(n-1)/max(1, rows-1)) for i in range(min(rows, n))})
    w, h = old[0].width+cur[0].width+12, max(old[0].height, cur[0].height)
    sheet = Image.new('RGB', (w, h*len(pick)+24), BG)
    d = ImageDraw.Draw(sheet)
    d.text((6, 5), f"{info['title']}   left: {labels[0]}   right: {labels[-1]}", fill=HOT)
    for r, i in enumerate(pick):
        sheet.paste(old[i], (0, 24+r*h))
        sheet.paste(cur[i], (old[0].width+12, 24+r*h))
    sheet.save(out/'before-after.jpg', quality=88)
body = sorted(after.glob('body-*-[0-9][0-9][0-9].png'))
if body:
    view = body[0].name.split('-')[1]
    ims = [Image.open(p).convert('RGB') for p in body if p.name.split('-')[1] == view]
    ims[0].save(out/'body.gif', save_all=True, append_images=ims[1:], duration=ms, loop=0)
print('HAND2 SHEET', info['title'], len(cur), out, flush=True)
