"""Score every stitched handoff under art/anim/wip/transitions/pairs (+ legacy transitions/*, locomotion8/*).
python matrix.py [--md out.md] [--fails]"""
import json
import sys
from pathlib import Path

WIP = Path(__file__).resolve().parents[1]
POS_GAIN, POS_SLACK = 1.25, 0.005       # m/frame^2
ROT_FLOOR, ROT_GAIN = 20.0, 1.25        # deg/frame^2 (fullqa pop limit)
PROP_GAIN, PROP_FLOOR = 1.5, 0.05       # per-frame VFX intensity step
JET_FLOOR = 10.0                        # deg/frame lit-jet exhaust turn
ROOT_MAX = 80.0                         # m/s^2 travel acceleration (RoR2 CharacterBody baseAcceleration)


def judge(h):
    why = []
    ref = h.get('ref_acc', h['clip_max_acc'])
    if h['acc'] > POS_GAIN*ref+POS_SLACK:
        why.append(f"pos {h['worst_bone']} {1000*h['acc']:.0f}/{1000*ref:.0f}mm")
    if 'rot_fail' in h:
        if h['rot_fail']:
            r = h['rot_fail']
            why.append(f"rot {r['bone']} {r['deg']}>{r['limit']}")
    elif 'rot_acc_deg' in h and h['rot_acc_deg'] > max(ROT_FLOOR, ROT_GAIN*h['rot_clip_max_deg']):
        why.append(f"rot {h['rot_bone']} {h['rot_acc_deg']}/{h['rot_clip_max_deg']}")
    floor = JET_FLOOR if h.get('prop') == 'jet_turn' else PROP_FLOOR
    if 'prop_step' in h and h['prop_step'] > max(floor, PROP_GAIN*h['prop_clip_max_step']):
        why.append(f"vfx {h['prop']} {h['prop_step']}/{h['prop_clip_max_step']}")
    if h.get('root_acc_mps2', 0) > ROOT_MAX:
        why.append(f"root {h['root_acc_mps2']} m/s2")
    return why


rows = []
pats = [sys.argv[sys.argv.index('--glob')+1]] if '--glob' in sys.argv else ['transitions/pairs/*', 'transitions/*',
                                                                              'locomotion8/*']
for pat in pats:
    for d in sorted(WIP.glob(pat)):
        f = d/'stitch.json'
        if not f.exists() or d.name == 'pairs':
            continue
        s = json.loads(f.read_text(encoding='utf-8'))
        for h in s['handoffs']:
            rows.append((s['name'], h, judge(h)))

fails = [r for r in rows if r[2]]
print(f'{len(rows)} handoffs, {len(fails)} fail, specs {len({r[0] for r in rows})}')
show = fails if '--fails' in sys.argv else rows
for name, h, why in show:
    print(f"{'FAIL' if why else 'ok  '} {name:<30} f{h['frame']:<4} b{h['blend']:<2} {h['from'][:44]:<44} -> "
          f"{h['to'][:44]:<44} {'; '.join(why)}")
if '--md' in sys.argv:
    out = Path(sys.argv[sys.argv.index('--md')+1])
    lines = ['| Pair | Frame | Blend | From | To | Pos (worst bone, handoff/ref mm/f^2) | Rot pop deg/f^2 | '
             'VFX step | Root m/s^2 | Result |', '|---|---|---|---|---|---|---|---|---|---|']
    for name, h, why in rows:
        rot = (f"{h['rot_fail']['bone']} {h['rot_fail']['deg']}>{h['rot_fail']['limit']}" if h.get('rot_fail') else
               f"{h.get('rot_bone', '-')} {h.get('rot_acc_deg', '-')}/{h.get('rot_clip_max_deg', '-')}")
        lines.append(f"| {name} | {h['frame']} | {h['blend']} | {h['from']} | {h['to']} | "
                     f"{h['worst_bone']} {1000*h['acc']:.0f}/{1000*h.get('ref_acc', h['clip_max_acc']):.0f} | "
                     f"{rot} | "
                     f"{h.get('prop', '-')} {h.get('prop_step', '-')}/{h.get('prop_clip_max_step', '-')} | "
                     f"{h.get('root_acc_mps2', '-')} | {'FAIL: '+'; '.join(why) if why else 'pass'} |")
    out.write_text('\n'.join(lines)+'\n', encoding='utf-8')
