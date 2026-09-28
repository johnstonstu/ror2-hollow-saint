"""QA-only pass (no renders) of clip modules in parallel background Blenders; prints a compact table.
Run: python art/anim/wip/_run5/qa_all.py <tag> [module ...] [--print-only]"""
import json
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
TOOLS = ROOT/'tools/blender/anim'
BLENDER = 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe'
ALL = ['run', 'walk', 'glide', 'air', 'run_dirs', 'loco8', 'turns', 'startstop', 'primary', 'arcstep', 'arcstep_dirs', 'special',
       'spear', 'presentation']
HERE = Path(__file__).resolve().parent
tag = sys.argv[1]
print_only = '--print-only' in sys.argv
mods = [a for a in sys.argv[2:] if not a.startswith('--')] or ALL
logs = HERE/'logs'
logs.mkdir(parents=True, exist_ok=True)


def run(m):
    with open(logs/f'{tag}-{m}.log', 'w', encoding='utf-8') as f:
        r = subprocess.run([BLENDER, '--background', '--factory-startup', '--python-exit-code', '1', '--python',
                            str(TOOLS/'preview.py'), '--', m, '--no-render'], cwd=ROOT, stdout=f, stderr=subprocess.STDOUT)
    return m, r.returncode


if print_only:
    codes = dict.fromkeys(mods, 0)
else:
    with ThreadPoolExecutor(3) as ex:
        codes = dict(ex.map(run, mods))
rows = []
for m in mods:
    q = ROOT/'art/anim/wip'/m/'qa.json'
    if codes[m] != 0 or not q.exists():
        print('FAILED', m, codes[m])
        continue
    for c in json.loads(q.read_text()):
        o = c.get('hand_orient', {})
        rows.append({'title': c['title'], 'status': c['status'], 'orient_ok': c.get('hand_orient_ok'),
                     'L': {k: o.get('L', {}).get(k) for k in ('fail_frames', 'worst_thumb_up', 'worst_palm_fwd')},
                     'R': {k: o.get('R', {}).get(k) for k in ('fail_frames', 'worst_thumb_up', 'worst_palm_fwd')},
                     'hand_ok': c['hand_qa_summary']['ok'], 'hand_pop': c['hand_qa_summary']['worst_pop_deg_f2'],
                     'pen': c['hand_qa_summary']['max_pen_mm'], 'contact_mm': c.get('hand_contact_max_mm'),
                     'arm_clear_ok': c.get('arm_clear_ok'), 'roll': c.get('orient_roll_max_deg'),
                     'contact_worst': c.get('hand_contact_worst'), 'clear_min': c.get('arm_clear_min_m'),
                     'pad_ok': c.get('pad_contact_ok'), 'pad_mm': c.get('pad_contact_max_mm'),
                     'pad_follow': c.get('pad_follow_max'),
                     'halo_clear': c.get('halo_clear_push_max_mm'), 'pad_pop': c.get('pad_pop_mm_f2'),
                     'halo_pop': c.get('halo_pop_mm_f2'), 'seam': {k: c.get(k) for k in ('seam_from', 'seam_to')},
                     'pad_top': sorted(((k, v['mm']) for k, v in (c.get('pad_contact') or {}).items()),
                                       key=lambda kv: -kv[1])[:4]})
out = HERE/f'qa-{tag}.json'
out.write_text(json.dumps(rows, indent=1), encoding='utf-8')
for r in rows:
    print(f"{r['title']:<18} {r['status']:<5} orient={r['orient_ok']!s:<5} hand={r['hand_ok']!s:<5} pop={r['hand_pop']:<5} "
          f"pen={r['pen']:<4} contact={r['contact_mm']:<4} clr={r['clear_min']} roll={r['roll']}"
          f"\n    L={r['L']} R={r['R']} {r['contact_worst'] if (r['contact_mm'] or 0) > 4 else ''}"
          f"\n    pad_ok={r['pad_ok']} pad={r['pad_mm']} {r['pad_top']} follow={r['pad_follow']}"
          f"\n    halo_clear={r['halo_clear']} pad_pop={r['pad_pop']} halo_pop={r['halo_pop']}")
