"""python fq_new.py [modules...]: full-QA fails per clip (short) for the 9h modules."""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
for m in sys.argv[1:] or ['run_dirs', 'loco8', 'turns', 'arcstep_dirs']:
    for c in json.loads((ROOT/'art/anim/wip'/m/'qa.json').read_text()):
        fq = c.get('full_qa') or {}
        fails = fq.get('fails', [])
        print(f"{m:12s} {c['title']:22s} {c['status']:5s} contact {c.get('full_contact_max_mm')} "
              f"pop {c.get('full_pop_max_deg_f2')} | {'; '.join(str(x) for x in fails)[:300]}")
