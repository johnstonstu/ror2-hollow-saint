"""Measure pose seams between clips (loop wraps and declared hand-offs).

Run: blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/seams.py -- run glide
Hand-offs come from clip meta: `seam_from` / `seam_to` = [action title, frame] (the first/last frame
must equal that frame of that clip), plus the legacy glide keys run_frame_start / run_frame_end.
"""
import bpy
import importlib
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
sys.path.insert(0, str(Path(__file__).parent/'clips'))
import hs_anim as H

modules = sys.argv[sys.argv.index('--')+1:]
p = H.open_start()
clips = {}
for m in modules:
    for act, info in importlib.import_module(m).build(p):
        clips[info['title']] = (act, info)


def pose_at(title, frame):
    act, _ = clips[title]
    p.rig.animation_data.action = act
    bpy.context.scene.frame_set(frame)
    p.update()
    return {n: p.pb[n].matrix.copy() for n in p.fk}


def diff(a, b):
    worst = max(((a[n].translation-b[n].translation).length, n) for n in a)
    return round(worst[0]*1000, 3), worst[1]


pairs = []
for title, (act, info) in clips.items():
    first, last = info['frames']
    if info['loop']:
        pairs.append((f'{title} wrap', (title, first), (title, last)))
    src = info.get('seam_from') or (['Run forward', info['run_frame_start']] if 'run_frame_start' in info else None)
    dst = info.get('seam_to') or (['Run forward', info['run_frame_end']] if 'run_frame_end' in info else None)
    if not src and info.get('starts_from') in clips:
        src = [info['starts_from'], clips[info['starts_from']][1]['frames'][0]]
    if not dst and info.get('ends_in') in clips:
        dst = [info['ends_in'], clips[info['ends_in']][1]['frames'][0]]
    for label, ours, other in (('from', (title, first), src), ('to', (title, last), dst)):
        if other and other[0] in clips:
            o_first, o_last = clips[other[0]][1]['frames']
            f = o_first+(other[1]-o_first) % (o_last-o_first) if clips[other[0]][1]['loop'] else other[1]
            pairs.append((f'{title} {label} {other[0]} f{f}', ours, (other[0], f)))
report = []
for label, a, b in pairs:
    mm, bone = diff(pose_at(*a), pose_at(*b))
    report.append({'seam': label, 'max_mm': mm, 'bone': bone})
    print('SEAM', label, mm, 'mm', bone, flush=True)
out = H.ROOT/'art/anim/wip'/f"seams-{'-'.join(modules)}.json"
out.write_text(json.dumps(report, indent=2), encoding='utf-8')
