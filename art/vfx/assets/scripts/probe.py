import bpy, json, os
out = {"version": bpy.app.version_string,
       "engines": [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items],
       "fbx": hasattr(bpy.ops.export_scene, 'fbx'),
       "compositing_node_group": hasattr(bpy.types.Scene, 'compositing_node_group')}
try:
    import numpy
    out["numpy"] = numpy.__version__
except Exception as e:
    out["numpy"] = str(e)
p = os.path.join(os.path.dirname(os.path.abspath(__file__)), "probe_out.json")
with open(p, "w") as f:
    json.dump(out, f, indent=1)
