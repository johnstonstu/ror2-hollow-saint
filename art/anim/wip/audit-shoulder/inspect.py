import bpy
print("=== OBJECTS")
for o in bpy.data.objects:
    extra=""
    if o.type=='MESH':
        extra=f" verts={len(o.data.vertices)} vg={len(o.vertex_groups)} mods={[m.type+':'+(getattr(m,'object',None).name if getattr(m,'object',None) else '') for m in o.modifiers]}"
    print(f"{o.name!r} type={o.type} parent={o.parent.name if o.parent else None} ptype={o.parent_type} pbone={o.parent_bone!r} cons={[c.type for c in o.constraints]}{extra}")
print("=== ARMATURES")
for o in bpy.data.objects:
    if o.type=='ARMATURE':
        for pb in o.pose.bones:
            b=pb.bone
            if any(k in pb.name.lower() for k in ('paul','shoulder','clav','upperarm','chest','pad')):
                print(f"{pb.name!r} parent={b.parent.name if b.parent else None} deform={b.use_deform} cons={[(c.type,c.name,getattr(c,'subtarget',''),round(c.influence,3),c.mute) for c in pb.constraints]}")
print("=== ACTIONS")
for a in bpy.data.actions:
    print(repr(a.name), tuple(a.frame_range), a.users)
print("scenes",[ (s.name, s.frame_start,s.frame_end, s.camera.name if s.camera else None) for s in bpy.data.scenes])
