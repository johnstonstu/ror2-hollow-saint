import bpy
bpy.ops.wm.open_mainfile(filepath=r"C:\Users\stuwj\Documents\Coding\ror2-lightning\art\hybrid\hollow-saint-hybrid-v18.blend")
rig = bpy.data.objects["Hollow Saint | v8 rig"]
for b in rig.data.bones:
    print("BONE", b.name, "|", b.parent.name if b.parent else None, "|",
          tuple(round(v, 3) for v in b.head_local), tuple(round(v, 3) for v in b.tail_local), b.use_deform)
for c in rig.children:
    print("CHILD", c.name, c.type, c.parent_type, c.parent_bone)
