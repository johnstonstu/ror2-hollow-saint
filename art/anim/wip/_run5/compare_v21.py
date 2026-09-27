"""9i before/after: audit baseline (fixes/qa-v21.json) vs current per-module qa.json full_qa.
python art/anim/wip/_run5/compare_v21.py out.md"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
WIP = ROOT/'art/anim/wip'
ALL = ['run', 'walk', 'glide', 'air', 'run_dirs', 'primary', 'arcstep', 'special', 'presentation']
base = json.loads((WIP/'audit-full/fixes/qa-v21.json').read_text())['clips']
now = {}
for m in ALL:
    for c in json.loads((WIP/m/'qa.json').read_text()):
        now[c['title']] = c


CREASE = {f'{s} {a} > {b}' for s in 'LR' for a, b in (('thigh', 'pelvis'), ('thigh', 'spine'), ('upperarm', 'chest'))}


def worst(fq):
    items = sorted(((v[0], k, v[1]) for k, v in fq.get('contact_mm', {}).items() if k not in CREASE), reverse=True)
    return items[0] if items else (0.0, '', 0)


lines = ['Contact = worst part-into-part depth, excluding the hip/armpit crease pairs '
         '(thigh > pelvis/spine, upperarm > chest), which full QA exempts as skin creases.\n',
         '| Clip | v21 contact mm | now contact mm | v21 pop deg/f2 | now pop deg/f2 | v21 ok | now ok | worst now |',
         '|---|---|---|---|---|---|---|---|']
tot = [0, 0]
for t, b in base.items():
    c = now.get(t)
    if c is None:
        continue
    w, wb = worst(c['full_qa']), worst(b['full_qa'])
    tot[0] += bool(b['full_qa_ok'])
    tot[1] += bool(c['full_qa_ok'])
    lines.append(f"| {t} | {wb[0]} | {w[0]} | {b['full_pop_max_deg_f2']} | "
                 f"{c['full_pop_max_deg_f2']} | {'yes' if b['full_qa_ok'] else 'no'} | {'yes' if c['full_qa_ok'] else 'no'} | "
                 f"{w[1]} {w[0]} mm f{w[2]} |")
lines.append(f'\nfull_qa ok: v21 {tot[0]}/{len(base)}, now {tot[1]}/{len(base)}')
Path(sys.argv[1]).write_text('\n'.join(lines)+'\n', encoding='utf-8')
print('\n'.join(lines))
