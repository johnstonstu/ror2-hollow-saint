import sys
from pathlib import Path
TOOLS = Path(r"C:\Users\stuwj\Documents\Coding\ror2-lightning\tools\blender\anim")
sys.path.insert(0, str(TOOLS))
sys.path.insert(0, str(TOOLS/'clips'))
import hs_anim as H
import primary

p = H.open_start()
for label, args in (('rest', (0, 0, 0, 0)), ('point', (-4, 1.0, 80, 34)), ('neg', (-30, 0, 0, 0)), ('pos', (30, 0, 0, 0))):
    for side in ('R', 'L'):
        p.reset()
        primary.fingers(p, side, *args)
        p.update()
        hand = p.world(f'{side} hand').to_3x3()
        hy = hand.col[1].normalized()
        rows = [f'{n}: ' + ','.join(f'{v:+.2f}' for v in p.world(f'{side} {n}').to_3x3().col[1].normalized())
                for n in ('index.1', 'index.2', 'index.3', 'muzzle')]
        ang = hy.angle(p.world(f'{side} muzzle').to_3x3().col[1])
        print('FING', label, side, 'hand', ','.join(f'{v:+.2f}' for v in hy), '| hand x', ','.join(f'{v:+.2f}' for v in hand.col[0]),
              '| hand z', ','.join(f'{v:+.2f}' for v in hand.col[2]), '|', ' | '.join(rows), f'| muzzle-hand {ang*57.3:.1f}', flush=True)
