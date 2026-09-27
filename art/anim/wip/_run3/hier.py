"""Print the deform/FK bone hierarchy (fresh v18, never saved)."""
import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[4]/'tools/blender/anim'
sys.path.insert(0, str(TOOLS))
import hs_anim as H

p = H.open_start()


def walk(b, d=0):
    kids = [c for c in b.children]
    print('HIER', '  '*d+b.name, f'[{len(kids)}]', 'deform' if b.use_deform else '', flush=True)
    for c in kids:
        walk(c, d+1)


for b in p.rig.data.bones:
    if b.parent is None:
        walk(b)
print('FK', sorted(p.fk), flush=True)
