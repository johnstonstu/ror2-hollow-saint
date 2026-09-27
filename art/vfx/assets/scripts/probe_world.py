import bpy, os, json
bpy.ops.wm.open_mainfile(filepath=os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'hs-vfx-v03.blend'))
w = bpy.context.scene.world
out = {'world': w.name if w else None, 'has_use_nodes': hasattr(w, 'use_nodes'),
       'use_nodes': getattr(w, 'use_nodes', None), 'node_tree': bool(w.node_tree),
       'nodes': [n.bl_idname for n in w.node_tree.nodes] if w.node_tree else [],
       'color': tuple(w.color)}
w2 = bpy.data.worlds.new('probe')
out['new_world_node_tree'] = bool(w2.node_tree)
out['new_world_nodes'] = [n.bl_idname for n in w2.node_tree.nodes] if w2.node_tree else []
with open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'logs', 'probe_world.json'), 'w') as f:
    json.dump(out, f, indent=1)
