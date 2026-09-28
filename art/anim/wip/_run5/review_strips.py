"""Per-handoff review strips for stitched transitions: for every handoff in stitch.json, frames -4..+5 of each
view as one labelled row (hero row, then chase row), cropped to the character. Writes <folder>/review.jpg.
python review_strips.py <folder> [...] [--scale 0.6]"""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw

args = sys.argv[1:]
scale = float(args[args.index('--scale')+1]) if '--scale' in args else 0.6
only = args[args.index('--views')+1].split(',') if '--views' in args else ('hero', 'chase')
name = args[args.index('--name')+1] if '--name' in args else 'review.jpg'
valued = {args[i+1] for i, a in enumerate(args) if a in ('--scale', '--views', '--name')}
folders = [Path(a) for a in args if not a.startswith('--') and a not in valued]
BEFORE, AFTER = 4, 5
CROP = {'hero': (0.12, 0.0, 0.88, 0.96), 'chase': (0.3, 0.3, 0.7, 0.8)}   # fractions of the frame


def tile(folder, view, n):
    p = folder/f'{view}-{n:03}.png'
    if not p.exists():
        return None
    im = Image.open(p).convert('RGB')
    x0, y0, x1, y1 = CROP.get(view, (0, 0, 1, 1))
    im = im.crop((int(x0*im.width), int(y0*im.height), int(x1*im.width), int(y1*im.height)))
    return im.resize((int(im.width*scale), int(im.height*scale)))


for folder in folders:
    st = json.loads((folder/'stitch.json').read_text(encoding='utf-8'))
    views = [v for v in only if (folder/f'{v}-000.png').exists()]
    rows = []
    for h in st['handoffs']:
        c = h['frame']-1                                  # 0-based index of the first frame of the new segment
        for v in views:
            ims = [(n, tile(folder, v, n)) for n in range(c-BEFORE, c+AFTER+1)]
            ims = [(n, im) for n, im in ims if im]
            if ims:
                rows.append((f"f{h['frame']} {h['from']} -> {h['to']}  b{h['blend']}  [{v}]", ims, c))
    if not rows:
        continue
    tw = max(im.width for _, ims, _ in rows for _, im in ims)
    th = max(im.height for _, ims, _ in rows for _, im in ims)
    W, H = tw*(BEFORE+AFTER+1), (th+16)*len(rows)
    sheet = Image.new('RGB', (W, H), '#161a1e')
    d = ImageDraw.Draw(sheet)
    for r, (title, ims, c) in enumerate(rows):
        y = r*(th+16)
        d.text((4, y+2), title[:160], fill='#a9e6ec')
        for n, im in ims:
            x = (n-(c-BEFORE))*tw
            sheet.paste(im, (x, y+16))
            d.text((x+4, y+18), f'{n+1}', fill='#ffd27a' if n == c else '#a9e6ec')
    sheet.save(folder/name, quality=88)
    print('REVIEW', folder, len(rows), 'rows')
