"""Item 10: write stitch specs for every transition pair into art/anim/wip/transitions/specs/pairs/.
python gen_pairs.py  -> prints the spec count; group names are the file prefixes (loco, dir, air, glide, step,
over, stand, cancel, snap)."""
import json
from pathlib import Path

OUT = Path(__file__).resolve().parents[1]/'transitions/specs/pairs'
OUT.mkdir(parents=True, exist_ok=True)
specs = {}


def seg(clip, frames, start=None, blend=0, sync=False, **kw):
    s = {'clip': clip, 'frames': frames}
    if start is not None:
        s['start'] = start
    if blend:
        s['blend'] = blend
    if sync:
        s['sync'] = True
    s.update(kw)
    return s


def add(name, segments, lower_track=None, root_motion=True, ortho=None):
    spec = {'name': name, 'modules': [], 'out': f'transitions/pairs/{name}', 'root_motion': root_motion,
            'segments': segments}
    if lower_track:
        spec['lower_track'] = lower_track
    if ortho:
        spec['ortho'] = ortho
    specs[name] = spec


def over(clip, frames, blend=2, start=None):
    return seg(clip, frames, start=start, blend=blend, mask='upper', lower='track')


TRACK = '@track'
RUN8 = ['Run forward', 'Run forward left', 'Run left', 'Run backward left', 'Run backward', 'Run backward right',
        'Run right', 'Run forward right']

# ---- locomotion starts / stops (Idle 86..97 wraps to f1 at the cut)
add('loco-idle-walk', [seg('Idle', 16, 81), seg('Walk forward', 40, blend=8)])
add('loco-walk-idle', [seg('Walk forward', 27), seg('Idle', 30, blend=10)])
add('loco-idle-run', [seg('Idle', 16, 81), seg('Run forward', 34, blend=6)])
add('loco-run-idle', [seg('Run forward', 17), seg('Idle', 30, blend=8)])
add('loco-run-stop-midstride', [seg('Run forward', 12), seg('Idle', 30, blend=8)])
add('loco-walk-run', [seg('Walk forward', 27), seg('Run forward', 34, blend=6, sync=True)])
add('loco-run-walk', [seg('Run forward', 17), seg('Walk forward', 40, blend=8, sync=True)])
add('loco-idle-walkback', [seg('Idle', 16, 81), seg('Walk backward', 40, blend=8)])
add('loco-idle-runleft', [seg('Idle', 16, 81), seg('Run left', 34, blend=6)])
add('loco-idlecombat-run', [seg('Idle combat', 16), seg('Run forward', 34, blend=6)])
add('loco-run-idlecombat', [seg('Run forward', 17), seg('Idle combat', 30, blend=8)])
add('loco-idle-idlecombat', [seg('Idle', 24), seg('Idle combat', 30, blend=8)])

# ---- direction changes
add('dir-run-fwd-back', [seg('Run forward', 17), seg('Run backward', 34, blend=6, sync=True)])
add('dir-run-left-right', [seg('Run left', 17), seg('Run right', 34, blend=6, sync=True)])
add('dir-walk-fwd-back', [seg('Walk forward', 27), seg('Walk backward', 40, blend=8, sync=True)])
add('dir-run-lean', [seg('Run forward', 17), seg('Run lean left', 34, blend=6, sync=True),
                     seg('Run forward', 17, blend=6, sync=True)])
add('dir-plant-turn', [seg('Run forward', 16), seg('Plant turn 90 left', 16), seg('Run forward', 24, 10)])
add('dir-run-diag-back', [seg('Run forward right', 17), seg('Run backward left', 34, blend=6, sync=True)])

# ---- air
add('air-idle-jump-land-idle', [seg('Idle', 12, 85), seg('Jump', 11), seg('Ascend', 10, blend=2),
                                seg('Descend', 12, blend=4), seg('Land', 15, blend=2), seg('Idle', 20, 2)])
add('air-run-jump-land-run', [seg('Run forward', 12), seg('Jump', 11, 3, blend=3), seg('Ascend', 10, blend=2),
                              seg('Descend', 10, blend=4), seg('Land', 6, blend=2), seg('Run forward', 24, blend=6)])
add('air-run-fall-land', [seg('Run forward', 12), seg('Descend', 16, blend=6), seg('Land', 15, blend=2),
                          seg('Idle', 20, 2)])
add('air-descend-land-run', [seg('Descend', 12), seg('Land', 6, blend=2), seg('Run forward', 24, blend=6)])
add('air-ascend-descend', [seg('Ascend', 21), seg('Descend', 21, blend=4)])

# ---- glide
add('glide-run-glide-run', [seg('Run forward', 9), seg('Glide enter', 13), seg('Glide loop', 31, 2),
                            seg('Glide exit', 14), seg('Run forward', 24, 2)])
add('glide-exit-midphase', [seg('Glide loop', 9, 1), seg('Glide exit', 14, blend=4), seg('Run forward', 24, 2)])
add('glide-exit-lowphase', [seg('Glide loop', 17, 1), seg('Glide exit', 14, blend=4), seg('Run forward', 24, 2)])
add('glide-fall-land', [seg('Glide loop', 16), seg('Descend', 12, blend=6), seg('Land', 15, blend=2),
                        seg('Idle', 20, 2)])
add('glide-exit-idle', [seg('Glide loop', 12), seg('Glide exit', 14, blend=4), seg('Idle', 30, blend=8)])
add('glide-from-air', [seg('Descend', 12), seg('Glide loop', 33, blend=6)])
add('glide-from-jump', [seg('Jump', 11), seg('Ascend', 8, blend=2), seg('Glide loop', 33, blend=6)])

# ---- Arc Step from every state (end cancel window at Arrive f5)
for d, run in (('', 'Run forward'), (' back', 'Run backward'), (' left', 'Run left'), (' right', 'Run right')):
    tag = (d.strip() or 'fwd')
    st, lp, en = f'Arc Step{d} start', f'Arc Step{d} loop', f'Arc Step{d} end'
    add(f'step-{tag}-idle', [seg('Idle', 12, 85), seg(st, 7, blend=2), seg(lp, 9, 2), seg(en, 16), seg('Idle', 24, blend=6)])
    add(f'step-{tag}-run', [seg(run, 12), seg(st, 7, blend=2), seg(lp, 9, 2), seg(en, 5), seg(run, 24, blend=4)])
add('step-fwd-air', [seg('Descend', 12), seg('Arc Step start', 7, blend=3), seg('Arc Step loop', 9, 2),
                     seg('Arc Step end', 5), seg('Descend', 16, blend=4)])
add('step-fwd-glide', [seg('Glide loop', 12), seg('Arc Step start', 7, blend=3), seg('Arc Step loop', 9, 2),
                       seg('Arc Step end', 5), seg('Glide loop', 24, blend=4)])
add('step-back-air', [seg('Descend', 12), seg('Arc Step back start', 7, blend=3), seg('Arc Step back loop', 9, 2),
                      seg('Arc Step back end', 5), seg('Descend', 16, blend=4)])

# ---- upper-body skills over each state (the lower track keeps playing; overlay fades out over 6)
BASES = {'idle': [seg('Idle', 80)], 'run': [seg('Run forward', 80)], 'air': [seg('Descend', 80)],
         'glide': [seg('Glide loop', 80)]}
GEST = {'arcbolt': ('Arc Bolt right', 20), 'spear': ('Conduit Spear', 20), 'discharge': ('Discharge', 28),
        'snap': ('Discharge snap', 14), 'flourish': ('Meter full flourish', 24)}
for bname, base in BASES.items():
    for gname, (clip, n) in GEST.items():
        add(f'over-{gname}-{bname}', [seg(TRACK, 12), over(clip, n), seg(TRACK, 20, blend=6)], base)
    add(f'over-opencircuit-{bname}', [seg(TRACK, 12), over('Open Circuit', 30), over('Open Circuit hold', 23, blend=0, start=2),
                                      over('Open Circuit end', 22, blend=0), seg(TRACK, 16, blend=6)],
        [seg(base[0]['clip'], 110)])
    add(f'over-charge-{bname}', [seg(TRACK, 12), over('Charge loop', 20), over('Charge full', 25, blend=6),
                                 over('Meter full flourish', 24, blend=4), seg(TRACK, 16, blend=6)],
        [seg(base[0]['clip'], 110)])

# ---- gestures full body from a standing idle (their start/end pose vs Idle)
for gname, (clip, n) in GEST.items():
    add(f'stand-{gname}', [seg('Idle', 12, 85), seg(clip, n, blend=4), seg('Idle', 24, blend=6)], root_motion=False)

# ---- cancels
add('cancel-arcbolt-chain-run', [seg(TRACK, 12), over('Arc Bolt right', 13), over('Arc Bolt left', 13, blend=2),
                                 over('Arc Bolt right', 20, blend=2), seg(TRACK, 16, blend=6)], [seg('Run forward', 80)])
add('cancel-arcbolt-chain-idle', [seg(TRACK, 12), over('Arc Bolt right', 13), over('Arc Bolt left', 13, blend=2),
                                  over('Arc Bolt right', 20, blend=2), seg(TRACK, 16, blend=6)], [seg('Idle', 80)])
add('cancel-arcbolt-to-run', [seg(TRACK, 12), over('Arc Bolt right', 13), seg(TRACK, 20, blend=4)],
    [seg('Run forward', 50)])
add('cancel-spear-to-arcbolt', [seg(TRACK, 12), over('Conduit Spear', 11), over('Arc Bolt right', 20, blend=2),
                                seg(TRACK, 16, blend=6)], [seg('Run forward', 60)])
add('cancel-arcbolt-to-arcstep', [seg(TRACK, 12), over('Arc Bolt right', 8), seg('Arc Step start', 7, blend=2),
                                  seg('Arc Step loop', 9, 2), seg('Arc Step end', 5), seg('Run forward', 24, blend=4)],
    [seg('Run forward', 30)])
add('cancel-jump-midcast', [seg(TRACK, 8), over('Arc Bolt right', 20), seg(TRACK, 16, blend=6)],
    [seg('Run forward', 14), seg('Jump', 11, 3, blend=3), seg('Ascend', 10, blend=2), seg('Descend', 12, blend=4)])
add('cancel-stop-midcast', [seg(TRACK, 8), over('Conduit Spear', 20), seg(TRACK, 20, blend=6)],
    [seg('Run forward', 14), seg('Idle', 40, blend=8)])
add('cancel-turn-midcast', [seg(TRACK, 8), over('Arc Bolt right', 20), seg(TRACK, 20, blend=6)],
    [seg('Run forward', 14), seg('Run left', 40, blend=6, sync=True)])
add('cancel-land-midcast', [seg(TRACK, 6), over('Conduit Spear', 20), seg(TRACK, 20, blend=6)],
    [seg('Descend', 10), seg('Land', 6, blend=2), seg('Run forward', 40, blend=6)])

# ---- Discharge snap over every state
SNAP_BASES = {'idle': 'Idle', 'walk': 'Walk forward', 'glide': 'Glide loop', 'descend': 'Descend',
              'ascend': 'Ascend', 'lean': 'Run lean left', 'idlecombat': 'Idle combat'}
for r in RUN8:
    SNAP_BASES[r.lower().replace(' ', '')] = r
for bname, clip in SNAP_BASES.items():
    add(f'snap-{bname}', [seg(TRACK, 10), over('Discharge snap', 14, blend=1), seg(TRACK, 16, blend=4)],
        [seg(clip, 60)])
add('snap-arcstep', [seg(TRACK, 16), over('Discharge snap', 14, blend=1), seg(TRACK, 14, blend=4)],
    [seg('Idle', 10, 87), seg('Arc Step start', 7, blend=2), seg('Arc Step loop', 9, 2), seg('Arc Step end', 16)])
add('snap-midcast-arcbolt', [seg(TRACK, 10), over('Arc Bolt right', 8), over('Discharge snap', 14, blend=1),
                             seg(TRACK, 16, blend=4)], [seg('Run forward', 60)])
add('snap-midcast-charge', [seg(TRACK, 10), over('Charge loop', 16, blend=4), over('Discharge snap', 14, blend=1),
                            seg(TRACK, 16, blend=4)], [seg('Idle', 60)])
add('snap-circle', [seg(TRACK, 20), over('Discharge snap', 14, blend=1), seg(TRACK, 20, blend=4),
                    over('Discharge snap', 14, blend=1), seg(TRACK, 20, blend=4)],
    [seg('Run forward', 12), seg('Run forward left', 12, blend=12, sync=True), seg('Run left', 12, blend=12, sync=True),
     seg('Run backward left', 12, blend=12, sync=True), seg('Run backward', 12, blend=12, sync=True),
     seg('Run backward right', 12, blend=12, sync=True), seg('Run right', 12, blend=12, sync=True)])

for name, spec in specs.items():
    (OUT/f'{name}.json').write_text(json.dumps(spec, indent=1), encoding='utf-8')
print(len(specs), 'specs ->', OUT)
