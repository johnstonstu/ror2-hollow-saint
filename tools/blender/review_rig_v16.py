"""Read-only v16 rig QA (never saves a .blend).

1) `-- dump`  on v15: cache evaluated body/overlay vertices for every HS_v10 check frame.
2) `-- check` on v16: compare against that cache (body must be unchanged), IK rest test,
   weight normalisation, then render an exercise pose (crouch via leg IK, toe curl,
   tabard swing, arm IK reach) from hero and side cameras.

Run each with: blender --background --factory-startup --python-exit-code 1 --python tools/blender/review_rig_v16.py -- dump|check
"""
import bpy
import json
import math
import pickle
import sys
import tempfile
from pathlib import Path
from mathutils import Euler, Vector

if not bpy.app.background:
    raise RuntimeError('Use isolated --background only')
ROOT=Path(__file__).resolve().parents[2]
MODE=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'check'
CACHE=Path(tempfile.gettempdir())/'hs_v15_eval_cache.pkl'
OUT_DIR=ROOT/'art/hybrid/v16'
version='v15' if MODE=='dump' else 'v16'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/f'art/hybrid/hollow-saint-hybrid-{version}.blend'))
scene=bpy.context.scene
rig=bpy.data.objects['Hollow Saint | v8 rig']
body=bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
tracked=[o for o in scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' for m in o.modifiers) and 'SHOULDER' not in o.name]


def evaluate(objs):
    dg=bpy.context.evaluated_depsgraph_get()
    out={}
    for o in objs:
        eo=o.evaluated_get(dg)
        m=eo.to_mesh()
        out[o.name]=[tuple(o.matrix_world@v.co) for v in m.vertices]
        eo.to_mesh_clear()
    return out


def frames_for(action):
    s,e=(int(f) for f in action.frame_range)
    return sorted(set([s,e]+[int(m.frame) for m in action.pose_markers]+list(range(s,e+1,6))))


actions=[a for a in bpy.data.actions if a.name.startswith('HS_v10')]
if MODE=='dump':
    cache={}
    for a in actions:
        rig.animation_data.action=a
        for f in frames_for(a):
            scene.frame_set(f)
            cache[(a.name,f)]=evaluate(tracked)
    CACHE.write_bytes(pickle.dumps(cache))
    print('DUMPED',len(cache),'frames',flush=True)
    raise SystemExit(0)

cache=pickle.loads(CACHE.read_bytes())
result={'version':'v16','frames_compared':0,'max_body_diff_m':0.0,'max_overlay_diff_m':0.0,'worst_overlay':None,'errors':[]}
for a in actions:
    rig.animation_data.action=a
    for f in frames_for(a):
        scene.frame_set(f)
        now=evaluate(tracked)
        ref=cache[(a.name,f)]
        for name,pts in now.items():
            if name not in ref:
                result['errors'].append(f'{name} missing from v15 cache');continue
            d=max((Vector(p)-Vector(q)).length for p,q in zip(pts,ref[name]))
            if name==body.name:
                result['max_body_diff_m']=max(result['max_body_diff_m'],d)
            elif d>result['max_overlay_diff_m']:
                result['max_overlay_diff_m'],result['worst_overlay']=d,name
        result['frames_compared']+=1

for o in [body]+[x for x in scene.objects if 'SHOULDER' in x.name or x.name.startswith('TABARD')]:
    for v in o.data.vertices:
        total=sum(g.weight for g in v.groups)
        if abs(total-1)>1e-3:
            result['errors'].append(f'{o.name}: vertex {v.index} weight sum {total:.4f}');break

pb=rig.pose.bones
ik_constraints=[c for p in pb for c in p.constraints if c.type in ('IK','COPY_ROTATION') and 'IK' in c.name]
rig.animation_data.action=None
for p in pb:
    p.location=(0,0,0);p.rotation_quaternion=(1,0,0,0);p.rotation_euler=(0,0,0);p.scale=(1,1,1)
bpy.context.view_layer.update()
fk=evaluate([body])[body.name]
for c in ik_constraints:c.influence=1
bpy.context.view_layer.update()
ik=evaluate([body])[body.name]
result['ik_rest_body_diff_m']=round(max((Vector(p)-Vector(q)).length for p,q in zip(fk,ik)),6)


def rot(name,deg_xyz):
    p=pb[name]
    p.rotation_mode='XYZ'
    p.rotation_euler=Euler(tuple(math.radians(d) for d in deg_xyz))


# Exercise pose: crouch 14 cm with feet planted by IK, toes curled, tabard swung, left arm IK reach.
pb['pelvis'].location=rig.data.bones['pelvis'].matrix_local.inverted().to_3x3()@Vector((0,0,-.14))
for s in 'LR':
    rot(f'{s} toe',(28,0,0))
rot('tabard front.1',(-12,0,0));rot('tabard front.2',(-10,0,0));rot('tabard front.3',(-8,0,0))
rot('tabard back.1',(10,0,0));rot('tabard back.2',(8,0,0))
hand_ik=pb['L hand IK']
hand_ik.location=(rig.data.bones['L hand IK'].matrix_local.inverted().to_3x3()@Vector((-.12,-.32,.42)))
bpy.context.view_layer.update()
posed=evaluate([body])[body.name]
edges=[(e.vertices[0],e.vertices[1]) for e in body.data.edges]
rest_len=[(Vector(fk[a])-Vector(fk[b])).length for a,b in edges]
stretch=[]
for (a,b),r in zip(edges,rest_len):
    if r<1e-5:continue
    stretch.append(((Vector(posed[a])-Vector(posed[b])).length/r,a))
worst=max(stretch)
result['exercise_pose']={'crouch_m':.14,'max_edge_stretch_ratio':round(worst[0],3),
                         'worst_vertex_z':round(fk[worst[1]][2],3),
                         'edges_over_1_5x':sum(1 for s,_ in stretch if s>1.5)}

prefs=bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type='OPTIX'
prefs.get_devices()
for d in prefs.devices:d.use=d.type!='CPU'
scene.cycles.device='GPU'
scene.cycles.samples=96
for cam,label in (('Hero three quarter','hero'),('Side orthographic','side'),('Front orthographic','front')):
    scene.camera=bpy.data.objects[cam]
    scene.render.resolution_x,scene.render.resolution_y=1100,1400
    scene.render.filepath=str(OUT_DIR/f'rig-exercise-{label}.png')
    bpy.ops.render.render(write_still=True)
result['status']='PASS' if not result['errors'] and result['max_body_diff_m']<1e-4 and result['ik_rest_body_diff_m']<1e-3 else 'CHECK'
for k in ('max_body_diff_m','max_overlay_diff_m'):
    result[k]=round(result[k],6)
(OUT_DIR/'rig-qa.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print('RIGQA',json.dumps(result),flush=True)
