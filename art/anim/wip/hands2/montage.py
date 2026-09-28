"""Scratch (hands2): tile PNGs into one labelled image.  python montage.py out.png cols img1 img2 ..."""
import sys
from PIL import Image, ImageDraw

out, cols, paths = sys.argv[1], int(sys.argv[2]), sys.argv[3:]
ims = [Image.open(p).convert('RGB') for p in paths]
w = max(i.width for i in ims)
h = max(i.height for i in ims)
rows = (len(ims)+cols-1)//cols
sheet = Image.new('RGB', (w*cols, (h+16)*rows), (30, 30, 30))
d = ImageDraw.Draw(sheet)
for k, (im, p) in enumerate(zip(ims, paths)):
    x, y = (k % cols)*w, (k//cols)*(h+16)
    sheet.paste(im, (x, y+16))
    d.text((x+4, y+2), p.replace('\\', '/').split('/')[-1], fill=(230, 230, 230))
sheet.save(out)
print('MONTAGE', out, sheet.size)
