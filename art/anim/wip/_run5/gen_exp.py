"""Blend-length experiments: specs into art/anim/wip/transitions/specs/exp (out transitions/exp/<name>)."""
import json
from pathlib import Path

OUT = Path(__file__).resolve().parents[1]/'transitions/specs/exp'
OUT.mkdir(parents=True, exist_ok=True)
n = 0


def add(name, segments, lower_track=None):
    global n
    spec = {'name': name, 'modules': [], 'out': f'transitions/exp/{name}', 'root_motion': True, 'segments': segments}
    if lower_track:
        spec['lower_track'] = lower_track
    (OUT/f'{name}.json').write_text(json.dumps(spec, indent=1), encoding='utf-8')
    n += 1


def seg(clip, frames, start=None, blend=0, **kw):
    s = {'clip': clip, 'frames': frames, **kw}
    if start is not None:
        s['start'] = start
    if blend:
        s['blend'] = blend
    return s


for g, ln in (('Discharge snap', 14), ('Arc Bolt right', 20), ('Conduit Spear', 20), ('Open Circuit', 30),
              ('Meter full flourish', 24)):
    for b in (1, 2, 3, 4):
        for base in ('Run forward', 'Run left', 'Idle'):
            for at in (10, 12, 14):
                add(f"ov-{g.split()[-1].lower()}-{base.replace(' ', '').lower()}-at{at}-b{b}",
                    [seg('@track', at), seg(g, ln, blend=b, mask='upper', lower='track'), seg('@track', 20, blend=6)],
                    [seg(base, 80)])
for b in (6, 8, 10, 12):
    add(f'rev-run-b{b}', [seg('Run forward', 17), seg('Run backward', 34, blend=b, sync=True)])
    add(f'rev-walk-b{b}', [seg('Walk forward', 27), seg('Walk backward', 40, blend=b, sync=True)])
    add(f'glideexit-idle-b{b}', [seg('Glide loop', 12), seg('Glide exit', 14, blend=4), seg('Idle', 30, blend=b)])
    add(f'run-fall-b{b}', [seg('Run forward', 12), seg('Descend', 16, blend=b)])
    add(f'zig-b{b}', [seg('Run forward', 8), seg('Run forward left', 24, blend=b, sync=True)])
for b in (2, 3, 4, 5):
    add(f'step-run-b{b}', [seg('Run forward', 12), seg('Arc Step start', 7, blend=b)])
    add(f'step-left-run-b{b}', [seg('Run left', 12), seg('Arc Step left start', 7, blend=b)])
    add(f'step-back-idle-b{b}', [seg('Idle', 12, 85), seg('Arc Step back start', 7, blend=b)])
    add(f'step-air-b{b}', [seg('Descend', 12), seg('Arc Step start', 7, blend=b)])
    for js in (1, 3, 4):
        add(f'runjump-j{js}-b{b}', [seg('Run forward', 12), seg('Jump', 12-js, js, blend=b)])
print(n, 'exp specs')
