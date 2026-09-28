"""Item 10 final transition matrix: the recommended handoffs (BLEND below, the Unity transition table) for every
pair, written to art/anim/wip/transitions/specs/matrix/ (out transitions/matrix/<name>).
The first pass with naive 2-frame overlay / 6-frame locomotion blends is kept in specs/pairs + transitions/pairs.
python gen_matrix.py"""
import json
from pathlib import Path

OUT = Path(__file__).resolve().parents[1]/'transitions/specs/matrix'
OUT.mkdir(parents=True, exist_ok=True)
specs = {}

# Recommended cross-fade frames at 24 fps (from the blend sweeps in transitions/exp, see TRANSITIONS.md).
BLEND = {
    'loco_dir': 10,         # 8-way run/walk blend-tree moves, run reversal, sprint lean in/out
    'walk_reverse': 10,
    'walk_idle': 12, 'idle_walk': 8, 'walk_run': 6, 'run_walk': 8, 'idle_combat': 8,
    'run_fall': 8, 'air_glide': 6, 'glide_fall': 10, 'ascend_descend': 4, 'descend_land': 3,
    'land_run': 6,          # from Land f6 (Compress) into Run forward
    'run_jump': 6,          # Run forward -> Jump from f4 (skips the standing crouch)
    'step_in': 4,           # any state -> Arc Step start
    'step_out': 4,          # Arc Step end f5 (Arrive, cancel window) -> locomotion
    'step_glide': 5,         # Glide loop <-> Arc Step (the glide glow cuts/relights over the blend)
    'glide_exit': 4,        # Glide loop at any phase -> Glide exit
    'overlay_out': 6,       # upper-body skill layer weight 1 -> 0
    'chain': 2,             # Arc Bolt interrupt (f13) / Spear cancel (f11) into the next cast
}
OVERLAY_IN = {'Arc Bolt right': 3, 'Arc Bolt left': 3, 'Discharge snap': 4, 'Conduit Spear': 5, 'Discharge': 4,
              'Meter full flourish': 10, 'Open Circuit': 10, 'Charge loop': 6, 'Charge full': 6}


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


def add(name, segments, lower_track=None, root_motion=True):
    spec = {'name': name, 'modules': [], 'out': f'transitions/matrix/{name}', 'root_motion': root_motion,
            'segments': segments}
    if lower_track:
        spec['lower_track'] = lower_track
    specs[name] = spec


def over(clip, frames, blend=None, start=None):
    return seg(clip, frames, start=start, blend=OVERLAY_IN.get(clip, 3) if blend is None else blend,
               mask='upper', lower='track')


T = '@track'
OUT_B = BLEND['overlay_out']
RUN8 = ['Run forward', 'Run forward left', 'Run left', 'Run backward left', 'Run backward', 'Run backward right',
        'Run right', 'Run forward right']
IDLE_TO_JUMP = seg('Idle', 12, 85)        # Idle f85..f96, so the next frame is Idle f1 = Jump f1

# ---- starts / stops
add('loco-idle-runstart-run', [IDLE_TO_JUMP, seg('Run start', 17, 1), seg('Run forward', 24, 10)])
add('loco-run-runstop-idle', [seg('Run forward', 16), seg('Run stop', 16, 1), seg('Idle', 24, 2)])
# stick released at Run forward f5: the run carries on to the next contact (R, f9) and Run stop R takes over
add('loco-run-stop-midstride', [seg('Run forward', 24), seg('Run stop R', 16, 1), seg('Idle', 24, 2)])
add('loco-run-idle-blend', [seg('Run forward', 12), seg('Idle', 30, blend=8)])
add('loco-idle-run-blend', [seg('Idle', 16, 81), seg('Run forward', 34, blend=6)])
add('loco-idle-walk', [seg('Idle', 16, 81), seg('Walk forward', 40, blend=BLEND['idle_walk'])])
add('loco-walk-idle', [seg('Walk forward', 27), seg('Idle', 30, blend=BLEND['walk_idle'])])
add('loco-walk-run', [seg('Walk forward', 27), seg('Run forward', 34, blend=BLEND['walk_run'], sync=True)])
add('loco-run-walk', [seg('Run forward', 17), seg('Walk forward', 40, blend=BLEND['run_walk'], sync=True)])
add('loco-idle-walkback', [seg('Idle', 16, 81), seg('Walk backward', 40, blend=BLEND['idle_walk'])])
add('loco-idle-runleft', [seg('Idle', 16, 81), seg('Run left', 34, blend=6)])
add('loco-idle-idlecombat', [seg('Idle', 24), seg('Idle combat', 30, blend=BLEND['idle_combat'])])
add('loco-idlecombat-idle', [seg('Idle combat', 24), seg('Idle', 30, blend=BLEND['idle_combat'])])
add('loco-idlecombat-run', [seg('Idle combat', 16), seg('Run forward', 34, blend=6)])

# ---- direction changes
D = BLEND['loco_dir']
add('dir-run-fwd-back', [seg('Run forward', 17), seg('Run backward', 34, blend=D, sync=True)])
add('dir-run-left-right', [seg('Run left', 17), seg('Run right', 34, blend=D, sync=True)])
add('dir-run-diag-back', [seg('Run forward right', 17), seg('Run backward left', 34, blend=D, sync=True)])
add('dir-walk-fwd-back', [seg('Walk forward', 27), seg('Walk backward', 40, blend=BLEND['walk_reverse'], sync=True)])
add('dir-run-lean', [seg('Run forward', 17), seg('Run lean left', 34, blend=D, sync=True),
                     seg('Run forward', 17, blend=D, sync=True)])
add('dir-run-zigzag', [seg('Run forward', 8), seg('Run forward left', 16, blend=D, sync=True),
                       seg('Run forward right', 16, blend=D, sync=True), seg('Run forward', 16, blend=D, sync=True)])
add('dir-plant-turn', [seg('Run forward', 16), seg('Plant turn 90 left', 16), seg('Run forward', 24, 10)])
add('dir-pivot', [seg('Run forward', 16), seg('Run pivot 180 right', 20), seg('Run forward', 24, 10)])
add('dir-stick-circle', [seg('Run forward', 12)]+[seg(r, 12, blend=D, sync=True) for r in RUN8[1:]]
    + [seg('Run forward', 16, blend=D, sync=True)])
add('dir-walk-circle', [seg('Walk forward', 14)]+[seg(r.replace('Run', 'Walk'), 14, blend=D, sync=True)
                                                  for r in RUN8[1:]]+[seg('Walk forward', 20, blend=D, sync=True)])

# ---- air
add('air-idle-jump-land-idle', [IDLE_TO_JUMP, seg('Jump', 11), seg('Ascend', 10, blend=2),
                                seg('Descend', 12, blend=BLEND['ascend_descend']),
                                seg('Land', 15, blend=BLEND['descend_land']), seg('Idle', 20, 2)])
add('air-run-jump-land-run', [seg('Run forward', 12), seg('Jump', 8, 4, blend=BLEND['run_jump']),
                              seg('Ascend', 10, blend=2), seg('Descend', 10, blend=BLEND['ascend_descend']),
                              seg('Land', 6, blend=BLEND['descend_land']), seg('Run forward', 24, blend=BLEND['land_run'])])
add('air-run-fall-land-idle', [seg('Run forward', 12), seg('Descend', 16, blend=BLEND['run_fall']),
                               seg('Land', 15, blend=BLEND['descend_land']), seg('Idle', 20, 2)])
add('air-descend-land-run', [seg('Descend', 12), seg('Land', 6, blend=BLEND['descend_land']),
                             seg('Run forward', 24, blend=BLEND['land_run'])])
add('air-ascend-descend', [seg('Ascend', 21), seg('Descend', 21, blend=BLEND['ascend_descend'])])

# ---- glide
add('glide-run-glide-run', [seg('Run forward', 9), seg('Glide enter', 13), seg('Glide loop', 31, 2),
                            seg('Glide exit', 14), seg('Run forward', 24, 2)])
for ph in (1, 9, 17, 25):
    add(f'glide-exit-phase{ph}', [seg('Glide loop', 8, ph), seg('Glide exit', 14, blend=BLEND['glide_exit']),
                                  seg('Run forward', 24, 2)])
add('glide-exit-runstop-idle', [seg('Glide loop', 12), seg('Glide exit', 14, blend=BLEND['glide_exit']),
                                seg('Run stop', 15, 2), seg('Idle', 24, 2)])
add('glide-fall-land', [seg('Glide loop', 16), seg('Descend', 12, blend=BLEND['glide_fall']),
                        seg('Land', 15, blend=BLEND['descend_land']), seg('Idle', 20, 2)])
add('glide-from-air', [seg('Descend', 12), seg('Glide loop', 33, blend=BLEND['air_glide'])])
add('glide-from-jump', [seg('Jump', 11), seg('Ascend', 8, blend=2), seg('Glide loop', 33, blend=BLEND['air_glide'])])

# ---- Arc Step from every state (end cancel window: Arrive f5)
SI, SO = BLEND['step_in'], BLEND['step_out']
for d, run in (('', 'Run forward'), (' back', 'Run backward'), (' left', 'Run left'), (' right', 'Run right')):
    tag = (d.strip() or 'fwd')
    st, lp, en = f'Arc Step{d} start', f'Arc Step{d} loop', f'Arc Step{d} end'
    add(f'step-{tag}-idle', [seg('Idle', 12, 85), seg(st, 7, blend=SI), seg(lp, 9, 2), seg(en, 15, 2),
                             seg('Idle', 24, blend=6)])
    add(f'step-{tag}-run', [seg(run, 12), seg(st, 7, blend=SI), seg(lp, 9, 2), seg(en, 4, 2), seg(run, 24, blend=SO)])
    add(f'step-{tag}-air', [seg('Descend', 12), seg(st, 7, blend=SI), seg(lp, 9, 2), seg(en, 4, 2),
                            seg('Descend', 16, blend=SO)])
    SG = BLEND['step_glide']
    add(f'step-{tag}-glide', [seg('Glide loop', 12), seg(st, 7, blend=SG), seg(lp, 9, 2), seg(en, 4, 2),
                              seg('Glide loop', 24, blend=SG)])
add('step-fwd-chain', [seg('Run forward', 12), seg('Arc Step start', 7, blend=SI), seg('Arc Step loop', 9, 2),
                       seg('Arc Step end', 4, 2), seg('Arc Step start', 7, blend=SI), seg('Arc Step loop', 9, 2),
                       seg('Arc Step end', 15, 2), seg('Idle', 24, blend=6)])

# ---- upper-body skills over each state
BASES = {'idle': 'Idle', 'run': 'Run forward', 'strafe': 'Run left', 'back': 'Run backward', 'walk': 'Walk forward',
         'air': 'Descend', 'glide': 'Glide loop'}
GEST = {'arcbolt': ('Arc Bolt right', 20), 'arcboltL': ('Arc Bolt left', 20), 'spear': ('Conduit Spear', 20),
        'discharge': ('Discharge', 28), 'flourish': ('Meter full flourish', 24)}
for bname, base in BASES.items():
    for gname, (clip, n) in GEST.items():
        add(f'over-{gname}-{bname}', [seg(T, 12), over(clip, n), seg(T, 20, blend=OUT_B)], [seg(base, 80)])
    add(f'over-opencircuit-{bname}', [seg(T, 12), over('Open Circuit', 30), over('Open Circuit hold', 23, 0, 2),
                                      over('Open Circuit end', 22, 0), seg(T, 16, blend=OUT_B)], [seg(base, 110)])
for bname in ('idle', 'idlecombat'):
    base = {'idle': 'Idle', 'idlecombat': 'Idle combat'}[bname]
    add(f'over-chargeloop-{bname}', [seg(T, 12), over('Charge loop', 41), seg(T, 16, blend=OUT_B)], [seg(base, 80)])
    add(f'over-chargefull-{bname}', [seg(T, 12), over('Charge full', 25), seg(T, 16, blend=OUT_B)], [seg(base, 60)])

# ---- gestures full body from a standing idle (their own start/end pose vs Idle)
for gname, (clip, n) in GEST.items():
    add(f'stand-{gname}', [seg('Idle', 12, 85), seg(clip, n, blend=4), seg('Idle', 24, blend=6)], root_motion=False)

# ---- cancels and mid-cast state changes
C = BLEND['chain']
add('cancel-arcbolt-chain-run', [seg(T, 12), over('Arc Bolt right', 13), over('Arc Bolt left', 13, C),
                                 over('Arc Bolt right', 20, C), seg(T, 16, blend=OUT_B)], [seg('Run forward', 80)])
add('cancel-arcbolt-chain-idle', [seg(T, 12), over('Arc Bolt right', 13), over('Arc Bolt left', 13, C),
                                  over('Arc Bolt right', 20, C), seg(T, 16, blend=OUT_B)], [seg('Idle', 80)])
add('cancel-arcbolt-to-run', [seg(T, 12), over('Arc Bolt right', 13), seg(T, 20, blend=4)], [seg('Run forward', 50)])
add('cancel-spear-to-arcbolt', [seg(T, 12), over('Conduit Spear', 11), over('Arc Bolt right', 20, C),
                                seg(T, 16, blend=OUT_B)], [seg('Run forward', 60)])
add('cancel-spear-to-run', [seg(T, 12), over('Conduit Spear', 11), seg(T, 20, blend=4)], [seg('Run forward', 50)])
add('cancel-arcbolt-to-arcstep', [seg(T, 12), over('Arc Bolt right', 8), seg('Arc Step start', 7, blend=SI),
                                  seg('Arc Step loop', 9, 2), seg('Arc Step end', 4, 2), seg('Run forward', 24, blend=SO)],
    [seg('Run forward', 30)])
add('cancel-arcstep-to-arcbolt', [seg('Run forward', 12), seg('Arc Step start', 7, blend=SI), seg('Arc Step loop', 9, 2),
                                  seg('Arc Step end', 4, 2), seg(T, 4, blend=SO), over('Arc Bolt right', 20),
                                  seg(T, 16, blend=OUT_B)],
    [seg('Run forward', 93)])
add('cancel-jump-midcast', [seg(T, 8), over('Arc Bolt right', 20), seg(T, 16, blend=OUT_B)],
    [seg('Run forward', 14), seg('Jump', 8, 4, blend=BLEND['run_jump']), seg('Ascend', 10, blend=2),
     seg('Descend', 12, blend=BLEND['ascend_descend'])])
add('cancel-stop-midcast', [seg(T, 8), over('Conduit Spear', 20), seg(T, 20, blend=OUT_B)],
    [seg('Run forward', 16), seg('Run stop', 16, 1), seg('Idle', 30, 2)])
add('cancel-start-midcast', [seg(T, 8), over('Arc Bolt right', 20), seg(T, 20, blend=OUT_B)],
    [IDLE_TO_JUMP, seg('Run start', 17, 1), seg('Run forward', 30, 10)])
add('cancel-turn-midcast', [seg(T, 8), over('Arc Bolt right', 20), seg(T, 20, blend=OUT_B)],
    [seg('Run forward', 14), seg('Run left', 40, blend=D, sync=True)])
add('cancel-land-midcast', [seg(T, 6), over('Conduit Spear', 20), seg(T, 20, blend=OUT_B)],
    [seg('Descend', 10), seg('Land', 6, blend=BLEND['descend_land']), seg('Run forward', 40, blend=BLEND['land_run'])])
add('cancel-glide-midcast', [seg(T, 8), over('Arc Bolt right', 20), seg(T, 20, blend=OUT_B)],
    [seg('Run forward', 9), seg('Glide enter', 13), seg('Glide loop', 30, 2)])

# ---- Discharge snap over every state (idle, walk/run 8 directions, glide, air, Arc Step, mid-cast)
SNAP_BASES = {'idle': 'Idle', 'idlecombat': 'Idle combat', 'glide': 'Glide loop', 'descend': 'Descend',
              'ascend': 'Ascend', 'lean': 'Run lean left'}
for r in RUN8:
    SNAP_BASES[r.lower().replace(' ', '')] = r
    w = r.replace('Run', 'Walk')
    SNAP_BASES[w.lower().replace(' ', '')] = w
for bname, clip in SNAP_BASES.items():
    add(f'snap-{bname}', [seg(T, 10), over('Discharge snap', 14), seg(T, 16, blend=OUT_B)], [seg(clip, 60)])
add('snap-arcstep', [seg(T, 16), over('Discharge snap', 14), seg(T, 14, blend=OUT_B)],
    [seg('Idle', 10, 87), seg('Arc Step start', 7, blend=SI), seg('Arc Step loop', 9, 2), seg('Arc Step end', 15, 2)])
add('snap-arcstep-run', [seg(T, 16), over('Discharge snap', 14), seg(T, 16, blend=OUT_B)],
    [seg('Run forward', 10), seg('Arc Step start', 7, blend=SI), seg('Arc Step loop', 9, 2), seg('Arc Step end', 4, 2),
     seg('Run forward', 30, blend=SO)])
add('snap-midcast-arcbolt', [seg(T, 10), over('Arc Bolt right', 8), over('Discharge snap', 14),
                             seg(T, 16, blend=OUT_B)], [seg('Run forward', 60)])
add('snap-midcast-spear', [seg(T, 10), over('Conduit Spear', 8), over('Discharge snap', 14),
                           seg(T, 16, blend=OUT_B)], [seg('Run forward', 60)])
add('snap-midcast-charge', [seg(T, 10), over('Charge loop', 16), over('Discharge snap', 14),
                            seg(T, 16, blend=OUT_B)], [seg('Idle', 60)])
add('snap-jump', [seg(T, 14), over('Discharge snap', 14), seg(T, 16, blend=OUT_B)],
    [seg('Run forward', 12), seg('Jump', 8, 4, blend=BLEND['run_jump']), seg('Ascend', 10, blend=2),
     seg('Descend', 14, blend=BLEND['ascend_descend'])])
add('snap-stop', [seg(T, 18), over('Discharge snap', 14), seg(T, 16, blend=OUT_B)],
    [seg('Run forward', 16), seg('Run stop', 16, 1), seg('Idle', 20, 2)])
add('snap-circle', [seg(T, 20), over('Discharge snap', 14), seg(T, 20, blend=OUT_B), over('Discharge snap', 14),
                    seg(T, 26, blend=OUT_B)],
    [seg('Run forward', 12)]+[seg(r, 12, blend=D, sync=True) for r in RUN8[1:]]+[seg('Run forward', 20, blend=D, sync=True)])

for name, spec in specs.items():
    (OUT/f'{name}.json').write_text(json.dumps(spec, indent=1), encoding='utf-8')
(OUT/'_blend-table.json').write_text(json.dumps({'blend': BLEND, 'overlay_in': OVERLAY_IN}, indent=1), encoding='utf-8')
print(len(specs), 'specs ->', OUT)
