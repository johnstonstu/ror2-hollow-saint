"""Parenting / modifiers / weight groups of objects whose name contains a substring (after open_start fixes).
blender -b --factory-startup --python obj_info.py -- "<substring>" """
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
import hs_anim as H

sub = sys.argv[sys.argv.index('--')+1]
p = H.open_start()
for o in bpy.data.objects:
    if sub in o.name:
        groups = {}
        names = {g.index: g.name for g in o.vertex_groups}
        if o.type == 'MESH':
            for v in o.data.vertices:
                for g in v.groups:
                    groups[names[g.group]] = max(groups.get(names[g.group], 0.0), round(g.weight, 2))
        print('OBJ', o.name, o.type, 'parent', o.parent.name if o.parent else None, o.parent_type, o.parent_bone,
              'mods', [m.type for m in getattr(o, 'modifiers', [])], 'verts', len(o.data.vertices) if o.type == 'MESH' else 0,
              'groups', groups)
