import bpy
for o in bpy.data.objects:
    print("OBJ",o.name,o.type, o.animation_data.action.name if o.animation_data and o.animation_data.action else None)
for a in bpy.data.actions:
    print("ACT",a.name,tuple(a.frame_range), a.use_fake_user)
arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]
import re
for b in arm.data.bones:
    if re.search(r'(?i)arm|hand|thumb|index|pinky|wrist|shoulder|clav|elbow|spine|chest|hip|pelvis|root|forearm|finger|middle|ring',b.name):
        print("BONE",b.name,b.parent.name if b.parent else None, [round(x,3) for x in b.head_local],[round(x,3) for x in b.tail_local])
print("SCENES",[ (s.name,s.frame_start,s.frame_end, s.camera.name if s.camera else None) for s in bpy.data.scenes])
print("MARKERS",[(m.name,m.frame) for s in bpy.data.scenes for m in s.timeline_markers][:80])
