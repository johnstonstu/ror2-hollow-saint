"""Summarize blend experiments: per spec, per handoff: pos ratio acc/ref, rot vs limit. python exp_sum.py [prefix]"""
import json
import sys
from pathlib import Path

WIP = Path(__file__).resolve().parents[1]
pre = sys.argv[1] if len(sys.argv) > 1 else ''
for d in sorted(WIP.glob(f'transitions/exp/{pre}*')):
    s = json.loads((d/'stitch.json').read_text(encoding='utf-8'))
    parts = []
    for h in s['handoffs']:
        pr = h['acc']/(1.25*h['clip_max_acc']+0.005)
        rr = h['rot_acc_deg']/max(20.0, 1.25*h['rot_clip_max_deg'])
        parts.append(f"f{h['frame']} pos {pr:4.2f}({h['worst_bone']}) rot {rr:4.2f}({h['rot_bone']} {h['rot_acc_deg']})"
                     + (f" root {h.get('root_acc_mps2')}" if 'root_acc_mps2' in h else ''))
    print(f"{s['name']:<34} " + ' | '.join(parts))
