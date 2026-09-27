"""Shoulder deformation metrics on a fresh v18 (never saved): stretch, collapse and pinch of the
skin weighted to the upper arms, on a controlled raise sweep and on chosen clip frames.

Run:
  blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/shoulder_diag.py -- \
      --tag base [--fix] [--sweep] [--clips "primary:Arc Bolt right:5,8;special:Discharge:9"] [--render]
Writes art/anim/wip/shoulder/<tag>.json (+ close-up PNGs with --render).
"""
import bpy
import importlib
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).parent))
sys.path.insert(0, str(Path(__file__).parent/'clips'))
import hs_anim as H

args = sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []


def opt(name, default):
    return args[args.index(name)+1] if name in args else default


tag = opt('--tag', 'base')
out = H.ROOT/'art/anim/wip/shoulder'
out.mkdir(parents=True, exist_ok=True)
p = H.open_start(fix='--fix' in args)
p.rig.animation_data.action = None
body = bpy.data.objects[H.BODY]
me = body.data
scene = bpy.context.scene

# Region: vertices within 20 cm of the upper-arm head (fixed geometry, comparable across weight fixes).
gi = {g.index: g.name for g in body.vertex_groups}
rest_co = [body.matrix_world @ v.co for v in me.vertices]
region = {s: {i for i, c in enumerate(rest_co) if (c-p.rest[f'{s} upperarm'].translation).length < 0.20}
          for s in H.SIDES}
edges = {s: [e.vertices[:] for e in me.edges if e.vertices[0] in region[s] and e.vertices[1] in region[s]
             and (rest_co[e.vertices[0]]-rest_co[e.vertices[1]]).length > 0.002] for s in H.SIDES}
faces = {s: [f.vertices[:] for f in me.polygons if all(i in region[s] for i in f.vertices)] for s in H.SIDES}


def area(co, idx):
    a = Vector((0, 0, 0))
    for i in range(1, len(idx)-1):
        a += (co[idx[i]]-co[idx[0]]).cross(co[idx[i+1]]-co[idx[0]])
    return a


rest_len = {s: [(rest_co[a]-rest_co[b]).length for a, b in edges[s]] for s in H.SIDES}
rest_area = {s: [area(rest_co, f) for f in faces[s]] for s in H.SIDES}


def measure():
    p.update()
    dg = bpy.context.evaluated_depsgraph_get()
    ev = body.evaluated_get(dg)
    m = ev.to_mesh()
    co = [body.matrix_world @ v.co for v in m.vertices]
    ev.to_mesh_clear()
    res = {}
    for s in H.SIDES:
        ratios = [(co[a]-co[b]).length/max(l, 1e-6) for (a, b), l in zip(edges[s], rest_len[s])]
        flips = collapse = 0
        for f, a0 in zip(faces[s], rest_area[s]):
            a1 = area(co, f)
            if a1.length < 0.3*a0.length:
                collapse += 1
        # Flips: posed face normal against the normal of the same face under the dominant bone's
        # rotation (upper arm for arm-heavy faces, chest otherwise).
        ua = p.pb[f'{s} upperarm'].matrix.to_3x3() @ p.r3[f'{s} upperarm'].inverted()
        ch = p.pb['chest'].matrix.to_3x3() @ p.r3['chest'].inverted()
        for f, a0 in zip(faces[s], rest_area[s]):
            a1 = area(co, f)
            if a1.length < 1e-9 or a0.length < 1e-9:
                continue
            ref_ua, ref_ch = (ua @ a0).normalized(), (ch @ a0).normalized()
            n1 = a1.normalized()
            if max(n1.dot(ref_ua), n1.dot(ref_ch)) < -0.2:
                flips += 1
        up = p.pb[f'{s} upperarm'].matrix.to_3x3().col[1]
        raise_deg = math.degrees(up.angle(p.r3[f'{s} upperarm'].col[1]))
        elev = math.degrees(math.asin(max(-1, min(1, up.z))))
        res[s] = {'raise_deg': round(raise_deg, 1), 'arm_elevation_deg': round(elev, 1),
                  'stretched_1.35': sum(r > 1.35 for r in ratios), 'stretched_2': sum(r > 2 for r in ratios),
                  'compressed_0.6': sum(r < 0.6 for r in ratios),
                  'max_stretch': round(max(ratios), 2), 'min_ratio': round(min(ratios), 2),
                  'collapsed_faces': collapse, 'flipped_faces': flips}
    return res


VIEW_DIRS = {'front': Vector((0, -1, 0.15)), 'back': Vector((0, 1, 0.25)), 'top': Vector((0, -0.25, 1)),
             'out': Vector((1, -0.35, 0.3))}


def closeups(name, sides=('R',)):
    """Upper-body framing (both shoulders) from front, back, top and the R outside."""
    cam = bpy.data.objects.get('DIAG shoulder cam')
    if cam is None:
        cam = bpy.data.objects.new('DIAG shoulder cam', bpy.data.cameras.new('DIAG shoulder cam'))
        scene.collection.objects.link(cam)
        cam.data.type = 'ORTHO'
        cam.data.ortho_scale = 1.05
    H.eevee(scene, 12)
    c = (p.pb['L upperarm'].matrix.translation+p.pb['R upperarm'].matrix.translation)*0.5
    c.z += 0.02
    for view, d in VIEW_DIRS.items():
        d = d.copy()
        if view == 'out':
            d.x *= -1
        cam.location = c+d.normalized()*2.5
        cam.rotation_euler = (c-cam.location).to_track_quat('-Z', 'Y').to_euler()
        H.render_still(scene, cam.name, out/f'{tag}-{name}-{view}.png', (480, 480))


report = {'tag': tag, 'fix': '--fix' in args, 'region_verts': {s: len(region[s]) for s in H.SIDES},
          'sweep': [], 'clips': []}
if '--weights' in args:
    # Weight layout of the R shoulder skin: upper-arm weight bands and co-weighted groups.
    head = p.rest['R upperarm'].translation
    bands = {}
    partners = {}
    for i in region['R']:
        v = me.vertices[i]
        ws = {gi[g.group]: g.weight for g in v.groups if g.weight > 0.001 and gi.get(g.group) in p.bones
              and p.bones[gi[g.group]].use_deform}
        tot = sum(ws.values()) or 1
        ua = ws.get('R upperarm', 0)/tot
        b = round(ua, 1)
        d = rest_co[i]-head
        bands.setdefault(b, []).append((round(d.x, 3), round(d.y, 3), round(d.z, 3)))
        for n in ws:
            partners[n] = partners.get(n, 0)+1
    report['weights_R'] = {str(k): {'n': len(v), 'mean_offset': [round(sum(c[j] for c in v)/len(v), 3) for j in range(3)]}
                           for k, v in sorted(bands.items())}
    report['partners_R'] = partners
    print('WEIGHTS', json.dumps(report['weights_R']), json.dumps(partners), flush=True)

    def ua_frac(i):
        ws = {gi[g.group]: g.weight for g in me.vertices[i].groups if gi.get(g.group) in p.bones
              and p.bones[gi[g.group]].use_deform}
        return ws.get('R upperarm', 0)/(sum(ws.values()) or 1)
    hard = []
    for a, b in edges['R']:
        da, db = ua_frac(a), ua_frac(b)
        if abs(da-db) > 0.4:
            m = (rest_co[a]+rest_co[b])*0.5-head
            hard.append([round(m.x, 3), round(m.y, 3), round(m.z, 3), round(da, 2), round(db, 2),
                         round((rest_co[a]-rest_co[b]).length, 4)])
    near = sum(1 for c in rest_co if (c-head).length < 0.15)
    report['hard_edges_R'] = hard
    print('HARD', len(hard), 'verts within 15 cm of joint', near, 'body verts', len(rest_co), flush=True)
    for h in sorted(hard, key=lambda h: h[2]):
        print('  HARD', h, flush=True)
p.reset()
report['rest'] = measure()

if '--sweep' in args:
    # Forward raise (swing -) and side raise (adduct -) of both arms, elbow slightly bent.
    for kind, key in (('forward', 'swing'), ('side', 'adduct')):
        for deg in (30, 50, 70, 90, 110):
            p.reset()
            p.ik(0, 0)
            for s in H.SIDES:
                p.arm(s, **{key: -deg}, elbow=15)
                if kind == 'forward':
                    p.rot(f'{s} scapula', H.R(x=-28*min(deg, 90)/90))
            p.followers(True)
            r = measure()
            report['sweep'].append({'kind': kind, 'deg': deg, **r})
            print('SWEEP', kind, deg, json.dumps(r), flush=True)
            if '--render' in args and deg in (50, 90):
                closeups(f'sweep-{kind}{deg}')

if '--clips' in args:
    wanted = {}
    for item in opt('--clips', '').split(';'):
        mod, title, frames = item.split(':')
        wanted.setdefault(mod, {})[title] = [int(v) for v in frames.split(',')]
    for mod, titles in wanted.items():
        built = importlib.import_module(mod).build(p)
        for act, info in built:
            if info['title'] not in titles:
                continue
            p.rig.animation_data.action = act
            for f in titles[info['title']]:
                scene.frame_set(f)
                r = measure()
                report['clips'].append({'clip': info['title'], 'frame': f, **r})
                print('CLIP', info['title'], f, json.dumps(r), flush=True)
                if '--render' in args:
                    closeups(f"{info['title'].replace(' ', '-').lower()}-f{f}", sides=('L', 'R'))
        p.rig.animation_data.action = None

(out/f'{tag}.json').write_text(json.dumps(report, indent=1), encoding='utf-8')
print('DIAG DONE', out/f'{tag}.json', flush=True)
