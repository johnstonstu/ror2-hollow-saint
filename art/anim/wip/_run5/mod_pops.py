"""Per-clip status, full-QA fails and pops above a floor.  python mod_pops.py module [module ...] [--floor 15]"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
args = sys.argv[1:]
floor = float(args[args.index('--floor')+1]) if '--floor' in args else 15.0
mods = [a for i, a in enumerate(args) if not a.startswith('--') and (i == 0 or args[i-1] != '--floor')]
for m in mods:
    for c in json.loads((ROOT/'art/anim/wip'/m/'qa.json').read_text()):
        fq = c.get('full_qa', {})
        extra = {k: v for k, v in c.items() if 'slide' in k or 'gap' in k}
        print(f"{c['title']:18s} {c.get('status')} ok={fq.get('ok')} fails={fq.get('fails')}")
        print('    pops', [x for x in fq.get('pops_deg_f2', []) if x[0] > floor][:6], extra or '')
