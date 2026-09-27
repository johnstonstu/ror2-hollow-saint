"""Contact sheet of every preview PNG (previews/_overview.png)."""
import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from hs_io import fresh, log  # noqa: E402

PRE = os.path.join(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')), 'previews')
names = sorted(n for n in os.listdir(PRE) if n.endswith('.png') and not n.startswith('_'))
T, cols = 256, 6
rows = (len(names) + cols - 1) // cols
sheet = Image.new('RGB', (cols * T, rows * (T + 14)), (10, 11, 15))
d = ImageDraw.Draw(sheet)
for i, n in enumerate(names):
    im = Image.open(os.path.join(PRE, n)).convert('RGB')
    im.thumbnail((T, T))
    r, c = divmod(i, cols)
    x, y = c * T, r * (T + 14)
    sheet.paste(im, (x + (T - im.width) // 2, y + 14 + (T - im.height) // 2))
    d.text((x + 3, y + 1), n[:40], fill=(150, 170, 185))
sheet.save(fresh(os.path.join(PRE, '_overview.png')))
log(f'overview contact sheet: {len(names)} previews')
