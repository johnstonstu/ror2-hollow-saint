"""S9 report: is the body mesh mirror-symmetric about MID_X where the L/R leg and arm bones sit?
For each L/R deform bone pair: mesh mirror error of the vertices weighted to it (nearest mirrored
vertex distance), plus how far each bone head sits from its own skin centroid.
blender -b --factory-startup --python mesh_sym.py -- out.json"""
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector
from mathutils.kdtree import KDTree

sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
import hs_anim as H

out = Path(sys.argv[sys.argv.index('--')+1])
p = H.open_start()
rig = p.rig
body = bpy.data.objects[H.BODY]
mw = body.matrix_world
mid = H.MID_X
co = [mw @ v.co for v in body.data.vertices]
kd = KDTree(len(co))
for i, c in enumerate(co):
    kd.insert(c, i)
kd.balance()
names = {g.index: g.name for g in body.vertex_groups}
by_group = {}
for v in body.data.vertices:
    for g in v.groups:
        if g.weight > 0.5:
            by_group.setdefault(names[g.group], []).append(v.index)


def mirror(c):
    return Vector((2*mid-c.x, c.y, c.z))


rows = {}
for b in rig.data.bones:
    if not b.name.startswith('L ') or not b.use_deform:
        continue
    rn = 'R '+b.name[2:]
    r = rig.data.bones.get(rn)
    if r is None or b.name not in by_group or rn not in by_group:
        continue
    errs = sorted(kd.find(mirror(co[i]))[2] for i in by_group[b.name])
    cl = sum((co[i] for i in by_group[b.name]), Vector())/len(by_group[b.name])
    cr = sum((co[i] for i in by_group[rn]), Vector())/len(by_group[rn])
    hl, hr = rig.matrix_world @ b.head_local, rig.matrix_world @ r.head_local
    rows[b.name[2:]] = {
        'mesh_mirror_mm_p50': round(errs[len(errs)//2]*1000, 1),
        'mesh_mirror_mm_p90': round(errs[int(len(errs)*0.9)]*1000, 1),
        'skin_centroid_mirror_mm': round((mirror(cl)-cr).length*1000, 1),
        'bone_head_mirror_mm': round((mirror(hl)-hr).length*1000, 1),
        'L_head_to_skin_centroid_mm': round((hl-cl).length*1000, 1),
        'R_head_to_skin_centroid_mm': round((hr-cr).length*1000, 1),
        'verts': [len(by_group[b.name]), len(by_group[rn])]}
out.write_text(json.dumps({'mid_x': mid, 'bones': rows}, indent=1), encoding='utf-8')
for k, v in rows.items():
    print('SYM', k, v)
