"""Read-only inventory of hybrid v10 for planning the v11 fidelity pass."""
import bpy
import json
from pathlib import Path

if not bpy.app.background:
    raise RuntimeError('Use isolated --background only')
ROOT=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'art/hybrid/hollow-saint-hybrid-v10.blend'))
scene=bpy.context.scene
objects=[]
for obj in scene.objects:
    entry={'name':obj.name,'type':obj.type,'parent':obj.parent.name if obj.parent else None,
           'parent_bone':obj.parent_bone or None,
           'materials':[m.name for m in getattr(obj.data,'materials',[]) if m] if obj.data else [],
           'loc':[round(v,3) for v in obj.matrix_world.translation],
           'dims':[round(v,3) for v in obj.dimensions]}
    if obj.type=='MESH':
        entry['verts']=len(obj.data.vertices)
        entry['modifiers']=[m.type for m in obj.modifiers]
    objects.append(entry)
rig=bpy.data.objects.get('Hollow Saint | v8 rig')
bones=[]
if rig:
    for b in rig.data.bones:
        bones.append({'name':b.name,'parent':b.parent.name if b.parent else None,
                      'head':[round(v,3) for v in rig.matrix_world@b.head_local],
                      'tail':[round(v,3) for v in rig.matrix_world@b.tail_local]})
materials={}
for m in bpy.data.materials:
    info={'users':m.users}
    if m.use_nodes:
        p=m.node_tree.nodes.get('Principled BSDF')
        if p:
            info['base']=[round(v,3) for v in p.inputs['Base Color'].default_value][:3]
            info['metal']=round(p.inputs['Metallic'].default_value,3)
            info['rough']=round(p.inputs['Roughness'].default_value,3)
            info['emit']=round(p.inputs['Emission Strength'].default_value,3)
            info['base_linked']=p.inputs['Base Color'].is_linked
    materials[m.name]=info
world=scene.world
report={'objects':objects,'bones':bones,'materials':materials,
        'actions':[a.name for a in bpy.data.actions],
        'render':{'engine':scene.render.engine,'view':scene.view_settings.view_transform,
                  'look':scene.view_settings.look,'samples':getattr(scene.cycles,'samples',None),
                  'res':[scene.render.resolution_x,scene.render.resolution_y],
                  'film_transparent':scene.render.film_transparent,
                  'use_nodes':scene.use_nodes},
        'world':world.name if world else None,
        'cameras':[o.name for o in scene.objects if o.type=='CAMERA'],
        'lights':[{'name':o.name,'type':o.data.type,'energy':o.data.energy,'loc':[round(v,2) for v in o.location]} for o in scene.objects if o.type=='LIGHT']}
out=ROOT/'art/hybrid/v11/source-inventory.json'
out.parent.mkdir(parents=True,exist_ok=True)
out.write_text(json.dumps(report,indent=1),encoding='utf-8')
print('INVENTORY',out)
