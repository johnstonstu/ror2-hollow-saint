import bpy, json, sys
from collections import defaultdict
OUT = bpy.path.abspath('//')
res = {'objects': [], 'bones': [], 'actions': [], 'drivers': []}
rigs = [o for o in bpy.data.objects if o.type == 'ARMATURE']
for o in bpy.data.objects:
    d = {'name': o.name, 'type': o.type, 'parent': o.parent.name if o.parent else None,
         'parent_type': o.parent_type, 'parent_bone': o.parent_bone, 'hide_render': o.hide_render,
         'visible': o.visible_get(), 'constraints': [(c.type, c.name, getattr(c, 'subtarget', ''), round(c.influence, 3), c.mute) for c in o.constraints]}
    if o.type == 'MESH':
        me = o.data
        d['verts'] = len(me.vertices)
        d['mods'] = [(m.type, getattr(getattr(m, 'object', None), 'name', None)) for m in o.modifiers]
        names = {g.index: g.name for g in o.vertex_groups}
        arm = [m for m in o.modifiers if m.type == 'ARMATURE']
        bonenames = set()
        if arm and arm[0].object:
            bonenames = {b.name for b in arm[0].object.data.bones if b.use_deform}
        dom = defaultdict(int); multi = 0; unweighted = 0; gsum = defaultdict(float)
        for v in me.vertices:
            ws = [(names.get(g.group), g.weight) for g in v.groups if g.weight > 0.01 and names.get(g.group) in bonenames]
            if not ws: unweighted += 1; continue
            ws.sort(key=lambda x: -x[1])
            dom[ws[0][0]] += 1
            if len(ws) > 1 and ws[1][1] > 0.05: multi += 1
            for n, w in ws: gsum[n] += w
        d['deform_groups_used'] = sorted(gsum, key=lambda n: -gsum[n])[:8]
        d['n_deform_groups'] = len(gsum)
        d['multi_weight_frac'] = round(multi / max(1, len(me.vertices)), 3)
        d['unweighted'] = unweighted
        d['dominant'] = dict(sorted(dom.items(), key=lambda x: -x[1])[:5])
        d['bbox_dims'] = [round(x, 3) for x in o.dimensions]
    if o.animation_data:
        for fc in o.animation_data.drivers:
            res['drivers'].append({'owner': o.name, 'path': fc.data_path, 'idx': fc.array_index, 'expr': fc.driver.expression, 'mute': fc.mute})
    res['objects'].append(d)
for r in rigs:
    for pb in r.pose.bones:
        b = pb.bone
        res['bones'].append({'rig': r.name, 'name': b.name, 'parent': b.parent.name if b.parent else None, 'deform': b.use_deform,
            'head': [round(x, 4) for x in b.head_local], 'tail': [round(x, 4) for x in b.tail_local], 'length': round(b.length, 4),
            'roll_axis_z': [round(x, 4) for x in b.matrix_local.col[2][:3]], 'x_axis': [round(x, 4) for x in b.matrix_local.col[0][:3]],
            'constraints': [(c.type, c.name, getattr(c, 'subtarget', ''), round(c.influence, 3), c.mute) for c in pb.constraints],
            'rotation_mode': pb.rotation_mode, 'inherit_rotation': b.use_inherit_rotation, 'connect': b.use_connect})
    if r.animation_data:
        res['rig_nla'] = [(t.name, t.mute, [s.action.name if s.action else None for s in t.strips]) for t in r.animation_data.nla_tracks]
        res['rig_action'] = r.animation_data.action.name if r.animation_data.action else None
for key in ('meshes',):
    pass
for id_ in list(bpy.data.meshes) + list(bpy.data.materials) + list(bpy.data.shape_keys):
    ad = getattr(id_, 'animation_data', None)
    if ad:
        for fc in ad.drivers:
            res['drivers'].append({'owner': id_.name, 'path': fc.data_path, 'idx': fc.array_index, 'expr': fc.driver.expression, 'mute': fc.mute})
for a in bpy.data.actions:
    res['actions'].append((a.name, list(a.frame_range), a.users, len(a.slots) if hasattr(a, 'slots') else None))
sc = bpy.context.scene
res['scene'] = {'fps': sc.render.fps, 'cams': [o.name for o in bpy.data.objects if o.type == 'CAMERA'], 'engine': sc.render.engine}
json.dump(res, open(OUT + 'rig_inspect.json', 'w'), indent=1)
print('DONE', len(res['objects']), len(res['bones']))
