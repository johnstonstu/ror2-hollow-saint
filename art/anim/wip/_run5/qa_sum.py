"""Compact summary of a qa_all.py result: python art/anim/wip/_run5/qa_sum.py <tag>"""
import json
import sys
from pathlib import Path

r = json.loads((Path(__file__).parent/f'qa-{sys.argv[1]}.json').read_text())
print(len(r), 'clips; not PASS:', [x['title'] for x in r if x['status'] != 'PASS'])
print('pad max', max(x['pad_mm'] for x in r), ' hand contact max', max(x['contact_mm'] for x in r))
for x in r:
    f = x['pad_follow'] or {}
    print(f"{x['title']:<18} {x['status']} pad={x['pad_mm']:<4} halo={x['halo_clear']:<5} padpop={x['pad_pop']} "
          f"halopop={x['halo_pop']} push={ {s: f[s]['push_mm'] for s in f} } k={ {s: f[s]['yield_min_k'] for s in f} }")
