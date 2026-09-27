"""Rest foot geometry: bone heads/tails and the body-mesh heel extent per side (fresh v18, never saved)."""
import sys
from pathlib import Path
from mathutils import Vector

TOOLS = Path(__file__).resolve().parents[4]/'tools/blender/anim'
sys.path.insert(0, str(TOOLS))
import hs_anim as H

p = H.open_start()
p.reset()
p.update()
fmt = lambda v: '(' + ', '.join(f'{c:+.3f}' for c in v) + ')'
for b in p.rig.data.bones:
    if b.name.startswith('L ') and any(k in b.name for k in ('foot', 'toe', 'heel', 'shin', 'ankle')):
        print('BONE', b.name, 'head', fmt(b.head_local), 'tail', fmt(b.tail_local), 'parent', b.parent.name if b.parent else None)
body = __import__('bpy').data.objects[H.BODY]
mw = body.matrix_world
vg = {g.index: g.name for g in body.vertex_groups}
pts = []
for v in body.data.vertices:
    names = [vg[g.group] for g in v.groups if g.weight > 0.5]
    if any(n in ('L foot', 'L toe') for n in names):
        pts.append((mw @ v.co, names[0]))
if pts:
    for i, lab in ((1, 'max y (back)'), (1, None)):
        pass
    back = max(pts, key=lambda q: q[0].y)
    low = min(pts, key=lambda q: q[0].z)
    front = min(pts, key=lambda q: q[0].y)
    print('MESH heel-most', fmt(back[0]), back[1], 'lowest', fmt(low[0]), 'front', fmt(front[0]))
    hb = [q[0] for q in pts if q[0].y > back[0].y-0.03]
    c = sum(hb, Vector())/len(hb)
    print('MESH heel cap centre', fmt(c), 'n', len(hb), 'zmin', round(min(q.z for q in hb), 3), 'zmax', round(max(q.z for q in hb), 3))
