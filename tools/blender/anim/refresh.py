"""Checkpoint refresh (system Python): QA + preview renders for every module, sheets/GIFs, seams,
review.html and a QA summary. Background Blender only (separate processes, never the open UI).

Run: python tools/blender/anim/refresh.py v7 [--modules special,primary] [--no-render] [--jobs 3]
Writes art/anim/<tag>/qa-summary.json (+ the usual wip/<module>/ previews and review.html).
"""
import json
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
TOOLS = ROOT/'tools/blender/anim'
BLENDER = 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe'
ALL = ['run', 'walk', 'glide', 'air', 'run_dirs', 'loco8', 'turns', 'startstop', 'primary', 'arcstep', 'arcstep_dirs', 'special',
       'spear', 'presentation']
VIEWS = {'primary': ('hero,side,front,gameplay', 1), 'special': ('hero,side,front,chase', 1),
         'presentation': ('hero,side,front,chase', 2), 'glide': ('hero,side,front,chase', 1),
         'turns': ('hero,side,front,chase', 1), 'startstop': ('hero,side,front,chase', 1), 'arcstep_dirs': ('hero,side,front,chase', 1),
         'spear': ('hero,side,front,chase', 1)}

args = sys.argv[1:]
tag = args[0]
modules = args[args.index('--modules')+1].split(',') if '--modules' in args else ALL
jobs = int(args[args.index('--jobs')+1]) if '--jobs' in args else 3
out = ROOT/'art/anim'/tag
out.mkdir(parents=True, exist_ok=True)
logs = ROOT/'art/anim/wip/logs'
logs.mkdir(parents=True, exist_ok=True)


def blender(script, *extra, log):
    cmd = [BLENDER, '--background', '--factory-startup', '--python-exit-code', '1',
           '--python', str(TOOLS/script), '--', *extra]
    with open(log, 'w', encoding='utf-8') as f:
        r = subprocess.run(cmd, cwd=ROOT, stdout=f, stderr=subprocess.STDOUT)
    return r.returncode


def preview(m):
    views, step = VIEWS.get(m, ('hero,side,front', 1))
    extra = [m, '--views', views, '--step', str(step)]
    if '--no-render' in args:
        extra.append('--no-render')
    code = blender('preview.py', *extra, log=logs/f'{tag}-preview-{m}.log')
    print('PREVIEW', m, 'exit', code, flush=True)
    return m, code


def seams():
    code = blender('seams.py', *ALL, log=logs/f'{tag}-seams.log')
    print('SEAMS exit', code, flush=True)
    return code


with ThreadPoolExecutor(jobs) as ex:
    fs = [ex.submit(preview, m) for m in modules]
    fseam = ex.submit(seams)
    codes = dict(f.result() for f in fs)
    seam_code = fseam.result()

if '--no-render' not in args:
    for m in modules:
        for clip in sorted((ROOT/'art/anim/wip'/m).glob('*/clip.json')):
            subprocess.run([sys.executable, str(TOOLS/'sheet.py'), str(clip.parent), '--cols', '8'], cwd=ROOT,
                           stdout=subprocess.DEVNULL)
    subprocess.run([sys.executable, str(TOOLS/'package_review.py')], cwd=ROOT)

summary = {'tag': tag, 'preview_exit': codes, 'seams_exit': seam_code, 'clips': [], 'seams': []}
for m in ALL:
    q = ROOT/'art/anim/wip'/m/'qa.json'
    if q.exists():
        for c in json.loads(q.read_text()):
            summary['clips'].append({k: c.get(k) for k in (
                'title', 'module', 'status', 'bake_error_m', 'ik_miss_m', 'max_leg_extension',
                'max_planted_slide_m_per_frame', 'min_body_z', 'max_arm_raise_deg', 'arm_clear_min_m',
                'arm_clearance', 'hand_qa_summary', 'hand_contact_max_mm', 'hand_contact_worst', 'hand_contact',
                'hand_orient_ok', 'hand_orient', 'pad_contact_ok', 'pad_contact_max_mm', 'pad_contact_worst',
                'pad_contact', 'pad_follow_max', 'halo_clear_push_max_mm', 'pad_pop_mm_f2', 'halo_pop_mm_f2',
                'full_qa_ok', 'hand_nat_ok')})
sp = ROOT/'art/anim/wip'/f"seams-{'-'.join(ALL)}.json"
if sp.exists():
    summary['seams'] = json.loads(sp.read_text())
bad = [c['title'] for c in summary['clips'] if c['status'] != 'PASS']
worst = max((s['max_mm'] for s in summary['seams']), default=None)
summary['result'] = {'clips': len(summary['clips']), 'not_pass': bad, 'seam_count': len(summary['seams']),
                     'worst_seam_mm': worst, 'seams_over_0.05mm': [s for s in summary['seams'] if s['max_mm'] > 0.05]}
(out/'qa-summary.json').write_text(json.dumps(summary, indent=1), encoding='utf-8')
print('SUMMARY', json.dumps(summary['result']), flush=True)
