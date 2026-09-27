import bpy
import json


def present(root,out,parts,report,version):
    scene=bpy.context.scene
    collection=bpy.data.collections.new('HOLLOW SAINT | HF hybrid '+version)
    scene.collection.children.link(collection)
    for obj in parts:
        for coll in list(obj.users_collection):coll.objects.unlink(obj)
        collection.objects.link(obj)
        obj['status']='Hybrid visual study; no rig, animation, export, or game validation'
    scene['status']='HF-based hybrid '+version+'; original HF texture and stronger body retained'
    scene['reference']='Selected LEFT A / Cracked Icon. Generated hands, halo and rear chest corrected provisionally.'
    scene.cycles.samples=40
    scene.camera=bpy.data.objects['Front orthographic']
    prefix='hollow-saint-hybrid-'+version
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(prefix+'.blend')))
    for suffix,camera in [('front','Front orthographic'),('hero','Hero three quarter'),
                          ('side','Side orthographic'),('back','Back orthographic'),
                          ('gameplay-distance','Simulated gameplay distance')]:
        scene.camera=bpy.data.objects[camera]
        scene.render.resolution_x=1500 if suffix=='gameplay-distance' else 1100
        scene.render.resolution_y=1000 if suffix=='gameplay-distance' else 1400
        scene.render.filepath=str(out/(prefix+'-'+suffix+'.png'))
        bpy.ops.render.render(write_still=True)
        print('HYBRID_RENDER '+suffix,flush=True)
    scene.camera=bpy.data.objects['Front orthographic']
    scene.render.resolution_x,scene.render.resolution_y=1100,1400
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(prefix+'.blend')))
    report['parts']=len(parts)
    report['meshes']=sum(p.type=='MESH' for p in parts)
    report['curves']=sum(p.type=='CURVE' for p in parts)
    report['base_vertices']=sum(len(p.data.vertices) for p in parts if p.type=='MESH')
    report['base_polygons']=sum(len(p.data.polygons) for p in parts if p.type=='MESH')
    (out/(prefix+'-metrics.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
