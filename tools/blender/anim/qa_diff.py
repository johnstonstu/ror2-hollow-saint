"""Compare per-clip QA numbers between two refresh summaries or the live wip qa.json (system Python).

Run: python tools/blender/anim/qa_diff.py <old tag> [<new tag>|live] [--clips "Glide loop,Ascend"]
Prints every numeric field that changed for the selected clips (live = art/anim/wip/*/qa.json).
"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
args = sys.argv[1:]
clips = set(args[args.index('--clips')+1].split(',')) if '--clips' in args else None
old_tag = args[0]
new_tag = args[1] if len(args) > 1 and not args[1].startswith('--') else 'live'


def flat(d, pre=''):
    out = {}
    for k, v in d.items():
        if isinstance(v, dict):
            out.update(flat(v, f'{pre}{k}.'))
        elif isinstance(v, (int, float)) and not isinstance(v, bool):
            out[pre+k] = v
        elif isinstance(v, list) and v and all(isinstance(x, (int, float)) for x in v[:1]):
            out[pre+k] = v[0]
    return out


def live():
    rows = {}
    for q in sorted((ROOT/'art/anim/wip').glob('*/qa.json')):
        for c in json.loads(q.read_text()):
            rows[c['title']] = c
    return rows


def summary(tag):
    return {c['title']: c for c in json.loads((ROOT/'art/anim'/tag/'qa-summary.json').read_text())['clips']}


old = summary(old_tag)
new = live() if new_tag == 'live' else summary(new_tag)
for title in sorted(new):
    if clips and title not in clips:
        continue
    a, b = flat(old.get(title, {})), flat(new[title])
    diffs = [(k, a.get(k), b[k]) for k in sorted(b) if a.get(k) != b[k]]
    print(f'== {title} [{new[title].get("status")}]')
    for k, x, y in diffs:
        print(f'   {k}: {x} -> {y}')
