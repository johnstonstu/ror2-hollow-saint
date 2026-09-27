"""Full-body audit (read-only, on a COPY). Usage:
blender --background --factory-startup audit-full-v16-copy.blend --python audit_measure.py -- rest
blender ... -- anim "Clip A;Clip B" TAG
Writes rest_audit.json / anim_<TAG>.json next to the blend. Never saves the blend."""
import bpy, sys, json, math, time
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
from collections import defaultdict

args = sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['rest']
MODE = args[0]
OUT = bpy.path.abspath('//')
sc = bpy.context.scene
RIG = bpy.data.objects['Hollow Saint | v8 rig']; ad = RIG.animation_data
for t in ad.nla_tracks: t.mute = True
BODY = bpy.data.objects['HF BODY | retained UV sculpt, corrected posterior']
GROUND_Z = 0.003
SHELL = 0.004
DIRS = [Vector(d).normalized() for d in ((1, 0.2, 0.1), (-0.3, 1, 0.2), (0.1, -0.2, 1), (-1, -0.4, -0.3), (0.2, 0.3, -1))]
deform = {b.name for b in RIG.data.bones if b.use_deform}

def dominant(o):
    names = {g.index: g.name for g in o.vertex_groups}
    out = []
    for v in o.data.vertices:
        best, bw = None, 0.0
        for g in v.groups:
            n = names.get(g.group)
            if n in deform and g.weight > bw: best, bw = n, g.weight
        out.append(best)
    return out

def region_of(g):
    if g is None: return 'misc'
    for s in 'LR':
        if g in (f'{s} upperarm', f'{s} shoulder', f'{s} pauldron'): return f'{s} upperarm'
        if g == f'{s} forearm': return f'{s} forearm'
        if g.startswith(s+' ') and any(k in g for k in ('hand', 'index', 'middle', 'ring', 'little', 'thumb')): return f'{s} hand'
        if g == f'{s} thigh': return f'{s} thigh'
        if g == f'{s} shin': return f'{s} shin'
        if g in (f'{s} foot', f'{s} toe'): return f'{s} foot'
    if g.startswith('tabard'): return 'pelvis'
    return g if g in ('head', 'neck', 'chest', 'spine', 'pelvis') else 'misc'

def group_of(o):
    n = o.name
    if n.startswith(('VFX', 'Warm', 'V11 bust')): return None
    if o.type not in ('MESH', 'CURVE'): return None
    if n.startswith('L HAND |'): return 'L hand parts'
    if n.startswith('R HAND |'): return 'R hand parts'
    if n.startswith('TABARD |'): return 'tabard back' if ' back ' in n else 'tabard front'
    if n.startswith('HALO'):
        if o.parent_type == 'BONE' and o.parent_bone.startswith('halo'): return 'halo arc ' + o.parent_bone.split()[-1]
        return 'yoke'
    if n.startswith('L SHOULDER'): return 'L pauldron'
    if n.startswith('R SHOULDER'): return 'R pauldron'
    if n.startswith('L BACK'): return 'L scapula shell'
    if n.startswith('R BACK'): return 'R scapula shell'
    if n.startswith('CHEST |'):
        if 'rib plate' in n: return 'rib plate ' + n[-1]
        if 'upper chest plate' in n: return 'chest plate ' + n[-1]
        return 'chest core'
    if n.startswith('ABDOMEN'): return 'abdomen plates'
    if n.startswith('BACK |'): return 'back node' if 'node' in n else 'back conductors'
    if n.startswith('MASK'): return 'mask'
    if n.startswith('NECK'): return 'neck cables'
    if n.startswith('L ARM'): return 'L forearm conductor'
    if n.startswith('R ARM'): return 'R forearm conductor'
    if n.startswith('HF BODY'): return None
    return 'other:' + n

OBJ_GROUPS = defaultdict(list)
for o in bpy.data.objects:
    g = group_of(o)
    if g and not o.hide_render: OBJ_GROUPS[g].append(o)

bdom = dominant(BODY)
breg = [region_of(g) for g in bdom]
REGIONS = sorted(set(breg))
reg_idx = {r: np.array([i for i, x in enumerate(breg) if x == r]) for r in REGIONS}

def ev_mesh(o, dg):
    ev = o.evaluated_get(dg)
    me = ev.to_mesh()
    n = len(me.vertices)
    co = np.empty(n*3, dtype=np.float64); me.vertices.foreach_get('co', co); co = co.reshape(-1, 3)
    mw = np.array(o.matrix_world)
    co = co @ mw[:3, :3].T + mw[:3, 3]
    polys = [tuple(p.vertices) for p in me.polygons]
    ne = len(me.edges); ed = np.empty(ne*2, dtype=np.int64); me.edges.foreach_get('vertices', ed)
    ev.to_mesh_clear()
    return co, polys, ed.reshape(-1, 2)

def closed(polys, nv):
    cnt = defaultdict(int)
    for p in polys:
        for i in range(len(p)):
            a, b = p[i], p[(i+1) % len(p)]
            cnt[(a, b) if a < b else (b, a)] += 1
    return len(cnt) > 0 and all(c == 2 for c in cnt.values())

def inside(tree, q):
    votes = 0
    for k, d in enumerate(DIRS):
        n, o = 0, q
        for _ in range(24):
            hit = tree.ray_cast(o, d)
            if hit[0] is None: break
            n += 1; o = hit[0] + d*1e-5
        votes += n % 2
        if votes >= 4 or votes + (len(DIRS)-1-k) < 4: break
    return votes >= 4

CLOSED = {}
class Frame:
    """Evaluated geometry for one frame."""
    def __init__(self):
        dg = bpy.context.evaluated_depsgraph_get()
        self.bco, self.bpolys, self.bedges = ev_mesh(BODY, dg)
        if len(self.bco) != len(breg): raise RuntimeError('body topology changed')
        self.groups = {}
        for g, objs in OBJ_GROUPS.items():
            cos, polys, edges, per = [], [], [], []
            base = 0; allclosed = True
            for o in objs:
                co, p, e, = ev_mesh(o, dg)
                if not p: continue
                if o.name not in CLOSED: CLOSED[o.name] = closed(p, len(co))
                allclosed &= CLOSED[o.name]
                cos.append(co); polys += [tuple(base+i for i in q) for q in p]; per.append((o.name, base, len(co), e))
                base += len(co)
            if not polys: continue
            co = np.vstack(cos)
            self.groups[g] = dict(co=co, polys=polys, closed=allclosed, per=per)
        self._trees = {}
    def body_polys(self, exclude):
        key = tuple(sorted(exclude))
        if key not in self._trees:
            ps = [p for p in self.bpolys if not any(breg[v] in exclude for v in p)]
            tree = BVHTree.FromPolygons([Vector(v) for v in self.bco], ps)
            self._trees[key] = (tree, [breg[p[0]] for p in ps])
        return self._trees[key]
    def region_tree(self, r):
        k = ('R', r)
        if k not in self._trees:
            s = set(reg_idx[r].tolist())
            ps = [p for p in self.bpolys if sum(v in s for v in p) * 2 > len(p)]
            self._trees[k] = BVHTree.FromPolygons([Vector(v) for v in self.bco], ps) if ps else None
        return self._trees[k]
    def group_tree(self, g):
        k = ('G', g)
        if k not in self._trees:
            d = self.groups[g]
            self._trees[k] = BVHTree.FromPolygons([Vector(v) for v in d['co']], d['polys'])
        return self._trees[k]
    def part_tree(self, p):
        return self.region_tree(p[5:]) if p.startswith('body:') else self.group_tree(p)
    def part_bbox(self, p):
        co = self.bco[reg_idx[p[5:]]] if p.startswith('body:') else self.groups[p]['co']
        return co.min(0), co.max(0)

PARTS = None
def all_parts(fr):
    return ['body:' + r for r in REGIONS if r != 'misc'] + sorted(fr.groups)

LIMB = {'L arm': ('L upperarm', 'L forearm', 'L hand'), 'R arm': ('R upperarm', 'R forearm', 'R hand'),
        'L leg': ('L thigh', 'L shin', 'L foot'), 'R leg': ('R thigh', 'R shin', 'R foot')}

def probes(fr):
    """(probe label, points array, vertex ids, solid tree, face labels, kind)."""
    out = []
    for limb, regs in LIMB.items():
        ids = np.concatenate([reg_idx[r] for r in regs if r in reg_idx])[::2]
        tree, labels = fr.body_polys(set(regs))
        out.append(('body ' + limb, ids, fr.bco[ids], tree, labels))
    full, flabels = fr.body_polys(set())
    for g, d in fr.groups.items():
        ids = np.arange(len(d['co']))[::2]
        out.append((g, ids, d['co'][ids], full, flabels))
    return out

def depth_scan(fr, label, pts, tree, labels, maxd=0.08):
    """per point: (depth, obstacle label) or (0, None)."""
    res = []
    for q in pts:
        q = Vector(q)
        loc, _, idx, dist = tree.find_nearest(q, maxd)
        if loc is not None and dist > 0.0005 and inside(tree, q):
            res.append((dist, labels[idx] if labels else None))
        else:
            res.append((0.0, None))
    return res

def obj_depth(fr, ga, gb):
    """depth of ga vertices inside gb (closed) or within shell of gb (sheet)."""
    A = fr.groups[ga] if not ga.startswith('body:') else None
    pts = fr.bco[reg_idx[ga[5:]]] if A is None else A['co']
    B = fr.groups.get(gb)
    tree = fr.part_tree(gb)
    lo, hi = fr.part_bbox(gb); lo = lo - 0.01; hi = hi + 0.01
    m = np.all((pts >= lo) & (pts <= hi), axis=1)
    best = 0.0
    isclosed = B['closed'] if B else False
    for q in pts[m]:
        q = Vector(q)
        if isclosed:
            loc, _, idx, dist = tree.find_nearest(q, 0.05)
            if loc is not None and dist > 0.0005 and inside(tree, q): best = max(best, dist)
        else:
            if B is None: continue  # body regions handled by probe scans
            loc, _, _, dist = tree.find_nearest(q, SHELL)
            if loc is not None: best = max(best, SHELL - dist)
    return best

def bone_state():
    mw = RIG.matrix_world
    d = {}
    for pb in RIG.pose.bones:
        m = mw @ pb.matrix
        d[pb.name] = [list(m[i][:4]) for i in range(3)] + [list((mw @ pb.tail)[:])]
    return d

def feet(fr):
    out = {}
    for s in 'LR':
        ids = reg_idx[f'{s} foot']
        co = fr.bco[ids]
        z = co[:, 2]
        m = z < GROUND_Z + 0.03
        out[s] = dict(minz=float(z.min()), low_ids=ids[m].tolist(), low_co=np.round(co[m], 5).tolist())
    return out

REST = {}
MINEDGE = 0.004
def edge_ratios(fr):
    L = np.linalg.norm(fr.bco[fr.bedges[:, 0]] - fr.bco[fr.bedges[:, 1]], axis=1)
    r = L / np.maximum(REST['body_edge'], 1e-6)
    out = {}
    for reg, mask in REST['edge_reg'].items():
        mask = mask[REST['body_edge'][mask] >= MINEDGE]
        rr = r[mask]
        if len(rr) == 0: continue
        imax, imin = int(rr.argmax()), int(rr.argmin())
        out[reg] = [round(float(rr.max()), 3), round(float(rr.min()), 3), int((rr > 1.4).sum()), int((rr < 0.6).sum())]
    rig = {}
    for g, d in fr.groups.items():
        for name, base, n, e in d['per']:
            if name not in REST['obj_edge'] or len(e) == 0: continue
            co = d['co']
            Lo = np.linalg.norm(co[base+e[:, 0]] - co[base+e[:, 1]], axis=1)
            R0 = REST['obj_edge'][name]; ok = R0 >= MINEDGE
            if not ok.any(): continue
            rr = Lo[ok] / R0[ok]
            rig[name] = round(float(np.abs(rr - 1).max()), 3)
    return out, rig

def overlap_counts(fr, pairs):
    out = {}
    for a, b in pairs:
        la, ha = fr.part_bbox(a); lb, hb = fr.part_bbox(b)
        if np.any(la > hb + 0.005) or np.any(lb > ha + 0.005): continue
        ta, tb = fr.part_tree(a), fr.part_tree(b)
        if ta is None or tb is None: continue
        n = len(ta.overlap(tb))
        if n: out[a + ' || ' + b] = n
    return out

def driver_of(p):
    if p.startswith('body:'): return p
    objs = OBJ_GROUPS[p]
    bones = set()
    for o in objs:
        if o.parent_type == 'BONE': bones.add(o.parent_bone)
        else: bones.add('weighted:' + o.name)
    return tuple(sorted(bones))

if len(args) > 2 and 'noshrink' in args[2]:
    for o in bpy.data.objects:
        for m in getattr(o, 'modifiers', []):
            if m.type == 'SHRINKWRAP': m.show_viewport = False; m.show_render = False
    print('SHRINKWRAP DISABLED')
# ---------------- REST ----------------
RIG.data.pose_position = 'REST'
sc.frame_set(1)
fr0 = Frame()
REST['body_edge'] = np.linalg.norm(fr0.bco[fr0.bedges[:, 0]] - fr0.bco[fr0.bedges[:, 1]], axis=1)
er = {}
for i, (a, b) in enumerate(fr0.bedges):
    ra, rb = breg[a], breg[b]
    er.setdefault(ra if ra == rb else 'seam ' + '/'.join(sorted((ra, rb))), []).append(i)
REST['edge_reg'] = {k: np.array(v) for k, v in er.items()}
REST['obj_edge'] = {}
for g, d in fr0.groups.items():
    for name, base, n, e in d['per']:
        o = bpy.data.objects[name]
        if any(m.type == 'ARMATURE' for m in getattr(o, 'modifiers', [])) and len(e):
            REST['obj_edge'][name] = np.linalg.norm(d['co'][base+e[:, 0]] - d['co'][base+e[:, 1]], axis=1)
parts = all_parts(fr0)
# body adjacency
adj = set()
for (a, b) in fr0.bedges:
    ra, rb = breg[a], breg[b]
    if ra != rb: adj.add(tuple(sorted(('body:'+ra, 'body:'+rb))))
allpairs = [(a, b) for i, a in enumerate(parts) for b in parts[i+1:] if tuple(sorted((a, b))) not in adj]
rest_ovl = overlap_counts(fr0, allpairs)
rest_probe = {}
for label, ids, pts, tree, labels in probes(fr0):
    rest_probe[label] = np.array([d for d, _ in depth_scan(fr0, label, pts, tree, labels)])

if MODE == 'rest':
    info = {'regions': {r: int(len(v)) for r, v in reg_idx.items()}, 'groups': {g: [o.name for o in v] for g, v in OBJ_GROUPS.items()},
            'closed': {g: d['closed'] for g, d in fr0.groups.items()}, 'adjacent': sorted(adj)}
    mods = {}
    for o in [BODY] + [o for o in bpy.data.objects if o.type == 'MESH']:
        for m in o.modifiers:
            if m.type == 'NODES': mods[o.name + '/' + m.name] = m.node_group.name if m.node_group else None
            if m.type == 'SHRINKWRAP': mods[o.name + '/' + m.name] = dict(target=m.target.name if m.target else None, mode=m.wrap_mode, method=m.wrap_method, offset=m.offset, vg=m.vertex_group)
            if m.type == 'SOLIDIFY' and o.name.startswith(('L SHOULDER', 'CHEST | V17 rib')): mods[o.name + '/' + m.name] = m.thickness
    info['mods'] = dict(list(mods.items())[:60])
    # rest overlap with depth, both directions, for every overlapping pair
    rows = []
    for key, n in sorted(rest_ovl.items(), key=lambda x: -x[1]):
        a, b = key.split(' || ')
        dab = obj_depth(fr0, a, b) if not b.startswith('body:') else 0.0
        dba = obj_depth(fr0, b, a) if not a.startswith('body:') else 0.0
        rows.append(dict(a=a, b=b, tri_pairs=n, depth_a_in_b_mm=round(dab*1000, 1), depth_b_in_a_mm=round(dba*1000, 1),
                         same_driver=driver_of(a) == driver_of(b), drv_a=str(driver_of(a)), drv_b=str(driver_of(b))))
    # object groups inside body regions at rest
    body_in = {}
    for label, arr in rest_probe.items():
        body_in[label] = round(float(arr.max())*1000, 1)
    info['rest_overlaps'] = rows
    info['rest_probe_max_mm'] = body_in
    # also Idle f1 posed
    RIG.data.pose_position = 'POSE'
    a = bpy.data.actions['HS_anim | Idle']; ad.action = a
    if ad.action_slot is None and len(a.slots): ad.action_slot = a.slots[0]
    sc.frame_set(1); fr1 = Frame()
    idle = overlap_counts(fr1, allpairs)
    info['idle_f1_overlaps'] = idle
    json.dump(info, open(OUT + 'rest_audit.json', 'w'), indent=1)
    print('DONE rest', len(rows)); sys.exit(0)

# ---------------- ANIM ----------------
RIG.data.pose_position = 'POSE'
clips = (open(OUT + args[1][5:]).read().strip() if args[1].startswith('list=') else args[1]).split(';')
TAG = args[2]
cand_pairs = [p for p in allpairs if driver_of(p[0]) != driver_of(p[1])]
res = {}
t0 = time.time()
for clip in clips:
    a = bpy.data.actions['HS_anim | ' + clip]; ad.action = a
    if ad.action_slot is None and len(a.slots): ad.action_slot = a.slots[0]
    f0, f1 = int(a.frame_range[0]), int(a.frame_range[1])
    step = 1
    frames = list(range(f0, f1+1, step))
    if frames[-1] != f1: frames.append(f1)
    # bones are cheap: every frame
    bones = {}
    for f in range(f0, f1+1):
        sc.frame_set(f); bones[f] = bone_state()
    per = {}
    for f in frames:
        sc.frame_set(f)
        fr = Frame()
        ovl = overlap_counts(fr, cand_pairs)
        ovl_ex = {}
        for k, n in ovl.items():
            ex = n - rest_ovl.get(k, 0)
            if ex > 0: ovl_ex[k] = ex
        dep = {}
        for label, ids, pts, tree, labels in probes(fr):
            base = rest_probe[label]
            for i, (d, obs) in enumerate(depth_scan(fr, label, pts, tree, labels)):
                ex = d - base[i]
                if ex > 0.002:
                    pl = label
                    if label.startswith('body '):
                        pl = 'body:' + breg[ids[i]]
                    k = pl + ' -> body:' + str(obs)
                    if ex > dep.get(k, (0,))[0]: dep[k] = (round(ex, 4), int(ids[i]))
        # object-object depth for pairs whose overlap grew
        oo = {}
        for k in ovl_ex:
            ga, gb = k.split(' || ')
            if ga.startswith('body:') and gb.startswith('body:'): continue
            d1 = obj_depth(fr, ga, gb) if not gb.startswith('body:') else 0.0
            d2 = obj_depth(fr, gb, ga) if not ga.startswith('body:') else 0.0
            oo[k] = round(max(d1, d2), 4)
        er_, rig_ = edge_ratios(fr)
        per[f] = dict(ovl=ovl_ex, depth={k: v[0] for k, v in dep.items()}, depth_vid={k: v[1] for k, v in dep.items()}, oo=oo, edges=er_, rigid=rig_, feet=feet(fr))
    res[clip] = dict(range=[f0, f1], step=step, bones=bones, per=per)
    print('CLIP', clip, len(frames), round(time.time()-t0, 1), flush=True)
    json.dump(res, open(OUT + f'anim_{TAG}.json', 'w'))
print('DONE anim', TAG)
