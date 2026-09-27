"""Print a compact per-clip QA table from art/anim/wip/<module>/qa.json (system Python)."""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
MODS = ['run', 'walk', 'glide', 'air', 'run_dirs', 'primary', 'arcstep', 'special', 'presentation']
mods = sys.argv[1].split(',') if len(sys.argv) > 1 else MODS
for m in mods:
    q = ROOT/'art/anim/wip'/m/'qa.json'
    if not q.exists():
        continue
    for c in json.loads(q.read_text()):
        h = c['hand_qa_summary']
        a = c['arm_clearance']
        side = lambda s: f"{a[s]['part'][:12]}>{a[s]['against'][:16]} f{a[s]['frame']} {a[s]['min_m']*1000:.1f}"
        print(f"{c['title'][:22]:22} {c['status']:5} clr[L {side('L')} | R {side('R')}] pop={h['worst_pop_deg_f2']}"
              f" acc={h['worst_accent_pop_deg_f2']} pen={h['max_pen_mm']} viol={h['violations']}"
              f" slide={c['max_planted_slide_m_per_frame']} ext={c['max_leg_extension']} jet={c.get('vfx_peak', {})}"
              f" contact={c.get('hand_contact_max_mm')} {c.get('hand_contact_worst', '')}")
