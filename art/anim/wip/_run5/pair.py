"""Full-size before/after rows for chosen frame indices of a pad_closeup clip.
Run: python art/anim/wip/_run5/pair.py <before_tag> <after_tag> <clip-slug> <i,j,..> [views]"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

W = Path(__file__).resolve().parents[1]/'shoulders'
b, a, clip, idx = sys.argv[1:5]
views = sys.argv[5].split(',') if len(sys.argv) > 5 else ['front', 'back', 'top', 'outL', 'outR']
rows = []
for i in map(int, idx.split(',')):
    for tag in (b, a):
        ims = [Image.open(W/tag/clip/f'{v}-{i:03}.png').convert('RGB') for v in views]
        w, h = ims[0].size
        r = Image.new('RGB', (w*len(ims), h), '#161a1e')
        for k, im in enumerate(ims):
            r.paste(im, (k*w, 0))
        ImageDraw.Draw(r).text((5, 5), f'{tag} {clip} #{i}', fill='#ffd27a')
        rows.append(r)
out = Image.new('RGB', (rows[0].width, sum(r.height for r in rows)))
y = 0
for r in rows:
    out.paste(r, (0, y))
    y += r.height
p = W/a/clip/f'pair-{idx.replace(",", "_")}.jpg'
out.save(p, quality=88)
print(p)
