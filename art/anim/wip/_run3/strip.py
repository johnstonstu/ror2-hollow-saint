"""Tile inspect frames side by side: python strip.py <clip-dir-rel-to-wip> <view> <n> [out-name]"""
import sys
from pathlib import Path
from PIL import Image

WIP = Path(__file__).resolve().parents[1]
clip, view, n = sys.argv[1], sys.argv[2], int(sys.argv[3])
name = sys.argv[4] if len(sys.argv) > 4 else f"strip-{clip.replace('/', '-')}-{view}"
ims = [Image.open(WIP/clip/'inspect'/f'{view}-{i:03d}.png').convert('RGB') for i in range(n)]
w, h = ims[0].size
s = Image.new('RGB', (w*n, h))
for i, im in enumerate(ims):
    s.paste(im, (i*w, 0))
s = s.resize((w*n*2//3, h*2//3))
out = Path(__file__).parent/f'{name}.png'
s.save(out)
print(out)
