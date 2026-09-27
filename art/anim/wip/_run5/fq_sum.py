"""Summarise a fullqa_audit JSON: worst per fail key across clips.  python fq_sum.py <json> [filter]"""
import json
import re
import sys
from collections import defaultdict

d = json.load(open(sys.argv[1]))
flt = sys.argv[2] if len(sys.argv) > 2 else ''
agg = defaultdict(list)
for clip, v in d['clips'].items():
    for f in v['full_qa']['fails']:
        m = re.match(r'(.*?) (-?[\d.]+)(mm|%|deg/f2|deg)? ?(f\d+)?$', f)
        key, val = (m.group(1), float(m.group(2))) if m else (f, 0)
        agg[key].append((val, clip, m.group(4) if m else ''))
ok = sum(1 for v in d['clips'].values() if v['full_qa_ok'])
print(f'clips ok {ok}/{len(d["clips"])}')
for k, v in sorted(agg.items(), key=lambda x: -max(abs(a[0]) for a in x[1])):
    if flt and flt not in k:
        continue
    v.sort(key=lambda a: -abs(a[0]))
    print(f'{k:55s} n={len(v):2d} ' + '; '.join(f'{a[0]:g} {a[1]} {a[2]}' for a in v[:4]))
