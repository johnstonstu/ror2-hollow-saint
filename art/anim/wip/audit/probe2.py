import bpy
from mathutils import Vector
def wl(n):
    o=bpy.data.objects.get(n); 
    if not o: return None
    return [round(x,3) for x in (o.matrix_world @ (sum((Vector(c) for c in o.bound_box),Vector())/8))]
for n in ["CHEST | V11 core ring","BACK | V11 upper spine conductor","MASK | V11 cyan centre stripe","L HAND | thumb segment 1","L HAND | index knuckle","L HAND | little knuckle","L HAND | tapered sculpted palm","R HAND | thumb segment 1","R HAND | index knuckle","R HAND | little knuckle","L HAND | index segment 3","L HAND | middle segment 3","R HAND | middle segment 3"]:
    o=bpy.data.objects[n]; print(n, wl(n), o.parent.name if o.parent else None, o.parent_type, o.parent_bone, [m.type for m in o.modifiers])
for n in ["Front orthographic","Side orthographic","Hero three quarter"]:
    o=bpy.data.objects[n]; print(n,[round(x,2) for x in o.matrix_world.translation], [round(x,2) for x in (o.matrix_world.to_3x3() @ Vector((0,0,-1)))])
arm=bpy.data.objects["Hollow Saint | v8 rig"]
print("armrot",arm.matrix_world.to_euler(), arm.animation_data.action_slot if hasattr(arm.animation_data,'action_slot') else 'noslot')
b=arm.data.bones["L hand"]; print("Lhand rest mat", b.matrix_local)
b=arm.data.bones["R hand"]; print("Rhand rest mat", b.matrix_local)
