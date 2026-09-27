import bpy, sys, json, math
sys.path.insert(0, r'C:\Users\stuwj\Documents\Coding\ror2-lightning\tools\blender\anim')
sys.path.insert(0, r'C:\Users\stuwj\Documents\Coding\ror2-lightning\tools\blender\anim\clips')
import hs_anim as H
import handorient, handpass
from mathutils import Euler

p = H.open_start()
p.reset(); p.update()
out = {'handedness_rest': {s: round(handorient.handedness(p.rig, s), 3) for s in 'LR'}}
hp = handpass.HandPass(p)
tip = lambda s, d: p.pb[f'{s} {d}.3'].tail.copy()
for s in 'LR':
    # tuck: thumb curled 20, fingers relaxed 10 -> thumb tip to index.2 head distance with/without pass tuck
    p.reset()
    for i in (1, 2, 3):
        p.pb[f'{s} thumb.{i}'].rotation_quaternion = Euler((math.radians(20), 0, 0)).to_quaternion()
    p.update()
    d0 = (p.pb[f'{s} thumb.3'].tail-p.pb[f'{s} index.2'].head).length
    hp.side(s); p.update()
    d1 = (p.pb[f'{s} thumb.3'].tail-p.pb[f'{s} index.2'].head).length
    # fan: index curled 60, middle 0 -> pass fans them apart? measure index/middle tip gap
    p.reset()
    for i in (1, 2, 3):
        p.pb[f'{s} index.{i}'].rotation_quaternion = Euler((math.radians(50), 0, 0)).to_quaternion()
    p.update()
    g0 = (tip(s, 'index')-tip(s, 'middle')).length
    hp.side(s); p.update()
    g1 = (tip(s, 'index')-tip(s, 'middle')).length
    # presentation splay: +splay should widen index-little tip span
    import presentation
    st = {f'{s}_{k}': 0.0 for k in ('curl', 'idx', 'mid', 'ring', 'lit', 'splay', 'thumb')}
    p.reset(); presentation.fingers(p, s, st); p.update()
    s0 = (tip(s, 'index')-tip(s, 'little')).length
    st[f'{s}_splay'] = 12.0
    p.reset(); presentation.fingers(p, s, st); p.update()
    s1 = (tip(s, 'index')-tip(s, 'little')).length
    out[s] = {'tuck_thumb_to_index_mm': [round(d0*1000, 1), round(d1*1000, 1)],
              'fan_index_middle_gap_mm': [round(g0*1000, 1), round(g1*1000, 1)],
              'presentation_splay_span_mm': [round(s0*1000, 1), round(s1*1000, 1)]}
p.reset(); p.update()
# layout at rest: index vs little knuckle x (lateral should be index for a palm-forward... report raw)
for s in 'LR':
    out[s]['index1_head'] = [round(x, 4) for x in p.pb[f'{s} index.1'].head]
    out[s]['little1_head'] = [round(x, 4) for x in p.pb[f'{s} little.1'].head]
    out[s]['thumb1_tail'] = [round(x, 4) for x in p.pb[f'{s} thumb.1'].tail]
print('PROBE', json.dumps(out), flush=True)
