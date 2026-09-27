"""Read-only M6 probe: arm joint seams per side, and a rigid fit of the mirrored L hand meshes onto the R ones."""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT/'tools/blender/anim'))
import bpy
import numpy as np
from mathutils import Matrix, Vector

import hs_anim as H

bpy.ops.wm.open_mainfile(filepath=str(H.START))
rig = bpy.data.objects[H.RIG]
body = bpy.data.objects[H.BODY]
rig.data.pose_position = 'REST'
bpy.context.view_layer.update()
dg = bpy.context.evaluated_depsgraph_get()
ev = body.evaluated_get(dg)
me = ev.to_mesh()
co = np.array([tuple(body.matrix_world @ v.co) for v in me.vertices])
ed = [tuple(e.vertices) for e in me.edges]
ev.to_mesh_clear()
names = {g.index: g.name for g in body.vertex_groups}
dom = [names[max(v.groups, key=lambda e: e.weight).group] if v.groups else '' for v in body.data.vertices]
mw = rig.matrix_world
out = {}


def seam(a, b):
    vs = sorted({k for i, j in ed if {dom[i], dom[j]} == {a, b} for k in (i, j)})
    return co[vs].mean(0) if vs else None, len(vs)


for s in 'LR':
    for a, b, joint in (('chest', f'{s} upperarm', f'{s} upperarm'), (f'{s} upperarm', f'{s} forearm', f'{s} forearm'),
                        (f'{s} forearm', f'{s} hand', f'{s} hand')):
        c, n = seam(a, b)
        bj = mw @ rig.data.bones[joint].head_local
        out[f'{s} {joint}'] = {'seam': None if c is None else [round(float(x), 4) for x in c], 'n': n,
                               'bone': [round(x, 4) for x in bj],
                               'bone_minus_seam': None if c is None else [round(float(x), 4) for x in (np.array(bj)-c)]}

MID = H.MID_X


def mirror(v):
    return np.array([2*MID-v[0], v[1], v[2]])


def kabsch(P, Q):
    """R, t minimising |R P + t - Q|."""
    pc, qc = P.mean(0), Q.mean(0)
    Hm = (P-pc).T @ (Q-qc)
    U, S, Vt = np.linalg.svd(Hm)
    d = np.sign(np.linalg.det(Vt.T @ U.T))
    D = np.diag([1, 1, d])
    R = Vt.T @ D @ U.T
    return R, qc-R @ pc


P, Q, parts = [], [], []
for o in bpy.data.objects:
    if o.type == 'MESH' and o.name.startswith('L HAND |'):
        r = bpy.data.objects.get('R'+o.name[1:])
        if r is None or len(r.data.vertices) != len(o.data.vertices):
            parts.append((o.name, 'no match' if r is None else f'{len(o.data.vertices)} vs {len(r.data.vertices)}'))
            continue
        P += [mirror(np.array(o.matrix_world @ v.co)) for v in o.data.vertices]
        Q += [np.array(r.matrix_world @ v.co) for v in r.data.vertices]
P, Q = np.array(P), np.array(Q)
R, t = kabsch(P, Q)
res = np.linalg.norm((P @ R.T + t)-Q, axis=1)
out['hand_fit'] = {'n': len(P), 'rms_mm': round(float(np.sqrt((res**2).mean()))*1000, 2),
                   'max_mm': round(float(res.max())*1000, 2), 'unmatched': parts,
                   'rot_deg': round(float(np.degrees(np.arccos(np.clip((np.trace(R)-1)/2, -1, 1)))), 2),
                   't': [round(float(x), 4) for x in t]}
# where the mirrored L hand/finger bones land under the fit, vs the actual R bones
off = {}
for b in rig.data.bones:
    if b.name.startswith('L ') and any(k in b.name for k in ('hand', 'index', 'middle', 'ring', 'little', 'thumb', 'muzzle')) \
            and 'IK' not in b.name:
        rb = rig.data.bones.get('R'+b.name[1:])
        h = R @ mirror(np.array(mw @ b.head_local)) + t
        tl = R @ mirror(np.array(mw @ b.tail_local)) + t
        off[b.name[2:]] = {'head_mm': round(float(np.linalg.norm(h-np.array(mw @ rb.head_local)))*1000, 1),
                           'tail_mm': round(float(np.linalg.norm(tl-np.array(mw @ rb.tail_local)))*1000, 1)}
out['R_bone_vs_fitted_mirror'] = off
# forearm/hand mesh (rigid, bone-parented) per-side: wrist cuff centroid
for s in 'LR':
    for o in bpy.data.objects:
        if o.type == 'MESH' and o.name.startswith(f'{s} HAND |') and 'cuff' in o.name:
            c = np.mean([np.array(o.matrix_world @ v.co) for v in o.data.vertices], 0)
            out[f'{s} cuff centroid {o.name}'] = [round(float(x), 4) for x in c]
            out[f'{s} cuff parent'] = o.parent_bone
print('ARMFIT', json.dumps(out), flush=True)
(ROOT/'art/anim/wip/_run4/armfit_probe.json').write_text(json.dumps(out, indent=1))
