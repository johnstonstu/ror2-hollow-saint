import bpy, json
import numpy as np
OUT = bpy.path.abspath('//')
RIG = bpy.data.objects['Hollow Saint | v8 rig']
BODY = bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
exec(open(OUT + 'parts_lib.py').read())
RIG.data.pose_position = 'REST'; bpy.context.scene.frame_set(1)
dg = bpy.context.evaluated_depsgraph_get()
ev = BODY.evaluated_get(dg); me = ev.to_mesh()
co = np.array([BODY.matrix_world @ v.co for v in me.vertices]); ed = [tuple(e.vertices) for e in me.edges]; ev.to_mesh_clear()
dom = dominant(BODY)
res = {}
def seam(a, b):
    vs = set()
    for i, j in ed:
        if {dom[i], dom[j]} == {a, b}: vs |= {i, j}
    vs = list(vs)
    return co[vs].mean(0).round(4).tolist() if vs else None
bones = {b.name: (list(b.head_local), list(b.tail_local)) for b in RIG.data.bones}
for s in 'LR':
    for a, b, joint in ((f'{s} thigh', f'{s} shin', f'{s} shin'), (f'{s} shin', f'{s} foot', f'{s} foot'), (f'{s} upperarm', f'{s} forearm', f'{s} forearm'), (f'{s} forearm', f'{s} hand', f'{s} hand'), (f'{s} foot', f'{s} toe', f'{s} toe'), ('pelvis', f'{s} thigh', f'{s} thigh')):
        res[f'seam {a}/{b}'] = dict(mesh_seam=seam(a, b), bone_joint=[round(x, 4) for x in bones[joint][0]])
    # region z-range
    for g in (f'{s} thigh', f'{s} shin', f'{s} foot', f'{s} toe', f'{s} upperarm', f'{s} forearm'):
        idx = [i for i, d in enumerate(dom) if d == g]
        if idx: res[f'zrange {g}'] = [round(float(co[idx, 2].min()), 3), round(float(co[idx, 2].max()), 3), 'mean', co[idx].mean(0).round(3).tolist()]
for g in ('head', 'neck', 'chest', 'spine', 'pelvis'):
    idx = [i for i, d in enumerate(dom) if d == g]
    res[f'zrange {g}'] = [round(float(co[idx, 2].min()), 3), round(float(co[idx, 2].max()), 3)]
hb = [i for i, d in enumerate(dom) if d == 'head' and co[i, 2] < 1.60]
res['head-dominant verts below z1.60 (collar/upper back)'] = [len(hb), round(float(co[hb, 2].min()), 3) if hb else None, 'back(y>0.03):', int(sum(co[i,1] > 0.03 for i in hb))]
# torso centreline
for g in ('pelvis', 'spine', 'chest', 'neck', 'head'):
    idx = [i for i, d in enumerate(dom) if d == g]
    res[f'centre x {g}'] = round(float((co[idx, 0].max() + co[idx, 0].min())/2), 4)
# hand part centroids
for s in 'LR':
    pts = []
    for o in bpy.data.objects:
        if o.type == 'MESH' and o.name.startswith(f'{s} HAND |'):
            pts += [o.matrix_world @ v.co for v in o.data.vertices]
    res[f'{s} hand parts centroid'] = np.array(pts).mean(0).round(4).tolist()
    res[f'{s} hand bone'] = [round(x, 4) for x in bones[f'{s} hand'][0]] + [round(x, 4) for x in bones[f'{s} hand'][1]]
# halo arcs & pads centroid
for n in ('L SHOULDER | V17 pauldron upper', 'R SHOULDER | V17 pauldron upper', 'HALO | independent copper arc 2', 'HALO | independent copper arc 3', 'HALO | V17 yoke bar'):
    o = bpy.data.objects[n]; ev = o.evaluated_get(dg); m2 = ev.to_mesh()
    res['centroid ' + n] = np.array([o.matrix_world @ v.co for v in m2.vertices]).mean(0).round(4).tolist(); ev.to_mesh_clear()
json.dump(res, open(OUT + 'probe_geo.json', 'w'), indent=1)
print('DONE')
