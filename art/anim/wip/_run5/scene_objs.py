import sys
from pathlib import Path
root = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(root/'tools/blender/anim'))
import bpy
import hs_anim as H
p = H.open_start()
for o in bpy.data.objects:
    if '|' not in o.name:
        print('OBJ', o.type, repr(o.name), tuple(round(v, 2) for v in o.location), tuple(round(v, 2) for v in o.dimensions), [m.name for m in getattr(o.data, 'materials', [])][:2] if o.type == 'MESH' else '')
