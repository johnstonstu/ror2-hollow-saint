"""Read-only saved-file QA for hybrid v11+: new detail stays seated on the body through every action.

Set HS_VERSION=v12 (etc.) to check a later checkpoint; objects tagged with any version from v11 up to it are checked.
"""
import bpy
import json
import math
import os
from pathlib import Path
from mathutils.bvhtree import BVHTree

if not bpy.app.background:
    raise RuntimeError('Use isolated --background only')
ROOT=Path(__file__).resolve().parents[2]
VERSION=os.environ.get('HS_VERSION','v11')
TAGS=[f'v{n}' for n in range(11,int(VERSION[1:])+1)]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/f'art/hybrid/hollow-saint-hybrid-{VERSION}.blend'))
scene=bpy.context.scene
rig=bpy.data.objects['Hollow Saint | v8 rig']
body=bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
new=[o for o in scene.objects if any(t in o.keys() for t in TAGS)]
skinned=[o for o in new if any(m.type=='ARMATURE' for m in o.modifiers)]
bone_parented=[o for o in new if o.parent_type=='BONE']
shells={o.name for o in skinned if 'SHOULDER' in o.name}
result={'version':VERSION,'new_objects':len(new),'skinned':len(skinned),'bone_parented':len(bone_parented),'actions':[],'errors':[]}

for o in skinned:
    for v in o.data.vertices:
        total=sum(g.weight for g in v.groups)
        if not v.groups or abs(total-1)>1e-3:
            result['errors'].append(f'{o.name}: vertex {v.index} weight sum {total:.4f}')
            break


def distances():
    dg=bpy.context.evaluated_depsgraph_get()
    ev=body.evaluated_get(dg)
    mesh=ev.to_mesh()
    verts=[body.matrix_world@v.co for v in mesh.vertices]
    bvh=BVHTree.FromPolygons(verts,[tuple(p.vertices) for p in mesh.polygons])
    ev.to_mesh_clear()
    out={}
    for o in skinned:
        eo=o.evaluated_get(dg)
        m=eo.to_mesh()
        ds=[]
        for v in m.vertices:
            w=o.matrix_world@v.co
            if not all(math.isfinite(c) for c in w):
                result['errors'].append(f'{o.name}: non-finite vertex')
                break
            ds.append(bvh.find_nearest(w)[3])
        eo.to_mesh_clear()
        out[o.name]=ds
    return out


rig.data.pose_position='REST'
scene.frame_set(1)
rest=distances()
rig.data.pose_position='POSE'
for action in [a for a in bpy.data.actions if a.name.startswith('HS_v10')]:
    rig.animation_data.action=action
    start,end=(int(f) for f in action.frame_range)
    frames=sorted(set([start,end]+[int(m.frame) for m in action.pose_markers]+list(range(start,end+1,6))))
    worst=0.0
    worst_obj=None
    shell_worst=(0.0,None,None)
    for f in frames:
        scene.frame_set(f)
        now=distances()
        for name,ds in now.items():
            drift=max(abs(a-b) for a,b in zip(ds,rest[name]))
            if name in shells:
                if drift>shell_worst[0]:
                    shell_worst=(drift,name,f)
            elif drift>worst:
                worst,worst_obj=drift,name
    entry={'name':action.name,'frames':len(frames),'max_surface_drift_m':round(worst,5),'worst_object':worst_obj}
    if shells:
        entry['shell_max_drift_m']=round(shell_worst[0],5)
        entry['shell_worst']=[shell_worst[1],shell_worst[2]]
    result['actions'].append(entry)

limit=.012
result['surface_drift_limit_m']=limit
result['shells']=sorted(shells)
result['shell_note']='Rigid shoulder shells are expected to move relative to skin; drift is informational and clipping is checked visually.'
result['status']='PASS' if not result['errors'] and all(a['max_surface_drift_m']<limit for a in result['actions']) else 'CHECK'
rig.animation_data.action=bpy.data.actions['HS_v10 | Idle - contained storm']
out=ROOT/f'art/hybrid/{VERSION}/qa.json'
out.write_text(json.dumps(result,indent=2),encoding='utf-8')
print('QA',result['status'],json.dumps(result['actions']))
