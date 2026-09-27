import bpy, json, math, sys, os
from mathutils import Vector, Matrix
OUT=os.path.dirname(bpy.data.filepath)
arm=bpy.data.objects["Hollow Saint | v8 rig"]
sc=bpy.context.scene
def cen(n):
    o=bpy.data.objects[n]; return o.matrix_world @ (sum((Vector(c) for c in o.bound_box),Vector())/8)
def bw(pb,tail=False):
    m=arm.matrix_world @ pb.matrix; 
    return m.translation.copy() if not tail else (arm.matrix_world @ (pb.matrix @ Vector((0,pb.length,0))))
P=arm.pose.bones
chest_rest=arm.data.bones["chest"].matrix_local.to_3x3()
def measure(side):
    S=side
    ua_h=bw(P[f"{S} upperarm"]); ua_t=bw(P[f"{S} upperarm"],True)
    fa_h=bw(P[f"{S} forearm"]); fa_t=bw(P[f"{S} forearm"],True)
    wrist=bw(P[f"{S} hand"])
    u=(ua_t-ua_h).normalized(); f=(fa_t-fa_h).normalized()
    elev=math.degrees(u.angle(Vector((0,0,-1))))
    elbow=180-math.degrees(u.angle(f))
    ik=cen(f"{S} HAND | index knuckle"); lk=cen(f"{S} HAND | little knuckle"); mk=cen(f"{S} HAND | middle knuckle")
    m3=cen(f"{S} HAND | middle segment 3")
    r=(ik-lk).normalized()
    d=(((ik+lk)/2)-wrist).normalized()
    n=(d.cross(r) if S=="L" else r.cross(d)).normalized()
    curl=(m3-mk); curl=(curl-curl.dot(d)*d)
    chir=curl.normalized().dot(n) if curl.length>1e-4 else 0
    tb=(bw(P[f"{S} thumb.1"],True)-bw(P[f"{S} thumb.1"])).normalized()
    ttip=cen(f"{S} HAND | V11 thumb tip light")-((ik+lk)/2)
    cm=arm.matrix_world.to_3x3() @ P["chest"].matrix.to_3x3() @ chest_rest.inverted()
    fwd=cm @ Vector((0,-1,0)); fwd.z=0; fwd.normalize()
    return dict(elev=elev,elbow=elbow,thumb_radial_up=r.z,thumb_bone_up=tb.z,thumb_tip_up=ttip.normalized().z,palm_fwd=n.dot(fwd),palm_up=n.z,chir=chir,fwd=list(fwd))
res={}
acts=[a for a in bpy.data.actions if a.name.startswith("HS_anim |")]
for a in acts:
    arm.animation_data.action=a
    try: arm.animation_data.action_slot=a.slots[0]
    except Exception as e: pass
    f0,f1=int(a.frame_range[0]),int(a.frame_range[1])
    rows=[]
    for fr in range(f0,f1+1):
        sc.frame_set(fr)
        rows.append(dict(frame=fr,L=measure("L"),R=measure("R")))
    res[a.name[10:]]=rows
    print("done",a.name,flush=True)
json.dump(res,open(os.path.join(OUT,"orient_raw.json"),"w"),indent=0)
