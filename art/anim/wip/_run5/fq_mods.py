"""Full-QA fail summary from module qa.json files.  python fq_mods.py [module ...] [--filter text] [--save tag]"""
import json
import re
import sys
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
ALL = ['run', 'walk', 'glide', 'air', 'run_dirs', 'loco8', 'turns', 'primary', 'arcstep', 'arcstep_dirs', 'special',
       'presentation']
args = sys.argv[1:]
flt = args[args.index('--filter')+1] if '--filter' in args else ''
save = args[args.index('--save')+1] if '--save' in args else ''
mods = [a for i, a in enumerate(args) if not a.startswith('--') and (i == 0 or args[i-1] not in ('--filter', '--save'))] or ALL
agg = defaultdict(list)
ok = tot = 0
status = {}
clips = {}
for m in mods:
    q = ROOT/'art/anim/wip'/m/'qa.json'
    if not q.exists():
        continue
    for c in json.loads(q.read_text()):
        tot += 1
        ok += bool(c.get('full_qa_ok'))
        status[c['title']] = c.get('status')
        clips[c['title']] = c
        for f in c.get('full_qa', {}).get('fails', []):
            mm = re.match(r'(.*?) (-?[\d.]+)(mm|%|deg/f2|deg)? ?(f\d+)?$', f)
            key, val = (mm.group(1), float(mm.group(2))) if mm else (f, 0)
            agg[key].append((val, c['title'], mm.group(4) if mm else ''))
print(f'full_qa ok {ok}/{tot}; status PASS {sum(v == "PASS" for v in status.values())}/{len(status)}')
print('CHECK:', ', '.join(k for k, v in status.items() if v != 'PASS'))
for k, v in sorted(agg.items(), key=lambda x: -max(abs(a[0]) for a in x[1])):
    if flt and flt not in k:
        continue
    v.sort(key=lambda a: -abs(a[0]))
    print(f'{k:50s} n={len(v):2d} ' + '; '.join(f'{a[0]:g} {a[1]} {a[2]}' for a in v[:4]))
if save:
    out = ROOT/'art/anim/wip/_run5'/f'fq-{save}.json'
    out.write_text(json.dumps(clips, indent=1), encoding='utf-8')
