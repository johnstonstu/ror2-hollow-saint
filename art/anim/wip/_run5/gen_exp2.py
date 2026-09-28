"""Item 10 sweep 2: blend/phase sweeps for the handoffs still failing the matrix (running jump, diagonal reversal,
flourish/Open Circuit over run, walk->idle, snap over lean, glide->fall->land).
Specs -> transitions/specs/exp2, out transitions/exp2/<name>.  python gen_exp2.py"""
import json
from pathlib import Path

OUT = Path(__file__).resolve().parents[1]/'transitions/specs/exp2'
OUT.mkdir(parents=True, exist_ok=True)
specs = {}
T = '@track'


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


def over(clip, frames, blend, start=None):
    return seg(clip, frames, start=start, blend=blend, mask='upper', lower='track')


def add(name, segments, lower_track=None):
    spec = {'name': name, 'modules': [], 'out': f'transitions/exp2/{name}', 'root_motion': True, 'segments': segments}
    if lower_track:
        spec['lower_track'] = lower_track
    specs[name] = spec


for ph in (4, 8, 10, 12, 14, 16):
    for js in (3, 4, 5):
        for b in (4, 5, 6):
            add(f'runjump-p{ph}-j{js}-b{b}', [seg('Run forward', ph), seg('Jump', 12-js, js, blend=b),
                                              seg('Ascend', 8, blend=2)])
for ph in (1, 5, 9, 13):
    for b in (8, 10, 12):
        add(f'diagback-p{ph}-b{b}', [seg('Run forward right', 16+ph), seg('Run backward left', 30, blend=b, sync=True)])
        add(f'fwdback-p{ph}-b{b}', [seg('Run forward', 16+ph), seg('Run backward', 30, blend=b, sync=True)])
for clip in ('Meter full flourish', 'Open Circuit'):
    for ph in (10, 12, 14, 16):
        for b in (6, 8, 10):
            add(f'ov-{clip.split()[0].lower()}-run-p{ph}-b{b}', [seg(T, ph), over(clip, 20, b), seg(T, 10, blend=6)],
                [seg('Run forward', 60)])
for ph in (5, 13, 19, 27):
    for b in (10, 12, 14):
        add(f'walkidle-p{ph}-b{b}', [seg('Walk forward', ph), seg('Idle', 30, blend=b)])
for ph in (6, 10, 14):
    for b in (3, 4, 5):
        add(f'snaplean-p{ph}-b{b}', [seg(T, ph), over('Discharge snap', 14, b), seg(T, 10, blend=6)],
            [seg('Run lean left', 50)])
for gb in (6, 8):
    for lb in (2, 3, 4):
        add(f'glidefall-g{gb}-l{lb}', [seg('Glide loop', 16), seg('Descend', 12, blend=gb), seg('Land', 15, blend=lb),
                                       seg('Idle', 12, 2)])

for name, spec in specs.items():
    (OUT/f'{name}.json').write_text(json.dumps(spec, indent=1), encoding='utf-8')
print(len(specs), 'specs ->', OUT)
