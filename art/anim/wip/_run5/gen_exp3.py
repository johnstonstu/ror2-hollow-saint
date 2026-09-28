"""Item 10 sweep 3 (after the eased-pose-change allowance): Spear / flourish / Open Circuit fade-in over the run at
every phase, glide->fall blend, Arc Step in/out of glide.  Specs -> transitions/specs/exp3, out transitions/exp3.
python gen_exp3.py"""
import json
from pathlib import Path

OUT = Path(__file__).resolve().parents[1]/'transitions/specs/exp3'
OUT.mkdir(parents=True, exist_ok=True)
specs = {}
T = '@track'


def seg(clip, frames, start=None, blend=0, **kw):
    s = {'clip': clip, 'frames': frames}
    if start is not None:
        s['start'] = start
    if blend:
        s['blend'] = blend
    s.update(kw)
    return s


def over(clip, frames, blend):
    return seg(clip, frames, blend=blend, mask='upper', lower='track')


def add(name, segments, lower_track=None):
    spec = {'name': name, 'modules': [], 'out': f'transitions/exp3/{name}', 'root_motion': True, 'segments': segments}
    if lower_track:
        spec['lower_track'] = lower_track
    specs[name] = spec


for base in ('Run forward', 'Run backward'):
    bn = base.split()[1]
    for ph in (2, 4, 6, 8, 10, 12, 14, 16):
        for clip, bls in (('Conduit Spear', (4, 5, 6)), ('Meter full flourish', (8, 10, 12)), ('Open Circuit', (8, 10, 12))):
            for b in bls:
                add(f'ov-{clip.split()[1].lower()}-{bn}-p{ph}-b{b}', [seg(T, ph), over(clip, 16, b)], [seg(base, 40)])
for gb in (8, 10, 12):
    add(f'glidefall-g{gb}', [seg('Glide loop', 16), seg('Descend', 14, blend=gb)])
for ph in (4, 8, 12, 16):
    for b in (4, 5, 6):
        add(f'stepglide-p{ph}-b{b}', [seg('Glide loop', ph), seg('Arc Step start', 7, blend=b), seg('Arc Step loop', 9, 2),
                                      seg('Arc Step end', 5), seg('Glide loop', 16, blend=b)])

for name, spec in specs.items():
    (OUT/f'{name}.json').write_text(json.dumps(spec, indent=1), encoding='utf-8')
print(len(specs), 'specs ->', OUT)
