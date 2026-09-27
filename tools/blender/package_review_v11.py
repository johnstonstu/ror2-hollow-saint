"""Normal Python + Pillow: concept vs before (v7/v9) vs v11 comparison sheet and review page."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[2]
HY=ROOT/'art/hybrid'
V11=HY/'v11'
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


pairs=[('v7 hero (before)',HY/'hollow-saint-hybrid-v7-hero.png'),('v11 hero',V11/'hollow-saint-hybrid-v11-hero.png'),
       ('v9 back (before)',HY/'v9-review-back.png'),('v11 back',V11/'hollow-saint-hybrid-v11-back.png'),
       ('v11 bust',V11/'hollow-saint-hybrid-v11-bust.png'),('v11 Arc Bolt pose',V11/'hollow-saint-hybrid-v11-pose-arc-bolt.png')]
tiles=[(label,load(path)) for label,path in pairs]
concept=load(ROOT/'art/concepts/hollow-saint-a-orthographic-v1.png',520)
gap,label_h=16,44
row_w=sum(im.width for _,im in tiles)+gap*(len(tiles)+1)
width=max(row_w,concept.width+2*gap)
height=gap+label_h+concept.height+gap+label_h+H+gap
sheet=Image.new('RGB',(width,height),(34,34,36))
draw=ImageDraw.Draw(sheet)
f=font(26)
draw.text((gap,gap),'Target: Hollow Saint A concept (orthographic reference)',fill=(235,235,235),font=f)
sheet.paste(concept,((width-concept.width)//2,gap+label_h))
y=gap+label_h+concept.height+gap
x=gap
for label,im in tiles:
    draw.text((x,y),label,fill=(120,220,255) if 'v11' in label else (200,200,200),font=f)
    sheet.paste(im,(x,y+label_h))
    x+=im.width+gap
sheet.save(V11/'comparison.jpg',quality=90)

views=['front','hero','side','back','gameplay-distance','bust','pose-arc-bolt']
cards='\n'.join(f'<figure><img src="hollow-saint-hybrid-v11-{v}.png" alt="v11 {v}"><figcaption>{v}</figcaption></figure>' for v in views)
(V11/'review.html').write_text(f'''<!doctype html>
<html><head><meta charset="utf-8"><title>Hollow Saint v11 fidelity pass</title>
<style>body{{background:#222;color:#ddd;font-family:Segoe UI,Arial,sans-serif;margin:24px}}
.grid{{display:flex;flex-wrap:wrap;gap:16px}}figure{{margin:0}}img{{max-height:560px;border:1px solid #444}}
figcaption{{text-align:center;padding:4px;color:#8cf}}h1{{font-weight:600}}</style></head>
<body><h1>Hollow Saint hybrid v11 &mdash; fidelity pass toward LEFT A</h1>
<p>Built from v10 (rig and six HS_v10 actions retained). Blender studio renders only; no game test.</p>
<h2>Concept vs before vs v11</h2><img src="comparison.jpg" style="max-height:none;max-width:100%">
<h2>v11 views</h2><div class="grid">{cards}</div></body></html>''',encoding='utf-8')
print('PACKAGED',V11/'comparison.jpg')
