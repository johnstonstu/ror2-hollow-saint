"""Tabard bone parents, rest head/tail, axes. blender -b --factory-startup --python tabard_bones.py"""
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
import hs_anim as H

p = H.open_start()
for n in H.TABARD_FRONT+H.TABARD_BACK:
    b = p.bones[n]
    m = p.rest[n].to_3x3()
    print('TB', n, 'parent', b.parent.name, 'head', tuple(round(v, 3) for v in b.head_local),
          'tail', tuple(round(v, 3) for v in b.tail_local), 'x', tuple(round(v, 2) for v in m.col[0]),
          'z', tuple(round(v, 2) for v in m.col[2]), 'deform', b.use_deform)
