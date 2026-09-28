"""Scratch (hands2): compare preview qa.json per clip against the v25 baseline.
python qa_compare.py [module ...]    |    python qa_compare.py --log <preview log> ... (QA lines of each log)"""
import json
import sys
from pathlib import Path

WIP = Path(__file__).resolve().parents[1]
BASE = Path(__file__).parent/'_work'/'baseline-v25'
MODS = sys.argv[1:] or ['run', 'walk', 'glide', 'air', 'run_dirs', 'loco8', 'turns', 'startstop', 'primary', 'arcstep',
                        'arcstep_dirs', 'special', 'spear', 'presentation']


def fails(c):
    out = list(c.get('full_qa', {}).get('fails', []))
    hq = c.get('hand_qa', {})
    out += [f'handqa {k} {v}' for k, v in hq.get('violations', {}).items()]
    out += [f'handqa pen {k} {v}' for k, v in hq.get('penetration_mm', {}).items()]
    s = c.get('hand_qa_summary', {})
    if s and s.get('worst_pop_deg_f2', 0) > 12:
        out.append(f"handqa pop {s['worst_pop_deg_f2']} {s['worst_pop']}")
    for k in ('hand_contact_ok', 'hand_orient_ok', 'pad_contact_ok', 'arm_clear_ok'):
        if c.get(k) is False:
            out.append(f'{k} False')
    return out


def read_log(p):
    raw = Path(p).read_bytes()
    return raw.decode('utf-16' if raw[:2] in (b'\xff\xfe', b'\xfe\xff') else 'utf-8', errors='replace')


def sources():
    if sys.argv[1:2] == ['--log']:
        for p in sys.argv[2:]:
            rows = [json.loads(l[3:]) for l in read_log(p).splitlines() if l.startswith('QA {')]
            print('==', p)
            if rows:
                yield rows[0]['module'], rows
        return
    for m in MODS:
        new = WIP/m/'qa.json'
        if new.exists():
            yield m, json.loads(new.read_text(encoding='utf-8'))
        else:
            print(m, 'no qa.json')


tot = {'pass': 0, 'check': 0, 'base_pass': 0}
for m, rows in sources():
    base = {c['title']: c for c in json.loads((BASE/f'{m}.json').read_text(encoding='utf-8'))} \
        if (BASE/f'{m}.json').exists() else {}
    for c in rows:
        b = base.get(c['title'], {})
        tot['pass' if c['status'] == 'PASS' else 'check'] += 1
        tot['base_pass'] += b.get('status') == 'PASS'
        nat = c.get('full_qa', {}).get('hand_nat', {})
        line = (f"{c['status']:5} (v25 {b.get('status', '-'):5}) {c['title'][:34]:34} orient_inf {c.get('orient_infeasible', '-')}"
                f"/{b.get('orient_infeasible', '-')} settle {c.get('hand_settle_max_deg', '-')} "
                f"bend {nat.get('min_bend_deg', ['-'])[0]} side {nat.get('max_sideways_deg', ['-'])[0]} "
                f"order {nat.get('curl_order_gap_deg', ['-'])[0]} clear {c.get('arm_clear_min_m')}/{b.get('arm_clear_min_m')}")
        print(line)
        old = set(fails(b))
        for x in fails(c):
            print('     ', 'NEW ' if x not in old else 'old ', x)
print(tot)
