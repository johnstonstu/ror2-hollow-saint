"""Normal Python + Pillow: concept vs before vs after polish comparison sheet and review page.

Usage: python tools/blender/package_review_polish.py v12 v14 "v13 pauldrons + v14 concept palette"
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[2]
HY=ROOT/'art/hybrid'
BEFORE,AFTER=sys.argv[1],sys.argv[2]
TITLE=sys.argv[3] if len(sys.argv)>3 else f'{AFTER} polish pass'
B,A=HY/BEFORE,HY/AFTER
H=700


def load(path,height=H):
    im=Image.open(path).convert('RGB')
    return im.resize((round(im.width*height/im.height),height),Image.LANCZOS)


def font(size):
    for name in ('segoeuib.ttf','arialbd.ttf','DejaVuSans-Bold.ttf'):
        try:
            return ImageFont.truetype(name,size)
        except OSError:
            continue
    return ImageFont.load_default()


def shot(folder,version,view):
    return folder/f'hollow-saint-hybrid-{version}-{view}.png'


pairs=[(f'{BEFORE} hero (before)',shot(B,BEFORE,'hero')),(f'{AFTER} hero',shot(A,AFTER,'hero')),
       (f'{BEFORE} bust (before)',shot(B,BEFORE,'bust')),(f'{AFTER} bust',shot(A,AFTER,'bust')),
       (f'{BEFORE} back (before)',shot(B,BEFORE,'back')),(f'{AFTER} back',shot(A,AFTER,'back'))]
tiles=[(label,load(path)) for label,path in pairs]
concepts=[load(ROOT/'art/concepts/hollow-saint-a-orthographic-v1.png',520),
          load(ROOT/'art/concepts/hollow-saint-a-detail-studies-v1.png',520)]
gap,label_h=16,44
row_w=sum(im.width for _,im in tiles)+gap*(len(tiles)+1)
concept_w=sum(c.width for c in concepts)+gap
width=max(row_w,concept_w+2*gap)
height=gap+label_h+520+gap+label_h+H+gap
sheet=Image.new('RGB',(width,height),(34,34,36))
draw=ImageDraw.Draw(sheet)
f=font(26)
draw.text((gap,gap),'Target: Hollow Saint A concept sheets',fill=(235,235,235),font=f)
x=(width-concept_w)//2
for c in concepts:
    sheet.paste(c,(x,gap+label_h))
    x+=c.width+gap
y=gap+label_h+520+gap
x=gap
for label,im in tiles:
    draw.text((x,y),label,fill=(120,220,255) if AFTER in label else (200,200,200),font=f)
    sheet.paste(im,(x,y+label_h))
    x+=im.width+gap
sheet.save(A/'comparison.jpg',quality=90)

views=[p.stem.replace(f'hollow-saint-hybrid-{AFTER}-','') for p in sorted(A.glob(f'hollow-saint-hybrid-{AFTER}-*.png'))]
cards='\n'.join(f'<figure><img src="hollow-saint-hybrid-{AFTER}-{v}.png" alt="{AFTER} {v}"><figcaption>{v}</figcaption></figure>' for v in views)
(A/'review.html').write_text(f'''<!doctype html>
<html><head><meta charset="utf-8"><title>Hollow Saint {AFTER}</title>
<style>body{{background:#222;color:#ddd;font-family:Segoe UI,Arial,sans-serif;margin:24px}}
.grid{{display:flex;flex-wrap:wrap;gap:16px}}figure{{margin:0}}img{{max-height:560px;border:1px solid #444}}
figcaption{{text-align:center;padding:4px;color:#8cf}}h1{{font-weight:600}}</style></head>
<body><h1>Hollow Saint hybrid {AFTER} &mdash; {TITLE}</h1>
<p>Rig and six HS_v10 actions retained. Blender studio renders only; no game test.</p>
<h2>Concept vs {BEFORE} vs {AFTER}</h2><img src="comparison.jpg" style="max-height:none;max-width:100%">
<h2>{AFTER} views</h2><div class="grid">{cards}</div></body></html>''',encoding='utf-8')
print('PACKAGED',A/'comparison.jpg')
