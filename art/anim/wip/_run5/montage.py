"""Self-review montages: every handoff of every finished render as one hero row (frames -4..+5, handoff frame
labelled in yellow), ROWS rows per image -> transitions/temp/_montage/NNN.jpg (+ index.txt: image -> handoffs).
python montage.py <render log> [--rows 5] [--final] [--force]"""
import json
import re
import sys
from pathlib import Path
from PIL import Image, ImageDraw

WIP = Path(__file__).resolve().parents[1]
TEMP = WIP/'transitions/temp'
OUT = TEMP/'_montage'
OUT.mkdir(parents=True, exist_ok=True)
ROWS = int(sys.argv[sys.argv.index('--rows')+1]) if '--rows' in sys.argv else 5
BEFORE, AFTER, SCALE = 4, 5, 0.4
CROP = (0.12, 0.0, 0.88, 0.96)
raw = Path(sys.argv[1]).read_bytes()
log = raw.decode('utf-16', errors='replace') if raw[:2] in (b'\xff\xfe', b'\xfe\xff') else raw.decode('utf-8', 'replace')
done = sorted(set(re.findall(r'^STITCH DONE (\S+)', log, re.M)))


def tile(folder, n):
    p = folder/f'hero-{n:03}.png'
    if not p.exists():
        return None
    im = Image.open(p).convert('RGB')
    im = im.crop((int(CROP[0]*im.width), int(CROP[1]*im.height), int(CROP[2]*im.width), int(CROP[3]*im.height)))
    return im.resize((int(im.width*SCALE), int(im.height*SCALE)))


rows = []
for name in done:
    st = json.loads((TEMP/name/'stitch.json').read_text(encoding='utf-8'))
    for h in st['handoffs']:
        rows.append((name, h))
index = []
for k in range(0, len(rows), ROWS):
    group = rows[k:k+ROWS]
    dst = OUT/f'{k//ROWS:03}.jpg'
    index.append(f"{dst.name}: "+'; '.join(f"{n} f{h['frame']}" for n, h in group))
    if (dst.exists() and '--force' not in sys.argv) or (len(group) < ROWS and '--final' not in sys.argv):
        continue
    strips = []
    for name, h in group:
        c = h['frame']-1
        strips.append((f"{name}  f{h['frame']}  {h['from']} -> {h['to']}  b{h['blend']}",
                       [(n, tile(TEMP/name, n)) for n in range(c-BEFORE, c+AFTER+1)], c))
    tw = max(im.width for _, ims, _ in strips for _, im in ims if im)
    th = max(im.height for _, ims, _ in strips for _, im in ims if im)
    sheet = Image.new('RGB', (tw*(BEFORE+AFTER+1), (th+16)*len(strips)), '#161a1e')
    d = ImageDraw.Draw(sheet)
    for r, (title, ims, c) in enumerate(strips):
        y = r*(th+16)
        d.text((4, y+2), title[:170], fill='#a9e6ec')
        for n, im in ims:
            if im:
                x = (n-(c-BEFORE))*tw
                sheet.paste(im, (x, y+16))
                d.text((x+4, y+18), f'{n+1}', fill='#ffd27a' if n == c else '#a9e6ec')
    sheet.save(dst, quality=85)
(OUT/'index.txt').write_text('\n'.join(index)+'\n', encoding='utf-8')
print(len(rows), 'handoffs,', len(index), 'montages')
