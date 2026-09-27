"""List every *_ok gate that is False (other than full_qa_ok) across module qa.json files.  python gates.py"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
ALL = ['run', 'walk', 'glide', 'air', 'run_dirs', 'loco8', 'turns', 'primary', 'arcstep', 'arcstep_dirs', 'special',
       'presentation']
bad = 0
for m in ALL:
    q = ROOT/'art/anim/wip'/m/'qa.json'
    if not q.exists():
        continue
    for c in json.loads(q.read_text()):
        for k, v in c.items():
            if k.endswith('_ok') and k != 'full_qa_ok' and v is False:
                bad += 1
                print(f"{m:12s} {c['title']:20s} {k}")
            if isinstance(v, dict) and v.get('ok') is False:
                bad += 1
                print(f"{m:12s} {c['title']:20s} {k}.ok")
print('non-full-QA gates failing:', bad)
