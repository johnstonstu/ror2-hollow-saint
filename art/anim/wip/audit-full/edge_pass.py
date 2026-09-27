"""Edge-length deformation relative to Idle f1 (reviewed stand), per region, every frame of every clip. Read-only."""
import bpy, json
import numpy as np
from collections import defaultdict
OUT = bpy.path.abspath('//')
sc = bpy.context.scene
RIG = bpy.data.objects['Hollow Saint | v8 rig']; ad = RIG.animation_data
BODY = bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
exec(open(OUT + 'parts_lib.py').read())
bdom = dominant(BODY); breg = [region_of(g) for g in bdom]
HARD = [o for o in bpy.data.objects if o.type == 'MESH' and any(m.type == 'ARMATURE' for m in o.modifiers) and o != BODY and not o.name.startswith('TABARD')]
def coords(o, dg):
    ev = o.evaluated_get(dg); me = ev.to_mesh(); n = len(me.vertices)
    co = np.empty(n*3); me.vertices.foreach_get('co', co); co = co.reshape(-1, 3)
    ed = np.empty(len(me.edges)*2, dtype=np.int64); me.edges.foreach_get('vertices', ed)
    ev.to_mesh_clear(); mw = np.array(o.matrix_world)
    return co @ mw[:3, :3].T + mw[:3, 3], ed.reshape(-1, 2)
def use(name):
    a = bpy.data.actions['HS_anim | ' + name]; ad.action = a
    if ad.action_slot is None and len(a.slots): ad.action_slot = a.slots[0]
    return a
use('Idle'); sc.frame_set(1); dg = bpy.context.evaluated_depsgraph_get()
bco, bed = coords(BODY, dg)
L0 = np.linalg.norm(bco[bed[:, 0]] - bco[bed[:, 1]], axis=1)
ok = L0 >= 0.004
reg = {}
for i, (a, b) in enumerate(bed):
    if not ok[i]: continue
    ra, rb = breg[a], breg[b]
    reg.setdefault(ra if ra == rb else 'seam ' + '/'.join(sorted((ra, rb))), []).append(i)
reg = {k: np.array(v) for k, v in reg.items()}
H0 = {}
for o in HARD:
    c, e = coords(o, dg); l = np.linalg.norm(c[e[:, 0]] - c[e[:, 1]], axis=1); H0[o.name] = (e, l)
res = {}
for a in [a for a in bpy.data.actions if a.name.startswith('HS_anim | ')]:
    name = a.name[10:]; use(name)
    f0, f1 = int(a.frame_range[0]), int(a.frame_range[1])
    r = {}
    for f in range(f0, f1+1):
        sc.frame_set(f); dg = bpy.context.evaluated_depsgraph_get()
        c, _ = coords(BODY, dg)
        rr = np.linalg.norm(c[bed[:, 0]] - c[bed[:, 1]], axis=1) / np.maximum(L0, 1e-6)
        fr = {}
        for k, idx in reg.items():
            x = rr[idx]
            fr[k] = [round(float(np.percentile(x, 99)), 3), round(float(np.percentile(x, 1)), 3), int((x > 1.4).sum()), int((x < 0.6).sum()), len(idx), round(float(x.max()), 2), round(float(x.min()), 2)]
        hd = {}
        for o in HARD:
            cc, _ = coords(o, dg); e, l = H0[o.name]; m = l >= 0.004
            if m.any():
                q = np.linalg.norm(cc[e[m, 0]] - cc[e[m, 1]], axis=1) / l[m]
                hd[o.name] = round(float(np.abs(q-1).max()), 3)
        r[f] = dict(body=fr, hard=hd)
    res[name] = r
    print('CLIP', name, flush=True)
json.dump(res, open(OUT + 'edges_vs_idle.json', 'w'))
print('DONE')
