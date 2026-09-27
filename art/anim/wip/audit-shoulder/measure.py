import bpy, json, math, sys
from mathutils import Vector
from mathutils.bvhtree import BVHTree
OUT = bpy.path.abspath('//')
RIG = bpy.data.objects['Hollow Saint | v8 rig']
BODY = bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
PAD = {'L': bpy.data.objects['L SHOULDER | V17 pauldron upper'], 'R': bpy.data.objects['R SHOULDER | V17 pauldron upper']}
info = {s: {'vgroups': [g.name for g in PAD[s].vertex_groups], 'mods': [m.type for m in PAD[s].modifiers],
            'solidify_thk': [m.thickness for m in PAD[s].modifiers if m.type=='SOLIDIFY']} for s in PAD}
pb = RIG.pose.bones
for s in 'LR':
    b = pb[f'{s} scapula']
    info[s]['scapula_parent'] = b.parent.name if b.parent else None
    info[s]['scapula_cons'] = [(c.type, getattr(c,'subtarget',''), c.influence, c.mute) for c in b.constraints]
names = {g.index: g.name for g in BODY.vertex_groups}
dom = [names[max(v.groups, key=lambda e: e.weight).group] if v.groups else '' for v in BODY.data.vertices]
def obst(g):
    gl = g.lower()
    if 'upperarm' in gl or gl.endswith('shoulder'): return 'upperarm'
    if 'head' in gl: return 'head'
    if 'neck' in gl: return 'neck/collar'
    if 'chest' in gl or 'spine' in gl or 'scapula' in gl or 'clav' in gl: return 'torso'
    return g or 'body'
DIRS = [Vector(d).normalized() for d in ((1,0.2,0.1),(-0.3,1,0.2),(0.1,-0.2,1),(-1,-0.4,-0.3),(0.2,0.3,-1))]
def inside(tree, q):
    votes = 0
    for i, d in enumerate(DIRS):
        n, o = 0, q
        for _ in range(24):
            h = tree.ray_cast(o, d)
            if h[0] is None: break
            n += 1; o = h[0] + d*1e-5
        votes += n % 2
        if votes >= 4 or votes + (len(DIRS)-1-i) < 4: break
    return votes >= 4
def mesh(obj, dg):
    ev = obj.evaluated_get(dg); me = ev.to_mesh(); mw = obj.matrix_world
    v = [mw @ x.co for x in me.vertices]; p = [tuple(x.vertices) for x in me.polygons]
    ev.to_mesh_clear(); return v, p
def measure():
    dg = bpy.context.evaluated_depsgraph_get()
    bv, bp = mesh(BODY, dg)
    tree = BVHTree.FromPolygons(bv, bp)
    res = {}
    for s in 'LR':
        pv, _ = mesh(PAD[s], dg)
        worst = {}
        for q in pv:
            if not inside(tree, q): continue
            loc, nrm, fi, dist = tree.find_nearest(q)
            o = obst(dom[bp[fi][0]])
            if dist > worst.get(o, (0,))[0]: worst[o] = (dist, tuple(q))
        res[s] = worst
    return res
def bone_angles():
    out = {}
    for s in 'LR':
        m = lambda n: (RIG.matrix_world @ pb[n].matrix).to_quaternion()
        pad, ua, ch = m(f'{s} pauldron'), m(f'{s} upperarm'), m('chest')
        rp, ru = pb[f'{s} pauldron'].bone.matrix_local.to_quaternion(), pb[f'{s} upperarm'].bone.matrix_local.to_quaternion()
        rc = pb['chest'].bone.matrix_local.to_quaternion()
        d_pc = ((ch @ rc.inverted()).inverted() @ (pad @ rp.inverted())).angle
        d_uc = ((ch @ rc.inverted()).inverted() @ (ua @ ru.inverted())).angle
        out[s] = (math.degrees(d_pc), math.degrees(d_uc))
    return out
sc = bpy.context.scene
ad = RIG.animation_data or RIG.animation_data_create()
for t in ad.nla_tracks: t.mute = True
def use(act):
    ad.action = act
    try:
        if ad.action_slot is None and len(act.slots): ad.action_slot = act.slots[0]
    except AttributeError: pass
# rest pose baseline
ad.action = None
for b in pb: b.location = (0,0,0); b.rotation_quaternion = (1,0,0,0); b.rotation_euler = (0,0,0); b.scale = (1,1,1)
bpy.context.view_layer.update()
rest = measure()
clips = [a for a in bpy.data.actions if a.name.startswith('HS_anim | ')]
table = []
for a in clips:
    use(a)
    f0, f1 = int(a.frame_range[0]), int(a.frame_range[1])
    per = {'L': [], 'R': []}
    ang = {'L': 0, 'R': 0}; angu = {'L': 0, 'R': 0}
    for f in range(f0, f1+1):
        sc.frame_set(f)
        r = measure()
        ba = bone_angles()
        for s in 'LR':
            ang[s] = max(ang[s], ba[s][0]); angu[s] = max(angu[s], ba[s][1])
            per[s].append((f, r[s]))
    for s in 'LR':
        byobs = {}
        for f, w in per[s]:
            for o, (d, q) in w.items():
                ex = d - rest[s].get(o, (0,))[0]
                byobs.setdefault(o, []).append((f, d, ex, q))
        for o, lst in byobs.items():
            wf = max(lst, key=lambda x: x[1])
            bad = [x[0] for x in lst if x[1] > 0.005]
            table.append(dict(clip=a.name[10:], side=s, obstacle=o, worst_mm=round(wf[1]*1000,1), excess_mm=round(wf[2]*1000,1),
                              worst_frame=wf[0], frames_over_5mm=(f'{min(bad)}-{max(bad)} ({len(bad)}f)' if bad else '-'),
                              point=[round(c,4) for c in wf[3]], pad_rot_vs_chest_deg=round(ang[s],1), arm_rot_vs_chest_deg=round(angu[s],1)))
    print('CLIP', a.name, flush=True)
json.dump(dict(info=info, rest={s: {o: round(d*1000,1) for o,(d,q) in rest[s].items()} for s in rest}, table=table), open(OUT+'shoulder-audit.json','w'), indent=1)
print('DONE')
