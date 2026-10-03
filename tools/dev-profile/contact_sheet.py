"""Build labelled contact sheets from an autopilot run: one row per shot, back/front/side.
Usage: python contact_sheet.py <run folder> [prefix ...]"""
import sys, os, glob
from PIL import Image, ImageDraw
run = sys.argv[1]; prefixes = sys.argv[2:]
shots = sorted({os.path.basename(p).rsplit('_', 1)[0] for p in glob.glob(os.path.join(run, '*_back.png'))})
if prefixes: shots = [s for s in shots if any(s.startswith(p) for p in prefixes)]
W, H, L = 400, 225, 150
sheet = Image.new('RGB', (L + 3 * W, len(shots) * H), (16, 18, 22))
d = ImageDraw.Draw(sheet)
for row, shot in enumerate(shots):
    d.text((8, row * H + 8), shot, fill=(230, 230, 230))
    for col, view in enumerate(('back', 'front', 'side')):
        p = os.path.join(run, shot + '_' + view + '.png')
        if os.path.exists(p): sheet.paste(Image.open(p).convert('RGB').resize((W, H)), (L + col * W, row * H))
name = 'sheet-' + ('-'.join(prefixes) if prefixes else 'all') + '.jpg'
sheet.save(os.path.join(run, name), quality=85); print(name, len(shots), 'rows')
