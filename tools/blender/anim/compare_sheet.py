"""Side-by-side panels of shoulder_diag renders (system Python with Pillow).

Run: python tools/blender/anim/compare_sheet.py art/anim/wip/shoulder base fix2 [--match sweep-side90]
Writes <dir>/compare-<tagA>-vs-<tagB>[-<match>].jpg: one row per render name, tag A left, tag B right.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

folder, a, b = Path(sys.argv[1]), sys.argv[2], sys.argv[3]
match = sys.argv[sys.argv.index('--match')+1] if '--match' in sys.argv else ''
rows = []
for pa in sorted(folder.glob(f'{a}-*.png')):
    rest = pa.name[len(a)+1:]
    pb = folder/f'{b}-{rest}'
    if match in rest and pb.exists():
        rows.append((rest[:-4], pa, pb))
if not rows:
    raise SystemExit('no matching pairs')
w, h = Image.open(rows[0][1]).size
sheet = Image.new('RGB', (2*w, len(rows)*h), '#161a1e')
d = ImageDraw.Draw(sheet)
for i, (name, pa, pb) in enumerate(rows):
    sheet.paste(Image.open(pa).convert('RGB'), (0, i*h))
    sheet.paste(Image.open(pb).convert('RGB'), (w, i*h))
    d.text((6, i*h+6), f'{a} {name}', fill='#ffe08a')
    d.text((w+6, i*h+6), f'{b} {name}', fill='#a9e6ec')
dst = folder/f"compare-{a}-vs-{b}{'-'+match if match else ''}.jpg"
sheet.save(dst, quality=88)
print('WROTE', dst, len(rows), 'rows')
