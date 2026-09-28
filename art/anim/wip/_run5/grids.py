"""Pass/fail grids for TRANSITIONS.md from the matrix stitch.json files (judged as matrix.py does).
python grids.py [--glob 'transitions/matrix/*'] --md <out.md>
Grid 1: locomotion/utility state -> state (every non-overlay handoff).  Grid 2: skill layer over each state
(overlay in/out handoffs, by the locomotion under it).  Cell = passed/total handoffs."""
import json
import re
import sys
from collections import defaultdict
from pathlib import Path

WIP = Path(__file__).resolve().parents[1]
src = Path(__file__).with_name('matrix.py').read_text(encoding='utf-8').split('\nrows = []')[0]
ns = {'__file__': str(Path(__file__).with_name('matrix.py'))}
exec(src, ns)
judge = ns['judge']

pat = sys.argv[sys.argv.index('--glob')+1] if '--glob' in sys.argv else 'transitions/matrix/*'
out = Path(sys.argv[sys.argv.index('--md')+1])
OVERLAYS = ('Arc Bolt', 'Conduit Spear', 'Discharge', 'Meter full flourish', 'Open Circuit', 'Charge')


def family(label):
    t = re.sub(r' f\d+$', '', label.strip('[]'))
    if t.startswith('Idle'):
        return t
    for pre, fam in (('Walk', 'Walk (8-way)'), ('Run stop', 'Run stop'), ('Run start', 'Run start'),
                     ('Run lean', 'Run lean'), ('Run pivot', 'Run pivot'), ('Plant turn', 'Plant turn'),
                     ('Run', 'Run (8-way)'), ('Glide enter', 'Glide enter'), ('Glide exit', 'Glide exit'),
                     ('Glide', 'Glide'), ('Jump', 'Jump'), ('Ascend', 'Ascend'), ('Descend', 'Fall (Descend)'),
                     ('Land', 'Land'), ('Arc Step', 'Arc Step')):
        if t.startswith(pre):
            return fam
    for o in OVERLAYS:
        if t.startswith(o):
            return o
    return t


def parts(label):
    """'Overlay fN / Base fM' -> (overlay, base); '[Base fN]' -> (None, base); plain -> (None, label)."""
    if ' / ' in label:
        a, b = label.split(' / ', 1)
        return a, b
    return None, label


loco = defaultdict(lambda: [0, 0])
skill = defaultdict(lambda: [0, 0])
for d in sorted(WIP.glob(pat)):
    f = d/'stitch.json'
    if not f.exists():
        continue
    s = json.loads(f.read_text(encoding='utf-8'))
    for h in s['handoffs']:
        ok = not judge(h)
        oa, ba = parts(h['from'])
        ob, bb = parts(h['to'])
        if oa or ob:
            if oa and ob:
                # same skill = the state changed underneath it; different = chained / cancelled into
                row = f'{family(ob)} (next phase / chain / state change under it)' if family(oa) == family(ob) else \
                    f'{family(oa)} -> {family(ob)}'
            else:
                row = f'{family(ob)} in' if ob else f'{family(oa)} out'
            key = (row, family(bb))
            skill[key][0] += ok
            skill[key][1] += 1
        else:
            key = (family(ba), family(bb))
            loco[key][0] += ok
            loco[key][1] += 1


def grid(cells, title, row_name):
    rows = sorted({k[0] for k in cells})
    cols = sorted({k[1] for k in cells})
    lines = [f'### {title}', '', f'| {row_name} | '+' | '.join(cols)+' |', '|---|'+'---|'*len(cols)]
    for r in rows:
        vals = []
        for c in cols:
            if (r, c) in cells:
                p, n = cells[(r, c)]
                vals.append(f'{p}/{n}' if p == n else f'**{p}/{n}**')
            else:
                vals.append('')
        lines.append(f'| {r} | '+' | '.join(vals)+' |')
    return lines


md = grid(loco, 'State -> state (rows: from, columns: to; passed/total handoffs, bold = has a flagged handoff)',
          'From \\ To')
md += ['']+grid(skill, 'Skill layer over locomotion (rows: skill event, columns: the state underneath)', 'Skill')
out.write_text('\n'.join(md)+'\n', encoding='utf-8')
print('grids ->', out, len(loco), 'loco cells', len(skill), 'skill cells')
