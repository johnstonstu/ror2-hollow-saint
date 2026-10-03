"""Read the saved animation checkpoint in isolated Blender; never save it."""
import json
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[2]
assert bpy.app.background
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'art/anim/hollow-saint-anim-v31.blend'))
rig = bpy.data.objects['Hollow Saint | v8 rig']
report = {
    'bones': [{'name': b.name, 'deform': b.use_deform, 'parent': b.parent.name if b.parent else None}
              for b in rig.data.bones],
    'objects': [{'name': o.name, 'type': o.type, 'parent': o.parent.name if o.parent else None,
                 'parent_bone': o.parent_bone, 'hidden': o.hide_render,
                 'armature': [m.object.name for m in o.modifiers if m.type == 'ARMATURE' and m.object]}
                for o in bpy.data.objects],
    'materials': [{'name': m.name, 'nodes': [(n.name, n.type) for n in m.node_tree.nodes] if m.use_nodes else []}
                  for m in bpy.data.materials],
    'images': [{'name': i.name, 'size': list(i.size), 'packed': bool(i.packed_file), 'path': i.filepath}
               for i in bpy.data.images],
}
out = ROOT / 'artifacts/unity-setup/source-inspection.json'
out.write_text(json.dumps(report, indent=2))
print('INSPECTED', len(report['objects']), 'objects', len(report['bones']), 'bones')
