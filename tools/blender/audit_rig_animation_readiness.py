"""Read-only rig audit for animation planning: bones, hierarchy, deform set, actions, sockets.

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/audit_rig_animation_readiness.py -- v12
Writes art/hybrid/<version>/rig-audit.json; never saves the .blend.
"""
import bpy
import json
import sys
from pathlib import Path

if not bpy.app.background:
    raise RuntimeError('Use isolated --background only')
ROOT=Path(__file__).resolve().parents[2]
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
VERSION=args[0] if args else 'v12'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/f'art/hybrid/hollow-saint-hybrid-{VERSION}.blend'))
rig=bpy.data.objects['Hollow Saint | v8 rig']
arm=rig.data


def depth(b):
    d=0
    while b.parent:
        b=b.parent;d+=1
    return d


bones=[{'name':b.name,'parent':b.parent.name if b.parent else None,'deform':b.use_deform,'depth':depth(b),
        'head':[round(c,3) for c in b.head_local],'tail':[round(c,3) for c in b.tail_local],
        'length':round(b.length,3)} for b in arm.bones]
roots=[b.name for b in arm.bones if not b.parent]
skinned_to=set()
for o in bpy.data.objects:
    if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers):
        skinned_to.update(g.name for g in o.vertex_groups)
bone_children={}
for o in bpy.data.objects:
    if o.parent==rig and o.parent_type=='BONE':
        bone_children.setdefault(o.parent_bone,[]).append(o.name)
constraints={pb.name:[c.type for c in pb.constraints] for pb in rig.pose.bones if pb.constraints}
actions=[]
for a in bpy.data.actions:
    s,e=a.frame_range
    actions.append({'name':a.name,'frames':[int(s),int(e)],'markers':[(m.name,m.frame) for m in a.pose_markers]})
keywords={'root':('root',),'pelvis':('pelvis','hip'),'spine':('spine','chest'),'head':('head','neck'),
          'arm':('upperarm','forearm','hand'),'leg':('thigh','calf','shin','foot','toe'),'finger':('finger','thumb','index','middle','ring','pinky'),
          'halo':('halo',),'tabard':('tabard','cloth','skirt')}
coverage={k:[b.name for b in arm.bones if any(w in b.name.lower() for w in words)] for k,words in keywords.items()}
report={'version':VERSION,'rig':rig.name,'bone_count':len(bones),'deform_count':sum(b['deform'] for b in bones),
        'roots':roots,'max_depth':max(b['depth'] for b in bones),'scale':[round(s,4) for s in rig.scale],
        'rig_location':[round(c,4) for c in rig.location],'coverage':coverage,
        'bones_without_skin':[b['name'] for b in bones if b['deform'] and b['name'] not in skinned_to and b['name'] not in bone_children],
        'bone_parented_objects':bone_children,'constraints':constraints,'actions':actions,'bones':bones}
out=ROOT/f'art/hybrid/{VERSION}/rig-audit.json'
out.write_text(json.dumps(report,indent=2),encoding='utf-8')
print('AUDIT',json.dumps({k:report[k] for k in ('bone_count','deform_count','roots','max_depth','scale')}))
print('COVERAGE',json.dumps({k:len(v) for k,v in coverage.items()}))
print('DONE')
