"""Hybrid v14: concept palette pass (warm bone ivory, deep aged copper, calmer cyan core).

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/build_hybrid_v14.py
"""
import bpy
import json
from pathlib import Path

if not bpy.app.background:
    raise RuntimeError('Use isolated --background --factory-startup only')
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'art/hybrid/hollow-saint-hybrid-v13.blend'
OUT_BLEND=ROOT/'art/hybrid/hollow-saint-hybrid-v14.blend'
OUT_DIR=ROOT/'art/hybrid/v14'
OUT_DIR.mkdir(parents=True,exist_ok=True)
TAG='v14'

bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
rig=bpy.data.objects['Hollow Saint | v8 rig']
report={'source':str(SOURCE.relative_to(ROOT)),'changes':[]}

IVORY_TINT=(.93,.84,.68,1)


def tint_base_color(mat):
    nt=mat.node_tree
    p=nt.nodes['Principled BSDF']
    link=next(l for l in nt.links if l.to_node==p and l.to_socket.name=='Base Color')
    mul=nt.nodes.new('ShaderNodeMixRGB');mul.name='V14 ivory tint'
    mul.blend_type='MULTIPLY';mul.inputs['Fac'].default_value=1
    mul.inputs['Color2'].default_value=IVORY_TINT
    nt.links.new(link.from_socket,mul.inputs['Color1'])
    nt.links.new(mul.outputs['Color'],p.inputs['Base Color'])


for name in ('HYBRID source texture | ceramic graphite and cyan','HYBRID posterior graphite | retained texture seam blend'):
    tint_base_color(bpy.data.materials[name])
warm=bpy.data.materials['HYBRID warm ivory'].node_tree.nodes['Principled BSDF']
c=warm.inputs['Base Color'].default_value
warm.inputs['Base Color'].default_value=(c[0]*IVORY_TINT[0],c[1]*IVORY_TINT[1],c[2]*IVORY_TINT[2],1)
plate=bpy.data.materials['V12 ivory ceramic plate'].node_tree
ramp=next(n for n in plate.nodes if n.bl_idname=='ShaderNodeValToRGB')
ramp.color_ramp.elements[0].color=(.56,.49,.37,1)
ramp.color_ramp.elements[1].color=(.64,.575,.445,1)
report['changes'].append('Warm bone-ivory tint on body texture, warm ivory parts and pauldron plates')

copper=bpy.data.materials['V11 aged copper blocks'].node_tree
mix=next(n for n in copper.nodes if n.bl_idname=='ShaderNodeMix')
mix.inputs[6].default_value=(.21,.072,.028,1)
mix.inputs[7].default_value=(.055,.02,.009,1)
copper.nodes['Principled BSDF'].inputs['Metallic'].default_value=.18
rough=next(n for n in copper.nodes if n.bl_idname=='ShaderNodeMapRange' and abs(n.inputs['To Min'].default_value-.42)<1e-4)
rough.inputs['To Min'].default_value=.52
edge=bpy.data.materials['HYBRID copper edge'].node_tree.nodes['Principled BSDF']
edge.inputs['Base Color'].default_value=(.10,.038,.016,1)
report['changes'].append('Halo copper deepened to concept red-brown, lower metallic, rougher')

hot=bpy.data.materials['V11 cyan core hot'].node_tree.nodes['Principled BSDF']
hot.inputs['Emission Color'].default_value=(.18,.78,1.0,1)
hot.inputs['Emission Strength'].default_value=4.0
report['changes'].append('Core hot centre emission 7 -> 4 with deeper cyan, relying on bloom for the glow')

glare=next(n for n in scene.compositing_node_group.nodes if n.bl_idname=='CompositorNodeGlare')
glare.inputs['Strength'].default_value=.7
glare.inputs['Threshold'].default_value=1.2

try:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    for backend in ('OPTIX','CUDA','HIP','ONEAPI'):
        try:
            prefs.compute_device_type=backend
            prefs.get_devices()
            if any(d.type!='CPU' for d in prefs.devices):
                for d in prefs.devices:d.use=d.type!='CPU'
                scene.cycles.device='GPU'
                report['render_device']=backend
                break
        except TypeError:
            continue
except Exception as exc:
    report['render_device_error']=str(exc)

rig.data.pose_position='POSE'
rig.animation_data.action=bpy.data.actions['HS_v10 | Idle - contained storm']
scene.frame_set(1)
scene[f'{TAG}_fidelity']='; '.join(report['changes'])
bpy.ops.wm.save_as_mainfile(filepath=str(OUT_BLEND))
print('SAVED',OUT_BLEND,flush=True)

backdrop=bpy.data.objects['V11 bust backdrop']
prefix=f'hollow-saint-hybrid-{TAG}'
shots=[('front','Front orthographic',1100,1400),('hero','Hero three quarter',1100,1400),
       ('side','Side orthographic',1100,1400),('back','Back orthographic',1100,1400),
       ('gameplay-distance','Simulated gameplay distance',1500,1000),('bust','V11 bust detail',1100,1100)]
for suffix,cam,w,h in shots:
    scene.camera=bpy.data.objects[cam]
    scene.render.resolution_x,scene.render.resolution_y=w,h
    scene.render.filepath=str(OUT_DIR/f'{prefix}-{suffix}.png')
    backdrop.hide_render=suffix!='bust'
    bpy.ops.render.render(write_still=True)
    print('RENDER',suffix,flush=True)
backdrop.hide_render=True
scene.camera=bpy.data.objects['Hero three quarter']
scene.render.resolution_x,scene.render.resolution_y=1100,1400
for action,frame,label in (('HS_v10 | Arc Bolt - two finger snap',9,'pose-arc-bolt'),('HS_v10 | Arc Step - in place dash',9,'pose-arc-step')):
    rig.animation_data.action=bpy.data.actions[action]
    scene.frame_set(frame)
    scene.render.filepath=str(OUT_DIR/f'{prefix}-{label}.png')
    bpy.ops.render.render(write_still=True)
    print('RENDER',label,flush=True)
report['status']='Visual fidelity study; renders are Blender studio evidence only, no game test'
(OUT_DIR/'metrics.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('DONE',flush=True)
