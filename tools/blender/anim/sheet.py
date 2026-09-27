"""Tile preview frames into contact sheets and GIFs (system Python with Pillow).

Run: python tools/blender/anim/sheet.py art/anim/wip/run/run-forward [--cols 8] [--scale 0.5]
Writes <view>-sheet.jpg and <view>.gif next to the frames.
"""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw

folder = Path(sys.argv[1])
args = sys.argv[2:]
cols = int(args[args.index('--cols')+1]) if '--cols' in args else 8
scale = float(args[args.index('--scale')+1]) if '--scale' in args else 0.5
info = json.loads((folder/'clip.json').read_text()) if (folder/'clip.json').exists() else {}
frames_list = info.get('rendered_frames')
step = info.get('step', 1)
for view in sorted({p.name.split('-')[0] for p in folder.glob('*-[0-9][0-9][0-9].png')}):
    paths = sorted(folder.glob(f'{view}-[0-9][0-9][0-9].png'))
    ims = [Image.open(p).convert('RGB') for p in paths]
    w, h = int(ims[0].width*scale), int(ims[0].height*scale)
    rows = (len(ims)+cols-1)//cols
    sheet = Image.new('RGB', (cols*w, rows*h), '#161a1e')
    d = ImageDraw.Draw(sheet)
    for i, im in enumerate(ims):
        x, y = (i % cols)*w, (i//cols)*h
        sheet.paste(im.resize((w, h)), (x, y))
        label = f'f{frames_list[i]}' if frames_list and i < len(frames_list) else str(i)
        d.text((x+6, y+6), label, fill='#a9e6ec')
    sheet.save(folder/f'{view}-sheet.jpg', quality=90)
    ms = int(1000/24*step)
    ims[0].save(folder/f'{view}.gif', save_all=True, append_images=ims[1:], duration=ms, loop=0)
    print('SHEET', folder/f'{view}-sheet.jpg', len(ims))
