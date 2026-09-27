"""Print the hand QA + arm clearance of every module's wip qa.json (system Python)."""
import json
import sys
from pathlib import Path

W = Path(__file__).parent
mods = sys.argv[1].split(',') if len(sys.argv) > 1 and not sys.argv[1].startswith('-') else ['run', 'walk', 'glide', 'air', 'run_dirs', 'primary', 'arcstep',
                                                          'special', 'presentation']
verbose = '-v' in sys.argv
tag = sys.argv[sys.argv.index('--tag')+1] if '--tag' in sys.argv else None


def clips(m):
    if tag:
        log = W/'logs'/f'{tag}-{m}.log'
        if log.exists():
            return [json.loads(ln[3:]) for ln in log.read_text(errors='replace').splitlines() if ln.startswith('QA ')]
        return []
    q = W/m/'qa.json'
    return json.loads(q.read_text()) if q.exists() else []


for m in mods:
    for c in clips(m):
        s = c.get('hand_qa_summary', {})
        print(f"{c['title']:<22} {c['status']:<5} hand_ok={s.get('ok')} viol={s.get('violations')} pen>={s.get('pen_over')} "
              f"maxpen={s.get('max_pen_mm')}mm pop={s.get('worst_pop_deg_f2')} ({s.get('worst_pop')}) "
              f"acc={s.get('worst_accent_pop_deg_f2')} clear={c.get('arm_clear_min_m')}")
        if verbose:
            h = c.get('hand_qa', {})
            for k, v in h.get('violations', {}).items():
                print('     V', k, v)
            for k, v in h.get('penetration_mm', {}).items():
                print('     P', k, v)
            print('     W', json.dumps(h.get('wrist')))
            print('     F', json.dumps(h.get('flex_range')))
