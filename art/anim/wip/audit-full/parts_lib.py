from collections import defaultdict
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

