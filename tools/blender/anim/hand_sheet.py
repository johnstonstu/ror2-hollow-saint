"""Tile hand_closeup.py frames: one row per frame of [L back | L palm | R back | R palm]
(+ [L context | R context] when hand_closeup.py ran with --context).

Run: python tools/blender/anim/hand_sheet.py art/anim/wip/hands/<clip-slug> [--cols 6] [--scale 0.5]
Writes hands.gif (all four views per frame, full size) and hands-sheet.jpg (L+R back views per frame).
"""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw

folder = Path(sys.argv[1])
args = sys.argv[2:]
cols = int(args[args.index('--cols')+1]) if '--cols' in args else 3
scale = float(args[args.index('--scale')+1]) if '--scale' in args else 0.5
info = json.loads((folder/'clip.json').read_text())
frames = info['rendered_frames']
accents = {a+d for a in info.get('finger_accents', []) for d in (-1, 0, 1)}
TILES = ('L-back', 'L-palm', 'R-back', 'R-palm')
if (folder/'L-context-000.png').exists():
    TILES = ('L-context', 'L-back', 'L-palm', 'R-back', 'R-palm', 'R-context')
strips = []
for n, f in enumerate(frames):
    ims = [Image.open(folder/f'{t}-{n:03}.png').convert('RGB') for t in TILES]
    w, h = ims[0].size
    strip = Image.new('RGB', (len(TILES)*w, h+18), '#161a1e')
    d = ImageDraw.Draw(strip)
    for i, (t, im) in enumerate(zip(TILES, ims)):
        strip.paste(im, (i*w, 18))
        d.text((i*w+6, 3), t, fill='#a9e6ec')
    d.text((len(TILES)*w-60, 3), f'f{f}'+(' *' if f in accents else ''), fill='#ffd27a' if f in accents else '#a9e6ec')
    strips.append(strip)
ms = int(1000/24*info.get('step', 1))
strips[0].save(folder/'hands.gif', save_all=True, append_images=strips[1:], duration=ms, loop=0)
w, h = int(strips[0].width*scale), int(strips[0].height*scale)
rows = (len(strips)+cols-1)//cols
sheet = Image.new('RGB', (min(cols, len(strips))*w, rows*h), '#161a1e')
for i, s in enumerate(strips):
    sheet.paste(s.resize((w, h)), ((i % cols)*w, (i//cols)*h))
sheet.save(folder/'hands-sheet.jpg', quality=88)
print('HAND SHEET', folder, len(strips))
