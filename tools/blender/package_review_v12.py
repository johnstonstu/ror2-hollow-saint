"""Normal Python + Pillow: concept vs v11 vs v12 pauldron comparison sheet and review page."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[2]
HY=ROOT/'art/hybrid'
V11=HY/'v11'
V12=HY/'v12'
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


pairs=[('v11 hero (before)',V11/'hollow-saint-hybrid-v11-hero.png'),('v12 hero',V12/'hollow-saint-hybrid-v12-hero.png'),
       ('v11 bust (before)',V11/'hollow-saint-hybrid-v11-bust.png'),('v12 bust',V12/'hollow-saint-hybrid-v12-bust.png'),
       ('v11 back (before)',V11/'hollow-saint-hybrid-v11-back.png'),('v12 back',V12/'hollow-saint-hybrid-v12-back.png'),
       ('v12 Arc Step f9',V12/'check-arc-step-f9-hero.png')]
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
    draw.text((x,y),label,fill=(120,220,255) if 'v12' in label else (200,200,200),font=f)
    sheet.paste(im,(x,y+label_h))
    x+=im.width+gap
sheet.save(V12/'comparison.jpg',quality=90)

views=['front','hero','side','back','gameplay-distance','bust','pose-arc-bolt']
cards='\n'.join(f'<figure><img src="hollow-saint-hybrid-v12-{v}.png" alt="v12 {v}"><figcaption>{v}</figcaption></figure>' for v in views)
checks='\n'.join(f'<figure><img src="{p.name}" alt="{p.stem}"><figcaption>{p.stem}</figcaption></figure>' for p in sorted(V12.glob('check-*.png')))
(V12/'review.html').write_text(f'''<!doctype html>
<html><head><meta charset="utf-8"><title>Hollow Saint v12 pauldron pass</title>
<style>body{{background:#222;color:#ddd;font-family:Segoe UI,Arial,sans-serif;margin:24px}}
.grid{{display:flex;flex-wrap:wrap;gap:16px}}figure{{margin:0}}img{{max-height:560px;border:1px solid #444}}
figcaption{{text-align:center;padding:4px;color:#8cf}}h1{{font-weight:600}}</style></head>
<body><h1>Hollow Saint hybrid v12 &mdash; broad ceramic pauldrons</h1>
<p>Built from v11 (rig, six HS_v10 actions and all v11 detail retained). Blender studio renders only; no game test.</p>
<h2>Concept vs v11 vs v12</h2><img src="comparison.jpg" style="max-height:none;max-width:100%">
<h2>v12 views</h2><div class="grid">{cards}</div>
<h2>Pose checks (worst pauldron QA frames)</h2><div class="grid">{checks}</div></body></html>''',encoding='utf-8')
print('PACKAGED',V12/'comparison.jpg')
