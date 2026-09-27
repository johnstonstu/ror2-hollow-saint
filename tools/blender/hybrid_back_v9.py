"""Subtle rear halo/shoulder connection revision, preserving v8 rig/actions and source."""
import bpy
import math
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'art/hybrid/hollow-saint-hybrid-v8.blend'
OUT=ROOT/'art/hybrid/hollow-saint-hybrid-v9.blend'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene=bpy.context.scene

# These two diagonal branches pulled the halo visually into the shoulder blades.
# The original HF rear uses a clean open back between shoulders and central node.
for name in ['YOKE | fitted branch -1','YOKE | fitted branch 1']:
    obj=bpy.data.objects.get(name)
    if obj:
        bpy.data.objects.remove(obj,do_unlink=True)

# Remove the hairline gap wires. The HF reference carries pale-cyan bands directly
# across the copper at the shoulder-side arc, rather than spanning a floating gap.
for name in ['HALO | geometric gap conductor 73.5','HALO | geometric gap conductor 286.5']:
    obj=bpy.data.objects.get(name)
    if obj:
        bpy.data.objects.remove(obj,do_unlink=True)

source=bpy.data.objects.get('BACK | small recessed charge node')
material=source.data.materials[0] if source and source.data.materials else None
if material is None:
    raise RuntimeError('Could not locate the existing cyan accent material')

def bridge(name,angle_deg,bone):
    angle=math.radians(angle_deg)
    center_z=1.79
    inner,outer=.320,.433
    half_angle=.045
    rear_y=.176
    front_y=.160
    vertices=[]
    # Eight corners: radial x angular rectangle, with shallow depth.
    for y in (front_y,rear_y):
        for r,a in [(inner,angle-half_angle),(outer,angle-half_angle),
                    (outer,angle+half_angle),(inner,angle+half_angle)]:
            vertices.append((r*math.sin(a),y,center_z+r*math.cos(a)))
    faces=[(0,1,2,3),(7,6,5,4)]
    for i in range(4): faces.append((i,(i+1)%4,(i+1)%4+4,i+4))
    mesh=bpy.data.meshes.new(name+' mesh')
    mesh.from_pydata(vertices,[],faces)
    mesh.materials.append(material)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    scene.collection.objects.link(obj)
    # Keep each band editable and seat it on its shoulder-side copper arc.
    rig=bpy.data.objects['Hollow Saint | v8 rig']
    world=obj.matrix_world.copy()
    obj.parent=rig
    obj.parent_type='BONE'
    obj.parent_bone=bone
    obj.matrix_world=world
    obj['design_note']='Pale cyan inlay crossing the shoulder-side copper arc, inspired by the original Higgsfield rear.'

bridge('HALO | HF-style cyan shoulder band L',104,'halo 2')
bridge('HALO | HF-style cyan shoulder band R',256,'halo 3')

# Retain v8 rig and actions; return to the clean review pose.
rig=bpy.data.objects['Hollow Saint | v8 rig']
rig.animation_data.action=bpy.data.actions.get('HS_v8 | Idle contained storm | 2s loop')
scene.frame_set(1)
scene['rear_refinement']='v9 removes two dark fitted yoke branches and replaces hairline side gap wires with broad pale-cyan halo seam bridges, inspired by the original Higgsfield rear.'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT))
print('SAVED',OUT)
