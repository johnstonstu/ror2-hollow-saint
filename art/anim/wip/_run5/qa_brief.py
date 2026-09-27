"""python qa_brief.py <module> [title]: compact QA per clip (status, gates, full-QA fails, spear/aim fields)."""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
m = sys.argv[1]
only = sys.argv[2] if len(sys.argv) > 2 else None
for c in json.loads((ROOT/'art/anim/wip'/m/'qa.json').read_text()):
    if only and c['title'] != only:
        continue
    gates = {k: v for k, v in c.items() if k.endswith('_ok')}
    bad = [k for k, v in gates.items() if v is False]
    print(f"== {c['title']} {c['status']} bad={bad}")
    print('  hand_qa', json.dumps(c.get('hand_qa_summary')), 'viol', json.dumps((c.get('hand_qa') or {}).get('violations')))
    print('  contact', c.get('hand_contact_worst'), c.get('hand_contact_max_mm'), '| pad', c.get('pad_contact_worst'),
          c.get('pad_contact_max_mm'), '| arm_clear', c.get('arm_clear_min_m'))
    o = c.get('hand_orient') or {}
    print('  orient', {s: {k: o[s].get(k) for k in ('fail_frames', 'worst_thumb_up', 'worst_palm_fwd', 'swept_back_frames')}
                       for s in o})
    print('  roll', c.get('orient_roll_max_deg'), 'infeasible', c.get('orient_infeasible'), 'smooth', c.get('orient_smooth_max_deg'))
    fq = c.get('full_qa') or {}
    print('  pops', fq.get('pops_deg_f2'), 'accent', fq.get('accent_pops_deg_f2'))
    print('  fails', fq.get('fails'))
    for k, v in c.items():
        if k.startswith(('spear_', 'snap_', 'start_rest', 'end_rest')):
            print('  ', k, v)
