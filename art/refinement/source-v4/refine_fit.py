"""Reference-based proportion pass applied to editable geometry before presentation."""
import bpy
import bmesh
from mathutils import Vector, Matrix


def fit(parts):
    bpy.context.view_layer.update()
    limb_terms=['arm |','forearm |','elbow |','wrist','hand |','phalanx','knuckle']
    leg_terms=['hip |','hip socket','thigh |','knee |','calf |','shin |','ankle','foot |']
    torso_terms=['Torso |','Pelvis |','Abdomen |','oblique |','thorax cyan','Back | spine']
    for obj in parts:
        name=obj.name
        category='arm' if any(t in name for t in limb_terms) else 'leg' if any(t in name for t in leg_terms) else 'torso' if any(t in name for t in torso_terms) else None
        if category:
            world=obj.matrix_world.copy()
            def point(v):
                v=world@Vector(v)
                if category=='arm':
                    v.x *= 1.12
                    if v.z >= 1.069:
                        v.z=1.6-(1.6-v.z)*(1.6-.985)/(1.6-1.069)
                    else:
                        v.z=.985-(1.069-v.z)*1.36
                elif category=='leg':
                    v.x*=1.32+max(0,min(1,(1.077-v.z)/.94))*.34
                    if v.z>.631:
                        v.z=.75+(v.z-.631)*(1.21-.75)/(1.077-.631)
                    elif v.z>.135:
                        v.z=.135+(v.z-.135)*(.75-.135)/(.631-.135)
                else:
                    v.z=1.65-(1.65-v.z)*(1.65-1.21)/(1.65-1.077)
                return v
            if obj.type=='MESH':
                for v in obj.data.vertices: v.co=point(v.co)
            elif obj.type=='CURVE':
                for spline in obj.data.splines:
                    for p in spline.points: p.co=(*point(p.co[:3]),1)
            obj.matrix_world=Matrix.Identity(4)
        if obj.type=='MESH':
            bm=bmesh.new()
            bm.from_mesh(obj.data)
            bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
            bm.to_mesh(obj.data)
            bm.free()
