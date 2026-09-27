"""Rig facts for the hand/clearance QA: body vertex groups, finger/thumb curl directions.

Run: blender --background --factory-startup --python tools/blender/anim/hand_probe.py
"""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Euler, Vector

sys.path.insert(0, str(Path(__file__).parent))
import hs_anim as H

p = H.open_start()
body = bpy.data.objects[H.BODY]
groups = {g.index: g.name for g in body.vertex_groups}
dom = {}
for v in body.data.vertices:
    if v.groups:
        g = max(v.groups, key=lambda e: e.weight)
        dom[groups[g.group]] = dom.get(groups[g.group], 0)+1
out = {'body_verts': len(body.data.vertices), 'body_polys': len(body.data.polygons),
       'dominant_group_counts': dict(sorted(dom.items(), key=lambda kv: -kv[1]))}


def tip(name):
    b = p.pb[name]
    return b.tail.copy()


def palm_frame(s):
    hand = p.pb[f'{s} hand']
    a = p.pb[f'{s} index.1'].head
    c = p.pb[f'{s} little.1'].head
    along = (hand.tail-hand.head).normalized()
    across = (c-a).normalized()
    return along, across, hand


p.reset()
p.update()
rest = {}
for s in H.SIDES:
    along, across, hand = palm_frame(s)
    n = along.cross(across).normalized()
    rest[s] = {'palm_n_candidate': [round(x, 3) for x in n], 'thumb_tip': [round(x, 3) for x in tip(f'{s} thumb.3')],
               'index_tip': [round(x, 3) for x in tip(f'{s} index.3')]}
out['rest'] = rest
tests = {}
for s in H.SIDES:
    for deg in (-30, 30, 60):
        for axis in ('X', 'Z'):
            p.reset()
            e = [0, 0, 0]
            e['XZ'.index(axis)*2] = math.radians(deg)
            for i in (1, 2, 3):
                p.pb[f'{s} thumb.{i}'].rotation_quaternion = Euler(e).to_quaternion()
            p.update()
            t = tip(f'{s} thumb.3')
            idx = (p.pb[f'{s} index.1'].tail+p.pb[f'{s} index.2'].tail)*0.5
            mid = p.pb[f'{s} middle.1'].head
            tests[f'{s} thumb {axis}{deg:+d}'] = {'tip_to_index2_m': round((t-idx).length, 4),
                                                  'tip_to_middle_knuckle_m': round((t-mid).length, 4),
                                                  'tip': [round(x, 3) for x in t]}
        p.reset()
        e = Euler((math.radians(deg), 0, 0)).to_quaternion()
        for i in (1, 2, 3):
            p.pb[f'{s} index.{i}'].rotation_quaternion = e
        p.update()
        tests[f'{s} index X{deg:+d}'] = {'tip': [round(x, 3) for x in tip(f'{s} index.3')],
                                         'tip_to_hand_head_m': round((tip(f'{s} index.3')-p.pb[f'{s} hand'].head).length, 4)}
out['tests'] = tests
p.reset()
p.update()
print('HANDPROBE', json.dumps(out, indent=1), flush=True)
(H.ROOT/'art/anim/wip/hands').mkdir(parents=True, exist_ok=True)
(H.ROOT/'art/anim/wip/hands/rig-probe.json').write_text(json.dumps(out, indent=1), encoding='utf-8')
