"""Actual Blender review renders, metrics and editable scene output."""
import bpy
import json
from mathutils import Vector
from refine_helpers import material


def present(root,out,parts):
    version='v5'
    scene=bpy.context.scene
    scene.unit_settings.system='METRIC'
    scene.render.engine='CYCLES'
    scene.cycles.samples=40
    scene.cycles.use_denoising=True
    scene.render.image_settings.file_format='PNG'
    scene.render.resolution_x=1100
    scene.render.resolution_y=1400
    scene.render.resolution_percentage=100
    scene.view_settings.view_transform='AgX'
    scene.world.color=(.28,.28,.28)
    collection=bpy.data.collections.new('HOLLOW SAINT | original editable refinement '+version)
    scene.collection.children.link(collection)
    for obj in parts:
        for col in list(obj.users_collection): col.objects.unlink(obj)
        collection.objects.link(obj)
        obj['asset_status']='REFINEMENT STUDY / UNRIGGED / NOT TESTED IN GAME'
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,.003))
    bpy.context.object.name='Warm gray studio ground'
    bpy.context.object.data.materials.append(material('Studio gray',(.27,.25,.225),0,.9))
    def area(name,loc,power,size,color):
        data=bpy.data.lights.new(name,'AREA')
        data.energy=power
        data.size=size
        data.color=color
        obj=bpy.data.objects.new(name,data)
        scene.collection.objects.link(obj)
        obj.location=loc
        obj.rotation_euler=(Vector((0,0,1.15))-obj.location).to_track_quat('-Z','Y').to_euler()
    area('Warm studio softbox',(-3,-4,5),650,4,(1,.94,.85))
    area('Neutral fill',(3,-2,2.7),250,3,(.88,.94,1))
    area('Soft rim',(1,3,4),700,3,(1,.95,.88))
    def cam(name,loc,target,scale):
        data=bpy.data.cameras.new(name)
        data.type='ORTHO'
        data.ortho_scale=scale
        obj=bpy.data.objects.new(name,data)
        scene.collection.objects.link(obj)
        obj.location=loc
        obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
        return obj
    cameras=[('front',cam('Front orthographic',(0,-8,1.15),(0,0,1.15),2.65)),
             ('hero',cam('Hero three quarter',(3,-7,3.1),(0,0,1.14),2.65)),
             ('side',cam('Side orthographic',(8,0,1.15),(0,0,1.15),2.65)),
             ('back',cam('Back orthographic',(0,8,1.15),(0,0,1.15),2.65)),
             ('gameplay-distance',cam('Simulated gameplay distance',(-3.9,7,4.4),(0,0,1.07),7.5))]
    scene.camera=cameras[0][1]
    scene['status']='ORIGINAL MODEL REFINEMENT '+version+': no rig, animation, export, or gameplay validation'
    scene['reference']='Selected LEFT A silhouette, four separate open-bottom halo arcs; construction remains provisional'
    for screen in bpy.data.screens:
        for a in screen.areas:
            if a.type=='VIEW_3D':
                a.spaces.active.region_3d.view_distance=3.3
                a.spaces.active.region_3d.view_location=(0,0,1.12)
    bpy.ops.wm.save_as_mainfile(filepath=str(out/('hollow-saint-refinement-'+version+'.blend')))
    for key,camera in cameras:
        scene.camera=camera
        scene.render.resolution_x=1500 if key=='gameplay-distance' else 1100
        scene.render.resolution_y=1000 if key=='gameplay-distance' else 1400
        scene.render.filepath=str(out/('hollow-saint-refinement-'+version+'-'+key+'.png'))
        bpy.ops.render.render(write_still=True)
        print('RENDER COMPLETE '+key,flush=True)
    scene.camera=cameras[0][1]
    scene.render.resolution_x=1100
    scene.render.resolution_y=1400
    bpy.ops.wm.save_as_mainfile(filepath=str(out/('hollow-saint-refinement-'+version+'.blend')))
    meshes=[o for o in parts if o.type=='MESH']
    (out/('hollow-saint-refinement-'+version+'-metrics.json')).write_text(json.dumps({
        'status':scene['status'],'mesh_objects':len(meshes),'curve_objects':len(parts)-len(meshes),
        'vertices':sum(len(o.data.vertices) for o in meshes),
        'polygons':sum(len(o.data.polygons) for o in meshes),'halo_quadrants':4,
        'fingers_per_hand':5,'body_height_m':2.001,'tabard_bottom_m':.395,
        'renders':[str(out/('hollow-saint-refinement-'+version+'-'+key+'.png')) for key,camera in cameras],
        'gameplay_view_note':'Simulated Blender camera, not in-game.'},indent=2),encoding='utf-8')
