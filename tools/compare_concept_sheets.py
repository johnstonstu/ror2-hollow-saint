"""Concept-vs-render comparison sheets. Usage: python tools/compare_concept_sheets.py v17 [v15]"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
VER=sys.argv[1] if len(sys.argv)>1 else 'v17'
PREV=sys.argv[2] if len(sys.argv)>2 else None
C=ROOT/'art/concepts'
OUT=ROOT/f'art/hybrid/{VER}'
font=ImageFont.truetype('segoeuib.ttf',28)


def render(ver,shot):
    return Image.open(ROOT/f'art/hybrid/{ver}/hollow-saint-hybrid-{ver}-{shot}.png').convert('RGB')


def fit(im,h):
    return im.resize((round(im.width*h/im.height),h),Image.LANCZOS)


def crop(im,box,ref=1024):
    # Crop boxes are authored against a 1024 px wide preview of each concept sheet.
    k=im.width/ref
    return im.crop(tuple(round(v*k) for v in box))


def sheet(pairs,out,h=700):
    ims=[(label,fit(im,h)) for label,im in pairs]
    width=sum(i.width for _,i in ims)+20*(len(ims)+1)
    s=Image.new('RGB',(width,h+70),(34,34,36))
    d=ImageDraw.Draw(s)
    x=20
    for label,im in ims:
        d.text((x,12),label,fill=(230,230,230) if label.startswith('Concept') else (120,220,255),font=font)
        s.paste(im,(x,55))
        x+=im.width+20
    s.save(out,quality=90)
    print('wrote',out)


ortho=Image.open(C/'hollow-saint-a-orthographic-v1.png').convert('RGB')
eight=Image.open(C/'hollow-saint-a-eight-angle-v1.png').convert('RGB')
detail=Image.open(C/'hollow-saint-a-detail-studies-v1.png').convert('RGB')
body=(150,120,960,1320)
sheet([('Concept front',crop(ortho,(40,35,265,450))),(f'{VER} front',render(VER,'front').crop(body)),
       ('Concept back',crop(ortho,(520,35,760,450))),(f'{VER} back',render(VER,'back').crop(body))],OUT/'compare-front-back.jpg')
sheet([('Concept 45 deg',crop(eight,(300,30,470,320))),(f'{VER} hero',render(VER,'hero').crop((180,120,940,1320))),
       ('Concept side',crop(ortho,(320,35,460,450))),(f'{VER} side',render(VER,'side').crop((300,120,820,1320)))],OUT/'compare-angles.jpg')
sheet([('Concept bust',crop(detail,(10,55,350,660))),(f'{VER} bust',render(VER,'bust'))],OUT/'compare-bust.jpg')
if PREV:
    sheet([(f'{PREV} hero',render(PREV,'hero').crop((180,120,940,1320))),(f'{VER} hero',render(VER,'hero').crop((180,120,940,1320))),
           (f'{PREV} bust',render(PREV,'bust')),(f'{VER} bust',render(VER,'bust'))],OUT/f'before-after-{PREV}.jpg')
