"""Stitched transition previews: clips played back to back (hard cut or cross-faded, as Unity would) on a
fresh v18 (never saved), rendered, with handoff metrics. For judging how clips flow, not single clips.

Run:
  blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/stitch.py -- <spec.json> [<spec.json> ...] [--blend art/anim/hollow-saint-anim-vN.blend] [--views hero,side,chase] [--res 400x500] [--no-render]
  --blend  play the baked actions of a checkpoint (catalog from art/anim/vN/catalog.json) instead of
           rebuilding each spec's modules; opened read-only (never saved). Much faster for many specs.
           --modules a,b (with --blend) rebuilds just those modules on top (new or changed clips).
spec.json:
  {"name": "walk-run-glide", "modules": ["walk", "run", "glide"], "ortho": 3.4,
   "segments": [{"clip": "Walk forward", "start": 1, "frames": 52},
                {"clip": "Run forward", "frames": 25, "blend": 6, "sync": true},
                {"clip": "Glide enter", "frames": 13}]}
  start   first clip frame to play (default 1). Loops wrap over their unique frames; one-shots hold the end.
  blend   cross-fade frames from the previous segment (default 0 = hard cut, so the handoff itself must
          be seamless). The outgoing clip keeps playing through the fade.
  sync    the incoming loop picks up the outgoing loop's normalized phase (blend-tree style).
  mask    "upper": the segment only drives the upper body (spine up, arms, hands, halo; `UPPER` below)
          over the previous lower-body segment, like a Unity avatar-mask layer. "lower_from": title of the
          locomotion clip to keep playing underneath (with its own start/sync rules via "lower_start").
          VFX under a mask (`layer`): hs_spear from the overlay, hs_glow = max of both layers, the rest
          (jets, sparks, hs_move, hs_jet_dir, hs_turn) from locomotion.
          "lower": "track" instead plays the spec's lower_track underneath (at the same output frame).
Optional spec keys:
  lower_track  segments (same keys) forming one continuous locomotion timeline for the whole spec, so the
               lower body can change state under an overlay (jump mid-cast, cast through a direction change).
               A segment with "clip": "@track" plays the track full body; handoffs inside the track under a
               segment are reported too (lower_track_cut).
  out          output folder under art/anim/wip (default "transitions/<name>").
  views        per-spec view list (overrides --views).
  root_motion  true: the rig travels and turns as the game would move it: velocity = the weighted sum over the
               playing clips of meta speed_mps (or per-frame speed_curve) x hs_move (character space), turned by the model yaw, which
               integrates each clip's hs_turn x meta turn_deg (+ = left). Cameras follow the character (the
               chase camera also its yaw, eased), the ground gets a checker so the travel reads, and the backdrop
               is hidden. Handoff metrics stay in place (root motion is applied after them).
Writes art/anim/wip/<out>/<view>-NNN.png, clip.json (labels for sheet.py) and stitch.json:
per-frame sources/weights, and per handoff the worst world-space jumps of key bones: position step
(m/frame) and acceleration (m/frame^2) within +-2 frames of the handoff vs the worst inside the stitch or in
either clip played standalone (so a fast gesture's own accent is not counted against its handoff),
plus the worst local rotation acceleration (deg/frame^2, the fullqa pop measure) over all bones.
"""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).parent))
sys.path.insert(0, str(Path(__file__).parent/'clips'))
import hs_anim as H
import importlib

args = sys.argv[sys.argv.index('--')+1:]


def opt(name, default):
    return args[args.index(name)+1] if name in args else default


VALUED = ('--blend', '--views', '--res', '--modules')
spec_paths = [a for i, a in enumerate(args) if a.endswith('.json') and (i == 0 or args[i-1] not in VALUED)]
specs = [json.loads(Path(s).read_text(encoding='utf-8')) for s in spec_paths]

VIEWS = {'hero': 'Hero three quarter', 'side': 'Side orthographic', 'front': 'Front orthographic',
         'back': 'Back orthographic', 'chase': 'Preview chase camera'}
default_views = opt('--views', 'hero,side,chase').split(',')
res = tuple(int(v) for v in opt('--res', '400x500').split('x'))
TRACK = ('pelvis', 'head', 'L hand', 'R hand', 'L toe', 'R toe', 'halo root', 'tabard front.3', 'tabard back.2',
         'L middle.3', 'R middle.3')
# Upper-body avatar mask (Unity): spine and everything parented under it (neck/head, halo, scapulae,
# pauldrons, arms, hands, fingers). Pelvis, legs and tabard stay with the locomotion layer.
UPPER_ROOTS = ('spine',)

checkpoint = opt('--blend', None)
clips = {}
if checkpoint:
    cp = H.ROOT/checkpoint if not Path(checkpoint).is_absolute() else Path(checkpoint)
    p = H.open_start(cp, fix=False)
    import vfx
    print('VFX', json.dumps(vfx.apply(p.rig)), flush=True)    # driver upgrades only (checkpoint already has VFX)
    ver = cp.stem.rsplit('-', 1)[-1]
    for info in json.loads((H.ROOT/'art/anim'/ver/'catalog.json').read_text(encoding='utf-8')):
        act = bpy.data.actions.get(info.get('action') or H.PREFIX+info['title'])
        if act:
            clips[info['title']] = (act, info)
    print('CHECKPOINT', cp.name, len(clips), 'clips', flush=True)
    for m in [m for m in opt('--modules', '').split(',') if m]:
        for act, info in importlib.import_module(m).build(p):
            clips[info['title']] = (act, info)
else:
    p = H.open_start()
    for m in sorted({m for s in specs for m in s['modules']}):
        for act, info in importlib.import_module(m).build(p):
            clips[info['title']] = (act, info)
rig = p.rig
if rig.animation_data is None:
    rig.animation_data_create()
scene = bpy.context.scene
scene.render.fps = H.FPS
if not bpy.data.objects.get(VIEWS['chase']):
    cam = bpy.data.objects.new(VIEWS['chase'], bpy.data.cameras.new(VIEWS['chase']))
    cam.data.lens = 32
    cam.location = (0.35, 5.2, 2.9)
    cam.rotation_euler = (Vector((0, -2.0, 1.2))-cam.location).to_track_quat('-Z', 'Y').to_euler()
    scene.collection.objects.link(cam)


def upper_set():
    out = set()
    for b in rig.pose.bones:
        q = b
        while q is not None:
            if q.name in UPPER_ROOTS:
                out.add(b.name)
                break
            q = q.parent
    return out


UPPER = upper_set()
root = p.pb['root']
from vfx import PROPS, jet_move_gain
cache = {}


def sample(title, f):
    key = (title, f)
    if key not in cache:
        act, _ = clips[title]
        rig.animation_data.action = act
        scene.frame_set(f)
        pose = {b.name: (b.location.copy(), b.rotation_quaternion.copy(), b.scale.copy()) for b in p.pb}
        props = {k: float(root.get(k, 0.0)) for k in PROPS}
        cache[key] = (pose, props)
    return cache[key]


def clip_len(title):
    act, info = clips[title]
    a, b = int(act.frame_range[0]), int(act.frame_range[1])
    return (b-a) if info['loop'] else (b-a+1), a, info['loop']


def clip_frame(title, start, k):
    n, a, loop = clip_len(title)
    if loop:
        return a+((start-a+k) % n)
    return min(start+k, a+n-1)


def blend(pa, pb_, w, bones=None):
    out = dict(pa[0])
    for name, (l, q, s) in pb_[0].items():
        if bones is not None and name not in bones:
            continue
        la, qa, sa = pa[0][name]
        qb = q if qa.dot(q) >= 0 else -q
        out[name] = (la.lerp(l, w), qa.slerp(qb, w), sa.lerp(s, w))
    props = {k: pa[1][k]+(pb_[1][k]-pa[1][k])*w for k in PROPS}
    return out, props


def smooth(x):
    x = min(max(x, 0.0), 1.0)
    return x*x*(3-2*x)


TRACK_CLIP = '@track'


def build_timeline(spec, track=None):
    """Per output frame a node: {'seg', 'clip', 'f'} (or 'track': node of the lower track for "@track"
    segments), 'lower': node playing under an upper-body overlay, 'from': (node of the outgoing segment, w)."""
    if track is None and spec.get('lower_track'):
        track = build_timeline({'segments': spec['lower_track']})
    timeline = []
    prev = None
    for i, seg in enumerate(spec['segments']):
        title = seg['clip']
        nfr = seg['frames']
        bl = seg.get('blend', 0) if prev else 0
        on_track = title == TRACK_CLIP
        start = 0 if on_track else seg.get('start', clip_len(title)[1])
        if seg.get('sync') and prev and not on_track and prev['clip'] != TRACK_CLIP:
            pn, pa_, ploop = clip_len(prev['clip'])
            n, a, loop = clip_len(title)
            pf = clip_frame(prev['clip'], prev['start'], len(timeline)-prev['t0'])
            start = a+round((pf-pa_)/pn*n) % n
        t0 = len(timeline)

        def make(t, i=i, title=title, start=start, t0=t0, seg=seg, on_track=on_track):
            k = t-t0
            if on_track:
                node = {'seg': i, 'clip': TRACK_CLIP, 'track': track[min(t, len(track)-1)]}
            else:
                node = {'seg': i, 'clip': title, 'f': clip_frame(title, start, k)}
            if seg.get('mask') == 'upper':
                if seg.get('lower') == 'track':
                    node['lower'] = track[min(t, len(track)-1)]
                elif seg.get('lower_from'):
                    lo = seg['lower_from']
                    node['lower'] = {'seg': -1, 'clip': lo, 'f': clip_frame(lo, seg.get('lower_start', 1), k)}
            return node

        for k in range(nfr):
            t = len(timeline)
            entry = make(t)
            if k < bl:
                entry['from'] = (prev['make'](t), smooth((k+1)/(bl+1)))
            timeline.append(entry)
        prev = {'clip': title, 'start': start, 't0': t0, 'make': make}
    return timeline


OVERLAY_PROPS = ('hs_spear',)      # upper-body layer owns these; hs_glow = max of both layers; the rest
                                   # (jets, sparks, travel, turn) stay with locomotion


def layer(lower, over_):
    """Upper-body overlay over a locomotion pose (the Unity avatar-mask layer, incl. its VFX rule)."""
    pose, _ = blend(lower, over_, 1.0, UPPER)
    props = dict(lower[1])
    for k in OVERLAY_PROPS:
        props[k] = over_[1][k]
    props['hs_glow'] = max(lower[1]['hs_glow'], over_[1]['hs_glow'])
    return pose, props


def pose_at(e):
    cur = pose_at(e['track']) if 'track' in e else sample(e['clip'], e['f'])
    if 'lower' in e:
        cur = layer(pose_at(e['lower']), cur)
    if 'from' in e:
        src, w = e['from']
        cur = blend(pose_at(src), cur, w)
    return cur


def label(e):
    s = f"{e['clip']} f{e['f']}" if 'f' in e else f"[{label(e['track'])}]"
    if 'lower' in e:
        s += f" / {label(e['lower'])}"
    return s


def titles(e):
    out = {e['clip']} if 'f' in e else set()
    for key in ('track', 'lower'):
        if key in e:
            out |= titles(e[key])
    if 'from' in e:
        out |= titles(e['from'][0])
    return out


def frames_of(e):
    """Every (title, clip frame) playing in node e (overlay, locomotion under it, fading sources)."""
    out = [(e['clip'], e['f'])] if 'f' in e else []
    for key in ('track', 'lower'):
        if key in e:
            out += frames_of(e[key])
    if 'from' in e:
        out += frames_of(e['from'][0])
    return out


def accent_flags(nodes):
    """(body accent, finger accent) within +-1 frame anywhere in these nodes (fullqa's accent allowance)."""
    body = finger = False
    for e in nodes:
        for title, f in frames_of(e):
            info = clips[title][1]
            body |= any(abs(f-a) <= 1 for a in info.get('accents', []))
            finger |= any(abs(f-a) <= 1 for a in info.get('finger_accents', []))
    return body, finger


def seg_key(e):
    sub = e.get('track') or e.get('lower') or {}
    return (e['seg'], sub.get('seg'))


def plain(e):
    """JSON-friendly copy of a timeline node."""
    out = {k: v for k, v in e.items() if k not in ('track', 'lower', 'from')}
    for key in ('track', 'lower'):
        if key in e:
            out[key] = plain(e[key])
    if 'from' in e:
        out['from'] = [plain(e['from'][0]), round(e['from'][1], 3)]
    return out


def apply(pose):
    for name, (l, q, s) in pose[0].items():
        b = p.pb[name]
        b.location, b.rotation_quaternion, b.scale = l, q, s
    for k, v in pose[1].items():
        root[k] = v


def layers(e):
    """(title, clip frame, weight) of the clips driving travel at timeline node e."""
    if 'track' in e:
        main = layers(e['track'])
    elif 'lower' in e:
        main = layers(e['lower'])
    else:
        main = [(e['clip'], e['f'], 1.0)]
    if 'from' not in e:
        return main
    src, w = e['from']
    return [(t, f, ww*w) for t, f, ww in main]+[(t, f, ww*(1.0-w)) for t, f, ww in layers(src)]


def root_path(timeline):
    """Per timeline frame (position xy, yaw rad) of the rig, integrated as the game's mover would."""
    x = y = yaw = 0.0
    path = []
    for e in timeline:
        vx = vy = dyaw = 0.0
        for title, f, w in layers(e):
            _, info = clips[title]
            props = sample(title, f)[1]
            curve = info.get('speed_curve')
            speed = curve[min(max(f-clip_len(title)[1], 0), len(curve)-1)] if curve else (info.get('speed_mps') or 0.0)
            # hs_move: +x = character right (Blender -X), +y = forward (Blender -Y)
            vx -= w*speed*props['hs_move_x']
            vy -= w*speed*props['hs_move_y']
            a = clip_len(title)[1]
            if f > a and info.get('turn_deg'):
                dyaw += w*math.radians(info['turn_deg'])*(props['hs_turn']-sample(title, f-1)[1]['hs_turn'])
        yaw += dyaw
        c, s = math.cos(yaw), math.sin(yaw)
        x += (c*vx-s*vy)/H.FPS
        y += (s*vx+c*vy)/H.FPS
        path.append((x, y, yaw))
    return path


def setup_follow():
    """Cameras parented to follow empties; checker ground; backdrop hidden (once). Returns (follow, chase follow)."""
    fol = bpy.data.objects.get('Stitch follow')
    if fol:
        return fol, bpy.data.objects['Stitch chase follow']
    fol = bpy.data.objects.new('Stitch follow', None)
    chase = bpy.data.objects.new('Stitch chase follow', None)
    for o in (fol, chase):
        scene.collection.objects.link(o)
    for name in VIEWS.values():
        cam = bpy.data.objects.get(name)
        if cam:
            cam.parent = chase if name == VIEWS['chase'] else fol
    ground = bpy.data.objects.get('Warm gray studio ground')
    if ground:
        mat = bpy.data.materials.new('Stitch ground checker')
        mat.use_nodes = True
        nt = mat.node_tree
        bsdf = nt.nodes['Principled BSDF']
        chk = nt.nodes.new('ShaderNodeTexChecker')
        chk.inputs['Scale'].default_value = 0.5
        chk.inputs['Color1'].default_value = (0.30, 0.29, 0.27, 1)
        chk.inputs['Color2'].default_value = (0.22, 0.215, 0.20, 1)
        tc = nt.nodes.new('ShaderNodeTexCoord')
        nt.links.new(tc.outputs['Object'], chk.inputs['Vector'])
        nt.links.new(chk.outputs['Color'], bsdf.inputs['Base Color'])
        bsdf.inputs['Roughness'].default_value = 0.8
        ground.data.materials.clear()
        ground.data.materials.append(mat)
    backdrop = bpy.data.objects.get('V11 bust backdrop')
    if backdrop:
        backdrop.hide_render = True
    return fol, chase


def rot_acc(poses, n, name):
    """Local rotation acceleration (deg/frame^2) of bone `name` at output frame n."""
    if n < 1 or n >= len(poses)-1:
        return 0.0
    q0, q1, q2 = (poses[k][0][name][1] for k in (n-1, n, n+1))
    d1 = q1 @ q0.inverted()
    d2 = q2 @ q1.inverted()
    return math.degrees((d2 @ d1.inverted()).angle) % 360.0


clip_cache = {}
INTENSITY = ('hs_glow', 'hs_jet', 'hs_spark_L', 'hs_spark_R', 'hs_spear')


def exhaust(props):
    """Character-space jet exhaust direction as vfx.py drives it: opposite the travel, tilted down hs_jet_dir."""
    m = Vector((props['hs_move_x'], props['hs_move_y']))
    m = m.normalized() if m.length > 1e-3 else Vector((0.0, 1.0))
    d = math.radians(props['hs_jet_dir'])
    return Vector((-m.x*math.cos(d), -m.y*math.cos(d), -math.sin(d)))


def vfx_steps(poses, j):
    """Per-frame change of the visible VFX from frame j-1 to j: intensity steps, and 'jet_turn' = exhaust
    direction change (deg) weighted by how lit the jets are."""
    a, b = poses[j-1][1], poses[j][1]
    out = {k: abs(b[k]-a[k]) for k in INTENSITY}
    # a turn while one side is dark reads as a relight, not a swing: weight by the dimmer side
    lit = min(a['hs_jet']*jet_move_gain(a['hs_move_x'], a['hs_move_y']),
              b['hs_jet']*jet_move_gain(b['hs_move_x'], b['hs_move_y']))
    out['jet_turn'] = math.degrees(exhaust(a).angle(exhaust(b), 0.0))*min(1.0, lit)
    return out


def clip_stats(title):
    """Standalone peaks of a clip: ({TRACK bone: max acc m/f^2}, {bone: max rotation acc deg/f^2}), loop-wrapped."""
    if title not in clip_cache:
        n, a, loop = clip_len(title)
        fr = [a+k for k in range(n)]
        if loop:
            fr = [fr[-1]]+fr+[fr[0]]
        poses = [sample(title, f) for f in fr]
        rig.animation_data.action = None
        rig.matrix_world = rig_m0
        pos = []
        for pose in poses:
            apply(pose)
            p.update()
            pos.append({b: (rig.matrix_world @ p.pb[b].head).copy() for b in TRACK if b in p.pb})
        acc = {b: max([(pos[k+1][b]-2*pos[k][b]+pos[k-1][b]).length for k in range(1, len(pos)-1)] or [0.0])
               for b in pos[0]}
        rot = {}
        for nm in poses[0][0]:
            vals = [rot_acc(poses, k, nm) for k in range(1, len(poses)-1)]
            rot[nm] = max([min(v, 360.0-v) for v in vals] or [0.0])
        st = [vfx_steps(poses, j) for j in range(1, len(poses))]
        prop = {k: max([s[k] for s in st] or [0.0]) for k in (*INTENSITY, 'jet_turn')}
        clip_cache[title] = (acc, rot, prop)
    return clip_cache[title]


CHASE_EASE = 0.15   # per-frame share of the yaw error the chase camera closes
# handoff rotation-pop limits (deg/frame^2): fullqa's 20 (or 1.25 x the clips' own peak), and on accent frames
# (+-1) its 60 for body bones / 24 for fingers
ROT_FLOOR, ROT_GAIN, ROT_ACCENT, ROT_FINGER_ACCENT = 20.0, 1.25, 60.0, 24.0
FINGERS = ('index', 'middle', 'ring', 'little', 'thumb', 'muzzle')
# entering/leaving the glide loop from any state lights/cuts the glide glow and jets; the authored rate for
# that is the one in Glide enter / Glide exit
GLIDE_VFX_REF = {'Glide enter', 'Glide exit'}
rig_m0 = rig.matrix_world.copy()
render_setup = False
ORTHO0 = {}


def run(spec):
    global render_setup
    timeline = build_timeline(spec)
    poses = [pose_at(e) for e in timeline]
    rig.animation_data.action = None
    rig.matrix_world = rig_m0

    # ---- metrics (world positions of TRACK bones; local rotation pops over all bones)
    pos = []
    for n, pose in enumerate(poses):
        apply(pose)
        scene.frame_set(n+1)
        p.update()
        pos.append({b: (rig.matrix_world @ p.pb[b].head).copy() for b in TRACK if b in p.pb})
    bounds = [n for n in range(1, len(timeline)) if seg_key(timeline[n]) != seg_key(timeline[n-1])]

    def acc(n, b):
        if n < 1 or n >= len(pos)-1:
            return 0.0
        return (pos[n+1][b]-2*pos[n][b]+pos[n-1][b]).length

    def stepv(n, b):
        return (pos[n][b]-pos[n-1][b]).length if n >= 1 else 0.0

    def blending(e):
        return 'from' in e or any(blending(e[k]) for k in ('track', 'lower') if k in e)
    # the inside reference skips every cut and every cross-fade frame (+-2)
    near = {n+d for n in bounds+[k for k, e in enumerate(timeline) if blending(e)] for d in range(-2, 3)}
    inside = [n for n in range(1, len(pos)-1) if n not in near]
    base_acc = {b: max([acc(n, b) for n in inside] or [0.0]) for b in pos[0]}
    def world(pose, n):
        apply(pose)
        scene.frame_set(n+1)
        p.update()
        return {b: (rig.matrix_world @ p.pb[b].head).copy() for b in TRACK if b in p.pb}

    def split(e):
        """(outgoing, incoming) nodes of the cross-fade starting in e (overlay, lower track or plain), else None."""
        if 'from' in e:
            return e['from'][0], {k: v for k, v in e.items() if k != 'from'}
        for key in ('track', 'lower'):
            if key in e:
                s = split(e[key])
                if s:
                    return {**e, key: s[0]}, {**e, key: s[1]}
        return None

    def ease_allowance(n):
        """Per bone, the acceleration a smoothstep cross-fade of this length needs just to carry the bone across
        the pose difference of the two clips (6 d / (bl+1)^2): an eased pose change, not a pop.  0 for cuts."""
        k1 = n
        while k1 < len(timeline) and split(timeline[k1]):
            k1 += 1
        span = k1-n
        if span == 0:
            return {}
        d = {}
        for k in range(n, k1):
            a, b = split(timeline[k])
            pa, pb_ = world(pose_at(a), k), world(pose_at(b), k)
            for bn in pa:
                d[bn] = max(d.get(bn, 0.0), (pa[bn]-pb_[bn]).length)
        return {bn: 6.0*v/(span+1)**2 for bn, v in d.items()}

    names = [b.name for b in p.pb if b.name != 'root']
    rot_series = {nm: [min(a, 360.0-a) for a in (rot_acc(poses, n, nm) for n in range(len(poses)))] for nm in names}
    base_rot = {nm: max([rot_series[nm][n] for n in inside] or [0.0]) for nm in names}
    handoffs = []
    for n in bounds:
        e0, e1 = timeline[n-1], timeline[n]
        win = range(max(1, n-2), min(len(pos)-1, n+3))
        near_nodes = [timeline[k] for k in range(max(0, n-3), min(len(timeline), n+4))]
        stats = [clip_stats(t) for t in set().union(*(titles(e) for e in near_nodes))]
        acc_body, acc_finger = accent_flags(near_nodes)
        ease = ease_allowance(n)
        rows = {}
        for b in pos[0]:
            a = max(acc(k, b) for k in win)
            ref = max([base_acc[b]]+[s[0].get(b, 0.0) for s in stats])
            rows[b] = {'acc': round(a, 4), 'clip_max_acc': round(ref, 4), 'ease_acc': round(ease.get(b, 0.0), 4),
                       'ref_acc': round(max(ref, ease.get(b, 0.0)), 4), 'step': round(stepv(n, b), 4),
                       'ratio': round(a/ref, 2) if ref > 1e-6 else None}
        worst = max(rows.items(), key=lambda kv: kv[1]['acc']-kv[1]['ref_acc'])
        rref = {nm: max([base_rot[nm]]+[s[1].get(nm, 0.0) for s in stats]) for nm in names}
        rw = max(names, key=lambda nm: max(rot_series[nm][k] for k in win)-rref[nm])
        rv = max(rot_series[rw][k] for k in win)

        def rot_limit(nm):
            lim = max(ROT_FLOOR, ROT_GAIN*rref[nm])
            if nm.split(' ')[-1].split('.')[0] in FINGERS:
                return max(lim, ROT_FINGER_ACCENT) if (acc_body or acc_finger) else lim
            return max(lim, ROT_ACCENT) if acc_body else lim
        over_ = [(max(rot_series[nm][k] for k in win)/rot_limit(nm), nm) for nm in names]
        rf = max(over_)
        rot_fail = {'bone': rf[1], 'deg': round(max(rot_series[rf[1]][k] for k in win), 1),
                    'limit': round(rot_limit(rf[1]), 1)} if rf[0] > 1.0 else None
        track_cut = e0['seg'] == e1['seg']
        handoffs.append({'frame': n+1, 'from': label(e0), 'to': label(e1),
                         'blend': (spec['lower_track'][seg_key(e1)[1]] if track_cut else spec['segments'][e1['seg']]).get('blend', 0),
                         'lower_track_cut': track_cut, 'accent_window': acc_body, 'finger_accent_window': acc_finger, 'worst_bone': worst[0], **worst[1],
                         'rot_bone': rw, 'rot_acc_deg': round(rv, 1), 'rot_clip_max_deg': round(rref[rw], 1),
                         'rot_fail': rot_fail,
                         'bones': rows})
    out = H.ROOT/'art/anim/wip'/spec.get('out', f"transitions/{spec['name']}")
    out.mkdir(parents=True, exist_ok=True)

    path = root_path(timeline) if spec.get('root_motion') else None
    # travel velocity (m/s) and its change (m/s^2), and per-frame VFX property steps, around each handoff
    vel = [Vector((0, 0))]+[Vector((path[n][0]-path[n-1][0], path[n][1]-path[n-1][1]))*H.FPS
                            for n in range(1, len(path))] if path else None
    for h, n in zip(handoffs, bounds):
        e0, e1 = timeline[n-1], timeline[n]
        win = range(max(1, n-2), min(len(poses), n+3))
        keys = (*INTENSITY, 'jet_turn')
        near_titles = set().union(*(titles(timeline[k]) for k in range(max(0, n-3), min(len(timeline), n+4))))
        if 'Glide loop' in near_titles:
            near_titles |= GLIDE_VFX_REF
        pref = {k: max(clip_stats(t)[2][k] for t in near_titles if t in clips) for k in keys}
        if vel:
            # the exhaust trails the travel: turning with it is following the motion, not a pop
            tt = max((math.degrees(vel[j-1].angle(vel[j], 0.0)) for j in range(max(2, n-2), min(len(vel), n+3))
                      if vel[j-1].length > 0.5 and vel[j].length > 0.5), default=0.0)
            pref['jet_turn'] = max(pref['jet_turn'], tt)
            h['travel_turn_deg'] = round(tt, 1)
        st = [vfx_steps(poses, j) for j in win]
        steps = {k: max(s[k] for s in st) for k in keys}
        pk = max(keys, key=lambda k: (steps[k]-pref[k])/(10.0 if k == 'jet_turn' else 0.05))
        h.update(prop=pk, prop_step=round(steps[pk], 3), prop_clip_max_step=round(pref[pk], 3))
        if vel:
            ra = max(((vel[j]-vel[j-1]).length*H.FPS for j in range(max(2, n-2), min(len(vel), n+3))), default=0.0)
            h.update(speed_before=round(vel[max(1, n-3)].length, 2), speed_after=round(vel[min(len(vel)-1, n+3)].length, 2),
                     root_acc_mps2=round(ra, 1))
    follow = chase_follow = None
    chase_yaw = []
    if path:
        follow, chase_follow = setup_follow()
        cyaw = path[0][2]
        for _, _, yaw in path:
            cyaw += CHASE_EASE*(yaw-cyaw)
            chase_yaw.append(cyaw)

    def place(n):
        if not path:
            rig.matrix_world = rig_m0
            for o in (bpy.data.objects.get('Stitch follow'), bpy.data.objects.get('Stitch chase follow')):
                if o:
                    o.location = (0.0, 0.0, 0.0)
                    o.rotation_euler = (0.0, 0.0, 0.0)
            return
        x, y, yaw = path[n]
        rig.matrix_world = Matrix.Translation((x, y, 0.0)) @ Matrix.Rotation(yaw, 4, 'Z') @ rig_m0
        follow.location = (x, y, 0.0)
        chase_follow.location = (x, y, 0.0)
        chase_follow.rotation_euler = (0.0, 0.0, chase_yaw[n])

    labels = [f"{n+1} {label(e)}"+(f" <{e['from'][0]['clip']} {e['from'][1]:.2f}" if 'from' in e else '')
              for n, e in enumerate(timeline)]
    summary = {'name': spec['name'], 'frames': len(timeline), 'handoffs': handoffs,
               'root_path': [[round(v, 4) for v in q] for q in path] if path else None,
               'acc_series_mm': {b: [round(1000*acc(n, b), 1) for n in range(len(pos))] for b in pos[0]},
               'timeline': [plain(e) for e in timeline]}
    (out/'stitch.json').write_text(json.dumps(summary, indent=1, default=str), encoding='utf-8')
    for h in handoffs:
        print('HANDOFF', spec['name'], h['frame'], h['from'], '->', h['to'], 'blend', h['blend'], 'worst', h['worst_bone'],
              'acc', h['acc'], 'clip_max', h['clip_max_acc'], 'ratio', h['ratio'],
              'rot', h['rot_bone'], h['rot_acc_deg'], '/', h['rot_clip_max_deg'], flush=True)

    if '--no-render' not in args:
        views = spec.get('views', default_views)
        if not render_setup:
            H.eevee(scene, 12)
            render_setup = True
        for v in VIEWS.values():
            c = bpy.data.objects.get(v)
            if c and c.data.type == 'ORTHO':
                c.data.ortho_scale = spec.get('ortho', ORTHO0.setdefault(v, c.data.ortho_scale))
        for view in views:
            for n, pose in enumerate(poses):
                apply(pose)
                place(n)
                scene.frame_set(n+1)
                p.update()
                H.render_still(scene, VIEWS[view], out/f'{view}-{n:03}.png', res)
            print('RENDERED', spec['name'], view, flush=True)
        fresh = {f'{v}-{n:03}.png' for v in views for n in range(len(poses))}
        for old in out.glob('*-[0-9][0-9][0-9].png'):
            if old.name not in fresh:
                (out/'stale').mkdir(exist_ok=True)
                old.replace(out/'stale'/old.name)
        (out/'clip.json').write_text(json.dumps({'title': spec['name'], 'step': 1, 'views': views,
                                                 'rendered_frames': labels}, indent=2), encoding='utf-8')
    rig.matrix_world = rig_m0
    print('STITCH DONE', spec['name'], flush=True)


for s in specs:
    run(s)
