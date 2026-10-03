"""Package rendered fitted-spear previews; no in-game claim."""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
full = '--full' in sys.argv
SRC = ROOT / ('artifacts/spear-v37-review01/flow03' if full else 'artifacts/spear-v37-review01/keys02')
OUT = ROOT / 'art/anim/v37'
font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 18)
small = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 14)
frames = []
for age in range(0, 139 if full else 64, 2):
    image = Image.new('RGB', (672, 838), '#192127')
    draw = ImageDraw.Draw(image)
    phase = 'Held sway' if age < 16 else 'Raise the spear' if age < 25 else 'Crackling fan' if age < 50 else 'Relax back into carry' if age < 63 else 'Throw' if age < 83 else 'Free-hand Arc Bolt' if age < 103 else 'Recall + catch' if age < 123 else 'Settle into carry'
    draw.text((14, 7), 'Fitted spear / ' + phase, font=font, fill='#d8f8ff')
    draw.text((14, 34), 'Blender motion + VFX study • not an in-game capture', font=small, fill='#93abb6')
    for row, move in enumerate(('standing', 'running')):
        for col, view in enumerate(('rear', 'side')):
            shot = Image.open(SRC / move / f'{view}-{age:03}.png').resize((336, 384))
            image.paste(shot, (col * 336, 60 + row * 389))
            draw.text((col * 336 + 10, 65 + row * 389), move.title() + ' / ' + view, font=small, fill='#13262c')
    frames.append(image)
target = OUT / ('spear-flow-standing-running.gif' if full else 'held-fan-standing-running.gif')
assert not target.exists(), 'Preserve existing review'
frames[0].save(target, save_all=True, append_images=frames[1:], duration=83, loop=0, optimize=True)
print(target)
