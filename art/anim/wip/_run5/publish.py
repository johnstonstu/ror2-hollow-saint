"""GIFs + review strips for finished transition renders, copied to transitions/gifs/<pair>-{hero,chase}.gif and
<pair>-review.jpg (frames stay in the git-ignored transitions/temp/).  A render is finished once its
'STITCH DONE <name>' line is in the log.   python publish.py <render log> [--force]"""
import re
import shutil
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageSequence

WIP = Path(__file__).resolve().parents[1]
ROOT = WIP.parents[2]
TEMP, GIFS = WIP/'transitions/temp', WIP/'transitions/gifs'
GIFS.mkdir(parents=True, exist_ok=True)
raw = Path(sys.argv[1]).read_bytes()
SCALE = 0.6       # published GIFs (git LFS) are downscaled; full size stays in temp/


def small(src, dst):
    im = Image.open(src)
    frames, durations = [], []
    for fr in ImageSequence.Iterator(im):
        durations.append(fr.info.get('duration', 42))
        frames.append(fr.convert('RGB').resize((int(im.width*SCALE), int(im.height*SCALE)), Image.LANCZOS)
                      .quantize(colors=128, method=Image.MEDIANCUT))
    frames[0].save(dst, save_all=True, append_images=frames[1:], duration=durations, loop=0, optimize=True)


log = raw.decode('utf-16', errors='replace') if raw[:2] in (b'\xff\xfe', b'\xfe\xff') else raw.decode('utf-8', 'replace')
done = re.findall(r'^STITCH DONE (\S+)', log, re.M)
n = 0
for name in done:
    src = TEMP/name
    if not src.exists() or ((GIFS/f'{name}-review.jpg').exists() and '--force' not in sys.argv):
        continue
    if not (src/'hero.gif').exists():
        subprocess.run([sys.executable, str(ROOT/'tools/blender/anim/sheet.py'), str(src), '--cols', '12', '--scale',
                        '0.35'], check=True, capture_output=True)
    if not (src/'review.jpg').exists():
        subprocess.run([sys.executable, str(Path(__file__).with_name('review_strips.py')), str(src), '--scale', '0.5'],
                       check=True, capture_output=True)
    for v in ('hero', 'chase'):
        if (src/f'{v}.gif').exists():
            small(src/f'{v}.gif', GIFS/f'{name}-{v}.gif')
    shutil.copyfile(src/'review.jpg', GIFS/f'{name}-review.jpg')
    n += 1
print(f'published {n} (finished {len(done)})')
