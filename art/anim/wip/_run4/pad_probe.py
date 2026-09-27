"""Read-only 9g baseline: pauldron penetration per clip on a saved checkpoint.
Run: blender -b --factory-startup --python pad_probe.py -- <blend> <out.json> [--step N] [--clips a,b]"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT/'tools/blender/anim'))
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

import hs_anim as H
from clearance import dominant_groups

args = sys.argv[sys.argv.index('--')+1:]
blend, out = args[0], args[1]
step = int(args[args.index('--step')+1]) if '--step' in args else 1
only = set(args[args.index('--clips')+1].split(',')) if '--clips' in args else None
bpy.ops.wm.open_mainfile(filepath=str(ROOT/blend))
rig = bpy.data.objects[H.RIG]
body = bpy.data.objects[H.BODY]
dom = dominant_groups(body)
PADS = {s: bpy.data.objects[f'{s} SHOULDER | V17 pauldron upper'] for s in H.SIDES}
HARD = {'arc2': 'HALO | independent copper arc 2', 'arc3': 'HALO | independent copper arc 3', 'yoke': 'HALO | V17 yoke bar'}
HARD = {k: bpy.data.objects[v] for k, v in HARD.items() if v in bpy.data.objects}
DIRS = [Vector(d).normalized() for d in ((1, 0.2, 0.1), (-0.3, 1, 0.2), (0.1, -0.2, 1), (-1, -0.4, -0.3), (0.2, 0.3, -1))]


def label(g):
    if g in ('head', 'neck'):
        return 'neck'
    if g.endswith(('upperarm', 'shoulder')):
        return g[0]+' upperarm'
    if g in ('chest', 'spine', 'pelvis'):
        return 'torso'
    return g or 'body'


polys = [tuple(p.vertices) for p in body.data.polygons]
poly_label = [label(dom[q[0]]) for q in polys]


def mesh(o, dg):
    ev = o.evaluated_get(dg)
    me = ev.to_mesh()
    v = [o.matrix_world @ x.co for x in me.vertices]
    p = [tuple(x.vertices) for x in me.polygons]
    ev.to_mesh_clear()
    return v, p


def inside(tree, q, need=4):
    votes = 0
    for k, d in enumerate(DIRS):
        n, o = 0, q
        for _ in range(24):
            hit = tree.ray_cast(o, d)
            if hit[0] is None:
                break
            n += 1
            o = hit[0]+d*1e-5
        votes += n % 2
        if votes >= need or votes+(len(DIRS)-1-k) < need:
            break
    return votes >= need


def frame():
    dg = bpy.context.evaluated_depsgraph_get()
    bv, _ = mesh(body, dg)
    solid = BVHTree.FromPolygons(bv, polys)
    hard = {k: mesh(o, dg) for k, o in HARD.items()}
    res = {}
    for s, pad in PADS.items():
        pv, pp = mesh(pad, dg)
        ptree = BVHTree.FromPolygons(pv, pp)
        d = {}
        for q in pv:
            loc, _, idx, dist = solid.find_nearest(q, 0.1)
            if loc is not None and dist > 0.0005 and inside(solid, q):
                g = poly_label[idx]
                d[g] = max(d.get(g, 0.0), dist)
        for k, (hv, hp) in hard.items():
            htree = BVHTree.FromPolygons(hv, hp)
            m = 0.0
            for q in hv:
                loc, _, _, dist = ptree.find_nearest(q, 0.05)
                if loc is not None and inside(ptree, q, 3):
                    m = max(m, dist)
            for q in pv:
                loc, _, _, dist = htree.find_nearest(q, 0.05)
                if loc is not None and inside(htree, q, 3):
                    m = max(m, dist)
            pairs = len(ptree.overlap(htree))
            if m > 0 or pairs:
                d[k] = max(d.get(k, 0.0), m)
                d[k+'_tris'] = pairs
        res[s] = d
    return res


rig.animation_data_create()
rig.animation_data.action = None
rig.data.pose_position = 'POSE'
for pb in rig.pose.bones:
    pb.location = (0, 0, 0)
    pb.rotation_quaternion = (1, 0, 0, 0)
    pb.rotation_euler = (0, 0, 0)
    pb.scale = (1, 1, 1)
bpy.context.view_layer.update()
rest = frame()
report = {'rest': rest, 'clips': {}}
for act in bpy.data.actions:
    if not act.name.startswith(H.PREFIX):
        continue
    title = act.name[len(H.PREFIX):]
    if only and title not in only:
        continue
    rig.animation_data.action = act
    if hasattr(rig.animation_data, 'action_slot') and act.slots:
        rig.animation_data.action_slot = act.slots[0]
    f0, f1 = (int(x) for x in act.frame_range)
    worst = {}
    for f in range(f0, f1+1, step):
        bpy.context.scene.frame_set(f)
        r = frame()
        for s in H.SIDES:
            for g, v in r[s].items():
                if g.endswith('_tris'):
                    continue
                ex = v-rest[s].get(g, 0.0)
                key = f'{s} > {g}'
                w = worst.setdefault(key, {'mm': 0.0, 'f': f, 'frames_over_5': 0, 'tris': 0})
                if ex > w['mm']/1000:
                    w['mm'], w['f'] = round(ex*1000, 1), f
                if ex > 0.005:
                    w['frames_over_5'] += 1
                w['tris'] = max(w['tris'], r[s].get(g+'_tris', 0))
    report['clips'][title] = {k: v for k, v in sorted(worst.items(), key=lambda kv: -kv[1]['mm'])}
    top = list(report['clips'][title].items())[:3]
    print('PAD', title, json.dumps(top), flush=True)
Path(ROOT/out).write_text(json.dumps(report, indent=1))
print('PADREST', json.dumps(rest), flush=True)
